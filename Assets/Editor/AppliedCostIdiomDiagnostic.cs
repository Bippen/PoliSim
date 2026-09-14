using System;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §497 (2026-09-14): THE APPLIED-COST IDIOM UNDER THE INDEXED BOOK. Three dial costs land on spending lines by the applied-difference
    /// idiom - the justice dials' on Justice, border enforcement's on HomelandSecurity, the sector dials' support on Commerce - each a
    /// stateless target composed with its line through a tracker on the country, so a boundary applies only what moved. Two escapes are
    /// measured here on the USA, whose book carries all three lines: (1) THE INDEX - `IndexSpendingLines` scales a line's whole amount, the
    /// applied cost inside it included, and a tracker left unindexed made the next boundary's difference re-apply the index's share every
    /// year; (2) THE CLONE - `ClonePreviewCountry` carried no tracker, so a preview re-applied every standing dial's whole cost on top of
    /// lines that already carried it. The dials are set off neutral directly, as passed bills would leave them, and the private seams are
    /// called by reflection so nothing but the idiom moves between the two readings.
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
            var sb = new StringBuilder("=== APPLIED-COST IDIOM (§497): the dial costs' trackers ride their line's index and cross the preview clone ===\n");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country usa = world.GetCountry(CountryId.USA);
                const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo index = typeof(SimulationManager).GetMethod("IndexSpendingLines", instance);
                MethodInfo enforcement = typeof(SimulationManager).GetMethod("ApplyEnforcementCostPressure", instance);
                MethodInfo sector = typeof(SimulationManager).GetMethod("ApplySectorSupportCostPressure", instance);
                MethodInfo clone = typeof(SimulationManager).GetMethod("ClonePreviewCountry", BindingFlags.Static | BindingFlags.NonPublic);
                if (index == null || enforcement == null || sector == null || clone == null)
                {
                    Debug.LogError("APPLIED COST: a seam was not found by reflection - the idiom is UNVERIFIED, not clean.");
                    CheckExit.Finish(1);
                    return;
                }

                SpendingLine justice = Find(usa, SpendingCategory.Justice), border = Find(usa, SpendingCategory.HomelandSecurity), commerce = Find(usa, SpendingCategory.Commerce);
                if (justice == null || border == null || commerce == null) { Debug.LogError("APPLIED COST: the USA's book lacks a landing line."); CheckExit.Finish(1); return; }

                // the dials off neutral, as passed bills would leave them, and the first boundary's costs applied
                usa.PoliceFundingLevel = 80f;
                usa.BorderEnforcementLevel = 80f;
                foreach (Sector s in usa.Sectors) { if (s.Type == SectorType.Manufacturing) { s.SubsidyLevel = 80f; } }
                enforcement.Invoke(sim, new object[] { usa });
                sector.Invoke(sim, new object[] { usa });
                double jBase = justice.Amount - usa.AppliedJusticeEnforcementCost, bBase = border.Amount - usa.AppliedBorderEnforcementCost, cBase = commerce.Amount - usa.AppliedSectorSupportCost;
                double j0 = usa.AppliedJusticeEnforcementCost, b0 = usa.AppliedBorderEnforcementCost, c0 = usa.AppliedSectorSupportCost;

                // (1) a year of prices, then the next boundary: each line's dial-cost share must be the new target, not the new target plus the index's share of the old
                usa.State.PriceLevel *= 1.05f;
                double jSeed = justice.SeedAmount, bSeed = border.SeedAmount, cSeed = commerce.SeedAmount;
                index.Invoke(sim, new object[] { usa });
                double fj = justice.SeedAmount / jSeed, fb = border.SeedAmount / bSeed, fc = commerce.SeedAmount / cSeed;
                enforcement.Invoke(sim, new object[] { usa });
                sector.Invoke(sim, new object[] { usa });
                sb.Append("    1. THE INDEX - a price level up 5 %, then the next boundary: the dial cost inside each line against its target\n");
                ok &= Share(sb, "Justice (police funding 80)", justice.Amount - jBase * fj, usa.AppliedJusticeEnforcementCost, j0, fj);
                ok &= Share(sb, "HomelandSecurity (border 80)", border.Amount - bBase * fb, usa.AppliedBorderEnforcementCost, b0, fb);
                ok &= Share(sb, "Commerce (manufacturing subsidy 80)", commerce.Amount - cBase * fc, usa.AppliedSectorSupportCost, c0, fc);

                // (2) the clone carries the trackers, so a boundary on the preview's copy applies only what moved - nothing, here
                Country copy = (Country)clone.Invoke(null, new object[] { usa });
                double cj = Find(copy, SpendingCategory.Justice).Amount, cb = Find(copy, SpendingCategory.HomelandSecurity).Amount, cc = Find(copy, SpendingCategory.Commerce).Amount;
                enforcement.Invoke(sim, new object[] { copy });
                sector.Invoke(sim, new object[] { copy });
                double dj = Find(copy, SpendingCategory.Justice).Amount - cj, db = Find(copy, SpendingCategory.HomelandSecurity).Amount - cb, dc = Find(copy, SpendingCategory.Commerce).Amount - cc;
                sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "    2. THE CLONE - the preview's copy carries trackers {0:F3} / {1:F3} / {2:F3} bn; a boundary on it moved the three lines by {3:+0.000;-0.000} / {4:+0.000;-0.000} / {5:+0.000;-0.000} bn\n",
                    copy.AppliedJusticeEnforcementCost, copy.AppliedBorderEnforcementCost, copy.AppliedSectorSupportCost, dj, db, dc));
                if (Math.Abs(dj) > ToleranceBillions || Math.Abs(db) > ToleranceBillions || Math.Abs(dc) > ToleranceBillions)
                {
                    ok = false;
                    Debug.LogError($"APPLIED COST: a boundary on the preview's copy re-applied the standing dial costs ({dj:F3} / {db:F3} / {dc:F3} bn) - the trackers did not cross the clone.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            Debug.Log(sb.ToString());
            Debug.Log(ok ? "=== AppliedCostIdiomDiagnostic: ALL ASSERTIONS PASS ===" : "=== AppliedCostIdiomDiagnostic: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static bool Share(StringBuilder sb, string name, double share, double target, double previous, double factor)
        {
            double off = share - target;
            sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "       {0,-38} inside the line {1:F3} bn, target {2:F3} (last year's {3:F3}, the line's index ×{4:F4}; an unindexed tracker would leave {5:F3} extra) - {6:+0.000;-0.000}\n",
                name, share, target, previous, factor, previous * (factor - 1.0), off));
            if (Math.Abs(off) <= ToleranceBillions && target > 0) { return true; }
            Debug.LogError($"APPLIED COST: {name} carries {share:F3} bn of dial cost against a target of {target:F3} - {off:F3} bn off.");
            return false;
        }

        private static SpendingLine Find(Country country, SpendingCategory category)
        {
            foreach (SpendingLine line in country.SpendingLines) { if (line.Category == category) { return line; } }
            return null;
        }
    }
}
