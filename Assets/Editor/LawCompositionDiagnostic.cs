using System;
using System.Collections.Generic;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The marathon's own "saturating composition re-run," repeated against the full 50-law catalog
    /// (close-out commit 555f4cc fixed this for the 38-law catalog; this is the promised re-run once
    /// the catalog reached the population that check exists to guard, not a new mechanism).
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.LawCompositionDiagnostic.Run -logFile &lt;path&gt;`
    ///
    /// Enacts a genuinely realistic set (27 of 50 laws, drawn across every batch and touching every
    /// dial, not hand-picked to hit a predetermined number) directly via SimulationManager's own
    /// private ApplyLawBillEffects (through reflection - the same idiom UiScreenshotDriver already
    /// uses for GameController's own private canvas-failure flag) so the REAL code path under test
    /// runs, not a hand-rolled reimplementation of it. The expected value for each dial is computed
    /// HERE by independently summing LawCatalog's own delta fields for the enacted set - not a
    /// hand-computed magic number, so the test is correct regardless of any arithmetic mistake in
    /// choosing the law list, and stays correct if any of the 50 laws' deltas are ever retuned.
    ///
    /// ⚠ A divergence here is a FINDING to report, never something to tune away - SaveLoadRoundTrip-
    /// Diagnostic's own standing rule, restated for this mechanism.
    /// </summary>
    public static class LawCompositionDiagnostic
    {
        private const float Baseline = 50f;
        private const float MinDial = 0f;
        private const float MaxDial = 100f;

        // Deliberately larger than the original ten-law test (27 of 50) since "if anything strains
        // at 45+, it's a finding, not a tuning target" - drawn across all five batches, every dial
        // touched by at least four laws, and the SentencingSeverity group alone (six laws) sums well
        // past +50 on its own, guaranteeing the ceiling is genuinely reached, not merely approached.
        private static readonly string[] EnactedSet =
        {
            // Sentencing-heavy - guarantees the +100 ceiling is actually reached, the exact real
            // shape the close-out finding named ("3-4 ordinarily-plausible enactments reach the
            // ceiling on their own").
            "truth_in_sentencing_act", "three_strikes_law", "mandatory_minimum_sentencing_act",
            "gang_crime_sentencing_escalation", "hate_crime_sentencing_enhancement", "antimafia_asset_confiscation_law",
            // Bail - a genuinely mixed-sign group (restrictive and toward-access laws together).
            "cash_bail_reform_act", "risk_based_pretrial_assessment", "federal_bail_reform_preventive_detention_act",
            "percentage_bail_deposit_program", "pretrial_services_agency_establishment",
            // Drug policy - mixed sign.
            "drug_decriminalization_act", "germany_cannabis_legalization", "drug_free_zone_sentencing_enhancement",
            "counter_narcotics_interdiction_funding_act",
            // Police funding.
            "community_policing_initiative", "hot_spot_policing_program", "financial_crimes_aml_unit",
            "human_trafficking_task_force",
            // Judicial funding.
            "public_defender_funding_act", "court_backlog_reduction_program", "mental_health_diversion_courts",
            "veterans_treatment_courts",
            // Border enforcement.
            "border_security_act", "frontex_border_cooperation_agreement", "ice_287g_agreements_law",
            "national_guard_border_deployment"
        };

        public static void Run()
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("LawComposition");
            bool ok = true;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country country = world.GetCountry(CountryId.USA);
                List<(string Name, int Bits)> untouched = Snapshot(country);   // P4-C2: the whole country, bit for bit, before any law

                Debug.Log($"COMPOSITION: enacting {EnactedSet.Length} of {LawCatalog.All.Count} laws.");
                foreach (string lawId in EnactedSet)
                {
                    if (LawCatalog.GetById(lawId) == null)
                    {
                        Debug.LogError($"COMPOSITION: '{lawId}' is not in LawCatalog - the test list itself is stale.");
                        ok = false;
                        continue;
                    }

                    ApplyLawBillEffects(sim, country, new LawBill { LawId = lawId, IsRepeal = false });
                }

                ok &= VerifyComposition(country, "post-enactment", EnactedSet);

                Debug.Log("COMPOSITION: repealing the full set.");
                foreach (string lawId in EnactedSet)
                {
                    ApplyLawBillEffects(sim, country, new LawBill { LawId = lawId, IsRepeal = true });
                }

                ok &= VerifyExactBaseline(country);
                ok &= VerifyByteIdentical(untouched, Snapshot(country));   // P4-C2
                ok &= VerifyInstitutions(sim, world.GetCountry(CountryId.Sweden));   // P4-C3

                Debug.Log(ok
                    ? "COMPOSITION: PASS - all six dials matched their independently-summed composed value " +
                      "(clamp genuinely reached and released), full repeal netted exactly 50.0000 on every dial."
                    : "COMPOSITION: FAIL - see errors above. A divergence is a finding, not something to tune away.");
                CheckExit.Finish(ok ? 0 : 1);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>Invokes SimulationManager's own private ApplyLawBillEffects by reflection - the
        /// real enact/repeal code path (which calls RecomputeCrimeJusticeDialsFromEnactedLaws
        /// internally), not a reimplementation of it that could drift from what actually ships.</summary>
        private static void ApplyLawBillEffects(SimulationManager sim, Country country, LawBill bill)
        {
            var method = typeof(SimulationManager).GetMethod("ApplyLawBillEffects",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("COMPOSITION: SimulationManager.ApplyLawBillEffects not found by reflection - method renamed?");
            }

            method.Invoke(sim, new object[] { country, bill });
        }

        private static bool VerifyComposition(Country country, string label, string[] enactedIds)
        {
            float expectedPolice = Baseline, expectedSentencing = Baseline, expectedBail = Baseline;
            float expectedDrug = Baseline, expectedJudicial = Baseline, expectedBorder = Baseline;

            foreach (string lawId in enactedIds)
            {
                LawDefinition law = LawCatalog.GetById(lawId);
                if (law == null)
                {
                    continue;
                }

                expectedPolice += law.PoliceFundingDelta;
                expectedSentencing += law.SentencingSeverityDelta;
                expectedBail += law.BailReformDelta;
                expectedDrug += law.DrugPolicyDelta;
                expectedJudicial += law.JudicialFundingDelta;
                expectedBorder += law.BorderEnforcementDelta;
            }

            bool ok = true;
            ok &= CheckDial(label, "PoliceFunding", country.PoliceFundingLevel, expectedPolice);
            ok &= CheckDial(label, "SentencingSeverity", country.SentencingSeverity, expectedSentencing);
            ok &= CheckDial(label, "BailReform", country.BailReformLevel, expectedBail);
            ok &= CheckDial(label, "DrugPolicy", country.DrugPolicyLevel, expectedDrug);
            ok &= CheckDial(label, "JudicialFunding", country.JudicialFundingLevel, expectedJudicial);
            ok &= CheckDial(label, "BorderEnforcement", country.BorderEnforcementLevel, expectedBorder);

            LogIfSaturating("PoliceFunding", expectedPolice);
            LogIfSaturating("SentencingSeverity", expectedSentencing);
            LogIfSaturating("BailReform", expectedBail);
            LogIfSaturating("DrugPolicy", expectedDrug);
            LogIfSaturating("JudicialFunding", expectedJudicial);
            LogIfSaturating("BorderEnforcement", expectedBorder);

            return ok;
        }

        private static void LogIfSaturating(string dialName, float rawUnclamped)
        {
            if (rawUnclamped < MinDial || rawUnclamped > MaxDial)
            {
                Debug.Log($"COMPOSITION: {dialName} raw composed value {rawUnclamped:F4} falls outside " +
                          $"[{MinDial:F0},{MaxDial:F0}] - the clamp is genuinely exercised by this set, not merely approached.");
            }
        }

        private static bool CheckDial(string label, string dialName, float actual, float expectedRaw)
        {
            float expectedClamped = Mathf.Clamp(expectedRaw, MinDial, MaxDial);
            if (Mathf.Abs(actual - expectedClamped) > 1e-4f)
            {
                Debug.LogError($"COMPOSITION: {label} {dialName} mismatch - expected {expectedClamped:F4} " +
                               $"(raw sum {expectedRaw:F4}, baseline {Baseline:F1}), actual {actual:F4}.");
                return false;
            }

            Debug.Log($"COMPOSITION: {label} {dialName} OK - {actual:F4} (raw sum {expectedRaw:F4}).");
            return true;
        }

        // P4-C3 (2026-09-04): the thirteenth effect composes like the twelve. Every LabourInstitutions law is enacted on a
        // fresh country; the natural rate must read base plus the independently summed deltas, clamped by the manager's own
        // bounds (the whole set sums to -3.6 pp - Sweden's 6.5 lands at 2.9, inside the clamp, so the sum is asserted exact);
        // then every law is repealed and the rate must be the base again, bit for bit.
        private static bool VerifyInstitutions(SimulationManager sim, Country country)
        {
            float before = country.NaturalUnemploymentRate;
            int baseBits = BitConverter.SingleToInt32Bits(country.NaturalUnemploymentRateBase);
            float expected = country.NaturalUnemploymentRateBase;
            int enacted = 0;
            foreach (LawDefinition law in LawCatalog.All)
            {
                if (law.Category != LawCategory.LabourInstitutions) { continue; }
                ApplyLawBillEffects(sim, country, new LawBill { LawId = law.Id, IsRepeal = false });
                expected += law.NaturalUnemploymentDelta;
                enacted++;
            }
            bool ok = enacted >= 10;
            if (!ok) { Debug.LogError($"COMPOSITION: P4-C3 - only {enacted} LabourInstitutions law(s) in the catalog; the category was built with ten."); }
            if (Mathf.Abs(country.NaturalUnemploymentRate - expected) > 1e-4f)
            {
                Debug.LogError($"COMPOSITION: P4-C3 - the natural rate after {enacted} enactments is {country.NaturalUnemploymentRate:F4}; base {country.NaturalUnemploymentRateBase:F4} plus the summed deltas is {expected:F4}.");
                ok = false;
            }
            foreach (LawDefinition law in LawCatalog.All)
            {
                if (law.Category != LawCategory.LabourInstitutions) { continue; }
                ApplyLawBillEffects(sim, country, new LawBill { LawId = law.Id, IsRepeal = true });
            }
            if (BitConverter.SingleToInt32Bits(country.NaturalUnemploymentRate) != baseBits)
            {
                Debug.LogError($"COMPOSITION: P4-C3 - after repealing the whole category the natural rate is {country.NaturalUnemploymentRate:R}, not the base {country.NaturalUnemploymentRateBase:R}.");
                ok = false;
            }
            Debug.Log(ok
                ? $"COMPOSITION: P4-C3 - {enacted} LabourInstitutions laws composed the natural rate from {before:F2} to {expected:F2} and back to the base bit for bit."
                : "COMPOSITION: P4-C3 - FAILED (see above).");
            return ok;
        }

        // P4-C2 (2026-09-04): repeal's promise is the whole state back, not six dials back. Every public float on the
        // Country, its EconomyState and its Sectors is snapshotted BIT FOR BIT before the first enactment and compared after
        // the last repeal; a one-ulp drift anywhere is a finding. The one named exception is ApprovalRating: enactment
        // charges the law's EnactmentApprovalCost, the political price of passing it, and a repeal does not refund a price
        // already paid - so the approval is expected to sit BELOW the untouched value by exactly the summed costs, which
        // is asserted too rather than merely excluded.
        private static List<(string Name, int Bits)> Snapshot(Country country)
        {
            var list = new List<(string Name, int Bits)>();
            foreach (FieldInfo f in typeof(Country).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType == typeof(float)) { list.Add(("Country." + f.Name, BitConverter.SingleToInt32Bits((float)f.GetValue(country)))); }
            }
            foreach (FieldInfo f in typeof(EconomyState).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType == typeof(float)) { list.Add(("State." + f.Name, BitConverter.SingleToInt32Bits((float)f.GetValue(country.State)))); }
            }
            foreach (Sector sector in country.Sectors)
            {
                foreach (FieldInfo f in typeof(Sector).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (f.FieldType == typeof(float)) { list.Add(($"Sector.{sector.Type}.{f.Name}", BitConverter.SingleToInt32Bits((float)f.GetValue(sector)))); }
                }
            }
            list.Add(("EnactedLaws.Count", country.EnactedLaws.Count));
            return list;
        }

        private static bool VerifyByteIdentical(List<(string Name, int Bits)> before, List<(string Name, int Bits)> after)
        {
            bool ok = before.Count == after.Count;
            int drifted = 0;
            float approvalBefore = 0f, approvalAfter = 0f;
            for (int i = 0; ok && i < before.Count; i++)
            {
                if (before[i].Name == "State.ApprovalRating")
                {
                    approvalBefore = BitConverter.Int32BitsToSingle(before[i].Bits);
                    approvalAfter = BitConverter.Int32BitsToSingle(after[i].Bits);
                    continue;
                }
                if (before[i].Bits != after[i].Bits)
                {
                    drifted++;
                    Debug.LogError($"COMPOSITION: after enact-then-repeal, {before[i].Name} is not byte-identical - " +
                                   $"{BitConverter.Int32BitsToSingle(before[i].Bits):R} became {BitConverter.Int32BitsToSingle(after[i].Bits):R}.");
                }
            }
            float expectedCost = 0f;
            foreach (string lawId in EnactedSet) { expectedCost += LawCatalog.GetById(lawId)?.EnactmentApprovalCost ?? 0f; }
            float expectedApproval = Mathf.Max(0f, approvalBefore - expectedCost);
            bool approvalOk = Mathf.Abs(approvalAfter - expectedApproval) <= 1e-3f;
            if (!approvalOk)
            {
                Debug.LogError($"COMPOSITION: approval after enact-then-repeal is {approvalAfter:F3}; expected {expectedApproval:F3} " +
                               $"(untouched {approvalBefore:F3} less the {expectedCost:F3} paid to pass the set - a repeal refunds no price).");
            }
            Debug.Log(drifted == 0 && approvalOk
                ? $"COMPOSITION: P4-C2 - {before.Count - 1} quantities byte-identical after enact-then-repeal of {EnactedSet.Length} laws; approval sits {expectedCost:F2} below untouched, the price paid to pass them."
                : $"COMPOSITION: P4-C2 - {drifted} quantit(ies) drifted after enact-then-repeal.");
            return ok && drifted == 0 && approvalOk;
        }

        /// <summary>After a full repeal of the entire enacted set, every dial must land on EXACTLY
        /// the 50.0000 baseline - the precise claim commit 555f4cc's fix makes and this re-run exists
        /// to confirm still holds against the full 50-law catalog, not just the 38-law one it was
        /// proven against originally.</summary>
        private static bool VerifyExactBaseline(Country country)
        {
            bool ok = true;
            ok &= CheckExactBaseline("PoliceFunding", country.PoliceFundingLevel);
            ok &= CheckExactBaseline("SentencingSeverity", country.SentencingSeverity);
            ok &= CheckExactBaseline("BailReform", country.BailReformLevel);
            ok &= CheckExactBaseline("DrugPolicy", country.DrugPolicyLevel);
            ok &= CheckExactBaseline("JudicialFunding", country.JudicialFundingLevel);
            ok &= CheckExactBaseline("BorderEnforcement", country.BorderEnforcementLevel);
            return ok;
        }

        private static bool CheckExactBaseline(string dialName, float actual)
        {
            if (Mathf.Abs(actual - Baseline) > 1e-4f)
            {
                Debug.LogError($"COMPOSITION: post-repeal {dialName} did not net back to {Baseline:F4} - actual {actual:F4}. " +
                               "This is the EXACT failure mode 555f4cc fixed (a clamp silently eating points repeal never gets back).");
                return false;
            }

            Debug.Log($"COMPOSITION: post-repeal {dialName} OK - exactly {actual:F4}.");
            return true;
        }
    }
}
