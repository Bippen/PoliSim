using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-B1 (2026-09-05): the probing machinery LeverMapDump (P4-C1) built, lifted into one place so the budget
    /// premise dump and the lever map probe the same levers the same way - one baseline run and one probe run per
    /// lever on a fresh world from the harness's seed, driven the way the trajectory dump drives (days then
    /// AdvanceTurn). A dial probe is ONE PolicyDecision override on the first turn (the per-turn path every bill's
    /// effects take); a law probe enacts one law through SimulationManager's own ApplyLawBillEffects before the
    /// first turn. Quantities are read from the objects the game reads them from.
    /// </summary>
    public static class LeverProbes
    {
        public const int Seed = 777;                 // the harness's seed, TrajectoryBaselineDump's first
        public const CountryId Probed = CountryId.Sweden;   // the player's country in every playtest
        public const float DialProbeLevel = 80f;     // 0-100 dials are pushed from their standing to 80
        public const float TaxProbePoints = 5f;
        public const float TariffProbePoints = 5f;
        public const float RateProbePoints = 1f;
        public const float MinimumWageProbePoints = 10f;   // Kaitz points
        public const float PaidLeaveProbeWeeks = 20f;
        public const float DrawdownProbePercentOfGdp = 2f;

        public sealed class Quantity
        {
            public string Group;
            public string Name;
            public Func<Country, float> Read;
        }

        public sealed class Lever
        {
            public string Family;
            public string Name;
            public Action<SimulationManager, Country, PolicyDecision> Arm;   // fills the first turn's decision or mutates the country before it
            public string[] ViaDials;   // laws: the dials the law moves (from DialDeltas); dials: null
            public string NotArmable;   // set by RunOne when Arm threw NotSupportedException: the reason, from the message
        }

        /// <summary>One probe run: the quantities after <paramref name="horizon"/> turns; <paramref name="armed"/> false when the lever does not exist for the probed country.</summary>
        public static float[] RunOne(Lever lever, List<Quantity> quantities, int horizon, out bool armed)
        {
            armed = true;
            float[] values = null;
            RunWorld(lever, horizon, (sim, world, country, turn) =>
            {
                if (turn != horizon) { return; }
                values = new float[quantities.Count];
                for (int i = 0; i < quantities.Count; i++) { values[i] = quantities[i].Read(country); }
            }, out armed);
            return values;
        }

        /// <summary>The general form: a fresh world, the lever armed on the first turn, <paramref name="onTurn"/> called after every AdvanceTurn with the turn number (1-based).</summary>
        public static void RunWorld(Lever lever, int horizon, Action<SimulationManager, World, Country, int> onTurn, out bool armed)
        {
            armed = true;
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("LEVERPROBE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country country = world.GetCountry(Probed);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                if (lever != null)
                {
                    PolicyDecision first = PolicyDecision.None();
                    try { lever.Arm(sim, country, first); }
                    catch (NotSupportedException ex) { armed = false; lever.NotArmable = string.IsNullOrEmpty(ex.Message) || ex.Message.StartsWith("Specified method") ? "the country has no such line" : ex.Message; }
                    decisions[Probed] = first;
                }
                for (int turn = 1; turn <= horizon; turn++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    decisions[Probed] = PolicyDecision.None();
                    onTurn(sim, world, country, turn);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ---- the quantities -------------------------------------------------------------------------

        private static readonly string[] CountryLeverPatterns =
        {
            "Base", "Baseline", "Seed", "Level", "Override", "Applied", "MinimumWagePercentOfMedian", "PaidFamilyLeaveWeeks",
            "SentencingSeverity", "BaseTariffRate", "PartyApprovalRating"
        };

        public static List<Quantity> BuildQuantities()
        {
            var list = new List<Quantity>();
            FieldInfo[] stateFields = typeof(EconomyState).GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.FieldType == typeof(float)).OrderBy(f => f.Name, StringComparer.Ordinal).ToArray();
            foreach (FieldInfo f in stateFields)
            {
                FieldInfo captured = f;
                list.Add(new Quantity { Group = "EconomyState", Name = "EconomyState." + f.Name, Read = c => (float)captured.GetValue(c.State) });
            }
            FieldInfo[] countryFields = typeof(Country).GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.FieldType == typeof(float) && !CountryLeverPatterns.Any(p => f.Name.Contains(p)))
                .OrderBy(f => f.Name, StringComparer.Ordinal).ToArray();
            foreach (FieldInfo f in countryFields)
            {
                FieldInfo captured = f;
                list.Add(new Quantity { Group = "Country parameter", Name = "Country." + f.Name, Read = c => (float)captured.GetValue(c) });
            }
            foreach (SectorType type in Enum.GetValues(typeof(SectorType)))
            {
                SectorType t = type;
                list.Add(new Quantity { Group = "Sector", Name = $"Sector.OutputShareOfGdp ({t})", Read = c => SectorOf(c, t)?.OutputShareOfGdp ?? 0f });
                list.Add(new Quantity { Group = "Sector", Name = $"Sector.EmploymentShare ({t})", Read = c => SectorOf(c, t)?.EmploymentShare ?? 0f });
                list.Add(new Quantity { Group = "Sector", Name = $"Sector.SectorMetric ({t})", Read = c => SectorOf(c, t)?.SectorMetric ?? 0f });
            }
            list.Add(new Quantity { Group = "Fund", Name = "SovereignWealthFund.TotalAssets", Read = c => c.SovereignWealthFund?.TotalAssets ?? 0f });
            list.Add(new Quantity { Group = "Central bank", Name = "CurrencyZone.InterestRate", Read = c => c.CurrencyZone?.InterestRate ?? 0f });
            // P4-C3 third category (2026-09-05): the zone's monetary parameters, reachable by the MonetaryRegime laws where the parliament owns its bank.
            list.Add(new Quantity { Group = "Central bank", Name = "Zone.InflationTarget", Read = c => TaylorRule.InflationTarget(c) });
            list.Add(new Quantity { Group = "Central bank", Name = "Zone.NeutralRealRate", Read = c => TaylorRule.NeutralRealRate(c) });
            list.Add(new Quantity { Group = "Central bank", Name = "Zone.InflationGapWeight", Read = c => TaylorRule.InflationGapWeight(c) });
            list.Add(new Quantity { Group = "Central bank", Name = "Zone.UnemploymentGapWeight", Read = c => TaylorRule.UnemploymentGapWeight(c) });
            list.Add(new Quantity { Group = "Central bank", Name = "TaylorRule.GetSuggestedInterestRate", Read = c => TaylorRule.GetSuggestedInterestRate(c) });
            list.Add(new Quantity { Group = "Central bank", Name = "TaylorRule.GetOutputGapPercent", Read = c => TaylorRule.GetOutputGapPercent(c) });
            list.Add(new Quantity { Group = "Central bank", Name = "TaylorRule.GetUnemploymentGapPercent", Read = c => TaylorRule.GetUnemploymentGapPercent(c) });
            return list;
        }

        private static Sector SectorOf(Country c, SectorType t) => c.Sectors.Find(s => s.Type == t);

        // ---- the levers -----------------------------------------------------------------------------

        /// <summary>Every player lever as one probe. Budget lines are stepped by the dial's own full range (P5-B1: the audit at magnitude) when
        /// <paramref name="budgetLinePercent"/> is null, else by that percentage; laws are included when asked.</summary>
        public static List<Lever> BuildLevers(bool includeLaws, float? budgetLinePercent = 10f)
        {
            var list = new List<Lever>();
            foreach (SpendingCategory cat in Enum.GetValues(typeof(SpendingCategory)))
            {
                SpendingCategory c0 = cat;
                list.Add(new Lever { Family = "Budget line", Name = $"Budget line {c0}" + (budgetLinePercent.HasValue ? $" ({budgetLinePercent.Value:+0} %)" : " (the dial's full range)"), Arm = (sim, c, d) =>
                {
                    SpendingLine line = c.SpendingLines.Find(l => l.Category == c0);
                    if (line == null) { throw new NotSupportedException(); }
                    d.SpendingLineChanges[c0] = budgetLinePercent ?? (line.IsMandatory ? 15f : 30f);
                } });
            }
            foreach (TaxType tax in Enum.GetValues(typeof(TaxType)))
            {
                TaxType t0 = tax;
                list.Add(new Lever { Family = "Tax rate", Name = $"Tax {t0} ({TaxProbePoints:+0} pts)", Arm = (sim, c, d) =>
                {
                    TaxLine line = c.TaxLines.Find(l => l.Type == t0);
                    if (line == null) { throw new NotSupportedException(); }
                    if (!line.IsImplemented) { throw new NotSupportedException("the country has not implemented this tax: the row is drawn disabled, no slider"); }
                    d.TaxRateOverrides[t0] = line.Rate + TaxProbePoints;
                } });
            }
            foreach (WelfareProgramType w in Enum.GetValues(typeof(WelfareProgramType)))
            {
                WelfareProgramType w0 = w;
                list.Add(new Lever { Family = "Welfare generosity", Name = $"Welfare {w0} (to {DialProbeLevel:0})", Arm = (sim, c, d) =>
                {
                    if (!c.WelfarePrograms.Exists(p => p.Type == w0 && p.IsImplemented)) { throw new NotSupportedException("the country has not implemented this programme: the row is drawn disabled, no slider"); }
                    d.WelfareGenerosityOverrides[w0] = DialProbeLevel;
                } });
            }
            list.Add(new Lever { Family = "Central bank", Name = $"Policy rate ({RateProbePoints:+0} pt)", Arm = (sim, c, d) => { if (c.CurrentFedChair != null) { throw new NotSupportedException("a governor sits: the bank is independent and sets the rate (FederalReserveSystem.ApplyFedChairInterestRate); the decision's InterestRateChange is read only where no governor sits"); } d.InterestRateChange = RateProbePoints; } });
            list.Add(new Lever { Family = "Trade", Name = $"Base tariff ({TariffProbePoints:+0} pts)", Arm = (sim, c, d) => { if (sim.World.TradeBlocs.Exists(b => b.IsMember(c.Id))) { throw new NotSupportedException("a customs-union member: every partner reads the bloc's internal or external rate, never this country's base rate (TradeSystem.GetStandingTariffRate); only the per-partner overrides move its take"); } d.TariffRateChange = TariffProbePoints; } });
            list.Add(new Lever { Family = "Trade", Name = $"Partner tariff override, first partner (+{TariffProbePoints * 2:0} pts)", Arm = (sim, c, d) =>
            {
                if (c.TradePartners.Count == 0) { throw new NotSupportedException("the country has no trade partners"); }
                d.PartnerTariffOverrides[c.TradePartners[0].PartnerId] = c.BaseTariffRate + TariffProbePoints * 2f;
            } });
            list.Add(new Lever { Family = "Labour dial", Name = $"Minimum Wage ({MinimumWageProbePoints:+0} Kaitz)", Arm = (sim, c, d) =>
            {
                if (!c.MinimumWageImplemented) { throw new NotSupportedException("the country has no statutory minimum wage: the dial is not drawn for it"); }
                d.MinimumWageOverride = c.MinimumWagePercentOfMedian + MinimumWageProbePoints;
            } });
            list.Add(new Lever { Family = "Labour dial", Name = $"Paid Family Leave ({PaidLeaveProbeWeeks:+0} weeks)", Arm = (sim, c, d) => d.PaidFamilyLeaveWeeksOverride = c.PaidFamilyLeaveWeeks + PaidLeaveProbeWeeks });
            list.Add(new Lever { Family = "Labour dial", Name = "Overtime Regulation (to 80)", Arm = (sim, c, d) => d.OvertimeRegulationOverride = DialProbeLevel });
            list.Add(new Lever { Family = "Labour dial", Name = "Retraining Programs (to 80)", Arm = (sim, c, d) => d.RetrainingProgramOverride = DialProbeLevel });
            list.Add(new Lever { Family = "Labour dial", Name = "Family Policy (to 80)", Arm = (sim, c, d) => d.FamilyPolicyOverride = DialProbeLevel });
            list.Add(new Lever { Family = "Labour dial", Name = "Immigration Policy (to 80)", Arm = (sim, c, d) => d.ImmigrationPolicyOverride = DialProbeLevel });
            list.Add(new Lever { Family = "Crime dial", Name = "Police Funding (to 80)", Arm = (sim, c, d) => d.PoliceFundingOverride = DialProbeLevel });
            list.Add(new Lever { Family = "Crime dial", Name = "Sentencing Severity (to 80)", Arm = (sim, c, d) => d.SentencingSeverityOverride = DialProbeLevel });
            list.Add(new Lever { Family = "Crime dial", Name = "Bail Reform (to 80)", Arm = (sim, c, d) => d.BailReformOverride = DialProbeLevel });
            list.Add(new Lever { Family = "Crime dial", Name = "Drug Policy (to 80)", Arm = (sim, c, d) => d.DrugPolicyOverride = DialProbeLevel });
            list.Add(new Lever { Family = "Crime dial", Name = "Judicial Funding (to 80)", Arm = (sim, c, d) => d.JudicialFundingOverride = DialProbeLevel });
            list.Add(new Lever { Family = "Crime dial", Name = "Border Enforcement (to 80)", Arm = (sim, c, d) => d.BorderEnforcementOverride = DialProbeLevel });
            foreach (SectorType type in Enum.GetValues(typeof(SectorType)))
            {
                SectorType t = type;
                list.Add(new Lever { Family = "Sector dial", Name = $"{t} Subsidy (to 80)", Arm = (sim, c, d) => d.SectorSubsidyOverrides[t] = DialProbeLevel });
                list.Add(new Lever { Family = "Sector dial", Name = $"{t} Regulation (to 80)", Arm = (sim, c, d) => d.SectorRegulationOverrides[t] = DialProbeLevel });
                list.Add(new Lever { Family = "Sector dial", Name = $"{t} Tax Credits (to 80)", Arm = (sim, c, d) => d.SectorTaxCreditOverrides[t] = DialProbeLevel });
                list.Add(new Lever { Family = "Sector dial", Name = $"{t} Research Grants (to 80)", Arm = (sim, c, d) => d.SectorResearchGrantsOverrides[t] = DialProbeLevel });
                list.Add(new Lever { Family = "Sector dial", Name = $"{t} Nationalization / Deregulation (to 80)", Arm = (sim, c, d) => d.SectorDeregulationNationalizationOverrides[t] = DialProbeLevel });
            }
            list.Add(new Lever { Family = "Fund", Name = "Fund contribution rate (+1 pt)", Arm = (sim, c, d) => { RequireFund(c); d.SwfContributionRateOverride = c.SovereignWealthFund.ContributionRatePercent + 1f; } });
            // P5-B4: the fund's domestic allocation is no longer a slider (C-N6: nothing reads the field; the surface is retired), so it is not in the audit.
            list.Add(new Lever { Family = "Fund", Name = "Fund equities weight (+20)", Arm = (sim, c, d) => { RequireFund(c); d.SwfEquitiesWeightOverride = c.SovereignWealthFund.EquitiesWeight + 20f; } });
            list.Add(new Lever { Family = "Fund", Name = "Fund bonds weight (+20)", Arm = (sim, c, d) => { RequireFund(c); d.SwfBondsWeightOverride = c.SovereignWealthFund.BondsWeight + 20f; } });
            list.Add(new Lever { Family = "Fund", Name = "Fund infrastructure weight (+20)", Arm = (sim, c, d) => { RequireFund(c); d.SwfInfrastructureWeightOverride = c.SovereignWealthFund.InfrastructureWeight + 20f; } });
            list.Add(new Lever { Family = "Fund", Name = "Fund real-estate weight (+20)", Arm = (sim, c, d) => { RequireFund(c); d.SwfRealEstateWeightOverride = c.SovereignWealthFund.RealEstateWeight + 20f; } });
            list.Add(new Lever { Family = "Fund", Name = $"Fund drawdown ({DrawdownProbePercentOfGdp:0} % of GDP)", Arm = (sim, c, d) =>
            {
                RequireFund(c);
                Invoke(sim, "ApplySwfDrawdownBillEffects", c, new SwfDrawdownBill { WithdrawalPercentOfGdp = DrawdownProbePercentOfGdp });
            } });
            if (includeLaws)
            {
                foreach (LawDefinition law in LawCatalog.All)
                {
                    LawDefinition l = law;
                    list.Add(new Lever { Family = "Law: " + l.Category, Name = $"Law {l.Id} - {l.Name}", ViaDials = DialsMoved(l),
                        Arm = (sim, c, d) => Invoke(sim, "ApplyLawBillEffects", c, new LawBill { LawId = l.Id, IsRepeal = false }) });
                }
            }
            return list;
        }

        private static void RequireFund(Country c) { if (c.SovereignWealthFund == null) { throw new NotSupportedException("the country has no sovereign fund"); } }

        private static readonly string[] LawDialNames =
        {
            "Police Funding", "Sentencing Severity", "Bail Reform", "Drug Policy", "Judicial Funding", "Border Enforcement",
            "Minimum Wage", "Paid Family Leave", "Overtime Regulation", "Retraining Programs", "Family Policy", "Immigration Policy"
        };

        public static string[] DialsMoved(LawDefinition law)
        {
            float[] deltas = law.DialDeltas;
            var moved = new List<string>();
            for (int i = 0; i < deltas.Length && i < LawDialNames.Length; i++)
            {
                if (Mathf.Abs(deltas[i]) > 0f) { moved.Add(LawDialNames[i] + " " + deltas[i].ToString("+0.#;-0.#", System.Globalization.CultureInfo.InvariantCulture)); }
            }
            foreach (StructuralDelta d in law.Structural)   // P4-C3: the structural effects, in their own units
            {
                StructuralParameters.Spec spec = StructuralParameters.Of(d.Parameter);
                moved.Add(spec.Name + " " + d.Delta.ToString("+0.##;-0.##", System.Globalization.CultureInfo.InvariantCulture) + " " + spec.Unit);
            }
            return moved.ToArray();
        }

        public static void Invoke(SimulationManager sim, string method, params object[] args)
        {
            MethodInfo m = typeof(SimulationManager).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            if (m == null) { throw new InvalidOperationException($"SimulationManager.{method} not found - the real path moved."); }
            m.Invoke(sim, args);
        }

        public static string Arg(string prefix, string fallback)
        {
            foreach (string a in Environment.GetCommandLineArgs())
            {
                if (a.StartsWith(prefix, StringComparison.Ordinal)) { return a.Substring(prefix.Length); }
            }
            return fallback;
        }
    }
}
