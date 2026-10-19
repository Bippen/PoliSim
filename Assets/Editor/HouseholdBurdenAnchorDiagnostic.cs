using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Persistence;
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
    /// (7) A-LOOP q34 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §1): the seed's levied set (`Country.SeedLevied`) is the lines levied at the seed, in every
    /// country. (8) On every household line of every country, at the seed, through the tax's own bill (`ParliamentSystem.ApplyTaxProgramBillResult`):
    /// a levy opens the gap by that line's revenue share and a removal of a seed tax closes it by the same (relief, a negative gap); the bill returns the
    /// move q37 will read - from 0 to the line's rate, or from its rate to 0 (`ParliamentSystem.TaxTermPoints`); the opposite bill returns the opposite
    /// move and puts the gap back; the excise line (seeded inactive at its placeholder: D-32's case) opens a positive gap everywhere and a seeded VAT's
    /// removal a negative one; a failed bill moves nothing. (9) A save from before q34 (no seed set) loads with the lines levied at the load as its set
    /// (`SimulationManager.RestoreSaveState`), so a line levied before it was cut reads no gap - the older build's reading.
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
            // A-LOOP q34: (7) and (8) on the seed world (each line put back after), then (9) on a world of its own
            ok &= LevyAndRemoval(seedWorld);
            ok &= OlderSaveReading();

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
            Debug.Log(ok ? "BURDEN ANCHOR: PASS - zero at the seed, the rate both ways, the base cancels, a new tax anchors at zero, the sub-rows reach the gap, no rates of its own read it to the bit, and (q34) a levy is a rise from zero, a removal is relief, the bill returns the move, and an older save loads to its own burden." : "BURDEN ANCHOR: FAILED (see above).");
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

        /// <summary>(7) and (8): the seed set is the seed's levied lines, and every household line's levy or removal moves the gap by that line's revenue share and
        /// returns its move; each line is put back by the opposite bill before the next.</summary>
        private static bool LevyAndRemoval(World world)
        {
            bool ok = true;
            int levies = 0, removals = 0;
            foreach (Country c in world.Countries)
            {
                var levied = new List<TaxType>();
                var outside = new List<string>();
                foreach (TaxLine line in c.TaxLines) { if (line.IsImplemented) { levied.Add(line.Type); } else { outside.Add(line.Type.ToString()); } }
                if (c.SeedLevied == null || c.SeedLevied.Count != levied.Count || !levied.TrueForAll(c.SeedLevies))
                {
                    Debug.LogError($"BURDEN ANCHOR: q34 {c.Id}'s seed set ({(c.SeedLevied == null ? "none" : string.Join(", ", c.SeedLevied))}) is not the lines levied at the seed ({string.Join(", ", levied)})."); ok = false;
                }
                Debug.Log($"BURDEN ANCHOR: q34 {c.Id} - the seed levied {levied.Count} lines; not levied at the seed: {string.Join(", ", outside)} (the game's seed, not the statute: §1 (6)).");

                foreach (TaxLine line in c.TaxLines)
                {
                    if (line.Type == TaxType.CorporateTax || line.Type == TaxType.Tariffs) { continue; }
                    bool levy = !line.IsImplemented;
                    float before = MacroSystem.HouseholdBurdenGap(c);
                    float points = ParliamentSystem.TaxTermPoints(c, line);
                    float share = TaxBases.RevenueAtRate(c, line.Type, line.Rate) / Mathf.Max(1f, c.State.NominalGdp);   // the "now" side's own term for the line
                    float expected = levy ? share : -share;
                    TaxLevyMove move = ParliamentSystem.ApplyTaxProgramBillResult(c, new TaxProgramBill { Type = line.Type, IsAdd = levy }, true);
                    float after = MacroSystem.HouseholdBurdenGap(c);
                    TaxLevyMove undo = ParliamentSystem.ApplyTaxProgramBillResult(c, new TaxProgramBill { Type = line.Type, IsAdd = !levy }, true);
                    float restored = MacroSystem.HouseholdBurdenGap(c);
                    string what = $"{c.Id} {(levy ? "levies" : "removes")} {line.Type}";
                    if (levy) { levies++; } else { removals++; }

                    if (Mathf.Abs((after - before) - expected) > 1e-5f + 1e-3f * Mathf.Abs(expected))
                    {
                        Debug.LogError($"BURDEN ANCHOR: q34 {what} - the gap moved {after - before:R}, the line's revenue share is {expected:R}."); ok = false;
                    }
                    if (Mathf.Abs(expected) > 1e-6f && (levy ? !(after > before) : !(after < before)))
                    {
                        Debug.LogError($"BURDEN ANCHOR: q34 {what} - the gap went {before:R} -> {after:R}, the wrong way."); ok = false;
                    }
                    if (line.Type == TaxType.ExciseTax && levy && !(expected > 1e-6f)) { Debug.LogError($"BURDEN ANCHOR: q34 {what} at its placeholder {line.Rate:R} - its revenue share is {expected:R}, so D-32's case is not tested."); ok = false; }
                    if (line.Type == TaxType.VAT && !levy && !(after < -1e-6f)) { Debug.LogError($"BURDEN ANCHOR: q34 {what} - the gap reads {after:R}, no relief."); ok = false; }
                    bool moveRight = move.Moved && move.Type == line.Type
                        && (levy ? move.FromPoints == 0f && move.ToPoints == points : move.FromPoints == points && move.ToPoints == 0f);
                    if (!moveRight) { Debug.LogError($"BURDEN ANCHOR: q34 {what} - the bill returned {(move.Moved ? $"{move.FromPoints:R} -> {move.ToPoints:R}" : "no move")}, the line's rate is {points:R} points."); ok = false; }
                    if (!undo.Moved || undo.FromPoints != move.ToPoints || undo.ToPoints != move.FromPoints)
                    {
                        Debug.LogError($"BURDEN ANCHOR: q34 {what} then the opposite bill - it returned {(undo.Moved ? $"{undo.FromPoints:R} -> {undo.ToPoints:R}" : "no move")}, not the move undone."); ok = false;
                    }
                    if (line.IsImplemented == levy || Mathf.Abs(restored - before) > 1e-7f)
                    {
                        Debug.LogError($"BURDEN ANCHOR: q34 {what} then the opposite bill - the line is {(line.IsImplemented ? "levied" : "not levied")} and the gap {restored:R} against {before:R} before."); ok = false;
                    }
                }
            }

            Country se = world.GetCountry(CountryId.Sweden);
            TaxLine probe = null;
            foreach (TaxLine line in se.TaxLines) { if (!line.IsImplemented) { probe = line; break; } }
            if (probe != null)
            {
                float approval = se.State.ApprovalRating;
                TaxLevyMove failed = ParliamentSystem.ApplyTaxProgramBillResult(se, new TaxProgramBill { Type = probe.Type, IsAdd = true }, false, false);
                if (failed.Moved || probe.IsImplemented || se.State.ApprovalRating != approval) { Debug.LogError($"BURDEN ANCHOR: q34 a failed bill to levy Sweden's {probe.Type} moved something (move {failed.Moved}, levied {probe.IsImplemented})."); ok = false; }
            }
            Debug.Log($"BURDEN ANCHOR: q34 - {levies} levies and {removals} removals on the seed, each through its bill and back.");
            return ok;
        }

        /// <summary>(9): a save from before q34 carries no seed set; `SimulationManager.RestoreSaveState` reads it as the lines levied at the load. Sweden's excise,
        /// levied before the "save", then reads no gap - the older build's reading - where the seed's own set would read its whole revenue share.
        /// A-LOOP q37 (review 877's L1): through the real load path - a save cut, its `SeedLevied` keys stripped from the JSON TEXT (what an older
        /// build wrote: no key), then deserialized and restored into a manager - so an initializer added to the field one day, which would turn the
        /// missing key into "the seed levied nothing", fails here rather than passing on a hand-nulled field.</summary>
        private static bool OlderSaveReading()
        {
            using System.IDisposable epoch = SimulationManager.EpochScope();   // a save is loaded: the process epoch is restored after (§618)
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("BURDEN.restore");
            var go2 = new GameObject("BURDEN.restore.B");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country c = world.GetCountry(CountryId.Sweden);
                TaxLine excise = null;
                foreach (TaxLine line in c.TaxLines) { if (line.Type == TaxType.ExciseTax) { excise = line; } }
                if (excise == null || excise.IsImplemented) { Debug.LogError("BURDEN ANCHOR: q34 Sweden carries no unlevied excise line - the older-save reading is not tested."); return false; }
                excise.IsImplemented = true;
                float seedSetGap = MacroSystem.HouseholdBurdenGap(c);
                string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null));
                var key = new System.Text.RegularExpressions.Regex("\"SeedLevied\":\\s*(?:null|\\[[^\\]]*\\])\\s*,");
                int stripped = key.Matches(json).Count;
                SaveGame loaded = SaveGameService.Deserialize(key.Replace(json, string.Empty));
                SimulationManager simB = go2.AddComponent<SimulationManager>();
                SaveGameService.RestoreInto(simB, loaded);
                Country back = simB.World.GetCountry(CountryId.Sweden);
                float loadedGap = MacroSystem.HouseholdBurdenGap(back);
                bool ok = stripped >= world.Countries.Count && !json.Equals(key.Replace(json, string.Empty)) && back.SeedLevied != null && back.SeedLevies(TaxType.ExciseTax)
                    && Mathf.Abs(loadedGap) <= 1e-5f && seedSetGap > 1e-6f;
                Debug.Log($"BURDEN ANCHOR: q34 an older save (its {stripped} SeedLevied keys stripped from the JSON, loaded and restored) - Sweden's excise levied before it was cut: the seed's set reads a gap of {seedSetGap:F6}, the set read at the load {loadedGap:F6} (the older build's reading).");
                if (!ok) { Debug.LogError($"BURDEN ANCHOR: q34 the older-save reading failed - {stripped} keys stripped of {world.Countries.Count}; the set read at the load is {(back.SeedLevied == null ? "none" : string.Join(", ", back.SeedLevied))} and the gap {loadedGap:R}."); }
                return ok;
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(go2); }
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
