using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-N4 — **the tax transmission gap: WHERE the impulse is lost, measured before anything is
    /// proposed and long before anything is changed.**
    ///
    /// <para><b>The ruling this runs under</b> (Elias, 2026-08-31): a FIX TRACK, not a calibration, and
    /// **ruling-first — measure where the impulse is lost before changing anything; propose with the
    /// sourced basis attached; apply nothing.** ⚠ **Nothing in this file writes a constant.**</para>
    ///
    /// <para><b>What is already measured</b> (`COMPLETED.md` §107, C-C11): three tax types, three step
    /// sizes, both directions, three horizons — every implied output multiplier is <b>exactly 0.000</b>
    /// while each produces a real revenue impulse. This harness answers the next question: the money
    /// leaves households and arrives in the budget, so **at which term does it stop?**</para>
    ///
    /// <para><b>The method: follow the impulse field by field.</b> One tax rise on one country, and every
    /// public float of `EconomyState` sorted into <b>MOVED</b> and <b>UNMOVED</b>. A transmission gap is
    /// not an opinion about a formula; it is a list of the things that did not move.</para>
    /// </summary>
    public static class TaxTransmissionDiagnostic
    {
        private const int Seed = 777;
        private const int Turns = 6;
        private static readonly CountryId Subject = CountryId.Sweden;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-N4: where does a tax rise stop? MEASURED, PROPOSED, NOTHING APPLIED ===\n");

            Dictionary<string, float> baseline = RunCase(false);
            Dictionary<string, float> taxed = RunCase(true);

            var moved = new List<string>();
            var unmoved = new List<string>();
            foreach (KeyValuePair<string, float> field in baseline)
            {
                float after = taxed[field.Key];
                if (Mathf.Approximately(after, field.Value)) { unmoved.Add(field.Key); }
                else { moved.Add(F("{0} {1:F4} -> {2:F4}", field.Key, field.Value, after)); }
            }

            sb.Append(F("\n    Sweden, seed {0}, {1} turns, income tax +10 points held from turn 1.\n\n", Seed, Turns));
            sb.Append("    MOVED\n");
            foreach (string line in moved) { sb.Append("        ").Append(line).Append('\n'); }

            sb.Append("\n    UNMOVED (every one of these is a term the impulse never reached)\n        ");
            sb.Append(string.Join(", ", unmoved)).Append('\n');

            // ⚠ THE ASSERTION THAT BINDS THE FINDING TO ITS CALL SITE (C-C2's precedent). The claim is not
            // "the multiplier is zero" - C-C11 measured that. The claim is that CONSUMPTION is where it
            // stops, and the way to bind it is to require Consumption to be in the UNMOVED list. If a
            // later change gives tax a consumption channel, THIS fails, and the finding is retired by the
            // guard rather than by someone remembering to.
            bool consumptionUnmoved = unmoved.Contains("Consumption");
            int failures = 0;
            if (!consumptionUnmoved)
            {
                failures++;
                Debug.LogError("C-N4: Consumption MOVED under a tax rise. That is the channel this item exists to report as "
                               + "missing - if it now exists, the finding is stale and the proposal below must be re-derived "
                               + "rather than carried forward.");
            }

            sb.Append(F("\n    THE LOSS POINT: {0}\n", consumptionUnmoved ? "CONSUMPTION" : "⚠ NOT WHERE THIS ITEM SAYS - see the error above"));
            sb.Append("    `MacroSystem.ApplyNationalAccounts` computes\n");
            sb.Append("        Consumption = priorGdp * BaseConsumptionRate * consumptionInterestFactor * effectiveConsumerConfidence\n");
            sb.Append("    ⚠ There is NO DISPOSABLE-INCOME TERM. Consumption is a fixed share of PRIOR GDP, adjusted by the\n");
            sb.Append("    interest rate and by confidence - and by nothing else. A tax rise takes money from households and\n");
            sb.Append("    the C term never learns of it. Government spending enters the same identity DIRECTLY, as its own\n");
            sb.Append("    G term, which is exactly why the spending multiplier works and the tax one is identically zero.\n");
            sb.Append("    The revenue reaches Budget and GovernmentDebt, and the rise reaches ApprovalRating through\n");
            sb.Append("    `TaxHikeApprovalSensitivity`. Both are real. Neither is output.\n");

            sb.Append("\n    THE PROPOSAL - strikeable, and NOTHING IS APPLIED\n");
            sb.Append("    P-N4a. Give the C term a disposable-income input, so a tax change reaches output through the\n");
            sb.Append("           household budget rather than through a coefficient bolted onto GDP. It is the one channel\n");
            sb.Append("           that is structurally honest: it is how the money actually moves.\n");
            sb.Append("    P-N4b. ⚠ THE MAGNITUDE STAYS BILLED. Romer & Romer (AER 100(3), 2010) measure -2 to -3 for the\n");
            sb.Append("           United States from narrative shocks, and it is the LARGEST estimate in the literature.\n");
            sb.Append("           It is NOT transplanted to Sweden. A Swedish anchor needs Riksbank WP 365 (2019) or KI\n");
            sb.Append("           Occasional Paper 2021:25, neither readable from here.\n");
            sb.Append("    P-N4c. ⚠ HARD CONSTRAINT: the spending multiplier is 0.603 / 0.850 / 0.966, inside Ramey (JEP\n");
            sb.Append("           33(2), 2019) 0.6-1.0 at every horizon. ANY fix that moves it out of that band is REJECTED\n");
            sb.Append("           by that fact alone. Since C and G share the same identity, a change to C moves the\n");
            sb.Append("           measured G multiplier too - so `ResponsivenessAuditHarness` is the acceptance test, not a\n");
            sb.Append("           formality.\n");
            sb.Append("    P-N4d. BASELINE when it lands, and NOT in the same pass as C-N5.\n");

            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        private static Dictionary<string, float> RunCase(bool raiseTax)
        {
            SimulationRandom.Seed(Seed);
            var go = new GameObject("C-N4 CASE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                World world = WorldFactory.CreateDefault();
                sim.SetWorld(world);

                Country subject = world.GetCountry(Subject);
                var none = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { none[c.Id] = PolicyDecision.None(); }

                var acting = new Dictionary<CountryId, PolicyDecision>(none);
                if (raiseTax)
                {
                    var decision = new PolicyDecision();
                    foreach (TaxLine line in subject.TaxLines)
                    {
                        if (line.Type == TaxType.IncomeTax && line.IsImplemented)
                        {
                            decision.TaxRateOverrides[TaxType.IncomeTax] = line.Rate + 10f;
                        }
                    }

                    acting[Subject] = decision;
                }

                for (int t = 0; t < Turns; t++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(t == 0 ? acting : none);
                }

                var state = new Dictionary<string, float>();
                foreach (FieldInfo f in typeof(EconomyState).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (f.FieldType == typeof(float)) { state[f.Name] = (float)f.GetValue(subject.State); }
                }

                return state;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
