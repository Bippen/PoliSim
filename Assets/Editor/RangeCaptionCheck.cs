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
                case "    Override rate": basis = "the take on this partner is its imports x rate (TradeSystem)"; return 1;
                case "Fund drawdown": basis = "the withdrawal is GDP x percent, booked as revenue (SimulationManager)"; return 1;
                // P6-F2b (§542): the Energy tab's four instruments - the Energy sector's own dials under S9's names, each sign re-derived from the rule the captions speak to
                case "Retail intervention": return RetailInterventionSign(out basis);
                case "Market liberalisation": return LiberalisationSign(out basis);
                case "Investment planning": basis = "the Tax Credits dial under its instrument's name (S9): MacroSystem's sector sensitivity, by sign"; return MacroSystem.SectorDialOutputSign("Tax Credits");
                case "State ownership": basis = "the Nationalization / Deregulation dial under its instrument's name (S9): MacroSystem's sector sensitivity, by sign"; return MacroSystem.SectorDialOutputSign("Nationalization / Deregulation");
                default:
                    int sector = MacroSystem.SectorDialOutputSign(key);
                    basis = sector != 0 ? "MacroSystem's sector sensitivity, by sign" : "no coupling known for this dial";
                    return sector;
            }
        }

        /// <summary>P6-F2b: the levy on the bill as the Subsidy dial rises - two links, both computed on a fresh world's Poland (a levy in its stack, an energy line in its book):
        /// the subsidy's cost on the energy line rises with the dial (SectorCouplings.EnergySupportCostTarget at 40 and at 60), and the levy scale falls as the line rises
        /// (EnergyLedger.Compute at the seed clearing, the line as seeded and ten per cent up). The sign is their product.</summary>
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
            float keptAmount = line.Amount; line.Amount = keptAmount * 1.1f;
            double scaleRaised = EnergyLedger.Compute(pl, r, 1.0, 0.0).LevyScale;
            line.Amount = keptAmount;
            int cost = costHigh > costLow ? 1 : costHigh < costLow ? -1 : 0, levy = scaleRaised > scaleSeed ? 1 : scaleRaised < scaleSeed ? -1 : 0;
            basis = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Poland: the subsidy's cost on the energy line {0:0.###} at 40 and {1:0.###} at 60 (SectorCouplings); the levy scale {2:0.####} on the seeded line and {3:0.####} on it +10 % (EnergyLedger.Compute)", costLow, costHigh, scaleSeed, scaleRaised);
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
