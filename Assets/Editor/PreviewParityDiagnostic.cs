using System;
using System.Collections.Generic;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Step 2's hand-list class killer: runs PreviewTurn and the real boundary FROM THE SAME
    /// STATE and compares approval ledgers TERM BY TERM - a preview-clone escape (the
    /// BaselineGini class, three recorded appearances) surfaces as a mismatched term that NAMES
    /// ITSELF, and every future term is covered the day it is added, with no list to maintain.
    ///
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 14): the assert covers the SEVEN terms whose
    /// inputs the preview does not advance - Reversion, TaxHikePenalty (0 under a no-op
    /// decision), SpendingEffect (likewise 0), WelfareEffect, PaidLeaveEffect, DrugPolicyEffect,
    /// GiniEffect (the preview never runs a Gini update; verified against PreviewTurn's body).
    /// The remaining five - GrowthEffect and the four misery gaps - are EXPECTED-DIFFERENT BY
    /// DESIGN: the preview models the COMING period (its clone runs the turn-form identity,
    /// Okun, Phillips and the crime/corruption updates before its formula), while the real
    /// boundary measures the elapsed one. They are printed for the record, never asserted, and
    /// an escape in THEIR constants (e.g. NAIRU) is outside this check's evidence - said here
    /// so the check is never cited for it.</para>
    ///
    /// <para><b>THE FISCAL LEDGER (Step 2's third section, 2026-08-25) - which side of the
    /// boundary each term sits on, stated per the build directive.</b> ZERO of its five terms
    /// (primary balance, fiscal reaction, interest at issuance, rate lag, erosion) are asserted
    /// here, and none are printed, BY DESIGN and not by omission: every one is accrued across
    /// 365 daily slices on a MOVING stock (the Phase-3 within-period feedback class), while the
    /// preview runs the single-step turn form on a clone that never enters the daily path and
    /// so has no ledger to print. Their turn-vs-daily agreement is the aggregation-equivalence
    /// bar's question (117/117 within 3%), not parity's; a term that sits on the "unadvanced
    /// inputs" side of this check's boundary does not exist in the fiscal chain. What this
    /// check DOES assert for the fiscal ledger is the hand-list property itself: the REAL
    /// country's accruing debt ledger is byte-untouched across a preview (days recorded and
    /// term sum identical before and after), so a future clone escape that reached the ledger
    /// would name itself here.</para>
    ///
    /// <para><b>§506 (2026-09-15) - two more sections, T-3's condition ("probe the preview path before it lands").</b> THE CLONE AUDIT: every value field of Country
    /// marked on a copy and read back through the preview's clone, by reflection, a field exempt only with its reason - what it found is ClonePreviewCountry's §506 block, the fields the hand-list never
    /// carried. THE IDENTITY'S G: with each country's first discretionary line raised, the G the preview hands its identity against the G the boundary's plan hands
    /// the day's - the preview handed the plan nominal until this item.</para>
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.PreviewParityDiagnostic.Run -logFile &lt;path&gt;`, or from the menu.
    /// </summary>
    public static class PreviewParityDiagnostic
    {
        private const float Tolerance = 0.0005f;

        [MenuItem("PoliSim/Run Preview Parity Diagnostic (approval terms)")]
        private static void RunFromMenu()
        {
            int code = CheckExit.Collect(Run);
            Debug.Log(code == 0 ? "PARITY: clean from menu." : $"PARITY: FAILED ({code}).");
        }

        public static void Run()
        {
            CheckExit.ArmLogFold(); // ruling 1: this advances turns; an ATTRIB during it now fails the run.
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("PARITY");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                // One full period of days so every daily system has real state, then previews
                // from the exact state the boundary will resolve.
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }

                // Step 2's third section: the real accruing DEBT ledger before any preview runs -
                // 365 observed days by now. A preview must leave it byte-untouched (the clone
                // carries null ledgers by the hand-list); this is the assert with teeth for that.
                var fiscalDaysBefore = new Dictionary<CountryId, int>();
                var fiscalTermSumBefore = new Dictionary<CountryId, float>();
                foreach (Country c in world.Countries)
                {
                    fiscalDaysBefore[c.Id] = c.FiscalLedgerAccruing?.DaysRecorded ?? -1;
                    fiscalTermSumBefore[c.Id] = c.FiscalLedgerAccruing?.TermSum ?? float.NaN;
                }

                var previews = new Dictionary<CountryId, ApprovalAttribution>();
                foreach (Country c in world.Countries)
                {
                    previews[c.Id] = sim.PreviewTurn(c.Id, PolicyDecision.None()).ApprovalTerms;
                }

                int failures = 0;
                // P2-3.4 (2026-09-02): THE PROJECTED RATE PATH IS THE RULE ON THE PROJECTION. For every country
                // with a sitting chair: the preview's rule reading re-derived from the rule's own formula on the
                // preview's year-end readings equals PolicyPreview.PreviewRuleRate; the path's three steps are the
                // chair's target arithmetic (reading + lean, clamped) and the adjustment speed applied twice, from
                // the rate today; and the previewed year ran at the target on today's readings.
                foreach (Country c in world.Countries)
                {
                    if (c.CurrentFedChair == null) { continue; }
                    PolicyPreview p = sim.PreviewTurn(c.Id, PolicyDecision.None());
                    RatePathProjection.Step[] path = RatePathProjection.Project(c, p);
                    float ruleOnPreview = Mathf.Max(0f, TaylorRule.NeutralRealRate(c) + p.PreviewInflation
                        + TaylorRule.InflationGapWeight(c) * (p.PreviewInflation - TaylorRule.InflationTarget(c))
                        + TaylorRule.UnemploymentGapWeight(c) * (p.PreviewNaturalUnemployment - p.PreviewUnemployment));
                    float ruleNow = TaylorRule.GetSuggestedInterestRate(c);
                    float targetNow = Mathf.Clamp(ruleNow + c.CurrentFedChair.RateBias, CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
                    float targetNext = Mathf.Clamp(ruleOnPreview + c.CurrentFedChair.RateBias, CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
                    float r0 = c.CurrencyZone.InterestRate;
                    float r1 = r0 + (targetNow - r0) * FederalReserveSystem.RateAdjustmentSpeed;
                    float r2 = r1 + (targetNext - r1) * FederalReserveSystem.RateAdjustmentSpeed;
                    bool ok = path != null && path.Length == 3
                        && Mathf.Abs(p.PreviewRuleRate - ruleOnPreview) < 1e-4f
                        && Mathf.Abs(path[2].RuleReading - ruleOnPreview) < 1e-4f
                        && Mathf.Abs(path[0].Rate - r0) < 1e-5f && Mathf.Abs(path[1].Rate - r1) < 1e-5f && Mathf.Abs(path[2].Rate - r2) < 1e-5f
                        && Mathf.Abs(path[1].Target - targetNow) < 1e-5f && Mathf.Abs(path[2].Target - targetNext) < 1e-5f
                        && Mathf.Abs(p.PreviewedInterestRate - targetNow) < 1e-4f;
                    if (!ok)
                    {
                        failures++;
                        Debug.LogError($"PARITY: {c.Id} rate path does not re-derive: preview rule {p.PreviewRuleRate:F4} vs formula {ruleOnPreview:F4}; path "
                                       + (path == null ? "null" : $"{path[0].Rate:F4} -> {path[1].Rate:F4} -> {path[2].Rate:F4}") + $" vs {r0:F4} -> {r1:F4} -> {r2:F4}; previewed rate {p.PreviewedInterestRate:F4} vs target {targetNow:F4}.");
                    }
                    Debug.Log($"PARITY: {c.Id} rate path {r0:F2}% -> {r1:F2}% -> {r2:F2}% (rule now {ruleNow:F2}%, on the preview {ruleOnPreview:F2}%; preview inflation {p.PreviewInflation:F2}%, unemployment {p.PreviewUnemployment:F2}% vs NAIRU {p.PreviewNaturalUnemployment:F2}%) {(ok ? "ok" : "FAIL")}.");
                }
                foreach (Country c in world.Countries)
                {
                    int daysAfter = c.FiscalLedgerAccruing?.DaysRecorded ?? -1;
                    float termSumAfter = c.FiscalLedgerAccruing?.TermSum ?? float.NaN;
                    bool untouched = daysAfter == fiscalDaysBefore[c.Id]
                        && (float.IsNaN(termSumAfter) ? float.IsNaN(fiscalTermSumBefore[c.Id]) : termSumAfter == fiscalTermSumBefore[c.Id]);
                    if (!untouched)
                    {
                        Debug.LogError($"PARITY: {c.Id} FISCAL LEDGER TOUCHED BY A PREVIEW - days {fiscalDaysBefore[c.Id]}->{daysAfter}, " +
                                       $"terms {fiscalTermSumBefore[c.Id]:F4}->{termSumAfter:F4}. A preview-clone reference escaped into the real country's ledger.");
                        failures++;
                    }
                }
                Debug.Log("PARITY: fiscal ledger - 0 of 5 terms asserted or printed BY DESIGN (every term is daily-accrued on a moving stock; " +
                          "turn-vs-daily agreement is the equivalence bar's question); the real accruing ledger asserted UNTOUCHED across the preview for all 6 countries.");

                sim.AdvanceTurn(decisions);

                foreach (Country c in world.Countries)
                {
                    ApprovalAttribution real = c.ApprovalLedgerLastPeriod;
                    ApprovalAttribution prev = previews[c.Id];
                    if (real == null || prev == null)
                    {
                        Debug.LogError($"PARITY: {c.Id} ledger missing (real={(real != null)}, preview={(prev != null)}) - the recording itself is broken.");
                        failures++;
                        continue;
                    }

                    failures += AssertTerm(c.Id, "Reversion", real.Reversion, prev.Reversion);
                    failures += AssertTerm(c.Id, "TaxHikePenalty", real.TaxHikePenalty, prev.TaxHikePenalty);
                    failures += AssertTerm(c.Id, "SpendingEffect", real.SpendingEffect, prev.SpendingEffect);
                    failures += AssertTerm(c.Id, "WelfareEffect", real.WelfareEffect, prev.WelfareEffect);
                    failures += AssertTerm(c.Id, "PaidLeaveEffect", real.PaidLeaveEffect, prev.PaidLeaveEffect);
                    failures += AssertTerm(c.Id, "DrugPolicyEffect", real.DrugPolicyEffect, prev.DrugPolicyEffect);
                    failures += AssertTerm(c.Id, "GiniEffect", real.GiniEffect, prev.GiniEffect);

                    Debug.Log($"PARITY: {c.Id} expected-different (the preview models the coming period): " +
                              $"Growth {real.GrowthEffect:F4}/{prev.GrowthEffect:F4} · " +
                              $"MiseryU {real.MiseryUnemployment:F4}/{prev.MiseryUnemployment:F4} · " +
                              $"MiseryPi {real.MiseryInflation:F4}/{prev.MiseryInflation:F4} · " +
                              $"MiseryCrime {real.MiseryCrime:F4}/{prev.MiseryCrime:F4} · " +
                              $"MiseryCorr {real.MiseryCorruption:F4}/{prev.MiseryCorruption:F4}");
                }

                // §506 (2026-09-15): the clone audit, then the identity's G previewed against the boundary on a world of its own
                failures += CloneAudit(world);
                failures += IdentityGovernmentParity();

                Debug.Log(failures == 0
                    ? "PARITY: 7 of 7 asserted terms match for all 6 countries - no clone escape in the covered set; the clone audit clean; the preview's identity G the boundary's."
                    : $"PARITY: {failures} mismatches - each names the escaped input above.");
                CheckExit.Finish(failures == 0 ? 0 : 1);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// §506 (2026-09-15) - T-3's condition, *"probe the preview path before it lands"*: THE PREVIEW HANDS THE IDENTITY THE BOUNDARY'S G. A world with the AI
        /// ministry off, a year of days, each country's first discretionary line raised ten per cent (the mechanism engaged, not at no policy); the preview's identity
        /// argument (PolicyPreview.PreviewIdentityGovernment) against the G the boundary's plan hands the next day's identity (SimulationManager.GetIdentityGovernmentConsumption,
        /// read right after the boundary - only the day moves the price level), within a hundredth of a per cent.
        /// </summary>
        private static int IdentityGovernmentParity()
        {
            const float raisePercent = 10f;
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("PARITY_IDENTITY_G");
            int failures = 0;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.AiFinanceMinistryEnabled = false;
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                // the preview through the public path's own two steps (SimulationManager.PreviewTurn is exactly these), holding the clone so a mismatch can name its lines
                MethodInfo cloneOf = typeof(SimulationManager).GetMethod("ClonePreviewCountry", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo previewOn = typeof(SimulationManager).GetMethod("PreviewTurnOnClone", BindingFlags.NonPublic | BindingFlags.Instance);
                var previews = new Dictionary<CountryId, float>();
                var clones = new Dictionary<CountryId, Country>();
                foreach (Country c in world.Countries)
                {
                    var clone = (Country)cloneOf.Invoke(null, new object[] { c });
                    previews[c.Id] = ((PolicyPreview)previewOn.Invoke(sim, new object[] { clone, c.Id, Raised(c, raisePercent) })).PreviewIdentityGovernment;
                    clones[c.Id] = clone;
                }
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = Raised(c, raisePercent); }
                sim.AdvanceTurn(decisions);
                foreach (Country c in world.Countries)
                {
                    float boundary = sim.GetIdentityGovernmentConsumption(c.Id);
                    float preview = previews[c.Id];
                    bool same = Mathf.Abs(preview - boundary) <= 1e-4f * Mathf.Abs(boundary);
                    if (!same)
                    {
                        failures++;
                        Debug.LogError($"PARITY: {c.Id} THE PREVIEW'S IDENTITY G {preview:F4} is not the boundary's {boundary:F4} ({(preview / boundary - 1f) * 100f:+0.000;-0.000} %) - the preview's turn form hands its identity a G the day will not.");
                        // name the lines: each discretionary line on the clone after its preview against the real line after the boundary, with the driver level each indexed on
                        for (int i = 0; i < c.SpendingLines.Count && i < clones[c.Id].SpendingLines.Count; i++)
                        {
                            SpendingLine real = c.SpendingLines[i], previewed = clones[c.Id].SpendingLines[i];
                            if (real.IsMandatory || Mathf.Abs(previewed.Amount - real.Amount) <= 1e-4f * Mathf.Abs(real.Amount)) { continue; }
                            Debug.LogError($"PARITY: {c.Id} line {real.Category} ({SpendingDrivers.Of(real.Category)}): preview {previewed.Amount:F4} vs boundary {real.Amount:F4} ({(previewed.Amount / real.Amount - 1f) * 100f:+0.000;-0.000} %); driver level the preview indexed on {previewed.DriverReference:F4}, the boundary {real.DriverReference:F4}; driver ratio {previewed.LastDriverRatio:F5} vs {real.LastDriverRatio:F5}.");
                        }
                    }
                    Debug.Log($"PARITY: {c.Id} identity G, first discretionary line +{raisePercent:F0} %: preview {preview:F4} vs boundary {boundary:F4} ({(preview / boundary - 1f) * 100f:+0.0000;-0.0000} %) {(same ? "ok" : "FAIL")}, price level {c.State.PriceLevel:F5}.");
                }
            }
            finally { Object.DestroyImmediate(go); }
            return failures;
        }

        private static PolicyDecision Raised(Country c, float percent)
        {
            PolicyDecision d = PolicyDecision.None();
            foreach (SpendingLine line in c.SpendingLines) { if (!line.IsMandatory) { d.SpendingLineChanges[line.Category] = percent; break; } }
            return d;
        }

        /// <summary>
        /// §506 (2026-09-15) - THE CLONE AUDIT, by reflection, so no list is kept. For each country a memberwise copy has EVERY public instance value field marked with a
        /// value no seed carries (a float or int a thousand and its index, a flag inverted, a string named for its index, an enum stepped, an array of marks); the preview's
        /// clone of that copy (SimulationManager.ClonePreviewCountry) must read every mark back - a field the hand-list drops reads the constructor's default, and a field
        /// equal to its default by coincidence cannot hide. A reference the copy carries and the clone does not is named too. A field the clone carries differently on
        /// purpose is exempt only with its reason (<see cref="CloneExempt"/>). The R4-1 clone-escape class - C-N4, §391/§398, §497, EN-7b, Q1 - found each time by a
        /// figure; this finds it by the field.
        /// </summary>
        private static int CloneAudit(World world)
        {
            MethodInfo clone = typeof(SimulationManager).GetMethod("ClonePreviewCountry", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo memberwise = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
            if (clone == null || memberwise == null) { Debug.LogError("PARITY: ClonePreviewCountry or MemberwiseClone not found - the clone audit verified NOTHING."); return 1; }
            FieldInfo[] fieldsOfCountry = typeof(Country).GetFields(BindingFlags.Public | BindingFlags.Instance);
            int failures = 0, valueFields = 0;
            var named = new HashSet<string>();
            // TWO PASSES: a flag has two values and an enum a few, so a single mark can land on the constructor's own default - the whole-block probe read 161 escapes
            // where 27 fields on six clones are 162, the USA's TracksHousingOverburden marked true (its false inverted) and read back as the default true. Marked both
            // ways, a dropped field reads its default in at least one pass; a field is named once however many passes catch it.
            for (int pass = 0; pass < 2; pass++)
            foreach (Country c in world.Countries)
            {
                var marked = (Country)memberwise.Invoke(c, null);
                int index = 0;
                valueFields = 0;
                foreach (FieldInfo f in fieldsOfCountry)
                {
                    index++;
                    if (f.IsNotSerialized || f.IsInitOnly) { continue; }
                    object mark = Mark(f.FieldType, f.GetValue(c), index, pass);
                    if (mark == null) { continue; }
                    f.SetValue(marked, mark);
                    valueFields++;
                }
                var copy = (Country)clone.Invoke(null, new object[] { marked });
                foreach (FieldInfo f in fieldsOfCountry)
                {
                    if (f.IsNotSerialized) { continue; }   // a memo rebuilt on first read (TaxSchedule.Memo) - never state
                    object expected = f.GetValue(marked), cloned = f.GetValue(copy);
                    string difference = null;
                    Type t = f.FieldType;
                    if (t.IsPrimitive || t.IsEnum || t == typeof(string)) { if (!Equals(expected, cloned)) { difference = $"marked {expected}, the clone read {cloned}"; } }
                    else if (t.IsArray && (t.GetElementType().IsPrimitive || t.GetElementType().IsEnum))
                    {
                        var ea = (Array)expected; var ca = (Array)cloned;
                        if ((ea == null) != (ca == null) || (ea != null && ea.Length != ca.Length)) { difference = "array length or presence differs"; }
                        else if (ea != null) { for (int i = 0; i < ea.Length; i++) { if (!Equals(ea.GetValue(i), ca.GetValue(i))) { difference = $"element {i}: marked {ea.GetValue(i)}, the clone read {ca.GetValue(i)}"; break; } } }
                    }
                    else if (expected != null && cloned == null) { difference = "the country carries it, the clone carries null"; }
                    if (difference == null) { continue; }
                    if (CloneExempt.TryGetValue(f.Name, out string reason)) { continue; }
                    if (!named.Add(c.Id + "." + f.Name)) { continue; }
                    failures++;
                    Debug.LogError($"PARITY: CLONE ESCAPE - {c.Id}.{f.Name}: {difference}. The preview's clone does not carry a field the country does (the R4-1 class); add it to ClonePreviewCountry's hand-list, or exempt it here with its reason.");
                }
            }
            Debug.Log($"PARITY: clone audit - {valueFields} value fields of Country marked two ways on six copies and read back through the preview's clone, {failures} escape(s); exempt by reason: " + string.Join("; ", CloneExempt.Keys) + ".");
            return failures;
        }

        /// <summary>A value no seed carries for a field of <paramref name="type"/> (the field's current value sizes an array), different in each <paramref name="pass"/>;
        /// a flag is true in the first pass and false in the second, an enum its first value then its second; null for a reference the audit does not mark.</summary>
        private static object Mark(Type type, object current, int index, int pass)
        {
            if (type == typeof(float)) { return 1000f + index + 0.25f + pass; }
            if (type == typeof(double)) { return 1000.0 + index + 0.25 + pass; }
            if (type == typeof(int)) { return 1000 + index + 1000 * pass; }
            if (type == typeof(long)) { return 1000L + index + 1000L * pass; }
            if (type == typeof(bool)) { return pass == 0; }
            if (type == typeof(string)) { return "MARK" + index + "_" + pass; }
            if (type.IsEnum) { Array values = Enum.GetValues(type); return values.Length > 1 ? values.GetValue(pass % values.Length) : null; }
            if (type == typeof(float[])) { var a = (float[])current; if (a == null) { return null; } var m = new float[a.Length]; for (int i = 0; i < m.Length; i++) { m[i] = 1000f + index + i + 0.5f + pass; } return m; }
            return null;
        }

        /// <summary>The fields the preview's clone carries differently on purpose, each with its reason (ClonePreviewCountry's own comments).</summary>
        private static readonly Dictionary<string, string> CloneExempt = new Dictionary<string, string>
        {
            { "FiscalLedgerAccruing", "null on the clone by design - the preview never runs the daily path, and a shared reference would be a latent escape (Step 2's third section)" },
            { "FiscalLedgerLastPeriod", "null on the clone by design, as the accruing ledger" },
            { "ApprovalLedgerLastPeriod", "null on the clone by design - a preview has no history and nothing reads it; the accruing ledger is a fresh one" },
        };

        private static int AssertTerm(CountryId id, string name, float real, float preview)
        {
            if (Mathf.Abs(real - preview) <= Tolerance)
            {
                return 0;
            }

            Debug.LogError($"PARITY: {id}.{name} MISMATCH - real {real:F5} vs preview {preview:F5}. " +
                           "A term that reads only unadvanced state and Country constants diverged: " +
                           "a preview-clone input escaped the hand-list. The term name points at its inputs.");
            return 1;
        }
    }
}
