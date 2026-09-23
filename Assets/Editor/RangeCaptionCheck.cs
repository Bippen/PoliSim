using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P4-B1 (Playtest 4, 2026-09-04): THE RANGE-CAPTION CATALOG AGAINST THE MODEL. (1) Every dial `DrawDialRow` prints in
    /// `GameController.cs` (the names DialLabelCheck reads) has a catalog entry, and every catalog entry has exactly ten
    /// bands with a name and a line. (2) Every band's claimed direction agrees with the dial's effect sign: bands whose
    /// centre lies above the dial's neutral level carry the sign the stat takes as the dial rises, bands below it the
    /// opposite, the neutral band zero. (3) The catalog's rising sign is the MODEL's, re-derived here rather than trusted:
    /// the six labour dials from `LaborCouplings.All` (the first edge on the stat the captions speak to), the five
    /// sector dials from `MacroSystem.SectorDialOutputSign`, the tariff, the override and the drawdown from the
    /// arithmetic that defines them (a take and a withdrawal rise with their rate). Exit 1 on any failure; the whole
    /// table is printed either way.
    /// </summary>
    public static class RangeCaptionCheck
    {
        /// <summary>CONVENTION (PF-4, §554): the words by which a Retail-intervention line would PROMISE A FLOOR - false wherever the full dial leaves levy standing.</summary>
        private static readonly Regex FloorPromise = new Regex("\\b(floor|nothing|none is left)\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// <summary>CONVENTION (PF-4, §554): the words by which a line above neutral ADMITS the levy may already be gone - owed wherever a levied country's is, at the band's lower edge.</summary>
        private static readonly Regex GoneAdmitted = new Regex("\\bgone\\b|past the levy", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DialCall = new Regex("(?:DrawDialRow|DrawRangeCaption)\\(\\s*\"([^\"]+)\"", RegexOptions.Compiled);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            var failures = new List<string>();
            sb.Append("=== RangeCaptionCheck (P4-B1): ten bands per dial, each band's direction the dial's own effect sign ===\n");

            // (1) the dials the game draws, read from the source the way DialLabelCheck reads them.
            string path = Path.Combine(Application.dataPath, "Scripts/UI/GameController.cs");
            var drawn = new List<string>();
            if (File.Exists(path))
            {
                foreach (Match m in DialCall.Matches(SourceText.WithoutComments(SourceText.ControllerPartials(path))))   // P6-F2b (§542): every partial of the controller
                {
                    string key = m.Groups[1].Value;
                    if (key == "Policy rate change" || key == "National rate push") { continue; }   // the Riksbank's two ±dials are not range dials; DialLabelCheck leaves them too
                    if (!drawn.Contains(key)) { drawn.Add(key); }
                }
            }
            else { failures.Add("GameController.cs not found - the drawn dials could not be enumerated, so this verified NOTHING about coverage"); }
            foreach (string key in drawn)
            {
                if (!RangeCaptions.TryGet(key, out RangeCaptions.Dial _)) { failures.Add($"dial '{key}' is drawn and has no caption catalog entry"); }
            }

            // (2) and (3): each catalog dial's bands against the model's sign.
            sb.Append(string.Format("    {0,-36} {1,-32} {2,6} {3,6}  {4}\n", "dial", "stat", "model", "catalog", "bands"));
            int dials = 0;
            foreach (RangeCaptions.Dial dial in RangeCaptions.All)
            {
                dials++;
                int modelSign = ModelRiseSign(dial.Key, out string basis);
                if (modelSign == 0) { failures.Add($"'{dial.Key}': no model sign could be derived ({basis})"); }
                else if (modelSign != dial.RiseSign) { failures.Add($"'{dial.Key}': the catalog says the {dial.Stat} {(dial.RiseSign > 0 ? "rises" : "falls")} as the dial rises; the model says it {(modelSign > 0 ? "rises" : "falls")} ({basis})"); }
                if (dial.Bands == null || dial.Bands.Length != 10) { failures.Add($"'{dial.Key}': {(dial.Bands == null ? 0 : dial.Bands.Length)} band(s), not ten"); continue; }
                var summary = new StringBuilder();
                for (int i = 0; i < 10; i++)
                {
                    RangeCaptions.Band b = dial.Bands[i];
                    if (string.IsNullOrWhiteSpace(b.Name) || string.IsNullOrWhiteSpace(b.Line)) { failures.Add($"'{dial.Key}' band {i}: an empty name or line"); }
                    if (b.Line != null && b.Line.Length > 96) { failures.Add($"'{dial.Key}' band {i} ('{b.Name}'): the line runs {b.Line.Length} characters - past the band at 1280"); }
                    // The neutral band by the catalog's own band rule (integer, not a float compared with a float: the
                    // first run of this check read band 4's centre 0.45 as below 0.5 − 0.05 by one ulp and failed nine dials).
                    int neutralBand = RangeCaptions.NeutralBand(dial);
                    int expected = i > neutralBand ? dial.RiseSign : i < neutralBand ? -dial.RiseSign : 0;
                    if (b.Sign != expected) { failures.Add($"'{dial.Key}' band {i} ('{b.Name}') claims {b.Sign:+0;-0;0} on the {dial.Stat}; at that range the model gives {expected:+0;-0;0}"); }
                    summary.Append(b.Sign > 0 ? '+' : b.Sign < 0 ? '-' : '0');
                }
                sb.Append(string.Format("    {0,-36} {1,-32} {2,6} {3,6}  {4}\n", dial.Key, dial.Stat, modelSign.ToString("+0;-0;0"), dial.RiseSign.ToString("+0;-0;0"), summary));
            }
            sb.Append($"    {dials} dial(s) in the catalog, {drawn.Count} drawn by GameController.\n");
            RetailReach(failures, sb);

            if (failures.Count == 0)
            {
                sb.Append("\n=== RangeCaptionCheck: ALL ASSERTIONS PASS ===\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
            }
            else
            {
                sb.Append($"\n=== RangeCaptionCheck: {failures.Count} FAILURE(S) ===\n");
                foreach (string f in failures) { sb.Append("    ").Append(f).Append('\n'); }
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
            }
        }

        /// <summary>
        /// PF-4 (2026-09-21, §554): RETAIL INTERVENTION'S LINES AGAINST THE DIAL'S REACH, on every levied country's book. The sign test above reads one country; the defect it missed
        /// was a PROMISE: *the levy at its floor* where the full dial leaves most of the levy standing (Italy, Poland), and lines describing a falling levy where it was gone a few
        /// points above neutral (Sweden, France). For each country with an energy line and a policy levy the dial's cost at 50, 60 … 100 is stood on the line the way
        /// SimulationManager composes it (the tracker and the line together) and the book's levy scale read: (1) where ANY country's levy still stands at the top of the dial, no
        /// line above neutral may promise a floor; (2) where ANY country's levy is already gone at a band's lower edge, that band's line must admit it.
        /// </summary>
        private static void RetailReach(List<string> failures, StringBuilder sb)
        {
            if (!RangeCaptions.TryGet("Retail intervention", out RangeCaptions.Dial dial) || dial.Bands == null || dial.Bands.Length != 10) { return; }   // its absence is the coverage pass's failure
            sb.Append("\n    RETAIL INTERVENTION'S REACH (PF-4): the levy scale with the dial's cost standing, per levied country\n");
            PoliSim.Data.World world = PoliSim.Data.WorldFactory.CreateDefault();
            var goneAt = new List<string>[10]; var standsAtTop = new List<string>(); int levied = 0;
            for (int i = 0; i < 10; i++) { goneAt[i] = new List<string>(); }
            foreach (PoliSim.Data.Country c in world.Countries)
            {
                PoliSim.Data.SpendingLine line = SectorCouplings.EnergyLine(c);
                if (line == null || !EnergyLedger.HasPolicyLevy(c.Id)) { continue; }
                levied++;
                EnergyMarket.Result r = EnergyMarket.ClearAtSeed(c.Id);
                float keptAmount = line.Amount, keptApplied = c.AppliedEnergySupportCost;
                var scale = new double[11];
                for (int k = 5; k <= 10; k++)
                {
                    float cost = SectorCouplings.SupportCost(c.State.NominalGdp, k * 10f, SectorCouplings.NeutralDialLevel, SectorCouplings.NeutralDialLevel);
                    line.Amount = keptAmount + cost; c.AppliedEnergySupportCost = keptApplied + cost;
                    scale[k] = EnergyLedger.Compute(c, r, 1.0, 0.0).LevyScale;
                }
                line.Amount = keptAmount; c.AppliedEnergySupportCost = keptApplied;
                for (int band = 5; band <= 9; band++) { if (scale[band] <= 0.0) { goneAt[band].Add(c.Id.ToString()); } }   // band b holds (10b, 10b + 10]: its lower edge is the dial at 10b
                if (scale[10] > 0.0) { standsAtTop.Add(c.Id.ToString()); }
                sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "    {0,-8} dial 50: {1:0.###} · 60: {2:0.###} · 70: {3:0.###} · 80: {4:0.###} · 90: {5:0.###} · 100: {6:0.###}\n", c.Id, scale[5], scale[6], scale[7], scale[8], scale[9], scale[10]));
            }
            if (levied == 0) { failures.Add("'Retail intervention': no country carries an energy line and a policy levy - the reach pass verified NOTHING"); return; }
            for (int band = 5; band <= 9; band++)
            {
                string text = dial.Bands[band].Line ?? string.Empty;
                if (standsAtTop.Count > 0 && FloorPromise.IsMatch(text)) { failures.Add($"'Retail intervention' band {band} ('{dial.Bands[band].Name}') promises a floor - \"{text}\" - and the full dial leaves levy standing in {string.Join(", ", standsAtTop)}"); }
                if (goneAt[band].Count > 0 && !GoneAdmitted.IsMatch(text)) { failures.Add($"'Retail intervention' band {band} ('{dial.Bands[band].Name}') speaks of a levy still falling - \"{text}\" - and at the band's lower edge it is already gone in {string.Join(", ", goneAt[band])}"); }
            }
        }

        /// <summary>The model's own sign for the stat the dial's captions speak to, re-derived from the coupling that defines it.</summary>
        private static int ModelRiseSign(string key, out string basis)
        {
            switch (key)
            {
                case "Minimum Wage": return Labor(LaborDial.MinimumWage, LaborEffectStat.PovertyRate, out basis);
                case "Paid Family Leave": return Labor(LaborDial.PaidFamilyLeave, LaborEffectStat.LaborForceParticipation, out basis);
                case "Overtime / Working-Hour Regulation": return Labor(LaborDial.OvertimeRegulation, LaborEffectStat.UnemploymentRate, out basis);
                case "Workforce Retraining Programs": return Labor(LaborDial.RetrainingProgram, LaborEffectStat.UnemploymentRate, out basis);
                case "Family Policy": return Labor(LaborDial.FamilyPolicy, LaborEffectStat.BirthRate, out basis);
                case "Immigration Policy": return Labor(LaborDial.ImmigrationPolicy, LaborEffectStat.NetMigrationRate, out basis);
                case "Discretionary line": basis = "the figure set is the line's amount (ApplySpendingLineChanges, the nominal target) - P5-B5"; return 1;
                case "Mandatory line": basis = "the figure set is the line's amount (ApplySpendingLineChanges, the nominal target) - P5-B5"; return 1;
                case "General Base Tariff": basis = "the take is imports x rate (TradeSystem)"; return 1;
                case "Override rate": basis = "the take on this partner is its imports x rate (TradeSystem)"; return 1;
                case "Fund drawdown": basis = "the withdrawal is GDP x percent, booked as revenue (SimulationManager)"; return 1;
                // P6-F2b (§542): the Energy tab's four instruments - the Energy sector's own dials under S9's names, each sign re-derived from the rule the captions speak to
                case "Retail intervention": return RetailInterventionSign(out basis);
                case "Market liberalisation": return LiberalisationSign(out basis);
                case "Investment planning": basis = "the Tax Credits dial under its instrument's name (S9): MacroSystem's sector sensitivity, by sign"; return MacroSystem.SectorDialOutputSign("Tax Credits");
                case "State ownership": basis = "the Nationalization / Deregulation dial under its instrument's name (S9): MacroSystem's sector sensitivity, by sign"; return MacroSystem.SectorDialOutputSign("Nationalization / Deregulation");
                case "Pension age": return PensionAgeSign(out basis);
                default:
                    int sector = MacroSystem.SectorDialOutputSign(key);
                    basis = sector != 0 ? "MacroSystem's sector sensitivity, by sign" : "no coupling known for this dial";
                    return sector;
            }
        }

        /// <summary>PN-1's dial (§590): the pension line's driver as the age rises - SpendingDrivers.Level(StatutoryPensionAge) on a fresh world's Sweden with the age SET BY A
        /// BILL at 63 and at 67 (the override the dial writes, so the check reads the dial's own route into the driver, not the statute's). Fewer people at or above the age is
        /// a smaller driver and, by the index, a smaller line.</summary>
        private static int PensionAgeSign(out string basis)
        {
            PoliSim.Data.World world = PoliSim.Data.WorldFactory.CreateDefault();
            PoliSim.Data.Country se = world.GetCountry(PoliSim.Data.CountryId.Sweden);
            if (se == null || se.Cohorts == null) { basis = "Sweden carries no pyramid - nothing to derive the sign from"; return 0; }
            float kept = se.PensionAgeOverride;
            se.PensionAgeOverride = 63f; float low = PoliSim.Data.SpendingDrivers.Level(PoliSim.Data.SpendingDriver.StatutoryPensionAge, se);
            se.PensionAgeOverride = 67f; float high = PoliSim.Data.SpendingDrivers.Level(PoliSim.Data.SpendingDriver.StatutoryPensionAge, se);
            se.PensionAgeOverride = kept;
            basis = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Sweden: the pension line's driver {0:0.###} M at an age set to 63 and {1:0.###} M at 67 (SpendingDrivers.StatutoryPensionAge through the override)", low, high);
            return high > low ? 1 : high < low ? -1 : 0;
        }

        /// <summary>P6-F2b: the levy on the bill as the Subsidy dial rises - two links, both computed on a fresh world's Poland (a levy in its stack, an energy line in its book):
        /// the subsidy's cost on the energy line rises with the dial (SectorCouplings.EnergySupportCostTarget at 40 and at 60), and the levy scale falls as that cost stands on
        /// the line (EnergyLedger.Compute at the seed clearing, the line as seeded and with a tenth of it on top AS THE DIAL'S APPLIED COST - the dial's own route: since FT-10 ·
        /// P-A′, §553, the levy reads the dial's cost in full and the line's own move at its support share, which is none in Poland). The sign is their product.</summary>
        private static int RetailInterventionSign(out string basis)
        {
            PoliSim.Data.World world = PoliSim.Data.WorldFactory.CreateDefault();
            PoliSim.Data.Country pl = world.GetCountry(PoliSim.Data.CountryId.Poland);
            PoliSim.Data.Sector energy = null; PoliSim.Data.SpendingLine line = null;
            if (pl != null)
            {
                foreach (PoliSim.Data.Sector s in pl.Sectors) { if (s.Type == PoliSim.Data.SectorType.Energy) { energy = s; } }
                foreach (PoliSim.Data.SpendingLine l in pl.SpendingLines) { if (l.Category == PoliSim.Data.SpendingCategory.Energy) { line = l; } }
            }
            if (energy == null || line == null) { basis = "Poland carries no Energy sector or no energy line - nothing to derive the sign from"; return 0; }
            float keptLevel = energy.SubsidyLevel;
            energy.SubsidyLevel = 40f; float costLow = SectorCouplings.EnergySupportCostTarget(pl);
            energy.SubsidyLevel = 60f; float costHigh = SectorCouplings.EnergySupportCostTarget(pl);
            energy.SubsidyLevel = keptLevel;
            EnergyMarket.Result r = EnergyMarket.ClearAtSeed(pl.Id);
            double scaleSeed = EnergyLedger.Compute(pl, r, 1.0, 0.0).LevyScale;
            float keptAmount = line.Amount, keptApplied = pl.AppliedEnergySupportCost, dialCost = keptAmount * 0.1f;
            line.Amount = keptAmount + dialCost; pl.AppliedEnergySupportCost = keptApplied + dialCost;   // as SimulationManager's ApplyCostOnLine composes it: the tracker and the line together
            double scaleRaised = EnergyLedger.Compute(pl, r, 1.0, 0.0).LevyScale;
            line.Amount = keptAmount; pl.AppliedEnergySupportCost = keptApplied;
            int cost = costHigh > costLow ? 1 : costHigh < costLow ? -1 : 0, levy = scaleRaised > scaleSeed ? 1 : scaleRaised < scaleSeed ? -1 : 0;
            basis = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Poland: the subsidy's cost on the energy line {0:0.###} at 40 and {1:0.###} at 60 (SectorCouplings); the levy scale {2:0.####} on the seeded line and {3:0.####} with a tenth of the line on it as the dial's applied cost (EnergyLedger.Compute)", costLow, costHigh, scaleSeed, scaleRaised);
            return cost * levy;
        }

        /// <summary>P6-F2b: industry's price against households' as the Regulation dial rises - EnergyLedger.LiberalisationShiftPerKwh at a gap of -0.1 (regulation ten points ABOVE
        /// the seeded anchor), Poland's catalog row, the seed's prices: non-households' shift and households'. +1 where industry's rises and households' falls.</summary>
        private static int LiberalisationSign(out string basis)
        {
            int ci = PoliSim.Data.EnergyLayer.Index(PoliSim.Data.CountryId.Poland);
            if (ci < 0) { basis = "Poland is not in the energy layer"; return 0; }
            double industry = EnergyLedger.LiberalisationShiftPerKwh(ci, EnergyLedger.NonHouseholds, -0.1, 1.0, 1.0);
            double households = EnergyLedger.LiberalisationShiftPerKwh(ci, EnergyLedger.Households, -0.1, 1.0, 1.0);
            basis = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Poland, regulation ten points above its anchor: non-households' margin {0:+0.#####;-0.#####} per kWh, households' {1:+0.#####;-0.#####} (EnergyLedger.LiberalisationShiftPerKwh)", industry, households);
            return industry > 0 && households < 0 ? 1 : industry < 0 && households > 0 ? -1 : 0;
        }

        private static int Labor(LaborDial dial, LaborEffectStat stat, out string basis)
        {
            foreach (LaborCoupling c in LaborCouplings.All)
            {
                if (c.Dial == dial && c.Stat == stat)
                {
                    basis = $"LaborCouplings.All {dial} -> {stat} {c.SignedSensitivity:+0.####;-0.####}{(c.Contested ? " (contested)" : "")}";
                    return c.SignedSensitivity > 0f ? 1 : c.SignedSensitivity < 0f ? -1 : 0;
                }
            }
            basis = $"no LaborCoupling {dial} -> {stat}";
            return 0;
        }
    }
}
