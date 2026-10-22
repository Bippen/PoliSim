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
            Check(strayOffices == 0 && setup.Parties.All(p => p.StartingMoney == LiveCampaignSetup.WarChest) && note.Contains("BILLED"),
                F("(f) every staged office on a jurisdiction where its party stands ({0}); every chest the model's one price table ({1:N0} each - a US table in dollars BILLED, the note says so)",
                    string.Join(", ", Enumerable.Range(0, keys.Length).Select(p => keys[p] + " " + string.Join("/", setup.Parties[p].Offices.Select(r => setup.Regions[r].Name)))), LiveCampaignSetup.WarChest));

            // ---- (g) an idle campaign on the start's staging is the start's prediction
            NationalElection.TryPredictShares(CountryId.USA, out Dictionary<string, double> atStart);
            CampaignRun.Result idleAtStart = CampaignAiHarness.RunSeeded(Idle(setup), Seed);
            double offAtStart = Enumerable.Range(0, keys.Length).Max(p => Math.Abs(idleAtStart.FinalShares[p] - atStart[keys[p]]));
            Check(offAtStart < 1e-12 && idleAtStart.Parties.Sum(l => l.TotalActions) == 0,
                F("(g) an idle campaign (no act, no debate, no story) on the start's staging ends on the start's prediction: {0} - within {1:0.0e+0}", Shares(keys, idleAtStart.FinalShares), offAtStart));

            // ---- (h) A US GAME'S OWN DAY PATH - the REP the player's party, stepped from the start through the polling day as the game steps it
            string saveMid = Path.Combine(dir, "mid_campaign.json");
            UsPresidentialElection.Contest played = null;
            CampaignRun.Setup liveSetup = default;
            Dictionary<string, double> counted = null, predicted = null, countShift = null, dayShift = null;
            DateTime? runUpSeen = null, campaignSeen = null, pollingDaySeen = null;
            int regionsSeen = 0, playerIndex = -1;
            bool cut = false, droppedAfter = false, resultAfter = false, recount = false;
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
                            UsPresidentialElection.Contest again = UsPresidentialElection.Count(CountryId.USA, today, counted, UsTermLimit.NomineesFor(usa, today),
                                UsPresidentialElection.TieSeedOf(SimulationRandom.MasterSeed, today.Year), previous,
                                previous != null ? UsPresidentialElection.StatesOf(usa, previous) : null, out ElectoralCollege.StateCall[] _, out string whyAgain);
                            recount = again != null && PresidencyDiagnostic.SameContest(again, played);
                            recountWhat = again?.Line ?? whyAgain;
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
                Check(replayed && loadedCount != null && played != null && PresidencyDiagnostic.SameContest(loadedCount, played),
                    F("(i) the save cut {0:yyyy-MM-dd} (format {1}) loads into the game with its campaign replayed ({2} day(s)); stepped to the polling day it counts the same contest: \"{3}\"",
                        CutOn, save.SaveVersion, replayedDays, loadedCount?.Line ?? "no count"));
            }
            finally { EnergyMarket.ResetTurnState(); }

            sb.Append("    note      ").Append(note.Replace("\n", "\n              ").Trim()).Append('\n');
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
