using System;
using System.Collections;
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
    /// C-N3 — **the lever-liveness guard: every player-facing lever must measurably move the model, or be
    /// named as retired.**
    ///
    /// <para><b>Why this exists.</b> Levers had been going dead in this codebase without anything failing —
    /// the interest rate wherever a central-bank head exists, the tax dials' output channel
    /// (`COMPLETED.md` §107), and the two demographic levers the day the cohort substrate lands if only the
    /// first of their two hops is re-pointed (§108). The mechanism behind all of it is simple: <b>nothing
    /// in this build asserted that a lever moves the model.</b> A field can stop being read and every check
    /// stays green.</para>
    ///
    /// <para>⚠ <b>WHAT THIS GUARD DOES NOT ANSWER, stated because its first run made the distinction
    /// necessary.</b> A lever is LIVE here when it moves ANY public `EconomyState` float of ANY country.
    /// That is deliberately the weakest useful question — "is this field read at all" — and it is <b>not</b>
    /// the same as "does this lever do the right thing". `TaxRateOverrides` is the worked example: it is
    /// <b>LIVE</b> by this test (it moves revenue, the budget balance, debt and approval) while C-C11
    /// measured its output multiplier as <b>exactly 0.000</b>. It was briefly listed here as a known gap
    /// and the guard correctly called that record WRONG. <b>C-N4's gap is narrower than lever-death and
    /// this check cannot see it</b> — a guard for "the right thing moves" is a different, harder
    /// instrument, and pretending this one is that instrument would be the more dangerous error.</para>
    ///
    /// <para>⚠ <b>The same first run corrected S-18.</b> That row said the player's interest-rate lever was
    /// dead "in every country"; this guard found it <b>LIVE in Germany</b>. It is dead only where
    /// `CurrentFedChair != null` (USA, Sweden, Poland); the eurozone trio have no chair, so
    /// `ApplyInterestRateChanges` routes them to `EurozoneRateSystem.ApplyEurozoneRate`, which reads the
    /// decision and gives each member a bounded push on the shared rate. The generalisation came from
    /// testing one country and assuming the other five — the exact error the per-country retry exists to
    /// prevent.</para>
    ///
    /// <para><b>The method is C-C10's, reused rather than reinvented.</b> For each field of
    /// `PolicyDecision` in turn: step it from the country's OWN seeded value, run a world with that one
    /// field set and an otherwise identical world without, and compare **every public float field of
    /// every country's `EconomyState`**. Leave-one-out with a set of one — the same arithmetic the impact
    /// ledger's contributions use, and the same reason it is trustworthy: it compares consequences, not
    /// call sites.</para>
    ///
    /// <para><b>THE ENUMERATION</b> (the enumeration rule — a guard that silently tested fewer levers than
    /// it claims reads exactly like a clean run). Every public instance field of `PolicyDecision` is
    /// tested, and <see cref="AssertEveryFieldHasAStepper"/> fails the check if one has no stepper, so a
    /// field added later cannot slip in untested. Each field is tried against **every country in turn**
    /// and is LIVE the moment one of them moves — so a lever live in only one country is live, and a
    /// lever reported dead was tried everywhere it could be applied.</para>
    ///
    /// <para><b>The four verdicts.</b></para>
    /// <list type="bullet">
    /// <item><description><b>LIVE</b> — the model moved. Nothing to say.</description></item>
    /// <item><description><b>RETIRED</b> — dead, and dead BY DESIGN, listed in <see cref="Retired"/> with
    /// the ruling that killed it. ⚠ Non-fatal, because a lever superseded by a mechanism is legitimate;
    /// the interest rate now belongs to the central bank. But it must be NAMED, not left to be
    /// rediscovered.</description></item>
    /// <item><description><b>GAP</b> — dead, a defect, listed in <see cref="KnownGaps"/> with the item
    /// that owns the fix. Non-fatal on `PartyMarkCoverageCheck`'s precedent: a known, owned gap is a
    /// measurement, not a regression.</description></item>
    /// <item><description>⚠ <b>FAIL</b> — dead and in NEITHER table. That is the case this guard exists
    /// for: a lever that stopped working and nobody noticed.</description></item>
    /// </list>
    ///
    /// <para>⚠ <b>A listed lever that turns out LIVE also FAILS.</b> A stale retirement is as bad as an
    /// unnoticed death: it tells the next reader a lever is gone when the player can still pull it.</para>
    ///
    /// <para>⚠ <b>NOT EXERCISED fails too.</b> If no country could apply a field, the guard did not test
    /// that lever and says so rather than counting it as a pass — C-C9's assertion 4 is the precedent for
    /// reporting an untested assertion as untested.</para>
    /// </summary>
    public static class LeverLivenessCheck
    {
        private const int Seed = 777;

        /// <summary>Turns per case. Two, not one: a decision handed to `AdvanceTurn` lands in the state
        /// the turn AFTER (measured at C-C11), so a one-turn case would report every lever dead.</summary>
        private const int Turns = 3;

        /// <summary>
        /// **Dead by design.** Each entry names the ruling that retired the lever. A lever here is
        /// reported, never failed — but see <see cref="Run"/>: if one of these turns out live, THAT fails,
        /// because the record would be lying about what the player can do.
        /// </summary>
        private static readonly Dictionary<string, string> Retired = BuildRetired();

        private static Dictionary<string, string> BuildRetired()
        {
            // ⚠ THE EIGHT LEGACY DISCRETIONARY SPENDING FIELDS - superseded by a mechanism, which is the
            // legitimate way for a lever to die. `PolicyDecision.SpendingLineChanges` is the player's real
            // spending input, and for a country with a detailed portfolio `ResolveSpendingForTurn` ->
            // `BuildEffectiveDecisionForDetailedSpending` DERIVES these eight from it, overwriting whatever
            // the caller put there. Their own doc comments already say so field by field. What makes them
            // retired rather than merely conditional is a fact about the seed, checked here rather than
            // assumed: ALL SIX seeded countries now have `SpendingLines` (WorldFactory seeds USA, Sweden,
            // Germany, France, Italy and Poland), so the "country without a portfolio" branch that would
            // read them has no country left to run on. ⚠ NOTHING IN Assets/Scripts/UI WRITES THEM - they
            // are retired AND not drawable, which is the state the guard is asking for.
            const string spendingSuperseded =
                "superseded by SpendingLineChanges. ResolveSpendingForTurn derives it for any country with a "
                + "SpendingLines portfolio, and all six seeded countries have one, so the branch that would read "
                + "a player-set value has no country to run on. Not drawable: no UI file writes it.";

            return new Dictionary<string, string>
            {
                { "HealthcareSpendingChange", spendingSuperseded },
                { "DefenseSpendingChange", spendingSuperseded },
                { "InfrastructureSpendingChange", spendingSuperseded },
                { "EducationSpendingChange", spendingSuperseded },
                { "JusticeSpendingChange", spendingSuperseded },
                { "HomelandSecuritySpendingChange", spendingSuperseded },
                { "EnergySpendingChange", spendingSuperseded },
                { "HousingSpendingChange", spendingSuperseded }
            };
        }

        /// <summary>
        /// **Dead, and a defect, with the item that owns the fix.** Non-fatal for the same reason
        /// `PartyMarkCoverageCheck` reports a missing mark as a GAP: a measured, owned absence is not a
        /// regression. ⚠ An entry may only be added here with a register row to point at.
        /// </summary>
        private static readonly Dictionary<string, string> KnownGaps = new Dictionary<string, string>
        {
            {
                "SwfDomesticAllocationOverride",
                "C-N6 (the sovereign fund's domestic-allocation dial reaches nothing). The value is applied - "
                + "ApplySwfPolicyChanges clamps it and writes SovereignWealthFund.DomesticAllocationPercent, "
                + "it is cloned, seeded per country and carried on a BudgetBill - but NOTHING READS IT. "
                + "SovereignWealthFundSystem never mentions it; the four asset-class weights drive the fund's "
                + "returns alone. Found by this guard on its FIRST RUN."
            }
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-N3: the lever-liveness guard ===\n");

            int failures = AssertEveryFieldHasAStepper(sb);
            if (failures > 0)
            {
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            FieldInfo[] fields = typeof(PolicyDecision).GetFields(BindingFlags.Public | BindingFlags.Instance);
            sb.Append(F("    THE ENUMERATION: {0} PolicyDecision fields, each tried against every country until one moves.\n", fields.Length));
            sb.Append(F("    Seed {0}, {1} turns per case; a case is LIVE when any public EconomyState float of any country differs.\n\n", Seed, Turns));

            var baselines = new Dictionary<CountryId, string>();
            var live = new List<string>();
            var retired = new List<string>();
            var gaps = new List<string>();
            var dead = new List<string>();
            var notExercised = new List<string>();
            var staleRecord = new List<string>();

            foreach (FieldInfo field in fields)
            {
                string movedIn = null;
                bool everApplied = false;

                foreach (CountryId id in AllCountries())
                {
                    if (!CanApply(field.Name, id)) { continue; }

                    everApplied = true;
                    if (!baselines.TryGetValue(id, out string baseline))
                    {
                        baseline = RunCase(id, null);
                        baselines[id] = baseline;
                    }

                    string treated = RunCase(id, field.Name);
                    if (!string.Equals(baseline, treated, StringComparison.Ordinal)) { movedIn = id.ToString(); break; }
                }

                bool isRetired = Retired.ContainsKey(field.Name);
                bool isGap = KnownGaps.ContainsKey(field.Name);

                if (!everApplied)
                {
                    notExercised.Add(field.Name);
                    sb.Append(F("    {0,-40} ⚠ NOT EXERCISED - no country could apply it, so this lever was NOT TESTED.\n", field.Name));
                    continue;
                }

                if (movedIn != null)
                {
                    if (isRetired || isGap)
                    {
                        staleRecord.Add(field.Name);
                        sb.Append(F("    {0,-40} ⚠ LIVE in {1}, but the record says it is {2}. The RECORD is wrong.\n",
                            field.Name, movedIn, isRetired ? "retired" : "a known gap"));
                        continue;
                    }

                    live.Add(field.Name);
                    sb.Append(F("    {0,-40} LIVE (moves the model in {1})\n", field.Name, movedIn));
                    continue;
                }

                if (isRetired)
                {
                    retired.Add(field.Name);
                    sb.Append(F("    {0,-40} RETIRED - {1}\n", field.Name, Retired[field.Name]));
                    continue;
                }

                if (isGap)
                {
                    gaps.Add(field.Name);
                    sb.Append(F("    {0,-40} GAP - {1}\n", field.Name, KnownGaps[field.Name]));
                    continue;
                }

                dead.Add(field.Name);
                Debug.LogError($"C-N3: {field.Name} is a DEAD LEVER - the player can set it and nothing in any country's "
                               + "EconomyState moves - and it is in neither the retired table nor the known-gap table. "
                               + "Either retire it with the ruling that killed it, open an item and list it as a gap, or fix it. "
                               + "This is the case this guard exists for.");
                sb.Append(F("    {0,-40} ⚠ DEAD AND UNLISTED - see the error above.\n", field.Name));
            }

            sb.Append(F("\n    {0} LIVE · {1} RETIRED · {2} GAP · {3} DEAD-AND-UNLISTED · {4} NOT EXERCISED · {5} STALE RECORD\n",
                live.Count, retired.Count, gaps.Count, dead.Count, notExercised.Count, staleRecord.Count));

            foreach (string name in notExercised)
            {
                Debug.LogError($"C-N3: {name} was NOT EXERCISED - no country's seeded state let the stepper produce a "
                               + "non-default value, so this lever is untested rather than passing.");
            }

            foreach (string name in staleRecord)
            {
                Debug.LogError($"C-N3: {name} is listed as retired or as a known gap, but it MOVES THE MODEL. A stale "
                               + "retirement is as bad as an unnoticed death - it tells the next reader a lever is gone "
                               + "while the player can still pull it. Correct the record.");
            }

            failures = dead.Count + notExercised.Count + staleRecord.Count;
            if (failures == 0)
            {
                sb.Append("    VERDICT: every lever either moves the model or is named. Nothing died unnoticed.\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
                return;
            }

            Debug.LogError(sb.ToString());
            CheckExit.Finish(1);
        }

        private static IEnumerable<CountryId> AllCountries()
        {
            yield return CountryId.USA;
            yield return CountryId.Sweden;
            yield return CountryId.Germany;
            yield return CountryId.France;
            yield return CountryId.Italy;
            yield return CountryId.Poland;
        }

        /// <summary>Can this country's seeded state produce a non-default value for this field? Answered on
        /// a throwaway world so the decision costs no simulation.</summary>
        private static bool CanApply(string fieldName, CountryId id)
        {
            SimulationRandom.Seed(Seed);
            World probe = WorldFactory.CreateDefault();
            Country country = probe.GetCountry(id);
            if (country == null) { return false; }

            var decision = new PolicyDecision();
            Step(fieldName, country, decision);
            return Differs(decision, fieldName);
        }

        /// <summary>One world advanced <see cref="Turns"/> turns with exactly one field of the player's
        /// decision stepped, returned as every country's whole economic state.</summary>
        private static string RunCase(CountryId id, string fieldName)
        {
            SimulationRandom.Seed(Seed);
            var go = new GameObject("C-N3 CASE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                World world = WorldFactory.CreateDefault();
                sim.SetWorld(world);

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                if (fieldName != null)
                {
                    var acting = new PolicyDecision();
                    Step(fieldName, world.GetCountry(id), acting);
                    decisions[id] = acting;
                }

                for (int t = 0; t < Turns; t++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                }

                return Fingerprint(world);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>Has the stepper actually produced a non-default value for this one field?</summary>
        private static bool Differs(PolicyDecision decision, string fieldName)
        {
            FieldInfo f = typeof(PolicyDecision).GetField(fieldName);
            if (f == null) { return false; }

            object value = f.GetValue(decision);
            if (value is IDictionary dictionary) { return dictionary.Count > 0; }

            return !Equals(value, f.GetValue(new PolicyDecision()));
        }

        /// <summary>
        /// ⚠ **Every step is read off the country's own seeded value and moved by a stated amount** — no
        /// step here is an authored target, and a country that does not have the instrument produces no
        /// step at all (which is what makes the per-country retry meaningful rather than noise).
        ///
        /// The steps are deliberately LARGE. This guard answers "does anything move at all", not "by how
        /// much"; a step small enough to be lost in float noise would report a live lever dead.
        /// </summary>
        private static void Step(string fieldName, Country c, PolicyDecision d)
        {
            if (c == null) { return; }

            switch (fieldName)
            {
                case "TaxRateOverrides":
                    foreach (TaxLine line in c.TaxLines)
                    {
                        if (line.IsImplemented) { d.TaxRateOverrides[line.Type] = line.Rate + 10f; }
                    }

                    break;

                case "WelfareGenerosityOverrides":
                    foreach (WelfareProgram p in c.WelfarePrograms)
                    {
                        if (p.IsImplemented) { d.WelfareGenerosityOverrides[p.Type] = Mathf.Min(100f, p.GenerosityLevel + 25f); }
                    }

                    break;

                case "SpendingLineChanges":
                    foreach (SpendingLine line in c.SpendingLines)
                    {
                        if (!line.IsMandatory) { d.SpendingLineChanges[line.Category] = 15f; }
                    }

                    break;

                // The eight legacy discretionary fields are dollar deltas, so they are stepped as a share
                // of the country's own GDP rather than a flat amount that would be huge for Sweden and
                // trivial for the USA.
                case "HealthcareSpendingChange": d.HealthcareSpendingChange = Impulse(c); break;
                case "DefenseSpendingChange": d.DefenseSpendingChange = Impulse(c); break;
                case "InfrastructureSpendingChange": d.InfrastructureSpendingChange = Impulse(c); break;
                case "EducationSpendingChange": d.EducationSpendingChange = Impulse(c); break;
                case "JusticeSpendingChange": d.JusticeSpendingChange = Impulse(c); break;
                case "HomelandSecuritySpendingChange": d.HomelandSecuritySpendingChange = Impulse(c); break;
                case "EnergySpendingChange": d.EnergySpendingChange = Impulse(c); break;
                case "HousingSpendingChange": d.HousingSpendingChange = Impulse(c); break;

                case "InterestRateChange": d.InterestRateChange = 1f; break;
                case "TariffRateChange": d.TariffRateChange = 10f; break;

                case "PartnerTariffOverrides":
                    foreach (TradePartner partner in c.TradePartners) { d.PartnerTariffOverrides[partner.PartnerId] = 25f; }
                    break;

                case "MinimumWageOverride":
                    if (c.MinimumWageImplemented) { d.MinimumWageOverride = Mathf.Min(90f, c.MinimumWagePercentOfMedian + 15f); }
                    break;

                case "PoliceFundingOverride": d.PoliceFundingOverride = Dial(c.PoliceFundingLevel); break;
                case "SentencingSeverityOverride": d.SentencingSeverityOverride = Dial(c.SentencingSeverity); break;
                case "BailReformOverride": d.BailReformOverride = Dial(c.BailReformLevel); break;
                case "DrugPolicyOverride": d.DrugPolicyOverride = Dial(c.DrugPolicyLevel); break;
                case "JudicialFundingOverride": d.JudicialFundingOverride = Dial(c.JudicialFundingLevel); break;
                case "BorderEnforcementOverride": d.BorderEnforcementOverride = Dial(c.BorderEnforcementLevel); break;
                case "PaidFamilyLeaveWeeksOverride": d.PaidFamilyLeaveWeeksOverride = c.PaidFamilyLeaveWeeks + 12f; break;
                case "OvertimeRegulationOverride": d.OvertimeRegulationOverride = Dial(c.OvertimeRegulationLevel); break;
                case "RetrainingProgramOverride": d.RetrainingProgramOverride = Dial(c.RetrainingProgramLevel); break;
                case "FamilyPolicyOverride": d.FamilyPolicyOverride = Dial(c.FamilyPolicyLevel); break;
                case "ImmigrationPolicyOverride": d.ImmigrationPolicyOverride = Dial(c.ImmigrationPolicyLevel); break;

                case "SectorSubsidyOverrides":
                    foreach (Sector s in c.Sectors) { d.SectorSubsidyOverrides[s.Type] = Dial(s.SubsidyLevel); }
                    break;

                case "SectorRegulationOverrides":
                    foreach (Sector s in c.Sectors) { d.SectorRegulationOverrides[s.Type] = Dial(s.RegulationLevel); }
                    break;

                case "SectorTaxCreditOverrides":
                    foreach (Sector s in c.Sectors) { d.SectorTaxCreditOverrides[s.Type] = Dial(s.TaxCreditLevel); }
                    break;

                case "SectorResearchGrantsOverrides":
                    foreach (Sector s in c.Sectors) { d.SectorResearchGrantsOverrides[s.Type] = Dial(s.ResearchGrantsLevel); }
                    break;

                case "SectorDeregulationNationalizationOverrides":
                    foreach (Sector s in c.Sectors) { d.SectorDeregulationNationalizationOverrides[s.Type] = Dial(s.DeregulationNationalizationLevel); }
                    break;

                case "SwfContributionRateOverride":
                    if (c.SovereignWealthFund != null) { d.SwfContributionRateOverride = c.SovereignWealthFund.ContributionRatePercent + 5f; }
                    break;

                case "SwfDomesticAllocationOverride":
                    if (c.SovereignWealthFund != null) { d.SwfDomesticAllocationOverride = Dial(c.SovereignWealthFund.DomesticAllocationPercent); }
                    break;

                case "SwfEquitiesWeightOverride":
                    if (c.SovereignWealthFund != null) { d.SwfEquitiesWeightOverride = Dial(c.SovereignWealthFund.EquitiesWeight); }
                    break;

                case "SwfBondsWeightOverride":
                    if (c.SovereignWealthFund != null) { d.SwfBondsWeightOverride = Dial(c.SovereignWealthFund.BondsWeight); }
                    break;

                case "SwfInfrastructureWeightOverride":
                    if (c.SovereignWealthFund != null) { d.SwfInfrastructureWeightOverride = Dial(c.SovereignWealthFund.InfrastructureWeight); }
                    break;

                case "SwfRealEstateWeightOverride":
                    if (c.SovereignWealthFund != null) { d.SwfRealEstateWeightOverride = Dial(c.SovereignWealthFund.RealEstateWeight); }
                    break;
            }
        }

        /// <summary>A fiscal impulse worth 2 % of the country's own GDP — big enough that no real channel
        /// can hide inside float noise, read off the country rather than authored.</summary>
        private static float Impulse(Country c) => c.State.GDP * 0.02f;

        /// <summary>A 0-100 dial moved 30 points from where the country sits, away from whichever end it
        /// is nearer, so a dial already at its maximum is still genuinely moved.</summary>
        private static float Dial(float current) => current <= 50f ? Mathf.Min(100f, current + 30f) : Mathf.Max(0f, current - 30f);

        /// <summary>⚠ The completeness check. A `PolicyDecision` field with no stepper would be silently
        /// untested by a guard whose whole purpose is that nothing is silently untested.</summary>
        private static int AssertEveryFieldHasAStepper(StringBuilder sb)
        {
            SimulationRandom.Seed(Seed);
            World probe = WorldFactory.CreateDefault();
            int missing = 0;

            foreach (FieldInfo field in typeof(PolicyDecision).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                bool anyCountryProducesAStep = false;
                foreach (Country c in probe.Countries)
                {
                    var d = new PolicyDecision();
                    Step(field.Name, c, d);
                    if (Differs(d, field.Name)) { anyCountryProducesAStep = true; break; }
                }

                if (!anyCountryProducesAStep)
                {
                    missing++;
                    Debug.LogError($"C-N3: PolicyDecision.{field.Name} has no stepper that any country can apply, so this "
                                   + "guard would report it untested forever. Add a stepper - a lever with no way to pull it "
                                   + "is exactly what this check exists to make impossible.");
                }
            }

            sb.Append(missing == 0
                ? "    the steppers   OK - every PolicyDecision field has a stepper some country can apply.\n"
                : F("    the steppers   ⚠ {0} FIELD(S) HAVE NO APPLICABLE STEPPER - see the errors above.\n", missing));

            return missing;
        }

        private static string Fingerprint(World world)
        {
            var sb = new StringBuilder();
            foreach (Country c in world.Countries)
            {
                sb.Append(c.Id).Append(':');
                foreach (FieldInfo f in typeof(EconomyState).GetFields())
                {
                    if (f.FieldType != typeof(float)) { continue; }
                    sb.Append(f.Name).Append('=')
                      .Append(((float)f.GetValue(c.State)).ToString("R", CultureInfo.InvariantCulture)).Append('|');
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
