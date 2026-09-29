using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §676 (SP-4): **THE CREATION FLOW, READ THROUGH THE MEMBERS THE SCREEN DRAWS FROM.** The mark picker offers exactly the cells D18's assignment
    /// does not spend, each installed and loadable (and none under `Emblems/`); the ink ring's fence refuses exactly the steps under the nudge's
    /// tolerance of a real party's drawn ink, and a clear one exists; the key rule folds the short name to ASCII capitals and takes a digit on a clash;
    /// every anchor the page registers has its slip; the origins' rules set what they say (the Splinter on its parent, the Protest movement's lock, the
    /// Regional party's region a valkrets its first office opens in); the commit registers the party - in the roster after the real ones, in the
    /// campaign's cast, drawn in its chosen ink and never nudged - with its declarations carried by the save's serialiser; the real parties' inks and
    /// the model's real prediction are untouched by a draft, registered or read.
    /// </summary>
    public static class PartyCreationCheck
    {
        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PartyCreationCheck (§676, SP-4): the creation flow's choices, slips and commit ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable created = CreatedParties.Scope();
            try
            {
                CreatedParties.Clear();
                const CountryId se = CountryId.Sweden;

                // ── the cells ──
                List<string> cells = PartyCreationFlow.UnspentCells();
                var spent = new HashSet<string>();
                foreach (D18MarkAssignment.Row row in D18MarkAssignment.Build(out List<string> _)) { spent.Add(row.Cell); }
                var all = new List<string>();
                foreach (string fill in PartyCreationFlow.Fills) { foreach (string cut in PartyCreationFlow.Cuts) { foreach (string s in PartyCreationFlow.Silhouettes) { all.Add($"mark_cell_{s}_{cut}_{fill}"); } } }
                var expected = all.Where(c => !spent.Contains(c)).OrderBy(c => c, StringComparer.Ordinal).ToList();
                Check(expected.SequenceEqual(cells.OrderBy(c => c, StringComparer.Ordinal)) && cells.Count == 35,
                    F("the mark picker offers exactly the cells D18's assignment does not spend: {0} of the {1} ({2} spent)", cells.Count, all.Count, spent.Count));
                int loaded = cells.Count(c => IconLibrary.GetPartyMark(c) != null);
                Check(loaded == cells.Count, F("every offered cell is installed and loads through IconLibrary.GetPartyMark: {0} of {1}", loaded, cells.Count));
                string[] strays = Directory.GetFiles("Assets/Resources/Art/UI/Emblems", "mark_cell_*", SearchOption.TopDirectoryOnly);
                Check(strays.Length == 0, F("no cell under Emblems/ (PartyMarkCoverageCheck would count it an orphan): {0}", strays.Length));

                // ── the ink ring ──
                int clear = 0, consistent = 0;
                var ring = new StringBuilder();
                for (int step = 0; step < PoliSimTheme.CreatedInkSteps; step++)
                {
                    bool ok = PartyCreationFlow.InkClear(se, step, out string nearest, out float distance);
                    if (ok) { clear++; }
                    if (ok == (nearest == null || distance >= PoliSimTheme.NudgeTolerance)) { consistent++; }
                    ring.Append(F(" {0}{1}:{2}@{3:0.000}", ok ? "" : "x", step, nearest, distance));
                }
                Check(clear > 0 && consistent == PoliSimTheme.CreatedInkSteps, F("the ink ring's fence: {0} of {1} steps clear, each refusal under the nudge's {2:0.00} of a real party's drawn ink -{3}", clear, PoliSimTheme.CreatedInkSteps, PoliSimTheme.NudgeTolerance, ring));

                // ── the key ──
                string[] keysBefore = LiveCampaignSetup.Keys(se);
                Check(PartyCreationFlow.KeyFrom(se, "Nya") == "NYA" && PartyCreationFlow.KeyFrom(se, "Östra 1") == "OSTRA1" && PartyCreationFlow.KeyFrom(se, "Är") == "AR"
                      && PartyCreationFlow.KeyFrom(se, "M") == "M2" && PartyCreationFlow.KeyFrom(se, "1 2") == string.Empty && PartyCreationFlow.KeyFrom(se, "Framtidspartiet") == "FRAMTI",
                    "the key rule: ASCII capitals and digits folded from the short name (Östra 1 -> OSTRA1), at most six, a letter first, a digit on a clash (M -> M2)");

                // ── the draft and the origins ──
                CreatedParty d = PartyCreationFlow.NewDraft(se);
                PartySystems.TryElectorate(se, out VoteModel.Electorate e, out double _);
                Check(d.Origin == PartyOrigin.Grassroots && Math.Abs(d.LrEcon - e.MuEcon) < 1e-4 && Math.Abs(d.Galtan - e.MuSoc) < 1e-4 && d.MarkStem == cells[0] && !string.IsNullOrEmpty(d.InkHex),
                    F("a new draft: grassroots, at the electorate's centre ({0:0.00}, {1:0.00}), the first unspent cell, the first clear ink {2}", d.LrEcon, d.Galtan, d.InkHex));
                PoliticalParty[] real = PartySystems.RealRoster(se);
                PoliticalParty m = real.First(p => p.Abbrev == "M");
                PartyCreationFlow.ChooseOrigin(d, PartyOrigin.Splinter);
                PartyCreationFlow.ChooseParent(d, "M");
                Check(d.Funding == 3 && d.ParentKey == "M" && d.LrEcon == m.LrEcon && d.Galtan == m.Galtan && d.Nationalism.Equals(m.Nationalism) && d.InheritedSlice > 0.0,
                    F("a Splinter of M: the origin's stats, M's nine positions, a slice of {0:P0}", d.InheritedSlice));
                PartyCreationFlow.ChooseOrigin(d, PartyOrigin.Protest);
                float most = real.Where(p => !float.IsNaN(p.PeopleVsElite)).Max(p => p.PeopleVsElite);
                Check(d.ParentKey == null && d.PeopleVsElite.Equals(most), F("a Protest movement: the parent dropped, people-versus-elite locked at the chamber's most anti-elite real position ({0:0.00})", most));
                PartyCreationFlow.ChooseOrigin(d, PartyOrigin.Regional);
                string[] regions = PartyCreationFlow.Regions(se);
                d.Region = regions[regions.Length - 1];
                Check(regions.Length > 0 && float.IsNaN(d.PeopleVsElite), F("a Regional party: its region one of the {0} valkrets names ({1}); the Protest lock released", regions.Length, d.Region));

                // ── the slips ──
                PeopleSlips.Book book = PartyCreationFlow.Slips(d);
                var anchors = new List<string> { "candidate", "outside/yes", "outside/no", "compass", "predicted", "key" };
                for (int s = 0; s < PartyCreationFlow.StepCount; s++) { anchors.Add("step/" + s); }
                for (int s = 0; s < 5; s++) { anchors.Add("stat/" + s); }
                foreach (PartyOrigins.Preset p in PartyOrigins.All) { anchors.Add("origin/" + p.Origin); }
                foreach (string c in cells) { anchors.Add("mark/" + c); }
                for (int step = 0; step < PoliSimTheme.CreatedInkSteps; step++) { anchors.Add("ink/" + step); }
                foreach (UiPalette.SystemArea a in PartyCreationFlow.Issues) { anchors.Add("issue/" + a); }
                foreach (double slice in PartyCreationFlow.Slices) { anchors.Add("slice/" + slice.ToString("0.00", CultureInfo.InvariantCulture)); }
                foreach (PoliticalParty p in real) { anchors.Add("party/" + p.Abbrev); anchors.Add("line/" + p.Abbrev); anchors.Add("parent/" + p.Abbrev); }
                var missing = anchors.Where(a => !book.Anchors.TryGetValue(a, out SlipContent c) || c.Lines.Count == 0).ToList();
                Check(missing.Count == 0, F("every anchor the page registers has its slip, with words: {0} anchors{1}", anchors.Count, missing.Count > 0 ? " - missing " + string.Join(", ", missing) : string.Empty));

                // ── the prediction reads and leaves nothing ──
                NationalElection.TryPredictShares(se, out Dictionary<string, double> before);
                PartyCreationFlow.ChooseOrigin(d, PartyOrigin.Grassroots);
                d.LrEcon = 5f; d.Galtan = 5f;
                double predicted = PartyCreationFlow.PredictedShare(d);
                NationalElection.TryPredictShares(se, out Dictionary<string, double> after);
                Check(!double.IsNaN(predicted) && predicted > 0.0 && predicted < 100.0 && !CreatedParties.Any(se) && before.Count == after.Count && before.All(kv => after[kv.Key] == kv.Value),
                    F("the placement's prediction: {0:0.00} % at (5, 5); the registry and the real prediction are as they were", predicted));

                // ── the commit ──
                var draftInk = new Dictionary<string, Color>();
                foreach (PoliticalParty p in real) { draftInk[p.Abbrev] = PoliSimTheme.PartyLaddered(se, p.Abbrev); }
                var empty = PartyCreationFlow.NewDraft(se);
                Check(!PartyCreationFlow.TryCommit(empty, out string why) && why != null && !CreatedParties.Any(se), "an unnamed draft is refused, with its reason: " + why);
                d.Name = "Framtidspartiet"; d.ShortName = "Framtid"; d.Key = PartyCreationFlow.KeyFrom(se, d.ShortName); d.LeaderName = "A. Testsson";
                d.RedLinesAgainst.Add("SD"); d.OneWayAgainst.Add("V"); d.BacksCandidateOf = d.Key; d.InOrAgainst = true;
                Check(PartyCreationFlow.TryCommit(d, out string refused), "the named draft commits: " + (refused ?? d.Key));
                IReadOnlyList<PoliticalParty> roster = PartySystems.For(se);
                string[] cast = LiveCampaignSetup.Keys(se);
                Check(roster.Count == real.Length + 1 && roster[real.Length].Abbrev == d.Key && roster[real.Length].MarkName == d.MarkStem && cast.Length == keysBefore.Length + 1 && cast[cast.Length - 1] == d.Key,
                    F("registered: {0} after the real {1} in the roster and in the campaign's cast, wearing {2}", d.Key, real.Length, d.MarkStem));
                ColorUtility.TryParseHtmlString(d.InkHex, out Color chosen);
                Check(PoliSimTheme.HasPartyInk(se, d.Key) && PoliSimTheme.Party(se, d.Key) == chosen && PoliSimTheme.PartyLaddered(se, d.Key) == chosen,
                    F("drawn in its chosen ink {0}, and never nudged (the fence refused every collision before it was chosen)", d.InkHex));
                Check(real.All(p => PoliSimTheme.PartyLaddered(se, p.Abbrev) == draftInk[p.Abbrev]), "the real parties' drawn inks are unchanged by a registered created party");
                CreatedParty back = JsonUtility.FromJson<CreatedParty>(JsonUtility.ToJson(d));
                Check(back.RedLinesAgainst.SequenceEqual(new[] { "SD" }) && back.OneWayAgainst.SequenceEqual(new[] { "V" }) && back.BacksCandidateOf == d.Key && back.InOrAgainst && (back.Region ?? string.Empty) == (d.Region ?? string.Empty),
                    "the declarations and the origin's choices survive the save's serialiser (a null string returns empty - JsonUtility's own rule, and every reader takes empty as none)");
                CreatedParties.Clear();
                var regional = PartyCreationFlow.NewDraft(se);
                PartyCreationFlow.ChooseOrigin(regional, PartyOrigin.Regional);
                regional.Region = regions[regions.Length - 1];
                regional.Name = "Regionpartiet"; regional.ShortName = "Region"; regional.Key = PartyCreationFlow.KeyFrom(se, regional.ShortName); regional.LeaderName = "B. Testsson";
                PartyCreationFlow.TryCommit(regional, out string _);
                RegionAudience[] sweden = LiveCampaignSetup.SwedenRegions(out double _);
                LiveCampaignSetup.TryCreatedDayZero(regional.Key, sweden, out double _, out int _, out int[] regionalOffices, out CandidateProfile _);
                Check(regionalOffices.Length > 0 && sweden[regionalOffices[0]].Name == regional.Region,
                    F("a Regional party's first office opens in the valkrets it chose ({0})", regional.Region));
            }
            catch (Exception ex) { failures++; sb.Append("    THREW: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace + "\n"); }
            finally
            {
                CreatedParties.Clear();
                NationalElection.TryPredictShares(CountryId.Sweden, out _);
            }

            if (failures > 0) { Debug.LogError($"PARTY CREATION: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
