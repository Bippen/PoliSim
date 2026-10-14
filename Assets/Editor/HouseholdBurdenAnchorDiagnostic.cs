using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// FT-3 (2026-09-07, §377) - what LANDED after the measured stop. (1) The household burden gap is zero for six at the seed, and (GA-11-1,
    /// 2026-10-14) so is the Gini's: its anchor is the income line's own flat-equivalent rate there. (2) Sweden over six turns:
    /// the income tax +5 points reads a POSITIVE gap and −5 a negative one (the rate reaches consumption, C-N4). (3) A household tax the country did not
    /// levy at the seed, implemented at 5 %, reads a POSITIVE gap - the C-N4 anchor defect closed here (before, its "seed" rate was today's and it cancelled
    /// itself). (4) Unemployment +5 points on turn 3 leaves the gap where the untouched run has it, to 1e-6: the base is read on both sides and cancels BY
    /// DESIGN - the stabiliser's tax side is NOT in this term; §377 measured the seed-share anchor and stopped it (the equivalence bar, the secular drift).
    /// (5) GA-2-11 (2026-10-14): the income tax's own sub-row rates reach the gap - Sweden's flat layer and state band at 0 open a large negative gap,
    /// Poland's top band five points up a positive one (both sides used to read the live line's rates, and the gap stayed zero). (6) A line with no
    /// rates of its own - no array, an empty one, every entry −1 - reads the gap to the bit in every country whose income tax responds.
    /// </summary>
    public static class HouseholdBurdenAnchorDiagnostic
    {
        private const int Turns = 6;
        private const int ShockTurn = 3;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            foreach (Country c in seedWorld.Countries)
            {
                float gap = MacroSystem.HouseholdBurdenGap(c);
                if (Mathf.Abs(gap) > 1e-5f) { Debug.LogError($"BURDEN ANCHOR: {c.Id} carries a nonzero burden gap at the seed ({gap:R})."); ok = false; }
                // GA-11-1 (2026-10-14, R-N5: the stale anchor's second cost - the burden's own baseline, caught on F4-5's first run, then the Gini's):
                // the Gini's redistribution term reads the income line's flat-equivalent rate against `Country.BaselineIncomeTaxRate`, so at the seed
                // the two are one figure, or the Gini's target opens displaced and inequality drifts with no policy
                foreach (TaxLine line in c.TaxLines)
                {
                    if (line.Type != TaxType.IncomeTax || !line.IsImplemented) { continue; }
                    float giniGap = TaxBases.EffectiveRate(c, line) - c.BaselineIncomeTaxRate;
                    if (Mathf.Abs(giniGap) > 1e-4f) { Debug.LogError($"BURDEN ANCHOR: {c.Id}'s Gini anchor is {c.BaselineIncomeTaxRate:R} against the income line's {TaxBases.EffectiveRate(c, line):R} at the seed - a gap of {giniGap:R} the Gini reads as policy."); ok = false; }
                }
            }
            // GA-2-11 (2026-10-14, R-N5: the burden's seed side following the live state has cost twice - FT-3's new tax cancelled itself in the gap,
            // then the income tax's own sub-rows did): (6) first, on every country whose income tax responds, then (5) on Sweden and Poland
            foreach (Country c in seedWorld.Countries)
            {
                TaxLine income = IncomeLine(c);
                if (income == null || !TaxSchedule.Responds(c.Id)) { continue; }
                float bare = GapWith(c, income, null);
                float empty = GapWith(c, income, new float[0]);
                float statute = GapWith(c, income, SubRowRates(c.Id, row => -1f));
                if (empty != bare || statute != bare) { Debug.LogError($"BURDEN ANCHOR: {c.Id}'s gap with no rates of its own reads {bare:R}, with an empty array {empty:R}, with every entry -1 {statute:R} - one statute read three ways must give one figure."); ok = false; }
            }
            Country se = seedWorld.GetCountry(CountryId.Sweden), pl = seedWorld.GetCountry(CountryId.Poland);
            double plTop = 0.0;
            foreach (TaxSchedule.SubRow row in TaxSchedule.SubRows(TaxSchedule.Of(CountryId.Poland))) { if (row.Adjustable) { plTop = System.Math.Max(plTop, row.StatuteRate); } }
            float seZeroed = GapWith(se, IncomeLine(se), SubRowRates(CountryId.Sweden, row => row.Adjustable ? 0f : -1f));
            float plRaised = GapWith(pl, IncomeLine(pl), SubRowRates(CountryId.Poland, row => row.Adjustable && row.StatuteRate == plTop ? (float)plTop + 5f : -1f));
            if (!(seZeroed < -0.05f)) { Debug.LogError($"BURDEN ANCHOR: Sweden's flat layer and state band at 0 leave the gap at {seZeroed:R} - the sub-rows do not reach consumption."); ok = false; }
            if (!(plRaised > 1e-4f)) { Debug.LogError($"BURDEN ANCHOR: Poland's top band at {plTop + 5.0:R} leaves the gap at {plRaised:R} - the sub-rows do not reach consumption."); ok = false; }

            float untouched = Run(0f, 0f, false);
            float rateUp = Run(5f, 0f, false);
            float rateDown = Run(-5f, 0f, false);
            float jobsShock = Run(0f, 5f, false);
            float newTax = Run(0f, 0f, true);
            if (!(rateUp > untouched + 1e-4f) || !(rateDown < untouched - 1e-4f)) { Debug.LogError($"BURDEN ANCHOR: the income tax +/-5 points does not move the gap both ways ({rateUp:F5} / {untouched:F5} / {rateDown:F5})."); ok = false; }
            if (Mathf.Abs(jobsShock - untouched) > 1e-6f) { Debug.LogError($"BURDEN ANCHOR: unemployment +5 points moved the gap ({jobsShock:F6} vs {untouched:F6}) - the base no longer cancels; that is FT-3's stopped form, not this build."); ok = false; }
            if (float.IsNaN(newTax)) { Debug.Log("BURDEN ANCHOR: Sweden levies every household tax at the seed - the new-tax anchor is not testable on it (stated, not failed)."); }
            else if (!(newTax > untouched + 1e-4f)) { Debug.LogError($"BURDEN ANCHOR: a household tax absent at the seed, implemented at 5 %, does not raise the gap ({newTax:F5} vs {untouched:F5})."); ok = false; }
            Debug.Log($"BURDEN ANCHOR: Sweden at turn {Turns} - untouched gap {untouched:F6}; income tax +5: {rateUp:F5}; -5: {rateDown:F5}; unemployment +5 on turn {ShockTurn}: {jobsShock:F6} (the base cancels by design); "
                + (float.IsNaN(newTax) ? "new-tax anchor untestable." : $"a household tax absent at the seed at 5 %: {newTax:F5}.")
                + $" At the seed, the sub-rows: Sweden's flat layer and state band at 0 {seZeroed:F5}; Poland's top band at {plTop + 5.0:R} {plRaised:F5}.");
            Debug.Log(ok ? "BURDEN ANCHOR: PASS - zero at the seed, the rate both ways, the base cancels, a new tax anchors at zero, the sub-rows reach the gap, and no rates of its own read it to the bit." : "BURDEN ANCHOR: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float Run(float ratePoints, float shockPoints, bool implementNewTax)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("BURDEN");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country c = world.GetCountry(CountryId.Sweden);
                float seedRate = 0f; foreach (TaxLine line in c.TaxLines) { if (line.Type == TaxType.IncomeTax) { seedRate = line.Rate; } }
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                bool newTaxFound = false;
                for (int year = 1; year <= Turns; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    if (shockPoints != 0f && year == ShockTurn) { c.State.Unemployment = Mathf.Clamp(c.State.Unemployment + shockPoints, 0f, 60f); }
                    if (implementNewTax && year == ShockTurn)
                    {
                        foreach (TaxLine line in c.TaxLines)
                        {
                            if (line.IsImplemented || line.Type == TaxType.CorporateTax || line.Type == TaxType.Tariffs) { continue; }
                            line.IsImplemented = true; line.Rate = 5f; newTaxFound = true; break;
                        }
                    }
                    PolicyDecision d = PolicyDecision.None();
                    if (ratePoints != 0f) { d.TaxRateOverrides[TaxType.IncomeTax] = seedRate + ratePoints; }
                    decisions[CountryId.Sweden] = d;
                    sim.AdvanceTurn(decisions);
                }
                if (implementNewTax && !newTaxFound) { return float.NaN; }
                return MacroSystem.HouseholdBurdenGap(c);
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static TaxLine IncomeLine(Country c)
        {
            foreach (TaxLine line in c.TaxLines) { if (line.Type == TaxType.IncomeTax && line.IsImplemented) { return line; } }
            return null;
        }

        /// <summary>A rate for every sub-row of the country's statute, <paramref name="pick"/> choosing each (−1: the statute's own figure stands).</summary>
        private static float[] SubRowRates(CountryId id, System.Func<TaxSchedule.SubRow, float> pick)
        {
            List<TaxSchedule.SubRow> rows = TaxSchedule.SubRows(TaxSchedule.Of(id));
            var rates = new float[rows.Count];
            for (int i = 0; i < rows.Count; i++) { rates[i] = pick(rows[i]); }
            return rates;
        }

        /// <summary>The burden's gap with the income line carrying <paramref name="rates"/> (null: none of its own) - both yield memos dropped first, so
        /// the yields are computed and not recalled, and the line and the memos put back after.</summary>
        private static float GapWith(Country c, TaxLine income, float[] rates)
        {
            float[] standing = income.BracketRates;
            income.BracketRates = rates;
            c.ScheduleMemo = null;
            c.ScheduleMemoStatute = null;
            try { return MacroSystem.HouseholdBurdenGap(c); }
            finally { income.BracketRates = standing; c.ScheduleMemo = null; c.ScheduleMemoStatute = null; }
        }
    }
}
