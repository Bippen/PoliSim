using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §697 (PS-4): **GERMANY'S CAMPAIGN ON THE LÄNDER, STAGED AND RUN.** On Germany's own start (6 November 2024): (a) the calendar - the run-up
    /// opens on the first whole week after the start, never before it, the campaign proper eight weeks before polling day (29 December, two days
    /// after the dissolution), and Sweden's calendar is the standard one, unchanged; (b) the staging - every party of the roster on the sixteen
    /// Länder, the audience each Land's valid Zweitstimmen and the mobilisable its registered electorate, both summing to the Bund's 2021 totals;
    /// (c) the candidacy facts as each party's reach - the CSU in Bayern alone, the CDU in the other fifteen, the SSW in Schleswig-Holstein, the
    /// Grüne nowhere in Saarland on 2021's Länder, the BSW and the FDP everywhere; (d) the salience EB102's; (e) the casts the CHES rule's; (f) every
    /// staged office where its party stands; (g) a seeded campaign run whole - no party's local act, AI or scripted, lands where it does not stand,
    /// and THE PLANTED PROOF: a scripted CSU rally in Hamburg lands when the candidacy is lifted and is skipped when it stands; (h) the run-up
    /// refuses an office where the party has no list; (i) a German GAME's own day path, the CDU the player's party, stepped from the start through
    /// polling day - the run-up begun on the calendar's day, the campaign opened on the Länder, polling day raised on 23 February 2025 with the
    /// campaign there for the count, and the finished run dropped the day after with its result kept, though no polling day follows the snap.
    /// </summary>
    public static class GermanCampaignDiagnostic
    {
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== GermanCampaignDiagnostic (§697): the campaign on the Länder - staged on the snap start, each party only where it stands ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                using (SimulationManager.EpochScope())
                {
                    WorldClock.ApplyStart(CountryId.Germany);
                    DateTime start = WorldClock.StartDate(CountryId.Germany);

                    // (a) the calendar
                    CampaignCalendar cal = CampaignCalendar.FromWorldStart(WorldClock.LatestElectionDay(CountryId.Germany), start);
                    Check(cal.ElectionDate == new DateTime(2025, 2, 23) && cal.CampaignStart == new DateTime(2024, 12, 29) && cal.PreCampaignStart >= start && (cal.PreCampaignStart - start).TotalDays < 7.0,
                        F("the snap calendar: run-up {0:yyyy-MM-dd} (the start {1:yyyy-MM-dd}, never before it), campaign {2:yyyy-MM-dd} (the dissolution was 2024-12-27), polling day {3:yyyy-MM-dd}", cal.PreCampaignStart, start, cal.CampaignStart, cal.ElectionDate));
                    var sweStandard = new CampaignCalendar(WorldClock.LatestElectionDay(CountryId.Sweden));
                    CampaignCalendar swe = CampaignCalendar.FromWorldStart(WorldClock.LatestElectionDay(CountryId.Sweden), WorldClock.StartDate(CountryId.Sweden));
                    Check(swe.PreCampaignStart == sweStandard.PreCampaignStart && swe.CampaignStart == sweStandard.CampaignStart && swe.PreCampaignWeeks == CampaignCalendar.DefaultPreCampaignWeeks,
                        F("Sweden's calendar is the standard one, unchanged: run-up {0:yyyy-MM-dd}, {1} weeks", swe.PreCampaignStart, swe.PreCampaignWeeks));

                    // (b) the staging
                    var none = new (int, int, Scandal)[0];
                    bool staged = LiveCampaignSetup.TryFor(CountryId.Germany, none, cal, out CampaignRun.Setup setup, out string note, onVoteModelCompatibility: true);
                    string[] keys = LiveCampaignSetup.Keys(CountryId.Germany);
                    Check(staged && setup.Parties != null && setup.Parties.Length == keys.Length && setup.Regions.Length == 16 && setup.Stands != null,
                        F("staged on the vote model: {0} parties on {1} Länder", staged ? setup.Parties.Length : 0, staged ? setup.Regions.Length : 0));
                    if (!staged) { throw new InvalidOperationException("no German campaign staged: " + note); }
                    double audience = 0.0, eligible = 0.0;
                    foreach (RegionAudience r in setup.Regions) { audience += r.Audience; eligible += r.Eligible; }
                    Check(Math.Abs(audience - 46_298_387.0) < 0.5 && Math.Abs(eligible - 61_172_771.0) < 0.5,
                        F("the Länder's audiences sum to 2021's valid Zweitstimmen ({0:N0}) and their electorates to its Wahlberechtigte ({1:N0}) - the seated chamber's, 2021", audience, eligible));

                    // (c) the candidacy facts as reach
                    int Land(string name) => Array.FindIndex(setup.Regions, r => r.Name == name);
                    int Party(string key) => Array.IndexOf(keys, key);
                    int Count(int p, out string only) { int n = 0; only = null; for (int r = 0; r < 16; r++) { if (setup.StandsIn(p, r)) { n++; only = setup.Regions[r].Name; } } return n; }
                    int bayern = Land("Bayern"), sh = Land("Schleswig-Holstein"), saarland = Land("Saarland"), hamburg = Land("Hamburg");
                    int csu = Party("CSU"), cdu = Party("CDU"), ssw = Party("SSW"), grune = Party("Grune"), bsw = Party("BSW"), fdp = Party("FDP");
                    Check(Count(csu, out string csuOnly) == 1 && csuOnly == "Bayern", "the CSU stands in Bayern alone (" + csuOnly + ")");
                    Check(Count(cdu, out _) == 15 && !setup.StandsIn(cdu, bayern), "the CDU stands in the fifteen other Länder");
                    Check(Count(ssw, out string sswOnly) == 1 && sswOnly == "Schleswig-Holstein", "the SSW stands in Schleswig-Holstein alone");
                    Check(Count(grune, out _) == 15 && !setup.StandsIn(grune, saarland), "on 2021's Länder the Grüne stand nowhere in Saarland (their 2021 list rejected - a limit stated: their 2025 list stood)");
                    Check(Count(bsw, out _) == 16 && Count(fdp, out _) == 16, "the BSW (no 2021 record) and the FDP stand everywhere");

                    // (d) the salience, (e) the casts
                    double[] s = setup.TrueSalience;
                    Check(s[(int)IssueId.Immigration] == 0.35 && s[(int)IssueId.Economy] == 0.31 && s[(int)IssueId.Housing] == 0.15 && double.IsNaN(s[(int)IssueId.Climate]),
                        "the salience EB102's (fieldwork 10-31 Oct 2024): immigration .35, economy .31, housing .15, no other slot");
                    int wrongCast = 0;
                    for (int p = 0; p < keys.Length; p++) { if (setup.Parties[p].Personality != LiveCampaignSetup.PersonalityOf(CountryId.Germany, keys[p])) { wrongCast++; } }
                    Check(wrongCast == 0 && setup.Parties[csu].Personality == AiPersonality.Professional && setup.Parties[bsw].Personality == AiPersonality.Populist,
                        F("every party cast by the CHES rule (§695): CSU {0}, BSW {1}, AfD {2}, Grüne {3}", setup.Parties[csu].Personality, setup.Parties[bsw].Personality, setup.Parties[Party("AfD")].Personality, setup.Parties[grune].Personality));

                    // (f) the staged offices
                    int strayOffices = 0;
                    for (int p = 0; p < keys.Length; p++) { foreach (int r in setup.Parties[p].Offices) { if (!setup.StandsIn(p, r)) { strayOffices++; } } }
                    Check(strayOffices == 0 && setup.Parties[csu].Offices.Length >= 1 && Array.TrueForAll(setup.Parties[csu].Offices, r => r == bayern),
                        F("every staged office where its party stands - the CSU's {0} in Bayern", setup.Parties[csu].Offices.Length));

                    // (g) a seeded run: no local act lands where its party does not stand
                    CampaignRun.Result run = CampaignAiHarness.RunSeeded(setup, 777);
                    int local = 0, stray = 0; string firstStray = null;
                    for (int p = 0; p < run.Parties.Length; p++)
                    {
                        foreach (CampaignRun.DecisionRecord d in run.Parties[p].Log)
                        {
                            if (Array.IndexOf(CampaignActions.TheEight, d.Kind) < 0 || !CampaignActions.Spec(d.Kind).IsLocal) { continue; }   // a commissioned poll is logged too, and is not one of the eight
                            string land = d.Target.Split(new[] { " / " }, StringSplitOptions.None)[0];
                            int r = Land(land);
                            if (r < 0) { continue; }
                            local++;
                            if (!setup.StandsIn(p, r)) { stray++; firstStray = firstStray ?? keys[p] + " " + d.Kind + " in " + land; }
                        }
                    }
                    Check(local > 0 && stray == 0, F("a campaign run whole (seed 777, {0} days): {1} local acts, {2} where the party does not stand{3}", run.DaysRun, local, stray, firstStray != null ? " - FIRST: " + firstStray : string.Empty));
                    var national = new StringBuilder("    read      the idle campaign's national shares against its prior (pp):");
                    for (int p = 0; p < keys.Length; p++) { national.Append(F(" {0} {1:F1}/{2:F1}", keys[p], run.FinalShares[p] * 100.0, setup.PriorShares[p] * 100.0)); }
                    sb.Append(national).Append('\n');

                    // THE PLANTED PROOF: the CSU scripted to rally in Hamburg every day - skipped where it stands, landing where the candidacy is lifted
                    CampaignActions.ActionSpec rally = CampaignActions.Spec(CampaignActionKind.Rally);
                    Func<int, AiDecision[]> script = d => new[] { new AiDecision(CampaignActionKind.Rally, new CampaignActions.ActionTarget(hamburg, -1, null), "Hamburg", rally.MoneyCost, rally.Hours, 0.0, false) };
                    LiveCampaignSetup.TryFor(CountryId.Germany, none, cal, out CampaignRun.Setup scripted, out _, onVoteModelCompatibility: true, playerParty: csu, playerScript: script);
                    CampaignRun.Setup lifted = new CampaignRun.Setup(scripted.Calendar, scripted.Parties, scripted.PriorShares, scripted.LoyaltyPerParty, scripted.Compatibility, scripted.TrueSalience,
                        scripted.NationalAudience, scripted.Regions, scripted.PublicHouse, scripted.PublicPollEveryDays, scripted.InternalHouse, scripted.ElectorateLoyalty, scripted.Outlets,
                        scripted.DebateDays, scripted.Scandals, scripted.LiveScandalRatePerPartyDay, scripted.RecordShift, scripted.Families, scripted.AwarenessStart, scripted.Grouping,
                        scripted.Positions, stands: null);
                    int Rallies(CampaignRun.Result r) { int n = 0; foreach (CampaignRun.DecisionRecord d in r.Parties[csu].Log) { if (d.Kind == CampaignActionKind.Rally && d.Target.StartsWith("Hamburg / ", StringComparison.Ordinal)) { n++; } } return n; }
                    int withCandidacy = Rallies(CampaignAiHarness.RunSeeded(scripted, 777));
                    int liftedRallies = Rallies(CampaignAiHarness.RunSeeded(lifted, 777));
                    Check(withCandidacy == 0 && liftedRallies > 0, F("THE PLANTED PROOF: the CSU scripted to rally in Hamburg every day - {0} rallies land where it stands (Bayern only), {1} where the candidacy is lifted", withCandidacy, liftedRallies));

                    // §699: THE PLANTED PROOF of national reach - a Land-only party scripted to air national television every day. A national act reaches
                    // only the voters with the party on their ballot. §704: the proof is the CSU's (Bayern alone) - the SSW, which the survey does not place,
                    // keeps its prior and no act moves it, so it can prove nothing about reach.
                    int csuAt = Party("CSU"), sswAt = Party("SSW");
                    CampaignActions.ActionSpec tv = CampaignActions.Spec(CampaignActionKind.TelevisionAd);
                    Func<int, AiDecision[]> tvScript = d => new[] { new AiDecision(CampaignActionKind.TelevisionAd, CampaignActions.ActionTarget.National(null), "Television", tv.MoneyCost, tv.Hours, 0.0, false) };
                    LiveCampaignSetup.TryFor(CountryId.Germany, none, cal, out CampaignRun.Setup tvStaged, out _, onVoteModelCompatibility: true, playerParty: csuAt, playerScript: tvScript);
                    CampaignRun.Setup tvLifted = new CampaignRun.Setup(tvStaged.Calendar, tvStaged.Parties, tvStaged.PriorShares, tvStaged.LoyaltyPerParty, tvStaged.Compatibility, tvStaged.TrueSalience,
                        tvStaged.NationalAudience, tvStaged.Regions, tvStaged.PublicHouse, tvStaged.PublicPollEveryDays, tvStaged.InternalHouse, tvStaged.ElectorateLoyalty, tvStaged.Outlets,
                        tvStaged.DebateDays, tvStaged.Scandals, tvStaged.LiveScandalRatePerPartyDay, tvStaged.RecordShift, tvStaged.Families, tvStaged.AwarenessStart, tvStaged.Grouping,
                        tvStaged.Positions, stands: null);
                    double csuReach = tvStaged.StandingShare(csuAt);
                    double csuStaged = CampaignAiHarness.RunSeeded(tvStaged, 777).FinalShares[csuAt];
                    double csuLifted = CampaignAiHarness.RunSeeded(tvLifted, 777).FinalShares[csuAt];
                    Check(csuStaged <= csuReach && csuLifted > csuStaged && Math.Abs(setup.StandingShare(Party("CDU")) + setup.StandingShare(Party("CSU")) - 1.0) < 1e-9,
                        F("THE PLANTED PROOF of reach: the CSU scripted to air national television daily ends at {0:F2} % with its candidacy (its reach of the country {1:F2} %, Bayern's), {2:F2} % where it is lifted and the whole country hears it; the CDU's and CSU's reaches sum to the country",
                            csuStaged * 100.0, csuReach * 100.0, csuLifted * 100.0));

                    // §704 (round 4 follow-up 1): THE SSW IS BOUNDED TO ITS LAND, NOT ZEROED. The survey does not place it (no CHES 2024 position) and it did not
                    // stand in 2017, so the spatial layer gave it nothing and the 2017-2021 pair no loyal base - its 2021 prior was discarded on day 0 and it
                    // ended every German campaign at 0.00 %. A party the survey does not place keeps its prior: day 0 is its prior exactly, it ends above zero,
                    // and the reach cap bounds it (3.81 %, Schleswig-Holstein's) without binding.
                    double sswReach = setup.StandingShare(sswAt);
                    double[] blended0 = PreferenceModel.Preference(setup.Compatibility, setup.PriorShares, setup.LoyaltyPerParty);
                    double priorSum0 = 0.0; foreach (double p0 in setup.PriorShares) { priorSum0 += p0; }
                    double sswPrior = setup.PriorShares[sswAt] / priorSum0;
                    double sswAuto = CampaignAiHarness.RunSeeded(setup, 777).FinalShares[sswAt];
                    // the per-party blend renormalises: each party's weight is lambda_i * prior_i + (1 - lambda_i) * spatial_i over their total - the SSW's
                    // weight is its prior alone (lambda 1), so its day-0 share is its prior over that total
                    double[] spatial0 = PreferenceModel.PersuadedShares(setup.Compatibility);
                    double blendTotal = 0.0;
                    for (int i = 0; i < spatial0.Length; i++)
                    {
                        double lambda = ElectionScales.Clamp(setup.LoyaltyPerParty[i]) / ElectionScales.Max;
                        blendTotal += lambda * setup.PriorShares[i] / priorSum0 + (1.0 - lambda) * spatial0[i];
                    }
                    Check(Math.Abs(blended0[sswAt] - sswPrior / blendTotal) < 1e-9 && sswAuto > 0.0 && sswAuto <= sswReach && setup.LoyaltyPerParty[sswAt] == NationalElection.UnplacedLoyalty,
                        F("the SSW keeps its prior: its day-0 weight is its 2021 share of the prior, {1:F3} %, alone (full loyalty, no spatial share) - {0:F3} % after the blend's renormalisation; the campaign's end {2:F3} % - above zero, bounded by its reach {3:F2} %",
                            blended0[sswAt] * 100.0, sswPrior * 100.0, sswAuto * 100.0, sswReach * 100.0));

                    // (h) the run-up refuses an office where the party has no list
                    PreCampaignRun.State pre = PreCampaignRun.Begin(scripted, csu, new Random(1));
                    PreCampaignRun.StepDay(pre, new[] { new PreCampaignRun.Decision(CampaignActionKind.EstablishOffice, hamburg) });
                    string refusal = pre.Log.Count > 0 ? pre.Log[pre.Log.Count - 1].Refusal : null;
                    Check(refusal == "the party has no list there" && !pre.HasOffice(hamburg), "the run-up refuses the CSU an office in Hamburg: " + (refusal ?? "NOTHING REFUSED"));

                    // (i) A GERMAN GAME'S OWN DAY PATH - a manager opened on the snap start, the CDU the player's party, stepped day by day as the
                    // game steps it (PlayerSmoke's loop) from 6 November into the campaign: the film holds its clock at the start and cannot reach this
                    var host = new UnityEngine.GameObject("GermanCampaignDiagnostic");
                    try
                    {
                        SimulationRandom.Seed(777);
                        EnergyMarket.ResetCalibration();
                        World world = WorldFactory.CreateDefault();
                        SimulationManager sim = host.AddComponent<SimulationManager>();
                        sim.SetWorld(world);
                        sim.PlayerCountryId = CountryId.Germany;
                        world.GetCountry(CountryId.Germany).PlayerPartyAbbrev = "CDU";
                        var noDecisions = new Dictionary<CountryId, PolicyDecision>();
                        DateTime? runUpSeen = null, campaignSeen = null, pollingDaySeen = null;
                        int regionsSeen = 0, cduLands = 0;
                        bool heldOnPollingDay = false, droppedAfter = false, resultAfter = false;
                        for (int step = 0; step < 130 && sim.CurrentDate < new DateTime(2025, 2, 25); step++)
                        {
                            if (sim.AdvanceDay()) { sim.AdvanceTurn(noDecisions); }
                            sim.AdvanceCountryDayTick(CountryId.Germany);
                            if (sim.PollingDayToday) { pollingDaySeen = sim.CurrentDate; heldOnPollingDay = sim.PlayerCampaign != null; }   // the controller counts it on this day, from PlayerCampaign
                            if (sim.CurrentDate == new DateTime(2025, 2, 24)) { droppedAfter = sim.PlayerCampaign == null && sim.PlayerPreCampaign == null; resultAfter = sim.PlayerCampaignResult != null; }
                            if (runUpSeen == null && sim.PlayerPreCampaign != null) { runUpSeen = sim.CurrentDate; }
                            if (campaignSeen == null && sim.PlayerCampaign != null)
                            {
                                campaignSeen = sim.CurrentDate;
                                CampaignRun.Setup live = sim.PlayerCampaign.Setup;
                                regionsSeen = live.Regions.Length;
                                int me = Array.IndexOf(LiveCampaignSetup.Keys(CountryId.Germany), "CDU");
                                for (int r = 0; r < regionsSeen; r++) { if (live.StandsIn(me, r)) { cduLands++; } }
                            }
                        }
                        Check(runUpSeen.HasValue && sim.CampaignRecord != null && sim.CampaignRecord.StartDate == cal.PreCampaignStart && campaignSeen.HasValue && regionsSeen == 16 && cduLands == 15,
                            F("a German game, the CDU the player's party, stepped from {0:yyyy-MM-dd}: its run-up begun on the calendar's {1:yyyy-MM-dd} (the record's {2}), its campaign opened {3} on {4} Länder, the CDU's reach {5} of them",
                                start, cal.PreCampaignStart, sim.CampaignRecord != null ? sim.CampaignRecord.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "NONE",
                                campaignSeen.HasValue ? campaignSeen.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "NEVER", regionsSeen, cduLands));
                        // the review's defect 3: after the snap Germany has no next polling day - the finished campaign must still be dropped, its result kept
                        Check(pollingDaySeen == new DateTime(2025, 2, 23) && heldOnPollingDay && droppedAfter && resultAfter,
                            F("polling day raised on {0} with the campaign there for the count; on 2025-02-24 the run dropped ({1}) and its result kept ({2}) - though no polling day follows the snap",
                                pollingDaySeen.HasValue ? pollingDaySeen.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "NEVER", droppedAfter, resultAfter));
                    }
                    finally { UnityEngine.Object.DestroyImmediate(host); EnergyMarket.ResetTurnState(); }

                    // §699: THE FILM'S OWN CASE - a German game with the SSW as the player's party (the §698 film's), stepped through polling day as the game
                    // steps it; its campaign's result, the count election night reads, printed - and the SSW held under its reach of the country
                    var hostSsw = new UnityEngine.GameObject("GermanCampaignDiagnostic SSW");
                    try
                    {
                        SimulationRandom.Seed(777);
                        EnergyMarket.ResetCalibration();
                        World worldSsw = WorldFactory.CreateDefault();
                        SimulationManager simSsw = hostSsw.AddComponent<SimulationManager>();
                        simSsw.SetWorld(worldSsw);
                        simSsw.PlayerCountryId = CountryId.Germany;
                        worldSsw.GetCountry(CountryId.Germany).PlayerPartyAbbrev = "SSW";
                        var none2 = new Dictionary<CountryId, PolicyDecision>();
                        for (int step = 0; step < 130 && simSsw.CurrentDate < new DateTime(2025, 2, 24); step++)
                        {
                            if (simSsw.AdvanceDay()) { simSsw.AdvanceTurn(none2); }
                            simSsw.AdvanceCountryDayTick(CountryId.Germany);
                        }
                        CampaignRun.Result sswResult = simSsw.PlayerCampaignResult;
                        var shares = new StringBuilder("    read      the SSW as the player's party - its game's campaign result (the count election night reads), %:");
                        int sswKey = Array.IndexOf(keys, "SSW");
                        for (int p = 0; sswResult != null && p < keys.Length && p < sswResult.FinalShares.Length; p++) { shares.Append(F(" {0} {1:F1}", keys[p], sswResult.FinalShares[p] * 100.0)); }
                        sb.Append(shares).Append('\n');
                        double sswShare = sswResult != null && sswKey >= 0 ? sswResult.FinalShares[sswKey] : double.NaN;
                        // §704: bounded, not zeroed - and the count seats it as the Bundestag's did, a national minority's list exempt from the 5 % line
                        int sswSeats = -1;
                        if (sswResult != null)
                        {
                            ElectionRecord sswNight = NationalElection.Run(CountryId.Germany, 0, NationalElection.SharesFromCampaign(CountryId.Germany, keys, sswResult.FinalShares), new DateTime(2025, 2, 23));
                            sswSeats = sswNight.Seats.TryGetValue("SSW", out int won) ? won : -1;
                        }
                        Check(sswResult != null && sswShare > 0.0 && sswShare <= setup.StandingShare(sswKey) && sswSeats >= 1,
                            F("a German game played as the SSW: its campaign ends at {0:F2} % - above zero, within its reach of the country ({1:F2} %) - and the count seats it with {2} (the Bundestag's 2025: 1 seat on 0.153 %; §698's film had 60 of 630, §699's 0)",
                                sswShare * 100.0, setup.StandingShare(sswKey) * 100.0, sswSeats));
                    }
                    finally { UnityEngine.Object.DestroyImmediate(hostSsw); EnergyMarket.ResetTurnState(); }
                    sb.Append("    note     ").Append(note.Replace("\n", "\n             ").Trim()).Append('\n');
                }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw: ").Append((e.InnerException ?? e).Message).Append('\n'); }
            sb.Append(failures == 0 ? "    CLEAN\n" : F("    {0} failure(s)\n", failures));
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
