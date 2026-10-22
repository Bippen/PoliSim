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

        /// <summary>SOURCED - Sweden's debt anchor, % of GDP: "Skuldankaret är satt till 35 procent av BNP och gäller tills vidare" (Regeringens skrivelse
        /// 2025/26:76, <c>ElectionsData/rules/sweden_fiscal_framework.md</c>).</summary>
        public const float SwedenDebtAnchorPercent = 35f;
        /// <summary>SOURCED - the deviation that obliges an explanation, points of GDP: "Om skuldkvoten avviker, uppåt eller nedåt, från skuldankaret med mer
        /// än 5 procent av BNP ..." (skr. 2025/26:76).</summary>
        public const float SwedenDebtAnchorBandPoints = 5f;

        /// <summary>
        /// §758 (Elias's ruling B6, 2026-10-02: "Yes, as a notice, not a breach ... Neutral ink, per A6"): a NOTICE the reading's level triggers under
        /// the country's own framework without breaching a rule - Sweden's debt more than <see cref="SwedenDebtAnchorBandPoints"/> points of GDP from its
        /// anchor, up or down: the government must explain the deviation to the Riksdag in a skrivelse with the spring fiscal policy bill (skr. 2025/26:76).
        /// The ink stays neutral; the words ride the slip. Read here on the year's level (the skrivelse reads the previous year's outcome or the current or
        /// next year's forecast - the model holds the level, stated). Null where none applies.
        /// </summary>
        public static string Notice(CountryId country, Measure measure, float percentOfGdp)
        {
            if (country != CountryId.Sweden || measure != Measure.Debt || !(System.Math.Abs(percentOfGdp - SwedenDebtAnchorPercent) > SwedenDebtAnchorBandPoints)) { return null; }
            return "More than " + SwedenDebtAnchorBandPoints.ToString("0", CultureInfo.InvariantCulture) + " points of GDP " + (percentOfGdp > SwedenDebtAnchorPercent ? "above" : "below")
                + " Sweden's " + SwedenDebtAnchorPercent.ToString("0", CultureInfo.InvariantCulture) + " % debt anchor - a notice, not a breach: the government explains why to the Riksdag with the spring fiscal policy bill (skr. 2025/26:76)";
        }

        /// <summary>The country's own national rule in one line, and why the model does not test it - the slip's second line.</summary>
        public static string NationalNote(CountryId country)
        {
            switch (country)
            {
                case CountryId.Sweden: return "Sweden's net-lending target is an average over a business cycle - a third of a per cent of GDP, balance (0 %) from 2027 (rskr. 2025/26:64) - and its 35 % debt anchor a benchmark (skr. 2025/26:76) - neither is a yearly limit";
                case CountryId.Germany: return "Germany's debt brake (Basic Law Art. 109, 115) limits the structural federal balance, which the model does not compute";
                case CountryId.France: return "France has no binding national numeric limit (Constitution Art. 34, an objective of balance)";
                case CountryId.Italy: return "Italy's balanced-budget rule (Constitution Art. 81) is the structural medium-term objective, which the model does not compute";
                case CountryId.Poland: return "Poland's 3/5 ceiling (Constitution Art. 216) and 55 % threshold measure national-definition debt, not the EU figure shown";
                case CountryId.USA: return "No statutory ratio rule: the federal debt limit is a dollar ceiling (31 U.S.C. 3101(b)) on a different measure of debt";
                default: return null;
            }
        }

        /// <summary>
        /// PS-6 US-24 (the plan: "`FiscalRules`' US note made live (the limit, the debt subject to it, the measures' days left)"): the national rule's line read on
        /// the day - for the USA, once the game holds its limit (`UsDebtLimit.IsRead`), the limit in force, the debt subject to it and where the episode stands;
        /// every other country's, and a US game before its first day, the fixed line (<see cref="NationalNote(CountryId)"/>).
        /// </summary>
        public static string NationalNote(Country country, System.DateTime today)
        {
            if (country == null) { return null; }
            if (!UsDebtLimit.IsRead(country)) { return NationalNote(country.Id); }
            string debt = country.DebtLimitReachedOn != System.DateTime.MinValue && !UsDebtLimit.Suspended(country, today) ? "debt subject to it held at the limit"   // the review's L3: the measures hold it there
                : "debt subject to it " + UiFormat.Money(Billions(UsDebtLimit.SubjectToLimitUsd(country)), MoneyUnit.Billions);
            string law = UsDebtLimit.Suspended(country, today)
                ? "the federal debt limit (31 U.S.C. 3101(b)), a dollar ceiling, is suspended through " + Day(country.DebtLimitSuspendedThrough) + " - " + debt
                : "the federal debt limit (31 U.S.C. 3101(b)) is a dollar ceiling of " + UiFormat.Money(Billions(country.DebtLimitUsd), MoneyUnit.Billions) + " - " + debt;
            int left = UsDebtLimit.MeasuresDaysLeft(country, today);
            string episode = left < 0 ? string.Empty
                : UsDebtLimit.Binding(country, today) ? "; the extraordinary measures have run out, and spending is cut to revenue until a raise is law"
                : "; at the limit, extraordinary measures last " + DaysWords(left) + " more";
            return "No statutory ratio rule: " + law + episode;
        }

        /// <summary>
        /// PS-6 US-24: THE DEBT LIMIT'S LINES for the debt tile's slip - the law in force, the debt subject to it against the limit, the measures' days left or the
        /// cut in force, and a raise before Congress (<paramref name="pendingRaise"/>, `SimulationManager.GetPendingDebtLimitBill`). Empty where the game holds no
        /// limit for the country.
        /// </summary>
        public static System.Collections.Generic.List<string> DebtLimitLines(Country country, System.DateTime today, BudgetBill pendingRaise)
        {
            var lines = new System.Collections.Generic.List<string>();
            if (!UsDebtLimit.IsRead(country)) { return lines; }
            double subject = UsDebtLimit.SubjectToLimitUsd(country);
            string subjectWords = "DEBT SUBJECT TO THE LIMIT " + UiFormat.Money(Billions(subject), MoneyUnit.Billions);
            if (UsDebtLimit.Suspended(country, today))
            {
                lines.Add("THE FEDERAL DEBT LIMIT IS SUSPENDED THROUGH " + Day(country.DebtLimitSuspendedThrough) + " · REINSTATED THE NEXT DAY AT THE DEBT SUBJECT TO IT THEN");
                lines.Add(subjectWords);
            }
            else
            {
                lines.Add("THE FEDERAL DEBT LIMIT " + UiFormat.Money(Billions(country.DebtLimitUsd), MoneyUnit.Billions) + " · IN FORCE SINCE " + Day(country.DebtLimitSince));
                double below = country.DebtLimitUsd - subject;
                // the review's L3, and the second review's L1: while the measures run they hold the debt subject to the limit AT it - what the model has borrowed
                // since is the measures' room, named as such, with their days left; once they have run out the limit BINDS, and what stands above it is what the
                // measures carried to their last day and, since, the interest the cut does not cover - never "held by the measures"
                string above = UiFormat.Money(Billions(System.Math.Max(0d, -below)), MoneyUnit.Billions);
                if (country.DebtLimitReachedOn == System.DateTime.MinValue) { lines.Add(subjectWords + " · " + UiFormat.Money(Billions(below), MoneyUnit.Billions) + " BELOW IT"); }
                else if (!UsDebtLimit.Binding(country, today))
                {
                    lines.Add("DEBT SUBJECT TO THE LIMIT HELD AT IT BY EXTRAORDINARY MEASURES SINCE " + Day(country.DebtLimitReachedOn) + " · " + above + " BORROWED THROUGH THEM · "
                        + DaysWords(UsDebtLimit.MeasuresDaysLeft(country, today)).ToUpperInvariant() + " LEFT, TO " + Day(UsDebtLimit.BindsOn(country)));
                }
                else
                {
                    lines.Add("DEBT SUBJECT TO THE LIMIT " + above + " ABOVE IT · BORROWED THROUGH THE MEASURES TO " + Day(UsDebtLimit.BindsOn(country))
                        + " AND, SINCE, ONLY FOR INTEREST THE CUT DOES NOT COVER - INTEREST IS ALWAYS PAID");
                }
            }
            if (!UsDebtLimit.Suspended(country, today) && UsDebtLimit.Binding(country, today))
            {
                lines.Add("THE EXTRAORDINARY MEASURES RAN OUT ON " + Day(UsDebtLimit.BindsOn(country)) + " · UNTIL A RAISE IS LAW, SPENDING OTHER THAN INTEREST IS CUT TO WHAT REVENUE LEAVES AFTER INTEREST"
                    + (country.DebtLimitCutBillions > 0f ? " · " + UiFormat.Money(country.DebtLimitCutBillions, MoneyUnit.Billions) + " CUT SO FAR" : string.Empty));
            }
            if (pendingRaise != null)
            {
                string raise = "A RAISE OF " + UiFormat.Money(Billions(UsDebtLimit.RaiseUsd), MoneyUnit.Billions);
                lines.Add(pendingRaise.Docket != null ? raise + " HAS PASSED CONGRESS AND IS ON THE PRESIDENT'S DESK"
                    : raise + " IS BEFORE CONGRESS · VOTED " + Day(today.AddDays(System.Math.Max(0, pendingRaise.DaysRemaining))) + " BY A MAJORITY IN BOTH CHAMBERS, THEN TO THE PRESIDENT");
            }
            lines.Add("DEBT SUBJECT TO LIMIT IS TREASURY'S MEASURE: THE DEBT ABOVE TIMES " + UiFormat.Number((float)UsDebtLimit.BridgeRatio, 3) + " - TREASURY'S DEBT SUBJECT TO LIMIT OVER THE GAME'S OPENING DEBT, IN THE SAME MONTH");
            return lines;
        }

        private static float Billions(double usd) => (float)(usd / 1e9);
        private static string Day(System.DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
        private static string DaysWords(int days) => days.ToString(CultureInfo.InvariantCulture) + (days == 1 ? " day" : " days");
    }
}
