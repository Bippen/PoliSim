using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PoliSim.Elections;

namespace PoliSim.Data
{
    /// <summary>
    /// The review's M4 (Reviews/2026-10-29_portfolioname_review.md): the names a player reads for the model's enums, one scoped lookup per enum - R4-4's
    /// rule, as <see cref="Effectiveness.DisplayName"/> is the portfolios': each overload is TYPED, so no lookup keyed on a member's spelling can cross
    /// from one enum to another that shares it (SpendingCategory.Education, CabinetPortfolio.Education and PolicyNodeId.Education are different things).
    /// The simulation's player-visible texts (a budget's description, a ledger line, a party's reasons on a vote, a formation's reason) read them here,
    /// as the UI does, so no raw identifier ("IncomeTax at 25.0 %", "Implement NegativeIncomeTax", "(NewElection)") reaches the player.
    ///
    /// <para><b>One set of names.</b> The tax, welfare and spending names are the curated ones the Policy Web drew first; they live here now, and
    /// <c>PolicyWebRenderer</c>'s node table reads them, so the UI's <c>DisplayName.Of</c> (which resolves a policy-space identifier through that table)
    /// and this lookup return the same words by construction - `EnumDisplayNameCheck` proves it every run. A spending line with no node of its own is
    /// spaced (<see cref="PoliSim.Wording.EnglishText.Spaced"/>, the UI's own spacer, moved beside the possessive for the same reason).</para>
    ///
    /// <para><b>A programme bill's division title is a KEY.</b> Poland's veto draw and the US chambers' veto draw are keyed on a division's title
    /// (`PresidentialVeto.Draw`, `UsVeto.Draw`; `UsLapseDraw.Draw` reads a title too, but only a continuing resolution reaches it), so the title a
    /// tax or welfare programme bill is voted under stays the identifier
    /// it always was (<see cref="ProgrammeBillKey(TaxType, bool)"/>) - renaming it would move every draw a player's programme bill meets - and a screen
    /// that prints a division's title reads it through <see cref="ShownTitle"/>, which puts the programme's name in the key's place: a KEY is ASCII
    /// while a NAME is the reader's, the rule `PoliticalParty.Abbrev` and `ShortName` follow.</para>
    /// </summary>
    public static class EnumNames
    {
        /// <summary>A tax's name as the Budget page and the Policy Web print it.</summary>
        public static string Of(TaxType type)
        {
            switch (type)
            {
                case TaxType.IncomeTax: return "Income Tax";
                case TaxType.CorporateTax: return "Corporate Tax";
                case TaxType.VAT: return "VAT";
                case TaxType.PayrollTax: return "Payroll Tax";
                case TaxType.CapitalGainsTax: return "Capital Gains Tax";
                case TaxType.SalesTax: return "Sales Tax";
                case TaxType.ExciseTax: return "Excise Tax";
                case TaxType.PropertyTax: return "Property Tax";
                case TaxType.EstateTax: return "Estate Tax";
                case TaxType.WealthTax: return "Wealth Tax";
                case TaxType.CarbonTax: return "Carbon Tax";
                case TaxType.Tariffs: return "Tariffs (Tax Line)";
                case TaxType.StampDuty: return "Stamp Duty";
                default: return PoliSim.Wording.EnglishText.Spaced(type.ToString());
            }
        }

        /// <summary>A welfare programme's name as the Welfare page and the Policy Web print it.</summary>
        public static string Of(WelfareProgramType program)
        {
            switch (program)
            {
                case WelfareProgramType.UBI: return "UBI";
                case WelfareProgramType.NegativeIncomeTax: return "Negative Income Tax";
                case WelfareProgramType.MeansTestedWelfare: return "Means-Tested Welfare";
                case WelfareProgramType.UniversalHealthcare: return "Universal Healthcare";
                case WelfareProgramType.HousingAssistance: return "Housing Assistance";
                case WelfareProgramType.ChildcareSubsidies: return "Childcare Subsidies";
                default: return PoliSim.Wording.EnglishText.Spaced(program.ToString());
            }
        }

        /// <summary>A spending line's name as the Budget page prints it: the Policy Web's curated name where the line has a node, the identifier spaced
        /// where it has none.</summary>
        public static string Of(SpendingCategory category)
        {
            switch (category)
            {
                case SpendingCategory.SocialSecurity: return "Social Security";
                case SpendingCategory.Medicare: return "Medicare";
                case SpendingCategory.Medicaid: return "Medicaid";
                case SpendingCategory.IncomeSecurity: return "Income Security";
                case SpendingCategory.VeteransBenefitsMandatory: return "Veterans Benefits (Mand.)";
                case SpendingCategory.FederalRetirement: return "Federal Retirement";
                case SpendingCategory.Defense: return "Defense";
                case SpendingCategory.Transportation: return "Transportation (Infra.)";
                case SpendingCategory.HHSDiscretionary: return "HHS (Healthcare)";
                case SpendingCategory.Education: return "Education";
                case SpendingCategory.Justice: return "Justice (Spending)";
                case SpendingCategory.HomelandSecurity: return "Homeland Security";
                case SpendingCategory.Energy: return "Energy (Spending)";
                case SpendingCategory.Housing: return "Housing (Spending)";
                default: return PoliSim.Wording.EnglishText.Spaced(category.ToString());
            }
        }

        /// <summary>A formation's outcome in words - the election night's (K284: one vocabulary across the nights), and for the two outcomes that
        /// form no government what the formation found: no admissible cabinet wins its investiture (<see cref="CoalitionFormation"/>'s empty set of
        /// viable governments), or the government fell.</summary>
        public static string Of(CoalitionOutcomeKind kind)
        {
            switch (kind)
            {
                case CoalitionOutcomeKind.MajorityCoalition: return "Majority government";
                case CoalitionOutcomeKind.ConfidenceAndSupply: return "Minority, with support";
                case CoalitionOutcomeKind.MinorityGovernment: return "Minority government";
                case CoalitionOutcomeKind.NewElection: return "No cabinet can win its investiture";
                case CoalitionOutcomeKind.Collapse: return "The government fell";
                default: return PoliSim.Wording.EnglishText.Spaced(kind.ToString());
            }
        }

        /// <summary>The title a tax programme bill is voted under - the division's title and so the draws' key (see the class's note): the identifier, as
        /// it always was. Never printed as it stands: a screen reads a division's title through <see cref="ShownTitle"/>.</summary>
        public static string ProgrammeBillKey(TaxType type, bool isAdd) => (isAdd ? "Implement " : "Remove ") + type.ToString();

        /// <summary>The title a welfare programme bill is voted under - the draws' key, as <see cref="ProgrammeBillKey(TaxType, bool)"/>.</summary>
        public static string ProgrammeBillKey(WelfareProgramType program, bool isAdd) => (isAdd ? "Implement " : "Remove ") + program.ToString();

        /// <summary>A tax programme bill as a player reads it: "Implement Negative Income Tax", "Remove Stamp Duty".</summary>
        public static string ProgrammeBillWords(TaxType type, bool isAdd) => (isAdd ? "Implement " : "Remove ") + Of(type);

        /// <summary>A welfare programme bill as a player reads it.</summary>
        public static string ProgrammeBillWords(WelfareProgramType program, bool isAdd) => (isAdd ? "Implement " : "Remove ") + Of(program);

        private static Regex _programmeKey;
        private static readonly Dictionary<string, string> ShownCache = new Dictionary<string, string>();

        /// <summary>A division's title as a screen prints it: every programme bill's key inside it ("Implement NegativeIncomeTax", also inside a veto's
        /// title that quotes it) in its words; any other title as it stands. Cached - the division lists resolve it once per row per frame.</summary>
        public static string ShownTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) { return title; }
            if (ShownCache.TryGetValue(title, out string shown)) { return shown; }
            if (_programmeKey == null)
            {
                var names = new List<string>(Enum.GetNames(typeof(TaxType)));
                names.AddRange(Enum.GetNames(typeof(WelfareProgramType)));
                names.Sort((a, b) => b.Length.CompareTo(a.Length));
                _programmeKey = new Regex(@"\b(Implement|Remove) (" + string.Join("|", names) + @")\b", RegexOptions.CultureInvariant);
            }
            shown = _programmeKey.Replace(title, m => m.Groups[1].Value + " " + ProgrammeName(m.Groups[2].Value));
            ShownCache[title] = shown;
            return shown;
        }

        private static string ProgrammeName(string identifier)
        {
            if (Enum.TryParse(identifier, out TaxType tax) && Enum.IsDefined(typeof(TaxType), tax)) { return Of(tax); }
            if (Enum.TryParse(identifier, out WelfareProgramType program) && Enum.IsDefined(typeof(WelfareProgramType), program)) { return Of(program); }
            return identifier;
        }
    }
}
