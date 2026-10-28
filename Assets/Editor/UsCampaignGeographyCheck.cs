using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-9 (`docs/specs/USA_STAGE_PLAN.md`, US-9; asks R-US16): THE CAMPAIGN-GEOGRAPHY PROOF - a US campaign staged on the jurisdictions in the
    /// EDITOR ONLY, wiring nothing, and measured three ways before anything is fought state by state:
    /// <list type="bullet">
    /// <item><description><b>(a) where the AI's local acts land.</b> Its candidate regions are the largest audiences (<see cref="CampaignAi.LocalCandidateRegions"/>,
    /// [AUTHORED-DRAFT]); each run's local acts are counted by jurisdiction - the share in the closest 2020 states against the largest, and against the
    /// states R-US16 (b)'s declared rule would pick on the run's last published poll.</description></item>
    /// <item><description><b>(b) a local act reaches a state only through the national total.</b> The count reads the campaign's national final shares, and
    /// `NationalElection.DeriveRegional` spreads them afterwards - "a readout of the national result, never an input to it". A scripted party (the player's
    /// stand-in, W-C2's seam) works one state; the state's derived share is held against the national move and against every other state's.</description></item>
    /// <item><description><b>(c) W-D1's per-region count</b> (<see cref="ElectionDay.Count"/> over the campaign's <see cref="RegionalMobilization"/> - pure, wired
    /// to nothing): how far it would move one state under a maximal ground game (the mobilisation curve's ceiling), against the 2024 margins of record; the
    /// scripted ground game's own move under it; the AI's; W-D1's declared regional noise; and R-US16 (c)'s size - the same count over every staged
    /// country's campaign.</description></item>
    /// </list>
    /// <para><b>The mirror, not a seam.</b> `DeriveRegional` is private and US-8 will change the runtime derivation, so this reads no seam: its mirror is the
    /// call `DeriveRegional` makes (<see cref="RegionalVoteModel.RegionalSharesByUniformSwing"/> on the regions and their prior), proved equal to the last bit
    /// on every staged country's campaign (the public path, `NationalElection.SharesFromCampaign`); the USA, which the runtime does not derive, takes it on
    /// 2020's jurisdictions - R-US14 (a), RULED, US-4's method. When the runtime starts deriving US states this fails by name, to be re-pointed.</para>
    /// <para><b>The national shares a run implies</b> (DECLARED): E1's construction - each nominee's support the party's share times the nominee's factor,
    /// every other column of the field at its share of record (`PresidencyOfRecord.CandidatesOf`) - with this harness's IDLE run standing for the reference
    /// world, so the factor is the record's share over the idle run's: the idle run gives 2024's national shares exactly, as US-6's factors do on its
    /// reference world, and a campaign moves them by its own change to each party's share. No reference world is held here (US-6's check holds it).</para>
    /// <para>Every parameter is declared below before the first run - the seed, the sets' size, the electorate, the ceiling, the replays, the salience, the
    /// scripted program, the targeting rule - and none is tuned on a result: a change after a run is a new reading and is recorded as one. Pinned in the
    /// cheap bar and the documents bar through the US model card's US-9 block (`docs/reference/US_ELECTIONS.md`, *The readings (US-9)*), written by
    /// <see cref="WriteReadings"/>; a block that no longer says what this measures fails here. No world is built and nothing live is touched.</para>
    /// </summary>
    public static class UsCampaignGeographyCheck
    {
        // ---- THE DECLARED PARAMETERS (US-9, committed before the first run) ----

        /// <summary>DECLARED: every campaign run's seed and the noise replays' - the project's film seed, the one `CampaignAiHarness` runs; one seed, so each
        /// figure is one run's.</summary>
        private const int Seed = 777;

        /// <summary>DECLARED: the election the jurisdictions' audience (the plan's "2020's valid votes"), their prior shares and the districts' prior are read
        /// from - US-3's catalog.</summary>
        private const int PriorYear = 2020;

        /// <summary>DECLARED: the election whose national shares the idle run reproduces, whose electors count, and whose margins of record the moves are held
        /// against - US-4's pair is <see cref="PriorYear"/> to this.</summary>
        private const int PredictedYear = 2024;

        /// <summary>DECLARED: the field of record is the election held for the term ending in this year (`PresidencyOfRecord.CandidatesOf`, US-6).</summary>
        private const int FieldTermEnd = 2025;

        /// <summary>DECLARED: the maximal ground game - weighted contacts per eligible voter in the one state, far past where `GotvModel.Mobilization` reaches
        /// its ceiling in double precision (asserted: the attribute reads exactly 100). No budget buys more (GotvModel's own words): the ceiling is the model's,
        /// not a campaign's.</summary>
        private const double CeilingEffort = 40.0;

        /// <summary>DECLARED: the replays of the per-state count under W-D1's declared regional noise - `ElectionDayHarness`'s replay count.</summary>
        private const int NoiseReplays = 400;

        // K18 (Elias's ruling of 2026-10-23): the card is re-cut to the inputs play stages on - the salience the game's US staging reads for the campaign
        // (`LiveCampaignSetup.UsSalience`: Gallup's August 2024 wave for the 2024 campaign, US-30) and the electorate it mobilises (`UsRegions.EligibleOf`: the
        // Census Bureau's CVAP 2020-2024, US-30), read from the game's own staging and never retyped. Until K18 the card read Gallup's July 2026 column and the
        // University of Florida Election Lab's VEP; the VEP stays read below for one purpose only, the registered reading (see RegisteredRelative).

        /// <summary>DECLARED (K18): the electorate US-9 was registered on - the University of Florida Election Lab's VEP for 2024 (saved byte for byte, its digest
        /// held to its folder's SHA256SUMS.txt). The ceiling's registered outcome (the range, the calls and the electors US-31's PLAN_PREREG reads off the card)
        /// is measured again on it beside the re-cut, so a re-cut that changed a registered outcome fails here by name instead of re-registering silently.</summary>
        private const string RegisteredRelative = "ElectionsData/usa/raw/returns/ufl_turnout_2024G_v0.4.csv";

        /// <summary>The scripted program's act (DECLARED): door-to-door in the one state, as many a day as the candidate's day holds at the act's own hours, each
        /// at the act's own price; the party's day-0 office plan the one office there. Its staging is otherwise its cast's (chest, volunteers, staff).</summary>
        private const CampaignActionKind ProgramAct = CampaignActionKind.DoorToDoor;

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsCampaignGeographyCheck (PS-6 US-9): the campaign on the jurisdictions in the Editor - where the AI's local acts land, a local act through the national total, W-D1's per-state count; R-US16's sizes ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string expected = CardBlock(card, digest);
                string onDisk = CardBlockOf(File.ReadAllText(PathOf(CardRelative), Encoding.UTF8));
                Check(onDisk == expected, onDisk == null ? F("the model card ({0}) carries no US-9 block", CardRelative)
                    : onDisk == expected ? F("the model card's US-9 block ({0}) says what this measures - {1} lines", CardRelative, card.Count)
                    : F("the model card's US-9 block ({0}) is STALE - regenerate: -executeMethod PoliSim.EditorTools.UsCampaignGeographyCheck.WriteReadings", CardRelative));
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }

            sb.Append(failures == 0 ? "=== UsCampaignGeographyCheck: the geography holds ===" : F("=== UsCampaignGeographyCheck: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Writes the card's US-9 block from this reading - the stamp and its END marker placed once by hand; refuses while any check fails.</summary>
        public static void WriteReadings()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsCampaignGeographyCheck.WriteReadings (PS-6 US-9): the US model card's US-9 block ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string path = PathOf(CardRelative);
                string text = File.ReadAllText(path, Encoding.UTF8);
                string old = CardBlockOf(text);
                if (failures > 0) { Check(false, "a check above failed - the card is not written; fix what failed, then write"); }
                else if (old == null) { Check(false, F("the model card ({0}) carries no US-9 block to write into", CardRelative)); }
                else
                {
                    string written = text.Replace("\r\n", "\n").Replace(old, CardBlock(card, digest));
                    if (text.Contains("\r\n")) { written = written.Replace("\n", "\r\n"); }
                    File.WriteAllText(path, written, new UTF8Encoding(false));
                    Check(true, F("the model card's US-9 block written ({0}) - {1} lines", CardRelative, card.Count));
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }

            sb.Append(failures == 0 ? "=== WriteReadings: written ===" : F("=== WriteReadings: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>The reading <see cref="Run"/> checks and <see cref="WriteReadings"/> writes, the random streams put back after. Returns the block's digest.</summary>
        private static string Measure(StringBuilder sb, Action<bool, string> Check, List<string> card)
        {
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try { return MeasureInner(sb, Check, card); }
            finally { SimulationRandom.RestoreState(seedWas, drawsWas); }
        }

        private static string MeasureInner(StringBuilder sb, Action<bool, string> Check, List<string> card)
        {
            int k = CampaignAi.LocalCandidateRegions;

            // ---- the jurisdictions: US-3's catalog, the prior election's rows and the predicted election's, one order
            var prev = UsPresidentialReturns.States.Where(s => s.Year == PriorYear).ToArray();
            var next = UsPresidentialReturns.States.Where(s => s.Year == PredictedYear).ToArray();
            Check(prev.Length > k && prev.Select(s => s.State).SequenceEqual(next.Select(s => s.State)),
                F("{0} and {1}: the same {2} jurisdictions in the same order (US-3's catalog)", PriorYear, PredictedYear, prev.Length));
            int n = prev.Length;
            string[] names = prev.Select(s => s.State).ToArray();
            double[][] prior = prev.Select(s => Shares(s.VotesD, s.VotesR, s.VotesOther, s.VotesTotal)).ToArray();
            double[][] recordNext = next.Select(s => Shares(s.VotesD, s.VotesR, s.VotesOther, s.VotesTotal)).ToArray();
            var regionInputs = prev.Select(s => new RegionalVoteModel.RegionInput(s.State, s.VotesTotal, null)).ToArray();
            double[] natCatalog = National(next.Select(s => (s.VotesD, s.VotesR, s.VotesOther, s.VotesTotal)));
            double[][] Mirror(double[] national) => RegionalVoteModel.RegionalSharesByUniformSwing(national, regionInputs, prior, out double _);
            int[] largest = Enumerable.Range(0, n).OrderByDescending(r => prev[r].VotesTotal).ThenBy(r => r).Take(k).ToArray();
            int[] closest = Enumerable.Range(0, n).OrderBy(r => Math.Abs(prior[r][1] - prior[r][0])).ThenBy(r => r).Take(k).ToArray();
            int[] closestNext = Enumerable.Range(0, n).OrderBy(r => Math.Abs(recordNext[r][1] - recordNext[r][0])).ThenBy(r => r).Take(k).ToArray();
            string Names(IEnumerable<int> rs) => string.Join(", ", rs.Select(r => names[r]));
            sb.Append(F("  the {0} largest {1} audiences: {2}; the {0} closest {1} states: {3}; the {0} closest {4} states: {5}\n", k, PriorYear, Names(largest), Names(closest), PredictedYear, Names(closestNext)));

            // ---- the electorate (K18): the game's own, the Census Bureau's CVAP (UsRegions, held figure by figure to its file by UsCampaignDiagnostic)
            double[] eligible = names.Select(s => (double)UsRegions.EligibleOf(s)).ToArray();
            var missing = names.Where(s => UsRegions.EligibleOf(s) <= 0).ToList();
            Check(missing.Count == 0, F("the electorate: the game's staging's own (UsRegions.EligibleOf - the Census Bureau's CVAP 2020-2024, US-30), one for every jurisdiction{0}",
                missing.Count == 0 ? string.Empty : " - MISSING: " + string.Join(", ", missing)));
            double usBase = prev.Sum(s => (double)s.VotesTotal) / eligible.Sum();
            // the registered electorate (UF's VEP, US-9's own), its bytes held to the folder's digest list - read for the registered reading of (c) alone
            byte[] turnoutBytes = File.ReadAllBytes(PathOf(RegisteredRelative));
            string turnoutDigest = Sha256(turnoutBytes);
            bool turnoutHashed = DigestListed(RegisteredRelative, turnoutDigest);
            Dictionary<string, double> vep = ReadVep(Encoding.UTF8.GetString(turnoutBytes));
            var vepMissing = names.Where(s => !vep.ContainsKey(s)).ToList();
            Check(turnoutHashed && vepMissing.Count == 0, F("the registered electorate: UF's VEP ({0}) - its SHA-256 in SHA256SUMS.txt, a VEP for every jurisdiction{1}", Path.GetFileName(RegisteredRelative),
                vepMissing.Count == 0 ? string.Empty : " - MISSING: " + string.Join(", ", vepMissing)));
            double[] registeredEligible = names.Select(s => vep.TryGetValue(s, out double v) ? v : double.NaN).ToArray();
            double registeredBase = prev.Sum(s => (double)s.VotesTotal) / registeredEligible.Sum();

            // ---- the field of record (US-6) and the anchor: E1's construction on the idle run
            IReadOnlyList<PresidencyOfRecord.CandidateOfRecord> field = PresidencyOfRecord.CandidatesOf(CountryId.USA, FieldTermEnd);
            string ticketD = UsPresidentialReturns.Candidates.Where(c => c.Year == PredictedYear && c.Ticket == "D").Select(c => c.Candidate).FirstOrDefault();
            string ticketR = UsPresidentialReturns.Candidates.Where(c => c.Year == PredictedYear && c.Ticket == "R").Select(c => c.Candidate).FirstOrDefault();
            string dKey = field.Where(c => c.Surname == ticketD).Select(c => c.BackingParty).FirstOrDefault();
            string rKey = field.Where(c => c.Surname == ticketR).Select(c => c.BackingParty).FirstOrDefault();
            double[] recordShare = field.Select(c => PresidencyOfRecord.ShareOfRecord(CountryId.USA, PredictedYear, c.Surname)).ToArray();
            Check(field.Count > 2 && dKey != null && rKey != null && dKey != rKey && recordShare.All(x => !double.IsNaN(x)),
                F("the field of record (US-6): {0} columns, the Democratic nominee backed by {1}, the Republican by {2}, every column's share of record read", field.Count, dKey ?? "none", rKey ?? "none"));

            // ---- the staging, at the USA's start (the epoch a US game opens on)
            string[] keys;
            double[] compatibility, priorShares, loyalty;
            AiPersonality[] casts;
            CampaignCalendar calendar = CampaignCalendar.FromWorldStart(WorldClock.ElectionDayOf(CountryId.USA, ElectionVintage.Usa2024), WorldClock.StartDate(CountryId.USA));
            Dictionary<string, double> predicted;
            bool staged, unwired;
            using (SimulationManager.EpochScope())
            {
                WorldClock.ApplyStart(CountryId.USA);
                staged = NationalElection.TryCompatibility(CountryId.USA, out keys, out compatibility, out priorShares, out loyalty);
                NationalElection.TryPredictShares(CountryId.USA, out predicted);
                unwired = NationalElection.LastRegionalShares == null;
            }
            Check(staged && predicted != null && keys != null && keys.SequenceEqual(LiveCampaignSetup.Keys(CountryId.USA)) && keys.Contains(dKey) && keys.Contains(rKey),
                F("staged on US-6's vote model (NationalElection.TryCompatibility at the USA's start, {0:yyyy-MM-dd}): parties {1}", WorldClock.StartDate(CountryId.USA), keys == null ? "none" : string.Join(", ", keys)));
            if (!staged) { throw new InvalidOperationException("no US vote model to stage on (US-6)"); }
            int pd = Array.IndexOf(keys, dKey), pr = Array.IndexOf(keys, rKey);
            casts = LiveCampaignSetup.CastsOf(CountryId.USA, keys);
            string castLine = string.Join(", ", keys.Select((key, i) => key + " " + casts[i]));
            var regions = new RegionAudience[n];
            for (int r = 0; r < n; r++) { regions[r] = new RegionAudience(names[r], prev[r].VotesTotal, eligible: eligible[r]); }
            double nationalAudience = regions.Sum(x => x.Audience);
            // the salience (K18): the game's own US staging's for this campaign's opening (US-30's newest Gallup wave fielded before it), never retyped
            double[] salience = LiveCampaignSetup.UsSalience(calendar.CampaignStart, out string salienceLabel);
            double salienceImmigration = salience[(int)IssueId.Immigration], salienceEconomy = salience[(int)IssueId.Economy];
            Check(salienceLabel.StartsWith("Gallup MIP August 2024", StringComparison.Ordinal) && !salienceLabel.Contains("fielded after"),
                F("the salience: the game's US staging's for a campaign opening {0:yyyy-MM-dd} (LiveCampaignSetup.UsSalience) - {1}; K18 re-cuts the card to Gallup's August 2024 wave", calendar.CampaignStart, salienceLabel));
            LiveCampaignSetup.TryFor(CountryId.Sweden, new (int, int, Scandal)[0], null, out CampaignRun.Setup houses, out _);   // the staging's own houses (W-E4's ladder), read, not retyped
            double electorateLoyalty = LoyaltyModel.WeightedMeanLoyalty(loyalty, priorShares);

            CampaignRun.PartySetup Party(int p, AiPersonality personality, int[] offices, Func<int, AiDecision[]> script)
            {
                var match = new double[IssueVector.IssueCount];
                for (int i = 0; i < match.Length; i++) { match[i] = double.IsNaN(salience[i]) ? double.NaN : LiveCampaignSetup.FlatIssueMatch; }
                return new CampaignRun.PartySetup(keys[p], personality, LiveCampaignSetup.FlatCredibility, LiveCampaignSetup.WarChest, match, LiveCampaignSetup.Volunteers,
                    LiveCampaignSetup.CandidateFor(personality, keys[p]), offices ?? LiveCampaignSetup.OfficesFor(personality, regions), LiveCampaignSetup.OfficeOperationsPerDay,
                    LiveCampaignSetup.StaffFor(personality), LiveCampaignSetup.TelevisionBuysFor(personality), script, null);
            }
            CampaignRun.Setup Staging(CampaignRun.PartySetup[] parties, int[] debateDays) => new CampaignRun.Setup(calendar, parties, priorShares, loyalty, compatibility, salience,
                nationalAudience, regions, new PollingHouse(houses.PublicHouse.Name, houses.PublicHouse.SampleSize, houses.PublicHouse.Cost, new double[keys.Length]),
                LiveCampaignSetup.PublicPollEveryDays, new PollingHouse(houses.InternalHouse.Name, houses.InternalHouse.SampleSize, houses.InternalHouse.Cost, new double[keys.Length], isInternal: true),
                electorateLoyalty, null, debateDays);
            Func<int, AiDecision[]> nothing = day => new AiDecision[0];
            sb.Append(F("  the staging: {0} on the {1} jurisdictions (audience their {2} votes cast, {3:N0} together; electorate the Census Bureau's CVAP 2020-2024, {5:N0}; salience {10}); the campaign {6:yyyy-MM-dd} to {7:yyyy-MM-dd}; casts by CampaignCasts.Of: {8}; seed {9}\n",
                string.Join(" and ", keys), n, PriorYear, nationalAudience, PredictedYear, eligible.Sum(), calendar.CampaignStart, calendar.ElectionDate.AddDays(-1), castLine, Seed, salienceLabel));

            // ---- THE IDLE RUN: nothing in it - no act, no debate, no story - and it is the prediction
            var idleParties = new CampaignRun.PartySetup[keys.Length];
            for (int p = 0; p < keys.Length; p++) { idleParties[p] = Party(p, casts[p], new int[0], nothing); }
            (CampaignRun.Result idle, _) = RunUs(Staging(idleParties, new int[0]));
            double[] idleFinal = idle.FinalShares;
            double idleOff = keys.Select((key, i) => Math.Abs(idleFinal[i] - predicted[key])).Max();
            int idleActs = idle.Parties.Sum(l => l.TotalActions);
            Check(idleOff < 1e-12 && idleActs == 0 && idle.FinalShares.SequenceEqual(idle.BaselineShares),
                F("the idle run (no act, no debate, no story) is the prediction: {0} - NationalElection.TryPredictShares within {1:0.0e+0}, {2} acts", Pct(keys, idleFinal), idleOff, idleActs));

            double[] NationalOf(double[] final)
            {
                var support = new double[field.Count];
                double sum = 0.0;
                for (int c = 0; c < field.Count; c++)
                {
                    int at = field[c].BackingParty == null ? -1 : Array.IndexOf(keys, field[c].BackingParty);
                    support[c] = at >= 0 ? recordShare[c] * final[at] / idleFinal[at] : recordShare[c];
                    sum += support[c];
                }
                double d = 0.0, rep = 0.0, other = 0.0;
                for (int c = 0; c < field.Count; c++)
                {
                    double share = support[c] / sum;
                    if (field[c].Surname == ticketD) { d += share; } else if (field[c].Surname == ticketR) { rep += share; } else { other += share; }
                }
                return new[] { d, rep, other };
            }
            double[] natIdle = NationalOf(idleFinal);
            double anchorOff = Enumerable.Range(0, 3).Max(i => Math.Abs(natIdle[i] - natCatalog[i]));
            Check(anchorOff < 1e-12, F("the anchor: the idle run's national shares by E1's construction are the catalog's {0} sums - D {1}, R {2}, others {3} (within {4:0.0e+0})",
                PredictedYear, Pct(natIdle[0]), Pct(natIdle[1]), Pct(natIdle[2]), anchorOff));

            // ---- THE MIRROR: the runtime derives no US state; the mirror is R-US14 (a) on the prior election's jurisdictions
            Check(unwired, unwired ? "the runtime derives no US state (NationalElection.DeriveRegional leaves the USA unwired): the mirror is R-US14 (a), US-4's method, on the prior election's jurisdictions"
                : "NationalElection.DeriveRegional now derives US states (US-8): re-point this check's mirror at it - its inputs are the presidential national shares (D, R, others) and the prior election's jurisdictions - and re-read (b)");
            double[][] mapIdle = Mirror(natIdle);
            int[] evIdle = Electors(mapIdle, prior, next, out int wrongIdle, out string wrongIdleList);
            var appointed = UsPresidentialReturns.Years.First(y => y.Year == PredictedYear);
            int recR = appointed.CastR + appointed.OthersRSlate, recD = appointed.CastD + appointed.OthersDSlate;
            Check(evIdle[ElectoralCollege.Republican] == recR && evIdle[ElectoralCollege.Democrat] == recD,
                F("the idle map is US-4's (a) row: electors R {0} - D {1} (the record as appointed {2} - {3}), states called wrong {4} ({5})", evIdle[ElectoralCollege.Republican], evIdle[ElectoralCollege.Democrat], recR, recD, wrongIdle, wrongIdleList));

            // the per-state count's frame (W-D1): one base turnout - it cancels from every state's shares unless a rate clamps, which is asserted against
            double maxMultiplier = TurnoutModel.Multiplier(100.0, TurnoutModel.MobilizationSpan);
            Check(usBase * maxMultiplier < 1.0, F("the per-state count's base turnout ({0:0.0000}: the votes cast over the electorate, as staged) clamps no rate at the mobilisation ceiling ({1:0.0000})", usBase, usBase * maxMultiplier));
            RegionalMobilization Empty() => new RegionalMobilization((double[])eligible.Clone(), 3);
            ElectionDay.Result CountStates(double[][] map, RegionalMobilization gotv, double sigmaPp, System.Random random) =>
                ElectionDay.Count(names, map, gotv, usBase, 50.0, 50.0, 50.0, random, sigmaPp);
            ElectionDay.Result Exact(double[][] map, RegionalMobilization gotv) => CountStates(map, gotv, 0.0, new System.Random(Seed));
            RegionalMobilization Columns(RegionalMobilization campaign) => CopyColumns(campaign, eligible, new[] { pd, pr }, Check);
            {
                double[][] back = CountShares(Exact(mapIdle, Empty()));
                double worst = Enumerable.Range(0, n).Max(r => Math.Abs(back[r][0] - mapIdle[r][0]) + Math.Abs(back[r][1] - mapIdle[r][1]));
                Check(worst < 1e-5, F("a jurisdiction nobody works counts its derived shares back (W-D1, no noise): the largest difference {0:0.0e+0} - the count's whole votes", worst));
            }

            // ---- (a) WHERE THE AI'S LOCAL ACTS LAND
            var runs = new List<(string Label, AiPersonality[] Casts)> { ("the rule's cast (the game's)", casts) };
            foreach (AiPersonality personality in PersonalityCatalog.TheFive) { runs.Add(("both " + personality, Enumerable.Repeat(personality, keys.Length).ToArray())); }
            card.Add("### (a) Where the AI's local acts land");
            card.Add("");
            card.Add(F("The AI weighs a local act only in its candidate regions - the {0} largest audiences (`CampaignAi.LocalCandidateRegions`, [AUTHORED-DRAFT]), and, for a reactive personality, the regions it sees an opponent working. Each run below is the whole campaign ({1:d MMM} to {2:d MMM yyyy}, seed {3}), every party an AI; its local acts (rally, town hall, door-to-door) counted by jurisdiction. \"The rule's {0}\" are the jurisdictions R-US16 (b)'s declared rule would pick on that run's last published poll (below). This is US-9's staging, which declares no targeting rule; since US-31 the game's US staging declares one and every AI weighs its local acts where it says (`UsCampaignDiagnostic` measures it).",
                k, calendar.CampaignStart, calendar.ElectionDate.AddDays(-1), Seed));
            card.Add("");
            card.Add(F("The {0} largest {1} audiences: {2}. The {0} closest {1} states: {3}.", k, PriorYear, Names(largest), Names(closest)));
            card.Add("");
            card.Add(F("| run | casts | local acts | in the {0} largest | in the {0} closest | in the rule's {0} | jurisdictions reached | most acts | offices (in the {0} largest) | compatibility at the close, before the clamp |", k));
            card.Add("|---|---|---:|---:|---:|---:|---:|---|---:|---|");
            (CampaignRun.Result Result, Poll? Tracker) ruleRun = default;
            double[] ruleWeights = null;
            foreach ((string label, AiPersonality[] runCasts) in runs)
            {
                var parties = new CampaignRun.PartySetup[keys.Length];
                for (int p = 0; p < keys.Length; p++) { parties[p] = Party(p, runCasts[p], null, null); }
                CampaignRun.Setup runSetup = Staging(parties, null);
                (CampaignRun.Result run, Poll? tracker) = RunUs(runSetup);
                int[] acts = new int[n];
                int stray = 0;
                for (int p = 0; p < keys.Length; p++) { stray += CountLocal(run.Parties[p], names, acts); }
                double[] weights = RuleWeights(tracker, keys, NationalOf, Mirror, next);
                int[] ruleTop = Top(weights, k);
                int total = acts.Sum(), offices = 0, officesLargest = 0;
                for (int p = 0; p < keys.Length; p++) { for (int r = 0; r < n; r++) { if (run.Offices[p].HasOffice(r)) { offices++; if (largest.Contains(r)) { officesLargest++; } } } }
                string most = string.Join(", ", Enumerable.Range(0, n).Where(r => acts[r] > 0).OrderByDescending(r => acts[r]).ThenBy(r => r).Take(k).Select(r => F("{0} {1}", names[r], acts[r])));
                Check(stray == 0, F("(a) {0}: every local act lands on a staged jurisdiction ({1} acts{2})", label, total, stray == 0 ? string.Empty : F(", {0} NOT", stray)));
                string Share(int[] set) => total == 0 ? "-" : F("{0:0.0} %", 100.0 * set.Sum(r => acts[r]) / total);
                card.Add(F("| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8} ({9}) | {10} |", label, string.Join(", ", keys.Select((key, i) => key + " " + runCasts[i])), total, Share(largest), Share(closest),
                    ruleTop.Length == 0 ? "- (no contested state)" : Share(ruleTop), acts.Count(a => a > 0), most.Length == 0 ? "-" : most, offices, officesLargest, Compat(keys, CompatibilityAtClose(runSetup, run))));
                sb.Append(F("  (a) {0}: {1} local acts - {2} in the {3} largest, {4} in the {3} closest, {5} in the rule's {3}; most: {6}\n", label, total, Share(largest), k, Share(closest), Share(ruleTop), most.Length == 0 ? "-" : most));
                if (ruleRun.Result == null) { ruleRun = (run, tracker); ruleWeights = weights; }
            }

            // R-US16 (b)'s DECLARED targeting rule, on the rule's-cast run's last published poll
            card.Add("");
            card.Add("**R-US16 (b)'s targeting rule** (DECLARED for this measurement: a jurisdiction's electors × §25's swing index - `SwingRegions.Index`, [AUTHORED-DRAFT] full scale - of its margin derived from the last published poll's national shares, by E1's construction and R-US14 (a)):");
            card.Add("");
            {
                int[] top = Top(ruleWeights, k);
                double all = ruleWeights == null ? 0.0 : ruleWeights.Sum();
                string poll = ruleRun.Tracker.HasValue ? Pct(keys, keys.Select((key, i) => ruleRun.Tracker.Value.Share(i)).ToArray()) : "none published";
                card.Add(top.Length == 0
                    ? F("On the rule's-cast run's last published poll ({0}) no jurisdiction is contested: every derived gap is at or past the index's full scale.", poll)
                    : F("On the rule's-cast run's last published poll ({0}) the rule's {1} are {2} - {3:0.0} % of the rule's whole weight over {4} jurisdictions with any.", poll, k,
                        string.Join(", ", top.Select(r => F("{0} ({1} electors, index {2:0})", names[r], next[r].Electors, ruleWeights[r] / next[r].Electors))), 100.0 * top.Sum(r => ruleWeights[r]) / all, ruleWeights.Count(w => w > 0.0)));
            }

            // ---- (b) A LOCAL ACT REACHES A STATE ONLY THROUGH THE NATIONAL TOTAL
            // PS-6 US-31 (R-US16 (b), I3): the US count reads W-D1's per-region count where a campaign ran, from its own file
            List<string> callers = CountCallers(@"\bElectionDay\s*\.\s*Count\b", "Elections/ElectionDay.cs", out int _);
            // the review's L4: the ground game's own entry points stay on the US path - its effects read by the US count alone, a campaign's ground game
            // read (and built) by the day loop's US count alone, once
            List<string> effectCallers = CountCallers(@"\bUsGroundGame\s*\.\s*(Effects|NoiseSeedOf)\b", "Elections/UsGroundGame.cs", out int _);
            List<string> campaignReaders = CountCallers(@"\bUsGroundGame\s*\.\s*Of\b|new\s+(UsGroundGame\s*\.\s*)?Campaign\b", "Elections/UsGroundGame.cs", out int campaignReads);
            bool pathOnly = effectCallers.Count == 1 && effectCallers[0] == "Assets/Scripts/Elections/UsPresidentialElection.cs"
                            && campaignReaders.Count == 1 && campaignReaders[0] == "Assets/Scripts/Simulation/SimulationManager.cs" && campaignReads == 1;
            Check(pathOnly, pathOnly
                ? "the ground game is reached on the US path alone: its effects (UsGroundGame.Effects, NoiseSeedOf) from UsPresidentialElection.cs, a campaign's ground game read once, from SimulationManager.cs's US count"
                : F("the ground game is reached beyond the US path - its effects from {0}; a campaign's ground game from {1} ({2} reads)", effectCallers.Count == 0 ? "nowhere" : string.Join(", ", effectCallers), campaignReaders.Count == 0 ? "nowhere" : string.Join(", ", campaignReaders), campaignReads));
            // GATE-18 q2 (the ORDER's "turnout and the declared noise are on in play") with A-LOOP q24 5b: the parliamentary count is W-D1's second caller -
            // Sweden's, Germany's and Poland's chambers counted region by region (ParliamentaryCount); the US count stays its own file's
            bool usOnly = callers.Count == 2 && callers.Contains(ParliamentaryCountFile) && callers.Contains(UsCountFile);   // a set, not an order (the review's L8)
            Check(usOnly, usOnly
                ? "W-D1's per-region count is called from the US count (" + UsCountFile + ", PS-6 US-31 - where a campaign ran, R-US16 (b)) and the parliamentary count (" + ParliamentaryCountFile + ", GATE-18 q2 with A-LOOP q24 5b - Sweden's, Germany's and Poland's chambers) and from no other runtime file; the (b) table below is the count as US-9 measured it, before US-31"
                : "W-D1's per-region count is called from " + (callers.Count == 0 ? "no runtime file - neither count reads it" : string.Join(", ", callers)) + " - not the US count and the parliamentary count alone: the premise of R-US16 (b) and of GATE-18 q2 as built has moved: re-measure");
            CampaignActions.ActionSpec act = CampaignActions.Spec(ProgramAct);
            int perDay = (int)Math.Floor(CampaignEconomy.HoursPerCampaignDay / act.Hours);
            int x = closest[0], y = largest[0];
            card.Add("");
            card.Add("### (b) A local act reaches a state only through the national total");
            card.Add("");
            card.Add(F("A scripted party - the player's stand-in, W-C2's seam - works one jurisdiction: its one office there from day 0 and, every campaign day, {0} {1} there ({2:N0} kr and {3:0.#} hours each, the model's one price table - a US table is BILLED), its staging otherwise its cast's; the other party is an AI at its cast. Each program is held against the same campaign with the scripted party idle (no office, no act). The jurisdictions worked: {4}, the closest {5} state, and {6}, the largest {5} audience.",
                perDay, ProgramAct, act.MoneyCost, act.Hours, names[x], PriorYear, names[y]));
            card.Add("");
            card.Add("| party | works | acts made | its mobilisation there (50 untouched) | its nominee nationally (pp) | that jurisdiction's derived move (pp) | largest departure of any jurisdiction's move from the national (pp) | its margin R − D, (a)'s count: idle → program | (b)'s count: idle → program (of which turnout) | electors R − D, (a): idle → program | (b): idle → program |");
            card.Add("|---|---|---:|---:|---:|---:|---:|---|---|---|---|");
            var programRows = new List<(string Party, string Where, double TurnoutPp, bool FlipsB)>();
            double widestDeparture = 0.0, ownDeparture = 0.0;
            double widestNational = 0.0, leastCompatibility = double.MaxValue;
            int programRuns = 0, saturatedRuns = 0, boundBreaches = 0, observedBreaches = 0;
            double widestEffect = 0.0;
            // A-LOOP q24 5a, the plan's (4)(b): every party's campaign effect at the close inside Σ_class upper × coverage, in every run of this table
            void Saturation(CampaignRun.Setup s, CampaignRun.Result r)
            {
                double[] c = CompatibilityAtClose(s, r); programRuns++; if (c.All(v => v >= ElectionScales.Max)) { saturatedRuns++; } leastCompatibility = Math.Min(leastCompatibility, c.Min());
                for (int p = 0; p < r.CampaignEffectPp.Length; p++) { widestEffect = Math.Max(widestEffect, Math.Abs(r.CampaignEffectPp[p])); if (Math.Abs(r.CampaignEffectPp[p]) > r.CampaignBoundPp[p] + 1e-12) { boundBreaches++; } }
                if (!CampaignPersuasionDiagnostic.ObservedMatchesSources(s, r, out _)) { observedBreaches++; }   // review 881b's M-r1: the observed move against the one its tally implies, recomputed independently
            }
            foreach (int q in new[] { pr, pd })
            {
                int col = q == pd ? 0 : 1;
                var idleQ = new CampaignRun.PartySetup[keys.Length];
                for (int p = 0; p < keys.Length; p++) { idleQ[p] = p == q ? Party(p, casts[p], new int[0], nothing) : Party(p, casts[p], null, null); }
                CampaignRun.Setup baseSetup = Staging(idleQ, null);
                (CampaignRun.Result baseRun, _) = RunUs(baseSetup);
                Saturation(baseSetup, baseRun);
                double[] natBase = NationalOf(baseRun.FinalShares);
                double[][] mapBase = Mirror(natBase);
                double[][] countBase = CountShares(Exact(mapBase, Columns(baseRun.Gotv)));
                int[] evBaseA = Electors(mapBase, prior, next, out _, out _), evBaseB = Electors(countBase, prior, next, out _, out _);
                foreach (int t in new[] { x, y })
                {
                    int target = t;
                    Func<int, AiDecision[]> program = day => Enumerable.Repeat(new AiDecision(ProgramAct, new CampaignActions.ActionTarget(target, -1, null), names[target], act.MoneyCost, act.Hours, 0.0, false), perDay).ToArray();
                    var programmed = new CampaignRun.PartySetup[keys.Length];
                    for (int p = 0; p < keys.Length; p++) { programmed[p] = p == q ? Party(p, casts[p], new[] { target }, program) : Party(p, casts[p], null, null); }
                    CampaignRun.Setup programSetup = Staging(programmed, null);
                    (CampaignRun.Result run, _) = RunUs(programSetup);
                    Saturation(programSetup, run);
                    int made = run.Parties[q].ActionCount[CampaignAi.IndexOfAction(ProgramAct)];
                    double mobilisation = GotvModel.Mobilization(run.Gotv.WeightedContacts(target, q), run.Gotv.Eligible(target));
                    Check(made > 0 && mobilisation > 50.0, F("(b) {0} works {1}: {2} acts made, its mobilisation there {3:0.000} - the program ran", keys[q], names[target], made, mobilisation));
                    double[] natRun = NationalOf(run.FinalShares);
                    double[][] mapRun = Mirror(natRun);
                    double natMove = 100.0 * (natRun[col] - natBase[col]);
                    double ownMove = 100.0 * (mapRun[target][col] - mapBase[target][col]);
                    double departure = Enumerable.Range(0, n).Max(r => Math.Abs(100.0 * (mapRun[r][col] - mapBase[r][col]) - natMove));
                    widestDeparture = Math.Max(widestDeparture, departure);
                    widestNational = Math.Max(widestNational, Math.Abs(natMove));
                    ownDeparture = Math.Max(ownDeparture, Math.Abs(ownMove - natMove));
                    double[][] countRun = CountShares(Exact(mapRun, Columns(run.Gotv)));
                    double[][] countRunIdleGotv = CountShares(Exact(mapRun, Empty()));
                    double turnout = Margin(countRun[target]) - Margin(countRunIdleGotv[target]);
                    int[] evRunA = Electors(mapRun, prior, next, out _, out _), evRunB = Electors(countRun, prior, next, out _, out _);
                    bool flipsA = Math.Sign(Margin(mapRun[target])) != Math.Sign(Margin(mapBase[target]));
                    bool flipsB = Math.Sign(Margin(countRun[target])) != Math.Sign(Margin(countBase[target]));
                    card.Add(F("| {0} | {1} | {2} | {3:0.000} | {4:+0.00e+0;-0.00e+0} | {5:+0.00e+0;-0.00e+0} | {6:0.0e+0} | {7:+0.000;-0.000} → {8:+0.000;-0.000}{9} | {10:+0.000;-0.000} → {11:+0.000;-0.000} ({12:+0.000;-0.000}){13} | {14} – {15} → {16} – {17} | {18} – {19} → {20} – {21} |",
                        keys[q], names[target], made, mobilisation, natMove, ownMove, departure, Margin(mapBase[target]), Margin(mapRun[target]), flipsA ? " FLIPS" : "",
                        Margin(countBase[target]), Margin(countRun[target]), turnout, flipsB ? " FLIPS" : "",
                        evBaseA[ElectoralCollege.Republican], evBaseA[ElectoralCollege.Democrat], evRunA[ElectoralCollege.Republican], evRunA[ElectoralCollege.Democrat],
                        evBaseB[ElectoralCollege.Republican], evBaseB[ElectoralCollege.Democrat], evRunB[ElectoralCollege.Republican], evRunB[ElectoralCollege.Democrat]));
                    sb.Append(F("  (b) {0} works {1}: {2} acts, mobilisation {3:0.000}; nominee {4:+0.00e+0;-0.00e+0} pp nationally, {1} {5:+0.00e+0;-0.00e+0} pp (largest departure {6:0.0e+0}); margin (a) {7:+0.000;-0.000} -> {8:+0.000;-0.000}, (b) {9:+0.000;-0.000} -> {10:+0.000;-0.000} (turnout {11:+0.000;-0.000})\n",
                        keys[q], names[target], made, mobilisation, natMove, ownMove, departure, Margin(mapBase[target]), Margin(mapRun[target]), Margin(countBase[target]), Margin(countRun[target]), turnout));
                    programRows.Add((keys[q], names[target], turnout, flipsB));
                }
            }
            card.Add("");
            card.Add(F("Under the count as built ((a)'s columns) the worked jurisdiction moved by what the nation moved to within {0:0.0e+0} points, and every jurisdiction to within {1:0.0e+0}: the derivation is a function of the national shares and the prior alone (R-US14 (a)'s uniform swing gives every jurisdiction one move, re-solved only where a floor binds), so working a state moves it as much as every state it was not worked in. Under the count US-9 measured, the ground game's turnout - the mobilisation the program built - reached no count at all: W-D1's per-region count, its one reader, was wired to nothing. (b)'s columns are that count, the derived shares as its preferences (below) - since US-31 the count the game makes where a campaign ran (R-US16 (b)), here without its regional noise.",
                ownDeparture, widestDeparture));
            Check(boundBreaches == 0, F("A-LOOP q24 5a (4)(b): in all {0} runs every party's campaign effect lies inside its sourced bound (the widest effect {1:0.000} pp; {2} breaches)", programRuns, widestEffect, boundBreaches));
            Check(observedBreaches == 0, F("A-LOOP q24 5a (4)(b)/(c), independent: in all {0} runs every party's OBSERVED move from its no-campaign share is the move its tally implies under the sourced CACEs ({1} run(s) breaching)", programRuns, observedBreaches));
            Check(CampaignPersuasionDiagnostic.AiValuesWithinSource(Staging(idleParties, new int[0]), out string aiValues), "A-LOOP q24 5a (review 881's H1): the AI's value of a contact at the US electorate is the sourced arithmetic and blind to turnout - " + aiValues);
            card.Add("");
            card.Add(F("The national move itself is at most {0:0.00e+0} points. Since A-LOOP q24 5a the campaign moves the shares by the sourced effects (`CampaignPersuasion`: Kalla & Broockman's CACEs per contact class times each class's coverage of the electorate) and writes no compatibility, so no clamp decides it: the widest campaign effect on any party in the {1} runs of this table is {2:0.000} points, every one inside its sourced bound. The compatibility entering the preference model is the staging's own; in {3} of the {1} runs both parties' sat at the scale's ceiling (`ElectionScales.Max` {4:0}; the least {5:0.00}).",
                widestNational, programRuns, widestEffect, saturatedRuns, ElectionScales.Max, leastCompatibility));

            // ---- (c) W-D1'S PER-STATE COUNT UNDER A MAXIMAL GROUND GAME
            card.Add("");
            card.Add("### (c) W-D1's per-state count under a maximal ground game");
            card.Add("");
            card.Add(F("The count (`ElectionDay.Count`, no noise): each jurisdiction's preferences its derived shares on the idle map ({0}'s national shares from {1}'s jurisdictions, R-US14 (a) - US-4's row), its electorate the game's staging's CVAP (K18), one base turnout ({2:0.0000}, the votes cast over the electorate - it cancels from every share; no rate clamps), the other attributes neutral. **The maximal ground game** is one party's mobilisation at the curve's ceiling in one jurisdiction (`GotvModel.Mobilization` at 100, its supporters' turnout × {3:0.00} - `TurnoutModel.MobilizationSpan`, [AUTHORED-DRAFT]): no budget buys more.",
                PredictedYear, PriorYear, usBase, maxMultiplier));
            var ceilingRows = new List<string>();
            double moveMin = double.MaxValue, moveMax = 0.0;
            var flippable = new List<int>();
            double[][] baseCount = CountShares(Exact(mapIdle, Empty()));
            var ceilingMove = new double[n][];
            var notCeiling = new List<string>();
            for (int r = 0; r < n; r++)
            {
                ceilingMove[r] = new double[2];
                for (int c = 0; c < 2; c++)
                {
                    RegionalMobilization ceiling = Empty();
                    double contacts = Math.Ceiling(CeilingEffort * eligible[r]);
                    GotvSpec spec = GotvModel.Spec(GotvOperation.DoorKnocking);
                    ceiling.Operate(r, c, GotvOperation.DoorKnocking, contacts * spec.MoneyPerContact, contacts * spec.VolunteerHoursPerContact, out _, out _);
                    if (ceiling.Mobilization(r, c) != 100.0) { notCeiling.Add(F("{0} column {1}: {2:R}", names[r], c, ceiling.Mobilization(r, c))); }
                    ceilingMove[r][c] = Margin(CountShares(Exact(mapIdle, ceiling))[r]) - Margin(baseCount[r]);
                    moveMin = Math.Min(moveMin, Math.Abs(ceilingMove[r][c]));
                    moveMax = Math.Max(moveMax, Math.Abs(ceilingMove[r][c]));
                }
                double m = Margin(baseCount[r]);
                bool flips = m > 0 ? m + ceilingMove[r][0] < 0 : m + ceilingMove[r][1] > 0;
                if (flips) { flippable.Add(r); }
                ceilingRows.Add(F("| {0} | {1} | {2:+0.00;-0.00} | {3:+0.00;-0.00} | {4:+0.00;-0.00} | {5:+0.00;-0.00} | {6} |", names[r], next[r].Electors, Margin(recordNext[r]), m, ceilingMove[r][0], ceilingMove[r][1], flips ? "flips" : "-"));
            }
            Check(notCeiling.Count == 0 && moveMin > 0.0, F("the maximal ground game reads mobilisation 100 in every jurisdiction, for either party, and moves each: {0:0.00} to {1:0.00} points of margin{2}",
                moveMin, moveMax, notCeiling.Count == 0 ? string.Empty : " - NOT AT THE CEILING: " + string.Join("; ", notCeiling)));
            card.Add("");
            card.Add(F("**The ceiling moves one jurisdiction's margin by {0:0.00} to {1:0.00} points** (R − D, either party's, over every jurisdiction), and could flip {2} of the idle map's calls - {3} electors: {4}. The {5} closest {6} states of record: {7}.",
                moveMin, moveMax, flippable.Count, flippable.Sum(r => next[r].Electors), flippable.Count == 0 ? "none" : Names(flippable), k, PredictedYear,
                string.Join(", ", closestNext.Select(r => F("{0} {1:+0.00;-0.00} (the ceiling's move for the trailing party {2:+0.00;-0.00})", names[r], Margin(recordNext[r]), Margin(recordNext[r]) > 0 ? ceilingMove[r][0] : ceilingMove[r][1])))));
            sb.Append(F("  (c) the ceiling: {0:0.00} to {1:0.00} pp of margin; could flip {2} calls ({3} electors): {4}\n", moveMin, moveMax, flippable.Count, flippable.Sum(r => next[r].Electors), flippable.Count == 0 ? "none" : Names(flippable)));

            // K18, THE REGISTERED READING: the same ceiling on the electorate US-9 was registered on (UF's VEP, its own base turnout) - US-31's PLAN_PREREG reads
            // the range, the calls and the electors off the sentence above, so the re-cut must print what the registered inputs print, at the card's precision,
            // or this fails by name: a re-cut that changes a registered outcome is put to Elias, never re-registered by regenerating the card.
            {
                double regMin = double.MaxValue, regMax = 0.0;
                var regFlippable = new List<int>();
                var regNotCeiling = new List<string>();
                ElectionDay.Result Registered(RegionalMobilization gotv) => ElectionDay.Count(names, mapIdle, gotv, registeredBase, 50.0, 50.0, 50.0, new System.Random(Seed), 0.0);
                double[][] regBase = CountShares(Registered(new RegionalMobilization((double[])registeredEligible.Clone(), 3)));
                for (int r = 0; r < n; r++)
                {
                    var move = new double[2];
                    for (int c = 0; c < 2; c++)
                    {
                        var ceiling = new RegionalMobilization((double[])registeredEligible.Clone(), 3);
                        double contacts = Math.Ceiling(CeilingEffort * registeredEligible[r]);
                        GotvSpec spec = GotvModel.Spec(GotvOperation.DoorKnocking);
                        ceiling.Operate(r, c, GotvOperation.DoorKnocking, contacts * spec.MoneyPerContact, contacts * spec.VolunteerHoursPerContact, out _, out _);
                        if (ceiling.Mobilization(r, c) != 100.0) { regNotCeiling.Add(names[r]); }
                        move[c] = Margin(CountShares(Registered(ceiling))[r]) - Margin(regBase[r]);
                        regMin = Math.Min(regMin, Math.Abs(move[c]));
                        regMax = Math.Max(regMax, Math.Abs(move[c]));
                    }
                    double m = Margin(regBase[r]);
                    if (m > 0 ? m + move[0] < 0 : m + move[1] > 0) { regFlippable.Add(r); }
                }
                string Printed(double lo, double hi, List<int> flips) => F("{0:0.00} to {1:0.00} points, {2} calls - {3} electors: {4}", lo, hi, flips.Count, flips.Sum(r => next[r].Electors), flips.Count == 0 ? "none" : Names(flips));
                string recut = Printed(moveMin, moveMax, flippable), registered = Printed(regMin, regMax, regFlippable);
                bool regClamps = registeredBase * maxMultiplier >= 1.0;
                Check(recut == registered && regNotCeiling.Count == 0 && !regClamps,
                    F("K18: the re-cut does not change US-9's registered outcome - the ceiling on the game's CVAP {0}; on the registered VEP (base turnout {1:0.0000}) {2}{3}",
                        recut, registeredBase, registered, recut == registered ? string.Empty : " - THE RE-CUT CHANGED A REGISTERED OUTCOME: US-31's PLAN_PREREG reads it off the card; put to Elias, the card not re-registered"));
                card.Add("");
                card.Add(F("**The registered reading** (K18): US-9 was registered on UF's VEP for 2024 (base turnout {0:0.0000}) and Gallup's July 2026 column; the ceiling on that electorate reads {1} - the re-cut's figures above to the card's precision, so the re-cut moves no registered outcome. The salience enters no figure of (c): the idle map and the ceiling read none.",
                    registeredBase, registered));
            }

            // the scripted ground game under the per-state count (from (b)), and the AI's own
            card.Add("");
            card.Add(F("**The staged ground game** (the scripted programs above, the model's one price table): its turnout move in the jurisdiction it worked - {0}.",
                string.Join("; ", programRows.Select(p => F("{0} in {1} {2:+0.000;-0.000}{3}", p.Party, p.Where, p.TurnoutPp, p.FlipsB ? " (flips it)" : string.Empty)))));
            {
                double[] natRule = NationalOf(ruleRun.Result.FinalShares);
                double[][] mapRule = Mirror(natRule);
                double[][] countRule = CountShares(Exact(mapRule, Columns(ruleRun.Result.Gotv)));
                double[][] countRuleIdle = CountShares(Exact(mapRule, Empty()));
                int widest = Enumerable.Range(0, n).OrderByDescending(r => Math.Abs(Margin(countRule[r]) - Margin(countRuleIdle[r]))).ThenBy(r => r).First();
                int[] evA = Electors(mapRule, prior, next, out _, out _), evB = Electors(countRule, prior, next, out _, out _);
                card.Add("");
                card.Add(F("**The AI's own ground game** (the rule's-cast run): its national result {0} - D {1}, R {2} by E1's construction; under the per-state count its widest turnout move is {3}'s, {4:+0.000;-0.000} points of margin; electors R − D {5} – {6} under (a)'s count, {7} – {8} under (b)'s.",
                    Pct(keys, ruleRun.Result.FinalShares), Pct(natRule[0]), Pct(natRule[1]), names[widest], Margin(countRule[widest]) - Margin(countRuleIdle[widest]),
                    evA[ElectoralCollege.Republican], evA[ElectoralCollege.Democrat], evB[ElectoralCollege.Republican], evB[ElectoralCollege.Democrat]));
            }

            // W-D1's declared regional noise on the idle map
            {
                SimulationRandom.Seed(Seed);
                System.Random noise = SimulationRandom.For(SimulationRandom.Stream.ElectionNoise);
                var flips = new int[n];
                int presidencyFlips = 0, noMajority = 0;
                int winnerIdle = ElectoralCollege.Winner(Electors(baseCount, prior, next, out _, out _));
                for (int i = 0; i < NoiseReplays; i++)
                {
                    double[][] noisy = CountShares(CountStates(mapIdle, Empty(), RegionalAggregation.RegionalNoiseSigmaPp, noise));
                    for (int r = 0; r < n; r++) { if (Math.Sign(Margin(noisy[r])) != Math.Sign(Margin(baseCount[r]))) { flips[r]++; } }
                    int winner = ElectoralCollege.Winner(Electors(noisy, prior, next, out _, out _));
                    if (winner < 0) { noMajority++; } else if (winner != winnerIdle) { presidencyFlips++; }
                }
                int[] closeOnMap = Enumerable.Range(0, n).OrderBy(r => Math.Abs(Margin(baseCount[r]))).ThenBy(r => r).Take(k).ToArray();
                card.Add("");
                card.Add(F("**W-D1's declared regional noise** (σ {0:0.0} points a party a jurisdiction, `RegionalAggregation.RegionalNoiseSigmaPp`; {1} replays on the idle map, seed {2}, no ground game): {3} jurisdictions change hands in at least one replay; the map's {4} closest - {5}; the presidency changes hands in {6} of {1}, no majority in {7}.",
                    RegionalAggregation.RegionalNoiseSigmaPp, NoiseReplays, Seed, flips.Count(f => f > 0), k,
                    string.Join(", ", closeOnMap.Select(r => F("{0} (margin {1:+0.00;-0.00}) {2:0.0} %", names[r], Margin(baseCount[r]), 100.0 * flips[r] / NoiseReplays))), presidencyFlips, noMajority));
                sb.Append(F("  (c) the noise: {0} jurisdictions ever change hands; the presidency in {1} of {2}, no majority {3}\n", flips.Count(f => f > 0), presidencyFlips, NoiseReplays, noMajority));
            }

            card.Add("");
            card.Add("Each jurisdiction - its electors, its margin of record, the idle map's (R − D, points of the jurisdiction's vote), and the ceiling's move for each party:");
            card.Add("");
            card.Add(F("| jurisdiction | electors | {0} of record | the idle map | the ceiling, {1} | the ceiling, {2} | the ceiling flips the map's call |", PredictedYear, dKey, rKey));
            card.Add("|---|---:|---:|---:|---:|---:|---|");
            card.AddRange(ceilingRows);

            // ---- R-US16 (c): THE SAME COUNT OVER EVERY STAGED COUNTRY'S CAMPAIGN
            card.Add("");
            card.Add("### R-US16 (c): every staged country");
            card.Add("");
            card.Add(F("Every country `LiveCampaignSetup.TryFor` stages, on the vote model at its own start, its campaign run whole (every party an AI at its cast, seed {0}); its regions derived from the run's national shares by `NationalElection.SharesFromCampaign` (the runtime's own derivation, which the mirror equals to the last bit) and counted by W-D1 as above - one base turnout, the staging's votes over its electorate. The run's ground game's effect is the count with the run's mobilisation against the count with none; the count with none against the run's national shares is W-D1's frame alone (one base turnout weighs each region by its electorate, not its votes). Seats by `NationalElection.Run` on each; no tactical layer in either.", Seed));
            card.Add("");
            card.Add("| country | regions | the ground game: largest party move (pp) | seats changing hands | W-D1's frame alone: largest party move (pp) | seats changing hands | parties past the compatibility ceiling at the close |");
            card.Add("|---|---:|---:|---:|---:|---:|---|");
            var stamp = new StringBuilder();
            foreach (CountryId country in (CountryId[])Enum.GetValues(typeof(CountryId)))
            {
                if (country == CountryId.Poland) { continue; }   // PL-C: Poland's okregi are counted by `ParliamentaryCount`'s own branch on the swing from 2023, never derived by W-D1's mirror - `PolishCampaignDiagnostic` holds its count
                if (country == CountryId.USA) { continue; }
                using (SimulationManager.EpochScope())
                {
                    WorldClock.ApplyStart(country);
                    CampaignCalendar cal = CampaignCalendar.FromWorldStart(WorldClock.LatestElectionDay(country), WorldClock.StartDate(country));
                    if (!LiveCampaignSetup.TryFor(country, new (int, int, Scandal)[0], cal, out CampaignRun.Setup setup, out _, onVoteModelCompatibility: true)) { continue; }
                    string[] cast = LiveCampaignSetup.Keys(country);
                    (CampaignRun.Result run, _) = RunUs(setup);
                    Dictionary<string, double> finalShares = NationalElection.SharesFromCampaign(country, cast, run.FinalShares, null, out TacticalResult _, cal.ElectionDate);
                    double[][] derived = NationalElection.LastRegionalShares;
                    string[] regionNames = NationalElection.LastRegionalNames;
                    if (!MirrorProof(country, cast, run.FinalShares, cal.ElectionDate, derived, regionNames, out string proof)) { Check(false, proof); continue; }
                    Check(true, proof);
                    int[] at = regionNames.Select(name => Array.FindIndex(setup.Regions, rg => rg.Name == name)).ToArray();
                    Check(at.All(i => i >= 0) && at.Distinct().Count() == at.Length && at.Length == setup.Regions.Length, F("R-US16 (c) {0}: the derivation's {1} regions are the campaign's, matched by name", country, regionNames.Length));
                    double[] regionEligible = at.Select(i => setup.Regions[i].Eligible).ToArray();
                    double baseTurnout = setup.Regions.Sum(rg => rg.Audience) / setup.Regions.Sum(rg => rg.Eligible);
                    double most = MaxMobilisationMultiplier(run.Gotv);
                    Check(baseTurnout * most < 1.0, F("R-US16 (c) {0}: no rate clamps - base turnout {1:0.0000} (the votes cast over the electorate, as staged) at the run's largest mobilisation multiplier {2:0.0000}", country, baseTurnout, most));
                    var withGame = new RegionalMobilization((double[])regionEligible.Clone(), cast.Length);
                    for (int r = 0; r < at.Length; r++) { for (int p = 0; p < cast.Length; p++) { CopyContacts(run.Gotv, at[r], p, withGame, r, p, Check); } }
                    var none = new RegionalMobilization((double[])regionEligible.Clone(), cast.Length);
                    double[] a = ElectionDay.Count(regionNames, derived, withGame, baseTurnout, 50.0, 50.0, 50.0, new System.Random(Seed), 0.0).NationalShares;
                    double[] b = ElectionDay.Count(regionNames, derived, none, baseTurnout, 50.0, 50.0, 50.0, new System.Random(Seed), 0.0).NationalShares;
                    Dictionary<string, int> seatsA = NationalElection.Run(country, 0, Dict(cast, a)).Seats, seatsB = NationalElection.Run(country, 0, Dict(cast, b)).Seats;
                    Dictionary<string, int> seatsFinal = NationalElection.Run(country, 0, finalShares).Seats;
                    double gameMove = Enumerable.Range(0, cast.Length).Max(p => Math.Abs(100.0 * (a[p] - b[p])));
                    double frameMove = Enumerable.Range(0, cast.Length).Max(p => Math.Abs(100.0 * (b[p] - finalShares[cast[p]])));
                    double[] closeCompat = CompatibilityAtClose(setup, run);
                    card.Add(F("| {0} | {1} | {2:0.000} | {3} | {4:0.000} | {5} | {6} of {7} |", country, regionNames.Length, gameMove, SeatsMoved(seatsA, seatsB), frameMove, SeatsMoved(seatsB, seatsFinal),
                        closeCompat.Count(v => v >= ElectionScales.Max), closeCompat.Length));
                    sb.Append(F("  R-US16 (c) {0}: the ground game moves a party {1:0.000} pp and {2} seats; W-D1's frame alone {3:0.000} pp and {4} seats\n", country, gameMove, SeatsMoved(seatsA, seatsB), frameMove, SeatsMoved(seatsB, seatsFinal)));
                    stamp.Append(country).Append(string.Join(",", setup.Regions.Select(rg => rg.Name)));
                }
            }

            // the card's head: the staging, then (a), (b), (c) and R-US16 (c) as added above
            string staging = F("A US campaign in the Editor only: {0} on the {1} jurisdictions - audience each one's {2} votes cast ({3:N0} together), electorate the Census Bureau's CVAP 2020-2024 ({5:N0}), the game's own staging's - staged on US-6's vote model at the USA's start ({6:d MMM yyyy}), the campaign {7:d MMM} to {8:d MMM yyyy}; casts by `CampaignCasts.Of` ({9}); the war chest the game's staging's (K19: the 2024 nominees' FEC disbursements over the campaign window, their mean, the purse re-denominated by K110 so money binds as before), the model's act prices times the chest's ratio and its [AUTHORED-DRAFT] staging (`LiveCampaignSetup`'s volunteers, offices and staff); salience the game's own staging's, Gallup's August 2024 wave (immigration {10:0.00}, the economy {11:0.00} - the newest fielded before the campaign; K18 re-cut the card from July 2026's column and UF's VEP); both parties on every ballot.",
                string.Join(" and ", keys), n, PriorYear, nationalAudience, PredictedYear, eligible.Sum(), WorldClock.StartDate(CountryId.USA), calendar.CampaignStart, calendar.ElectionDate.AddDays(-1),
                castLine, salienceImmigration, salienceEconomy);
            string idleLine = F("The idle run - no act, no debate, no story - is the prediction ({0}); by E1's construction its national shares are {1}'s exactly (D {2}, R {3}, others {4}), and derived to the jurisdictions by R-US14 (a) they are US-4's row: electors R {5} – D {6}, {7} states called wrong.",
                Pct(keys, idleFinal), PredictedYear, Pct(natIdle[0]), Pct(natIdle[1]), Pct(natIdle[2]), evIdle[ElectoralCollege.Republican], evIdle[ElectoralCollege.Democrat], wrongIdle);
            card.InsertRange(0, new[] { "### The staging", "", staging + " " + idleLine, "" });

            // the stamp: the catalog's digests, the electorate's, the roster's positions, the declared parameters and the staged countries' regions
            stamp.Append(UsPresidentialReturns.StateSourceDigest).Append(UsPresidentialReturns.YearSourceDigest).Append(UsPresidentialReturns.DistrictSourceDigest).Append(UsPresidentialReturns.CandidateSourceDigest)
                .Append(UsPresidentialReturns.HouseSourceDigest).Append(turnoutDigest);
            foreach (PoliticalParty party in PartySystems.RealRoster(CountryId.USA)) { stamp.Append(F("{0}:{1:R}:{2:R}:{3:R}", party.Abbrev, party.LrEcon, party.Galtan, party.PeopleVsElite)); }
            stamp.Append(F("{0}|{1}|{2}|{3:R}|{4}|{5:R}|{6:R}|{7}|{8}", Seed, PriorYear, PredictedYear, CeilingEffort, NoiseReplays, salienceImmigration, salienceEconomy, ProgramAct, k)).Append(string.Join(",", UsRegions.CitizenVotingAgePopulation.Select(c => c.State + ":" + c.Cvap))).Append(UsCampaignSpending.SourceDigest);
            return Sha256(Encoding.UTF8.GetBytes(stamp.ToString()));
        }

        // ---- the mirror and its proof

        /// <summary>The mirror proved on a staged country's campaign: <see cref="RegionalVoteModel.RegionalSharesByUniformSwing"/> on the regions and prior
        /// `DeriveRegional` reads for the country, equal to what the runtime derived from the same national shares - every share, every name, every weight.</summary>
        private static bool MirrorProof(CountryId country, string[] keys, double[] national, DateTime on, double[][] derived, string[] derivedNames, out string line)
        {
            RegionalVoteModel.RegionInput[] regions;
            double[][] prior;
            if (country == CountryId.Sweden) { regions = SwedishRegions.Regions(keys); prior = SwedishRegions.PriorShares(keys); }
            else if (country == CountryId.Germany) { regions = GermanRegions.Regions(keys, on); prior = GermanRegions.PriorShares(keys, on); }
            else { line = F("R-US16 (c) {0}: a staged country this check holds no mirror for - add its regions to MirrorProof, then re-read", country); return false; }
            if (derived == null || derivedNames == null) { line = F("R-US16 (c) {0}: the runtime derived no regions from the campaign's shares", country); return false; }
            double[][] mirror = RegionalVoteModel.RegionalSharesByUniformSwing(national, regions, prior, out double _);
            bool same = mirror.Length == derived.Length && regions.Select(r => r.Name).SequenceEqual(derivedNames)
                && regions.Select(r => r.ElectorateWeight).SequenceEqual(NationalElection.LastRegionalWeights)
                && Enumerable.Range(0, mirror.Length).All(r => mirror[r].SequenceEqual(derived[r]));
            line = F("the mirror is NationalElection.DeriveRegional on {0}'s campaign: {1} regions, every share equal to the last bit{2}", country, derived.Length, same ? string.Empty : " - NOT EQUAL");
            return same;
        }

        /// <summary>PS-6 US-31: the one runtime file R-US16 (b) licenses to call W-D1's per-region count - the US count's ground game.</summary>
        private const string UsCountFile = "Assets/Scripts/Elections/UsGroundGame.cs";

        /// <summary>GATE-18 q2 with A-LOOP q24 5b: the other - the parliamentary count in play (Sweden, Germany, Poland).</summary>
        private const string ParliamentaryCountFile = "Assets/Scripts/Elections/ParliamentaryCount.cs";

        /// <summary>Every runtime file naming <paramref name="pattern"/> (a call or a method group) but <paramref name="own"/> - comments stripped, the capture
        /// driver's folder aside as `UnwiredSubsystemCheck` sets it; <paramref name="hits"/> the matches in all of them.</summary>
        private static List<string> CountCallers(string pattern, string own, out int hits)
        {
            var callers = new List<string>();
            hits = 0;
            string scripts = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "Assets", "Scripts");
            var calls = new System.Text.RegularExpressions.Regex(pattern);
            foreach (string file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
            {
                string relative = file.Substring(scripts.Length + 1).Replace('\\', '/');
                if (relative.StartsWith("Testing/", StringComparison.Ordinal) || relative == own) { continue; }
                int n = calls.Matches(SourceText.ReadWithoutComments(file)).Count;
                if (n > 0) { callers.Add("Assets/Scripts/" + relative); hits += n; }
            }
            return callers;
        }

        // ---- the runs

        /// <summary>A whole campaign on the seed's streams (`CampaignAiHarness`'s), stepped as `CampaignRun.Simulate` steps it, with the last published poll kept.</summary>
        private static (CampaignRun.Result Result, Poll? Tracker) RunUs(CampaignRun.Setup setup)
        {
            SimulationRandom.Seed(Seed);
            CampaignRun.State s = CampaignRun.Begin(setup, SimulationRandom.For(SimulationRandom.Stream.CampaignAi), SimulationRandom.For(SimulationRandom.Stream.Debate), SimulationRandom.For(SimulationRandom.Stream.Scandal));
            while (!s.Finished) { CampaignRun.StepDay(s); }
            Poll? tracker = s.PublicPoll;
            return (CampaignRun.Finish(s), tracker);
        }

        /// <summary>A party's local acts (§12's three) added to <paramref name="acts"/> by jurisdiction; returns how many named no staged one.</summary>
        private static int CountLocal(CampaignRun.PartyLedger ledger, string[] names, int[] acts)
        {
            int stray = 0;
            foreach (CampaignRun.DecisionRecord d in ledger.Log)
            {
                if (Array.IndexOf(CampaignActions.TheEight, d.Kind) < 0 || !CampaignActions.Spec(d.Kind).IsLocal) { continue; }   // a commissioned poll is logged too, and is not one of the eight
                int r = Array.IndexOf(names, d.Target.Split(new[] { " / " }, StringSplitOptions.None)[0]);
                if (r < 0) { stray++; } else { acts[r]++; }
            }
            return stray;
        }

        /// <summary>R-US16 (b)'s DECLARED rule: each jurisdiction's electors × §25's swing index of its margin derived from the poll's national shares (E1's
        /// construction, R-US14 (a)); null where no poll was published.</summary>
        private static double[] RuleWeights(Poll? tracker, string[] keys, Func<double[], double[]> nationalOf, Func<double[], double[][]> mirror,
            (int Year, string State, int Electors, long VotesD, long VotesR, long VotesOther, long VotesTotal, int CastD, int CastR, int CastOther, string CastOtherTo)[] next)
        {
            if (!tracker.HasValue || tracker.Value.PartyCount != keys.Length) { return null; }
            double[][] map = mirror(nationalOf(keys.Select((key, i) => tracker.Value.Share(i)).ToArray()));
            return Enumerable.Range(0, next.Length).Select(r => next[r].Electors * SwingRegions.Index(Margin(map[r]))).ToArray();
        }

        /// <summary>The <paramref name="count"/> heaviest jurisdictions with any weight, heaviest first, ties in catalog order.</summary>
        private static int[] Top(double[] weights, int count) =>
            weights == null ? new int[0] : Enumerable.Range(0, weights.Length).Where(r => weights[r] > 0.0).OrderByDescending(r => weights[r]).ThenBy(r => r).Take(count).ToArray();

        /// <summary>Each party's compatibility at a campaign's close, as it enters `PreferenceModel`: the staging's own - since A-LOOP q24 5a the campaign moves the
        /// shares directly and writes no compatibility, so nothing a campaign does can carry a party past the clamp (the plan's (4)(c)).</summary>
        private static double[] CompatibilityAtClose(CampaignRun.Setup setup, CampaignRun.Result run) => (double[])setup.Compatibility.Clone();

        private static string Compat(string[] keys, double[] compatibility) => string.Join(", ", keys.Select((key, i) =>
            compatibility[i] >= ElectionScales.Max ? F("{0} past the ceiling ({1:0})", key, compatibility[i]) : F("{0} {1:0.00}", key, compatibility[i])));

        // ---- the count's frame

        /// <summary>The campaign's mobilisation in the count's columns (D, R, others): each party's weighted contacts carried to its nominee's column, the
        /// others' column untouched - copied contact for contact, and checked.</summary>
        private static RegionalMobilization CopyColumns(RegionalMobilization campaign, double[] eligible, int[] partyOfColumn, Action<bool, string> Check)
        {
            var columns = new RegionalMobilization((double[])eligible.Clone(), 3);
            for (int r = 0; r < campaign.RegionCount; r++)
            {
                for (int c = 0; c < partyOfColumn.Length; c++) { CopyContacts(campaign, r, partyOfColumn[c], columns, r, c, Check); }
            }
            return columns;
        }

        /// <summary>One region's weighted contacts for one party, copied through `RegionalMobilization.Operate` as door-knocking (the campaign's one operation,
        /// a weight of one): exact for the whole-number contacts a campaign books; a copy that does not land exact fails by name.</summary>
        private static void CopyContacts(RegionalMobilization from, int fromRegion, int fromParty, RegionalMobilization to, int toRegion, int toParty, Action<bool, string> Check)
        {
            double w = from.WeightedContacts(fromRegion, fromParty);
            if (w <= 0.0) { return; }
            GotvSpec spec = GotvModel.Spec(GotvOperation.DoorKnocking);
            double contacts = w / spec.Weight;
            to.Operate(toRegion, toParty, GotvOperation.DoorKnocking, contacts * spec.MoneyPerContact, contacts * spec.VolunteerHoursPerContact, out _, out _);
            if (to.WeightedContacts(toRegion, toParty) != w) { Check(false, F("a copied mobilisation is not exact: region {0} party {1} - {2:R} against {3:R}", toRegion, toParty, to.WeightedContacts(toRegion, toParty), w)); }
        }

        /// <summary>The largest turnout multiplier any party's mobilisation reaches in a campaign's ground game.</summary>
        private static double MaxMobilisationMultiplier(RegionalMobilization gotv)
        {
            double most = 1.0;
            for (int r = 0; r < gotv.RegionCount; r++) { for (int p = 0; p < gotv.PartyCount; p++) { most = Math.Max(most, TurnoutModel.Multiplier(gotv.Mobilization(r, p), TurnoutModel.MobilizationSpan)); } }
            return most;
        }

        /// <summary>Each region's shares as the count gave them - its votes over its votes.</summary>
        private static double[][] CountShares(ElectionDay.Result result) => result.Regions.Select(r => { double sum = r.Votes.Sum(); return r.Votes.Select(v => sum > 0.0 ? v / sum : 0.0).ToArray(); }).ToArray();

        // ---- the electors

        /// <summary>
        /// The electors a set of jurisdiction shares (D, R, others) gives, through the statute's allocator - US-4's (a) reading: Maine's and Nebraska's
        /// districts swing with their state in two-party terms, each district's prior-election row standing in for the predicted election's of the same
        /// number; the electors in force the predicted election's. Also the states called wrong against the predicted election's record.
        /// </summary>
        private static int[] Electors(double[][] shares, double[][] prior,
            (int Year, string State, int Electors, long VotesD, long VotesR, long VotesOther, long VotesTotal, int CastD, int CastR, int CastOther, string CastOtherTo)[] next,
            out int statesWrong, out string wrongList)
        {
            var college = new List<ElectoralCollege.Jurisdiction>();
            var wrong = new List<string>();
            for (int r = 0; r < next.Length; r++)
            {
                double[] s = shares[r];
                int win = s[1] > s[0] ? ElectoralCollege.Republican : ElectoralCollege.Democrat;
                int actual = next[r].VotesR > next[r].VotesD ? ElectoralCollege.Republican : ElectoralCollege.Democrat;
                if (win != actual) { wrong.Add(next[r].State); }
                string state = next[r].State;
                var dPrev = UsPresidentialReturns.Districts.Where(d => d.Year == PriorYear && d.State == state).OrderBy(d => d.District).ToArray();
                var dNext = UsPresidentialReturns.Districts.Where(d => d.Year == PredictedYear && d.State == state).OrderBy(d => d.District).ToArray();
                if (dNext.Length == 0 || dPrev.Length != dNext.Length) { college.Add(new ElectoralCollege.Jurisdiction(state, next[r].Electors, win)); continue; }
                double statePrior = prior[r][0] / (prior[r][0] + prior[r][1]), stateNow = s[0] / (s[0] + s[1]);
                var winners = new int[dNext.Length];
                for (int i = 0; i < dNext.Length; i++)
                {
                    double d2 = (double)dPrev[i].VotesD / (dPrev[i].VotesD + dPrev[i].VotesR) + (stateNow - statePrior);
                    winners[i] = d2 > 0.5 ? ElectoralCollege.Democrat : ElectoralCollege.Republican;
                }
                college.Add(new ElectoralCollege.Jurisdiction(state, next[r].Electors, win, next[r].Electors - dNext.Length, winners));
            }
            statesWrong = wrong.Count;
            wrongList = wrong.Count == 0 ? "none" : string.Join(", ", wrong);
            return ElectoralCollege.Allocate(college.ToArray(), 2);
        }

        // ---- small readings

        /// <summary>A jurisdiction's shares: the Democratic nominee, the Republican, all others together.</summary>
        private static double[] Shares(long d, long r, long o, long t) => new[] { (double)d / t, (double)r / t, (double)o / t };

        /// <summary>The national shares: the jurisdictions' votes summed.</summary>
        private static double[] National(IEnumerable<(long D, long R, long O, long T)> rows)
        {
            double d = 0, r = 0, o = 0, t = 0;
            foreach (var s in rows) { d += s.D; r += s.R; o += s.O; t += s.T; }
            return new[] { d / t, r / t, o / t };
        }

        /// <summary>The margin, Republican less Democratic, in points of the jurisdiction's vote (columns D, R, …).</summary>
        private static double Margin(double[] shares) => 100.0 * (shares[1] - shares[0]);

        private static Dictionary<string, double> Dict(string[] keys, double[] shares)
        {
            var d = new Dictionary<string, double>();
            for (int i = 0; i < keys.Length; i++) { d[keys[i]] = shares[i]; }
            return d;
        }

        /// <summary>Seats changing hands between two chambers - the sum of every party's gain.</summary>
        private static int SeatsMoved(Dictionary<string, int> a, Dictionary<string, int> b) =>
            a.Keys.Union(b.Keys).Sum(key => Math.Max(0, (a.TryGetValue(key, out int x) ? x : 0) - (b.TryGetValue(key, out int y) ? y : 0)));

        /// <summary>UF's VEP by state abbreviation - the file's own header names the columns; quoted figures carry thousands separators.</summary>
        private static Dictionary<string, double> ReadVep(string csv)
        {
            var vep = new Dictionary<string, double>();
            string[] lines = csv.Replace("\r\n", "\n").Split('\n');
            List<string> header = SplitCsv(lines[0]);
            int abv = header.IndexOf("STATE_ABV"), at = header.IndexOf("VEP");
            if (abv < 0 || at < 0) { return vep; }
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) { continue; }
                List<string> cells = SplitCsv(lines[i]);
                if (cells.Count <= Math.Max(abv, at)) { continue; }
                if (double.TryParse(cells[at].Replace(",", string.Empty), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) { vep[cells[abv]] = v; }
            }
            return vep;
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

        private static string Pct(double share) => F("{0:0.00} %", 100.0 * share);
        private static string Pct(string[] keys, double[] shares) => string.Join(", ", keys.Select((key, i) => key + " " + Pct(shares[i])));

        // ---- the US model card's US-9 block (docs/reference/US_ELECTIONS.md) - written by WriteReadings

        private const string CardRelative = "docs/reference/US_ELECTIONS.md";
        private const string CardStamp = "<!-- GENERATED by PoliSim.EditorTools.UsCampaignGeographyCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: ";
        private const string CardEnd = "<!-- END GENERATED -->";

        private static string PathOf(string relative) => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), relative);

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant(); }
        }

        /// <summary>Whether a saved file's digest is the line its folder's SHA256SUMS.txt lists for it.</summary>
        private static bool DigestListed(string relative, string digest)
        {
            string sums = File.ReadAllText(PathOf(Path.GetDirectoryName(relative).Replace('\\', '/') + "/SHA256SUMS.txt"), Encoding.ASCII);
            return sums.Replace("\r\n", "\n").Split('\n').Contains(digest + " *" + Path.GetFileName(relative));
        }

        private static string CardBlock(List<string> lines, string digest)
        {
            var block = new StringBuilder(CardStamp).Append(digest).Append(" -->\n");
            foreach (string line in lines) { block.Append(line).Append('\n'); }
            return block.Append(CardEnd).ToString();
        }

        private static string CardBlockOf(string card)
        {
            string text = card.Replace("\r\n", "\n");
            int start = text.IndexOf(CardStamp, StringComparison.Ordinal);
            int end = start < 0 ? -1 : text.IndexOf(CardEnd, start, StringComparison.Ordinal);
            return end < 0 ? null : text.Substring(start, end + CardEnd.Length - start);
        }
    }
}
