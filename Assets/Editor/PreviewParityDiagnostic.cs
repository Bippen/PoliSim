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
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 14): the assert covers the boundary formula's
    /// terms - TaxHikePenalty (0 under a no-op decision) and SpendingEffect (likewise 0). Since
    /// MF-13 (K191) the reversion and the sustained terms run on approval's daily step: the real
    /// ledger holds the closed period's daily sums, the preview its projection of the coming year.
    /// Four of them - WelfareEffect, PaidLeaveEffect, DrugPolicyEffect, GiniEffect - read inputs
    /// the clone's year never moves, so each is asserted as a year of the REAL country's own day
    /// term at the preview's instant (`MacroSystem.SustainedTermsForDay`): six asserted in all.
    /// Reversion is printed as expected-different. Before MF-13 the assert covered seven terms,
    /// those four and Reversion with the two above. The economy's five - GrowthEffect and the four misery gaps - are EXPECTED-DIFFERENT BY
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
    /// the day's - the preview handed the plan nominal until this item. Since T-3 form A (§507) both sides read MacroSystem.IdentityGovernmentConsumption - k on the
    /// plan, deflated - and the clone carries k, so a clone without it previews an identity with no government and this section names it.</para>
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
                var dayTerms = new Dictionary<CountryId, MacroSystem.ApprovalDayTerms>();
                foreach (Country c in world.Countries)
                {
                    dayTerms[c.Id] = MacroSystem.SustainedTermsForDay(c, c.PotentialGrowthRate);   // MF-13 (review 2's N-1): the real country's day terms at the preview's instant
                    previews[c.Id] = sim.PreviewTurn(c.Id, PolicyDecision.None()).ApprovalTerms;
                }

                int failures = 0;
                failures += DraftTaxCase(sim, world.GetCountry(CountryId.Sweden));   // MF-13 (review 3's R3-1): a budget draft's tax impulse in the preview's approval change
                // P2-3.4 (2026-09-02): THE PROJECTED RATE PATH IS THE RULE ON THE PROJECTION. For every country
                // with a sitting chair: the preview's rule reading re-derived from the rule's own formula on the
                // preview's year-end readings equals PolicyPreview.PreviewRuleRate; the path's three steps are the
                // chair's target arithmetic (reading + lean, clamped) and the adjustment speed applied twice, from
                // the rate today; and the previewed year ran at the damped step on today's readings (AF-7, q10 - at the target until then).
                foreach (Country c in world.Countries)
                {
                    if (c.CurrentFedChair == null) { continue; }
                    PolicyPreview p = sim.PreviewTurn(c.Id, PolicyDecision.None());
                    RatePathProjection.Step[] path = RatePathProjection.Project(c, p);
                    float ruleOnPreview = Mathf.Max(0f, TaylorRule.NeutralRealRate(c) + p.PreviewInflation
                        + TaylorRule.InflationGapWeight(c) * (p.PreviewInflation - TaylorRule.InflationTarget(c))
                        + TaylorRule.UnemploymentGapWeight(c) * (p.PreviewNaturalUnemployment - p.PreviewUnemployment));
                    float ruleNow = TaylorRule.GetSuggestedInterestRate(c);
                    float targetNow = Mathf.Clamp(ruleNow + FederalReserveSystem.PolicyHead(c).RateBias, CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
                    float targetNext = Mathf.Clamp(ruleOnPreview + FederalReserveSystem.PolicyHead(c).RateBias, CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
                    float r0 = c.CurrencyZone.InterestRate;
                    float r1 = r0 + (targetNow - r0) * FederalReserveSystem.RateAdjustmentSpeed;
                    float r2 = r1 + (targetNext - r1) * FederalReserveSystem.RateAdjustmentSpeed;
                    bool ok = path != null && path.Length == 3
                        && Mathf.Abs(p.PreviewRuleRate - ruleOnPreview) < 1e-4f
                        && Mathf.Abs(path[2].RuleReading - ruleOnPreview) < 1e-4f
                        && Mathf.Abs(path[0].Rate - r0) < 1e-5f && Mathf.Abs(path[1].Rate - r1) < 1e-5f && Mathf.Abs(path[2].Rate - r2) < 1e-5f
                        && Mathf.Abs(path[1].Target - targetNow) < 1e-5f && Mathf.Abs(path[2].Target - targetNext) < 1e-5f
                        && Mathf.Abs(p.PreviewedInterestRate - r1) < 1e-4f;   // AF-7 (q10): the previewed year runs at the damped step the boundary sets, not the target
                    if (!ok)
                    {
                        failures++;
                        Debug.LogError($"PARITY: {c.Id} rate path does not re-derive: preview rule {p.PreviewRuleRate:F4} vs formula {ruleOnPreview:F4}; path "
                                       + (path == null ? "null" : $"{path[0].Rate:F4} -> {path[1].Rate:F4} -> {path[2].Rate:F4}") + $" vs {r0:F4} -> {r1:F4} -> {r2:F4}; previewed rate {p.PreviewedInterestRate:F4} vs the damped step {r1:F4} (target {targetNow:F4}).");
                    }
                    Debug.Log($"PARITY: {c.Id} rate path {r0:F2}% -> {r1:F2}% -> {r2:F2}% (rule now {ruleNow:F2}%, on the preview {ruleOnPreview:F2}%; preview inflation {p.PreviewInflation:F2}%, unemployment {p.PreviewUnemployment:F2}% vs NAIRU {p.PreviewNaturalUnemployment:F2}%) {(ok ? "ok" : "FAIL")}.");
                }
                // AF-7 (q10): THE SHARED ZONE'S PREVIEW IS DAMPED AS ITS BOUNDARY IS - the blend on today's readings, no push, the ECB President's lean,
                // clamped, and the zone's adjustment speed applied once from the rate today (EurozoneRateSystem.ApplyEurozoneRate's arithmetic).
                foreach (Country c in world.Countries)
                {
                    if (c.CurrentFedChair != null || !CurrencySystem.SharesCurrencyZoneWithOthers(c, world)) { continue; }
                    float blend = EurozoneRateSystem.GetBlendedSuggestedRate(world, c);
                    float r0 = c.CurrencyZone.InterestRate;
                    float target = Mathf.Clamp(blend + EurozoneRateSystem.HeadLean(c.CurrencyZone), CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
                    float r1 = r0 + (target - r0) * EurozoneRateSystem.RateAdjustmentSpeed;
                    PolicyPreview p = sim.PreviewTurn(c.Id, PolicyDecision.None());
                    bool ok = Mathf.Abs(p.PreviewedInterestRate - r1) < 1e-4f && c.CurrencyZone.Head != null;
                    if (!ok)
                    {
                        failures++;
                        Debug.LogError($"PARITY: {c.Id} the shared rate's preview {p.PreviewedInterestRate:F4} is not the damped step {r1:F4} (blend {blend:F4}, target {target:F4}, from {r0:F4}), or the zone has no head ({c.CurrencyZone.Head?.Name ?? "none"}).");
                    }
                    Debug.Log($"PARITY: {c.Id} shared rate previewed {p.PreviewedInterestRate:F2}% - the damped step from {r0:F2}% toward {target:F2}% (head {c.CurrencyZone.Head?.Name ?? "none"}) {(ok ? "ok" : "FAIL")}.");
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

                // K251 (review 2's M-N1): the AI finance ministry decides at this boundary on the year it closes - before, it read the stored report, which is
                // null until the first boundary has closed, so this one-boundary harness never saw it write. The preview applies no ministry decision (it
                // previews the caller's), so where the ministry writes lines here SpendingEffect is expected-different, as the Finance partner's step is
                // below; the lines it would write are read now, from the same pure rule on the same closing balance, before the boundary moves anything
                var ministryLines = new Dictionary<CountryId, int>();
                int spendingWaived = 0;   // review 4's N1: how many countries' SpendingEffect the realistic pass waives - the summary prints the true count
                MethodInfo periodOf = typeof(SimulationManager).GetMethod("GetOrSeedFiscalPeriod", BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (Country c in world.Countries)
                {
                    if (!sim.AiFinanceMinistryEnabled || sim.PlayerGoverns(c) || (sim.PlayerCountryId.HasValue && sim.PlayerCountryId.Value == c.Id) || periodOf == null) { continue; }   // review 4's N2: the player's country with a party seated sends the ministry's budget to the chamber as a bill
                    var closing = (SimulationManager.FiscalPeriod)periodOf.Invoke(sim, new object[] { c });
                    ministryLines[c.Id] = AiFinanceMinistry.Apply(c, closing.AccruedBudgetBalance, new PolicyDecision()).Lines.Count;
                }

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

                    // MF-13 (K191): the reversion and the sustained terms run on approval's daily step, so the real ledger holds the closed period's daily
                    // sums and the preview its projection of the coming year. The boundary formula's one-off terms (the tax impulse, the spending change)
                    // still read the same boundary state on both sides and are asserted against the real boundary. Four daily terms read inputs the
                    // clone's year never moves - welfare (the programmes and the seeded portfolio, LISTS the clone audit only tests for null), paid
                    // leave, drug policy (a no-op decision moves no dial) and Gini (the preview runs no Gini update) - so the preview's sum is exactly
                    // DaysPerTurn of the real country's own day term at the preview's instant, accumulated as the ledger accumulates it (review 2's N-1).
                    // Reversion, growth and the misery gaps are expected-different (printed below).
                    MacroSystem.ApprovalDayTerms day = dayTerms[c.Id];
                    failures += AssertTerm(c.Id, "WelfareEffect (a year of the day term)", YearOf(day.Welfare), prev.WelfareEffect);
                    failures += AssertTerm(c.Id, "PaidLeaveEffect (a year of the day term)", YearOf(day.PaidLeave), prev.PaidLeaveEffect);
                    failures += AssertTerm(c.Id, "DrugPolicyEffect (a year of the day term)", YearOf(day.DrugPolicy), prev.DrugPolicyEffect);
                    failures += AssertTerm(c.Id, "GiniEffect (a year of the day term)", YearOf(day.Gini), prev.GiniEffect);
                    failures += AssertTerm(c.Id, "TaxHikePenalty", real.TaxHikePenalty, prev.TaxHikePenalty);
                    // §755 (ruling A2): a Finance partner's stance step writes LINES into the no-op decision, so SpendingEffect is no longer 0 there - and the
                    // term divides by GDP and damps by the debt ratio, which the preview advances (the coming period's, by design): expected-different where
                    // the partner has stepped (the harness runs one boundary, so it stepped at this one), asserted everywhere else
                    if (c.FinanceStanceHolder != null && c.FinancePartnerSteppedOn != System.DateTime.MinValue) { spendingWaived++; }
                    else if (ministryLines.TryGetValue(c.Id, out int waivedLines) && waivedLines > 0) { spendingWaived++; }
                    if (c.FinanceStanceHolder != null && c.FinancePartnerSteppedOn != System.DateTime.MinValue) { Debug.Log($"PARITY: {c.Id} SpendingEffect expected-different - the Finance partner's stance step ({c.FinanceStanceHolder}, {c.FinanceStanceApplied:+0.000;-0.000} pp) wrote lines into the no-op decision, and the term reads GDP and the debt ratio, which the preview advances: {real.SpendingEffect:F4}/{prev.SpendingEffect:F4}"); }
                    else if (ministryLines.TryGetValue(c.Id, out int wroteLines) && wroteLines > 0) { Debug.Log($"PARITY: {c.Id} SpendingEffect expected-different - the AI finance ministry wrote {wroteLines} line(s) into the boundary's decision (K251: it decides on the year this boundary closes); the preview applies no ministry decision: real {real.SpendingEffect:F5}, preview {prev.SpendingEffect:F5}."); }
                    else { failures += AssertTerm(c.Id, "SpendingEffect", real.SpendingEffect, prev.SpendingEffect); }
                    Debug.Log($"PARITY: {c.Id} expected-different (the preview models the coming period; the daily terms are the closed period's sums against the coming year's projection, MF-13): " +
                              $"Reversion {real.Reversion:F4}/{prev.Reversion:F4} · " +
                              $"Growth {real.GrowthEffect:F4}/{prev.GrowthEffect:F4} · " +
                              $"MiseryU {real.MiseryUnemployment:F4}/{prev.MiseryUnemployment:F4} · " +
                              $"MiseryPi {real.MiseryInflation:F4}/{prev.MiseryInflation:F4} · " +
                              $"MiseryCrime {real.MiseryCrime:F4}/{prev.MiseryCrime:F4} · " +
                              $"MiseryCorr {real.MiseryCorruption:F4}/{prev.MiseryCorruption:F4}");
                }

                // §506 (2026-09-15): the clone audit, then the identity's G previewed against the boundary on a world of its own
                failures += CloneAudit(world);
                failures += IdentityGovernmentParity();
                failures += SpendingEffectWithoutMinistry(out int spendingAssertedOff);
                int countries = world.Countries.Count;

                Debug.Log(failures == 0
                    ? $"PARITY: every asserted term matches (review 4's N1, the true counts): TaxHikePenalty and the four daily terms for all {countries} countries; SpendingEffect for {countries - spendingWaived} of {countries} in the realistic pass ({spendingWaived} waived - the AI ministry's or a Finance partner's lines at the boundary, which the preview does not apply) and for {spendingAssertedOff} of {countries} with the ministry held off - no clone escape in the covered set; the clone audit clean; the preview's identity G the boundary's."
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
                // PS-6 US-15: the second chamber is a dictionary the value marks cannot reach (a clone that dropped it would carry the constructor's empty one,
                // which the reference test below cannot see) - marked by content and read back by content, as the clone's OWN copy
                var upperMark = new Dictionary<string, int> { { "MARK_UPPER_" + pass, 1000 + pass } };
                marked.SecondChamberSeats = upperMark;
                // AF-30 (the review's H1): the chairs, marked by content the same way - the preview's EFFICIENCY reads them
                var chairsMark = new Dictionary<CabinetPortfolio, CabinetMinister> { { CabinetPortfolio.HealthSocialAffairs, new CabinetMinister { Name = "MARK_MINISTER_" + pass, Portfolio = CabinetPortfolio.HealthSocialAffairs, Efficiency = 10f + pass } } };
                marked.CabinetMinisters = chairsMark;
                var copy = (Country)clone.Invoke(null, new object[] { marked });
                if (copy.CabinetMinisters == null || ReferenceEquals(copy.CabinetMinisters, chairsMark) || copy.CabinetMinisters.Count != 1
                    || !copy.CabinetMinisters.TryGetValue(CabinetPortfolio.HealthSocialAffairs, out CabinetMinister chairRead) || chairRead == null || chairRead.Name != "MARK_MINISTER_" + pass)
                {
                    if (named.Add(c.Id + ".CabinetMinisters"))
                    {
                        failures++;
                        Debug.LogError($"PARITY: CLONE ESCAPE - {c.Id}.CabinetMinisters: the preview's clone does not carry the chairs as its own copy (marked by content) - the preview would read a governing player's every chair vacant (AF-30). Add it to ClonePreviewCountry's hand-list.");
                    }
                }
                if (copy.SecondChamberSeats == null || ReferenceEquals(copy.SecondChamberSeats, upperMark) || copy.SecondChamberSeats.Count != 1
                    || !copy.SecondChamberSeats.TryGetValue("MARK_UPPER_" + pass, out int upperRead) || upperRead != 1000 + pass)
                {
                    if (named.Add(c.Id + ".SecondChamberSeats"))
                    {
                        failures++;
                        Debug.LogError($"PARITY: CLONE ESCAPE - {c.Id}.SecondChamberSeats: the preview's clone does not carry the second chamber as its own copy (marked by content). Add it to ClonePreviewCountry's hand-list, copied.");
                    }
                }
                foreach (FieldInfo f in fieldsOfCountry)
                {
                    if (f.IsNotSerialized) { continue; }   // a memo rebuilt on first read (TaxSchedule.Memo) - never state
                    object expected = f.GetValue(marked), cloned = f.GetValue(copy);
                    string difference = null;
                    Type t = f.FieldType;
                    if (t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(DateTime)) { if (!Equals(expected, cloned)) { difference = $"marked {expected}, the clone read {cloned}"; } }
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
        /// a flag is true in the first pass and false in the second, an enum its first value then its second, a date a day of 2000 or after, never MinValue
        /// (§779's review: a date field escaped the audit unseen); null for a reference the audit does not mark.</summary>
        private static object Mark(Type type, object current, int index, int pass)
        {
            if (type == typeof(float)) { return 1000f + index + 0.25f + pass; }
            if (type == typeof(double)) { return 1000.0 + index + 0.25 + pass; }
            if (type == typeof(int)) { return 1000 + index + 1000 * pass; }
            if (type == typeof(long)) { return 1000L + index + 1000L * pass; }
            if (type == typeof(bool)) { return pass == 0; }
            if (type == typeof(string)) { return "MARK" + index + "_" + pass; }
            if (type == typeof(DateTime)) { return new DateTime(2000, 1, 1).AddDays(index + 1000 * pass); }
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
            { "FinanceStanceApplied", "§755: the Finance partner's count - the preview computes the partner's step on the REAL country (previewedReal: the clone carries no government) and records never; the clone never reads it" },
            { "FinanceStanceHolder", "§755: the partner the count belongs to - read on the real country only, as FinanceStanceApplied" },
            { "FinanceStancePlayerTarget", "§755: the player partner's dial - read on the real country only, as FinanceStanceApplied" },
            { "FinancePartnerSteppedOn", "§716: the day the partner last stepped - the preview computes the step on the REAL country (previewedReal) and records never; the clone never reads it" },
            { "FinanceStanceGovernment", "§755: the government the count was made under - read on the real country only, as FinanceStanceApplied" },
        };

        /// <summary>
        /// MF-13 (review 3's R3-1): a budget draft raising VAT by two points, previewed against the standing preview. The draft's bill charges its
        /// impulse I to approval at once (approval − I, T + I) and the projected year's fading hands back I × (1 − p³⁶⁵), so the draft's tax part of
        /// the approval change is −p³⁶⁵ × I = −0.81⁴ × I a year on - the figure the game reaches. Asserted in two parts, each against the turn the
        /// clones' own ledgers recorded (the draft also moves the economy, and so the daily terms, which the ledgers carry): (1) the change less what
        /// the ledgers recorded differs between the two previews by exactly the bill's charge, −I - the charge is IN the previewed change (before the
        /// fix it was measured from after the bill and this read 0); (2) the tax term's fading differs by I × (1 − p³⁶⁵). I is measured on a clone of
        /// its own, the same bill applied the same way.
        /// </summary>
        private static int DraftTaxCase(SimulationManager sim, Country country)
        {
            TaxLine vat = null;
            foreach (TaxLine line in country.TaxLines) { if (line.Type == TaxType.VAT) { vat = line; } }
            if (vat == null) { Debug.LogError($"PARITY: {country.Id} has no VAT line - the draft case VERIFIED NOTHING"); return 1; }
            var bill = new BudgetBill();
            bill.TaxLines[TaxType.VAT] = vat.Rate + 2f;

            MethodInfo cloneOf = typeof(SimulationManager).GetMethod("ClonePreviewCountry", BindingFlags.NonPublic | BindingFlags.Static);
            var probe = cloneOf?.Invoke(null, new object[] { country }) as Country;
            if (probe == null) { Debug.LogError("PARITY: ClonePreviewCountry not reachable - the draft case VERIFIED NOTHING"); return 1; }
            float stockBefore = probe.TaxTermStock, approvalBefore = probe.State.ApprovalRating;
            ParliamentSystem.ApplyBillResult(probe, bill, true, (c, b) => { }, sim.CurrentDate.Year);
            float impulse = probe.TaxTermStock - stockBefore, charged = probe.State.ApprovalRating - approvalBefore;

            PolicyPreview standing = sim.PreviewTurnWithBudgetDraft(country.Id, PolicyDecision.None(), null);
            PolicyPreview drafted = sim.PreviewTurnWithBudgetDraft(country.Id, PolicyDecision.None(), bill);
            float Recorded(PolicyPreview p) => p.ApprovalTerms.TermSum + p.ApprovalTerms.ClampLoss + p.ApprovalTerms.EventSum;
            float outsideLedger = (drafted.ApprovalChange - Recorded(drafted)) - (standing.ApprovalChange - Recorded(standing));
            float fadingGap = drafted.ApprovalTerms.TaxTermFading - standing.ApprovalTerms.TaxTermFading;
            double keptYear = Math.Pow(TaxTerm.DailyPersistence, SimulationManager.DaysPerTurn);
            bool ok = impulse > 0.5f && Mathf.Abs(charged + impulse) <= 1e-4f
                      && Mathf.Abs(outsideLedger - charged) <= 1e-3f
                      && Math.Abs(fadingGap - impulse * (1.0 - keptYear)) <= 1e-3;
            string what = $"PARITY: {country.Id} budget draft VAT +2: impulse {impulse:F4} charged {charged:+0.0000;-0.0000} at once; the previewed change outside the ledgers differs by {outsideLedger:+0.0000;-0.0000} (the charge); the year's fading by {fadingGap:+0.0000;-0.0000} (I x (1 - p^365) = {impulse * (1.0 - keptYear):+0.0000;-0.0000}); the draft's tax part {charged + fadingGap:+0.0000;-0.0000} against -0.81^4 x I = {-keptYear * impulse:+0.0000;-0.0000}";
            if (ok) { Debug.Log(what); return 0; }
            Debug.LogError(what + " - MISMATCH (MF-13 R3-1: the draft's charge must be inside the previewed approval change, and fade as the game fades it)");
            return 1;
        }

        /// <summary>A day term summed over a turn's days, from zero, in float, as approval's daily step adds it into a fresh ledger field (MF-13).</summary>
        private static float YearOf(float perDay)
        {
            float sum = 0f;
            for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sum += perDay; }
            return sum;
        }

        /// <summary>
        /// K251 (review 4's N1): SPENDINGEFFECT FOR ALL SIX, with the AI finance ministry held off - the realistic pass above waives the term wherever the
        /// ministry writes lines at the harness's boundary (the preview applies no ministry decision), which in year one is every EU country; here the same
        /// seed, the same year of days and the same no-op decision with <c>AiFinanceMinistryEnabled = false</c>, so the boundary's one-off spending term is
        /// asserted against the preview's for every country but one a Finance partner stepped in (counted and printed). Returns the failures; the count
        /// asserted comes back in <paramref name="asserted"/>.
        /// </summary>
        private static int SpendingEffectWithoutMinistry(out int asserted)
        {
            asserted = 0;
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("PARITY_NO_MINISTRY");
            int failures = 0;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.AiFinanceMinistryEnabled = false;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                var previews = new Dictionary<CountryId, ApprovalAttribution>();
                foreach (Country c in world.Countries) { previews[c.Id] = sim.PreviewTurn(c.Id, PolicyDecision.None()).ApprovalTerms; }
                sim.AdvanceTurn(decisions);
                foreach (Country c in world.Countries)
                {
                    ApprovalAttribution real = c.ApprovalLedgerLastPeriod, prev = previews[c.Id];
                    if (real == null || prev == null) { Debug.LogError($"PARITY: {c.Id} ledger missing in the ministry-off pass."); failures++; continue; }
                    if (c.FinanceStanceHolder != null && c.FinancePartnerSteppedOn != System.DateTime.MinValue) { Debug.Log($"PARITY: {c.Id} SpendingEffect expected-different in the ministry-off pass too - the Finance partner's stance step."); continue; }
                    failures += AssertTerm(c.Id, "SpendingEffect (the ministry held off)", real.SpendingEffect, prev.SpendingEffect);
                    asserted++;
                }
            }
            finally { Object.DestroyImmediate(go); }
            return failures;
        }

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
