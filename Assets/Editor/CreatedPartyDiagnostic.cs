using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §671 (SP-3, the spec's S3, structural): **A CREATED PARTY IN THE MODEL.** Asserted: none registered, nothing reads differently (`For` is the very
    /// array it was; the history's length stands) - the no-policy path stays byte-inert; the key rules (ASCII, no roster key, no duplicate; a Splinter
    /// needs a real parent); a registered party is APPENDED - every real index stands; the six origins and the pip table are data; the spec's third - a
    /// Splinter begins at its inherited slice of the parent's last result, moved from the parent; and a save carries the party back. MEASURED, not
    /// asserted (§674, the units corrected - the prediction returns fractions): the spec's first (a party on a real party's positions competes for THAT
    /// party's voters) - the model has no source term, a newcomer's persuaded share comes out of every party through the renormalisation; and its second
    /// (a Grassroots newcomer polls near zero until it campaigns) - it does not: with no loyal base it takes its whole persuaded share at once.
    /// </summary>
    public static class CreatedPartyDiagnostic
    {
        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== CreatedPartyDiagnostic (§671, SP-3): a created party in the model ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            using IDisposable created = CreatedParties.Scope();
            WorldClock.ApplyStart(CountryId.Sweden);
            var hosts = new List<GameObject>();
            try
            {
                CreatedParties.Clear();
                PoliticalParty[] real = PartySystems.RealRoster(CountryId.Sweden);

                // Inert with none registered.
                PartySystems.TryHistory(CountryId.Sweden, out double[] baseLatest, out double[] basePrevious);
                NationalElection.TryPredictShares(CountryId.Sweden, out Dictionary<string, double> baseShares);
                Check(ReferenceEquals(PartySystems.For(CountryId.Sweden), real) && baseLatest.Length == real.Length,
                    F("none registered: For(Sweden) is the very roster array ({0} parties) and the history {1} long - the no-policy path reads nothing new", real.Length, baseLatest.Length));

                // The key rules.
                CreatedParty Make(string key) { var c = new CreatedParty { Country = CountryId.Sweden, Key = key, Name = "Testpartiet " + key, LeaderName = "A. Testsson" }; c.ApplyOrigin(PartyOrigin.Grassroots); return c; }
                Check(!CreatedParties.TryRegister(Make("np"), real, out string r1), "a lower-case key is refused: " + r1);
                Check(!CreatedParties.TryRegister(Make("M"), real, out string r2), "a real party's key is refused: " + r2);
                var orphan = Make("SPL"); orphan.ApplyOrigin(PartyOrigin.Splinter); orphan.ParentKey = "ZZ";
                Check(!CreatedParties.TryRegister(orphan, real, out string r3), "a Splinter with no real parent is refused: " + r3);

                // The origins and the pip table as data.
                bool origins = PartyOrigins.All.Length == 6 && PartyOrigins.FundingShare.Length == 5 && PartyOrigins.Offices.Length == 5 && PartyOrigins.Awareness.Length == 5
                               && PartyOrigins.ActivistShare.Length == 5 && PartyOrigins.LeaderAttribute.Length == 5;
                var g = Make("GR");
                Check(origins && g.Activists == PartyOrigins.Of(PartyOrigin.Grassroots).Activists, "the six origins and the five-pip parameter table are data, [AUTHORED-DRAFT]; an origin's preset sets the five stats");

                // (i) placed exactly on M: appended, and it competes for M's voters.
                var twin = Make("NM");
                int m = Array.FindIndex(real, p => p.Abbrev == "M");
                twin.PlaceOn(real[m]);
                Check(CreatedParties.TryRegister(twin, real, out string r4), "a valid party registers" + (r4 == null ? string.Empty : " - " + r4));
                IReadOnlyList<PoliticalParty> roster = PartySystems.For(CountryId.Sweden);
                bool indices = roster.Count == real.Length + 1 && roster[real.Length].Abbrev == "NM";
                for (int i = 0; i < real.Length; i++) { if (roster[i].Abbrev != real[i].Abbrev) { indices = false; } }
                Check(indices, F("it is APPENDED: For(Sweden) is {0} long, every real party at its own index, the created one last", roster.Count));
                PartySystems.TryHistory(CountryId.Sweden, out double[] twinLatest, out double[] twinPrevious);
                Check(twinLatest.Length == real.Length + 1 && twinLatest[real.Length] == 0.0 && twinPrevious[real.Length] == 0.0, "a newcomer's history is padded with prior 0 and previous 0 - no loyal base (§2.7)");
                // §674: TryPredictShares returns FRACTIONS - §671 printed them as percentages, so its "0.06 %" was 5.5 % and its (ii) passed on a fraction
                // (0.12 < 1.0) where the share is 12 %. Every share below is read ×100.
                if (NationalElection.TryPredictShares(CountryId.Sweden, out Dictionary<string, double> twinShares))
                {
                    string most = null; double mostLoss = double.MinValue;
                    var losses = new List<string>();
                    foreach (PoliticalParty p in real)
                    {
                        double loss = (baseShares[p.Abbrev] - twinShares[p.Abbrev]) * 100.0;
                        losses.Add(F("{0} {1:+0.00;-0.00}", p.Abbrev, -loss));
                        if (loss > mostLoss) { mostLoss = loss; most = p.Abbrev; }
                    }
                    sb.Append(F("    measured  (i) on M's positions, no loyal base: NM {0:0.00} %; the real parties move (pp) {1}; {2} loses the most - NOT asserted: the model has no source term, a newcomer's persuaded share comes out of every party through the renormalisation (§674)\n",
                        twinShares["NM"] * 100.0, string.Join(", ", losses), most));
                    Check(twinShares["NM"] > 0.0 && twinShares["NM"] < baseShares["M"], F("a party on M's positions is predicted a share, and less than M's own ({0:0.00} % against {1:0.00} %)", twinShares["NM"] * 100.0, baseShares["M"] * 100.0));
                }
                else { Check(false, "(i) the vote model predicts no shares with a created party registered"); }

                // (ii) measured: a Grassroots newcomer at (5, 5), before any campaign. §674: NOT near zero - with no loyal base it takes its whole persuaded share
                // at once; the vote model has no awareness term (Recognition reaches nothing). Recorded, not asserted.
                CreatedParties.Clear();
                var grass = Make("GRS");
                grass.LrEcon = 5f; grass.Galtan = 5f;
                CreatedParties.TryRegister(grass, real, out _);
                if (NationalElection.TryPredictShares(CountryId.Sweden, out Dictionary<string, double> grassShares))
                {
                    sb.Append(F("    measured  (ii) a Grassroots newcomer at (5, 5), no campaign: {0:0.00} % - NOT near zero: no loyal base means its whole persuaded share at once, and no awareness term holds it back (§674)\n", grassShares["GRS"] * 100.0));
                }

                // (iii) a Splinter begins at its inherited slice, moved from its parent.
                CreatedParties.Clear();
                var splinter = Make("SPS"); splinter.ApplyOrigin(PartyOrigin.Splinter); splinter.ParentKey = "S"; splinter.InheritedSlice = 0.25;
                int s = Array.FindIndex(real, p => p.Abbrev == "S");
                splinter.PlaceOn(real[s]);
                Check(CreatedParties.TryRegister(splinter, real, out string r5), "a Splinter of S registers" + (r5 == null ? string.Empty : " - " + r5));
                PartySystems.TryHistory(CountryId.Sweden, out double[] spLatest, out double[] spPrevious);
                double slice = baseLatest[s] * 0.25, sumBase = 0, sumSplit = 0;
                foreach (double v in baseLatest) { sumBase += v; }
                foreach (double v in spLatest) { sumSplit += v; }
                Check(Math.Abs(spLatest[real.Length] - slice) < 1e-9 && Math.Abs(spLatest[s] - (baseLatest[s] - slice)) < 1e-9 && Math.Abs(sumBase - sumSplit) < 1e-9 && spPrevious[real.Length] == 0.0,
                    F("(iii) a Splinter of S at a quarter begins at its inherited slice: prior {0:0.00} % (S's {1:0.00} % × 0.25), S down to {2:0.00} %, the total unchanged, no previous result", spLatest[real.Length], baseLatest[s], spLatest[s]));

                // The save carries it back.
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                var go = new GameObject("CreatedPartyDiagnostic"); hosts.Add(go);
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                string path = Path.Combine(Application.temporaryCachePath, "created_party_" + Guid.NewGuid().ToString("N") + ".json");
                try
                {
                    SaveGameService.SaveToFile(path, SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null));
                    CreatedParties.Clear();
                    SaveGame loaded = SaveGameService.LoadFromFile(path);
                    var go2 = new GameObject("CreatedPartyDiagnostic.B"); hosts.Add(go2);
                    SaveGameService.RestoreInto(go2.AddComponent<SimulationManager>(), loaded);
                    IReadOnlyList<CreatedParty> back = CreatedParties.Of(CountryId.Sweden);
                    Check(back.Count == 1 && back[0].Key == "SPS" && back[0].ParentKey == "S" && Math.Abs(back[0].InheritedSlice - 0.25) < 1e-12 && back[0].LrEcon == real[s].LrEcon
                          && PartySystems.For(CountryId.Sweden).Count == real.Length + 1,
                        "a save carries the created party back: cut, the set cleared, loaded and restored - the same key, parent, slice and positions, appended again");
                }
                finally { try { if (File.Exists(path)) { File.Delete(path); } if (File.Exists(path + ".bak")) { File.Delete(path + ".bak"); } } catch (Exception) { } }
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally
            {
                foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); }
                EnergyMarket.ResetTurnState();
                // the prediction derives the regions as a side effect: derive them again on the real roster, so no check after this reads a created party's
                CreatedParties.Clear();
                NationalElection.TryPredictShares(CountryId.Sweden, out _);
            }

            if (failures > 0) { Debug.LogError($"CREATED PARTY: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
