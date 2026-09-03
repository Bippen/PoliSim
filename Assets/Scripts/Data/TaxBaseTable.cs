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
    /// <para>⚠ <b>The USA is EXCLUDED, with F-B's reason (Elias, 2026-09-01: keep the federal perimeter).</b> The
    /// sourced bases are GENERAL GOVERNMENT for all six; the USA's whole calibration is FEDERAL by `WorldFactory`'s
    /// perimeter rule, because the state and local layer is not modelled. A general-government base under a federal
    /// target is a perimeter error no constant can absorb (the mismatch measured ×1.372), so the USA keeps the
    /// uniform stand-in bases of <see cref="TaxTypeBaseShares"/> and its unchanged CE, and says so at the call site
    /// (<see cref="BaseShareOfGdp"/>). Consistency inside a country outranks uniformity across the set.</para>
    ///
    /// <para><b>Coverage.</b> The four sourced instruments per country; every other implemented instrument (capital
    /// gains, carbon, estate, sales...) and every country not in the table falls back to the uniform stand-in, which
    /// is what `TaxLine.BaseShareOfGdp` has always been. ONE ACCESSOR, READ BY EVERY REVENUE SITE: the turn's
    /// revenue, the household burden term, the Budget's estimates, the Policy Web's caption and the diagnostics all
    /// call <see cref="BaseShareOfGdp"/>, so no site can quietly stay on the uniform base for a sourced pair.</para>
    /// </summary>
    public static class TaxBaseTable
    {
        /// <summary>The sourced bases, keyed "CountryId/TaxType". [VERIFIED] revenue (OECD/Eurostat, 2022) over the seeded rate; see the class doc.</summary>
        private static readonly Dictionary<string, float> Sourced = new Dictionary<string, float>
        {
            { "Germany/IncomeTax", 0.2317f }, { "Germany/CorporateTax", 0.0772f }, { "Germany/VAT", 0.3860f }, { "Germany/PayrollTax", 0.3673f },
            { "France/IncomeTax", 0.2154f },  { "France/CorporateTax", 0.1139f },  { "France/VAT", 0.3745f },  { "France/PayrollTax", 0.2469f },
            { "Italy/IncomeTax", 0.2491f },   { "Italy/CorporateTax", 0.1106f },   { "Italy/VAT", 0.3151f },   { "Italy/PayrollTax", 0.4257f },
            { "Poland/IncomeTax", 0.1406f },  { "Poland/CorporateTax", 0.1474f },  { "Poland/VAT", 0.3132f },  { "Poland/PayrollTax", 0.3775f },
            { "Sweden/IncomeTax", 0.1998f },  { "Sweden/CorporateTax", 0.1675f },  { "Sweden/VAT", 0.3798f },  { "Sweden/PayrollTax", 0.4488f },
        };

        /// <summary>True for a country the table covers - the five on the general-government perimeter. False for the USA (F-B: the federal perimeter; the uniform stand-in stays) and for any country seeded later without a row.</summary>
        public static bool IsSourced(CountryId country) => country != CountryId.USA && Sourced.ContainsKey(country + "/IncomeTax");

        /// <summary>True when this country and instrument have a sourced row.</summary>
        public static bool HasSourcedBase(CountryId country, TaxType type) => Sourced.ContainsKey(country + "/" + type);

        /// <summary>
        /// The base a revenue site multiplies `GDP × rate` by: the sourced share for a sourced pair, otherwise
        /// the uniform stand-in for that instrument (<see cref="TaxTypeBaseShares.GetBaseShareOfGdp"/>).
        /// ⚠ The USA is served the stand-in on purpose - F-B's perimeter reason in the class doc - and not by an
        /// absence in the table.
        /// </summary>
        public static float BaseShareOfGdp(CountryId country, TaxType type)
        {
            if (country != CountryId.USA && Sourced.TryGetValue(country + "/" + type, out float share))
            {
                return share;
            }
            return TaxTypeBaseShares.GetBaseShareOfGdp(type);
        }
    }
}
