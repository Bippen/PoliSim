using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// D9 ROW 6's return — **the two things Design asked for so it can draw the valkrets cartogram**,
    /// emitted by the code that owns them rather than transcribed by hand.
    ///
    /// <para><b>What was asked.</b> Board 3a's row 6 rules the FORM (a 29-cell cartogram of paper tiles,
    /// area by mandate count, north-to-south) and stops at the geometry: *"the mandate column (fixed seats
    /// per valkrets) from `valkrets_votes_2022.csv`, and the built cartogram's own cell order from
    /// `SwingRegions.cs`. Tile area is proportional to a number I will not guess, and a cartogram that
    /// disagrees with the built one is worse than no board."* ⚠ **That refusal to guess is why this
    /// diagnostic exists rather than a hand-typed table.**</para>
    ///
    /// <para>⚠ <b>ONE HALF OF THE ASK RESTS ON A WRONG PREMISE, AND IT IS CORRECTED RATHER THAN
    /// SATISFIED.</b> The mandate column is <b>not IN the CSV</b> — the file carries `eligible`, and the
    /// 310 fixed seats are DERIVED from it by the statute's own rule, which
    /// <see cref="SeatConversion.FixedSeatsPerRegion"/> implements ("one seat per 310th part of the
    /// national eligible electorate, the remainder by largest surplus"). So the column below is computed
    /// by the shipping allocator, not read. And <b>`SwingRegions.cs` holds no cell order at all</b>: it
    /// takes a name and a weight per region from its caller and knows nothing about where a region sits.
    /// **There is no built cartogram to disagree with.** The order every harness uses is the CSV's own row
    /// order, which is Valmyndigheten's valkrets numbering 01–29 — and that is a numbering, not a
    /// geography, so a north-to-south arrangement is Design's to make and ours to have never had.</para>
    ///
    /// <para>Read-only: it opens one sourced file, calls one shipping function, and prints. It writes
    /// nothing and seeds nothing.</para>
    /// </summary>
    public static class ValkretsMandateColumnDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();

            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "sweden", "valkrets_votes_2022.csv"));
            if (!File.Exists(path))
            {
                Debug.LogError("D9 row 6: valkrets_votes_2022.csv is not on disk at " + path
                               + ", so the mandate column cannot be derived and this verified NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            var names = new List<string>();
            var eligible = new List<double>();
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("valkrets;")) { continue; }
                string[] cells = line.Split(';');
                names.Add(cells[0]);
                eligible.Add(double.Parse(cells[10], CultureInfo.InvariantCulture));
            }

            // The enumeration rule: 29 valkretsar is the statute's own number, and a run that read a
            // different count has read a different file.
            if (names.Count != 29)
            {
                Debug.LogError($"D9 row 6: read {names.Count} valkretsar, not 29 - the file is not the one this "
                               + "column is defined against, and the numbers below would be a different country's.");
                CheckExit.Finish(1);
                return;
            }

            int[] mandates = SeatConversion.FixedSeatsPerRegion(eligible.ToArray());

            int total = 0;
            foreach (int m in mandates) { total += m; }

            var sb = new StringBuilder();
            sb.Append("=== D9 ROW 6 RETURN: THE MANDATE COLUMN, DERIVED BY THE SHIPPING ALLOCATOR ===\n");
            sb.Append("    source: ElectionsData/sweden/valkrets_votes_2022.csv, column 11 (eligible, SOURCED from\n");
            sb.Append("    Valmyndigheten). Derivation: SeatConversion.FixedSeatsPerRegion - the statute's one-seat-\n");
            sb.Append("    per-310th-part rule with the remainder by largest surplus. NOT a column in the file.\n\n");
            sb.Append("    #   valkrets                          eligible   FIXED SEATS\n");
            sb.Append("    ---------------------------------------------------------------\n");
            for (int i = 0; i < names.Count; i++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {0,2}  {1,-30} {2,10:N0}  {3,10}\n", i + 1, names[i], eligible[i], mandates[i]));
            }

            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n    TOTAL FIXED SEATS: {0} (the statute's 310) + 39 adjustment = 349.\n", total));

            sb.Append("\n    THE SECOND HALF OF THE ASK, CORRECTED RATHER THAN ANSWERED\n");
            sb.Append("    ----------------------------------------------------------\n");
            sb.Append("    The order above is the CSV's own row order, which is Valmyndigheten's valkrets numbering\n");
            sb.Append("    01-29. It is what every harness stages and the only order the build has.\n");
            sb.Append("    ⚠ SwingRegions.cs holds NO cell order and no geometry: it takes a name and a weight per\n");
            sb.Append("    region from its caller. THERE IS NO BUILT CARTOGRAM for a board to disagree with, so the\n");
            sb.Append("    north-to-south, coast-to-coast arrangement is Design's to make - and nothing here\n");
            sb.Append("    constrains it except that the tiles carry these names and these areas.\n");

            if (total != SeatConversion.FixedSeats)
            {
                Debug.LogError($"D9 row 6: the derived column sums to {total}, not {SeatConversion.FixedSeats}. "
                               + "A mandate column that does not sum to the statute's fixed seats is wrong, and "
                               + "sending it would put the error on Design's board.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
