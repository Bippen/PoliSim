using System;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The Labor Market category's composition-and-coexistence harness (pass 3, 2026-08-26) - the
    /// labor sibling of LawCompositionDiagnostic, extended for what the coexistence ruling makes
    /// new: labor dials have TWO writers (LaborPolicyBill sets the statutory base, laws sum deltas
    /// on top), so beyond the C&amp;J harness's stacking/exact-repeal claims this one must prove
    /// ORDER INVARIANCE (bill-then-laws lands byte-identical to laws-then-bill) and that repeal
    /// returns each dial exactly to the BILL-SET base, not the seed.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.LaborLawCompositionDiagnostic.Run -logFile &lt;path&gt;`
    ///
    /// Uses the REAL private code paths by reflection (ApplyLawBillEffects for laws,
    /// ApplyLaborBillEffects for the bill) - the LawCompositionDiagnostic idiom, never a
    /// reimplementation. Expected values are computed here by independently summing LawCatalog's
    /// own labor delta fields over the enacted set onto the country's own base fields - correct
    /// regardless of any future retuning of the laws.
    ///
    /// ⚠ A divergence here is a FINDING to report, never something to tune away.
    /// </summary>
    public static class LaborLawCompositionDiagnostic
    {
        private const float Tolerance = 1e-4f;

        /// <summary>The full labor set enacted in phase 1 - grown at every batch boundary so the
        /// stacking claim always covers the whole authored category (batches 1-2 at this
        /// revision). Every labor dial touched, opposed pairs on MinimumWage, Overtime, Family
        /// and Immigration; USA's own bases keep these sums inside the clamps here (the
        /// end-of-category saturating re-run is a separate, deliberate set once all batches
        /// land, per the C&amp;J precedent).</summary>
        private static readonly string[] LaborSet =
        {
            // Batch 1.
            "raise_the_wage_act", "minimum_wage_indexation_act", "subminimum_wage_abolition_act",
            "wage_floor_restraint_act", "paid_family_leave_insurance_act", "parental_leave_expansion_act",
            "working_time_regulation_act", "working_hours_deregulation_act",
            "active_labor_market_programs_act", "skilled_worker_immigration_act",
            // Batch 2.
            "universal_child_benefit_act", "universal_childcare_act", "child_tax_credit_expansion_act",
            "family_benefit_retrenchment_act", "humanitarian_admissions_expansion_act",
            "immigration_restriction_act", "seasonal_guest_worker_program_act",
            "apprenticeship_system_act", "lifelong_learning_accounts_act", "shorter_workweek_pilot_act",
            // Batch 3 (multi-dial laws included - flexicurity and the demographic package
            // exercise composition beyond simple pairs).
            "paternity_leave_equalization_act", "parental_quota_act", "right_to_disconnect_act",
            "working_time_opt_out_act", "adequate_minimum_wage_directive_act",
            "trade_adjustment_assistance_act", "citizenship_modernization_act",
            "family_housing_support_act", "flexicurity_package_act", "demographic_response_package_act",
            // Batch 4.
            "youth_minimum_wage_act", "living_wage_procurement_act", "wage_theft_enforcement_act",
            "maternity_protection_act", "carers_leave_act", "night_work_restriction_act",
            "annualized_hours_act", "workfare_activation_act", "remote_work_visa_act",
            "labor_migration_quota_act",
            // Batch 5 - the category closes at 50; this full set IS the end-of-category
            // SATURATING composition (Retraining raw 104 and FamilyPolicy raw 101 on USA bases:
            // two ceilings genuinely reached, then released exactly by the full repeal).
            "sectoral_wage_boards_act", "public_sector_family_leave_act", "adoption_leave_parity_act",
            "telework_rights_act", "overtime_pay_threshold_act", "national_retraining_guarantee_act",
            "parental_benefit_modernization_act", "family_reunification_act",
            "refugee_work_authorization_act", "immigration_skills_levy_act"
        };

        /// <summary>The cross-category set for phase 4 - one law from each category.</summary>
        private static readonly string[] MixedSet = { "three_strikes_law", "parental_leave_expansion_act" };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var go = new GameObject("LaborLawComposition");
            try
            {
                // ── Phase 1+2: pure law stacking on the seeded base, then exact repeal. ──
                SimulationRandom.Seed(777);
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country usa = world.GetCountry(CountryId.USA);

                Debug.Log($"LABORCOMP: enacting {LaborSet.Length} labor laws on USA (seed bases: minWage {usa.MinimumWagePercentOfMedianBase:F1}, paidLeave {usa.PaidFamilyLeaveWeeksBase:F1}).");
                foreach (string lawId in LaborSet)
                {
                    if (LawCatalog.GetById(lawId) == null)
                    {
                        Debug.LogError($"LABORCOMP: '{lawId}' is not in LawCatalog - the test list itself is stale.");
                        ok = false;
                        continue;
                    }

                    ApplyLaw(sim, usa, lawId, repeal: false);
                }

                ok &= VerifyComposition(usa, "post-enactment", LaborSet);

                Debug.Log("LABORCOMP: repealing the full labor set.");
                foreach (string lawId in LaborSet)
                {
                    ApplyLaw(sim, usa, lawId, repeal: true);
                }

                ok &= VerifyExactBase(usa, "post-repeal");

                // ── Phase 3: COEXISTENCE - order invariance of bill vs laws, and repeal-to-bill-base. ──
                var billA = NewBill();
                var billB = NewBill();
                string[] coexistSet = { "raise_the_wage_act", "parental_leave_expansion_act", "working_time_regulation_act" };

                // World A: bill first, then laws.
                SimulationRandom.Seed(777);
                World worldA = WorldFactory.CreateDefault();
                sim.SetWorld(worldA);
                Country usaA = worldA.GetCountry(CountryId.USA);
                ApplyBill(sim, usaA, billA);
                foreach (string lawId in coexistSet) { ApplyLaw(sim, usaA, lawId, repeal: false); }

                // World B: laws first, then bill.
                SimulationRandom.Seed(777);
                World worldB = WorldFactory.CreateDefault();
                sim.SetWorld(worldB);
                Country usaB = worldB.GetCountry(CountryId.USA);
                foreach (string lawId in coexistSet) { ApplyLaw(sim, usaB, lawId, repeal: false); }
                ApplyBill(sim, usaB, billB);

                ok &= VerifyOrderInvariance(usaA, usaB);
                ok &= VerifyComposition(usaA, "coexistence (bill base + laws)", coexistSet);

                Debug.Log("LABORCOMP: repealing the coexistence set - dials must land exactly on the BILL-set base, not the seed.");
                foreach (string lawId in coexistSet) { ApplyLaw(sim, usaA, lawId, repeal: true); }
                ok &= VerifyExactBase(usaA, "post-repeal-to-bill-base");

                // ── Phase 4: CROSS-CATEGORY - one law each; both recomputes, neither stomps. ──
                SimulationRandom.Seed(777);
                World worldC = WorldFactory.CreateDefault();
                sim.SetWorld(worldC);
                Country usaC = worldC.GetCountry(CountryId.USA);
                foreach (string lawId in MixedSet) { ApplyLaw(sim, usaC, lawId, repeal: false); }
                ok &= VerifyComposition(usaC, "cross-category labor side", MixedSet);
                ok &= CheckValue("cross-category C&J side", "SentencingSeverity",
                    usaC.SentencingSeverity, Mathf.Clamp(50f + LawCatalog.GetById("three_strikes_law").SentencingSeverityDelta, 0f, 100f));
                foreach (string lawId in MixedSet) { ApplyLaw(sim, usaC, lawId, repeal: true); }
                ok &= VerifyExactBase(usaC, "cross-category post-repeal labor side");
                ok &= CheckValue("cross-category post-repeal C&J side", "SentencingSeverity", usaC.SentencingSeverity, 50f);

                // ── Phase 5: the SWEDEN GATE - a minimum-wage law is honestly inert where no
                // statutory minimum exists, in the dial AND in Parliament's lean. ──
                SimulationRandom.Seed(777);
                World worldD = WorldFactory.CreateDefault();
                sim.SetWorld(worldD);
                Country sweden = worldD.GetCountry(CountryId.Sweden);
                float swedenWageBefore = sweden.MinimumWagePercentOfMedian;
                ApplyLaw(sim, sweden, "raise_the_wage_act", repeal: false);
                ok &= CheckValue("Sweden gate", "MinimumWagePercentOfMedian (unchanged)", sweden.MinimumWagePercentOfMedian, swedenWageBefore);
                float swedenDirection = ParliamentSystem.GetLawBillDirection(sweden, new LawBill { LawId = "raise_the_wage_act", IsRepeal = false });
                ok &= CheckValue("Sweden gate", "GetLawBillDirection (skipped term)", swedenDirection, 0f);
                ApplyLaw(sim, sweden, "raise_the_wage_act", repeal: true);

                Debug.Log(ok
                    ? "LABORCOMP: PASS - stacking, exact repeal to base, bill/law ORDER INVARIANCE, repeal-to-bill-base, cross-category isolation and the Sweden gate all hold."
                    : "LABORCOMP: FAIL - see errors above. A divergence is a finding, not something to tune away.");
                CheckExit.Finish(ok ? 0 : 1);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static LaborPolicyBill NewBill()
        {
            // Deliberately off-seed values on every dial (USA seed: 29/0/50/50/50/50) so the
            // bill's base shift is visible on all six.
            return new LaborPolicyBill
            {
                MinimumWage = 40f,
                PaidFamilyLeaveWeeks = 8f,
                OvertimeRegulation = 62f,
                RetrainingProgram = 57f,
                FamilyPolicy = 54f,
                ImmigrationPolicy = 44f
            };
        }

        private static void ApplyLaw(SimulationManager sim, Country country, string lawId, bool repeal)
        {
            Invoke(sim, "ApplyLawBillEffects", country, new LawBill { LawId = lawId, IsRepeal = repeal });
        }

        private static void ApplyBill(SimulationManager sim, Country country, LaborPolicyBill bill)
        {
            Invoke(sim, "ApplyLaborBillEffects", country, bill);
        }

        private static void Invoke(SimulationManager sim, string method, params object[] args)
        {
            var info = typeof(SimulationManager).GetMethod(method,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (info == null)
            {
                throw new InvalidOperationException($"LABORCOMP: SimulationManager.{method} not found by reflection - method renamed?");
            }

            info.Invoke(sim, args);
        }

        /// <summary>Expected effective value per dial: clamp(country's CURRENT base + independent
        /// sum of the enacted set's deltas) - the composition claim itself.</summary>
        private static bool VerifyComposition(Country country, string label, string[] enactedIds)
        {
            float wage = country.MinimumWagePercentOfMedianBase;
            float leave = country.PaidFamilyLeaveWeeksBase;
            float overtime = country.OvertimeRegulationBase;
            float retraining = country.RetrainingProgramBase;
            float family = country.FamilyPolicyBase;
            float immigration = country.ImmigrationPolicyBase;

            foreach (string lawId in enactedIds)
            {
                LawDefinition law = LawCatalog.GetById(lawId);
                if (law == null)
                {
                    continue;
                }

                wage += law.MinimumWageDelta;
                leave += law.PaidFamilyLeaveWeeksDelta;
                overtime += law.OvertimeRegulationDelta;
                retraining += law.RetrainingProgramDelta;
                family += law.FamilyPolicyDelta;
                immigration += law.ImmigrationPolicyDelta;
            }

            bool ok = true;
            ok &= CheckValue(label, "MinimumWagePercentOfMedian", country.MinimumWagePercentOfMedian, Mathf.Clamp(wage, 0f, 100f));
            ok &= CheckValue(label, "PaidFamilyLeaveWeeks", country.PaidFamilyLeaveWeeks, Mathf.Clamp(leave, 0f, 104f));
            ok &= CheckValue(label, "OvertimeRegulationLevel", country.OvertimeRegulationLevel, Mathf.Clamp(overtime, 0f, 100f));
            ok &= CheckValue(label, "RetrainingProgramLevel", country.RetrainingProgramLevel, Mathf.Clamp(retraining, 0f, 100f));
            ok &= CheckValue(label, "FamilyPolicyLevel", country.FamilyPolicyLevel, Mathf.Clamp(family, 0f, 100f));
            ok &= CheckValue(label, "ImmigrationPolicyLevel", country.ImmigrationPolicyLevel, Mathf.Clamp(immigration, 0f, 100f));

            // The C&J harness's saturation-evidence idiom: RECORD when a raw composed sum falls
            // outside its clamp range, so the log itself proves the ceiling was genuinely
            // exercised, not merely approached (the end-of-category saturating claim).
            LogIfSaturating(label, "MinimumWagePercentOfMedian", wage, 0f, 100f);
            LogIfSaturating(label, "PaidFamilyLeaveWeeks", leave, 0f, 104f);
            LogIfSaturating(label, "OvertimeRegulationLevel", overtime, 0f, 100f);
            LogIfSaturating(label, "RetrainingProgramLevel", retraining, 0f, 100f);
            LogIfSaturating(label, "FamilyPolicyLevel", family, 0f, 100f);
            LogIfSaturating(label, "ImmigrationPolicyLevel", immigration, 0f, 100f);
            return ok;
        }

        /// <summary>After a full repeal every effective dial must equal its base EXACTLY - the
        /// coexistence form of the C&amp;J harness's exact-50 claim (base is the seed in phases
        /// 1-2, the BILL-set values in phase 3).</summary>
        private static bool VerifyExactBase(Country country, string label)
        {
            bool ok = true;
            ok &= CheckValue(label, "MinimumWagePercentOfMedian == base", country.MinimumWagePercentOfMedian, country.MinimumWagePercentOfMedianBase);
            ok &= CheckValue(label, "PaidFamilyLeaveWeeks == base", country.PaidFamilyLeaveWeeks, country.PaidFamilyLeaveWeeksBase);
            ok &= CheckValue(label, "OvertimeRegulationLevel == base", country.OvertimeRegulationLevel, country.OvertimeRegulationBase);
            ok &= CheckValue(label, "RetrainingProgramLevel == base", country.RetrainingProgramLevel, country.RetrainingProgramBase);
            ok &= CheckValue(label, "FamilyPolicyLevel == base", country.FamilyPolicyLevel, country.FamilyPolicyBase);
            ok &= CheckValue(label, "ImmigrationPolicyLevel == base", country.ImmigrationPolicyLevel, country.ImmigrationPolicyBase);
            return ok;
        }

        private static bool VerifyOrderInvariance(Country a, Country b)
        {
            bool ok = true;
            ok &= CheckValue("order-invariance", "MinimumWagePercentOfMedian", a.MinimumWagePercentOfMedian, b.MinimumWagePercentOfMedian);
            ok &= CheckValue("order-invariance", "PaidFamilyLeaveWeeks", a.PaidFamilyLeaveWeeks, b.PaidFamilyLeaveWeeks);
            ok &= CheckValue("order-invariance", "OvertimeRegulationLevel", a.OvertimeRegulationLevel, b.OvertimeRegulationLevel);
            ok &= CheckValue("order-invariance", "RetrainingProgramLevel", a.RetrainingProgramLevel, b.RetrainingProgramLevel);
            ok &= CheckValue("order-invariance", "FamilyPolicyLevel", a.FamilyPolicyLevel, b.FamilyPolicyLevel);
            ok &= CheckValue("order-invariance", "ImmigrationPolicyLevel", a.ImmigrationPolicyLevel, b.ImmigrationPolicyLevel);
            ok &= CheckValue("order-invariance (bases)", "MinimumWagePercentOfMedianBase", a.MinimumWagePercentOfMedianBase, b.MinimumWagePercentOfMedianBase);
            ok &= CheckValue("order-invariance (bases)", "PaidFamilyLeaveWeeksBase", a.PaidFamilyLeaveWeeksBase, b.PaidFamilyLeaveWeeksBase);
            return ok;
        }

        private static void LogIfSaturating(string label, string dialName, float rawUnclamped, float min, float max)
        {
            if (rawUnclamped < min || rawUnclamped > max)
            {
                Debug.Log($"LABORCOMP: {label} {dialName} raw composed value {rawUnclamped:F4} falls outside " +
                          $"[{min:F0},{max:F0}] - the clamp is genuinely exercised by this set, not merely approached.");
            }
        }

        private static bool CheckValue(string label, string name, float actual, float expected)
        {
            if (Mathf.Abs(actual - expected) > Tolerance)
            {
                Debug.LogError($"LABORCOMP: {label} {name} mismatch - expected {expected:F4}, actual {actual:F4}.");
                return false;
            }

            Debug.Log($"LABORCOMP: {label} {name} OK - {actual:F4}.");
            return true;
        }
    }
}
