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
                // SEAT-1: the count is derived, never typed - the 50 cells less the ones the runtime's assignment spends (`PartyCreationFlow.SeedCells`, which
                // D18MarkAssignment.Build reads too): no cell both assigned and offered
                int runtimeSpent = PartyCreationFlow.SeedCells().Select(r => r.Cell).Distinct().Count();
                Check(expected.SequenceEqual(cells.OrderBy(c => c, StringComparer.Ordinal)) && spent.Count == runtimeSpent && cells.Count == all.Count - runtimeSpent
                      && !cells.Any(spent.Contains),
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
                    if (ok == (nearest == null || distance >= PartyCreationFlow.TakenWithin)) { consistent++; }   // S5 (answer 4): taken within the ring's radius (K333: the chamber's 0.06)
                    ring.Append(F(" {0}{1}:{2}@{3:0.000}", ok ? "" : "x", step, nearest, distance));
                }
                Check(clear > 0 && consistent == PoliSimTheme.CreatedInkSteps, F("the ink ring's fence: {0} of {1} steps clear, each TAKEN cell within {2:0.00} of a seated party's drawn ink (the return's answer 4) -{3}", clear, PoliSimTheme.CreatedInkSteps, PartyCreationFlow.TakenWithin, ring));

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

                // ── S5 (Design's return of 2026-10-30): the constituency on the HQ's tile map, the glyphs ──
                CampaignTiles.TileMap regionMap = PartyCreationFlow.RegionMap(se);
                var untiled = regions.Where(name => !PartyCreationFlow.TileOf(regionMap, se, name).HasValue).ToList();
                Check(regionMap != null && regionMap.Key == "se_valkretsar" && untiled.Count == 0 && PartyCreationFlow.RegionMap(CountryId.Germany) == null,
                    F("answer 6: every one of the {0} valkretsar is a tile on the HQ's map ({1}), none unplaced{2}; Germany offers no map (K166: a Regional party there reads nationally)",
                        regions.Length, regionMap != null ? regionMap.Key : "none", untiled.Count > 0 ? " - unplaced " + string.Join(", ", untiled) : string.Empty));
                var glyphStems = new List<string>();
                for (int s = 0; s < 5; s++) { glyphStems.Add("stat_" + PartyCreationFlow.StatGlyph(s) + "_16"); glyphStems.Add("stat_" + PartyCreationFlow.StatGlyph(s) + "_32"); }
                foreach (PartyOrigins.Preset p in PartyOrigins.All) { glyphStems.Add("origin_" + PartyCreationFlow.OriginGlyph(p.Origin) + "_20"); glyphStems.Add("origin_" + PartyCreationFlow.OriginGlyph(p.Origin) + "_40"); }
                var unloaded = glyphStems.Where(stem => IconLibrary.PartyCreationGlyph(stem) == null).ToList();
                Check(unloaded.Count == 0 && glyphStems.Distinct().Count() == 22,
                    F("answers 1 and 2: the five stat glyphs and the six origin glyphs, both sizes, load through IconLibrary.PartyCreationGlyph (Art/UI/PartyCreation): {0} of {1}{2}", glyphStems.Count - unloaded.Count, glyphStems.Count,
                        unloaded.Count > 0 ? " - drawn as their word: " + string.Join(", ", unloaded) : string.Empty));
                int whiteCut = cells.Count(c => IconLibrary.IsWhiteCutCell(c));
                Check(whiteCut == cells.Count, F("answer 5: every offered cell is a white-cut cell, tinted at every draw site (Tools/s5_cell_whitecut.pl --check proves the pixels): {0} of {1}", whiteCut, cells.Count));

                // ── the slips ──
                PeopleSlips.Book book = PartyCreationFlow.Slips(d);
                var anchors = new List<string> { "candidate", "outside/yes", "outside/no", "compass", "predicted", "key" };
                for (int s = 0; s < PartyCreationFlow.StepCount; s++) { anchors.Add("step/" + s); }
                for (int s = 0; s < 5; s++) { anchors.Add("stat/" + s); }
                for (int s = 0; s < 5; s++) { anchors.Add("statkey/" + s); }   // S5 (answer 1): the ORIGIN caption's key
                anchors.Add("region/national"); anchors.Add("region/picked");
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
                CreatedParties.Clear();

                CheckUsForm(Check);
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

        /// <summary>
        /// PS-6 US-33 (SP-6a): **THE FLOW'S US FORM, READ THROUGH THE MEMBERS ITS SCREEN DRAWS FROM** (`OwnNomineeFlow`). Offered for the USA alone, where the party
        /// form is not; a new draft heads the first ticket on that party's own point, unnamed; a ticket chosen moves the nominee onto its party's point, a party the
        /// flow does not offer changes nothing; the compass's placement goes to a tenth and stays inside the range; the unnamed draft is refused with its reason and
        /// nothing is kept; every anchor the four steps register has its slip; the prediction reads the moved point - at the party's own point it is the real
        /// prediction exactly - and leaves the registry and the real prediction as they were; the commit registers the nominee and adds no party to the roster or
        /// the campaign's cast. Inside the own nominees' scope, so the process's set is put back whatever happens.
        /// </summary>
        private static void CheckUsForm(Action<bool, string> Check)
        {
            using (OwnNominees.Scope())
            {
                OwnNominees.Clear();
                const CountryId us = CountryId.USA;
                var offeredElsewhere = new List<string>();
                foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId))) { if (id != us && OwnNomineeFlow.Offered(id)) { offeredElsewhere.Add(id.ToString()); } }
                Check(OwnNomineeFlow.Offered(us) && !PartyCreationFlow.Offered(us) && offeredElsewhere.Count == 0 && OwnNomineeFlow.StepCount == OwnNomineeFlow.StepHeads.Length,
                    F("US-33: the flow's US form is offered for the USA alone, and the party form not there{0} - {1} steps: {2}",
                        offeredElsewhere.Count > 0 ? "; ALSO: " + string.Join(", ", offeredElsewhere) : string.Empty, OwnNomineeFlow.StepCount, string.Join(", ", OwnNomineeFlow.StepHeads)));

                PoliticalParty[] real = PartySystems.RealRoster(us);
                PoliticalParty rep = real.First(p => p.Abbrev == "REP"), dem = real.First(p => p.Abbrev == "DEM");
                OwnNominee d = OwnNomineeFlow.NewDraft(us);
                Check(d.Party == "REP" && d.LrEcon == rep.LrEcon && d.Galtan == rep.Galtan && string.IsNullOrEmpty(d.Name),
                    F("US-33: a new draft heads the first ticket offered (REP) on its own point ({0:0.00}, {1:0.00}), unnamed", d.LrEcon, d.Galtan));
                OwnNomineeFlow.ChooseTicket(d, "DEM");
                bool onDem = d.Party == "DEM" && d.LrEcon == dem.LrEcon && d.Galtan == dem.Galtan;
                OwnNomineeFlow.ChooseTicket(d, "S");
                Check(onDem && d.Party == "DEM", F("US-33: DEM chosen, the nominee on DEM's own point ({0:0.00}, {1:0.00}); a party the flow does not offer changes nothing", dem.LrEcon, dem.Galtan));
                OwnNominees.TryRangeOf(us, "DEM", out OwnNominees.Range range);
                OwnNomineeFlow.Place(d, 10f, 0f);
                bool clamped = d.LrEcon == range.EconHi && d.Galtan == range.SocLo;
                OwnNomineeFlow.Place(d, range.EconLo + 0.46f, range.SocLo + 0.54f);
                Check(clamped && Math.Abs(d.LrEcon - (range.EconLo + 0.5f)) < 1e-4 && Math.Abs(d.Galtan - (range.SocLo + 0.5f)) < 1e-4,
                    F("US-33: the compass's placement - (10, 0) kept inside DEM's range at ({0:0.0}, {1:0.0}); a point inside it to a tenth, ({2:0.00}, {3:0.00})", range.EconHi, range.SocLo, d.LrEcon, d.Galtan));
                bool unnamedRefused = !OwnNomineeFlow.TryCommit(d, out string why);
                Check(OwnNomineeFlow.Missing(d) == "name" && unnamedRefused && why == "the name is not set" && !OwnNominees.Any(us),
                    "US-33: the unnamed draft is refused, with its reason, and nothing is kept: " + why);

                // the slips
                PeopleSlips.Book book = OwnNomineeFlow.Slips(d, 50.0, 49.0);
                var anchors = new List<string> { "compass", "predicted", "name" };
                for (int s = 0; s < OwnNomineeFlow.StepCount; s++) { anchors.Add("step/" + s); }
                foreach (string ticket in OwnNominees.Tickets(us)) { anchors.Add("ticket/" + ticket); }
                foreach (PoliticalParty p in real) { anchors.Add("party/" + p.Abbrev); }
                var missing = anchors.Where(a => !book.Anchors.TryGetValue(a, out SlipContent c) || c.Lines.Count == 0).ToList();
                Check(missing.Count == 0, F("US-33: every anchor the four steps register has its slip, with words: {0} anchors{1}", anchors.Count, missing.Count > 0 ? " - missing " + string.Join(", ", missing) : string.Empty));

                // the prediction reads the moved point and leaves nothing
                NationalElection.TryPredictShares(us, out Dictionary<string, double> before);
                double atParty = OwnNomineeFlow.PredictedShare(d, atPartyPoint: true);
                double moved = OwnNomineeFlow.PredictedShare(d);
                NationalElection.TryPredictShares(us, out Dictionary<string, double> after);
                Check(atParty.Equals(before["DEM"] * 100.0) && !double.IsNaN(moved) && !moved.Equals(atParty) && !OwnNominees.Any(us) && before.Count == after.Count && before.All(kv => after[kv.Key].Equals(kv.Value)),
                    F("US-33: the placement's prediction - DEM {0:0.0000} % at its own point (the real prediction exactly), {1:0.0000} % with the nominee at ({2:0.0}, {3:0.0}); the registry and the real prediction are as they were", atParty, moved, d.LrEcon, d.Galtan));

                // the commit
                string[] castBefore = LiveCampaignSetup.Keys(us);
                d.Name = "Maria Reyes";
                bool committed = OwnNomineeFlow.TryCommit(d, out string refused);
                Check(committed && OwnNominees.TryOf(us, out OwnNominee kept) && ReferenceEquals(kept, d) && PartySystems.For(us).Count == real.Length && LiveCampaignSetup.Keys(us).SequenceEqual(castBefore)
                      && OwnNomineeFlow.TicketLine(d) == dem.ShortName + " · " + dem.Name,
                    F("US-33: the named draft commits - {0} heads the {1} ticket; no party is added to the roster ({2}) or the campaign's cast{3}", d.Name, OwnNomineeFlow.TicketLine(d), PartySystems.For(us).Count, committed ? string.Empty : " - refused: " + refused));
            }
            NationalElection.TryPredictShares(CountryId.USA, out _);
        }
    }
}
