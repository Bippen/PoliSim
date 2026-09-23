using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §596's ATTRIBUTION PROBE: the no-policy world twice from one seed, the participation response suspended in the first and on in the second, turn by
    /// turn; per country the fiscal report's terms (revenue, the mandatory and discretionary lines, the unemployment benefit, welfare, interest) and GDP,
    /// the difference on minus off. Written to answer one question - why France's accumulated budget fell while its GDP rose - and kept as the family's
    /// attribution. Prints; asserts nothing.
    /// </summary>
    public static class PensionParticipationProbe
    {
        private static readonly List<Dictionary<string, float>> LastLines = new List<Dictionary<string, float>>();
        private static readonly StringBuilder Macro = new StringBuilder();

        public static void Run()
        {
            LastLines.Clear();
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);
            int turns = 12;
            foreach (string arg in System.Environment.GetCommandLineArgs()) { if (arg.StartsWith("-probeturns=")) { int.TryParse(arg.Substring(12), out turns); } }
            var off = new List<Dictionary<CountryId, (FiscalTurnReport R, float Gdp, float Lfp, float U, float Budget, float Debt)>>();
            var on = new List<Dictionary<CountryId, (FiscalTurnReport R, float Gdp, float Lfp, float U, float Budget, float Debt)>>();
            try
            {
                PensionParticipationResponse.ProbeSuspended = true;
                off = RunWorld(turns);
                PensionParticipationResponse.ProbeSuspended = false;
                on = RunWorld(turns);
            }
            finally { PensionParticipationResponse.ProbeSuspended = false; }

            sb.Append("=== PensionParticipationProbe (§596): on minus off, per turn ===\n");
            foreach (CountryId id in new[] { CountryId.France, CountryId.Germany, CountryId.Italy, CountryId.USA, CountryId.Sweden, CountryId.Poland })
            {
                sb.Append(F("  {0}\n    turn   dGDP   dLFP(pts)  dU(pts)   dRevenue  dMandatory  dDiscretionary  dUBcost  dWelfare  dInterest  dBalance  dBudgetAcc     dDebt  budgetOff\n", id));
                for (int t = 0; t < turns; t++)
                {
                    if (!off[t].TryGetValue(id, out var a) || !on[t].TryGetValue(id, out var b) || a.R == null || b.R == null) { continue; }
                    float balA = a.R.Revenue - a.R.MandatorySpending - a.R.DiscretionarySpending - a.R.UnemploymentBenefitCost - a.R.WelfareCost - a.R.InterestOnDebt;
                    float balB = b.R.Revenue - b.R.MandatorySpending - b.R.DiscretionarySpending - b.R.UnemploymentBenefitCost - b.R.WelfareCost - b.R.InterestOnDebt;
                    sb.Append(F("    {0,4} {1,8:0.00} {2,9:0.0000} {3,9:0.0000} {4,10:0.000} {5,11:0.000} {6,15:0.000} {7,8:0.000} {8,9:0.000} {9,10:0.000} {10,9:0.000} {11,11:0.000} {12,9:0.000} {13,10:0.000}\n",
                        t + 1, b.Gdp - a.Gdp, b.Lfp - a.Lfp, b.U - a.U, b.R.Revenue - a.R.Revenue, b.R.MandatorySpending - a.R.MandatorySpending,
                        b.R.DiscretionarySpending - a.R.DiscretionarySpending, b.R.UnemploymentBenefitCost - a.R.UnemploymentBenefitCost,
                        b.R.WelfareCost - a.R.WelfareCost, b.R.InterestOnDebt - a.R.InterestOnDebt, balB - balA, b.Budget - a.Budget, b.Debt - a.Debt, a.Budget));
                }
            }
            // every field of the report, France: which term the accumulator's move rides on
            sb.Append("  France - every fiscal-report field, on minus off, turns 3 to 12:\n");
            foreach (System.Reflection.FieldInfo fi in typeof(FiscalTurnReport).GetFields())
            {
                if (fi.FieldType != typeof(float)) { continue; }
                var line = new StringBuilder(); bool any = false;
                for (int t = 2; t < turns; t++)
                {
                    if (!off[t].TryGetValue(CountryId.France, out var a) || !on[t].TryGetValue(CountryId.France, out var b) || a.R == null || b.R == null) { continue; }
                    float d = (float)fi.GetValue(b.R) - (float)fi.GetValue(a.R);
                    if (Mathf.Abs(d) > 1e-4f) { any = true; }
                    line.Append(F(" {0,9:0.000}", d));
                }
                if (any) { sb.Append(F("    {0,-34}{1}\n", fi.Name, line)); }
            }
            if (LastLines.Count == 2)
            {
                sb.Append(F("  France - each spending line at the last turn, off -> on:\n"));
                foreach (KeyValuePair<string, float> kv in LastLines[0])
                {
                    float on2 = LastLines[1].TryGetValue(kv.Key, out float v) ? v : float.NaN;
                    if (Mathf.Abs(on2 - kv.Value) > 1e-3f) { sb.Append(F("    {0,-52} {1,10:0.000} -> {2,10:0.000}  {3:+0.000;-0.000}\n", kv.Key, kv.Value, on2, on2 - kv.Value)); }
                }
            }
            sb.Append("  France's macro per turn:\n").Append(Macro);
            Macro.Clear();
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static List<Dictionary<CountryId, (FiscalTurnReport R, float Gdp, float Lfp, float U, float Budget, float Debt)>> RunWorld(int turns)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("PP_PROBE");
            var rows = new List<Dictionary<CountryId, (FiscalTurnReport R, float Gdp, float Lfp, float U, float Budget, float Debt)>>();
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                for (int t = 0; t < turns; t++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    var row = new Dictionary<CountryId, (FiscalTurnReport, float, float, float, float, float)>();
                    foreach (Country c in world.Countries) { row[c.Id] = (sim.GetLastFiscalReport(c.Id), c.State.GDP, c.State.LaborForceParticipationRate, c.State.Unemployment, c.State.Budget, c.State.GovernmentDebt); }
                    Country frm = world.GetCountry(CountryId.France);
                    // what the AI finance ministry will write next turn (pure): the mean per-cent change it asks of France's lines, and the rule's required adjustment
                    PolicyDecision next = AiFinanceMinistry.Decide(frm, sim.GetLastFiscalReport(CountryId.France));
                    float meanCut = 0f; foreach (float v in next.SpendingLineChanges.Values) { meanCut += v; }
                    if (next.SpendingLineChanges.Count > 0) { meanCut /= next.SpendingLineChanges.Count; }
                    FiscalTurnReport rep = sim.GetLastFiscalReport(CountryId.France);
                    float req = rep != null ? AiFinanceMinistry.EuRequiredAdjustmentPercentOfGdp(rep.BudgetBalance / frm.State.NominalGdp * 100f, frm.State.DebtToGdpRatio, frm.DebtRatioLastReport > 0f ? frm.DebtRatioLastReport - frm.State.DebtToGdpRatio : 0f) : float.NaN;
                    Macro.Append(string.Format(CultureInfo.InvariantCulture, "{0}#{1}# ministry next: mean line change {2:+0.000;-0.000} % on {3} lines, required adjustment {4:0.000} % of GDP, expenditure excess {5:0.0000}, debt ratio {6:0.00}\n",
                        PensionParticipationResponse.ProbeSuspended ? "off" : "on ", t + 1, meanCut, next.SpendingLineChanges.Count, req, AiFinanceMinistry.EuExpenditureExcess(frm), frm.State.DebtToGdpRatio));
                    Macro.Append(string.Format(CultureInfo.InvariantCulture, "{0}|{1}|P {2:0.00000} infl {3:0.0000} realwage {4:0.0000} gdp {5:0.000} pot {6:0.000} gap {7:0.00000} nomgdp {8:0.000} lfp {9:0.0000} U {10:0.0000} year {11}\n",
                        PensionParticipationResponse.ProbeSuspended ? "off" : "on ", t + 1, frm.State.PriceLevel, frm.State.Inflation, frm.State.RealWageIndex, frm.State.GDP, frm.State.PotentialGDP, frm.State.GDP / frm.State.PotentialGDP, frm.State.NominalGdp, frm.State.LaborForceParticipationRate, frm.State.Unemployment, frm.CalendarYear));
                    rows.Add(row);
                }
                var fr = world.GetCountry(CountryId.France);
                var lines = new Dictionary<string, float>();
                foreach (SpendingLine l in fr.SpendingLines) { lines[(l.IsMandatory ? "M " : "D ") + l.Category + " (" + SpendingDrivers.Of(l.Category) + ")"] = l.Amount; }
                LastLines.Add(lines);
            }
            finally { Object.DestroyImmediate(go); }
            return rows;
        }
    }
}
