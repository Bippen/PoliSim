using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// K53 (Elias, 2026-10-23, on GA-0-45 premise 6): "Sweden's blocs for 2026 and later are sourced from 2026's dated declarations (MP's paraphrase
    /// declared)". It holds:
    /// <list type="bullet">
    /// <item>(a) the sources - the pages K53's pass fetched (`ElectionsData/sweden/raw/declarations2026/`) and the 2026 sweep's
    /// (`ElectionsData/sweden/2026/raw/declarations/`) each held to its folder's SHA256SUMS; C's three pages and MP's of 2026-08-24 carrying their own
    /// dates and the words the sides rest on; every source id a side cites registered in `coalition_declarations_2026.md`;</item>
    /// <item>(b) the timeline - each party's sides chained without a gap from the record's start, 2022's table on 2022's polling day;</item>
    /// <item>(c) the ruled sides on the 2026 polling day and on a game's 2030 one: M, KD, L and SD the Tido side, S, V, MP and C the side of S's candidacy
    /// (K80: C from 2026-06-09, its leader's words on SVT's page); MP's resting on Hellden's own words on SVT's page, a declared reading;</item>
    /// <item>(d) the election reads them - the tactical layer's spec in `NationalElection.SharesFromCampaign` takes the sides standing on its polling day,
    /// the live night passes its own day, and on a planted poll with C at the threshold, C is lent none on 2026-03-01 (C neither, by its 2025-11-13
    /// speech) and votes on the 2026 polling day.</item>
    /// <item>(e) K79/K80 (2026-10-23): every other bloc reader (`NationalElection.BlocOf`) reads the same dated sides on the world's day - C in no bloc on
    /// Sweden's start (2026, before 9 June), 2022's sides on Italy's.</item>
    /// </list>
    /// </summary>
    public static class SwedenBlocsDiagnostic
    {
        private static readonly string[] Keys = { "S", "M", "SD", "V", "C", "KD", "MP", "L" };

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== SwedenBlocsDiagnostic (K53): Sweden's blocs for 2026 and later from 2026's dated declarations ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                string sweden = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "sweden"));
                string fetched = Path.Combine(sweden, "raw", "declarations2026"), swept = Path.Combine(sweden, "2026", "raw", "declarations");

                // ---- (a) the sources ----
                foreach (string folder in new[] { fetched, swept })
                {
                    List<string> faults = ItalySeatingDiagnostic.DigestFaults(folder, out int held);
                    Check(held > 0 && faults.Count == 0, F("(a) {0}: {1} file(s) held to SHA256SUMS.txt{2}", Path.GetFileName(folder), held, faults.Count == 0 ? string.Empty : " - " + string.Join("; ", faults)));
                }
                var pages = new[]
                {
                    (File: "centerpartiet_2026-09-07-vad-ar-en-mittenregering-centerpartiet-forklarar.html", Date: "<time datetime=\"2026-09-07T", Words: "Det stämmer inte."),
                    (File: "centerpartiet_2026-09-09-centerpartiet-darfor-behovs-en-regering-i-mitten.html", Date: "<time datetime=\"2026-09-09T", Words: "Det finns mer än två alternativ"),
                    (File: "centerpartiet_2026-09-12-elisabeth-thand-ringqvist-holl-valspurtstal-pa-medborgarplatsen.html", Date: "<time datetime=\"2026-09-12T", Words: "en regering i mitten av svensk politik, utan ytterkantspartierna"),
                    (File: "mp_miljopartiet-sager-nej-till-hogervriden-regering.html", Date: "Publicerad 2026-08-24", Words: "högervriden"),
                };
                foreach (var page in pages)
                {
                    string path = Path.Combine(fetched, page.File);
                    string text = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : string.Empty;
                    Check(text.Contains(page.Date) && text.Contains(page.Words), F("(a) {0}: dated by its own page ({1}) and carries \"{2}\"", page.File, page.Date.Replace("<time datetime=\"", string.Empty).TrimEnd('T'), page.Words));
                }
                string register = File.ReadAllText(Path.Combine(sweden, "2026", "coalition_declarations_2026.md"), Encoding.UTF8);
                var cited = new SortedSet<string>(StringComparer.Ordinal);
                var unregistered = new List<string>();
                foreach (NationalElection.DatedSide side in NationalElection.SwedenSides)
                {
                    foreach (Match id in Regex.Matches(side.Basis, @"\[([A-Z]+-[A-Z]+\d+[a-z]?)\]"))
                    {
                        cited.Add(id.Groups[1].Value);
                        if (!register.Contains("- [" + id.Groups[1].Value + "] ")) { unregistered.Add(id.Groups[1].Value); }
                    }
                }
                Check(cited.Count > 0 && unregistered.Count == 0, F("(a) the sides cite {0} source id(s) - {1} - each registered in coalition_declarations_2026.md{2}",
                    cited.Count, string.Join(" ", cited), unregistered.Count == 0 ? string.Empty : "; NOT: " + string.Join(" ", unregistered)));

                // ---- (b) the timeline ----
                var gaps = new List<string>();
                foreach (string party in Keys)
                {
                    var rows = new List<NationalElection.DatedSide>();
                    foreach (NationalElection.DatedSide side in NationalElection.SwedenSides) { if (side.Party == party) { rows.Add(side); } }
                    rows.Sort((x, y) => x.From.CompareTo(y.From));
                    bool chained = rows.Count > 0 && rows[0].From == DateTime.MinValue && rows[rows.Count - 1].Until == DateTime.MaxValue;
                    for (int i = 1; i < rows.Count; i++) { chained &= rows[i].From == rows[i - 1].Until; }
                    if (!chained) { gaps.Add(party); }
                }
                int[] on2022 = NationalElection.SwedenBlocsOn(Keys, new DateTime(2022, 9, 11)), table2022 = NationalElection.SwedenBlocs2022(Keys);
                Check(gaps.Count == 0 && string.Join(",", on2022) == string.Join(",", table2022),
                    F("(b) every party's sides chain from the record's start with no gap{0}; on 2022-09-11 they are 2022's table ({1})", gaps.Count == 0 ? string.Empty : " - NOT " + string.Join(" ", gaps), Line(on2022)));

                // ---- (c) the ruled sides ----
                var ruled = new Dictionary<string, int> { { "M", 1 }, { "KD", 1 }, { "L", 1 }, { "SD", 1 }, { "S", 0 }, { "V", 0 }, { "MP", 0 }, { "C", 0 } };
                foreach (DateTime polling in new[] { new DateTime(2026, 9, 13), new DateTime(2030, 9, 8) })
                {
                    int[] sides = NationalElection.SwedenBlocsOn(Keys, polling);
                    bool asRuled = true;
                    for (int i = 0; i < Keys.Length; i++) { asRuled &= sides[i] == ruled[Keys[i]]; }
                    Check(asRuled, F("(c) on {0:yyyy-MM-dd}: {1} - the Tido side M KD L SD, the side of S's candidacy S V MP C (K80: C from 2026-06-09)", polling, Line(sides)));
                }
                string mpBasis = null;
                foreach (NationalElection.DatedSide side in NationalElection.SwedenSides) { if (side.Party == "MP" && side.StandsOn(new DateTime(2026, 9, 13))) { mpBasis = side.Basis; } }
                Check(mpBasis != null && mpBasis.Contains("[MP-I3]") && mpBasis.Contains("DECLARED reading") && mpBasis.Contains("[MP-I4]"), "(c) MP's 2026 side rests on Hellden's own words on SVT's page [MP-I3] (F2), read as a DECLARED reading, with TT's paraphrase [MP-I4] beside it");

                // ---- (d) the election reads them ----
                string election = SourceText.ReadWithoutComments(Path.Combine("Assets", "Scripts", "Elections", "NationalElection.cs"));
                string night = SourceText.ReadWithoutComments(Path.Combine("Assets", "Scripts", "Simulation", "SimulationManager.PlayerElection.cs"));
                // GATE-18 q25 (L-3 of its review): the night reaches the layer through SharesOnPublishedPolls, which hands SharesFromCampaign the day it is given.
                // COAL-2 (K137, K330; the COAL-2 review's H1): and the played world - the sides a party re-declared after a count there - which the count's call
                // passes as played: country
                Check(election.Contains("SwedenBlocsOn(keys, on.Value, played)") && Regex.IsMatch(night, @"SharesOnPublishedPolls\([^;]*CurrentDate[^;]*played: country\)")
                        && election.Contains("SharesFromCampaign(country, keys, finalShares, belief, out tactical, on: pollingDay, played: played)"),
                    "(d) SharesFromCampaign's tactical spec reads the sides standing on its polling day in the played world (SwedenBlocsOn on the day passed in, with the country), and the live night passes its own day and its own country (SharesOnPublishedPolls on CurrentDate with played: country, which hands SharesFromCampaign on: pollingDay and played)");
                double[] planted = { 0.33, 0.19, 0.20, 0.08, 0.04, 0.055, 0.06, 0.045 };   // PLANTED: C at the threshold, so its fate is in play
                var moe = new double[Keys.Length];
                for (int i = 0; i < moe.Length; i++) { moe[i] = 1.0; }
                double LentToC(DateTime polling)
                {
                    var spec = new TacticalSpec(SeatConversion.NationalThreshold, NationalElection.TacticalAwareness, NationalElection.SwedenBlocsOn(Keys, polling), null);
                    return TacticalVoting.Apply(planted, planted, moe, spec).Inflow(Array.IndexOf(Keys, "C"));
                }
                double before = LentToC(new DateTime(2026, 3, 1)), after = LentToC(new DateTime(2026, 9, 13));
                Check(before == 0.0 && after > 0.0, F("(d) a planted poll with C at 4.0 %: C is lent {0:F2} pp on the sides of 2026-03-01 (C in neither, by its 2025-11-13 speech) and {1:F2} pp on the polling day's (C on S's side, K80)", before * 100.0, after * 100.0));

                // ---- (e) K79 (2026-10-23): the same dated sides EVERYWHERE - `NationalElection.BlocOf` (the seat map, the inks' fence, the stance
                // model's bloc pull) reads them on the date of the world that asks, its opening date: Sweden's start reads C in no bloc, a world opened on
                // Italy's start (2022) reads 2022's ----
                var readings = new List<string>();
                bool everywhere = true;
                foreach ((CountryId start, int expectC) in new[] { (CountryId.Sweden, -1), (CountryId.Italy, 0) })
                {
                    using (PoliSim.Simulation.SimulationManager.EpochScope())
                    {
                        WorldClock.ApplyStart(start);
                        DateTime opens = PoliSim.Simulation.SimulationManager.EpochDate;
                        int[] dated = NationalElection.SwedenBlocsOn(Keys, opens);
                        for (int i = 0; i < Keys.Length; i++) { everywhere &= NationalElection.BlocOf(CountryId.Sweden, Keys[i]) == dated[i]; }
                        int c = NationalElection.BlocOf(CountryId.Sweden, "C");
                        everywhere &= c == expectC;
                        readings.Add(F("a world opened on {0}'s start ({1:yyyy-MM-dd}) reads C {2}", start, opens, c));
                    }
                }
                Check(everywhere, "(e) K79: BlocOf reads the dated sides on the world's date - " + string.Join("; ", readings));

                // ---- (f) K80 on a running world, and a counterfactual's restore (the review of 2026-10-24, M1): a Sweden-start manager advanced across
                // 9 June 2026 reads C -1 on the 8th and 0 on the 9th through BOTH readers (the country's own day and the played day); a shadow fork restored
                // from it on the 10th leaves the played day where the played world put it, so the seat map does not fall back to the epoch's reading ----
                var host = new GameObject("SwedenBlocsDiagnostic (f)");
                PoliSim.Simulation.ShadowBaseline fork = null;
                try
                {
                    using (PoliSim.Simulation.SimulationManager.EpochScope())
                    {
                        WorldClock.ApplyStart(CountryId.Sweden);
                        PoliSim.Simulation.SimulationRandom.Seed(777);
                        World world = WorldFactory.CreateDefault();
                        var sim = host.AddComponent<PoliSim.Simulation.SimulationManager>();
                        sim.SetWorld(world);
                        sim.PlayerCountryId = CountryId.Sweden;
                        Country se = world.GetCountry(CountryId.Sweden);
                        var eve = new DateTime(2026, 6, 8);
                        int guard = 0;
                        while (sim.CurrentDate < eve && guard++ < 400) { sim.AdvanceDay(); }
                        int onEveCountry = NationalElection.BlocOf(se, "C"), onEveId = NationalElection.BlocOf(CountryId.Sweden, "C");
                        sim.AdvanceDay();
                        int onDayCountry = NationalElection.BlocOf(se, "C"), onDayId = NationalElection.BlocOf(CountryId.Sweden, "C");
                        sim.AdvanceDay();
                        DateTime played = PoliSim.Simulation.SimulationManager.PlayedDay;
                        fork = new PoliSim.Simulation.ShadowBaseline(sim, world, CountryId.Sweden, insideTurn: true);
                        DateTime afterFork = PoliSim.Simulation.SimulationManager.PlayedDay;
                        int afterForkId = NationalElection.BlocOf(CountryId.Sweden, "C");
                        bool ok = sim.CurrentDate == new DateTime(2026, 6, 10) && onEveCountry == -1 && onEveId == -1 && onDayCountry == 0 && onDayId == 0
                                  && played == sim.CurrentDate.Date && afterFork == played && afterForkId == 0;
                        Check(ok, F("(f) K80 on a running Sweden-start world: C reads {0} and {1} on 2026-06-08 (the country's day, the played day), {2} and {3} on 2026-06-09; a shadow fork restored on {4:yyyy-MM-dd} leaves the played day {5:yyyy-MM-dd} (was {6:yyyy-MM-dd}) and C at {7}",
                            onEveCountry, onEveId, onDayCountry, onDayId, sim.CurrentDate, afterFork, played, afterForkId));
                    }
                }
                finally
                {
                    fork?.Dispose();
                    UnityEngine.Object.DestroyImmediate(host);
                    PoliSim.Simulation.EnergyMarket.ResetTurnState();
                }
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            sb.Append(failures == 0 ? "=== SwedenBlocsDiagnostic: the blocs are 2026's dated sides ===" : F("=== SwedenBlocsDiagnostic: {0} FAILED ===", failures));
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string Line(int[] sides)
        {
            var parts = new List<string>();
            for (int i = 0; i < Keys.Length; i++) { parts.Add(Keys[i] + " " + sides[i].ToString(CultureInfo.InvariantCulture)); }
            return string.Join(", ", parts);
        }
    }
}
