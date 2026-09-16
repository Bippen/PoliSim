using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// D-16 (a), EXECUTED 2026-09-04 (`COMPLETED.md` §282; ruled by Elias 2026-09-01 at F-B, taken at D-16 as an
    /// R-N1 decide-and-log): **the per-country tax-base table for the five, and the reason it is not for six.**
    ///
    /// <para><b>What a row is.</b> The share of GDP a country's tax base represents for one instrument, so that
    /// `GDP × rate × base` is that instrument's revenue. SOURCED, then DERIVED: base = (realised revenue, % of GDP) /
    /// (this project's seeded rate, %). Revenue from OECD Revenue Statistics
    /// (`OECD.CTP.TPS,DSD_REV_COMP_OECD@DF_RSOECD`, general government, % of GDP, 2022): income `T_1110`, corporate
    /// `T_1210`, VAT `T_5111`, payroll `T_2000+T_3000`; ⚠ Poland's income and corporate rows from Eurostat
    /// `gov_10a_taxag` D51A/D51B, the OECD flow carrying no income rows for Poland in any of 2020–2023. The fetch and
    /// the arithmetic are recorded in `COMPLETED.md` §197's D-9 sheet (2026-09-01); the values here are that sheet's,
    /// `[VERIFIED]` revenue over `[AUTHORED-DRAFT]` seeded rates. The seeded rates are `WorldFactory.SeedTaxLines`'s
    /// and the table is only true against them - a re-seeded rate re-derives its row.</para>
    ///
    /// <para>⚠ <b>Why the base already contains the collection loss, and what that did to
    /// <c>CollectionEfficiency</c>.</b> Realised revenue is what was collected, so a base derived from it is a base
    /// AFTER evasion, exemptions and enforcement. `CollectionEfficiency` used to mark theoretical revenue down for
    /// exactly those things; on this basis that would apply one correction twice (F-A, `COMPLETED.md` §147, measured
    /// by `CollectionEfficiencyBasisDiagnostic`). D-16 (a) re-solves the constant for the five as `Target / Implied`
    /// on THIS basis and re-documents it as the COVERAGE BRIDGE between the four modelled instruments and a whole tax
    /// system - a value above 1 means the four under-cover that country's real receipts, and is named as coverage,
    /// never as efficiency. The anchored quantity (the year-1 primary balance) is preserved exactly by construction;
    /// what moves is the RESPONSE family - a point of income tax now costs each country its own base's worth.</para>
    ///
    /// <para>⚠ <b>F4-5 (2026-09-16) puts the USA's INCOME row in, on the federal series, and leaves its other three out.</b> The paragraph below is D-16's
    /// and stands for corporate, VAT and payroll: their sourced figures are general government and the USA's calibration is federal, so they keep the uniform
    /// stand-in. The income row is different because the same OECD flow publishes it at the FEDERAL level (`S1311`, 9.300111 % of GDP in 2022 against 11.384784
    /// at general government), which is the perimeter this project calibrates on - the exclusion's reason does not reach it. The USA's `CollectionEfficiency`
    /// is re-solved for the change, and the income line it prices is the one the lever moves.</para>
    ///
    /// <para>⚠ <b>The USA is EXCLUDED, with F-B's reason (Elias, 2026-09-01: keep the federal perimeter).</b> The
    /// sourced bases are GENERAL GOVERNMENT for all six; the USA's whole calibration is FEDERAL by `WorldFactory`'s
    /// perimeter rule, because the state and local layer is not modelled. A general-government base under a federal
    /// target is a perimeter error no constant can absorb (the mismatch measured ×1.372), so the USA keeps the
    /// uniform stand-in bases of <see cref="TaxTypeBaseShares"/> and its unchanged CE, and says so at the call site
    /// (<see cref="BaseShareOfGdp"/>). Consistency inside a country outranks uniformity across the set.</para>
    ///
    /// <para><b>Coverage.</b> The four sourced instruments per country; every other implemented instrument (capital
    /// gains, carbon, estate, sales...) and every country not in the table falls back to the uniform stand-in, which
    /// is what `TaxLine.BaseShareOfGdp` has always been. ONE ACCESSOR, READ BY EVERY REVENUE SITE - since P5-B3 (2026-09-05) that
    /// accessor is <see cref="TaxBases.Base"/> / <see cref="TaxBases.Revenue"/>, which reads THIS table for the share at the
    /// seed and carries it forward by the base's own driver (the wage bill, consumption, housing, output); the turn's
    /// revenue, the household burden term, the Budget's estimates, the Policy Web's caption and the diagnostics all go
    /// through it, so no site can quietly stay on a fixed share of GDP or on the uniform base for a sourced pair.</para>
    /// </summary>
    public static class TaxBaseTable
    {
        /// <summary>The sourced bases, keyed "CountryId/TaxType". [VERIFIED] revenue (OECD/Eurostat, 2022) over the seeded rate; see the class doc.</summary>
        private static readonly Dictionary<string, float> Sourced = new Dictionary<string, float>
        {
            // F4-5 (2026-09-16): the four bracketed schedules' income rows are re-derived for the lever's new level - the schedule's own average rate on the
            // income it taxes, not its statutory top rate - so each row is still that country's realised revenue over the rate the row is read against.
            { "Germany/IncomeTax", 0.387336f }, { "Germany/CorporateTax", 0.0772f }, { "Germany/VAT", 0.3860f }, { "Germany/PayrollTax", 0.3673f },
            { "France/IncomeTax", 0.557394f },  { "France/CorporateTax", 0.1139f },  { "France/VAT", 0.3745f },  { "France/PayrollTax", 0.2469f },
            { "Italy/IncomeTax", 0.2491f },     { "Italy/CorporateTax", 0.1106f },   { "Italy/VAT", 0.3151f },   { "Italy/PayrollTax", 0.4257f },
            { "Poland/IncomeTax", 0.363939f },  { "Poland/CorporateTax", 0.1474f },  { "Poland/VAT", 0.3132f },  { "Poland/PayrollTax", 0.3775f },
            { "Sweden/IncomeTax", 0.3209f },    { "Sweden/CorporateTax", 0.1675f },  { "Sweden/VAT", 0.3798f },  { "Sweden/PayrollTax", 0.4488f },
            // THE USA's INCOME ROW ONLY, on the FEDERAL perimeter (F4-5): its other three keep the uniform stand-in, as F-B ruled.
            { "USA/IncomeTax", 0.543803f },
        };

        /// <summary>
        /// F4-3 (2026-09-15, §514): the realised revenue Sweden's income row is derived from - taxes on the income and profits of individuals (`T_1110`), general
        /// government (`S13`), % of GDP, 2022: 10.389453, re-fetched from the OECD's Revenue Statistics (`DSD_REV_COMP_OECD`, version 2.0, key
        /// `SWE.TAX_REV.S13.T_1110._T.PT_B1GQ.A`) when the blended 52 was retired - the same figure the D-9 sheet's 0.1998 was (10.389453 ÷ 52 = 0.1998), now
        /// over the municipal layer's 32.38 (= 0.3209). The same fetch books the layers apart - local government (`S1313`) 14.656972 % of GDP, central
        /// government (`S1311`) −4.267519 %, the state layer net of the credits it pays - so the one revenue figure the line reports is the sum, as S5 holds.
        /// </summary>
        public const double SwedenIncomeTaxRevenuePctOfGdp = 10.389453;

        /// <summary>
        /// F4-5 (2026-09-16): the realised revenue each income row is derived from, % of GDP, 2022 - that figure over the rate the row is read against IS the
        /// row above, so a re-seeded rate re-derives its row and this table says from what. Every figure re-fetched on the day, not recalled:
        /// <list type="bullet">
        /// <item>Germany <b>10.425679</b> and France <b>9.693475</b> - OECD Revenue Statistics (`DSD_REV_COMP_OECD` 2.0), taxes on the income and profits of
        /// individuals (`T_1110`), general government (`S13`), keys `DEU.TAX_REV.S13.T_1110._T.PT_B1GQ.A` and `FRA.TAX_REV.S13.T_1110._T.PT_B1GQ.A`. Both
        /// reproduce the D-9 sheet's own rows over the old top rates (45 × 0.2317 = 10.4265; 45 × 0.2154 = 9.693), so the source confirms the table it re-derives.</item>
        /// <item>Poland <b>4.5</b> - Eurostat `gov_10a_taxag`, D51A, general government, % of GDP, 2022 (the OECD flow carries no income rows for Poland in any
        /// of 2020-2023, which is D-16's own note); it reproduces 32 × 0.1406 = 4.4992.</item>
        /// <item>The USA <b>9.300111</b> - the same OECD flow at the FEDERAL level (`USA.TAX_REV.S1311.T_1110._T.PT_B1GQ.A`). This is the row D-16 (a) could not
        /// take: it excluded the USA because the sourced bases were general government (the USA reads 11.384784 there) while this project calibrates the USA on the
        /// FEDERAL perimeter. The federal series is that perimeter, so the reason for the exclusion does not hold for this one row, and F-B's rule - consistency
        /// inside a country outranks uniformity across the set - is what puts it in. Its other three instruments stay on the uniform stand-in.</item>
        /// </list>
        /// </summary>
        public static readonly System.Collections.Generic.IReadOnlyDictionary<CountryId, double> IncomeTaxRevenuePctOfGdp = new Dictionary<CountryId, double>
        {
            { CountryId.Germany, 10.425679 }, { CountryId.France, 9.693475 }, { CountryId.Poland, 4.5 },
            { CountryId.USA, 9.300111 }, { CountryId.Sweden, SwedenIncomeTaxRevenuePctOfGdp },
        };

        /// <summary>True for a country the table covers WHOLE - the five on the general-government perimeter. False for the USA, whose corporate, VAT and payroll
        /// rows are still the uniform stand-in (F-B: the federal perimeter), even though F4-5 sources its INCOME row federally - ask <see cref="HasSourcedBase"/>
        /// for one instrument. False for any country seeded later without a row.</summary>
        public static bool IsSourced(CountryId country) => country != CountryId.USA && Sourced.ContainsKey(country + "/IncomeTax");

        /// <summary>True when this country and instrument have a sourced row.</summary>
        public static bool HasSourcedBase(CountryId country, TaxType type) => Sourced.ContainsKey(country + "/" + type);

        /// <summary>
        /// The base a revenue site multiplies `GDP × rate` by: the sourced share for a sourced pair, otherwise
        /// the uniform stand-in for that instrument (<see cref="TaxTypeBaseShares.GetBaseShareOfGdp"/>).
        /// ⚠ The USA is served the stand-in on purpose for corporate, VAT and payroll - F-B's perimeter reason in the
        /// class doc - and not by an absence in the table; its INCOME row is in the table on the FEDERAL series (F4-5),
        /// which is the perimeter that reason defends, so the table answers for it.
        /// </summary>
        public static float BaseShareOfGdp(CountryId country, TaxType type)
        {
            if ((country != CountryId.USA || type == TaxType.IncomeTax) && Sourced.TryGetValue(country + "/" + type, out float share))
            {
                return share;
            }
            return TaxTypeBaseShares.GetBaseShareOfGdp(type);
        }
    }
}
