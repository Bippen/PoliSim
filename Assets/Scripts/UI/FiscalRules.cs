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
        /// limit for the country. PS-6 US-35 (board 30c, answer 4; "For Code" 7: the capitalised lines become the slip drawn in 30c): in sentence case - the
        /// limit and since when, where the debt stands, the state's own lines (the measures' end, the cause where it binds: a raise before Congress, on the
        /// President's desk, or fallen), then the term; the absent history is the caller's line.
        /// </summary>
        public static System.Collections.Generic.List<string> DebtLimitLines(Country country, System.DateTime today, BudgetBill pendingRaise)
        {
            var lines = new System.Collections.Generic.List<string>();
            if (!UsDebtLimit.IsRead(country)) { return lines; }
            double subject = UsDebtLimit.SubjectToLimitUsd(country);
            string subjectMoney = UiFormat.Money(Billions(subject), MoneyUnit.Billions);
            if (UsDebtLimit.Suspended(country, today))
            {
                lines.Add("Suspended through " + Day(country.DebtLimitSuspendedThrough) + ".");
                lines.Add("Debt subject to limit " + subjectMoney + ". No limit applies until then.");
                lines.Add("The day after, the limit returns at the debt subject to it that day.");
            }
            else
            {
                lines.Add("Limit " + UiFormat.Money(Billions(country.DebtLimitUsd), MoneyUnit.Billions) + ", in force since " + Day(country.DebtLimitSince) + ".");
                double below = country.DebtLimitUsd - subject;
                // the review's L3, and the second review's L1: while the measures run they hold the debt subject to the limit AT it - what the model has borrowed
                // since is the measures' room, named as such, with their days left; once they have run out the limit BINDS, and what stands above it is what the
                // measures carried to their last day and, since, the interest the cut does not cover - never "held by the measures"
                string above = UiFormat.Money(Billions(System.Math.Max(0d, -below)), MoneyUnit.Billions);
                if (country.DebtLimitReachedOn == System.DateTime.MinValue)
                {
                    lines.Add("Debt subject to it " + subjectMoney + ", " + UiFormat.Money(Billions(below), MoneyUnit.Billions) + " below.");
                    lines.Add("Once it is reached, extraordinary measures hold the debt there for " + DaysWords(UsDebtLimit.MeasuresSpanDays) + ".");
                }
                else if (!UsDebtLimit.Binding(country, today))
                {
                    lines.Add("Reached on " + Day(country.DebtLimitReachedOn) + ". Extraordinary measures hold the debt at the limit; " + above + " borrowed through them so far.");
                    lines.Add("They run out on " + Day(UsDebtLimit.BindsOn(country)) + ", in " + DaysWords(UsDebtLimit.MeasuresDaysLeft(country, today)) + ".");
                    lines.Add("Then a raise must be law, or spending other than interest is cut to revenue less interest.");
                }
                else
                {
                    lines.Add("Debt subject to it " + subjectMoney + ", " + above + " above.");
                    lines.Add("The extraordinary measures ran out on " + Day(UsDebtLimit.BindsOn(country)) + ".");
                }
            }
            bool binding = !UsDebtLimit.Suspended(country, today) && UsDebtLimit.Binding(country, today);
            // PS-6 K21-K23 (Elias's rulings of 2026-10-23): the bill's own act - a suspension to its day, a proposed raise by its size, the government's by the
            // standard size - every raise the day's excess and its size (K77); the cause where it binds (withheld, fallen, or no proposal of the President's)
            bool suspension = pendingRaise != null && pendingRaise.DebtLimitSuspendThrough != System.DateTime.MinValue;
            string raise = suspension ? "A suspension through " + Day(pendingRaise.DebtLimitSuspendThrough)
                : "A raise of the excess and " + UiFormat.Money(Billions(pendingRaise != null && pendingRaise.DebtLimitRaiseUsd > 0d ? pendingRaise.DebtLimitRaiseUsd : UsDebtLimit.RaiseUsd), MoneyUnit.Billions);
            bool playerPresident = !string.IsNullOrEmpty(country.PlayerPartyAbbrev) && country.Government != null && country.Government.RoleOf(country.PlayerPartyAbbrev) == PoliSim.Elections.PlayerRole.PrimeMinister;
            string withheld = country.DebtLimitWithheldBy == null ? null : "The raise is withheld by " + PartySystems.ShortName(country.Id, country.DebtLimitWithheldBy) + ": it votes against it in both chambers, and a chamber it holds stops it.";
            if (pendingRaise != null)
            {
                lines.Add(pendingRaise.Docket != null ? raise + " passed both chambers and waits on the President's desk: signed, it is law at once."
                    : raise + " is before Congress, voted on " + Day(today.AddDays(System.Math.Max(0, pendingRaise.DaysRemaining))) + " in both chambers by reconciliation, then to the President.");
                if (!suspension) { lines.Add("Law, its size is added to the debt's excess over the limit that day: it clears the excess and leaves that much room."); }
            }
            else if (binding)
            {
                lines.Add(playerPresident ? (country.DebtLimitLetBind ? "You let the limit bind: no raise is before Congress until you propose one, voted the day it is tabled."   // PS-6 K90
                        : "No raise is before Congress. Propose one, or a suspension, or let it bind: past the deadline a proposal is voted the day it is tabled.")
                    : "No raise is before Congress: the last fell, and another is tabled the next day while the limit binds.");
            }
            else if (playerPresident && country.DebtLimitReachedOn != System.DateTime.MinValue && !UsDebtLimit.Suspended(country, today))
            {
                lines.Add("No raise is before Congress. Propose one, or a suspension: it is voted on " + Day(UsDebtLimit.BindsOn(country)) + ", when the measures run out.");
            }
            if (withheld != null) { lines.Add(withheld); }
            if (withheld != null && country.DebtLimitWithheldBy != country.PlayerPartyAbbrev && !UsDebtLimit.Suspended(country, today) && country.DebtLimitReachedOn != System.DateTime.MinValue)
            {
                lines.Add("It is released on " + Day(UsDebtLimit.AiReleaseDay(country)) + ", " + DaysWords(UsDebtLimit.AiHoldDaysPastDeadline) + " after the measures run out: a raise voted before then falls, and the cut binds meanwhile.");   // PS-6 K78, K89
            }
            if (binding)
            {
                lines.Add("Until a raise is law, spending other than interest is cut to revenue less interest. Interest is always paid.");
                lines.Add("Since " + Day(UsDebtLimit.BindsOn(country)) + ", borrowing covers interest only" + (country.DebtLimitCutBillions > 0f ? "; " + UiFormat.Money(country.DebtLimitCutBillions, MoneyUnit.Billions) + " cut so far." : "."));
            }
            lines.Add("Debt subject to limit is Treasury's measure: the debt above × " + UiFormat.Number((float)UsDebtLimit.BridgeRatio, 3) + ", Treasury's figure over the game's opening debt in the same month.");
            return lines;
        }

        /// <summary>MF q33 (K141-K144): the debt crisis in sentences for the debt tile's slip - empty while the country has the market. UI minimal: Design's
        /// D-WORKLIST section 1 redraws it.</summary>
        public static System.Collections.Generic.List<string> CrisisLines(Country country, System.DateTime today)
        {
            var lines = new System.Collections.Generic.List<string>();
            SovereignCrisisState crisis = country?.Crisis;
            if (crisis == null || crisis.Phase == CrisisPhase.None) { return lines; }
            string since = Day(crisis.StartedOn);
            if (crisis.Phase == CrisisPhase.Default)
            {
                lines.Add("In default since " + since + ", when its debt rated CCC without the reserve currency's discount and the market closed.");
                lines.Add("A " + (SovereignCrisis.HaircutFraction * 100f).ToString("0", CultureInfo.InvariantCulture) + " % haircut wrote " + UiFormat.Money(crisis.HaircutBillions, MoneyUnit.Billions) + " off the debt.");
                lines.Add("Shut out of the market through " + Day(crisis.EndsOn) + ": spending other than interest is cut to revenue less interest.");
            }
            else
            {
                lines.Add("In a programme with " + SovereignCrisis.FacilityOf(country.Id) + " since " + since + ", when the rating reached CCC and the market closed. It runs through " + Day(crisis.EndsOn) + ".");
                lines.Add("This year's deficit target is " + SovereignCrisis.TargetFor(crisis.EntryDeficitPercent, crisis.ProgrammeYear).ToString("0.0", CultureInfo.InvariantCulture) + " % of GDP, reviewed on " + Day(crisis.NextReviewOn) + ".");
                lines.Add("The programme's loans carry no market premium.");   // the q32 review's L2: K214's pricing holds in a withheld year too
                if (SovereignCrisis.Unfinanced(country, today)) { lines.Add("A missed target withheld this year's loan through " + Day(crisis.WithheldThrough) + ": spending other than interest is cut to revenue less interest."); }
            }
            if (crisis.CutBillions > 0f) { lines.Add(UiFormat.Money(crisis.CutBillions, MoneyUnit.Billions) + (crisis.Crises > 1 ? " cut so far, across the run's crises." : " cut so far.")); }   // the review of wave/dcrisis, L9: the sum is the run's, so a second crisis says so
            return lines;
        }

        /// <summary>The debt tile's state's ground, PS-6 US-35 (board 30c, answer 4): amber a clock running, red the cut in force, the Neutral a limit set aside.</summary>
        public enum DebtLimitInk { Clock, Cut, SetAside }

        /// <summary>
        /// PS-6 US-35 (board 30c, answer 4): THE DEBT TILE'S STATE in a few words, for the chip under its name - null where the game holds no limit, or the debt is
        /// below it (then <paramref name="sub"/> is the muted line: the limit and the headroom). The states the model reaches: AT THE LIMIT with the measures'
        /// days (a clock running); RAISE BEFORE CONGRESS and RAISE ON YOUR DESK while the measures run (a clock); LIMIT BINDS · CUT IN FORCE and BINDS · RAISE ON
        /// YOUR DESK once they have run out (the cut in force); SUSPENDED TO a day (the limit set aside - the record's P.L. 118-5 at a start inside it, or the
        /// game's own suspension, K22). A withheld raise (K22) keeps the board's chip - RAISE BEFORE CONGRESS while the measures run, LIMIT BINDS · CUT IN FORCE
        /// once it has fallen - and the slip names the cause (`DebtLimitLines`).
        /// </summary>
        public static string DebtLimitChip(Country country, System.DateTime today, BudgetBill pendingRaise, out DebtLimitInk ink, out string sub)
        {
            ink = DebtLimitInk.Clock;
            sub = null;
            if (!UsDebtLimit.IsRead(country)) { return null; }
            if (UsDebtLimit.Suspended(country, today)) { ink = DebtLimitInk.SetAside; return "SUSPENDED TO " + Day(country.DebtLimitSuspendedThrough).ToUpperInvariant(); }
            if (country.DebtLimitReachedOn == System.DateTime.MinValue)
            {
                double below = country.DebtLimitUsd - UsDebtLimit.SubjectToLimitUsd(country);
                sub = "limit " + UiFormat.Money(Billions(country.DebtLimitUsd), MoneyUnit.Billions) + " · " + UiFormat.Money(Billions(below), MoneyUnit.Billions) + " below";
                return null;
            }
            bool onDesk = pendingRaise != null && pendingRaise.Docket != null;
            if (UsDebtLimit.Binding(country, today))
            {
                ink = DebtLimitInk.Cut;
                return onDesk ? "BINDS · RAISE ON YOUR DESK" : "LIMIT BINDS · CUT IN FORCE";
            }
            if (onDesk) { return "RAISE ON YOUR DESK"; }
            if (pendingRaise != null) { return "RAISE BEFORE CONGRESS"; }
            return "AT THE LIMIT · " + DaysWords(UsDebtLimit.MeasuresDaysLeft(country, today)).ToUpperInvariant();
        }

        private static float Billions(double usd) => (float)(usd / 1e9);
        private static string Day(System.DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        private static string DaysWords(int days) => days.ToString(CultureInfo.InvariantCulture) + (days == 1 ? " day" : " days");
    }
}
