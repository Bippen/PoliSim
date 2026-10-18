using System.Collections.Generic;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-C7 (2026-09-05): the effectiveness mechanic's proof. (1) At no policy every recorded portfolio's effectiveness equals the minister's
    /// efficiency (allocated = requested: the ratio is the multiplier alone) and a line no ministry reads is in no portfolio. (2) Sweden with
    /// its health lines cut a fifth in the first year's decision: the Health portfolio's allocation ratio is 0.80 to the tolerance that year and
    /// its effectiveness 0.80 × efficiency; after twenty years the waits are longer and quality worse than the untouched run - the family
    /// reads the mechanic, not the stand-in (the row's "underfunding a ministry measurably degrades its outcomes through the minister").
    /// (3) A minister of lower efficiency, same money: effectiveness lower, quality worse - the multiplier is felt.
    /// (4) AF-30 (q30, ruled 2026-10-18): the scale - an average minister of the pool is neutral (x1, the health targets exactly the unmodelled
    /// cabinet's), a vacant chair the player's to fill sits below the pool's floor and its targets are worse, a chair the player does not hold reads x1.
    /// </summary>
    public static class EffectivenessDiagnostic
    {
        private const int Years = 20;
        private const float Tolerance = 0.002f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;

            // (1) the identity at no policy
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            foreach (SpendingCategory c in System.Enum.GetValues(typeof(SpendingCategory)))
            {
                CabinetPortfolio? p = Effectiveness.PortfolioOf(c);
                if (c == SpendingCategory.SocialSecurity && p != null) { Debug.LogError("EFFECTIVENESS: pensions have a ministry; 9d says no ministry reads them."); ok = false; }
                if (c == SpendingCategory.HealthcareAndSocialCare && p != CabinetPortfolio.HealthSocialAffairs) { Debug.LogError("EFFECTIVENESS: the health line is not the Health ministry's."); ok = false; }
            }
            float[] untouched = RunSweden(Years, cutShare: 0f, efficiencyOverride: -1f, out float ratioYear1, out float allocYear1, out float efficiencyYear1);
            if (Mathf.Abs(allocYear1 - 1f) > Tolerance) { Debug.LogError($"EFFECTIVENESS: at no policy the Health allocation ratio in year 1 is {allocYear1:F4}, not 1."); ok = false; }
            if (Mathf.Abs(ratioYear1 - efficiencyYear1) > Tolerance) { Debug.LogError($"EFFECTIVENESS: at no policy effectiveness {ratioYear1:F4} is not the efficiency {efficiencyYear1:F4}."); ok = false; }

            // (2) the health lines cut a fifth through the DECISION (the mechanic's own path: request indexed, allocation the player's)
            float[] cut = RunSweden(Years, cutShare: -0.2f, efficiencyOverride: -1f, out float ratioCut, out float allocCut, out float efficiencyCut);
            // The decision asks for -20 %; the seed-range clamp (ClampToSeedRange) may allow less - the assertion is the identity and the direction, not the asked figure.
            if (!(allocCut < 0.99f) || allocCut <= 0f) { Debug.LogError($"EFFECTIVENESS: a cut through the decision gives an allocation ratio of {allocCut:F4}, not below 1."); ok = false; }
            if (Mathf.Abs(ratioCut - allocCut * efficiencyCut) > Tolerance) { Debug.LogError($"EFFECTIVENESS: effectiveness {ratioCut:F4} is not allocation {allocCut:F4} x efficiency {efficiencyCut:F4}."); ok = false; }
            if (!(cut[1] > untouched[1]) || !(cut[2] > untouched[2]) || !(cut[0] > untouched[0])) { Debug.LogError($"EFFECTIVENESS: with the Health ministry at {ratioCut:F2} the waits ({cut[1]:F1} / {cut[2]:F1} d) and quality ({cut[0]:F1}) are not worse than untouched ({untouched[1]:F1} / {untouched[2]:F1} d, {untouched[0]:F1})."); ok = false; }

            // (2b) the ruled distinction (2026-09-06): the cut stands, so in year 2 the FLOW is met again (the request re-based on the indexed line) while the LEVEL stays low.
            if (!(LevelYear2 < 0.99f)) { Debug.LogError($"EFFECTIVENESS: after a standing cut the Health LEVEL in year 2 is {LevelYear2:F4}, not below 1 - the level should carry the shrunken ask."); ok = false; }
            if (!(FlowYear2 > LevelYear2)) { Debug.LogError($"EFFECTIVENESS: in year 2 the flow {FlowYear2:F4} is not above the level {LevelYear2:F4} - the flow should read the year's ask as met while the level stays low."); ok = false; }
            Debug.Log($"EFFECTIVENESS: the distinction - year 2 of the standing cut: flow (allocated / requested) {FlowYear2:F3}, level (spending per unit against its seed) {LevelYear2:F3}; the ask was met, the ask has shrunk.");

            // (3) a less efficient minister, the same money
            float[] slack = RunSweden(Years, cutShare: 0f, efficiencyOverride: 60f, out float ratioSlack, out _, out float efficiencySlack);
            if (Mathf.Abs(ratioSlack - efficiencySlack) > Tolerance || Mathf.Abs(efficiencySlack - 60f / CabinetSystem.PoolMeanEfficiency) > Tolerance) { Debug.LogError($"EFFECTIVENESS: with efficiency set to 60 the ratio is {ratioSlack:F4} (efficiency read {efficiencySlack:F4}, AF-30's scale gives {60f / CabinetSystem.PoolMeanEfficiency:F4})."); ok = false; }
            if (!(slack[0] > untouched[0])) { Debug.LogError($"EFFECTIVENESS: a minister at efficiency 60 leaves quality at {slack[0]:F1}, not worse than {untouched[0]:F1}."); ok = false; }

            Debug.Log($"EFFECTIVENESS: Sweden, Health - year 1 at no policy: allocation {allocYear1:F3}, efficiency {efficiencyYear1:F2}, effectiveness x{ratioYear1:F2}; the health lines cut through the decision (-20 % asked, the seed range deciding): allocation {allocCut:F3}, effectiveness x{ratioCut:F2}; "
                + $"after {Years} years - untouched: treatable mortality {untouched[0]:F1}, cataract {untouched[1]:F1} d, knee {untouched[2]:F1} d; cut: {cut[0]:F1}, {cut[1]:F1} d, {cut[2]:F1} d; efficiency 60 with the same money: x{ratioSlack:F2}, quality {slack[0]:F1}. Underfunding degrades the outcomes through the minister.");
            ok &= Af30Scale();

            Debug.Log(ok ? "EFFECTIVENESS: PASS - the ratio is allocated / requested x efficiency, lines without a ministry print none, the family reads it." : "EFFECTIVENESS: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>(4) AF-30: the scale read on the seed's world, through the targets the health family drifts toward (no run: an election inside a run would
        /// re-form the government and change whose chairs they are).</summary>
        private static bool Af30Scale()
        {
            bool ok = true;
            void Fail(string what) { Debug.LogError("EFFECTIVENESS: AF-30: " + what); ok = false; }
            float mean = CabinetSystem.PoolMeanEfficiency, floor = CabinetSystem.PoolFloorEfficiency, vacant = CabinetSystem.VacantChairEfficiency;
            if (!(vacant < floor)) { Fail($"the vacant chair's {vacant:F1} is not below the pool's floor {floor:F1}."); }
            if (!(floor < mean)) { Fail($"the pool's floor {floor:F1} is not below its mean {mean:F2}."); }

            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            Country se = world.GetCountry(CountryId.Sweden);
            const CabinetPortfolio health = CabinetPortfolio.HealthSocialAffairs;
            se.CabinetMinisters.Remove(health);
            float spending = HealthFamily.HealthSpendingReal(se);
            HealthFamily.Targets Read(out float efficiency) { efficiency = Effectiveness.EfficiencyOf(se, health); return HealthFamily.TargetsFor(se, spending); }

            // the second review's N1 (DECLARED: the factor is symmetric): a cut and an equal restore cancel on the three stocks EFFICIENCY's spending term moves -
            // no round-trip pump. Float addition is not associative, so the bound is a rounding's, far below what a pump leaves (sensitivity x change x (f - 1/f)).
            void RoundTrip(string chairs)
            {
                se.State.EnergyIndustryBillShareChange = 0f;   // the energy line's term rides the same call and does not cancel; held at zero here
                float before0 = se.State.ConsumerConfidence, before1 = se.State.BusinessConfidence, before2 = se.BaselineCrimeIndex;
                float change = se.State.NominalGdp * 0.005f;
                var cut = PolicyDecision.None(); cut.HealthcareSpendingChange = -change; cut.EducationSpendingChange = -change; cut.JusticeSpendingChange = -change;
                var restore = PolicyDecision.None(); restore.HealthcareSpendingChange = change; restore.EducationSpendingChange = change; restore.JusticeSpendingChange = change;
                MacroSystem.ApplyCategorySpendingEffects(se, cut);
                float moved = Mathf.Abs(se.State.ConsumerConfidence - before0) + Mathf.Abs(se.State.BusinessConfidence - before1) + Mathf.Abs(se.BaselineCrimeIndex - before2);
                MacroSystem.ApplyCategorySpendingEffects(se, restore);
                float d0 = se.State.ConsumerConfidence - before0, d1 = se.State.BusinessConfidence - before1, d2 = se.BaselineCrimeIndex - before2;
                if (!(moved > 0f) || Mathf.Abs(d0) > 1e-4f || Mathf.Abs(d1) > 1e-4f || Mathf.Abs(d2) > 1e-4f) { Fail($"with {chairs}, a cut and an equal restore leave consumer confidence {d0:+0.000000;-0.000000}, business confidence {d1:+0.000000;-0.000000}, the crime index {d2:+0.000000;-0.000000} (the cut moved them {moved:F4}) - a round-trip pump, or nothing moved."); }
                else { Debug.Log($"EFFECTIVENESS: AF-30 - with {chairs}, a cut of half a point of GDP on three lines moved the stocks {moved:F4} and the restore returned them (residuals {d0:E1}, {d1:E1}, {d2:E1}) - no round-trip pump."); }
                se.State.ConsumerConfidence = before0; se.State.BusinessConfidence = before1; se.BaselineCrimeIndex = before2;
            }

            // the unmodelled cabinet (no party chosen): x1
            se.PlayerPartyAbbrev = null;
            HealthFamily.Targets neutral = Read(out float effNone);
            if (Mathf.Abs(effNone - 1f) > 1e-6f || Mathf.Abs(CabinetSystem.EfficiencyFactor(se, health) - 1f) > 1e-6f) { Fail($"an unmodelled cabinet reads {effNone:F4}, not 1."); }

            // the player's party leads: the chair is the player's, and vacant
            se.Government = new PoliSim.Elections.GovernmentRecord { PmParty = "AF30" };
            se.PlayerPartyAbbrev = "AF30";
            HealthFamily.Targets empty = Read(out float effVacant);
            if (!(effVacant < floor / mean - 1e-6f)) { Fail($"a vacant chair the player's to fill reads {effVacant:F4}, not below the floor's {floor / mean:F4}."); }
            if (!(empty.WaitFactor > neutral.WaitFactor) || !(empty.TreatableMortality > neutral.TreatableMortality)) { Fail($"a vacant chair's targets (waits x{empty.WaitFactor:F3}, mortality {empty.TreatableMortality:F2}) are not worse than an average minister's (x{neutral.WaitFactor:F3}, {neutral.TreatableMortality:F2})."); }
            if (!(CabinetSystem.EfficiencyFactor(se, health) < floor / mean)) { Fail("the spending sensitivity's factor does not read the vacant chair below the floor."); }
            RoundTrip("a vacant chair");

            // an average minister of the pool, seated: x1, the targets exactly the unmodelled cabinet's - neither helps nor harms
            se.CabinetMinisters[health] = new CabinetMinister { Name = "Probe", Portfolio = health, Efficiency = mean };
            HealthFamily.Targets average = Read(out float effAverage);
            se.CabinetMinisters[CabinetPortfolio.Education] = new CabinetMinister { Name = "Probe", Portfolio = CabinetPortfolio.Education, Efficiency = 90f };
            se.CabinetMinisters[CabinetPortfolio.InteriorJustice] = new CabinetMinister { Name = "Probe", Portfolio = CabinetPortfolio.InteriorJustice, Efficiency = 55f };
            RoundTrip("ministers seated (Health average, Education 90, Interior 55)");
            se.CabinetMinisters.Remove(CabinetPortfolio.Education);
            se.CabinetMinisters.Remove(CabinetPortfolio.InteriorJustice);
            if (Mathf.Abs(effAverage - 1f) > 1e-5f || Mathf.Abs(average.WaitFactor - neutral.WaitFactor) > 1e-5f || Mathf.Abs(average.TreatableMortality - neutral.TreatableMortality) > 1e-3f) { Fail($"an average minister ({mean:F2}) reads {effAverage:F4}, waits x{average.WaitFactor:F4} against x{neutral.WaitFactor:F4}, mortality {average.TreatableMortality:F3} against {neutral.TreatableMortality:F3} - not neutral."); }

            // the pool's floor and best bracket the neutral point, and every candidate beats the vacant chair
            se.CabinetMinisters[health].Efficiency = floor;
            HealthFamily.Targets weakest = Read(out float effFloor);
            if (!(effFloor < 1f) || !(effFloor > effVacant) || !(weakest.WaitFactor < empty.WaitFactor)) { Fail($"the pool's floor reads {effFloor:F4} (vacant {effVacant:F4}) - it must sit between the vacant chair and 1, its waits shorter than the empty chair's."); }

            // the preview's clone reads the same chairs (the review's H1): a governing player with a minister at the floor seated - a clone without the chairs
            // would read the vacant chair's figure instead
            MethodInfo clonePreview = typeof(SimulationManager).GetMethod("ClonePreviewCountry", BindingFlags.NonPublic | BindingFlags.Static);
            if (clonePreview == null) { Fail("SimulationManager.ClonePreviewCountry not found - the preview's reading verified nothing."); }
            else
            {
                var previewed = (Country)clonePreview.Invoke(null, new object[] { se });
                float effPreview = Effectiveness.EfficiencyOf(previewed, health);
                if (Mathf.Abs(effPreview - effFloor) > 1e-6f) { Fail($"the preview's clone reads the seated minister at {effPreview:F4} against the live {effFloor:F4} (vacant {effVacant:F4})."); }
            }

            // the player in opposition: the chair is not the player's - a minister left seated is ignored, not counted (the review's L), and an empty chair is not modelled: x1
            se.Government = new PoliSim.Elections.GovernmentRecord { PmParty = "OTHER" };
            Read(out float effOppositionSeated);
            se.CabinetMinisters.Remove(health);
            Read(out float effOpposition);
            if (Mathf.Abs(effOpposition - 1f) > 1e-6f || Mathf.Abs(effOppositionSeated - 1f) > 1e-6f) { Fail($"a chair the player does not hold reads {effOpposition:F4} empty and {effOppositionSeated:F4} with a minister left in it, not 1."); }

            // a junior partner holding Finance: the gate locks the chair (it acts through the fiscal stance), so it is not the player's - x1; one holding Health: the player's, vacant
            var coalition = new PoliSim.Elections.GovernmentRecord { PmParty = "OTHER" };
            coalition.Cabinet.Add("AF30");
            coalition.Portfolios["AF30"] = new List<CabinetPortfolio> { CabinetPortfolio.FinanceTreasury, health };
            se.Government = coalition;
            float effPartnerFinance = Effectiveness.EfficiencyOf(se, CabinetPortfolio.FinanceTreasury), effPartnerHealth = Effectiveness.EfficiencyOf(se, health);
            if (Mathf.Abs(effPartnerFinance - 1f) > 1e-6f || !(effPartnerHealth < floor / mean)) { Fail($"a junior partner holding Finance and Health reads Finance {effPartnerFinance:F4} (the gate locks it: 1) and Health {effPartnerHealth:F4} (its own vacant chair: below the floor)."); }

            Debug.Log($"EFFECTIVENESS: AF-30 - the pool's mean efficiency {mean:F2} is x1, its floor {floor:F0} x{floor / mean:F3}, the vacant chair {vacant:F0} x{effVacant:F3}; "
                + $"Sweden's Health targets at the seed's spending: waits x{neutral.WaitFactor:F3} (average minister, or no cabinet modelled) against x{empty.WaitFactor:F3} (vacant) and x{weakest.WaitFactor:F3} (the floor); "
                + $"treatable mortality {neutral.TreatableMortality:F2} against {empty.TreatableMortality:F2} and {weakest.TreatableMortality:F2}.");
            return ok;
        }

        /// <summary>Sweden for <paramref name="years"/>; a nonzero <paramref name="cutShare"/> asks the decision for that percent on every health line each year (the
        /// player's figure through the mechanic's own path); a positive <paramref name="efficiencyOverride"/> sets the Health minister's efficiency before the run.
        /// Returns treatable mortality and the two waits at the end; the out values are year 1's Health record.</summary>
        private static float FlowYear2, LevelYear2;   // the cut run's second year: the flow met, the level low (the ruled distinction)

        private static float[] RunSweden(int years, float cutShare, float efficiencyOverride, out float ratioYear1, out float allocYear1, out float efficiencyYear1)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("EFFECTIVENESS");
            ratioYear1 = allocYear1 = efficiencyYear1 = float.NaN;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country se = world.GetCountry(CountryId.Sweden);
                if (efficiencyOverride > 0f)
                {
                    // Sweden seats no Health minister at the seed (no party is chosen here, so the empty chair is unmodelled and multiplies by 1 - AF-30); the probe appoints one with the efficiency asked.
                    if (!se.CabinetMinisters.TryGetValue(CabinetPortfolio.HealthSocialAffairs, out CabinetMinister m) || m == null) { m = new CabinetMinister { Name = "Probe", Portfolio = CabinetPortfolio.HealthSocialAffairs }; se.CabinetMinisters[CabinetPortfolio.HealthSocialAffairs] = m; }
                    m.Efficiency = efficiencyOverride;
                }
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (cutShare != 0f)
                    {
                        foreach (SpendingLine line in se.SpendingLines) { if (HealthFamily.IsHealthLine(line.Category)) { d.SpendingLineChanges[line.Category] = cutShare * 100f; } }
                    }
                    decisions[CountryId.Sweden] = d;
                    sim.AdvanceTurn(decisions);
                    if (year == 1 && se.Effectiveness.TryGetValue(CabinetPortfolio.HealthSocialAffairs, out PortfolioEffectiveness e))
                    {
                        ratioYear1 = e.Ratio; allocYear1 = e.AllocationRatio; efficiencyYear1 = e.Efficiency;
                    }
                    if (year == 2 && se.Effectiveness.TryGetValue(CabinetPortfolio.HealthSocialAffairs, out PortfolioEffectiveness e2)) { FlowYear2 = e2.AllocationRatio; LevelYear2 = e2.LevelRatio; }
                }
                return new[] { se.State.TreatableMortality, se.State.WaitCataractDays, se.State.WaitKneeDays };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
