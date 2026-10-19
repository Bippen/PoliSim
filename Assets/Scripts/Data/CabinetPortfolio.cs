namespace PoliSim.Data
{
    /// <summary>
    /// Political Systems Overhaul Part A (Cabinet), Master Sequence step 1. The confirmed FULL scope
    /// is six generic portfolios (Finance/Treasury, Foreign Affairs, Defense, Interior/Justice,
    /// Health &amp; Social Affairs, Economy/Trade &amp; Industry - see COMPLETED.md §2, Part A's
    /// record), but per the original plan's content-authoring warning ("6-8 roles x 2-3 candidates x
    /// multiple decisions each is a real content burden - build 2-3 portfolios with real,
    /// fully-realized content first, mirroring the Sectors 4-&gt;8 pattern, prove the pattern feels
    /// right, then expand"), Part A implemented only the first three - chosen because each lands on a
    /// well-understood, already-audited existing channel (Fiscal Reaction Function/
    /// CollectionEfficiency, Crime &amp; Justice's gap-based target formulas, Welfare/PovertyRate's
    /// reduction terms).
    ///
    /// ROUND 4 BATCH R4-4 (2026-08-17) added Defense, ForeignAffairs and Education - the pattern
    /// proven, the expansion authored per the signed pre-report (consumed to COMPLETED.md §19 on
    /// 2026-08-27 when the portraits landed; docs/archive/CLAUDE_LESSONS_2026-08.md "Round 4 batch R4-4") and its seven recorded
    /// rulings. Economy/Trade &amp; Industry stayed NOT defined until AF-6 (q6, 2026-10-18) added it for the departments none of the six fits -
    /// SectorType's own history (members added only when actually built, never as unused
    /// placeholders) is still the convention. ⚠ Passive competence is intentionally ASYMMETRIC
    /// across the six (ruling R3): Education lands on the youth-unemployment reversion target
    /// (MacroSystem.ApplyYouthUnemployment); Defense and ForeignAffairs are DECISIONS-ONLY this
    /// pass - Defense because no audited channel exists to land on (no military state, and the
    /// G-term is fiscal-engine contact the F1 sequencing keeps closed), ForeignAffairs because its
    /// candidates (TradeSystem dampening, CurrencyStrength) would need rule-11 ceiling audits the
    /// batch doesn't otherwise require. Their ministers' CompetenceBias values are authored but
    /// inert - see the CandidatePool comments in CabinetSystem.
    /// </summary>
    public enum CabinetPortfolio
    {
        FinanceTreasury,
        InteriorJustice,
        HealthSocialAffairs,

        // ROUND 4 BATCH R4-4: appended after the shipped three - append-only growth keeps every
        // serialized appointment/pending-decision in a pre-R4-4 save loading unchanged.
        Defense,
        ForeignAffairs,
        Education,

        // AF-6 (q6, ruled 2026-10-18: "add an economy portfolio only where none fits"): appended - the departments of
        // the catalog that none of the six fits (Departments.PortfolioOf) are its. No candidate pool is authored for it
        // (Rule 9's names are Elias's to sign): its chair starts with the average minister (AF-30b) and a search draws none.
        Economy
    }

    /// <summary>
    /// AF-6 (q6): the catalog's EIGHT DEPARTMENTS (`docs/data/SOCIETY_STATS.md` §6, Elias's metric list as `COMPLETED.md` §369a lifts it:
    /// "Education, Security, Health, Agriculture, State, Transportation, Energy, Science &amp; Nature"), in the list's order.
    /// </summary>
    public enum Department
    {
        Education,
        Security,
        Health,
        Agriculture,
        State,
        Transportation,
        Energy,
        ScienceAndNature
    }

    /// <summary>
    /// AF-6 (q6, ruled 2026-10-18): "one generated table maps each of the eight departments to a portfolio; add an economy portfolio only where
    /// none fits". This is the table; <c>docs/generated/DEPARTMENT_PORTFOLIOS.md</c> is its rendering (<see cref="RenderTable"/>), written by
    /// <c>DepartmentPortfolioCheck.Write</c> and held by <c>DepartmentPortfolioCheck</c>, which fails while the file differs from it. DECLARED
    /// readings, each its row's <see cref="Basis"/>: Security is national security (Defense - the list carries no Defense or Justice department
    /// of its own); Science &amp; Nature rides Education, the pairing Druckman &amp; Warwick's survey rates as one post in Germany and Sweden.
    /// </summary>
    public static class Departments
    {
        public static CabinetPortfolio PortfolioOf(Department department)
        {
            switch (department)
            {
                case Department.Education: return CabinetPortfolio.Education;
                case Department.Security: return CabinetPortfolio.Defense;
                case Department.Health: return CabinetPortfolio.HealthSocialAffairs;
                case Department.State: return CabinetPortfolio.ForeignAffairs;
                case Department.ScienceAndNature: return CabinetPortfolio.Education;
                default: return CabinetPortfolio.Economy;   // Agriculture, Transportation, Energy: none of the six fits
            }
        }

        public static string Name(Department department) => department == Department.ScienceAndNature ? "Science & Nature" : department.ToString();

        /// <summary>Why the row maps where it does - the one place the reading is stated.</summary>
        public static string Basis(Department department)
        {
            switch (department)
            {
                case Department.Education: return "the same subject";
                case Department.Security: return "DECLARED: national security - the list names no Defense or Justice department, and the Military is its own category there; Interior & Justice is the alternative";
                case Department.Health: return "the same subject (Health & Social Affairs)";
                case Department.State: return "the US name for the foreign ministry";
                case Department.ScienceAndNature: return "DECLARED: science rides the education post, as Druckman & Warwick rate it in Germany (Science & Education) and Sweden (Education & Science); nature has no post of the six";
                default: return "none of the six fits - the economy portfolio, added for it (q6)";
            }
        }

        /// <summary>The departments a portfolio holds, in the list's order - the cabinet card's slip reads it.</summary>
        public static System.Collections.Generic.List<Department> Of(CabinetPortfolio portfolio)
        {
            var held = new System.Collections.Generic.List<Department>();
            foreach (Department d in (Department[])System.Enum.GetValues(typeof(Department))) { if (PortfolioOf(d) == portfolio) { held.Add(d); } }
            return held;
        }

        /// <summary>The generated table, LF line ends: the department, its portfolio, the basis; then each portfolio with the departments it holds.</summary>
        public static string RenderTable()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("| department | portfolio | basis |\n|---|---|---|\n");
            foreach (Department d in (Department[])System.Enum.GetValues(typeof(Department)))
            {
                sb.Append("| ").Append(Name(d)).Append(" | ").Append(PortfolioOf(d)).Append(" | ").Append(Basis(d)).Append(" |\n");
            }
            sb.Append("\n| portfolio | departments |\n|---|---|\n");
            foreach (CabinetPortfolio p in (CabinetPortfolio[])System.Enum.GetValues(typeof(CabinetPortfolio)))
            {
                var names = new System.Collections.Generic.List<string>();
                foreach (Department d in Of(p)) { names.Add(Name(d)); }
                sb.Append("| ").Append(p).Append(" | ").Append(names.Count > 0 ? string.Join(", ", names) : "none of the eight").Append(" |\n");
            }
            return sb.ToString();
        }
    }
}
