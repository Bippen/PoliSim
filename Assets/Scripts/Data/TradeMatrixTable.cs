using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// **P6-D1 (2026-09-17, `COMPLETED.md` §533): the bilateral trade matrix for the six, SOURCED, one vintage.**
    ///
    /// <para><b>What a flow is.</b> One directed flow, from one country to another, in the source's euros: goods
    /// as the EXPORTER reports them (Eurostat Comext `ds-045409`, product TOTAL, flow 2 exports, `VALUE_IN_EUROS`,
    /// annual 2023) and services as the exporter's CREDITS (Eurostat `bop_its6_det`, item S, `CRE`, million euro,
    /// 2023, multiplied out to euros here). ⚠ <b>The USA is not a Eurostat reporter</b>, so a flow FROM the USA is
    /// the EU country's own report of what it took in - goods flow 1 (imports, CIF) and services DEBITS - which is
    /// the same book read from the other side, not a second source. Both datasets were pulled through Eurostat's
    /// dissemination API on 2026-09-17; the goods dataset was last updated by Eurostat on 2026-09-15, the services
    /// one on 2026-04-16.</para>
    ///
    /// <para><b>The convention, and the mirror gap it settles.</b> Two EU reporters describe every intra-EU pair
    /// twice, and the two books disagree - the exporter's FOB against the importer's CIF, plus each office's own
    /// attribution - by up to fifteen per cent on the Italian pairs in this vintage (§533 has every pair's gap).
    /// One directed flow needs one number, so every flow is the exporter's; the importer's report is not used and
    /// not lost, it is in the record. A country's link therefore carries `ExportVolume` = its own report and
    /// `ImportVolume` = its partner's report, and the seeded world is symmetric by construction.</para>
    ///
    /// <para><b>The book's units.</b> `TradePartner` volumes are in the book's own units, US dollars in billions,
    /// so the euro figures are converted at the rate the book already uses everywhere - the ECB 2023 reference rate
    /// the energy layer carries - read from where it lives rather than typed here a second time.</para>
    ///
    /// <para>⚠ <b>No figure here is authored.</b> Every number is a transcription of the source's own value for the
    /// pair and the year, held by `TradeMatrixDiagnostic`: every pair present, symmetric, reproducing this table,
    /// and each reporter's five-partner sum inside its own world total from the same source.</para>
    /// </summary>
    public static class TradeMatrixTable
    {
        /// <summary>The year both datasets are read at.</summary>
        public const int Vintage = 2023;

        /// <summary>One directed flow in the source's euros, as the exporter reports it (see the class note for the USA).</summary>
        public readonly struct Flow
        {
            public readonly CountryId From;
            public readonly CountryId To;
            public readonly long GoodsEur;
            public readonly long ServicesEur;

            public Flow(CountryId from, CountryId to, long goodsEur, long servicesEur)
            {
                From = from; To = to; GoodsEur = goodsEur; ServicesEur = servicesEur;
            }
        }

        /// <summary>A reporter's own world totals from the same two datasets: goods exports and imports (partner WORLD),
        /// services credits and debits (partner WRL_REST), euros.</summary>
        public readonly struct Total
        {
            public readonly CountryId Country;
            public readonly long GoodsExportsEur;
            public readonly long GoodsImportsEur;
            public readonly long ServicesCreditsEur;
            public readonly long ServicesDebitsEur;

            public Total(CountryId country, long goodsExportsEur, long goodsImportsEur, long servicesCreditsEur, long servicesDebitsEur)
            {
                Country = country; GoodsExportsEur = goodsExportsEur; GoodsImportsEur = goodsImportsEur;
                ServicesCreditsEur = servicesCreditsEur; ServicesDebitsEur = servicesDebitsEur;
            }
        }

        /// <summary>The thirty directed flows: twenty between the five EU members, five from each of them to the USA, five from the USA read off the five's own books.</summary>
        public static readonly Flow[] Flows =
        {
            new Flow(CountryId.Sweden, CountryId.Germany, 19067776452, 7104500000),
            new Flow(CountryId.Sweden, CountryId.France, 8190075235, 4579100000),
            new Flow(CountryId.Sweden, CountryId.Italy, 5445315051, 1308300000),
            new Flow(CountryId.Sweden, CountryId.Poland, 7020614387, 1310600000),
            new Flow(CountryId.Sweden, CountryId.USA, 16284471612, 13334800000),
            new Flow(CountryId.Germany, CountryId.France, 119763779209, 29594000000),
            new Flow(CountryId.Germany, CountryId.Italy, 85373857285, 12441000000),
            new Flow(CountryId.Germany, CountryId.Poland, 90575789621, 10184000000),
            new Flow(CountryId.Germany, CountryId.Sweden, 29582890613, 6308000000),
            new Flow(CountryId.Germany, CountryId.USA, 157726847016, 68956000000),
            new Flow(CountryId.France, CountryId.Germany, 81817582467, 34726000000),
            new Flow(CountryId.France, CountryId.Italy, 53053965224, 15689000000),
            new Flow(CountryId.France, CountryId.Poland, 14499803120, 2809000000),
            new Flow(CountryId.France, CountryId.Sweden, 6416639181, 3743000000),
            new Flow(CountryId.France, CountryId.USA, 43883988999, 45886000000),
            new Flow(CountryId.Italy, CountryId.Germany, 74726856796, 15864900000),
            new Flow(CountryId.Italy, CountryId.France, 63567463620, 13963200000),
            new Flow(CountryId.Italy, CountryId.Poland, 19768167015, 2872400000),
            new Flow(CountryId.Italy, CountryId.Sweden, 6122913417, 1501000000),
            new Flow(CountryId.Italy, CountryId.USA, 67166032074, 13479800000),
            new Flow(CountryId.Poland, CountryId.Germany, 98599833042, 22222800000),
            new Flow(CountryId.Poland, CountryId.France, 21547381421, 3571300000),
            new Flow(CountryId.Poland, CountryId.Italy, 16051873613, 1909500000),
            new Flow(CountryId.Poland, CountryId.Sweden, 8715190101, 3004000000),
            new Flow(CountryId.Poland, CountryId.USA, 11002916144, 9654600000),
            // From the USA: the EU reporter's imports from the USA (goods flow 1, CIF) and its services debits.
            new Flow(CountryId.USA, CountryId.Sweden, 6300797391, 14124100000),
            new Flow(CountryId.USA, CountryId.Germany, 72037923600, 60477000000),
            new Flow(CountryId.USA, CountryId.France, 43826763711, 29033000000),
            new Flow(CountryId.USA, CountryId.Italy, 25235639698, 11714500000),
            new Flow(CountryId.USA, CountryId.Poland, 10410386631, 4173700000),
        };

        /// <summary>The five reporters' own world totals. ⚠ The USA has none here: Eurostat carries the five's books, not the USA's, and a
        /// total from a second source would break the one-source rule this table is built on - the diagnostic names that absence rather than filling it.</summary>
        public static readonly Total[] Totals =
        {
            new Total(CountryId.Sweden, 182728384443, 178691325887, 97826000000, 106428300000),
            new Total(CountryId.Germany, 1574462443685, 1357423831439, 418631000000, 482069000000),
            new Total(CountryId.France, 602223492411, 729003617869, 346396000000, 306913000000),
            new Total(CountryId.Italy, 625949746357, 591938823053, 137258600000, 139003000000),
            new Total(CountryId.Poland, 352925738506, 342302352388, 100546900000, 61107000000),
        };

        /// <summary>The book's dollars per euro - the ECB 2023 reference rate the energy layer already carries, read through
        /// its own accessor so the book has one rate: a euro member's currency per book dollar is euros per dollar, and its
        /// inverse is this. Germany's slot is any euro member's; they are one number.</summary>
        public static double UsdPerEur => 1.0 / EnergyLayer.NationalPerUsd(CountryId.Germany);

        public static bool TryGetFlow(CountryId from, CountryId to, out Flow flow)
        {
            foreach (Flow f in Flows)
            {
                if (f.From == from && f.To == to) { flow = f; return true; }
            }

            flow = default;
            return false;
        }

        public static bool TryGetTotal(CountryId id, out Total total)
        {
            foreach (Total t in Totals)
            {
                if (t.Country == id) { total = t; return true; }
            }

            total = default;
            return false;
        }

        /// <summary>Goods plus services from one country to another, in the book's units (US dollars, billions). ⚠ A pair the
        /// table does not carry is an error at seed time, not a zero: the whole point of the table is that no pair is missing.</summary>
        public static float VolumeUsdBn(CountryId from, CountryId to)
        {
            if (!TryGetFlow(from, to, out Flow flow))
            {
                throw new InvalidOperationException($"TradeMatrixTable carries no flow from {from} to {to}; every pair of the six must be sourced.");
            }

            return (float)((flow.GoodsEur + flow.ServicesEur) * UsdPerEur / 1e9);
        }

        /// <summary>Every unordered pair of the six, for the diagnostic's enumeration.</summary>
        public static IEnumerable<(CountryId A, CountryId B)> Pairs()
        {
            CountryId[] all = (CountryId[])Enum.GetValues(typeof(CountryId));
            for (int i = 0; i < all.Length; i++)
            {
                for (int j = i + 1; j < all.Length; j++) { yield return (all[i], all[j]); }
            }
        }
    }
}
