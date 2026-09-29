using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §675 (SP-3 part two): **A CREATED PARTY IN THE CAMPAIGN, ITS FUNDING AND THE SEAT ALLOCATOR.** Asserted: none registered, the campaign's cast is
    /// `LiveCampaignSetup.SwedenParties` exactly (every staging and digest stands); a created party joins the cast after the real eight with its origin's
    /// personality and a day 0 from its stats through the pip table - the war chest its Funding share of the real parties' and no state support (the
    /// party-support act pays on past elections), the volunteers its Activists share, its Organisation's offices, its Leader's candidate; the player's
    /// party index finds a created party; the count seats it by the same rule as every party - above the 4 % threshold and not below it.
    /// MEASURED, not asserted: the spec's (i) and (ii) re-tested on a seeded campaign, every party's change printed (§674 read the model: no source
    /// term, no awareness term).
    /// </summary>
    public static class CreatedPartyCampaignDiagnostic
    {
        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== CreatedPartyCampaignDiagnostic (§675, SP-3 part two): the campaign, funding, the seats ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            using IDisposable created = CreatedParties.Scope();
            var hosts = new List<GameObject>();
            try
            {
                WorldClock.ApplyStart(CountryId.Sweden);
                CreatedParties.Clear();
                PoliticalParty[] real = PartySystems.RealRoster(CountryId.Sweden);
                string[] cast0 = LiveCampaignSetup.Keys(CountryId.Sweden);
                Check(string.Join(",", cast0) == string.Join(",", LiveCampaignSetup.SwedenParties), "none registered: the campaign's cast is the eight exactly - " + string.Join(" ", cast0));

                // the game's own staging (SimulationManager's call): the vote model's positional compatibility (D-21), no player script - every party its cast AI.
                // ⚠ Not LiveCampaignSetup.Sweden() alone: that is the harness's fixed-point staging, compatibility derived from the PRIOR, where a newcomer's
                // prior 0 is compatibility 0 and it cannot move (§675's first run measured exactly that: 0.00 %, every real party unchanged).
                CampaignRun.Result Campaign()
                {
                    if (!LiveCampaignSetup.TryFor(CountryId.Sweden, new (int, int, Scandal)[0], null, out CampaignRun.Setup setup, out string why, onVoteModelCompatibility: true)) { throw new InvalidOperationException("no campaign staged: " + why); }
                    return CampaignRun.Simulate(setup, new System.Random(777), new System.Random(778), new System.Random(779));
                }
                CampaignRun.Result baseRun = Campaign();
                Dictionary<string, double> baseShares = new Dictionary<string, double>();
                for (int i = 0; i < cast0.Length; i++) { baseShares[cast0[i]] = baseRun.FinalShares[i]; }

                CreatedParty Make(string key) { var c = new CreatedParty { Country = CountryId.Sweden, Key = key, Name = "Testpartiet " + key, LeaderName = "A. Testsson" }; c.ApplyOrigin(PartyOrigin.Grassroots); return c; }
                string Decompose(string key, CampaignRun.Result run, string[] cast)
                {
                    var parts = new List<string>();
                    double total = 0.0; foreach (double v in run.FinalShares) { total += v; }
                    double scale = total > 1.5 ? 1.0 : 100.0;   // the campaign's shares on their own scale, read as percentages
                    for (int i = 0; i < cast.Length; i++) { parts.Add(F("{0} {1:0.00}{2}", cast[i], run.FinalShares[i] * scale, baseShares.TryGetValue(cast[i], out double b) ? F(" ({0:+0.00;-0.00})", (run.FinalShares[i] - b) * scale) : " (new)")); }
                    return string.Join(", ", parts);
                }

                // A twin of M: the cast, its day 0, and (i) on the campaign.
                int m = Array.FindIndex(real, p => p.Abbrev == "M");
                var twin = Make("NM"); twin.PlaceOn(real[m]);
                CreatedParties.TryRegister(twin, real, out _);
                string[] cast1 = LiveCampaignSetup.Keys(CountryId.Sweden);
                Check(cast1.Length == 9 && cast1[8] == "NM" && LiveCampaignSetup.PersonalityOf(8, "NM") == AiPersonality.Grassroots && LiveCampaignSetup.PersonalityOf(2, "M") == LiveCampaignSetup.SwedenPersonalities[2],
                    "a created party joins the cast after the real eight, with its origin's personality (Grassroots: grassroots); the real eight keep theirs");
                RegionAudience[] regions = LiveCampaignSetup.SwedenRegions(out double _);
                LiveCampaignSetup.TryCreatedDayZero("NM", regions, out double money, out int volunteers, out int[] offices, out CandidateProfile candidate);
                Check(Math.Abs(money - LiveCampaignSetup.WarChest * PartyOrigins.FundingShare[0]) < 1e-6 && volunteers == (int)Math.Round(LiveCampaignSetup.Volunteers * PartyOrigins.ActivistShare[4])
                      && offices.Length == PartyOrigins.Offices[1],
                    F("its day 0 from its stats: war chest {0:N0} kr (Funding 1: {1:P0} of the real parties' {2:N0}, no state support - the act pays on past elections), {3} volunteers (Activists 5), {4} office(s) (Organisation 2)",
                        money, PartyOrigins.FundingShare[0], LiveCampaignSetup.WarChest, volunteers, offices.Length));
                CampaignRun.Result twinRun = Campaign();
                sb.Append("    measured  (i) on the campaign, seeded (777), M's twin with the Grassroots stats: ").Append(Decompose("NM", twinRun, cast1)).Append('\n');

                // The player's party index finds a created party.
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                var go = new GameObject("CreatedPartyCampaignDiagnostic"); hosts.Add(go);
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                world.GetCountry(CountryId.Sweden).PlayerPartyAbbrev = "NM";
                Check(sim.PlayerPartyIndexForCampaign() == 8, F("the player's party index finds a created party: NM at {0}", sim.PlayerPartyIndexForCampaign()));

                // The count: the campaign's shares through the game's own election and allocator.
                Dictionary<string, double> counted = NationalElection.SharesFromCampaign(CountryId.Sweden, cast1, twinRun.FinalShares);
                ElectionRecord record = NationalElection.Run(CountryId.Sweden, 0, counted);
                int seatsTotal = 0; foreach (int s in record.Seats.Values) { seatsTotal += s; }
                double share = counted.TryGetValue("NM", out double v) ? v : 0.0;
                double sharePct = share <= 1.0 ? share * 100.0 : share;
                int nmSeats = record.Seats.TryGetValue("NM", out int ns) ? ns : 0;
                bool rule = sharePct >= SeatConversion.NationalThreshold * (SeatConversion.NationalThreshold < 1.0 ? 100.0 : 1.0) ? nmSeats > 0 : nmSeats == 0;
                Check(seatsTotal == PartySystems.ChamberSeats(CountryId.Sweden) && record.Seats.ContainsKey("NM") && rule,
                    F("the count allocates the created party by the same rule as every party: NM {0:0.00} % of the vote, {1} seat(s) of {2} (threshold {3})", sharePct, nmSeats, seatsTotal, SeatConversion.NationalThreshold));

                // (ii) on the campaign: a Grassroots newcomer at (5, 5) - the idle prediction and the campaign's result.
                CreatedParties.Clear();
                var grass = Make("GRS"); grass.LrEcon = 5f; grass.Galtan = 5f;
                CreatedParties.TryRegister(grass, real, out _);
                NationalElection.TryPredictShares(CountryId.Sweden, out Dictionary<string, double> idle);
                CampaignRun.Result grassRun = Campaign();
                string[] cast2 = LiveCampaignSetup.Keys(CountryId.Sweden);
                double gTotal = 0.0; foreach (double x in grassRun.FinalShares) { gTotal += x; }
                double gScale = gTotal > 1.5 ? 1.0 : 100.0;
                sb.Append(F("    measured  (ii) a Grassroots newcomer at (5, 5): the idle prediction {0:0.00} %, the seeded campaign's result {1:0.00} % - ", idle["GRS"] * 100.0, grassRun.FinalShares[8] * gScale))
                  .Append(Decompose("GRS", grassRun, cast2)).Append('\n');
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally
            {
                foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); }
                EnergyMarket.ResetTurnState();
                CreatedParties.Clear();
                NationalElection.TryPredictShares(CountryId.Sweden, out _);
            }

            if (failures > 0) { Debug.LogError($"CREATED PARTY CAMPAIGN: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
