using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// D-PL (Elias, 2026-10-23: "Build the Polish election night"; Design's boards 28a-28d): POLAND'S SEJM NIGHT, its arithmetic - Germany's
    /// "the night's own count reproduces the Bundestag's 630" for the Sejm's 460. (a) The night's own count at 41 of 41 (`PolishSejmNight`) on the 2023
    /// record's shares returns the PKW's 10th Sejm - PiS 194 · KO 157 · TD 65 · NL 26 · Konf 18 · MN 0 - and every okreg's seats as the chamber's own
    /// count divides them (`PolishSejmAllocation.Allocate`). (b) On a seeded sweep of national votes, and on §767's played prediction, the night at
    /// 41 of 41 calls exactly the lists the chamber admits - 5 % a party, 8 % a coalition, the minority exempt - and returns the chamber's seats okreg
    /// by okreg. (c) Through the night: a call made at any instant is the chamber's verdict; an okreg's certain seats are the least it holds over every way the
    /// open calls can land, counted out in full (the closed form `PolishSejmNight.At` uses), and never exceed its end; the minority's list is called against
    /// no line.
    /// (d) Board 28a's cartogram as generated: 41 okregi of their statutory magnitudes, 460 cells inside the grid and none twice, each okreg one block,
    /// its number in one of its own cells, the sixteen voivodeships named. (e) The inks: the five committees drawn in board 28b's hexes, unmoved by the
    /// nudge; the minority's committee given none.
    /// </summary>
    public static class PolishNightDiagnostic
    {
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        /// <summary>SOURCED: the 10th-term Sejm as the PKW's notice announced it (Dz.U. 2023 poz. 2234; `PolishSejmAllocationDiagnostic`'s own table).</summary>
        private static readonly Dictionary<string, int> Record2023 = new Dictionary<string, int> { { "PiS", 194 }, { "KO", 157 }, { "TD", 65 }, { "NL", 26 }, { "Konf", 18 }, { "MN", 0 } };

        /// <summary>[AUTHORED-REFERENCE] board 28b's legend, as installed in `PoliSimTheme`.</summary>
        private static readonly Dictionary<string, string> BoardInks = new Dictionary<string, string> { { "PiS", "384575" }, { "KO", "755638" }, { "TD", "407437" }, { "NL", "753862" }, { "Konf", "242C5D" } };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PolishNightDiagnostic (D-PL): Poland's Sejm night, its arithmetic ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                // (a) the record
                Dictionary<string, double> record = PolishSejmNight.Record2023Shares();
                PolishSejmNight.State night = PolishSejmNight.At(PolishSejmNight.FinalMinute, record);
                bool exact = night.Complete;
                var got = new List<string>();
                for (int k = 0; k < night.Keys.Length; k++)
                {
                    got.Add(night.Keys[k] + " " + night.Seats[k].ToString(CultureInfo.InvariantCulture));
                    if (!Record2023.TryGetValue(night.Keys[k], out int want) || want != night.Seats[k]) { exact = false; }
                }
                Check(exact, F("(a) the night's own count at 41 of 41 on the 2023 record's shares returns the PKW's 10th Sejm: {0}", string.Join(" · ", got)));
                Check(AgreesWithChamber(record, out string recordWhy), "(a) ... and every okreg's seats as the chamber's own count divides them" + recordWhy);

                // (b) the sweep, and §767's played prediction
                var played = new Dictionary<string, double> { { "PiS", 0.376 }, { "KO", 0.261 }, { "TD", 0.173 }, { "NL", 0.118 }, { "Konf", 0.070 }, { "MN", 0.002 } };
                Check(AgreesWithChamber(played, out string playedWhy), "(b) §767's played prediction (PiS 37.6 · KO 26.1 · TD 17.3 · NL 11.8 · Konf 7.0 · MN 0.2): the night calls and seats what the chamber seats" + playedWhy);
                var random = new System.Random(20261023);
                int sweep = 600, disagree = 0, shut = 0;
                string firstWhy = null;
                for (int t = 0; t < sweep; t++)
                {
                    Dictionary<string, double> shares = Swept(record, random, t % 3 == 0);
                    if (!AgreesWithChamber(shares, out string why)) { disagree++; firstWhy = firstWhy ?? why; }
                    foreach (KeyValuePair<string, double> kv in shares) { double line = PolishSejmAllocation.LineOf(kv.Key); if (line > 0.0 && kv.Value < line) { shut++; break; } }
                }
                Check(disagree == 0, F("(b) a seeded sweep of {0} national votes (each committee 0.2 to 1.8 times its 2023 share, a third with a new list beside them; {1} put a list under its line): the night at 41 of 41 calls the chamber's lists and returns its seats okreg by okreg in every one - {2} disagree{3}",
                    sweep, shut, disagree, firstWhy ?? string.Empty));
                foreach (double edge in new[] { 0.0499, 0.05, 0.0501 })
                {
                    Dictionary<string, double> s = Planted(record, "Konf", edge);
                    Check(AgreesWithChamber(s, out string why), F("(b) Konfederacja planted at {0:0.00} % of the vote (its line 5 %): the night calls what the chamber admits{1}", 100.0 * edge, why));
                }
                foreach (double edge in new[] { 0.0799, 0.08, 0.0801 })
                {
                    Dictionary<string, double> s = Planted(record, "TD", edge);
                    Check(AgreesWithChamber(s, out string why), F("(b) Trzecia Droga planted at {0:0.00} % (a coalition, its line 8 %): the night calls what the chamber admits{1}", 100.0 * edge, why));
                }

                // (c) through the night
                int broken = 0, nights = 20;
                string brokenWhy = null;
                var walk = new System.Random(7);
                for (int t = 0; t < nights; t++)
                {
                    Dictionary<string, double> shares = t == 0 ? record : Swept(record, walk, false);
                    PolishSejmNight.State end = PolishSejmNight.At(PolishSejmNight.FinalMinute, shares);
                    int mn = Array.IndexOf(end.Keys, "MN");
                    int rank21 = end.Okregi[20].Rank;
                    // the minority stands where it has a vote - okreg 21 alone unless the swing lifts it elsewhere (a rise in its national share spreads by the
                    // same uniform swing the chamber counts on); its seats are owed until every such okreg has declared
                    bool only21 = mn >= 0;
                    for (int d = 0; d < end.Okregi.Length && only21; d++) { if (d != 20 && end.Okregi[d].Votes[mn] > 0.0) { only21 = false; } }
                    for (int i = 0; i <= end.Total; i++)
                    {
                        PolishSejmNight.State st = PolishSejmNight.At(i == 0 ? 0 : PolishSejmNight.ArrivalMinute(i, end.Total), shares);
                        if (st.Declared != i) { broken++; brokenWhy = brokenWhy ?? F(" - at step {0} {1} declared", i, st.Declared); }
                        // a call made at any instant is the chamber's own verdict on the national vote (the admission `PolishSejmAllocation.Allocate` makes)
                        for (int k = 0; k < st.Keys.Length; k++)
                        {
                            if (st.Calls[k] != PolishSejmNight.Call.In && st.Calls[k] != PolishSejmNight.Call.Out) { continue; }
                            bool admitted = st.National[k] >= PolishSejmAllocation.LineOf(st.Keys[k]);
                            if ((st.Calls[k] == PolishSejmNight.Call.In) != admitted) { broken++; brokenWhy = brokenWhy ?? F(" - {0} called {1} at {2} of 41 on a national {3:0.0000} %", st.Keys[k], st.Calls[k], i, 100.0 * st.National[k]); }
                        }
                        // the certain seats against the minimum over every way the open calls can land, counted out in full - the closed form's proof
                        var open = new List<int>();
                        for (int k = 0; k < st.Keys.Length; k++) { if (st.Calls[k] == PolishSejmNight.Call.Open) { open.Add(k); } }
                        foreach (PolishSejmNight.Okreg o in st.Okregi)
                        {
                            if (!o.Declared) { continue; }
                            var least = new int[st.Keys.Length];
                            for (int k = 0; k < least.Length; k++) { least[k] = int.MaxValue; }
                            for (int m = 0; m < 1 << open.Count; m++)
                            {
                                var admitted = new bool[st.Keys.Length];
                                for (int k = 0; k < admitted.Length; k++) { admitted[k] = st.Calls[k] == PolishSejmNight.Call.In || st.Calls[k] == PolishSejmNight.Call.Exempt; }
                                for (int j = 0; j < open.Count; j++) { if ((m & (1 << j)) != 0) { admitted[open[j]] = true; } }
                                int[] seats = PolishSejmNight.DistrictSeats(o.Number - 1, st.Keys, o.Votes, admitted);
                                for (int k = 0; k < least.Length; k++) { least[k] = Math.Min(least[k], seats[k]); }
                            }
                            for (int k = 0; k < st.Keys.Length; k++)
                            {
                                if (o.Certain[k] != least[k]) { broken++; brokenWhy = brokenWhy ?? F(" - okreg {0} at {1} of 41: {2} certain {3}, the least over the open calls {4}", o.Number, i, st.Keys[k], o.Certain[k], least[k]); }
                                if (o.Certain[k] > end.Okregi[o.Number - 1].Certain[k]) { broken++; brokenWhy = brokenWhy ?? F(" - okreg {0} certain {1} for {2} past its end", o.Number, o.Certain[k], st.Keys[k]); }
                            }
                        }
                        if (mn >= 0)
                        {
                            if (st.Calls[mn] != PolishSejmNight.Call.Exempt) { broken++; brokenWhy = brokenWhy ?? " - MN called against a line"; }
                            if (only21 && st.SeatsKnown(mn) != (i >= rank21)) { broken++; brokenWhy = brokenWhy ?? F(" - MN's seats {0} at {1} of 41 (okreg 21 declares {2})", st.SeatsKnown(mn) ? "known" : "owed", i, rank21); }
                        }
                    }
                }
                Check(broken == 0, F("(c) {0} nights walked okreg by okreg: every call made early is the chamber's verdict on the national vote, every okreg's certain seats are the least it holds over every way the open calls can land (counted out in full) and never exceed its end, MN is called against no line and, where it has votes in okreg 21 alone, owed until 21 declares - {1} broken{2}", nights, broken, brokenWhy ?? string.Empty));
                var landed = new List<string>();
                for (int k = 0; k < night.Keys.Length; k++) { landed.Add(night.Keys[k] + " " + night.Calls[k] + (night.CalledAt[k] > 0 ? " at " + night.CalledAt[k].ToString(CultureInfo.InvariantCulture) : string.Empty)); }
                sb.Append("    the 2023 record's night, the calls as they land: ").Append(string.Join(" · ", landed)).Append('\n');
                sb.Append("    the order of declaration (fewer 2023 votes first): ").Append(string.Join(",", PolishSejmNight.DeclarationOrder())).Append('\n');

                // (d) the cartogram
                PolishOkregiCartogram.Okreg[] okregi = PolishOkregiCartogram.Okregi;
                bool shape = okregi.Length == PolishSejmDistricts2023.Magnitudes.Length;
                var seen = new HashSet<long>();
                var voivodeships = new HashSet<string>();
                string shapeWhy = null;
                int cells = 0;
                for (int d = 0; d < okregi.Length && shape; d++)
                {
                    PolishOkregiCartogram.Okreg o = okregi[d];
                    int n = o.Cells.GetLength(0);
                    cells += n;
                    if (o.Number != d + 1 || n != PolishSejmDistricts2023.Magnitudes[d]) { shape = false; shapeWhy = F(" - okreg {0}: {1} cells for {2} seats", o.Number, n, PolishSejmDistricts2023.Magnitudes[d]); break; }
                    var mine = new HashSet<long>();
                    bool label = false;
                    for (int c = 0; c < n; c++)
                    {
                        int x = o.Cells[c, 0], y = o.Cells[c, 1];
                        long id = y * 1000L + x;
                        if (x < 0 || y < 0 || x >= PolishOkregiCartogram.Columns || y >= PolishOkregiCartogram.Rows || !seen.Add(id)) { shape = false; shapeWhy = F(" - okreg {0}: cell {1},{2} outside the grid or taken twice", o.Number, x, y); }
                        mine.Add(id);
                        if (x == o.LabelColumn && y == o.LabelRow) { label = true; }
                    }
                    if (!label) { shape = false; shapeWhy = F(" - okreg {0}: its number is not in its own cells", o.Number); }
                    if (!OneBlock(mine)) { shape = false; shapeWhy = F(" - okreg {0} is not one block", o.Number); }
                    if (!PolishOkregiCartogram.VoivodeshipNames.ContainsKey(o.Voivodeship)) { shape = false; shapeWhy = F(" - okreg {0}: voivodeship {1} unnamed", o.Number, o.Voivodeship); }
                    voivodeships.Add(o.Voivodeship);
                }
                Check(shape && cells == 460 && voivodeships.Count == 16, F("(d) board 28a's cartogram: {0} okregi of their statutory magnitudes, {1} cells inside the {2} × {3} grid and none twice, each okreg one block with its number in its own cells, {4} voivodeships named (source {5}){6}",
                    okregi.Length, cells, PolishOkregiCartogram.Columns, PolishOkregiCartogram.Rows, voivodeships.Count, PolishOkregiCartogram.SourceDigest.Substring(0, 12), shapeWhy ?? string.Empty));

                // (e) the inks
                bool inks = true;
                var drawn = new List<string>();
                foreach (KeyValuePair<string, string> kv in BoardInks)
                {
                    string hex = ColorUtility.ToHtmlStringRGB(PoliSimTheme.PartyLaddered(CountryId.Poland, kv.Key));
                    drawn.Add(kv.Key + " #" + hex);
                    if (!PoliSimTheme.HasPartyInk(CountryId.Poland, kv.Key) || !string.Equals(hex, kv.Value, StringComparison.OrdinalIgnoreCase)) { inks = false; }
                }
                Check(inks && !PoliSimTheme.HasPartyInk(CountryId.Poland, "MN"), F("(e) the five committees drawn in board 28b's hexes, unmoved by the nudge ({0}); the minority's committee given none (drawn in the Neutral where it seats)", string.Join(" · ", drawn)));

            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            if (failures > 0) { Debug.LogError($"POLISH NIGHT: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>The night at 41 of 41 against the chamber's own count on the same shares: every list's call against its line (the chamber's
        /// admission - `PolishSejmAllocation.LineOf`, the minority exempt) and every okreg's seats.</summary>
        private static bool AgreesWithChamber(Dictionary<string, double> shares, out string why)
        {
            why = string.Empty;
            Dictionary<string, int> chamber = PolishSejmAllocation.Allocate(shares, out List<Dictionary<string, int>> perDistrict);
            PolishSejmNight.State night = PolishSejmNight.At(PolishSejmNight.FinalMinute, shares);
            if (night == null || !night.Complete) { why = " - the night did not complete"; return false; }
            for (int k = 0; k < night.Keys.Length; k++)
            {
                double line = PolishSejmAllocation.LineOf(night.Keys[k]);
                bool admitted = line <= 0.0 || night.National[k] >= line;
                bool calledIn = night.Calls[k] == PolishSejmNight.Call.In || night.Calls[k] == PolishSejmNight.Call.Exempt;
                if (admitted != calledIn) { why = F(" - {0} at {1:0.0000} % called {2}", night.Keys[k], 100.0 * night.National[k], night.Calls[k]); return false; }
                int seated = chamber.TryGetValue(night.Keys[k], out int s) ? s : 0;
                if (seated != night.Seats[k]) { why = F(" - {0} night {1}, chamber {2}", night.Keys[k], night.Seats[k], seated); return false; }
            }
            for (int d = 0; d < night.Okregi.Length; d++)
            {
                for (int k = 0; k < night.Keys.Length; k++)
                {
                    int want = perDistrict[d].TryGetValue(night.Keys[k], out int w) ? w : 0;
                    if (night.Okregi[d].Certain[k] != want) { why = F(" - okreg {0}: {1} night {2}, chamber {3}", d + 1, night.Keys[k], night.Okregi[d].Certain[k], want); return false; }
                }
            }
            return true;
        }

        /// <summary>A swept national vote: each 2023 committee at 0.2 to 1.8 times its 2023 share, with or without a new list, normalised.</summary>
        private static Dictionary<string, double> Swept(Dictionary<string, double> record, System.Random random, bool withNewList)
        {
            var s = new Dictionary<string, double>();
            foreach (KeyValuePair<string, double> kv in record) { s[kv.Key] = kv.Value * (0.2 + 1.6 * random.NextDouble()); }
            if (withNewList) { s["NEW"] = 0.15 * random.NextDouble(); }
            double sum = 0.0;
            foreach (double v in s.Values) { sum += v; }
            var keys = new List<string>(s.Keys);
            foreach (string k in keys) { s[k] /= sum; }
            return s;
        }

        /// <summary>The record's vote with one list planted at <paramref name="share"/>, the others scaled to the rest.</summary>
        private static Dictionary<string, double> Planted(Dictionary<string, double> record, string key, double share)
        {
            var s = new Dictionary<string, double>(record);
            double others = 0.0;
            foreach (KeyValuePair<string, double> kv in record) { if (kv.Key != key) { others += kv.Value; } }
            var keys = new List<string>(s.Keys);
            foreach (string k in keys) { s[k] = k == key ? share : s[k] * (1.0 - share) / others; }
            return s;
        }

        /// <summary>Whether a set of grid cells (row × 1000 + column) is one block by its edges.</summary>
        private static bool OneBlock(HashSet<long> cells)
        {
            if (cells.Count == 0) { return false; }
            var reached = new HashSet<long>();
            var queue = new Queue<long>();
            foreach (long c in cells) { queue.Enqueue(c); reached.Add(c); break; }
            while (queue.Count > 0)
            {
                long c = queue.Dequeue();
                foreach (long n in new[] { c + 1, c - 1, c + 1000L, c - 1000L })
                {
                    if (cells.Contains(n) && reached.Add(n)) { queue.Enqueue(n); }
                }
            }
            return reached.Count == cells.Count;
        }
    }
}
