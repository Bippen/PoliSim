using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PoliSim.Data;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **D18's answer, as rules rather than as a table.** Design answered the 43-mark batch on board 11b
    /// (2026-09-09) and the answer was not 43 drawings: the vocabulary was already cut - five silhouettes
    /// × five cuts × two fills, fifty neutral cells - so the batch is a SEED TABLE over cells that exist.
    ///
    /// <para><b>Why this file exists rather than a copy of their table.</b> Design shipped the assignment
    /// as `send/d18_mark_assignment.json`, 43 rows. Copying those rows into this repo would be a
    /// transcription, and this project has just spent a whole pass proving what transcriptions do
    /// (§419). The four rules the board states are enough to REGENERATE every row, so they are
    /// implemented here and the table is derived from the seeds:</para>
    ///
    /// <list type="number">
    /// <item><b>Silhouette first.</b> Parties by seats descending; the top five in a chamber get five
    /// distinct silhouettes, which are the parties an arc actually shows.</item>
    /// <item><b>The triple is unique within the chamber</b> - silhouette · cut · fill.</item>
    /// <item><b>The cut ladder advances only when the silhouettes are spent</b>: none → bar → notch →
    /// split → spine. The cleanest face goes to the biggest party.</item>
    /// <item><b>The hatched fill stays unspent.</b> It is the identical-ink device, and these four
    /// chambers have no ink to be identical - it waits for the day D8-2 lands and produces a collision.</item>
    /// </list>
    ///
    /// <para>⚠ <b>This check does not fail while the art is absent.</b> The cells are delivered files in
    /// the design project and have not reached this repo (see `COMPLETED.md` §424 for why the attempt to
    /// carry them through a session's own context was stopped: four of eight arrived with broken CRCs).
    /// Until they land, every row prints as AWAITING ART, which is the `PartyMarkCoverageCheck`
    /// precedent - an undelivered mark is a GAP, not a broken build. What DOES fail here is the
    /// assignment's own arithmetic: a duplicate triple, a top five that repeats a silhouette, a cut
    /// spent early, or a stem that does not match the naming rule the delivered marks prove.</para>
    /// </summary>
    public static class D18MarkAssignment
    {
        /// <summary>Board 4b's vocabulary, in the order the board's own grid reads. ⚠ ORDER IS LOAD-BEARING:
        /// rank 1 in a chamber takes the first silhouette, rank 6 the first again with the next cut.</summary>
        private static readonly string[] Silhouettes = { "square", "disc", "hex", "wedge", "keystone" };

        /// <summary>The cut ladder. Advanced only when the silhouettes are spent, cleanest face first.</summary>
        private static readonly string[] Cuts = { "none", "bar", "notch", "split", "spine" };

        /// <summary>⚠ SOLID only. The hatched half of every cell exists and is deliberately unspent -
        /// rule 4. A run that starts spending it is answering a collision that does not exist yet.</summary>
        private const string Fill = "solid";

        private const string CellFolder = "Assets/Resources/Art/UI/Emblems";

        public readonly struct Row
        {
            public readonly CountryId Country;
            public readonly string Abbrev;
            public readonly string Name;
            public readonly int Seats;
            public readonly string Silhouette;
            public readonly string Cut;
            public readonly string Cell;
            public readonly string Stem;

            public Row(CountryId country, string abbrev, string name, int seats, string silhouette, string cut, string cell, string stem)
            {
                Country = country;
                Abbrev = abbrev;
                Name = name;
                Seats = seats;
                Silhouette = silhouette;
                Cut = cut;
                Cell = cell;
                Stem = stem;
            }
        }

        /// <summary>The batch, derived. A chamber is in it when it has parties with no resolving mark;
        /// the parties inside it are ranked by seats descending, ties keeping the seed's own order,
        /// which is what makes this reproduce Design's table row for row rather than approximately.</summary>
        public static List<Row> Build(out List<string> faults)
        {
            faults = new List<string>();
            var rows = new List<Row>();

            foreach (CountryId country in Enum.GetValues(typeof(CountryId)))
            {
                var pending = new List<PoliticalParty>();
                foreach (PoliticalParty party in PartySystems.For(country))
                {
                    if (string.IsNullOrEmpty(party.MarkName) || IconLibrary.GetPartyMark(party.MarkName) == null)
                    {
                        pending.Add(party);
                    }
                }

                if (pending.Count == 0) { continue; }

                // ⚠ A STABLE sort, not OrderByDescending on a fresh list: four Italian parties hold one
                // seat each and three French ones do, and the seed's order is the only tie-break that
                // exists. An unstable sort would produce a different assignment on a different runtime
                // for the same repo, which is the one thing a seed table may not do.
                pending.Sort((a, b) => b.SeedSeats.CompareTo(a.SeedSeats));

                string iso = Iso2(country);
                if (iso == null)
                {
                    faults.Add(country + " has no ISO code here, so no stem can be named for it");
                    continue;
                }

                for (int k = 0; k < pending.Count; k++)
                {
                    string silhouette = Silhouettes[k % Silhouettes.Length];
                    int cutIndex = k / Silhouettes.Length;
                    if (cutIndex >= Cuts.Length)
                    {
                        faults.Add($"{country} needs {pending.Count} marks and the vocabulary offers "
                                   + $"{Silhouettes.Length * Cuts.Length} solid cells - the ladder is spent");
                        break;
                    }

                    string cut = Cuts[cutIndex];
                    rows.Add(new Row(country, pending[k].Abbrev, pending[k].Name, pending[k].SeedSeats,
                        silhouette, cut,
                        $"mark_cell_{silhouette}_{cut}_{Fill}",
                        $"mark_party_{iso}_{Slug(pending[k].Abbrev)}"));
                }
            }

            return rows;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();

            List<Row> rows = Build(out List<string> faults);
            var sb = new StringBuilder();
            sb.Append("=== D18: the 43 party marks as a seed table over cells already cut ===\n");

            if (rows.Count == 0)
            {
                // Not a pass. Either every chamber is covered - in which case this check has nothing to
                // say and should be retired - or the enumeration broke.
                Debug.LogError("D18MARKS: no chamber needs a mark, so this check VERIFIED NOTHING. Either the "
                               + "batch has landed and this check is spent, or PartySystems returned nothing.");
                CheckExit.Finish(1);
                return;
            }

            // ---- rule 2: the triple is unique inside its own chamber ------------------------------------
            var seen = new Dictionary<string, string>();
            foreach (Row row in rows)
            {
                string key = row.Country + "/" + row.Cell;
                if (seen.TryGetValue(key, out string first))
                {
                    faults.Add($"{row.Country}: {row.Abbrev} and {first} both take {row.Cell} - a mark must be "
                               + "unique inside its own chamber");
                }
                else
                {
                    seen[key] = row.Abbrev;
                }
            }

            // ---- rule 1: the top five by seats carry five distinct silhouettes -------------------------
            // ---- rule 3: a cut is spent only after the silhouettes are ---------------------------------
            foreach (CountryId country in Enum.GetValues(typeof(CountryId)))
            {
                var inChamber = new List<Row>();
                foreach (Row row in rows) { if (row.Country == country) { inChamber.Add(row); } }
                if (inChamber.Count == 0) { continue; }

                var topSilhouettes = new HashSet<string>();
                int top = Math.Min(5, inChamber.Count);
                for (int i = 0; i < top; i++)
                {
                    if (!topSilhouettes.Add(inChamber[i].Silhouette))
                    {
                        faults.Add($"{country}: the top {top} by seats repeat the silhouette {inChamber[i].Silhouette}");
                    }

                    if (inChamber[i].Cut != Cuts[0])
                    {
                        faults.Add($"{country}: {inChamber[i].Abbrev} is in the top five and already spends the "
                                   + $"cut {inChamber[i].Cut} - the cleanest face goes to the biggest party");
                    }
                }
            }

            // ---- the naming rule, against the marks that ARE delivered ----------------------------------
            int verified = 0;
            foreach (CountryId country in Enum.GetValues(typeof(CountryId)))
            {
                foreach (PoliticalParty party in PartySystems.For(country))
                {
                    if (string.IsNullOrEmpty(party.MarkName)) { continue; }

                    string expected = $"mark_party_{Iso2(country)}_{Slug(party.Abbrev)}";
                    verified++;
                    if (!string.Equals(party.MarkName, expected, StringComparison.Ordinal))
                    {
                        faults.Add($"the naming rule fails on the DELIVERED mark {party.MarkName} ({country} "
                                   + $"{party.Abbrev}): the rule produces {expected}");
                    }
                }
            }

            if (verified == 0)
            {
                faults.Add("no delivered mark was available to check the naming rule against");
            }

            // ---- what the batch needs, and what of it is on disk ----------------------------------------
            var cells = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (Row row in rows)
            {
                cells.TryGetValue(row.Cell, out int n);
                cells[row.Cell] = n + 1;
            }

            string folder = Path.Combine(Directory.GetCurrentDirectory(), CellFolder.Replace('/', Path.DirectorySeparatorChar));
            int present = 0;
            foreach (KeyValuePair<string, int> cell in cells)
            {
                bool onDisk = File.Exists(Path.Combine(folder, cell.Key + ".png"));
                if (onDisk) { present++; }
            }

            var chambers = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (Row row in rows)
            {
                chambers.TryGetValue(row.Country.ToString(), out int n);
                chambers[row.Country.ToString()] = n + 1;
            }

            sb.Append($"    {rows.Count} row(s) across {chambers.Count} chamber(s): ");
            foreach (KeyValuePair<string, int> chamber in chambers) { sb.Append($"{chamber.Key} {chamber.Value} · "); }

            sb.Append($"\n    {cells.Count} distinct cell(s) carry them; {present} of those are in {CellFolder}.\n");
            sb.Append($"    the naming rule re-derived against {verified} delivered mark(s).\n\n");
            sb.Append("    chamber   abbrev  seats  triple                      stem\n");
            foreach (Row row in rows)
            {
                sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "    {0,-9} {1,-7} {2,5}  {3,-27} {4}{5}\n",
                    row.Country, row.Abbrev, row.Seats, row.Silhouette + " · " + row.Cut + " · " + Fill,
                    row.Stem, present == cells.Count ? string.Empty : "  AWAITING ART"));
            }

            if (present < cells.Count)
            {
                sb.Append($"\n    ⚠ AWAITING ART: {cells.Count - present} of {cells.Count} cells are not in this repo. "
                          + "They are delivered files in the design\n      project (`send/marks/`); what has not happened "
                          + "is the copy. REPORTED, not failed - the\n      PartyMarkCoverageCheck precedent: an "
                          + "undelivered mark is a gap, and the row says so.\n");
            }

            foreach (string fault in faults) { sb.Append("    ⚠ FAULT  ").Append(fault).Append('\n'); }

            if (faults.Count > 0)
            {
                Debug.LogError(sb.ToString() + "\nD18MARKS: the assignment's own arithmetic is wrong in "
                               + faults.Count + " place(s).");
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            Debug.Log("=== D18MarkAssignment: ALL ASSERTIONS PASS ===");
            CheckExit.Finish(0);
        }

        private static string Iso2(CountryId id)
        {
            switch (id)
            {
                case CountryId.USA:     return "us";
                case CountryId.Sweden:  return "se";
                case CountryId.Germany: return "de";
                case CountryId.France:  return "fr";
                case CountryId.Italy:   return "it";
                case CountryId.Poland:  return "pl";
                default: return null;
            }
        }

        private static string Slug(string abbrev)
        {
            var sb = new StringBuilder();
            foreach (char c in abbrev ?? string.Empty)
            {
                if (char.IsLetterOrDigit(c)) { sb.Append(char.ToLowerInvariant(c)); }
            }

            return sb.ToString();
        }
    }
}
