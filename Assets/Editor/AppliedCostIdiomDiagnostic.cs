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
    /// idiom - the justice dials' on Justice, border enforcement's on HomelandSecurity, the sector dials' support on Commerce, and since EN-7a the
    /// Energy sector's subsidy on the energy line - each a stateless target composed with its line through a tracker on the country, so a boundary
    /// applies only what moved. Measured on the USA, whose book carries all four lines: (1) THE INDEX - `IndexSpendingLines` scales a line's whole
    /// amount, the applied cost inside it included, and a tracker left unindexed made the next boundary's difference re-apply the index's share every
    /// year; (2) THE CLONE - `ClonePreviewCountry` carried no tracker, so a preview re-applied every standing dial's whole cost on top of lines that
    /// already carried it. (3) THE ROUND TRIP (EN-7a, review-found) - a dial at its ceiling on a line its bound holds, then back to neutral: the line
    /// must return to its path (Justice and Commerce on the USA, the energy line on Italy, where the levy reads the line's move); the tracker that
    /// recorded the requested target left each line below its path for good. The dials are set directly, as passed bills would leave them, and
    /// the private seams are called by reflection so nothing but the idiom moves between the readings.
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
            sb.Append("    3. THE ROUND TRIP - each dial at 100 for a boundary (the line's bound holding part of the cost), then back to 50: the line against its path\n");
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
            double off = back - path, bound = seed * 3.0;
            sb.Append(F("       {0,-42} path {1:F3} bn · asked {2:F3} · at 100 the line {3:F3} (bound {4:F3}, the tracker {5:F3} = the move {6:F3}) · back at 50 the line {7:F3}, the tracker {8:F3} - {9:+0.0000;-0.0000} off its path\n",
                name, path, asked, up, bound, appliedUp, up - path, back, appliedBack, off));
            bool held = up < path + asked - 1e-3;   // the round trip means something only where the bound held part of the ask
            bool ok = held && Math.Abs(off) <= Math.Max(1e-4, 1e-5 * path) && Math.Abs(appliedBack) <= Math.Max(1e-4, 1e-5 * path) && Math.Abs(appliedUp - (up - path)) <= Math.Max(1e-4, 1e-5 * path);
            if (!held) { Debug.LogError($"APPLIED COST: {name} - the bound did not hold any of the ask (line {up:F3} for {path + asked:F3}); the round trip tests nothing - raise the ask."); }
            else if (!ok) { Debug.LogError($"APPLIED COST: {name} did not return to its path - {back:F4} against {path:F4} ({off:F4} off), the tracker {appliedBack:F4} after, {appliedUp:F4} at the ceiling against a move of {up - path:F4}."); }
            return ok;
        }

        private static SpendingLine Find(Country country, SpendingCategory category)
        {
            foreach (SpendingLine line in country.SpendingLines) { if (line.Category == category) { return line; } }
            return null;
        }

        private static string F(string format, params object[] args) => string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
    }
}
