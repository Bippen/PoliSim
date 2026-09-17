using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **P6-D1 (2026-09-17, `COMPLETED.md` §533): the seeded trade matrix held to its source.** Four assertions over a
    /// fresh world: (1) every one of the fifteen pairs of the six carries a link in BOTH directions - the absence the pair
    /// page used to report honestly is now a failure; (2) the world is symmetric - what A says it sells to B is what B
    /// says it buys from A, both ways, to the bit; (3) every link reproduces `TradeMatrixTable`'s own euros at the book's
    /// rate - the seeding did not drift from the transcription; (4) each reporter's five-partner exports and imports sit
    /// INSIDE its own world totals from the same source, and the share is printed - a matrix that summed past a country's
    /// whole trade would be a transcription error, and the share is the number a reader wants anyway.
    ///
    /// <para>⚠ <b>The USA's total is an explicit absence, stated, not asserted.</b> The source carries the five's books
    /// and not the USA's; its pair flows are the five's mirrors, and a USA total from a second source would break the
    /// one-source rule the table is built on. The line is printed so nobody reads the missing assertion as a passing one.</para>
    /// </summary>
    public static class TradeMatrixDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var failures = new List<string>();
            var sb = new StringBuilder("=== TradeMatrixDiagnostic: the seeded trade links against the sourced matrix ===\n");
            World world = WorldFactory.CreateDefault();
            double rate = TradeMatrixTable.UsdPerEur;
            sb.Append(F("    the book's rate: {0:F4} USD per EUR (the ECB {1} reference the energy layer carries); vintage {1}\n", rate, TradeMatrixTable.Vintage));

            int pairs = 0;
            foreach ((CountryId a, CountryId b) in TradeMatrixTable.Pairs())
            {
                pairs++;
                Country ca = world.GetCountry(a), cb = world.GetCountry(b);
                TradePartner ab = ca?.TradePartners.Find(p => p.PartnerId == b);
                TradePartner ba = cb?.TradePartners.Find(p => p.PartnerId == a);
                if (ab == null || ba == null)
                {
                    failures.Add(F("{0}-{1}: {2} - a pair of the six without a link in both directions", a, b, ab == null && ba == null ? "no link either way" : ab == null ? a + " holds no link to " + b : b + " holds no link to " + a));
                    continue;
                }

                // (2) symmetry, to the bit - the seeding mirrors, so anything else is a second write.
                if (ab.ExportVolume != ba.ImportVolume || ab.ImportVolume != ba.ExportVolume)
                {
                    failures.Add(F("{0}-{1}: not symmetric - {0} exports {2:R} but {1} imports {3:R}; {0} imports {4:R} but {1} exports {5:R}", a, b, ab.ExportVolume, ba.ImportVolume, ab.ImportVolume, ba.ExportVolume));
                }

                // (3) the table, reproduced.
                float tableAb = TradeMatrixTable.VolumeUsdBn(a, b), tableBa = TradeMatrixTable.VolumeUsdBn(b, a);
                if (ab.ExportVolume != tableAb || ba.ExportVolume != tableBa)
                {
                    failures.Add(F("{0}-{1}: the seeded volumes ({2:R} / {3:R}) are not the table's ({4:R} / {5:R})", a, b, ab.ExportVolume, ba.ExportVolume, tableAb, tableBa));
                }

                sb.Append(F("    {0,-8} -> {1,-8} {2,8:F2} bn USD    {1,-8} -> {0,-8} {3,8:F2} bn USD\n", a, b, ab.ExportVolume, ba.ExportVolume));
            }

            if (pairs != 15) { failures.Add(F("{0} pairs enumerated, not the fifteen of six countries", pairs)); }

            // (4) each reporter's five-partner sums inside its own totals, in the source's euros.
            foreach (Country c in world.Countries)
            {
                if (!TradeMatrixTable.TryGetTotal(c.Id, out TradeMatrixTable.Total total))
                {
                    sb.Append(F("    {0}: NO REPORTER TOTAL IN THE SOURCE - Eurostat carries the five's books, not the USA's; its pair flows are the five's mirrors and its total is not asserted.\n", c.Id));
                    continue;
                }

                double exportsEur = 0, importsEur = 0;
                foreach (TradePartner link in c.TradePartners)
                {
                    if (TradeMatrixTable.TryGetFlow(c.Id, link.PartnerId, out TradeMatrixTable.Flow outFlow)) { exportsEur += outFlow.GoodsEur + outFlow.ServicesEur; }
                    if (TradeMatrixTable.TryGetFlow(link.PartnerId, c.Id, out TradeMatrixTable.Flow inFlow)) { importsEur += inFlow.GoodsEur + inFlow.ServicesEur; }
                }

                double totalExports = total.GoodsExportsEur + (double)total.ServicesCreditsEur;
                double totalImports = total.GoodsImportsEur + (double)total.ServicesDebitsEur;
                if (exportsEur >= totalExports) { failures.Add(F("{0}: its five partners' exports ({1:F1} bn EUR) exceed its world exports ({2:F1} bn EUR)", c.Id, exportsEur / 1e9, totalExports / 1e9)); }
                if (importsEur >= totalImports) { failures.Add(F("{0}: its five partners' imports ({1:F1} bn EUR) exceed its world imports ({2:F1} bn EUR)", c.Id, importsEur / 1e9, totalImports / 1e9)); }
                sb.Append(F("    {0,-8} the five take {1:F1} % of its exports ({2:F1} of {3:F1} bn EUR) and supply {4:F1} % of its imports ({5:F1} of {6:F1} bn EUR)\n",
                    c.Id, 100 * exportsEur / totalExports, exportsEur / 1e9, totalExports / 1e9, 100 * importsEur / totalImports, importsEur / 1e9, totalImports / 1e9));
            }

            foreach (string f in failures) { sb.Append("    ⚠ ").Append(f).Append('\n'); }
            sb.Append(failures.Count == 0
                ? "    CLEAN - fifteen pairs, symmetric, the table reproduced, every reporter's five inside its own totals."
                : F("    {0} FAILURE(S).", failures.Count));
            if (failures.Count == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures.Count == 0 ? 0 : 1);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
