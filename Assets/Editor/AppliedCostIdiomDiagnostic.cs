using System;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §497 (2026-09-14): THE APPLIED-COST IDIOM UNDER THE INDEXED BOOK. Four dial costs land on spending lines by the applied-difference
    /// idiom - the justice dials' on Justice, border enforcement's on HomelandSecurity, the sector dials' support on the support line (each statute
    /// budget's Business-and-industry line, the USA's Commerce - SC-1), and since EN-7a the Energy sector's subsidy on the energy line - each a stateless
    /// target composed with its line through a tracker on the country, so a boundary applies only what moved. Measured on the USA, whose book carries
    /// all four lines: (1) THE INDEX - a tracker left unindexed made the next boundary's difference re-apply the index's share every year; (2) THE
    /// CLONE - `ClonePreviewCountry` carried no tracker, so a preview re-applied every standing dial's whole cost on top of lines that already carried
    /// it. (3) THE COST OUTSIDE THE BAND (SC-1, ruled 2026-09-15: "the seed-relative clamp bounds the line's own path, not a cost the player
    /// deliberately set") - a dial at its ceiling asking more than the line's band would hold, then back to neutral: the whole cost lands and the line
    /// returns to its path (Justice and Commerce on the USA, the energy line on Italy). (4) SC-1 PER COUNTRY - where the support cost lands in each
    /// of the six books against what the rule before booked, the index and a percent change moving the own path with the cost standing, a cut below
    /// neutral held at the one bound a cost meets (zero), and the way back. The dials are set directly, as passed bills would leave them, and the
    /// private seams are called by reflection so nothing but the idiom moves between the readings.
    /// </summary>
    public static class AppliedCostIdiomDiagnostic
    {
        /// <summary>CONVENTION: a twentieth of a billion - float resolution on a line of a few hundred billion is under a thousandth of one.</summary>
        private const double ToleranceBillions = 0.05;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("APPLIED_COST_IDIOM");
            bool ok = true;
            var sb = new StringBuilder("=== APPLIED-COST IDIOM (§497): the dial costs' trackers ride their line's index, cross the preview clone, and give back only what they added ===\n");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country usa = world.GetCountry(CountryId.USA);
                const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo index = typeof(SimulationManager).GetMethod("IndexSpendingLines", instance);
                MethodInfo enforcement = typeof(SimulationManager).GetMethod("ApplyEnforcementCostPressure", instance);
                MethodInfo sector = typeof(SimulationManager).GetMethod("ApplySectorSupportCostPressure", instance);
                MethodInfo energy = typeof(SimulationManager).GetMethod("ApplyEnergySupportCostPressure", instance);
                MethodInfo clone = typeof(SimulationManager).GetMethod("ClonePreviewCountry", BindingFlags.Static | BindingFlags.NonPublic);
                if (index == null || enforcement == null || sector == null || energy == null || clone == null)
                {
                    Debug.LogError("APPLIED COST: a seam was not found by reflection - the idiom is UNVERIFIED, not clean.");
                    CheckExit.Finish(1);
                    return;
                }

                SpendingLine justice = Find(usa, SpendingCategory.Justice), border = Find(usa, SpendingCategory.HomelandSecurity), commerce = Find(usa, SpendingCategory.Commerce), energyLine = Find(usa, SpendingCategory.Energy);
                if (justice == null || border == null || commerce == null || energyLine == null) { Debug.LogError("APPLIED COST: the USA's book lacks a landing line."); CheckExit.Finish(1); return; }

                // the dials off neutral, as passed bills would leave them, and the first boundary's costs applied
                usa.PoliceFundingLevel = 80f;
                usa.BorderEnforcementLevel = 80f;
                foreach (Sector s in usa.Sectors)
                {
                    if (s.Type == SectorType.Manufacturing) { s.SubsidyLevel = 80f; }
                    if (s.Type == SectorType.Energy) { s.SubsidyLevel = 80f; }
                }
                enforcement.Invoke(sim, new object[] { usa });
                sector.Invoke(sim, new object[] { usa });
                energy.Invoke(sim, new object[] { usa });
                double jBase = justice.Amount - usa.AppliedJusticeEnforcementCost, bBase = border.Amount - usa.AppliedBorderEnforcementCost, cBase = commerce.Amount - usa.AppliedSectorSupportCost, eBase = energyLine.Amount - usa.AppliedEnergySupportCost;
                double j0 = usa.AppliedJusticeEnforcementCost, b0 = usa.AppliedBorderEnforcementCost, c0 = usa.AppliedSectorSupportCost, e0 = usa.AppliedEnergySupportCost;

                // (1) a year of prices, then the next boundary: each line's dial-cost share must be the new target, not the new target plus the index's share of the old
                usa.State.PriceLevel *= 1.05f;
                double jSeed = justice.SeedAmount, bSeed = border.SeedAmount, cSeed = commerce.SeedAmount, eSeed = energyLine.SeedAmount;
                index.Invoke(sim, new object[] { usa });
                double fj = justice.SeedAmount / jSeed, fb = border.SeedAmount / bSeed, fc = commerce.SeedAmount / cSeed, fe = energyLine.SeedAmount / eSeed;
                enforcement.Invoke(sim, new object[] { usa });
                sector.Invoke(sim, new object[] { usa });
                energy.Invoke(sim, new object[] { usa });
                sb.Append("    1. THE INDEX - a price level up 5 %, then the next boundary: the dial cost inside each line against its target\n");
                ok &= Share(sb, "Justice (police funding 80)", justice.Amount - jBase * fj, usa.AppliedJusticeEnforcementCost, j0, fj);
                ok &= Share(sb, "HomelandSecurity (border 80)", border.Amount - bBase * fb, usa.AppliedBorderEnforcementCost, b0, fb);
                ok &= Share(sb, "Commerce (manufacturing subsidy 80)", commerce.Amount - cBase * fc, usa.AppliedSectorSupportCost, c0, fc);
                ok &= Share(sb, "Energy (energy subsidy 80)", energyLine.Amount - eBase * fe, usa.AppliedEnergySupportCost, e0, fe);

                // (2) the clone carries the trackers, so a boundary on the preview's copy applies only what moved - nothing, here
                Country copy = (Country)clone.Invoke(null, new object[] { usa });
                double cj = Find(copy, SpendingCategory.Justice).Amount, cb = Find(copy, SpendingCategory.HomelandSecurity).Amount, cc = Find(copy, SpendingCategory.Commerce).Amount, ce = Find(copy, SpendingCategory.Energy).Amount;
                enforcement.Invoke(sim, new object[] { copy });
                sector.Invoke(sim, new object[] { copy });
                energy.Invoke(sim, new object[] { copy });
                double dj = Find(copy, SpendingCategory.Justice).Amount - cj, db = Find(copy, SpendingCategory.HomelandSecurity).Amount - cb, dc = Find(copy, SpendingCategory.Commerce).Amount - cc, de = Find(copy, SpendingCategory.Energy).Amount - ce;
                sb.Append(F("    2. THE CLONE - the preview's copy carries trackers {0:F3} / {1:F3} / {2:F3} / {3:F3} bn; a boundary on it moved the four lines by {4:+0.000;-0.000} / {5:+0.000;-0.000} / {6:+0.000;-0.000} / {7:+0.000;-0.000} bn\n",
                    copy.AppliedJusticeEnforcementCost, copy.AppliedBorderEnforcementCost, copy.AppliedSectorSupportCost, copy.AppliedEnergySupportCost, dj, db, dc, de));
                if (Math.Abs(dj) > ToleranceBillions || Math.Abs(db) > ToleranceBillions || Math.Abs(dc) > ToleranceBillions || Math.Abs(de) > ToleranceBillions)
                {
                    ok = false;
                    Debug.LogError($"APPLIED COST: a boundary on the preview's copy re-applied the standing dial costs ({dj:F3} / {db:F3} / {dc:F3} / {de:F3} bn) - the trackers did not cross the clone.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            // (3) the round trip, on a fresh world: every dial that lands a cost at its ceiling for a boundary, then back to neutral for the next
            sb.Append("    3. THE COST OUTSIDE THE BAND (SC-1) - each dial at 100 for a boundary, asking more than the line's seed band would hold, then back to 50: the whole cost on the line, then the line against its path\n");
            SimulationRandom.Seed(777);
            World trip = WorldFactory.CreateDefault();
            var go3 = new GameObject("APPLIED_COST_ROUND_TRIP");
            try
            {
                SimulationManager sim = go3.AddComponent<SimulationManager>();
                sim.SetWorld(trip);
                const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo enforcement = typeof(SimulationManager).GetMethod("ApplyEnforcementCostPressure", instance);
                MethodInfo sector = typeof(SimulationManager).GetMethod("ApplySectorSupportCostPressure", instance);
                MethodInfo energy = typeof(SimulationManager).GetMethod("ApplyEnergySupportCostPressure", instance);
                Country usa = trip.GetCountry(CountryId.USA), italy = trip.GetCountry(CountryId.Italy);
                SpendingLine justice = Find(usa, SpendingCategory.Justice), commerce = Find(usa, SpendingCategory.Commerce), italyEnergy = Find(italy, SpendingCategory.Energy);
                if (justice == null || commerce == null || italyEnergy == null) { ok = false; Debug.LogError("APPLIED COST: the round trip's lines are missing (USA Justice, USA Commerce, Italy Energy)."); }
                else
                {
                    float justice0 = justice.Amount, commerce0 = commerce.Amount, energy0 = italyEnergy.Amount;
                    void Boundary()
                    {
                        foreach (Country k in new[] { usa, italy })
                        {
                            enforcement.Invoke(sim, new object[] { k });
                            sector.Invoke(sim, new object[] { k });
                            energy.Invoke(sim, new object[] { k });
                        }
                    }
                    usa.PoliceFundingLevel = 100f;
                    foreach (Sector s in usa.Sectors) { if (s.Type != SectorType.Energy) { s.SubsidyLevel = 100f; } }
                    foreach (Sector s in italy.Sectors) { if (s.Type == SectorType.Energy) { s.SubsidyLevel = 100f; } }
                    // the asks: police funding's own term (the judicial dial and the prison stock stand at neutral in a fresh world), the two sector targets
                    float justiceTarget = usa.State.NominalGdp / 100f * CrimeJusticeCouplings.PoliceFundingBudgetCostPercentOfGdpPerPoint * (100f - CrimeJusticeCouplings.NeutralDialLevel);
                    float commerceTarget = SectorCouplings.SupportCostTarget(usa), energyTarget = SectorCouplings.EnergySupportCostTarget(italy);
                    Boundary();
                    float justiceUp = justice.Amount, commerceUp = commerce.Amount, energyUp = italyEnergy.Amount;
                    float justiceApplied = usa.AppliedJusticeEnforcementCost, commerceApplied = usa.AppliedSectorSupportCost, energyApplied = italy.AppliedEnergySupportCost;
                    usa.PoliceFundingLevel = 50f;
                    foreach (Sector s in usa.Sectors) { s.SubsidyLevel = 50f; }
                    foreach (Sector s in italy.Sectors) { s.SubsidyLevel = 50f; }
                    Boundary();
                    ok &= Trip(sb, "USA Justice (police funding 100)", justice0, justiceTarget, justiceUp, justiceApplied, justice.Amount, usa.AppliedJusticeEnforcementCost, justice.SeedAmount);
                    ok &= Trip(sb, "USA Commerce (seven sectors' subsidy 100)", commerce0, commerceTarget, commerceUp, commerceApplied, commerce.Amount, usa.AppliedSectorSupportCost, commerce.SeedAmount);
                    ok &= Trip(sb, "Italy Energy (energy subsidy 100)", energy0, energyTarget, energyUp, energyApplied, italyEnergy.Amount, italy.AppliedEnergySupportCost, italyEnergy.SeedAmount);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go3);
            }

            // (4) SC-1 per country, each on a fresh world
            sb.Append("    4. SC-1 PER COUNTRY - every sector's subsidy at 100 (tax credits and research at 50): where the support cost lands against what the rule before booked; then, the cost standing, a price level up 5 % and a +10 % change on the line (each moves the own path, the cost rides outside); then every subsidy at 0 (a cut below neutral, held at zero); then back to 50\n");
            HeldAtZero = 0;
            foreach (CountryId id in new[] { CountryId.USA, CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland })
            {
                ok &= PerCountry(sb, id);
            }
            if (HeldAtZero == 0) { ok = false; Debug.LogError("APPLIED COST: no book's cut reached the zero bound - section 4's zero-bound assertion tests nothing; deepen the cut."); }
            sb.Append(F("       the zero bound held the cut in {0} of the six books\n", HeldAtZero));

            Debug.Log(sb.ToString());
            Debug.Log(ok ? "=== AppliedCostIdiomDiagnostic: ALL ASSERTIONS PASS ===" : "=== AppliedCostIdiomDiagnostic: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static bool Share(StringBuilder sb, string name, double share, double target, double previous, double factor)
        {
            double off = share - target;
            sb.Append(F("       {0,-38} inside the line {1:F3} bn, target {2:F3} (last year's {3:F3}, the line's index ×{4:F4}; an unindexed tracker would leave {5:F3} extra) - {6:+0.000;-0.000}\n",
                name, share, target, previous, factor, previous * (factor - 1.0), off));
            if (Math.Abs(off) <= ToleranceBillions && target > 0) { return true; }
            Debug.LogError($"APPLIED COST: {name} carries {share:F3} bn of dial cost against a target of {target:F3} - {off:F3} bn off.");
            return false;
        }

        private static bool Trip(StringBuilder sb, string name, float path, float asked, float up, float appliedUp, float back, float appliedBack, float seed)
        {
            double off = back - path, bound = seed * 3.0, tol = Math.Max(1e-3, 1e-6 * (path + Math.Abs(asked)));
            sb.Append(F("       {0,-42} path {1:F3} bn · asked {2:F3} · at 100 the line {3:F3} (the seed band's top {4:F3}, the tracker {5:F3}) · back at 50 the line {6:F3}, the tracker {7:F3} - {8:+0.0000;-0.0000} off its path\n",
                name, path, asked, up, bound, appliedUp, back, appliedBack, off));
            bool beyondBand = path + asked > bound + 1e-3;   // the test means something only where the band would have held part of the ask
            bool whole = Math.Abs(up - (path + asked)) <= tol && Math.Abs(appliedUp - asked) <= tol;
            bool ok = beyondBand && whole && Math.Abs(off) <= Math.Max(1e-4, 1e-5 * path) && Math.Abs(appliedBack) <= Math.Max(1e-4, 1e-5 * path);
            if (!beyondBand) { Debug.LogError($"APPLIED COST: {name} - the ask ({path + asked:F3}) stays inside the seed band ({bound:F3}); the check tests nothing - raise the ask."); }
            else if (!whole) { Debug.LogError($"APPLIED COST: {name} - the line carried {up - path:F4} of a {asked:F4} cost (the tracker {appliedUp:F4}): the seed band held the cost, which SC-1 puts outside it."); }
            else if (!ok) { Debug.LogError($"APPLIED COST: {name} did not return to its path - {back:F4} against {path:F4} ({off:F4} off), the tracker {appliedBack:F4} after."); }
            return ok;
        }

        /// <summary>SC-1 on one country's book, on a fresh world: the landing, the cost against the rule before, the index and a percent change with the cost
        /// standing, the cut held at zero, the way back - each read against figures computed here from the seams' inputs, not from the composing code.</summary>
        private static bool PerCountry(StringBuilder sb, CountryId id)
        {
            bool ok = true;
            SimulationRandom.Seed(777);
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("SC1_" + id);
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(w);
                const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo index = typeof(SimulationManager).GetMethod("IndexSpendingLines", instance);
                MethodInfo sectorPressure = typeof(SimulationManager).GetMethod("ApplySectorSupportCostPressure", instance);
                MethodInfo energyPressure = typeof(SimulationManager).GetMethod("ApplyEnergySupportCostPressure", instance);
                MethodInfo changes = typeof(SimulationManager).GetMethod("ApplySpendingLineChanges", instance);
                if (index == null || sectorPressure == null || energyPressure == null || changes == null) { Debug.LogError("APPLIED COST: an SC-1 seam was not found by reflection - SC-1 is UNVERIFIED."); return false; }
                Country k = w.GetCountry(id);
                SpendingLine line = SectorCouplings.SupportLine(k);
                SpendingCategory expected = id == CountryId.USA ? SpendingCategory.Commerce : SpendingCategory.BusinessAndIndustry;
                if (line == null || line.Category != expected) { Debug.LogError($"APPLIED COST: {id}'s sector support lands on {(line == null ? "no line" : line.Category.ToString())}, not {expected} (SC-1)."); return false; }
                SpendingLine oldLanding = Find(k, SpendingCategory.Commerce) ?? Find(k, SpendingCategory.PublicServices);   // the rule before SC-1
                void Boundary() { sectorPressure.Invoke(sim, new object[] { k }); energyPressure.Invoke(sim, new object[] { k }); }
                double tol(double x) => Math.Max(1e-3, 2e-6 * Math.Abs(x));
                double gdp = k.State.NominalGdp;

                // the stance
                double path = line.Amount, seed = line.SeedAmount;
                foreach (Sector s in k.Sectors) { s.SubsidyLevel = 100f; }
                double target = SectorCouplings.SupportCostTarget(k);
                Boundary();
                double up = line.Amount;
                double before = oldLanding == null ? 0.0 : Math.Min(Math.Max(path + target, seed * 0.2), seed * 3.0) - path;   // what the rule before would have booked
                if (Math.Abs(up - (path + target)) > tol(up) || Math.Abs(k.AppliedSectorSupportCost - target) > tol(target)) { ok = false; Debug.LogError($"APPLIED COST: {id}'s support line carries {up - path:F4} of a {target:F4} cost (tracker {k.AppliedSectorSupportCost:F4}) - not the whole cost outside its band."); }

                // the index with the cost standing: the own path indexed and clamped, the cost at the same factor outside
                k.State.PriceLevel *= 1.05f;
                double ownBefore = up - k.AppliedSectorSupportCost, trackerBefore = k.AppliedSectorSupportCost;
                index.Invoke(sim, new object[] { k });
                double f = line.SeedAmount / seed, seedNow = line.SeedAmount;
                double wantIndexed = Math.Min(Math.Max(ownBefore * f, seedNow * 0.2), seedNow * 3.0) + trackerBefore * f;
                double indexed = line.Amount;
                if (Math.Abs(indexed - wantIndexed) > tol(indexed) || Math.Abs(k.AppliedSectorSupportCost - trackerBefore * f) > tol(trackerBefore)) { ok = false; Debug.LogError($"APPLIED COST: {id}'s indexed support line {indexed:F4} is not its clamped indexed own path plus the indexed cost ({wantIndexed:F4})."); }

                // a +10 % change on the line with the cost standing: the own path moves, the cost does not
                PolicyDecision plus = PolicyDecision.None();
                plus.SpendingLineChanges[line.Category] = 10f;
                double ownNow = indexed - k.AppliedSectorSupportCost, costNow = k.AppliedSectorSupportCost;
                changes.Invoke(sim, new object[] { k, plus });
                double wantChanged = Math.Min(Math.Max(ownNow * 1.1, seedNow * 0.2), seedNow * 3.0) + costNow;
                double changed = line.Amount, changedAtPlus = line.Amount, costAtPlus = costNow;
                if (Math.Abs(changed - wantChanged) > tol(changed) || Math.Abs(k.AppliedSectorSupportCost - costNow) > tol(costNow)) { ok = false; Debug.LogError($"APPLIED COST: {id}'s support line after +10 % is {changed:F4}, not its own path's move plus the standing cost ({wantChanged:F4}) - the change scaled the cost."); }

                // a figure set on the line with the cost standing: the figure is the line's total - its own path the figure less the cost, clamped; the cost unmoved
                double figure = seedNow + costNow, wantSet = Math.Min(Math.Max(figure - costNow, seedNow * 0.2), seedNow * 3.0) + costNow;
                ok &= SetFigure(changes, sim, k, line, figure, wantSet, costNow, id, "a figure of its seed plus the cost", tol);
                // the band's two edges, the cost standing: a figure asking an own path of five seeds lands at the band's top, one of a twentieth at its floor
                double topFigure = seedNow * 5.0 + costNow;
                ok &= SetFigure(changes, sim, k, line, topFigure, seedNow * 3.0 + costNow, costNow, id, "an own path of five seeds (past the band's top)", tol);
                // the index at the band's top: the own path indexed and held at the NEW band's top, the cost at the factor outside
                k.State.PriceLevel *= 1.05f;
                double seedBeforeTop = line.SeedAmount, trackerAtTop = k.AppliedSectorSupportCost;
                index.Invoke(sim, new object[] { k });
                double f2 = line.SeedAmount / seedBeforeTop, wantTopIndexed = Math.Min(seedBeforeTop * 3.0 * f2, line.SeedAmount * 3.0) + trackerAtTop * f2;
                if (Math.Abs(line.Amount - wantTopIndexed) > tol(line.Amount)) { ok = false; Debug.LogError($"APPLIED COST: {id}'s support line indexed at the band's top is {line.Amount:F4}, not the new band's top plus the indexed cost ({wantTopIndexed:F4})."); }
                seedNow = line.SeedAmount; costNow = k.AppliedSectorSupportCost;
                double floorFigure = seedNow * 0.05 + costNow;
                ok &= SetFigure(changes, sim, k, line, floorFigure, seedNow * 0.2 + costNow, costNow, id, "an own path of a twentieth of the seed (past the band's floor)", tol);
                changed = line.Amount;

                // the cut: every subsidy at 0 - a negative target, held at zero where it would take the line under
                foreach (Sector s in k.Sectors) { s.SubsidyLevel = 0f; }
                double cutTarget = SectorCouplings.SupportCostTarget(k), ownCut = changed - k.AppliedSectorSupportCost;
                if (cutTarget < -ownCut) { HeldAtZero++; }
                Boundary();
                double cut = line.Amount, wantCut = Math.Max(0.0, ownCut + cutTarget), wantTracker = Math.Max(cutTarget, -ownCut);
                if (cut < 0.0 || Math.Abs(cut - wantCut) > tol(ownCut) || Math.Abs(k.AppliedSectorSupportCost - wantTracker) > tol(ownCut)) { ok = false; Debug.LogError($"APPLIED COST: {id}'s support line under a cut is {cut:F4} (tracker {k.AppliedSectorSupportCost:F4}) - not its own path plus the cut held at zero ({wantCut:F4}, tracker {wantTracker:F4})."); }

                // back to neutral: the own path, no cost
                foreach (Sector s in k.Sectors) { s.SubsidyLevel = 50f; }
                Boundary();
                double back = line.Amount;
                if (Math.Abs(back - ownCut) > tol(ownCut) || Math.Abs(k.AppliedSectorSupportCost) > tol(ownCut)) { ok = false; Debug.LogError($"APPLIED COST: {id}'s support line back at neutral is {back:F4}, not its own path {ownCut:F4} (tracker {k.AppliedSectorSupportCost:F4})."); }

                sb.Append(F("       {0,-8} {1} (seed {2:F3} bn, band {3:F3}-{4:F3}): the stance's cost {5:F3} bn ({6:F3} % of GDP) - on the line {7:+0.000;-0.000}, the rule before booked {8:+0.000;-0.000}{9}; indexed ×{10:F4} → {11:F3}; +10 % → {12:F3} (the cost {13:F3} unmoved); a figure at the seed plus the cost, then past the band's top ({18:F3}), indexed there, then past its floor → {19:F3}; every subsidy at 0: the cut {14:F3} → the line {15:F3}{16}; back at 50 → {17:F3}\n",
                    id, DisplayLine(line.Category), seed, seed * 0.2, seed * 3.0, target, target / gdp * 100.0, up - path, before, oldLanding == null ? " (no line)" : "", f, indexed, changedAtPlus, costAtPlus, cutTarget, cut, cutTarget < -ownCut ? " (held at zero)" : "", back, topFigure, changed));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            return ok;
        }

        private static string DisplayLine(SpendingCategory c) => c == SpendingCategory.BusinessAndIndustry ? "Business and industry" : c.ToString();

        /// <summary>How many of the six books' cuts section 4 held at zero - the zero bound is tested only if one did.</summary>
        private static int HeldAtZero;

        /// <summary>A figure set on the support line as a passed bill sets it (SpendingNominalTargets), read against the total computed by the caller; the cost must not move.</summary>
        private static bool SetFigure(MethodInfo changes, SimulationManager sim, Country k, SpendingLine line, double figure, double want, double cost, CountryId id, string what, Func<double, double> tol)
        {
            PolicyDecision set = PolicyDecision.None();
            set.SpendingNominalTargets[line.Category] = (float)figure;
            changes.Invoke(sim, new object[] { k, set });
            if (Math.Abs(line.Amount - want) <= tol(want) && Math.Abs(k.AppliedSectorSupportCost - cost) <= tol(cost)) { return true; }
            Debug.LogError($"APPLIED COST: {id}'s support line set to {figure:F4} ({what}) reads {line.Amount:F4}, not its clamped own path plus the standing cost ({want:F4}; the tracker {k.AppliedSectorSupportCost:F4} against {cost:F4}).");
            return false;
        }

        private static SpendingLine Find(Country country, SpendingCategory category)
        {
            foreach (SpendingLine line in country.SpendingLines) { if (line.Category == category) { return line; } }
            return null;
        }

        private static string F(string format, params object[] args) => string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
    }
}
