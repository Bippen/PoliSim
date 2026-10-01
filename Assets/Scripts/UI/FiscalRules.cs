using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;

namespace PoliSim.UI
{
    /// <summary>
    /// §725 (Elias's ruling, 2026-10-01, relayed with UI v3.5): GOVERNMENT DEBT AND THE BALANCE ARE NEUTRAL INK BY DEFAULT. They turn to the warning
    /// state only when they breach the country's own statutory fiscal rule - the EU's 3 % and 60 % for the member states, and each country's national
    /// rule where it has one, sourced - with the slip naming the rule breached. This class is that test and its words; the inks are the UI's (V35).
    ///
    /// <para><b>What is tested</b> (sourced in <c>ElectionsData/rules/fiscal_rules.md</c>, every page kept byte-exact): the Treaty's reference values for
    /// the five Member States - a deficit above 3 % of GDP, gross debt above 60 % (TFEU Art. 126(2); Protocol No 12 Art. 1) - the constants the AI
    /// finance ministry already applies (<see cref="AiFinanceMinistry.EuDeficitReferencePercent"/>, <see cref="AiFinanceMinistry.EuDebtReferencePercent"/>,
    /// <see cref="AiFinanceMinistry.IsEuMember"/>), so the desk and the ministry read one rule. Both ratios are nominal over nominal GDP (P5-B6).</para>
    ///
    /// <para><b>What is not, and why</b> - each national rule is sourced and none is a figure the model computes (<see cref="NationalNote"/>): Sweden's
    /// net-lending target is a business-cycle average and its debt anchor a benchmark; Germany's brake binds the structural federal balance; France has
    /// no binding numeric limit; Italy's balance is the structural medium-term objective; Poland's 55 % and 60 % are on the national debt definition,
    /// not the EU-comparable one the model holds. The United States has no ratio rule - its debt limit is a dollar ceiling on debt subject to limit,
    /// a different measure - so its readings stay neutral.</para>
    ///
    /// <para><b>A display rule, so it lives in UI.</b> It decides an ink and the slip's words and nothing in the simulation reads it; the money it judges
    /// is computed elsewhere (the fiscal report, the state's debt ratio) and this class moves none of it.</para>
    /// </summary>
    public static class FiscalRules
    {
        public enum Measure { Deficit, Debt }

        /// <summary>Whether <paramref name="percentOfGdp"/> - a deficit as a positive share of GDP, or gross debt - breaches the country's statutory
        /// rule; <paramref name="rule"/> names it for the slip, null when nothing is breached.</summary>
        public static bool Breaches(CountryId country, Measure measure, float percentOfGdp, out string rule)
        {
            rule = null;
            if (!AiFinanceMinistry.IsEuMember(country)) { return false; }
            float limit = measure == Measure.Deficit ? AiFinanceMinistry.EuDeficitReferencePercent : AiFinanceMinistry.EuDebtReferencePercent;
            if (!(percentOfGdp > limit)) { return false; }
            rule = "Above the EU's " + limit.ToString("0", CultureInfo.InvariantCulture) + " % " + (measure == Measure.Deficit ? "deficit" : "debt")
                + " reference value (TFEU Art. 126(2), Protocol No 12)";
            return true;
        }

        /// <summary>The breached rule in a caption's few words (<c>over the EU's 3 % limit</c>), for a row that has no slip until its screen's v3.5 pass.</summary>
        public static string Short(Measure measure) =>
            "over the EU's " + (measure == Measure.Deficit ? AiFinanceMinistry.EuDeficitReferencePercent : AiFinanceMinistry.EuDebtReferencePercent).ToString("0", CultureInfo.InvariantCulture) + " % limit";

        /// <summary>The country's own national rule in one line, and why the model does not test it - the slip's second line.</summary>
        public static string NationalNote(CountryId country)
        {
            switch (country)
            {
                case CountryId.Sweden: return "Sweden's net-lending target is an average over a business cycle and its 35 % debt anchor a benchmark - neither is a yearly limit";
                case CountryId.Germany: return "Germany's debt brake (Basic Law Art. 109, 115) limits the structural federal balance, which the model does not compute";
                case CountryId.France: return "France has no binding national numeric limit (Constitution Art. 34, an objective of balance)";
                case CountryId.Italy: return "Italy's balanced-budget rule (Constitution Art. 81) is the structural medium-term objective, which the model does not compute";
                case CountryId.Poland: return "Poland's 3/5 ceiling (Constitution Art. 216) and 55 % threshold measure national-definition debt, not the EU figure shown";
                case CountryId.USA: return "No statutory ratio rule: the federal debt limit is a dollar ceiling (31 U.S.C. 3101(b)) on a different measure of debt";
                default: return null;
            }
        }
    }
}
