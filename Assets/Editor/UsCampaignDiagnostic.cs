using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-30 (`docs/specs/USA_STAGE_PLAN.md`, US-30; Elias's ruling I3: "US-30 stages now"): **THE USA'S CAMPAIGN ON THE JURISDICTIONS, STAGED AND RUN**,
    /// on <see cref="GermanCampaignDiagnostic"/>'s pattern. On the USA's start (12 March 2024): (a) the calendar - the run-up from the start, the campaign
    /// proper eight weeks before the polling day of 3 U.S.C. §21(1); (b) the staging - both parties on the 51 jurisdictions of US-3's catalog, the audience
    /// each one's valid presidential votes at the previous election (2020), the electorate the Census Bureau's citizen voting-age population, every figure
    /// held to the file it was read from; (c) the ballot - both major nominees everywhere, and no campaigning party backs a minor candidate, each of whom
    /// stands in the count only where on the ballot of record (US-6c); (d) the salience the newest Gallup wave fielded before the campaign, held to the
    /// saved table, and the seed for a campaign after July 2026; (e) the casts the CHES rule's (§695); (f) every staged office where its party stands, the
    /// war chest the model's one table; (g) an idle campaign on the start's staging is the start's prediction; (h) A US GAME'S OWN DAY PATH, the REP the
    /// player's party, stepped from the start through the polling day - the run-up begun on the calendar's day, the campaign opened on the jurisdictions,
    /// the count reading the run's final shares (then the states, then the electors), and THE DONE-WHEN: the run's own staging replayed idle reproduces
    /// the day's prediction exactly, on the record the count was judged on; (i) a save cut mid-campaign, loaded into the game, replays the campaign and
    /// counts the same contest; (j) PS-6 US-26 composed: that game's next campaign (2028) stages its prior and its loyalty on the history rolled forward -
    /// the game's own 2024 House count - exactly as the 2028 prediction reads them, and run idle ends on that prediction.
    /// <para><b>PS-6 US-31</b> (R-US16 (b), RULED - I3; the plan's done-when "the campaign diagnostic shows the AI's local acts concentrated where the rule
    /// says, and a player's ground game in one state moving it by US-9's measured amount, able to flip it"): (k) the USA's staging declares the AI's
    /// targeting rule, its weights recomputed off planted polls; (l) every local act of a party that never looks lands in the rule's regions of its day, and
    /// the planted tooth (the rule stripped) lands outside them; (m) US-31's registered function (W-B11's curve, the count's own until K18) at US-9's maximal ground game on US-9's own map gives US-9's
    /// measured amount as its card prints it (PLAN_PREREG, <see cref="Us9CardReading"/>), the count's own 5b function since K18 read beside it on that map and on the game's staging, the trailing party's ceiling in
    /// the closest state flipping it through the count's own path and nothing else, US-9's scripted program read, the planted tooth (no ground game) below
    /// the range; (n) the regional noise keyed - the same count bit for bit, another master seed other numbers, no counted stream drawn; (h) the game's own
    /// polling day counting each state with its ground game, the stored contest re-deriving it (its effects cleared, the derived states - the tooth), the
    /// House swing and the Senate's states carrying it (I3's "alike"); (i) the mid-campaign save counting the same effects; (o) Sweden's and Germany's
    /// stagings declaring no rule.</para>
    /// </summary>
    public static class UsCampaignDiagnostic
    {
        /// <summary>DECLARED (CONVENTION): the project's film seed, the one `CampaignAiHarness` runs.</summary>
        private const int Seed = 777;

        /// <summary>DECLARED (CONVENTION): the player's party in the game path - the Republicans, the party the film driver's USA plays.</summary>
        private const string PlayerParty = "REP";

        private const string CvapRelative = "ElectionsData/usa/raw/cvap/State.csv";
        private const string NationRelative = "ElectionsData/usa/raw/cvap/Nation.csv";
        private const string UfRelative = "ElectionsData/usa/raw/returns/ufl_turnout_2024G_v0.4.csv";
        private const string GallupRelative = "ElectionsData/usa/raw/salience/gallup_mip_vEKjO_47_dataset.csv";

        /// <summary>DECLARED (CONVENTION): the day the mid-campaign save is cut - three weeks into the campaign proper.</summary>
        private static readonly DateTime CutOn = new DateTime(2024, 10, 1);

        /// <summary>DECLARED (CONVENTION): a later campaign's opening the salience is read for - the 2028 campaign's (the polling day of 7 Nov 2028 less eight weeks).</summary>
        private static readonly DateTime LaterCampaignOpens = new DateTime(2028, 9, 12);

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsCampaignDiagnostic (PS-6 US-30): the campaign on the jurisdictions - staged on the USA's start, run in a game, counted ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            string dir = Path.Combine(Application.temporaryCachePath, "us_campaign_" + Guid.NewGuid().ToString("N"));
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            double[][] regionalShares = NationalElection.LastRegionalShares;
            string[] regionalNames = NationalElection.LastRegionalNames;
            double[] regionalWeights = NationalElection.LastRegionalWeights;
            double regionalWorst = NationalElection.LastRegionalWorstAbsError;
            IReadOnlyList<string> regionalKeys = NationalElection.LastRegionalKeys;
            try
            {
                Directory.CreateDirectory(dir);
                using (SimulationManager.EpochScope()) { Measure(sb, Check, hosts, dir); }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw: ").Append(e).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                EnergyMarket.ResetTurnState();
                SimulationRandom.RestoreState(seedWas, drawsWas);
                void Set(string property, object value) =>
                    typeof(NationalElection).GetProperty(property, BindingFlags.Public | BindingFlags.Static).GetSetMethod(true).Invoke(null, new[] { value });
                Set(nameof(NationalElection.LastRegionalShares), regionalShares);
                Set(nameof(NationalElection.LastRegionalNames), regionalNames);
                Set(nameof(NationalElection.LastRegionalWeights), regionalWeights);
                Set(nameof(NationalElection.LastRegionalWorstAbsError), regionalWorst);
                Set(nameof(NationalElection.LastRegionalKeys), regionalKeys);
                try { if (Directory.Exists(dir)) { Directory.Delete(dir, true); } } catch (Exception e) { sb.Append("    note      the temporary folder stays: ").Append(e.Message).Append('\n'); }
            }
            sb.Append(failures == 0 ? "    CLEAN\n" : F("    {0} failure(s)\n", failures));
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static void Measure(StringBuilder sb, Action<bool, string> Check, List<GameObject> hosts, string dir)
        {
            WorldClock.ApplyStart(CountryId.USA);
            DateTime start = WorldClock.StartDate(CountryId.USA);
            DateTime polled = UsPresidentialElection.PresidentialDay(2024);

            // ---- (a) the calendar
            Check(WorldClock.TryNextPollingDay(CountryId.USA, start, out DateTime first) && first == polled, F("(a) the USA's start {0:yyyy-MM-dd} offers its polling day {1:yyyy-MM-dd} (3 U.S.C. §21(1))", start, first));
            CampaignCalendar cal = CampaignCalendar.FromWorldStart(polled, start);
            Check(cal.ElectionDate == polled && cal.CampaignStart == polled.AddDays(-7 * CampaignCalendar.DefaultCampaignWeeks) && cal.PreCampaignStart >= start && (cal.PreCampaignStart - start).TotalDays < 7.0,
                F("(a) the calendar: run-up {0:yyyy-MM-dd} (the start {1:yyyy-MM-dd}, never before it), campaign {2:yyyy-MM-dd}, polling day {3:yyyy-MM-dd}", cal.PreCampaignStart, start, cal.CampaignStart, cal.ElectionDate));

            // ---- (b) the staging
            var none = new (int, int, Scandal)[0];
            bool staged = LiveCampaignSetup.TryFor(CountryId.USA, none, cal, out CampaignRun.Setup setup, out string note, onVoteModelCompatibility: true);
            string[] keys = LiveCampaignSetup.Keys(CountryId.USA);
            var prev = UsPresidentialReturns.States.Where(s => s.Year == 2020).ToArray();
            Check(staged && setup.Parties != null && setup.Parties.Length == keys.Length && keys.Contains("REP") && keys.Contains("DEM") && setup.Regions.Length == prev.Length && prev.Length == 51
                  && setup.Regions.Select(r => r.Name).SequenceEqual(prev.Select(s => s.State)),
                F("(b) staged: {0} parties ({1}) on {2} jurisdictions, US-3's catalog in its order", staged ? setup.Parties.Length : 0, string.Join(", ", keys), staged ? setup.Regions.Length : 0));
            if (!staged) { throw new InvalidOperationException("no US campaign staged: " + note); }
            double audience = setup.Regions.Sum(r => r.Audience), cast2020 = prev.Sum(s => (double)s.VotesTotal);
            Check(UsRegions.PriorYear(polled) == 2020 && audience == cast2020 && setup.NationalAudience == cast2020 && setup.Regions.Zip(prev, (r, s) => r.Audience == s.VotesTotal).All(same => same),
                F("(b) each jurisdiction's audience its 2020 valid presidential votes (the catalog's votes cast) - {0:N0} together, the national audience", audience));

            // the electorate: the Census Bureau's CVAP, every figure held to the file, the names mapped through UF's file
            byte[] cvapBytes = File.ReadAllBytes(PathOf(CvapRelative)), ufBytes = File.ReadAllBytes(PathOf(UfRelative)), nationBytes = File.ReadAllBytes(PathOf(NationRelative));
            bool hashed = DigestListed(CvapRelative, Sha256(cvapBytes)) && DigestListed(UfRelative, Sha256(ufBytes)) && DigestListed(NationRelative, Sha256(nationBytes));
            Dictionary<string, long> cvapByName = ReadCvapTotals(Encoding.UTF8.GetString(cvapBytes));
            Dictionary<string, string> abbrev = ReadUfAbbreviations(Encoding.UTF8.GetString(ufBytes));
            var cvap = new Dictionary<string, long>();
            foreach (KeyValuePair<string, long> kv in cvapByName) { if (abbrev.TryGetValue(kv.Key, out string ab)) { cvap[ab] = kv.Value; } }
            var wrong = new List<string>();
            foreach (RegionAudience r in setup.Regions)
            {
                if (!cvap.TryGetValue(r.Name, out long fromFile) || r.Eligible != fromFile || UsRegions.EligibleOf(r.Name) != fromFile) { wrong.Add(r.Name + (cvap.ContainsKey(r.Name) ? " " + r.Eligible.ToString("R", CultureInfo.InvariantCulture) + " against " + fromFile.ToString(CultureInfo.InvariantCulture) : " not in the file")); }
            }
            double eligible = setup.Regions.Sum(r => r.Eligible);
            long nation = ReadCvapTotals(Encoding.UTF8.GetString(nationBytes)).Values.FirstOrDefault();
            Check(hashed && wrong.Count == 0 && UsRegions.CitizenVotingAgePopulation.Count == 51,
                F("(b) each jurisdiction's electorate its citizen voting-age population (the Census Bureau's 2020-2024 ACS tabulation, {0}; its SHA-256, Nation.csv's and UF's in their SHA256SUMS.txt): every one of the 51 the file's, mapped by UF's names - {1:N0} together (the file's nation line {2:N0}, the difference its own rounding){3}",
                    Path.GetFileName(CvapRelative), eligible, nation, wrong.Count == 0 ? string.Empty : " - WRONG: " + string.Join("; ", wrong)));
            Check(setup.Regions.All(r => r.Eligible > r.Audience), "(b) every jurisdiction's electorate exceeds its votes cast - a ground game has voters to mobilise everywhere");

            // ---- (c) the ballot: both major nominees everywhere; no campaigning party backs a minor candidate, who stands in the count where on the ballot of record
            int StandsCount(int p) { int n = 0; for (int r = 0; r < setup.Regions.Length; r++) { if (setup.StandsIn(p, r)) { n++; } } return n; }
            Check(setup.Stands != null && Enumerable.Range(0, keys.Length).All(p => StandsCount(p) == setup.Regions.Length && setup.StandingShare(p) == 1.0),
                F("(c) both major nominees on every ballot: {0}", string.Join(", ", Enumerable.Range(0, keys.Length).Select(p => F("{0} {1} of {2} jurisdictions", keys[p], StandsCount(p), setup.Regions.Length)))));
            IReadOnlyList<PresidencyOfRecord.CandidateOfRecord> field = PresidencyOfRecord.CandidatesOf(CountryId.USA, 2025);
            var nominees = new HashSet<string>(UsPresidentialReturns.Candidates.Where(c => c.Year == 2024 && (c.Ticket == "D" || c.Ticket == "R")).Select(c => c.Candidate));
            var minors = field.Where(c => !nominees.Contains(c.Surname)).ToList();
            var minorLine = minors.Select(c => F("{0} {1}", c.Surname, PresidencyOfRecord.StandingOfRecord(CountryId.USA, 2024, c.Surname).Count)).ToList();
            Check(minors.Count > 0 && minors.All(c => c.BackingParty == null || !keys.Contains(c.BackingParty)) && minors.All(c => PresidencyOfRecord.StandingOfRecord(CountryId.USA, 2024, c.Surname).Count > 0),
                F("(c) no campaigning party backs a minor candidate, so no campaign reaches one; each of the field's {0} other columns stands in the count only where on the ballot of record (US-6c's standing, jurisdictions): {1}", minors.Count, string.Join(", ", minorLine)));

            // ---- (d) the salience: the newest Gallup wave fielded before the campaign, held to the saved table
            byte[] gallupBytes = File.ReadAllBytes(PathOf(GallupRelative));
            string[] rows = Encoding.UTF8.GetString(gallupBytes).Replace("\r\n", "\n").Split('\n');
            string[] header = rows[0].Split('\t');
            int aug = Array.IndexOf(header, "Aug-24");
            var items = new List<(string Item, int Percent)>();
            foreach (string row in rows.Skip(2))
            {
                string[] cells = row.Split('\t');
                if (cells.Length <= aug || aug < 0 || cells[0].Length == 0 || cells[0].StartsWith("<b>", StringComparison.Ordinal) || cells[0] == "Other non-economic" || cells[0] == "No opinion" || cells[0] == "Total") { continue; }
                if (int.TryParse(cells[aug], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pct)) { items.Add((cells[0].Trim(), pct)); }
            }
            int Of(string item) => items.Where(i => i.Item == item).Select(i => i.Percent).DefaultIfEmpty(-1).First();
            int fifth = items.Select(i => i.Percent).OrderByDescending(v => v).Skip(4).DefaultIfEmpty(-1).First();
            int immigration = Of("Immigration"), economy = Of("Economy in general"), prices = Of("High cost of living/Inflation");
            double[] sal = setup.TrueSalience;
            int slots = Enumerable.Range(0, sal.Length).Count(i => !double.IsNaN(sal[i]));
            Check(DigestListed(GallupRelative, Sha256(gallupBytes)) && aug == 1 && cal.CampaignStart >= new DateTime(2024, 9, 1)
                  && immigration >= fifth && Math.Max(economy, prices) >= fifth && sal[(int)IssueId.Immigration] == immigration / 100.0 && sal[(int)IssueId.Economy] == Math.Max(economy, prices) / 100.0 && slots == 2
                  && LiveCampaignSetup.GallupAugust2024Immigration == immigration / 100.0 && LiveCampaignSetup.GallupAugust2024Economy == Math.Max(economy, prices) / 100.0,
                F("(d) the salience Gallup's August 2024 wave - the table's newest column ({0}), fielded before the campaign opened {1:yyyy-MM-dd} - its SHA-256 listed: immigration .{2:00}, the economy .{3:00} (\"Economy in general\" {4}, \"High cost of living/Inflation\" {5} - the larger), both in the column's top five (its fifth {6}); no other slot",
                    aug >= 0 ? header[aug] : "NOT FOUND", cal.CampaignStart, immigration, Math.Max(economy, prices), economy, prices, fifth));
            double[] later = LiveCampaignSetup.UsSalience(LaterCampaignOpens, out string laterLabel);
            bool seedRead = true;
            foreach (IssueId issue in Enum.GetValues(typeof(IssueId)))
            {
                float seed = StanceModel.SalienceOf(CountryId.USA, issue, out bool measured);
                seedRead &= measured ? later[(int)issue] == Math.Round(seed, 2) : double.IsNaN(later[(int)issue]);
            }
            Check(seedRead, F("(d) a campaign opening {0:yyyy-MM-dd} reads the newest wave on disk, the USA's seed: {1}", LaterCampaignOpens, laterLabel));

            // ---- (e) the casts, (f) the offices and the chest
            Check(Enumerable.Range(0, keys.Length).All(p => setup.Parties[p].Personality == LiveCampaignSetup.PersonalityOf(CountryId.USA, keys[p])),
                F("(e) every party cast by the CHES rule (§695): {0}", string.Join(", ", Enumerable.Range(0, keys.Length).Select(p => keys[p] + " " + setup.Parties[p].Personality))));
            int strayOffices = 0;
            for (int p = 0; p < keys.Length; p++) { foreach (int r in setup.Parties[p].Offices) { if (r < 0 || r >= setup.Regions.Length || !setup.StandsIn(p, r)) { strayOffices++; } } }
            double perUnit = LiveCampaignSetup.PursePerUnit(CountryId.USA);
            Check(strayOffices == 0 && setup.Parties.All(p => p.StartingMoney == LiveCampaignSetup.WarChest) && LiveCampaignSetup.UsWarChest == PoliSim.Elections.Generated.UsCampaignSpending.WarChestUsd
                  && perUnit == LiveCampaignSetup.UsWarChest / LiveCampaignSetup.WarChest && Math.Abs(LiveCampaignSetup.WarChest * perUnit - LiveCampaignSetup.UsWarChest) < 1e-6
                  && LiveCampaignSetup.PursePerUnit(CountryId.Sweden) == 1.0 && note.Contains("K19") && note.Contains("K110"),
                F("(f) every staged office on a jurisdiction where its party stands ({0}); every chest K19's, from FEC spending - {1:N0} units of the model's table at K110's {2:N4} dollars a unit, {3:N2} dollars each (the 2024 nominees' disbursements over the campaign window, their mean), so every act's price is the model's times that ratio and money binds as before - the note says so",
                    string.Join(", ", Enumerable.Range(0, keys.Length).Select(p => keys[p] + " " + string.Join("/", setup.Parties[p].Offices.Select(r => setup.Regions[r].Name)))), LiveCampaignSetup.WarChest, perUnit, LiveCampaignSetup.WarChest * perUnit));

            // ---- (g) an idle campaign on the start's staging is the start's prediction
            NationalElection.TryPredictShares(CountryId.USA, out Dictionary<string, double> atStart);
            CampaignRun.Result idleAtStart = CampaignAiHarness.RunSeeded(Idle(setup), Seed);
            double offAtStart = Enumerable.Range(0, keys.Length).Max(p => Math.Abs(idleAtStart.FinalShares[p] - atStart[keys[p]]));
            Check(offAtStart < 1e-12 && idleAtStart.Parties.Sum(l => l.TotalActions) == 0,
                F("(g) an idle campaign (no act, no debate, no story) on the start's staging ends on the start's prediction: {0} - within {1:0.0e+0}", Shares(keys, idleAtStart.FinalShares), offAtStart));

            // ---- (k)-(n) PS-6 US-31: the AI's targeting, the ground game in one state, the keyed noise - on the start's staging
            MeasureTargeting(sb, Check, setup, keys);
            MeasureGroundGame(sb, Check, setup, keys, polled, atStart, idleAtStart);

            // ---- (h) A US GAME'S OWN DAY PATH - the REP the player's party, stepped from the start through the polling day as the game steps it
            string saveMid = Path.Combine(dir, "mid_campaign.json");
            UsPresidentialElection.Contest played = null;
            CampaignRun.Setup liveSetup = default;
            Dictionary<string, double> counted = null, predicted = null, countShift = null, dayShift = null;
            DateTime? runUpSeen = null, campaignSeen = null, pollingDaySeen = null;
            int regionsSeen = 0, playerIndex = -1;
            bool cut = false, droppedAfter = false, resultAfter = false, recount = false;
            UsGroundGame.Campaign groundCampaign = null;   // PS-6 US-31: what the polling day's count read of the campaign
            ElectoralCollege.StateCall[] derivedStates = null;
            string derivedLine = null;
            UsCongressElection.Contest congressOnDay = null;
            string recountWhat = "not reached", predictedLine = "not reached";
            {
                (SimulationManager sim, World w, Country usa) = Open(hosts);
                playerIndex = sim.PlayerPartyIndexForCampaign();
                for (int d = 0; d < 400 && sim.CurrentDate < polled.AddDays(2); d++)
                {
                    Step(sim, w);
                    DateTime today = sim.CurrentDate;
                    if (runUpSeen == null && sim.PlayerPreCampaign != null) { runUpSeen = today; }
                    if (campaignSeen == null && sim.PlayerCampaign != null) { campaignSeen = today; regionsSeen = sim.PlayerCampaign.Setup.Regions.Length; }
                    if (today == CutOn) { SaveGameService.SaveToFile(saveMid, SaveGameService.CreateSaveGame(sim, w, CountryId.USA, null)); cut = true; }
                    if (sim.PollingDayToday)
                    {
                        pollingDaySeen = today;
                        played = usa.UsPresidentialElections.FirstOrDefault(c => c.PollingDay == today);
                        if (sim.TryPlayerCampaignShares(out counted) && played != null)
                        {
                            liveSetup = sim.PlayerCampaign.Setup;
                            countShift = sim.CampaignRecord.CountRecordShift;
                            UsPresidentialElection.Contest previous = UsPresidentialElection.PreviousCount(usa, today);   // PS-6 US-26: a game that has counted before swings from its own states, as Hold does
                            groundCampaign = UsGroundGame.Of(sim.PlayerCampaign.Setup, sim.PlayerCampaignResult);   // PS-6 US-31: the count reads the campaign's ground game
                            UsPresidentialElection.Contest again = UsPresidentialElection.Count(CountryId.USA, today, counted, UsTermLimit.NomineesFor(usa, today),
                                UsPresidentialElection.TieSeedOf(SimulationRandom.MasterSeed, today.Year), previous,
                                previous != null ? UsPresidentialElection.StatesOf(usa, previous) : null, groundCampaign, UsGroundGame.NoiseSeedOf(CountryId.USA, today), out ElectoralCollege.StateCall[] _, out string whyAgain);
                            recount = again != null && PresidencyDiagnostic.SameContest(again, played);
                            recountWhat = again?.Line ?? whyAgain;
                            UsPresidentialElection.Contest derivedOnly = UsPresidentialElection.Count(CountryId.USA, today, counted, UsTermLimit.NomineesFor(usa, today),
                                UsPresidentialElection.TieSeedOf(SimulationRandom.MasterSeed, today.Year), previous,
                                previous != null ? UsPresidentialElection.StatesOf(usa, previous) : null, out derivedStates, out string _);
                            derivedLine = derivedOnly?.Line;
                            congressOnDay = usa.UsCongressElections.FirstOrDefault(c => c.PollingDay == today);
                        }
                        dayShift = EconomicVote.RecordOverTerm(usa, today, out _);
                        if (NationalElection.TryPredictShares(CountryId.USA, out predicted, countShift, on: today, played: usa))   // PS-6 US-26: the history rolled forward, as the game's staging reads it
                        {
                            UsPresidentialElection.Contest byPrediction = UsPresidentialElection.Count(CountryId.USA, today, predicted, UsTermLimit.NomineesFor(usa, today), 0, out ElectoralCollege.StateCall[] _, out string whyPredicted);
                            predictedLine = byPrediction?.Line ?? whyPredicted;
                        }
                    }
                    if (today == polled.AddDays(1)) { droppedAfter = sim.PlayerCampaign == null && sim.PlayerPreCampaign == null; resultAfter = sim.PlayerCampaignResult != null; }
                }
                Check(runUpSeen.HasValue && sim.CampaignRecord != null && sim.CampaignRecord.StartDate == cal.PreCampaignStart && campaignSeen == cal.CampaignStart && regionsSeen == 51 && playerIndex >= 0,
                    F("(h) a US game, {0} the player's party, stepped from {1:yyyy-MM-dd}: its run-up begun {2} (the calendar's {3:yyyy-MM-dd}, the record's {4}), its campaign opened {5} on {6} jurisdictions",
                        PlayerParty, start, runUpSeen.HasValue ? runUpSeen.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "NEVER", cal.PreCampaignStart,
                        sim.CampaignRecord != null ? sim.CampaignRecord.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "NONE",
                        campaignSeen.HasValue ? campaignSeen.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "NEVER", regionsSeen));
                Check(pollingDaySeen == polled && counted != null && played != null && recount && played.ElectorsAppointed == played.Electors.Sum(t => t.Votes),
                    F("(h) the polling day {0} counts the campaign's final shares ({1}), then the states, then the electors - the contest is the count of those shares exactly: \"{2}\"",
                        pollingDaySeen.HasValue ? pollingDaySeen.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "NEVER", counted == null ? "none" : Shares(keys, keys.Select(k => counted[k]).ToArray()), recountWhat));
                ElectoralCollege.StateCall[] states = played != null ? UsPresidentialElection.StatesOf(usa, played) : null;   // PS-6 US-26: in the form it was counted in
                Check(states != null && states.Length == 51, F("(h) the stored contest re-derives its {0} jurisdictions' calls from the shares it keeps (UsPresidentialElection.StatesOf)", states?.Length ?? 0));
                MeasureGameCount(sb, Check, usa, played, states, derivedStates, derivedLine, groundCampaign, congressOnDay, polled);
                Check(droppedAfter && resultAfter, F("(h) on {0:yyyy-MM-dd} the finished run is dropped ({1}) and its result kept ({2})", polled.AddDays(1), droppedAfter, resultAfter));
                CampaignRun.Result result = sim.PlayerCampaignResult;
                if (result != null)
                {
                    var acts = new Dictionary<string, int>();
                    for (int p = 0; p < result.Parties.Length; p++)
                    {
                        foreach (CampaignRun.DecisionRecord dr in result.Parties[p].Log)
                        {
                            if (Array.IndexOf(CampaignActions.TheEight, dr.Kind) < 0 || !CampaignActions.Spec(dr.Kind).IsLocal) { continue; }
                            string where = keys[p] + " in " + dr.Target.Split(new[] { " / " }, StringSplitOptions.None)[0];
                            acts[where] = acts.TryGetValue(where, out int n) ? n + 1 : 1;
                        }
                    }
                    sb.Append(F("    read      (h) the game's campaign: {0} acts in all ({1}); local acts by jurisdiction {2}\n", result.Parties.Sum(l => l.TotalActions),
                        string.Join(", ", result.Parties.Select((l, i) => keys[i] + " " + l.TotalActions)), acts.Count == 0 ? "none" : string.Join(", ", acts.OrderByDescending(a => a.Value).Take(8).Select(a => a.Key + " " + a.Value))));
                }
                sb.Append(F("    read      (h) the count on the campaign's shares: {0}\n", played?.Line ?? "no count"));
                sb.Append(F("    read      (h) the count the day's prediction would have made (no campaign): {0}\n", predictedLine));

                // ---- (j) PS-6 US-26 composed: the game's NEXT campaign (2028) staged on the history rolled forward - the game's own 2024 House count - as the prediction reads it
                DateTime polled2028 = UsPresidentialElection.PresidentialDay(2028);
                CampaignCalendar cal2028 = CampaignCalendar.FromWorldStart(polled2028, start);
                bool ownHouse = usa.UsCongressElections != null && usa.UsCongressElections.Any(c => c.PollingDay == polled);
                bool rolledStaged = LiveCampaignSetup.TryFor(CountryId.USA, none, cal2028, out CampaignRun.Setup rolledSetup, out string rolledNote, onVoteModelCompatibility: true, played: usa);
                bool recordStaged = LiveCampaignSetup.TryFor(CountryId.USA, none, cal2028, out CampaignRun.Setup recordSetup, out string _, onVoteModelCompatibility: true);
                bool modelRead = NationalElection.TryCompatibility(CountryId.USA, out string[] modelKeys, out double[] _, out double[] modelPrior, out double[] modelLoyalty, usa, polled2028);
                bool asPredicted = rolledStaged && modelRead && Enumerable.Range(0, rolledSetup.Parties.Length).All(p =>
                {
                    int k = Array.IndexOf(modelKeys, rolledSetup.Parties[p].Name);
                    return k >= 0 && rolledSetup.PriorShares[p] == modelPrior[k] && rolledSetup.LoyaltyPerParty[p] == modelLoyalty[k];
                });
                bool moved = rolledStaged && recordStaged && Enumerable.Range(0, rolledSetup.Parties.Length).Any(p => rolledSetup.PriorShares[p] != recordSetup.PriorShares[p]);
                Check(ownHouse && asPredicted && moved && rolledNote.Contains("on the history rolled forward"),
                    F("(j) the 2028 campaign ({0:yyyy-MM-dd}) staged in the game that counted 2024 stages its prior and its loyalty on the history rolled forward - the game's own House count of {1:yyyy-MM-dd} - exactly as the 2028 prediction reads them, off the record's pair, its note saying so: prior {2} (the record's {3})",
                        polled2028, polled, rolledStaged ? Shares(rolledSetup.Parties.Select(p => p.Name).ToArray(), rolledSetup.PriorShares) : "NOT STAGED",
                        recordStaged ? Shares(recordSetup.Parties.Select(p => p.Name).ToArray(), recordSetup.PriorShares) : "NOT STAGED"));
                if (rolledStaged && NationalElection.TryPredictShares(CountryId.USA, out Dictionary<string, double> rolledPrediction, null, polled2028, usa))
                {
                    CampaignRun.Result idle2028 = CampaignAiHarness.RunSeeded(Idle(rolledSetup), Seed);
                    double off2028 = Enumerable.Range(0, rolledSetup.Parties.Length).Max(p => Math.Abs(idle2028.FinalShares[p] - rolledPrediction[rolledSetup.Parties[p].Name]));
                    Check(off2028 < 1e-12 && idle2028.Parties.Sum(l => l.TotalActions) == 0,
                        F("(j) that staging run idle ends on the 2028 prediction on the game's own history: {0} - within {1:0.0e+0}", Shares(rolledSetup.Parties.Select(p => p.Name).ToArray(), idle2028.FinalShares), off2028));
                }
                else { Check(false, "(j) no 2028 staging or no 2028 prediction on the game's own history"); }
                EnergyMarket.ResetTurnState();
            }

            // THE DONE-WHEN: the run's own staging, replayed idle, reproduces the day's prediction exactly - on the record the count was judged on (§752: the eve's)
            if (counted != null && predicted != null && liveSetup.Parties != null)
            {
                CampaignRun.Result idleDay = CampaignAiHarness.RunSeeded(Idle(liveSetup), Seed);
                double off = Enumerable.Range(0, keys.Length).Max(p => Math.Abs(idleDay.FinalShares[p] - predicted[keys[p]]));
                Check(off < 1e-12 && idleDay.Parties.Sum(l => l.TotalActions) == 0 && liveSetup.RecordShift != null,
                    F("(h) THE DONE-WHEN: the game's campaign staging replayed idle - no act, no debate, no story, the government's record as the count judged it - ends on the day's prediction on that record: {0} against {1} - within {2:0.0e+0}",
                        Shares(keys, idleDay.FinalShares), Shares(keys, keys.Select(k => predicted[k]).ToArray()), off));
                double shiftGap = keys.Max(k => Math.Abs((countShift != null && countShift.TryGetValue(k, out double a) ? a : 0.0) - (dayShift != null && dayShift.TryGetValue(k, out double b) ? b : 0.0)));
                sb.Append(F("    read      (h) the record judged for the count on the eve (§752) against the record judged on the polling day itself: the largest difference {0:0.0e+0} of a share\n", shiftGap));
            }
            else { Check(false, "(h) THE DONE-WHEN: no campaign result or no prediction on the polling day - nothing to compare"); }

            // ---- (i) a save cut mid-campaign, loaded into the game: the campaign replayed, the same contest counted
            if (!cut) { Check(false, F("(i) no save was cut on {0:yyyy-MM-dd}", CutOn)); return; }
            SaveGame save = SaveGameService.LoadFromFile(saveMid);
            (SimulationManager adopted, PoliSim.UI.GameController _) = PresidencyDiagnostic.Adopt(save, hosts, out string error);
            if (adopted == null) { Check(false, "(i) the mid-campaign save: " + error); return; }
            try
            {
                Country u = adopted.World.GetCountry(CountryId.USA);
                bool replayed = adopted.PlayerCampaign != null && adopted.CampaignRecord != null && adopted.CampaignRecord.ElectionDate == polled;
                int replayedDays = adopted.PlayerCampaign?.Day ?? -1;
                for (int d = 0; d < 60 && adopted.CurrentDate < polled; d++) { Step(adopted, adopted.World); }
                UsPresidentialElection.Contest loadedCount = u.UsPresidentialElections.FirstOrDefault(c => c.PollingDay == polled);
                Check(replayed && loadedCount != null && played != null && PresidencyDiagnostic.SameContest(loadedCount, played) && loadedCount.GroundGame.Count == played.GroundGame.Count && played.GroundGame.Count > 0,
                    F("(i) the save cut {0:yyyy-MM-dd} (format {1}) loads into the game with its campaign replayed ({2} day(s)); stepped to the polling day it counts the same contest - PS-6 US-31: its {4} states' ground-game effects, the regional noise among them, to the last bit (the noise keyed, not counted): \"{3}\"",
                        CutOn, save.SaveVersion, replayedDays, loadedCount?.Line ?? "no count", loadedCount?.GroundGame.Count ?? 0));
            }
            finally { EnergyMarket.ResetTurnState(); }

            // ---- (o) PS-6 US-31: USA-gated - Sweden's and Germany's stagings declare no targeting rule (R-US16 (c) is not ruled)
            MeasureGate(sb, Check);

            sb.Append("    note      ").Append(note.Replace("\n", "\n              ").Trim()).Append('\n');
        }

        // ================================================================== PS-6 US-31: the state-by-state count and the AI's targeting

        /// <summary>DECLARED (US-9's, §846): the maximal ground game - weighted contacts per eligible voter in the one state, past where
        /// `GotvModel.Mobilization` reaches its ceiling in double precision (asserted: it reads exactly 100). No budget buys more.</summary>
        private const double CeilingEffort = 40.0;

        /// <summary>CONVENTION: half the last printed digit of the card's two-decimal range - its own rounding, not a tolerance on the model.</summary>
        private const double CardRoundingPp = 0.005;

        /// <summary>CONVENTION: an unworked state's margin moves only by the count's rounding to whole votes (US-9: below 1e-5 of a share) - in points.</summary>
        private const double UnworkedPp = 1e-3;

        /// <summary>The program US-9's (b) scripted (DECLARED there): door-to-door in the one state, as many a day as the candidate's day holds, the party's one
        /// office there from day 0.</summary>
        private const CampaignActionKind ProgramAct = CampaignActionKind.DoorToDoor;

        /// <summary>DECLARED: the state the scripted program works - US-9's, the closest 2020 state its (b) worked.</summary>
        private const string ProgramState = "GA";

        private static double Margin(double[] shares) => 100.0 * (shares[ElectoralCollege.ShareRepublican] - shares[ElectoralCollege.ShareDemocrat]);

        /// <summary>K112 (PREREGISTERED, `Reviews/2026-10-24_usdata_prereg.md` rule 1): 5b's closed form - the margin move (REP less DEM, points) when the worked party's
        /// doors reach k = 1 - e^(-doors / supporters) of its supporters, who vote at the base plus Gerber &amp; Green's band at that base, while everyone else votes at
        /// the base. DERIVED from 5b's arithmetic, never measured.</summary>
        private static double ClosedFormMovePp(double[] shares, bool rep, double baseTurnout, double eligible, double doors)
        {
            double d = shares[ElectoralCollege.ShareDemocrat], r = shares[ElectoralCollege.ShareRepublican], o = shares[ElectoralCollege.ShareOthers];
            double sum = d + r + o;
            d /= sum; r /= sum; o /= sum;
            double sp = rep ? r : d;
            double c = GotvTurnout.DoorCacePp(CountryId.USA, baseTurnout) / 100.0;
            double k = GotvTurnout.Coverage(doors, eligible * sp);
            double total = baseTurnout + sp * k * c;
            double d2 = (d * baseTurnout + (rep ? 0.0 : d * k * c)) / total, r2 = (r * baseTurnout + (rep ? r * k * c : 0.0)) / total;
            return 100.0 * ((r2 - d2) - (r - d));
        }

        /// <summary>(k) and (l): the rule declared on the USA's staging and read off a poll, and where the AI's local acts land - every one of a party that
        /// never looks (Chaotic, W-C2's reactivity 0) inside the rule's regions of its day; the planted tooth, the same staging with the rule stripped, lands
        /// outside them.</summary>
        private static void MeasureTargeting(StringBuilder sb, Action<bool, string> Check, CampaignRun.Setup setup, string[] keys)
        {
            RegionTargeting rule = setup.Targeting;
            Check(rule != null, F("(k) US-31: the USA's staging declares the AI's targeting rule ([AUTHORED-DRAFT], the play-calibration list): {0}", rule?.Rule ?? "NONE"));
            if (rule == null) { return; }
            string[] names = setup.Regions.Select(r => r.Name).ToArray();
            DateTime polled = setup.Calendar.ElectionDate;

            // (p) PS-6 K27 (Elias's ruling of 2026-10-23: "the AI's offices follow the targeting rule (electors x closeness)"): every AI party's day-0 offices are
            // the rule's on the staging's opening shares, recomputed here; the planted tooth - the audience's plan - differs, so the assertion bites
            int[] opening = LiveCampaignSetup.OpeningOrder(rule, setup.Calendar.CampaignStart, setup.Compatibility, setup.PriorShares, setup.LoyaltyPerParty);
            var officeWrong = new List<string>();
            var officeLines = new List<string>();
            bool audienceDiffers = false;
            for (int p = 0; p < setup.Parties.Length; p++)
            {
                CampaignRun.PartySetup party = setup.Parties[p];
                int[] expected = LiveCampaignSetup.OfficesByTargeting(party.Personality, setup.Regions, setup.Stands?[p], opening);
                int[] byAudience = LiveCampaignSetup.OfficesFor(party.Personality, setup.Regions, setup.Stands?[p]);
                if (!party.Offices.SequenceEqual(expected)) { officeWrong.Add(party.Name); }
                if (!byAudience.SequenceEqual(expected)) { audienceDiffers = true; }
                officeLines.Add(F("{0} {1} (the audience's plan {2})", party.Name, string.Join("/", party.Offices.Select(r => names[r])), string.Join("/", byAudience.Select(r => names[r]))));
            }
            bool contested = opening != null && setup.Parties.All(party => party.Offices.Take(Math.Min(party.Offices.Length, opening.Length)).All(r => Array.IndexOf(opening, r) >= 0));
            Check(opening != null && officeWrong.Count == 0 && audienceDiffers && contested,
                F("(p) PS-6 K27: every AI party's day-0 offices open where the targeting rule weighs most on the staging's opening shares ({0} states contested, first {1}): {2}{3}",
                    opening?.Length ?? 0, opening == null ? "none" : string.Join(", ", opening.Take(6).Select(r => names[r])), string.Join("; ", officeLines),
                    officeWrong.Count == 0 ? string.Empty : " - NOT THE RULE'S: " + string.Join(", ", officeWrong)));

            // (k) the rule read off a poll, recomputed here: electors x the swing index of the margin the count derives from the poll's shares
            bool Recomputed(double[] shares, out int[] order, out string top)
            {
                var poll = new Poll(setup.Calendar.CampaignStart, 600, "planted", shares, new double[shares.Length]);
                double[] weights = rule.WeightsOn(poll);
                var dict = new Dictionary<string, double>();
                for (int i = 0; i < keys.Length; i++) { dict[keys[i]] = shares[i]; }
                UsPresidentialElection.Count(CountryId.USA, polled, dict, null, 0, null, null, out ElectoralCollege.StateCall[] states, out string _);
                bool same = states != null && weights.Length == names.Length;
                for (int r = 0; same && r < names.Length; r++)
                {
                    ElectoralCollege.StateCall c = states.First(s => s.State == names[r]);
                    same = weights[r] == c.Electors * SwingRegions.Index(Margin(c.Shares));
                }
                order = rule.OrderOn(poll);
                int[] o = order;
                top = string.Join(", ", o.Take(CampaignAi.LocalCandidateRegions).Select(r => F("{0} ({1:0})", names[r], weights[r])));
                return same;
            }
            double[] even = keys.Select(k => k == "REP" || k == "DEM" ? 0.5 : 0.0).ToArray();
            double[] landslide = keys.Select(k => k == "REP" ? 0.65 : k == "DEM" ? 0.35 : 0.0).ToArray();
            bool evenSame = Recomputed(even, out int[] evenOrder, out string evenTop);
            bool slideSame = Recomputed(landslide, out int[] slideOrder, out string slideTop);
            Check(evenSame && slideSame && !evenOrder.SequenceEqual(slideOrder) && evenOrder.Length >= CampaignAi.LocalCandidateRegions,
                F("(k) the rule's weights are electors x the swing index of the margin the count derives from the poll, recomputed here state by state; on a planted even poll it contests {0} states (first {1}), on a planted REP 65 % it contests {2} (first {3}) - another order: it reads the poll",
                    evenOrder.Length, evenTop, slideOrder.Length, slideOrder.Length == 0 ? "none" : slideTop));

            // (l) where the AI's local acts land, the rule against its planted absence
            int fallbackDays = 0;
            (int Acts, int Inside, string Where) Land(CampaignRun.Setup s)
            {
                SimulationRandom.Seed(Seed);
                CampaignRun.State st = CampaignRun.Begin(s, SimulationRandom.For(SimulationRandom.Stream.CampaignAi), SimulationRandom.For(SimulationRandom.Stream.Debate), SimulationRandom.For(SimulationRandom.Stream.Scandal));
                var picks = new List<HashSet<int>>();
                while (!st.Finished)
                {
                    CampaignRun.StepDay(st);
                    picks.Add(st.PublicPoll.HasValue ? new HashSet<int>(rule.OrderOn(st.PublicPoll.Value).Where(r => setup.StandsIn(0, r)).Take(CampaignAi.LocalCandidateRegions)) : new HashSet<int>());
                }
                CampaignRun.Result run = CampaignRun.Finish(st);
                fallbackDays = run.Parties.Sum(l => l.RuleFallbackDays);
                // K19/K110, READ (not judged): how much of K19's chest the AI's campaign spends at the re-denominated prices (the model's times the chest's ratio)
                double dollars = LiveCampaignSetup.PursePerUnit(CountryId.USA);
                sb.Append(F("    (l) K110, read: each party's chest spent by polling day, at the re-denominated prices - {0}\n", string.Join(", ", run.Parties.Select((l, i) => F("{0} {1:0.00} % of {2:N0} dollars ({3:N0} left)",
                    s.Parties[i].Name, s.Parties[i].StartingMoney > 0.0 ? 100.0 * (s.Parties[i].StartingMoney - l.MoneyLeft) / s.Parties[i].StartingMoney : double.NaN, s.Parties[i].StartingMoney * dollars, l.MoneyLeft * dollars)))));
                int acts = 0, inside = 0;
                var by = new Dictionary<string, int>();
                foreach (CampaignRun.PartyLedger ledger in run.Parties)
                {
                    foreach (CampaignRun.DecisionRecord d in ledger.Log)
                    {
                        if (Array.IndexOf(CampaignActions.TheEight, d.Kind) < 0 || !CampaignActions.Spec(d.Kind).IsLocal) { continue; }
                        int r = Array.IndexOf(names, d.Target.Split(new[] { " / " }, StringSplitOptions.None)[0]);
                        acts++;
                        if (r >= 0 && d.Day >= 0 && d.Day < picks.Count && picks[d.Day].Contains(r)) { inside++; }
                        string at = r >= 0 ? names[r] : "?";
                        by[at] = by.TryGetValue(at, out int n) ? n + 1 : 1;
                    }
                }
                return (acts, inside, by.Count == 0 ? "none" : string.Join(", ", by.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(6).Select(kv => kv.Key + " " + kv.Value)));
            }
            (int acts, int inside, string where) = Land(Cast(setup, AiPersonality.Chaotic, rule));
            int ruleFallback = fallbackDays;
            Check(acts > 0 && inside == acts && ruleFallback == 0,
                F("(l) THE DONE-WHEN, the AI's local acts where the rule says: both parties Chaotic (it never looks - W-C2's reactivity 0, so its candidate regions are the rule's alone), seed {0}: {1} local acts, {2} in the rule's {3} of their day; by state {4}", Seed, acts, inside, CampaignAi.LocalCandidateRegions, where));
            (int oldActs, int oldInside, string oldWhere) = Land(Cast(setup, AiPersonality.Chaotic, null));
            Check(oldActs > 0 && oldInside < oldActs,
                F("(l) the planted tooth - the same staging with the rule stripped: {0} local acts, only {1} in the rule's regions of their day (the largest audiences instead: {2}) - the assertion above bites", oldActs, oldInside, oldWhere));
            // the second review's L3: a rule that contests nothing falls back to the largest audiences, every day, for every party - counted, not logged
            var nothing = new RegionTargeting("planted: contests nothing", poll => new double[setup.Regions.Length]);
            (int noneActs, int _, string noneWhere) = Land(Cast(setup, AiPersonality.Chaotic, nothing));
            int noneFallback = fallbackDays, days = (int)(setup.Calendar.ElectionDate - setup.Calendar.CampaignStart).TotalDays;
            Check(noneActs > 0 && noneFallback > 0 && noneFallback % setup.Parties.Length == 0 && ruleFallback == 0,
                F("(l) the second review's L3: a planted rule that contests nothing leaves no party without local acts - {0} acts, by state {1} (the largest audiences); the fallback counted on {2} party-days of the campaign's {3} days x {4} parties, none under the real rule", noneActs, noneWhere, noneFallback, days, setup.Parties.Length));
            (int castActs, int castInside, string castWhere) = Land(setup);
            sb.Append(F("    read      (l) the rule's cast ({0}): {1} local acts, {2} in the rule's regions of their day (a reactive personality also weighs where it sees an opponent working); by state {3}\n",
                string.Join(", ", setup.Parties.Select(p => p.Name + " " + p.Personality)), castActs, castInside, castWhere));
        }

        /// <summary>The staging with every party cast as <paramref name="personality"/> - its candidate, offices, staff and buys the staging's for that cast - and
        /// <paramref name="rule"/> as its targeting (null strips it, the planted tooth).</summary>
        private static CampaignRun.Setup Cast(CampaignRun.Setup s, AiPersonality personality, RegionTargeting rule)
        {
            var parties = s.Parties.Select((p, i) => new CampaignRun.PartySetup(p.Name, personality, p.Credibility, p.StartingMoney, p.TrueIssueMatch, p.Volunteers,
                LiveCampaignSetup.CandidateFor(personality, p.Name), LiveCampaignSetup.OfficesFor(personality, s.Regions, s.Stands?[i]), p.OfficeOperationsPerDay,
                LiveCampaignSetup.StaffFor(personality), LiveCampaignSetup.TelevisionBuysFor(personality), null, null)).ToArray();
            return WithParties(s, parties, rule);
        }

        private static CampaignRun.Setup WithParties(CampaignRun.Setup s, CampaignRun.PartySetup[] parties, RegionTargeting rule) =>
            new CampaignRun.Setup(s.Calendar, parties, s.PriorShares, s.LoyaltyPerParty, s.Compatibility, s.TrueSalience, s.NationalAudience, s.Regions, s.PublicHouse,
                s.PublicPollEveryDays, s.InternalHouse, s.ElectorateLoyalty, s.Outlets, s.DebateDays, s.Scandals, s.LiveScandalRatePerPartyDay, s.RecordShift, s.Families, s.AwarenessStart,
                s.Grouping, s.Positions, s.Stands, rule);

        private static UsGroundGame.Campaign Copy(UsGroundGame.Campaign c) => c.Copy();

        /// <summary>(m) and (n): a ground game in one state through the US count - the ceiling in every state moving it by US-9's measured amount (PLAN_PREREG),
        /// the worked state alone; the trailing party's ceiling in the closest state flipping it, its electors and nothing else; US-9's scripted program read;
        /// the noise keyed - the same seed the same count, no counted stream drawn.</summary>
        private static void MeasureGroundGame(StringBuilder sb, Action<bool, string> Check, CampaignRun.Setup setup, string[] keys, DateTime polled,
            Dictionary<string, double> atStart, CampaignRun.Result idleAtStart)
        {
            SimulationRandom.Seed(Seed);
            int tieSeed = UsPresidentialElection.TieSeedOf(SimulationRandom.MasterSeed, polled.Year);
            UsPresidentialElection.Contest idleCount = UsPresidentialElection.Count(CountryId.USA, polled, atStart, null, tieSeed, null, null, out ElectoralCollege.StateCall[] derived, out string why);
            UsGroundGame.Campaign none = UsGroundGame.Of(setup, idleAtStart);
            bool empty = none != null && none.DemocratContacts.All(c => c == 0.0) && none.RepublicanContacts.All(c => c == 0.0) && none.DemocratDoors.All(c => c == 0.0)
                         && none.RepublicanDoors.All(c => c == 0.0) && none.DemocratCalls.All(c => c == 0.0) && none.RepublicanCalls.All(c => c == 0.0);
            Check(idleCount != null && derived != null && derived.Length == 51 && empty && none.Regions.SequenceEqual(setup.Regions.Select(r => r.Name)),
                F("(m) US-31: the count's frame on the start's staging - the idle run's ground game read by UsGroundGame.Of ({0} jurisdictions, no contact), base turnout {1:0.0000} (votes cast over the electorate), the derived states the prediction's count{2}",
                    none?.Regions.Length ?? 0, none?.BaseTurnout ?? double.NaN, idleCount == null ? " - NO COUNT: " + why : string.Empty));
            if (idleCount == null || none == null) { return; }
            double stagedEligible = setup.Regions.Sum(r => r.Eligible);
            double stagedCace = GotvTurnout.UsDoorCacePp(setup.NationalAudience / stagedEligible);
            Check(setup.DoorCacePp == stagedCace && stagedCace > 0.0 && none.Regions.Select((s, i) => none.BaseTurnoutAt(i)).All(b => b > 0.0 && b < 1.0),
                F("(m) K18: the US staging's count reads 5b - every state at its own base turnout (votes cast of record over its CVAP, {0:0.0000} to {1:0.0000}), the AI valuing a door at Gerber & Green's band of the national base {2:0.0000}: {3} points (the staging carries {4})",
                    none.Regions.Select((s, i) => none.BaseTurnoutAt(i)).Min(), none.Regions.Select((s, i) => none.BaseTurnoutAt(i)).Max(), setup.NationalAudience / stagedEligible, stagedCace, setup.DoorCacePp));

            // the maximal ground game in one state, either party, noise off - through US-31's registered function (W-B11's curve, `UsGroundGame.EffectsOnCurve`,
            // the count's own until K18) on US-9's own map, where the PLAN_PREREG holds; then through the count's own function since K18 (5b, `UsGroundGame.Effects`),
            // on US-9's map and on this staging's, read beside it (K18 moved the count; a pass rule for 5b's figure is Elias's to register)
            (double Low, double High, double Stray, List<string> Flippable, int FlippableElectors, int NotAt100, string Why) Sweep(ElectoralCollege.StateCall[] map, bool registered)
            {
                double lo = double.MaxValue, hi = 0.0, stray = 0.0;
                var flippable = new List<string>();
                int electors = 0, not100 = 0;
                for (int r = 0; r < none.Regions.Length; r++)
                {
                    var move = new double[2];
                    foreach (bool rep in new[] { false, true })
                    {
                        UsGroundGame.Campaign c = Copy(none);
                        double contacts = Math.Ceiling(CeilingEffort * c.Eligible[r]);
                        if (registered && GotvModel.Mobilization(contacts, c.Eligible[r]) != 100.0) { not100++; }
                        if (rep) { c.RepublicanContacts[r] = contacts; c.RepublicanDoors[r] = contacts; } else { c.DemocratContacts[r] = contacts; c.DemocratDoors[r] = contacts; }
                        List<UsGroundGame.StateEffect> e = registered ? UsGroundGame.EffectsOnCurve(map, c, 0, 0.0, out string whyCeiling) : UsGroundGame.Effects(map, c, 0, 0.0, out whyCeiling);
                        if (e != null && !registered)
                        {
                            // 5b's bound: a party's extra votes are at most its supporters times the band's upper CACE, so a margin moves by at most that CACE over the base
                            double b = c.BaseTurnoutAt(r);
                            if (Math.Abs(100.0 * (e[r].Republican - e[r].Democrat)) > GotvTurnout.DoorCaceUpperPp(CountryId.USA, b) / b + UnworkedPp) { not100++; }
                        }
                        if (e == null) { return (double.NaN, double.NaN, double.NaN, flippable, 0, not100, none.Regions[r] + " not counted: " + whyCeiling); }
                        move[rep ? 1 : 0] = 100.0 * (e[r].Republican - e[r].Democrat);
                        for (int o = 0; o < e.Count; o++) { if (o != r) { stray = Math.Max(stray, Math.Abs(100.0 * (e[o].Republican - e[o].Democrat))); } }
                        lo = Math.Min(lo, Math.Abs(move[rep ? 1 : 0])); hi = Math.Max(hi, Math.Abs(move[rep ? 1 : 0]));
                    }
                    ElectoralCollege.StateCall call = map.First(s => s.State == none.Regions[r]);
                    double m = Margin(call.Shares);
                    if (m > 0 ? m + move[0] < 0 : m + move[1] > 0) { flippable.Add(call.State); electors += call.Electors; }   // US-9's (c) reading: the trailing party's ceiling
                }
                return (lo, hi, stray, flippable, electors, not100, null);
            }
            // K112: every state, either party, measured through 5b against the closed form - the band (smallest and largest closed-form move) and the worst departure
            (double Low, double High, double Worst, int Off, string First, string Why) FiveBBand(ElectoralCollege.StateCall[] map)
            {
                double lo = double.MaxValue, hi = 0.0, worst = 0.0;
                int off = 0;
                string first = null;
                for (int r = 0; r < none.Regions.Length; r++)
                {
                    ElectoralCollege.StateCall call = map.First(s => s.State == none.Regions[r]);
                    foreach (bool rep in new[] { false, true })
                    {
                        UsGroundGame.Campaign c = Copy(none);
                        double doors = Math.Ceiling(CeilingEffort * c.Eligible[r]);
                        if (rep) { c.RepublicanDoors[r] = doors; } else { c.DemocratDoors[r] = doors; }
                        List<UsGroundGame.StateEffect> e = UsGroundGame.Effects(map, c, 0, 0.0, out string why);
                        if (e == null) { return (double.NaN, double.NaN, double.NaN, -1, null, none.Regions[r] + " not counted: " + why); }
                        double measured = 100.0 * (e[r].Republican - e[r].Democrat);
                        double closed = ClosedFormMovePp(call.Shares, rep, c.BaseTurnoutAt(r), c.Eligible[r], doors);
                        lo = Math.Min(lo, Math.Abs(closed)); hi = Math.Max(hi, Math.Abs(closed));
                        double gap = Math.Abs(measured - closed);
                        worst = Math.Max(worst, gap);
                        if (gap > UnworkedPp) { off++; first = first ?? F("{0} {1} measured {2:+0.0000;-0.0000} against {3:+0.0000;-0.0000}", call.State, rep ? "REP" : "DEM", measured, closed); }
                    }
                }
                return (lo, hi, worst, off, first, null);
            }
            var prev2020 = UsPresidentialReturns.States.Where(s => s.Year == 2020).ToArray();
            var rows2024 = UsPresidentialReturns.States.Where(s => s.Year == 2024).ToArray();
            double cast2024 = rows2024.Sum(s => (double)s.VotesTotal);
            double[] national2024 = { rows2024.Sum(s => (double)s.VotesD) / cast2024, rows2024.Sum(s => (double)s.VotesR) / cast2024, rows2024.Sum(s => (double)s.VotesOther) / cast2024 };
            ElectoralCollege.FromNationalShares(2020, 2024, national2024, out ElectoralCollege.StateCall[] us9Map, out double _);   // US-9's idle map: US-4's (a) row, 2024's national shares on 2020's states
            var us9 = Sweep(us9Map, true);
            bool cardRead = Us9CardReading(out double cardLow, out double cardHigh, out int cardFlips, out int cardElectors, out string cardList, out string cardWhy);
            bool asUs9 = cardRead && us9.Why == null && us9.NotAt100 == 0 && Math.Abs(us9.Low - cardLow) <= CardRoundingPp + UnworkedPp && Math.Abs(us9.High - cardHigh) <= CardRoundingPp + UnworkedPp
                         && us9.Flippable.Count == cardFlips && us9.FlippableElectors == cardElectors && string.Join(", ", us9.Flippable.OrderBy(x => x, StringComparer.Ordinal)) == cardList
                         && us9.Stray < UnworkedPp && prev2020.Length == us9Map.Length;
            Check(asUs9,
                F("(m) THE DONE-WHEN, US-9's measured amount (PLAN_PREREG): US-31's registered function (UsGroundGame.EffectsOnCurve - W-B11's curve, the count's own until K18) at US-9's maximal ground game on US-9's own map moves one state's margin {0:0.000} to {1:0.000} points, either party - the card's US-9 block {2:0.00} to {3:0.00} (within its rounding {4}) - and could flip {5} calls, {6} electors ({7}), the card's {8}, {9} ({10}); every unworked state by at most {11:0.0e+0} points{12}",
                    us9.Low, us9.High, cardLow, cardHigh, CardRoundingPp, us9.Flippable.Count, us9.FlippableElectors, string.Join(", ", us9.Flippable), cardFlips, cardElectors, cardList, us9.Stray,
                    cardRead ? (us9.Why == null ? string.Empty : " - " + us9.Why) : " - THE CARD: " + cardWhy));
            // K112 (PREREGISTERED, `Reviews/2026-10-24_usdata_prereg.md` rule 1, committed before this check read it): THE REGISTERED 5b CEILING - every state, either
            // party, through the count's own function (5b) moves its margin by exactly 5b's closed form, within the count's whole-vote rounding
            var us9Now = Sweep(us9Map, false);
            var us9Band = FiveBBand(us9Map);
            Check(us9Now.Why == null && us9Now.NotAt100 == 0 && us9Now.Stray < UnworkedPp && us9Band.Why == null && us9Band.Off == 0,
                F("(m) K112, THE REGISTERED 5b CEILING on US-9's own map: every state, either party, moves by 5b's closed form (the worked party's supporters at the base plus Gerber & Green's band, everyone else at the base) within {0:0.0e+0} points - the band {1:0.000} to {2:0.000} points (worst off the closed form {3:0.0e+0}{4}); each state inside 5b's bound; could flip {5} calls, {6} electors ({7}) - against US-9's registered {8:0.00} to {9:0.00} on the curve; every unworked state by at most {10:0.0e+0} points{11}",
                    UnworkedPp, us9Band.Low, us9Band.High, us9Band.Worst, us9Band.First == null ? string.Empty : ", " + us9Band.First, us9Now.Flippable.Count, us9Now.FlippableElectors, string.Join(", ", us9Now.Flippable),
                    cardLow, cardHigh, us9Now.Stray, (us9Now.Why ?? us9Band.Why) == null ? string.Empty : " - " + (us9Now.Why ?? us9Band.Why)));
            var live = Sweep(derived, false);
            var liveBand = FiveBBand(derived);
            Check(live.Why == null && live.NotAt100 == 0 && live.Stray < UnworkedPp && liveBand.Why == null && liveBand.Off == 0,
                F("(m) K112, THE REGISTERED 5b CEILING on the game's own staging (its map the prediction's count, its electorate the CVAP): every state, either party, by 5b's closed form within {0:0.0e+0} points - the band {1:0.000} to {2:0.000} points (worst off {3:0.0e+0}{4}); could flip {5} calls, {6} electors ({7}); every unworked state by at most {8:0.0e+0} points{9}",
                    UnworkedPp, liveBand.Low, liveBand.High, liveBand.Worst, liveBand.First == null ? string.Empty : ", " + liveBand.First, live.Flippable.Count, live.FlippableElectors, string.Join(", ", live.Flippable), live.Stray,
                    (live.Why ?? liveBand.Why) == null ? string.Empty : " - " + (live.Why ?? liveBand.Why)));
            // the planted tooth (the review's L-5, the prereg's last rule): no ground game anywhere moves no state by 5b's smallest closed-form move
            List<UsGroundGame.StateEffect> idleEffects = UsGroundGame.Effects(derived, none, 0, 0.0, out string _);
            double idleMost = idleEffects == null ? double.NaN : idleEffects.Max(x => Math.Abs(100.0 * (x.Republican - x.Democrat)));
            Check(idleEffects != null && idleMost < liveBand.Low,
                F("(m) the planted tooth - no ground game anywhere: the widest move {0:0.0e+0} points, below 5b's smallest closed-form move {1:0.000}, so the band above bites", idleMost, liveBand.Low));

            // able to flip it: the trailing party's ceiling in the closest winner-take-all state, through the count's own path with its keyed noise
            int noiseSeed = UsGroundGame.NoiseSeedOf(CountryId.USA, polled);
            UsPresidentialElection.Contest noiseOnly = UsPresidentialElection.Count(CountryId.USA, polled, atStart, null, tieSeed, null, null, none, noiseSeed, out ElectoralCollege.StateCall[] noisy, out string whyNoise);
            if (noiseOnly == null || noisy == null) { Check(false, "(m) no count with the declared noise: " + whyNoise); return; }
            int t = Enumerable.Range(0, noisy.Length).Where(i => noisy[i].Districts.Length == 0).OrderBy(i => Math.Abs(Margin(noisy[i].Shares))).ThenBy(i => i).First();
            bool trailingRep = noisy[t].Winner == ElectoralCollege.Democrat;
            int at = Array.IndexOf(none.Regions, noisy[t].State);
            UsGroundGame.Campaign worked = Copy(none);
            double ceilingDoors = Math.Ceiling(CeilingEffort * worked.Eligible[at]);
            if (trailingRep) { worked.RepublicanContacts[at] = ceilingDoors; worked.RepublicanDoors[at] = ceilingDoors; } else { worked.DemocratContacts[at] = ceilingDoors; worked.DemocratDoors[at] = ceilingDoors; }
            UsPresidentialElection.Contest flipped = UsPresidentialElection.Count(CountryId.USA, polled, atStart, null, tieSeed, null, null, worked, noiseSeed, out ElectoralCollege.StateCall[] after, out string _);
            string party = trailingRep ? "REP" : "DEM";
            bool othersSame = after != null && Enumerable.Range(0, noisy.Length).All(i => i == t || (after[i].Winner == noisy[i].Winner && after[i].Shares.SequenceEqual(noisy[i].Shares)));
            bool flips = after != null && after[t].Winner != noisy[t].Winner;
            int gained = flipped != null ? flipped.ElectorsOf(party) - noiseOnly.ElectorsOf(party) : -1;
            // K112/K113, K114 (PREREGISTERED, rule 2 and amendment 1): pass if the call flips; if it does not, the NAMED line (not a failure - Elias's K114) only
            // where the live path itself is judged: nothing else moved, no elector gained, a ground-game effect for every state, the live move the count's own
            // function on the live inputs (to 1e-9 points), and the worked state's 5b move with the noise off on its noisy-map shares the closed form
            List<UsGroundGame.StateEffect> liveWorked = UsGroundGame.Effects(derived, worked, noiseSeed, UsGroundGame.NoiseSigmaPp, out string _);
            List<UsGroundGame.StateEffect> liveNone = UsGroundGame.Effects(derived, none, noiseSeed, UsGroundGame.NoiseSigmaPp, out string _);
            int liveAt = liveWorked == null ? -1 : liveWorked.FindIndex(x => x.State == noisy[t].State);
            double liveExpected = liveAt < 0 || liveNone == null ? double.NaN
                : 100.0 * ((liveWorked[liveAt].Republican - liveWorked[liveAt].Democrat) - (liveNone[liveAt].Republican - liveNone[liveAt].Democrat));
            double liveMove = after != null ? Margin(after[t].Shares) - Margin(noisy[t].Shares) : double.NaN;
            bool liveIsCount = Math.Abs(liveMove - liveExpected) <= 1e-9;
            List<UsGroundGame.StateEffect> flipNoNoise = UsGroundGame.Effects(noisy, worked, 0, 0.0, out string _);
            double flipMove = flipNoNoise == null ? double.NaN : 100.0 * (flipNoNoise[at].Republican - flipNoNoise[at].Democrat);
            double flipClosed = ClosedFormMovePp(noisy[t].Shares, trailingRep, worked.BaseTurnoutAt(at), worked.Eligible[at], ceilingDoors);
            bool asClosed = Math.Abs(flipMove - flipClosed) <= UnworkedPp;
            string flipLine = F("the closest winner-take-all state on the count with the declared noise (seed keyed on {0:yyyy-MM-dd}), {1} at {2:+0.00;-0.00}, worked at the ceiling by the trailing {3}: its margin {2:+0.00;-0.00} -> {4:+0.00;-0.00}, the call {5}; every other state's shares bit for bit the same; {3} gains its {6} electors ({7} -> {8}); 5b's move there {9:+0.000;-0.000} points (noise off), the closed form {10:+0.000;-0.000}",
                polled, noisy[t].State, Margin(noisy[t].Shares), party, after != null ? Margin(after[t].Shares) : double.NaN, flips ? "FLIPS" : "DOES NOT FLIP", noisy[t].Electors,
                noiseOnly.ElectorsOf(party), flipped?.ElectorsOf(party) ?? -1, flipMove, flipClosed);
            if (flips || !othersSame || !asClosed || flipped == null || gained != 0 || flipped.GroundGame.Count != noisy.Length || !liveIsCount)
            {
                Check(flips && othersSame && gained == noisy[t].Electors && flipped.GroundGame.Count == noisy.Length && liveIsCount,
                    "(m) THE DONE-WHEN, able to flip it (K112, preregistered; amendment 1): " + flipLine + F("; the live move {0:+0.000000;-0.000000} against the count's own function {1:+0.000000;-0.000000}", liveMove, liveExpected));
            }
            else
            {
                sb.Append(F("    REGISTERED OUTCOME CHANGED (K113): US-31's able-to-flip clause no longer holds on the live path - {0}; the shortfall {1:0.000} points. K18 changed it and 5b is kept (Elias's K113)\n",
                    flipLine, Math.Abs(Margin(noisy[t].Shares)) - Math.Abs(flipClosed)));
            }

            // the second review's L2: no noise votes for a column that stands nowhere in a state (US-6c, J3) - every state planted with no other candidate on
            // its ballot (its others' share given to the Democratic nominee), counted with the declared noise: every other candidate stays at 0 in every state
            {
                ElectoralCollege.StateCall[] bare = derived.Select(c => new ElectoralCollege.StateCall
                {
                    State = c.State, Electors = c.Electors, PriorShares = c.PriorShares, Winner = c.Winner, Districts = c.Districts, DistrictDemocraticTwoParty = c.DistrictDemocraticTwoParty, DistrictWinners = c.DistrictWinners,
                    Shares = new[] { c.Shares[ElectoralCollege.ShareDemocrat] + c.Shares[ElectoralCollege.ShareOthers], c.Shares[ElectoralCollege.ShareRepublican], 0.0 },
                }).ToArray();
                List<UsGroundGame.StateEffect> bareEffects = UsGroundGame.Effects(bare, none, noiseSeed, UsGroundGame.NoiseSigmaPp, out string whyBare);
                int othersGiven = bareEffects == null ? -1 : bareEffects.Count(e => e.Others != 0.0);
                double bareMove = bareEffects == null ? double.NaN : bareEffects.Max(e => Math.Abs(100.0 * (e.Republican - e.Democrat)));
                int givenInDerived = derived.Count(c => c.Shares[ElectoralCollege.ShareOthers] == 0.0);
                Check(bareEffects != null && othersGiven == 0 && bareMove > 0.0,
                    F("(n) the second review's L2: with no other candidate on any ballot (planted), the declared noise gives every other candidate no vote in any of the {0} states ({1} given one) while it still moves the nominees (widest {2:0.00} points); on the game's own map {3} state(s) list no other candidate{4}",
                        bare.Length, othersGiven, bareMove, givenInDerived, bareEffects == null ? " - NOT COUNTED: " + whyBare : string.Empty));
            }

            // the noise: keyed (the same seed, the same count; another master seed, another), and no counted stream drawn
            Dictionary<SimulationRandom.Stream, int> drawsBefore = SimulationRandom.CaptureDrawCounts();
            UsPresidentialElection.Contest again = UsPresidentialElection.Count(CountryId.USA, polled, atStart, null, tieSeed, null, null, none, UsGroundGame.NoiseSeedOf(CountryId.USA, polled), out ElectoralCollege.StateCall[] _, out string _);
            Dictionary<SimulationRandom.Stream, int> drawsAfter = SimulationRandom.CaptureDrawCounts();
            bool streamsStill = drawsBefore.Count == drawsAfter.Count && drawsBefore.All(kv => drawsAfter.TryGetValue(kv.Key, out int n) && n == kv.Value);
            SimulationRandom.Seed(Seed + 1);
            int otherSeed = UsGroundGame.NoiseSeedOf(CountryId.USA, polled);
            UsPresidentialElection.Contest otherGame = UsPresidentialElection.Count(CountryId.USA, polled, atStart, null, tieSeed, null, null, none, otherSeed, out ElectoralCollege.StateCall[] _, out string _);
            SimulationRandom.Seed(Seed);
            bool keyed = again != null && PresidencyDiagnostic.SameContest(again, noiseOnly) && UsGroundGame.NoiseSeedOf(CountryId.USA, polled) == noiseSeed;
            bool drawn = otherGame != null && otherSeed != noiseSeed && !otherGame.GroundGame.Zip(noiseOnly.GroundGame, (a, b) => a.Democrat == b.Democrat && a.Republican == b.Republican).All(same => same);
            double noiseMost = noiseOnly.GroundGame.Max(x => Math.Abs(100.0 * (x.Republican - x.Democrat)));
            Check(keyed && drawn && streamsStill,
                F("(n) the regional noise is keyed, never counted: the same master seed and day give the same count bit for bit; master seed {0} draws other numbers; no counted stream moved ({1} streams held); the noise alone moves a margin by at most {2:0.00} points (sigma {3:0.0} a share)",
                    Seed + 1, drawsBefore.Count, noiseMost, UsGroundGame.NoiseSigmaPp));

            // US-9's scripted program (read): the REP works GA - its one office there, door-to-door every day - its own ground game's move there
            int ga = Array.IndexOf(none.Regions, ProgramState);
            int repIndex = Array.IndexOf(keys, "REP");
            CampaignActions.ActionSpec act = CampaignActions.Spec(ProgramAct);
            int perDay = (int)Math.Floor(CampaignEconomy.HoursPerCampaignDay / act.Hours);
            Func<int, AiDecision[]> program = day => Enumerable.Repeat(new AiDecision(ProgramAct, new CampaignActions.ActionTarget(ga, -1, null), ProgramState, act.MoneyCost, act.Hours, 0.0, false), perDay).ToArray();
            CampaignRun.PartySetup[] programmed = setup.Parties.Select((p, i) => i != repIndex ? p : new CampaignRun.PartySetup(p.Name, p.Personality, p.Credibility, p.StartingMoney, p.TrueIssueMatch, p.Volunteers,
                p.Candidate, new[] { ga }, p.OfficeOperationsPerDay, p.Staff, p.TelevisionBuys, program, null)).ToArray();
            CampaignRun.Result programRun = CampaignAiHarness.RunSeeded(WithParties(setup, programmed, setup.Targeting), Seed);
            var programShares = new Dictionary<string, double>();
            for (int i = 0; i < keys.Length; i++) { programShares[keys[i]] = programRun.FinalShares[i]; }
            UsPresidentialElection.Count(CountryId.USA, polled, programShares, null, tieSeed, null, null, out ElectoralCollege.StateCall[] programDerived, out string _);
            UsGroundGame.Campaign repOnly = UsGroundGame.Of(setup, programRun);
            if (repOnly != null) { repOnly = Copy(repOnly); for (int r = 0; r < repOnly.DemocratContacts.Length; r++) { repOnly.DemocratContacts[r] = 0.0; repOnly.DemocratDoors[r] = 0.0; repOnly.DemocratCalls[r] = 0.0; } }
            List<UsGroundGame.StateEffect> programEffects = programDerived != null && repOnly != null ? UsGroundGame.Effects(programDerived, repOnly, 0, 0.0, out string _) : null;
            int made = programRun.Parties[repIndex].ActionCount[CampaignAi.IndexOfAction(ProgramAct)];
            double gaMove = programEffects != null ? 100.0 * (programEffects[ga].Republican - programEffects[ga].Democrat) : double.NaN;
            int widest = programEffects == null ? -1 : Enumerable.Range(0, programEffects.Count).OrderByDescending(r => Math.Abs(programEffects[r].Republican - programEffects[r].Democrat)).ThenBy(r => r).First();
            Check(made > 0 && gaMove > 0.0 && widest == ga,
                F("(m) a player's ground game through the count: US-9's scripted program (the REP's one office in {0} and {1} door-to-door a day there; {2} acts made, the other party an AI at its cast) moves {0}'s margin {3:+0.000;-0.000} points by the REP's own contacts - the widest move of any state (US-9's (b) measured the same program on its own staging: the card's US-9 block)",
                    ProgramState, perDay, made, gaMove));
        }

        /// <summary>
        /// PLAN_PREREG (US-31, fixed before the first run): "US-9's measured amount" is US-9's (c) as the US model card's US-9 block prints it
        /// (`docs/reference/US_ELECTIONS.md`, written by `UsCampaignGeographyCheck`; `COMPLETED.md` §846) - the maximal ground game moves one jurisdiction's margin
        /// by its range of points (two decimals) and could flip its list of calls and electors. The US count's own ground-game function, at US-9's maximal
        /// ground game on US-9's own map, must give that range to within half the card's last digit (<see cref="CardRoundingPp"/>) plus the count's whole-vote rounding (<see cref="UnworkedPp"/> - US-9's baseline
        /// is a count with no ground game, this one the derived shares) and the same calls and
        /// electors exactly; the game's own staging's range is read beside it, with no tolerance. Read from the card, never retyped, so a re-measured card
        /// carries the figure; a card this cannot read fails by name.
        /// </summary>
        private static bool Us9CardReading(out double low, out double high, out int flips, out int electors, out string list, out string why)
        {
            low = high = double.NaN; flips = electors = -1; list = null; why = null;
            string path = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "docs", "reference", "US_ELECTIONS.md");
            if (!File.Exists(path)) { why = "docs/reference/US_ELECTIONS.md not found"; return false; }
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(path, Encoding.UTF8),
                @"\*\*The ceiling moves one jurisdiction's margin by ([0-9]+\.[0-9]+) to ([0-9]+\.[0-9]+) points\*\*.*?could flip ([0-9]+) of the idle map's calls - ([0-9]+) electors: ([A-Z]{2}(?:, [A-Z]{2})*)\.");
            if (!m.Success) { why = "the US-9 block's ceiling sentence is not on the card"; return false; }
            low = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            high = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            flips = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
            electors = int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
            list = string.Join(", ", m.Groups[5].Value.Split(new[] { ", " }, StringSplitOptions.None).OrderBy(x => x, StringComparer.Ordinal));
            return true;
        }

        /// <summary>(h), PS-6 US-31: the game's own polling day - the count kept each state's ground game; the stored contest re-derives the counted states
        /// (the derived plus the effect) and, with its effect cleared (the planted tooth), the derived ones; the House swing and the Senate's states carry it.</summary>
        private static void MeasureGameCount(StringBuilder sb, Action<bool, string> Check, Country usa, UsPresidentialElection.Contest played, ElectoralCollege.StateCall[] states,
            ElectoralCollege.StateCall[] derivedStates, string derivedLine, UsGroundGame.Campaign groundCampaign, UsCongressElection.Contest congress, DateTime polled)
        {
            if (played == null || states == null || derivedStates == null || groundCampaign == null)
            {
                Check(false, "(h) US-31: no campaign count, its derived states or its ground game on the polling day - nothing to compare");
                return;
            }
            ElectoralCollege.TryElectorsYear(polled.Year, out int inForce);
            UsGroundGame.Apply(derivedStates, played.GroundGame, inForce, played.TieSeed, out ElectoralCollege.StateCall[] rebuilt);
            bool asCounted = rebuilt.Length == states.Length && rebuilt.Zip(states, (a, b) => a.State == b.State && a.Winner == b.Winner && a.Shares.SequenceEqual(b.Shares) && a.DistrictWinners.SequenceEqual(b.DistrictWinners)).All(same => same);
            bool moved = states.Zip(derivedStates, (a, b) => !a.Shares.SequenceEqual(b.Shares)).Count(x => x) == states.Length;
            List<UsGroundGame.StateEffect> kept = played.GroundGame;
            ElectoralCollege.StateCall[] cleared;
            try
            {
                played.GroundGame = new List<UsGroundGame.StateEffect>();
                cleared = UsPresidentialElection.StatesOf(usa, played);
            }
            finally { played.GroundGame = kept; }
            bool tooth = cleared != null && cleared.Zip(derivedStates, (a, b) => a.Shares.SequenceEqual(b.Shares) && a.Winner == b.Winner).All(same => same);
            Check(played.GroundGame.Count == states.Length && asCounted && moved && tooth && played.Line.Contains("each state counted with the campaign ground game"),
                F("(h) US-31: the polling day counts each of the {0} states with the campaign ground game and the declared noise - the stored contest re-derives the counted states (the derived plus each kept effect) bit for bit, every state moved off its derived shares; the planted tooth, its effects cleared, re-derives the derived states: \"{1}\"",
                    played.GroundGame.Count, played.Line));
            int changed = states.Zip(derivedStates, (a, b) => a.Winner != b.Winner).Count(x => x);
            // (q) PS-6 K29 (Elias's ruling of 2026-10-23: "the carry stays; the popular vote is re-summed from the counted states"): each tally's popular vote is the
            // counted states re-summed on the derivation's weights, recomputed here; the derived states re-summed give the derivation's own shares (the carry kept)
            ElectoralCollege.TryPriorYear(polled.Year, out int priorYear);
            double[] resum = UsPresidentialElection.ResummedShares(states, priorYear), resumDerived = UsPresidentialElection.ResummedShares(derivedStates, priorYear);
            UsPresidentialElection.Tally repT = played.Electors.FirstOrDefault(t => t.Party == "REP"), demT = played.Electors.FirstOrDefault(t => t.Party == "DEM");
            UsPresidentialElection.Tally othT = played.Electors.FirstOrDefault(t => t.Candidate == UsPresidentialElection.OtherCandidates);
            bool popularAgrees = resum != null && repT != null && demT != null && othT != null
                                 && repT.Counted == 100.0 * resum[ElectoralCollege.ShareRepublican] && demT.Counted == 100.0 * resum[ElectoralCollege.ShareDemocrat] && othT.Counted == 100.0 * resum[ElectoralCollege.ShareOthers]
                                 && repT.Popular() == repT.Counted && Math.Abs(repT.Counted + demT.Counted + othT.Counted - 100.0) < 1e-9;
            bool carryKept = resumDerived != null && repT != null && demT != null
                             && Math.Abs(100.0 * resumDerived[ElectoralCollege.ShareRepublican] - repT.Share) < 1e-3 && Math.Abs(100.0 * resumDerived[ElectoralCollege.ShareDemocrat] - demT.Share) < 1e-3;   // within the derivation's own residual, points
            Check(popularAgrees && carryKept && repT.Counted != repT.Share && played.Line.Contains("the popular vote re-summed from the counted states"),
                F("(q) PS-6 K29: the popular vote re-summed from the {0} counted states (weighed by {1}'s votes cast, the derivation's): REP {2:0.0000} %, DEM {3:0.0000} %, others {4:0.0000} % - against the derived {5:0.0000} / {6:0.0000} %, which the derived states re-sum to (the carry kept: the states still re-derive from it)",
                    states.Length, priorYear, repT?.Counted ?? double.NaN, demT?.Counted ?? double.NaN, othT?.Counted ?? double.NaN, repT?.Share ?? double.NaN, demT?.Share ?? double.NaN));
            sb.Append(F("    read      (h) the same shares counted without the ground game: {0}; calls apart {1}\n", derivedLine ?? "no count", changed));

            if (congress == null) { Check(false, "(h) US-31: no Congress count on the polling day"); return; }
            Dictionary<string, double> swing = UsGroundGame.TwoPartySwing(played.GroundGame);
            List<UsCongressElection.StateHouse> with = UsCongressElection.CountHouse(polled, congress.V, null, swing, out int _, out double _, out bool _, out string _);
            List<UsCongressElection.StateHouse> without = UsCongressElection.CountHouse(polled, congress.V, null, null, out int _, out double _, out bool _, out string _);
            bool houseAsCounted = with != null && with.Count == congress.House.Count && with.Zip(congress.House, (a, b) => a.State == b.State && a.Share == b.Share && a.Rep == b.Rep).All(same => same);
            bool houseMoved = with != null && without != null && with.Zip(without, (a, b) => a.Share != b.Share).Any(x => x);
            int seatsApart = with != null && without != null ? with.Zip(without, (a, b) => Math.Abs(a.Rep - b.Rep)).Sum() : -1;
            var senateWrong = new List<string>();
            int senateMoved = 0;
            foreach (UsCongressElection.SenateSeat seat in congress.Senate)
            {
                ElectoralCollege.StateCall c = states.FirstOrDefault(s => s.State == seat.State), d = derivedStates.FirstOrDefault(s => s.State == seat.State);
                if (c == null || d == null) { senateWrong.Add(seat.State + " none"); continue; }
                double parent = c.Shares[ElectoralCollege.ShareRepublican] / (c.Shares[ElectoralCollege.ShareRepublican] + c.Shares[ElectoralCollege.ShareDemocrat]);
                double parentDerived = d.Shares[ElectoralCollege.ShareRepublican] / (d.Shares[ElectoralCollege.ShareRepublican] + d.Shares[ElectoralCollege.ShareDemocrat]);
                if (seat.Parent != parent) { senateWrong.Add(seat.State); }
                if (parent != parentDerived) { senateMoved++; }
            }
            Check(houseAsCounted && houseMoved && senateWrong.Count == 0 && senateMoved == congress.Senate.Count && congress.Senate.Count > 0 && congress.Line.Contains("carry the ground game"),
                F("(h) US-31, I3's \"alike\": the House swing carries each state's ground game (R-US18 (b) recounted with it gives the stored House bit for bit; without it {0} seat(s) apart), and every one of the {1} Senate seats reads its state's counted share, none its derived one{2}",
                    seatsApart, congress.Senate.Count, senateWrong.Count == 0 ? string.Empty : " - WRONG: " + string.Join(", ", senateWrong)));

            // the second review's M1: two cycles - the ground-game count, then a midterm with no campaign (2 U.S.C. §7), on the poll's V and either side of it:
            // its House is the one a base counted with no ground game gives, share for share and seat for seat, so no cycle's ground game compounds
            if (without == null) { return; }
            var midterm = new DateTime(polled.Year + 2, 11, 1);
            while (midterm.DayOfWeek != DayOfWeek.Tuesday || midterm.Day == 1) { midterm = midterm.AddDays(1); }
            var noGround = new UsCongressElection.Contest { PollingDay = congress.PollingDay, V = congress.V, House = without };
            var buggy = new UsCongressElection.Contest
            {
                PollingDay = congress.PollingDay, V = congress.V,
                House = congress.House.Select(h => new UsCongressElection.StateHouse { State = h.State, Seats = h.Seats, Rep = h.Rep, Dem = h.Dem, Share = h.ShareWithoutGround, UnopposedRep = h.UnopposedRep, UnopposedDem = h.UnopposedDem }).ToList(),
            };
            bool twoCycles = true;
            int buggyApart = 0, groundSeats = congress.House.Sum(h => Math.Abs(h.GroundSeats));
            foreach (double v2 in new[] { congress.V - 0.01, congress.V, congress.V + 0.01 })
            {
                List<UsCongressElection.StateHouse> a = UsCongressElection.CountHouse(midterm, v2, congress, out int _, out double _, out bool _, out string _);
                List<UsCongressElection.StateHouse> b = UsCongressElection.CountHouse(midterm, v2, noGround, out int _, out double _, out bool _, out string _);
                List<UsCongressElection.StateHouse> bug = UsCongressElection.CountHouse(midterm, v2, buggy, out int _, out double _, out bool _, out string _);
                twoCycles &= a != null && b != null && a.Count == b.Count && a.Zip(b, (x, y) => x.State == y.State && x.Rep == y.Rep && x.Share == y.Share).All(same => same);
                if (a != null && bug != null) { buggyApart += a.Zip(bug, (x, y) => Math.Abs(x.Rep - y.Rep)).Sum(); }
            }
            Check(twoCycles && (groundSeats == 0 || buggyApart > 0),
                F("(h) US-31, the second review's M1 - two cycles: a midterm on {0:yyyy-MM-dd} (no campaign) swung from the ground-game count gives, at V {1:0.0000} and a point either side, the House a base counted with no ground game gives, share for share and seat for seat; the ground game moved {2} seat(s) in 2024, and the planted tooth (the base keeping those seats) is {3} seat(s) apart{4}",
                    midterm, congress.V, groundSeats, buggyApart, groundSeats == 0 ? " - no seat moved by the ground game this run, so the tooth cannot bite (read)" : string.Empty));
        }

        /// <summary>(o): R-US16 (c) is not ruled - Sweden's and Germany's stagings declare no targeting rule, so their AI weighs the largest audiences as before
        /// (their counts read no ground game: `UsCampaignGeographyCheck` holds that no other runtime file calls W-D1's count).</summary>
        private static void MeasureGate(StringBuilder sb, Action<bool, string> Check)
        {
            foreach (CountryId country in new[] { CountryId.Sweden, CountryId.Germany })
            {
                using (SimulationManager.EpochScope())
                {
                    WorldClock.ApplyStart(country);
                    CampaignCalendar cal = CampaignCalendar.FromWorldStart(WorldClock.LatestElectionDay(country), WorldClock.StartDate(country));
                    bool staged = LiveCampaignSetup.TryFor(country, new (int, int, Scandal)[0], cal, out CampaignRun.Setup s, out string _, onVoteModelCompatibility: true);
                    Check(staged && s.Targeting == null, F("(o) US-31 is the USA's alone: {0}'s staging declares no targeting rule{1}", country, staged ? string.Empty : " - NOT STAGED"));
                }
            }
        }

        /// <summary>The setup with every party idle - no office, no act, no hire, no buy, no debate, no story - and everything else as staged.</summary>
        private static CampaignRun.Setup Idle(CampaignRun.Setup s)
        {
            Func<int, AiDecision[]> nothing = day => new AiDecision[0];
            var parties = s.Parties.Select(p => new CampaignRun.PartySetup(p.Name, p.Personality, p.Credibility, p.StartingMoney, p.TrueIssueMatch, p.Volunteers, p.Candidate,
                new int[0], p.OfficeOperationsPerDay, new StaffRole[0], 0, nothing, null)).ToArray();
            return new CampaignRun.Setup(s.Calendar, parties, s.PriorShares, s.LoyaltyPerParty, s.Compatibility, s.TrueSalience, s.NationalAudience, s.Regions, s.PublicHouse,
                s.PublicPollEveryDays, s.InternalHouse, s.ElectorateLoyalty, s.Outlets, new int[0], new (int, int, Scandal)[0], 0.0, s.RecordShift, s.Families, s.AwarenessStart,
                s.Grouping, s.Positions, s.Stands);
        }

        /// <summary>A world on the USA's start as a new game builds one, the player's country and party set.</summary>
        private static (SimulationManager Sim, World World, Country Usa) Open(List<GameObject> hosts)
        {
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("UsCampaignDiagnostic"); hosts.Add(go);
            var s = go.AddComponent<SimulationManager>();
            s.SetWorld(w);
            s.PlayerCountryId = CountryId.USA;
            w.GetCountry(CountryId.USA).PlayerPartyAbbrev = PlayerParty;
            return (s, w, w.GetCountry(CountryId.USA));
        }

        /// <summary>One day in the controller's order (GameController: AdvanceDay, the player's day tick, the boundary's turn).</summary>
        private static void Step(SimulationManager s, World w)
        {
            bool boundary = s.AdvanceDay();
            if (s.PlayerCountryId.HasValue) { s.AdvanceCountryDayTick(s.PlayerCountryId.Value); }
            if (boundary)
            {
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in w.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                s.AdvanceTurn(decisions);
            }
        }

        private static string Shares(string[] keys, double[] shares) => string.Join(" ", keys.Select((k, i) => F("{0} {1:0.0000} %", k, 100.0 * shares[i])));

        /// <summary>Each geography's "Total" line of a CVAP table: its name and its citizen voting-age population (cvap_est).</summary>
        private static Dictionary<string, long> ReadCvapTotals(string csv)
        {
            var totals = new Dictionary<string, long>();
            string[] lines = csv.Replace("\r\n", "\n").Split('\n');
            List<string> header = SplitCsv(lines[0]);
            int name = header.IndexOf("geoname"), title = header.IndexOf("lntitle"), est = header.IndexOf("cvap_est");
            if (name < 0 || title < 0 || est < 0) { return totals; }
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) { continue; }
                List<string> cells = SplitCsv(lines[i]);
                if (cells.Count <= Math.Max(est, Math.Max(name, title)) || cells[title] != "Total") { continue; }
                if (long.TryParse(cells[est], NumberStyles.Integer, CultureInfo.InvariantCulture, out long v)) { totals[cells[name]] = v; }
            }
            return totals;
        }

        /// <summary>UF's file: each state's name and its postal code (STATE, STATE_ABV).</summary>
        private static Dictionary<string, string> ReadUfAbbreviations(string csv)
        {
            var map = new Dictionary<string, string>();
            string[] lines = csv.Replace("\r\n", "\n").Split('\n');
            List<string> header = SplitCsv(lines[0]);
            int name = header.IndexOf("STATE"), ab = header.IndexOf("STATE_ABV");
            if (name < 0 || ab < 0) { return map; }
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) { continue; }
                List<string> cells = SplitCsv(lines[i]);
                if (cells.Count > Math.Max(name, ab) && cells[ab].Length == 2) { map[cells[name]] = cells[ab]; }
            }
            return map;
        }

        private static List<string> SplitCsv(string line)
        {
            var cells = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            foreach (char ch in line)
            {
                if (ch == '"') { quoted = !quoted; }
                else if (ch == ',' && !quoted) { cells.Add(cell.ToString()); cell.Clear(); }
                else { cell.Append(ch); }
            }
            cells.Add(cell.ToString());
            return cells;
        }

        private static string PathOf(string relative) => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), relative);

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create()) { return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2", CultureInfo.InvariantCulture))); }
        }

        private static bool DigestListed(string relative, string digest)
        {
            string sums = File.ReadAllText(PathOf(Path.GetDirectoryName(relative).Replace('\\', '/') + "/SHA256SUMS.txt"), Encoding.ASCII);
            return sums.Replace("\r\n", "\n").Split('\n').Contains(digest + " *" + Path.GetFileName(relative));
        }
    }
}
