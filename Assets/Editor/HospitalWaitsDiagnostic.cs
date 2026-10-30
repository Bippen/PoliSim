using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// MF q28 (ruled 2026-10-18: "hospital waits read spending, as coverage and mortality do"; the audit's GA-7-1): the proof that the waits read
    /// real health spending per age-cost unit against its seed at the derived elasticity (<see cref="HealthFamily.WaitSpendingElasticity"/>), the
    /// ministry's effectiveness kept as its multiplier. It fails while any of these is false, for Sweden, Poland and Italy (the three that publish waits):
    /// (a) THE ARITHMETIC: at the seed the waits' target factor at a fifth more and a fifth less real health spending is the factor at the seed's
    /// spending times (1 + x)^(-e); at the seed's own spending the factor is the effectiveness term alone (the old form, unchanged where spending holds).
    /// (b) THE PLAYED PATH: each country the player for three years from its own start, the health lines raised a tenth in the first year's decision
    /// and the level left to stand, against the same run untouched and against a tenth cut: both waits SHORTER when spending rose and LONGER when it fell,
    /// the raise must have landed, and each wait's gap against the untouched run, NET of the part the effectiveness flow alone gives (the first year's
    /// allocated / requested, which the formula before q28 already read), is at least <see cref="GapShare"/> of the closed-form spending gap
    /// seed x |1 - q^-e| x (1 - (1 - k)^years), q the run's spending ratio over the untouched run's - so a fifth of the elasticity fails it (the q28 review's L1).
    /// Every year's step on every run equals the stated arithmetic - wait + (seed x factor - wait) x the reversion, the factor read from
    /// <see cref="HealthFamily.TargetsFor"/>.
    /// (c) THE IDLE PATHS: (c1) the shared no-policy century's first ten years (<see cref="NoPolicyCentury"/>, the player's own book, whose caseload lines carry
    /// prices, their driver and the real wage since K269 (a) - until then prices and their driver only) keep both waits within <see cref="IdleBand"/> of their sourced seeds; (c2) THE AI BOOK (the q28 review's M1): Germany the
    /// player, so Sweden, Poland and Italy run the AI's book for ten years and their health lines also carry the real wage index (RF-2, §384), and the AI
    /// finance ministry's uniform line moves under its fiscal rule (AiFinanceMinistry) besides. (i) Every year each wait sits at the step identity with the
    /// factor the model computes from the MEASURED spending ratio and effectiveness, to the tolerance. (ii) The drift is decomposed and printed: the wage
    /// path's part (the DECLARED M1 drift, (wage index / its seed)^-e), the AI ministry's line moves (the product of the Health portfolio's yearly allocation
    /// ratios, the years it moved, and the debt-crisis phase), and the effectiveness term. (iii) Where the Health portfolio's allocation was met every year
    /// (no AI line move), spending per age-cost unit follows the real wage index to <see cref="WagePathTolerance"/> - the M1 declaration holds where the book
    /// follows the wage path; a country the AI moved is decomposed, not judged (Italy's book cuts health under the EU rule on the merged tree).
    /// </summary>
    public static class HospitalWaitsDiagnostic
    {
        private static readonly CountryId[] Three = { CountryId.Sweden, CountryId.Poland, CountryId.Italy };

        /// <summary>CONVENTION - the share the played path moves the health lines by, inside the decision's seed range for every health line.</summary>
        private const float Step = 0.10f;
        /// <summary>CONVENTION - the share the arithmetic test moves real health spending by, either way.</summary>
        private const float ArithmeticShare = 0.20f;
        /// <summary>CONVENTION - years of the played path (the audit's GA-7-1 ran three).</summary>
        private const int Years = 3;
        /// <summary>CONVENTION - the share of the closed-form spending gap a played wait's gap, net of the effectiveness flow's part, must reach: a fifth of the
        /// elasticity gives a fifth of the gap and fails.</summary>
        private const float GapShare = 0.5f;
        /// <summary>CONVENTION - the least the played raise or cut must move real health spending per age-cost unit against the untouched run, or (b) proves nothing.</summary>
        private const float MinLanded = 0.05f;
        /// <summary>CONVENTION - the relative tolerance of the arithmetic.</summary>
        private const float Tolerance = 1e-3f;
        /// <summary>[AUTHORED-DRAFT] - the idle guard on the player's own book: at no policy each wait stays within this share of its sourced seed over ten years. A sanity band, not a source.</summary>
        private const float IdleBand = 0.25f;
        /// <summary>CONVENTION - on an AI book the AI never moved, the relative gap allowed between spending per age-cost unit and the real wage index after ten years
        /// (the identity is exact but for the first indexation's timing and the seed band's clamp).</summary>
        private const float WagePathTolerance = 0.01f;
        /// <summary>CONVENTION - a year's Health allocation ratio further than this from 1 is an AI line move.</summary>
        private const float MoveThreshold = 1e-4f;
        /// <summary>CONVENTION - the idle horizon (the audit's GA-7-1 ran ten).</summary>
        private const int IdleYears = 10;

        private static bool _ok;
        private static StringBuilder _sb;

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        private static void Check(bool pass, string line)
        {
            if (!pass) { _ok = false; }
            _sb.Append(pass ? "      ok   " : "      FAIL ").Append(line).Append('\n');
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            _ok = true;
            _sb = new StringBuilder();
            _sb.Append(F("HOSPITALWAITS: MF q28 - the waits read real health spending per age-cost unit at e = 1 / ({0:F3} - ({1:F3})) = {2:F4}, the effectiveness multiplier kept\n",
                HealthFamily.WaitSupplyElasticity, HealthFamily.WaitDemandElasticity, HealthFamily.WaitSpendingElasticity));

            Arithmetic();
            foreach (CountryId id in Three) { Played(id); }
            Idle();
            AiBook();

            Debug.Log(_sb.ToString());
            Debug.Log(_ok
                ? "HOSPITALWAITS: PASS - the waits read health spending at the derived elasticity in Sweden, Poland and Italy: shorter when it rises, longer when it falls, by the stated arithmetic, near their seeds on the player's idle book, and on the AI's book at the model's step with the drift decomposed, following the real wage index where the AI moved no line."
                : "HOSPITALWAITS: FAILED (see above).");
            CheckExit.Finish(_ok ? 0 : 1);
        }

        // ============================================================ (a) the arithmetic at the seed
        private static void Arithmetic()
        {
            _sb.Append("    a. the target factor at the seed\n");
            SimulationRandom.Seed(NoPolicyCentury.Seed);
            using IDisposable epoch = SimulationManager.EpochScope();
            World world = WorldFactory.CreateDefault();
            foreach (CountryId id in Three)
            {
                Country c = world.GetCountry(id);
                if (c == null || c.Health == null || !c.Health.Seeded || !c.Health.HasWaits) { Check(false, F("{0,-7} carries no seeded waits", id)); continue; }
                float real = HealthFamily.HealthSpendingReal(c);
                float atSeed = HealthFamily.TargetsFor(c, real).WaitFactor;
                float effectivenessRatio = Mathf.Max(0.01f, HealthFamily.HealthEffectiveness(c) / c.Health.EfficiencySeed);
                float oldForm = Mathf.Pow(1f / effectivenessRatio, HealthFamily.WaitEffectivenessElasticity);
                Check(Mathf.Abs(atSeed - oldForm) <= Tolerance * oldForm,
                    F("{0,-7} at the seed spending the factor {1:F5} is the effectiveness term alone {2:F5} (spending per age-cost unit at its seed)", id, atSeed, oldForm));
                foreach (float x in new[] { ArithmeticShare, -ArithmeticShare })
                {
                    float moved = HealthFamily.TargetsFor(c, real * (1f + x)).WaitFactor;
                    float expected = atSeed * Mathf.Pow(1f + x, -HealthFamily.WaitSpendingElasticity);
                    bool direction = x > 0f ? moved < atSeed : moved > atSeed;
                    Check(direction && Mathf.Abs(moved - expected) <= Tolerance * expected,
                        F("{0,-7} real health spending {1:+0%;-0%}: the factor {2:F5} against the stated {3:F5} ({4:F5} x {5:F4}^-{6:F4})",
                          id, x, moved, expected, atSeed, 1f + x, HealthFamily.WaitSpendingElasticity));
                }
            }
        }

        // ============================================================ (b) the played path
        private sealed class Played3
        {
            public float Cataract, Knee, Ratio, FirstYearAllocation, SeedCataract, SeedKnee, WorstStep;
        }

        private static void Played(CountryId id)
        {
            _sb.Append(F("    b. {0}: the health lines {1:+0%;-0%} and {2:+0%;-0%} in the first year's decision, the level left to stand, against untouched - {3} years\n", id, Step, -Step, Years));
            Played3 idle = RunCountry(id, 0f);
            Played3 up = RunCountry(id, Step);
            Played3 down = RunCountry(id, -Step);
            if (idle == null || up == null || down == null) { Check(false, F("{0,-7} carries no seeded waits", id)); return; }
            Gaps(id, "raised", up, idle, shorter: true);
            Gaps(id, "cut", down, idle, shorter: false);
            float worst = Mathf.Max(idle.WorstStep, Mathf.Max(up.WorstStep, down.WorstStep));
            Check(worst <= Tolerance,
                F("{0,-7} every year's step on the three runs is wait + (seed x factor - wait) x {1:F2}: the largest relative gap {2:E2}", id, HealthFamily.ReversionPerYear, worst));
        }

        private static void Gaps(CountryId id, string label, Played3 run, Played3 idle, bool shorter)
        {
            float k = HealthFamily.ReversionPerYear;
            float q = idle.Ratio > 0f ? run.Ratio / idle.Ratio : 1f;
            float r1 = idle.FirstYearAllocation > 0f ? run.FirstYearAllocation / idle.FirstYearAllocation : 1f;
            Check(Mathf.Abs(q - 1f) >= MinLanded,
                F("{0,-7} {1}: real health spending per age-cost unit x{2:F4} against the untouched run - the move landed (at least {3:P0})", id, label, q, MinLanded));
            float spendShare = Mathf.Abs(1f - Mathf.Pow(Mathf.Max(0.01f, q), -HealthFamily.WaitSpendingElasticity)) * (1f - Mathf.Pow(1f - k, Years));
            // The effectiveness flow's part: the first year's allocation ratio enters one year's target, stepped once at k and decayed (1 - k) for the years after.
            float flowShare = Mathf.Abs(Mathf.Pow(Mathf.Max(0.01f, r1), -HealthFamily.WaitEffectivenessElasticity) - 1f) * k * Mathf.Pow(1f - k, Years - 1);
            Leg(id, label, "cataract", run.Cataract, idle.Cataract, run.SeedCataract, spendShare, flowShare, shorter);
            Leg(id, label, "knee", run.Knee, idle.Knee, run.SeedKnee, spendShare, flowShare, shorter);
        }

        private static void Leg(CountryId id, string label, string wait, float run, float idle, float seed, float spendShare, float flowShare, bool shorter)
        {
            float gap = shorter ? idle - run : run - idle;
            float expectedSpend = seed * spendShare, flowPart = seed * flowShare;
            Check(gap > 0f && gap - flowPart >= GapShare * expectedSpend,
                F("{0,-7} {1}: {2} {3:F2} d against untouched {4:F2} d - {5} by {6:F2} d; net of the effectiveness flow ({7:F2} d) {8:F2} d against the closed-form spending gap {9:F2} d (at least {10:P0})",
                  id, label, wait, run, idle, shorter ? "shorter" : "longer", gap, flowPart, gap - flowPart, expectedSpend, GapShare));
        }

        /// <summary>One country the player from its own start for <see cref="Years"/> turns; a nonzero <paramref name="share"/> moves every health line by it in the
        /// first year's decision only, so the level stands after. Returns the two waits, the spending-per-age-cost ratio at the end, the Health portfolio's allocation
        /// ratio after the first year, the seeds, and the largest relative gap between a year's step and the stated arithmetic.</summary>
        private static Played3 RunCountry(CountryId id, float share)
        {
            SimulationRandom.Seed(NoPolicyCentury.Seed);
            using IDisposable epoch = SimulationManager.EpochScope();
            PoliSim.Elections.WorldClock.ApplyStart(id);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("HOSPITALWAITS");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = id;
                Country c = world.GetCountry(id);
                if (c == null || c.Health == null || !c.Health.Seeded || !c.Health.HasWaits) { return null; }
                HealthSeeds s = c.Health;
                var result = new Played3 { SeedCataract = s.WaitCataract, SeedKnee = s.WaitKnee };
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= Years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (year == 1 && share != 0f)
                    {
                        foreach (SpendingLine line in c.SpendingLines) { if (HealthFamily.IsHealthLine(line.Category)) { d.SpendingLineChanges[line.Category] = share * 100f; } }
                    }
                    decisions[id] = d;
                    float cataractBefore = c.State.WaitCataractDays, kneeBefore = c.State.WaitKneeDays;
                    sim.AdvanceTurn(decisions);
                    if (year == 1) { result.FirstYearAllocation = Effectiveness.AllocationRatioOf(c, CabinetPortfolio.HealthSocialAffairs); }
                    // The step the family took at this boundary, recomputed from the state it read (the spending, the age-cost index and the recorded effectiveness are the boundary's).
                    float factor = HealthFamily.TargetsFor(c, HealthFamily.HealthSpendingReal(c)).WaitFactor;
                    result.WorstStep = Mathf.Max(result.WorstStep, StepGap(cataractBefore, s.WaitCataract * factor, c.State.WaitCataractDays));
                    result.WorstStep = Mathf.Max(result.WorstStep, StepGap(kneeBefore, s.WaitKnee * factor, c.State.WaitKneeDays));
                }
                result.Cataract = c.State.WaitCataractDays;
                result.Knee = c.State.WaitKneeDays;
                result.Ratio = s.SpendPerAgeCostSeed > 0f ? HealthFamily.SpendPerAgeCost(c) / s.SpendPerAgeCostSeed : 1f;
                return result;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static float StepGap(float before, float target, float after)
        {
            float t = Mathf.Clamp(target, HealthFamily.MinWaitDays, HealthFamily.MaxWaitDays);
            float expected = Mathf.Clamp(before + (t - before) * HealthFamily.ReversionPerYear, HealthFamily.MinWaitDays, HealthFamily.MaxWaitDays);
            return Mathf.Abs(after - expected) / Mathf.Max(1f, expected);
        }

        // ============================================================ (c1) the player's own idle book
        private static void Idle()
        {
            _sb.Append(F("    c1. the no-policy century (the player's own book) - first {0} years: each wait within {1:P0} of its sourced seed\n", IdleYears, IdleBand));
            foreach (CountryId id in Three)
            {
                IReadOnlyList<NoPolicyCentury.Year> century = NoPolicyCentury.For(id);
                float seedCataract = -1f, seedKnee = -1f;
                SimulationRandom.Seed(NoPolicyCentury.Seed);
                using (SimulationManager.EpochScope())
                {
                    Country fresh = WorldFactory.CreateDefault().GetCountry(id);
                    if (fresh != null && fresh.Health != null) { seedCataract = fresh.Health.WaitCataract; seedKnee = fresh.Health.WaitKnee; }
                }
                if (seedCataract <= 0f || seedKnee <= 0f || century.Count < IdleYears) { Check(false, F("{0,-7} no seeded waits or a short century", id)); continue; }
                float worst = 0f;
                for (int y = 0; y < IdleYears; y++)
                {
                    worst = Mathf.Max(worst, Mathf.Abs(century[y].WaitCataractDays / seedCataract - 1f));
                    worst = Mathf.Max(worst, Mathf.Abs(century[y].WaitKneeDays / seedKnee - 1f));
                }
                NoPolicyCentury.Year last = century[IdleYears - 1];
                Check(worst <= IdleBand,
                    F("{0,-7} year {1}: cataract {2:F2} d (seed {3:F1}), knee {4:F2} d (seed {5:F1}), spending per age-cost unit x{6:F4} - the widest departure {7:P1}",
                      id, IdleYears, last.WaitCataractDays, seedCataract, last.WaitKneeDays, seedKnee, last.SpendPerAgeCostRatio, worst));
            }
        }

        // ============================================================ (c2) the AI's book (the q28 review's M1)
        private static void AiBook()
        {
            _sb.Append(F("    c2. the AI's book - Germany the player, Sweden, Poland and Italy idle under their AI governments for {0} years: (i) the model's step every year, (ii) the drift decomposed, (iii) the wage path where the AI moved no line (to {1:P0})\n", IdleYears, WagePathTolerance));
            SimulationRandom.Seed(NoPolicyCentury.Seed);
            using IDisposable epoch = SimulationManager.EpochScope();
            PoliSim.Elections.WorldClock.ApplyStart(CountryId.Germany);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("HOSPITALWAITS_AIBOOK");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Germany;
                var wageSeed = new Dictionary<CountryId, float>();
                var worstStep = new Dictionary<CountryId, float>();
                var moves = new Dictionary<CountryId, double>();      // the product of the Health portfolio's yearly allocation ratios - the AI ministry's line moves
                var movedYears = new Dictionary<CountryId, StringBuilder>();
                foreach (CountryId id in Three)
                {
                    Country c = world.GetCountry(id);
                    if (c == null || c.Health == null || !c.Health.Seeded || !c.Health.HasWaits || c.State.RealWageIndex <= 0f) { Check(false, F("{0,-7} carries no seeded waits or no real wage index", id)); return; }
                    if (sim.PlayerGoverns(c)) { Check(false, F("{0,-7} reads as the player's book - the case does not test the AI's", id)); return; }
                    wageSeed[id] = c.State.RealWageIndex; worstStep[id] = 0f; moves[id] = 1.0; movedYears[id] = new StringBuilder();
                }
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var before = new Dictionary<CountryId, (float Cataract, float Knee)>();
                for (int year = 1; year <= IdleYears; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    foreach (CountryId id in Three) { Country c = world.GetCountry(id); before[id] = (c.State.WaitCataractDays, c.State.WaitKneeDays); }
                    sim.AdvanceTurn(decisions);
                    foreach (CountryId id in Three)
                    {
                        Country c = world.GetCountry(id);
                        HealthSeeds s = c.Health;
                        // (i) the step the family took, from the measured spending ratio and the recorded effectiveness (TargetsFor - the target the model computes).
                        float factor = HealthFamily.TargetsFor(c, HealthFamily.HealthSpendingReal(c)).WaitFactor;
                        worstStep[id] = Mathf.Max(worstStep[id], StepGap(before[id].Cataract, s.WaitCataract * factor, c.State.WaitCataractDays));
                        worstStep[id] = Mathf.Max(worstStep[id], StepGap(before[id].Knee, s.WaitKnee * factor, c.State.WaitKneeDays));
                        // (ii) the AI ministry's uniform line moves land in the year's allocation ratio (allocated / the indexed request).
                        float allocation = Effectiveness.AllocationRatioOf(c, CabinetPortfolio.HealthSocialAffairs);
                        moves[id] *= allocation;
                        if (Mathf.Abs(allocation - 1f) > MoveThreshold) { movedYears[id].Append(movedYears[id].Length > 0 ? ", " : "").Append(F("y{0} x{1:F4}", year, allocation)); }
                    }
                }
                foreach (CountryId id in Three)
                {
                    Country c = world.GetCountry(id);
                    HealthSeeds s = c.Health;
                    float wageRatio = c.State.RealWageIndex / wageSeed[id];
                    float spendRatio = s.SpendPerAgeCostSeed > 0f ? HealthFamily.SpendPerAgeCost(c) / s.SpendPerAgeCostSeed : 1f;
                    float effectivenessRatio = Mathf.Max(0.01f, HealthFamily.HealthEffectiveness(c) / s.EfficiencySeed);
                    float e = HealthFamily.WaitSpendingElasticity;
                    float wagePart = Mathf.Pow(Mathf.Max(0.01f, wageRatio), -e);
                    float movesPart = Mathf.Pow(Mathf.Max(0.01f, (float)moves[id]), -e);
                    float residualPart = Mathf.Pow(Mathf.Max(0.01f, spendRatio / Mathf.Max(0.01f, wageRatio * (float)moves[id])), -e);
                    float effectivenessPart = Mathf.Pow(1f / effectivenessRatio, HealthFamily.WaitEffectivenessElasticity);
                    string crisis = c.Crisis != null && c.Crisis.Phase != CrisisPhase.None ? F("{0} (programme year {1})", c.Crisis.Phase, c.Crisis.ProgrammeYear)
                        : c.Crisis != null && c.Crisis.Crises > 0 ? F("none now, {0} entered", c.Crisis.Crises) : "none";
                    Check(worstStep[id] <= Tolerance,
                        F("{0,-7} (i) every year the waits sit at the model's step from the measured spending ratio and effectiveness: the largest relative gap {1:E2}", id, worstStep[id]));
                    _sb.Append(F("      ii   {0,-7} year {1}: cataract {2:F2} d (seed {3:F1}), knee {4:F2} d (seed {5:F1}); spending per age-cost unit x{6:F4} = the real wage index x{7:F4} (the DECLARED M1 drift, the waits' target x{8:F4}) "
                        + "x the AI ministry's line moves x{9:F4} (target x{10:F4}; moved {11}) x the rest x{12:F4} (target x{13:F4}); effectiveness target x{14:F4}; debt crisis: {15}\n",
                        id, IdleYears, c.State.WaitCataractDays, s.WaitCataract, c.State.WaitKneeDays, s.WaitKnee, spendRatio, wageRatio, wagePart,
                        moves[id], movesPart, movedYears[id].Length > 0 ? movedYears[id].ToString() : "never", spendRatio / Mathf.Max(0.01f, wageRatio * (float)moves[id]), residualPart, effectivenessPart, crisis));
                    if (movedYears[id].Length == 0)
                    {
                        Check(Mathf.Abs(spendRatio / Mathf.Max(0.01f, wageRatio) - 1f) <= WagePathTolerance,
                            F("{0,-7} (iii) no AI line move in {1} years: spending per age-cost unit x{2:F4} follows the real wage index x{3:F4} (to {4:P0}) - the M1 drift as declared", id, IdleYears, spendRatio, wageRatio, WagePathTolerance));
                    }
                    else
                    {
                        _sb.Append(F("      --   {0,-7} (iii) not judged: the AI ministry moved the Health portfolio's lines, so its book does not follow the wage path alone (decomposed above)\n", id));
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
