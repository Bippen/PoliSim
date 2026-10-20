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
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-6 (R-US2 (a) and R-US15 (a), RULED - F8 §781, G2 §797; E1 §772, F4 §779): THE US PREDICTION, AND E1'S FACTORS FOR THE USA - checked on
    /// every bar, on the pattern of `PresidentialElectionLiveDiagnostic`.
    /// <list type="bullet">
    /// <item><description><b>The poll is R-US15's.</b> For every US chamber of record the history the runtime reads (`PartySystems.TryHistory`) is the
    /// House pair US-5's instrument sums from the catalog, and the electorate it holds (`PartySystems.UsElectorate`) is that instrument's own fit at the
    /// four's median (`UsNationalVoteCheck.HeldAtMedian`), equal to the last bit allowed; the start's epoch resolves the vintage both read, and the
    /// reference world stepped on to the record's next chamber seats it and still reads the epoch's history (the reading DECLARED with US-6).</description></item>
    /// <item><description><b>The reference world</b> (`PresidentialReferenceWorld.Hold` for the USA: the epoch set to the eve of the general election of
    /// record before the world is created, one day stepped, the random streams and the energy state put back after, as US-5's eve worlds are): it seats
    /// the government of record (a presidency of DEM) and the chamber of record, its poll carries the record over the term, and that poll is the ruled
    /// chain composed by hand - so the live wiring is the ruling's, not merely a poll.</description></item>
    /// <item><description><b>The factors are refit each run</b> - each nominee's share of the FEC's national total ÷ the party's poll on that world - and the
    /// check fails where a stored factor (`PresidencyOfRecord.CandidatesOf`) is no longer the fit, printing the refit as FIT lines for
    /// `Tools/us_factor_patch.pl` to patch in from the log. The field, at the stored factors, reproduces the FEC's national shares on that world - BY
    /// CONSTRUCTION, which the card says in words: the acceptance rests on the states (US-4) and on the out-of-sample case (US-5's 2020 from 2016).
    /// A later field holds no candidate of record (E1's factor 1.0).</description></item>
    /// <item><description><b>The standing</b> (US-6c, Elias's ruling J3): every other column stands only where on the ballot of record - the jurisdictions
    /// `PresidencyOfRecord.StandingOfRecord` gives are the catalog's listed cells (the FEC's roman cells), the scattered write-ins, on no ballot, where they
    /// are reported; a nominee holds no ballot row. The national field reads each column's share whole, so the standing moves no factor.</description></item>
    /// <item><description><b>The naming</b> (R-US2 (a)): each convention's date of record held to its saved page's bytes (SHA-256)
    /// and date words - the DNC's; the RNC's BILLED - and, R-US23 as ruled (even-handed), neither nominee named before both are dated, then both by NARA's names.</description></item>
    /// </list>
    /// Pinned in the cheap bar and the documents bar through the US model card's US-6 block (`docs/reference/US_ELECTIONS.md`, *The readings
    /// (US-6)*), written by <see cref="WriteReadings"/>; a block that no longer says what this measures fails here. Nothing here counts a vote: the count is
    /// US-8's (the reference world's step holds it on the same poll, and `UsPresidentialElectionDiagnostic` reads it there).
    /// </summary>
    public static class UsPresidentialFactorCheck
    {
        /// <summary>E1's refit tolerance, the Polish check's: a stored factor within one part in a million of the fit.</summary>
        private const double FitTolerance = 1e-6;

        /// <summary>E1: how close the field at the stored factors must come to the record's national shares on the reference world, in points (the Polish check's).</summary>
        private const double ReproductionTolerance = 0.005;

        /// <summary>The DNC's dated page the Democratic naming is held to, and the words that date it (records_by_date.md §2a, [DNC-NOMINEE]).</summary>
        private const string NomineePage = "ElectionsData/usa/raw/records/democrats_20240806_harris_walz_certified_nominees.html";
        private const string NomineeWords = "officially accepted the nominations today";

        /// <summary>US-8b (R-US23 as ruled): the RNC's dated page the Republican convention's date is held to, and its words - null while the date is BILLED
        /// (`PresidencyOfRecord`'s table, the one place the date goes). ⚠ For the integrator: once Elias's saved page is in `ElectionsData/usa/raw/records/` with
        /// its SHA256SUMS line, name it here with the words that record the nomination, beside the date in the table.</summary>
        private const string RncPage = "ElectionsData/usa/raw/records/wayback_gop_rnc_nomination_20240716113343.html";
        private const string RncWords = "nominated Donald J. Trump for President";

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsPresidentialFactorCheck (PS-6 US-6): the US poll R-US15 ruled, E1's factors refit on the reference world, the field of record and its naming ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string expected = CardBlock(card, digest);
                string onDisk = CardBlockOf(File.ReadAllText(PathOf(CardRelative), Encoding.UTF8));
                Check(onDisk == expected, onDisk == null ? F("the model card ({0}) carries no US-6 block", CardRelative)
                    : onDisk == expected ? F("the model card's US-6 block ({0}) says what this measures - {1} lines", CardRelative, card.Count)
                    : F("the model card's US-6 block ({0}) is STALE - regenerate: -executeMethod PoliSim.EditorTools.UsPresidentialFactorCheck.WriteReadings", CardRelative));
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }

            sb.Append(failures == 0 ? "=== UsPresidentialFactorCheck: the prediction holds ===" : F("=== UsPresidentialFactorCheck: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Writes the card's US-6 block from this reading - the stamp and its END marker placed once by hand; refuses while any check fails
        /// (a stale stored factor among them: patch the FIT lines in first).</summary>
        public static void WriteReadings()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsPresidentialFactorCheck.WriteReadings (PS-6 US-6): the US model card's US-6 block ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string path = PathOf(CardRelative);
                string text = File.ReadAllText(path, Encoding.UTF8);
                string old = CardBlockOf(text);
                if (failures > 0) { Check(false, "a check above failed - the card is not written; fix what failed (a stale factor: perl Tools/us_factor_patch.pl <this log>), then write"); }
                else if (old == null) { Check(false, F("the model card ({0}) carries no US-6 block to write into", CardRelative)); }
                else
                {
                    string written = text.Replace("\r\n", "\n").Replace(old, CardBlock(card, digest));
                    if (text.Contains("\r\n")) { written = written.Replace("\n", "\r\n"); }
                    File.WriteAllText(path, written, new UTF8Encoding(false));
                    Check(true, F("the model card's US-6 block written ({0}) - {1} lines", CardRelative, card.Count));
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }

            sb.Append(failures == 0 ? "=== WriteReadings: written ===" : F("=== WriteReadings: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>The reading <see cref="Run"/> checks and <see cref="WriteReadings"/> writes. Returns the digest the block is stamped with. The
        /// game's predictions this check makes for the USA (on the reference world and beside it) rewrite the regional derivation the last prediction
        /// left (`NationalElection.LastRegional*`, a static the next check may read); it is captured first and put back after, and the put-back held.</summary>
        private static string Measure(StringBuilder sb, Action<bool, string> Check, List<string> card)
        {
            RegionalState found = RegionalState.Capture();
            string digest;
            try { digest = MeasureAll(sb, Check, card); }
            finally { found.Restore(); }
            Check(found.IsCurrent(), "the regional derivation the last prediction left (NationalElection.LastRegional*) is put back as the check found it");
            return digest;
        }

        /// <summary>The five statics a prediction's regional derivation writes - captured whole, and written back through their own setters.</summary>
        private sealed class RegionalState
        {
            private double[][] _shares; private string[] _names; private double[] _weights; private double _worst; private IReadOnlyList<string> _keys;

            public static RegionalState Capture() => new RegionalState
            {
                _shares = NationalElection.LastRegionalShares, _names = NationalElection.LastRegionalNames, _weights = NationalElection.LastRegionalWeights,
                _worst = NationalElection.LastRegionalWorstAbsError, _keys = NationalElection.LastRegionalKeys,
            };

            public void Restore()
            {
                Set(nameof(NationalElection.LastRegionalShares), _shares);
                Set(nameof(NationalElection.LastRegionalNames), _names);
                Set(nameof(NationalElection.LastRegionalWeights), _weights);
                Set(nameof(NationalElection.LastRegionalWorstAbsError), _worst);
                Set(nameof(NationalElection.LastRegionalKeys), _keys);
            }

            public bool IsCurrent() => ReferenceEquals(NationalElection.LastRegionalShares, _shares) && ReferenceEquals(NationalElection.LastRegionalNames, _names)
                && ReferenceEquals(NationalElection.LastRegionalWeights, _weights) && NationalElection.LastRegionalWorstAbsError.Equals(_worst) && ReferenceEquals(NationalElection.LastRegionalKeys, _keys);

            private static void Set(string property, object value) =>
                typeof(NationalElection).GetProperty(property, BindingFlags.Public | BindingFlags.Static).GetSetMethod(true).Invoke(null, new[] { value });
        }

        private static string MeasureAll(StringBuilder sb, Action<bool, string> Check, List<string> card)
        {
            PoliticalParty[] roster = PartySystems.RealRoster(CountryId.USA);
            string[] keys = roster.Select(p => p.Abbrev).ToArray();
            Check(keys.SequenceEqual(new[] { "REP", "DEM" }), F("the US roster is REP and DEM, in that order: {0}", string.Join(", ", keys)));
            VoteModel.PartyPoint[] points = roster.Select(p => new VoteModel.PartyPoint(p.Abbrev, p.LrEcon, p.Galtan)).ToArray();
            double w = PartySystems.UsEconomicWeight;
            Check(w == VoteShareBacktest.UsEconomicWeight, F("one economic weight: the runtime's {0:R}, the Editor's name for it the same", w));
            Check(PartySystems.HistoryNote(CountryId.USA) == null && !string.IsNullOrEmpty(PartySystems.HistoryBasis(CountryId.USA)),
                "the US history maps clean (no LOW CONFIDENCE note) and its basis is stated (PartySystems.HistoryBasis)");

            // ---- (1) the history and the electorate, per US chamber of record
            var rows = new List<string>();
            var electorates = new Dictionary<ElectionVintage, (VoteModel.Electorate Electorate, double Place, int T1, int T2)>();
            foreach (WorldClock.ChamberOfRecord chamber in WorldClock.Chambers(CountryId.USA))
            {
                ElectionVintage v = chamber.Vintage;
                if (!PartySystems.TryUsHistoryYears(v, out int t1, out int t2)) { Check(false, F("{0}: no House pair in the catalog", v)); continue; }
                bool history = PartySystems.TryHistory(CountryId.USA, out double[] latest, out double[] previous, v);
                double[] catT1 = UsNationalVoteCheck.HouseShares(t1), catT2 = UsNationalVoteCheck.HouseShares(t2);
                Check(history && latest.SequenceEqual(catT1) && previous.SequenceEqual(catT2),
                    F("{0}: the history the poll reads is House {1} against {2}, the catalog's sums as US-5's instrument makes them - R {3:0.000000} / D {4:0.000000}, then R {5:0.000000} / D {6:0.000000}", v, t1, t2,
                        history ? latest[0] : double.NaN, history ? latest[1] : double.NaN, history ? previous[0] : double.NaN, history ? previous[1] : double.NaN));
                bool solved = PartySystems.UsElectorate(v, out VoteModel.Electorate e, out double place);
                var failed = new List<string>();
                VoteModel.Electorate instrument = UsNationalVoteCheck.HeldAtMedian(t1, out double instrumentPlace, failed);
                double off = Math.Max(Math.Max(Math.Abs(e.MuEcon - instrument.MuEcon), Math.Abs(e.MuSoc - instrument.MuSoc)), Math.Max(Math.Abs(e.Sigma - instrument.Sigma), Math.Abs(e.Tau - instrument.Tau)));
                Check(solved && failed.Count == 0 && off <= 1e-12 && Math.Abs(place - instrumentPlace) <= 1e-12,
                    F("{0}: the electorate is US-5's fit held at the four's median on House {1} - μ ({2:0.000000}, {3:0.000000}), σ {4:0.00}, τ {5:0.00}, place {6:0.000000} (the instrument's within {7:0.0e+0}){8}",
                        v, t1, e.MuEcon, e.MuSoc, e.Sigma, e.Tau, place, off, failed.Count == 0 ? string.Empty : " - " + string.Join("; ", failed)));
                double split = TwoParty(catT1), given = VoteModel.PredictShares(points, e, w)[0];
                Check(Math.Abs(given - split) < 1e-9, F("{0}: it gives T-1's split, REP {1:0.000000} of the two (T-1 {2:0.000000})", v, given, split));
                electorates[v] = (e, place, t1, t2);
                double[] loyalty = history ? LoyaltyModel.PartyLoyalties(latest, previous) : new[] { double.NaN, double.NaN };
                rows.Add(F("| {0} ({1}) | House {2} against {3} | {4} | {5:0.0} / {6:0.0} | {7:0.0000} | {8:0.000}, {9:0.000} |", v, chamber.ElectionDay.Year == t1 ? F("elected {0}", t1) : F("elected {0} - read as the latest sourced", chamber.ElectionDay.Year),
                    t1, t2, Pct(split), loyalty[0], loyalty[1], place, e.MuEcon, e.MuSoc));
            }

            // ---- (2) the start: the vintage the poll reads at a US game's epoch, and the electorate TryElectorate answers with there
            DateTime start = WorldClock.StartDate(CountryId.USA);
            ElectionVintage atStart, onDayOfChange;
            using (SimulationManager.EpochScope())
            {
                WorldClock.ApplyStart(CountryId.USA);
                atStart = WorldClock.Resolve(CountryId.USA, ElectionVintage.Seated);
                // the vintage held against the record: the House of record on the start's day (WorldClock's rows - convened before it, its term not
                // ended, its seat table sourced), and the history it reads is that House's own election
                WorldClock.ChamberOfRecord onStart = WorldClock.ChamberAt(CountryId.USA, start);
                bool historyYears = PartySystems.TryUsHistoryYears(onStart.Vintage, out int startT1, out int _);
                Check(atStart == onStart.Vintage && onStart.Convened <= start && start < onStart.Until && PartySystems.SeatsSourced(onStart.Vintage) && historyYears && startT1 == onStart.ElectionDay.Year,
                    F("the start ({0:yyyy-MM-dd}) seats {1}, the House of record that day - elected {2:yyyy-MM-dd}, convened {3:yyyy-MM-dd}, its table sourced - and the poll reads that House's own election, {4}",
                        start, atStart, onStart.ElectionDay, onStart.Convened, startT1));
                // and the electorate TryElectorate answers with there, held against US-5's instrument directly - not against this check's own reading
                bool has = PartySystems.TryElectorate(CountryId.USA, out VoteModel.Electorate live, out double liveWeight);
                var failedStart = new List<string>();
                VoteModel.Electorate instrumentAtStart = UsNationalVoteCheck.HeldAtMedian(startT1, out double _, failedStart);
                double offStart = Math.Max(Math.Max(Math.Abs(live.MuEcon - instrumentAtStart.MuEcon), Math.Abs(live.MuSoc - instrumentAtStart.MuSoc)), Math.Max(Math.Abs(live.Sigma - instrumentAtStart.Sigma), Math.Abs(live.Tau - instrumentAtStart.Tau)));
                Check(has && failedStart.Count == 0 && offStart <= 1e-12 && liveWeight == w,
                    F("at the start's epoch PartySystems.TryElectorate answers with US-5's fit held at the four's median on House {0} (within {1:0.0e+0}) and the weight {2:0.00}", startT1, offStart, liveWeight));
            }
            DateTime nextSeated = WorldClock.Chambers(CountryId.USA).Select(c => c.Convened).Where(d => d > start).DefaultIfEmpty(DateTime.MaxValue).Min();
            onDayOfChange = nextSeated == DateTime.MaxValue ? atStart : WorldClock.SeatedVintage(CountryId.USA, nextSeated);
            sb.Append(F("  the start's epoch resolves {0}; the record seats {1} on {2:yyyy-MM-dd}, inside a game from the start - which moves the chamber, not the history (Resolve reads the epoch, §767)\n", atStart, onDayOfChange, nextSeated));

            // ---- (3) the reference world: the eve of the general election of record, built as a new game builds one
            DateTime vote = PresidentialReferenceWorld.VoteOfRecord(CountryId.USA), eve = vote.AddDays(-1);
            DateTime laterChamber = WorldClock.Chambers(CountryId.USA).Select(c => c.Convened).Where(d => d > eve).DefaultIfEmpty(DateTime.MaxValue).Min();
            PresidentialReferenceWorld.Result world = HoldWorld(laterChamber == DateTime.MaxValue ? (DateTime?)null : laterChamber);
            ElectionVintage atEve;
            using (SimulationManager.EpochScope()) { SimulationManager.SetEpoch(eve); atEve = WorldClock.Resolve(CountryId.USA, ElectionVintage.Seated); }
            Check(world.Country != null && world.FirstVoteDay == vote && world.Day == vote && world.Contest == null,
                F("the reference world: the epoch {0:yyyy-MM-dd}, the eve of the general election of record, one day stepped to {1:yyyy-MM-dd}; no two-round contest held (the US count, US-8's, is held on that day on the same poll - UsPresidentialElectionDiagnostic's)", eve, world.Day));
            WorldClock.TryGovernmentAt(CountryId.USA, eve, out WorldClock.GovernmentOfRecord ofRecord);
            Check(world.GovernmentKind == WorldClock.ExecutiveKind.Presidency && world.GovernmentPmParty == "DEM" && ofRecord.Head != null,
                F("it seats the government of record - a presidency of {0}: {1}", world.GovernmentPmParty ?? "none", ofRecord.Head ?? "none on record"));
            Dictionary<string, int> chamberOfRecord = PartySystems.InitialSeats(CountryId.USA, atEve);
            Check(world.Seats != null && chamberOfRecord.Count == world.Seats.Count && chamberOfRecord.All(kv => world.Seats.TryGetValue(kv.Key, out int n) && n == kv.Value),
                F("it seats the chamber of record, {0} as elected: {1}", atEve, world.Seats == null ? "none" : string.Join(", ", world.Seats.Select(kv => kv.Key + " " + kv.Value))));
            Check(world.Term.Unemployment.Complete && world.Term.Inflation.Complete && world.Term.TookOffice < eve && world.Term.TookOffice == ofRecord.From,
                "its poll carries the government's record over the term - " + EconomicVote.Describe(world.Term));
            Dictionary<string, double> magnitudes = EconomicVote.Magnitudes(world.Country);
            Check(world.Shift != null && world.Shift.Count == 1 && world.Shift.ContainsKey("DEM") && magnitudes.Count == 1 && magnitudes.ContainsKey("DEM"),
                F("the record shifts DEM alone: {0:+0.00;-0.00} pp at DEM's economic vote {1:0.00} (Table 9.1's presidential row)", 100.0 * (world.Shift?.Values.FirstOrDefault() ?? double.NaN), magnitudes.TryGetValue("DEM", out double mDem) ? mDem : double.NaN));
            Dictionary<string, double> poll = world.PollOnFirstVote;
            Check(poll != null && poll.Count == 2 && poll.ContainsKey("REP") && poll.ContainsKey("DEM") && Math.Abs(poll["REP"] + poll["DEM"] - 1.0) < 1e-12,
                F("the poll on {0:yyyy-MM-dd}: REP {1}, DEM {2}", world.Day, poll != null && poll.ContainsKey("REP") ? Pct(poll["REP"]) : "-", poll != null && poll.ContainsKey("DEM") ? Pct(poll["DEM"]) : "-"));
            Check(atEve == atStart, F("the reference world reads the start's vintage ({0}) - the history a game from the start reads on the day", atEve));

            // ---- (4) the poll is the ruled chain, composed by hand as US-5 composed it: the spatial shares on the held electorate, the compatibility
            // scale, the preference over T-1 with T-1 against T-2's loyalty, the record's shift; and before the shift, T-1's split (the identity)
            double plainSplit = double.NaN, chainOff = double.NaN;
            using (SimulationManager.EpochScope())
            {
                SimulationManager.SetEpoch(eve);
                if (NationalElection.TryPredictShares(CountryId.USA, out Dictionary<string, double> plain)) { plainSplit = plain["REP"] / (plain["REP"] + plain["DEM"]); }
                PartySystems.TryHistory(CountryId.USA, out double[] prior, out double[] before);
                VoteModel.Electorate held = electorates.TryGetValue(atEve, out var ae) ? ae.Electorate : default;
                double[] spatial = VoteModel.PredictShares(points, held, w);
                double[] preference = PreferenceModel.Preference(GateReRun.ToCompatScale(spatial), prior, LoyaltyModel.PartyLoyalties(prior, before));
                double[] chain = EconomicVote.ApplyRecordShift(keys, preference, world.Shift);
                if (poll != null && poll.Count == 2) { chainOff = Math.Max(Math.Abs(chain[0] - poll["REP"]), Math.Abs(chain[1] - poll["DEM"])); }
            }
            double t1Split = electorates.TryGetValue(atEve, out var atEveRow) ? TwoParty(UsNationalVoteCheck.HouseShares(atEveRow.T1)) : double.NaN;
            Check(Math.Abs(plainSplit - t1Split) < 1e-9, F("the identity: before the record the poll gives T-1's split - REP {0:0.000000} of the two, T-1 {1:0.000000}", plainSplit, t1Split));
            Check(chainOff <= 1e-12, F("the poll is R-US15's chain composed by hand - history (a), the four's median, T-1's loyalty, the record - within {0:0.0e+0}", chainOff));

            // ---- the reading DECLARED with US-6: the same world stepped on by the game's loop to the record's next chamber - it seats it (US-2), and the
            // live poll that day still reads the epoch's history: a chamber the record seats later moves the seats, not the history. What is held is the
            // game's own call (NationalElection.TryPredictShares, no record) against BOTH histories - the epoch's House and the House then seated, whose
            // splits differ - so a poll that followed the seated chamber would fail here; the TryHistory/TryElectorate reads beside it are by construction
            ElectionVintage laterVintage = laterChamber == DateTime.MaxValue ? atEve : WorldClock.SeatedVintage(CountryId.USA, laterChamber);
            // PS-6 US-16: the world holds its own Congress count on 5 Nov 2024 and seats that House on the record's next chamber day - what it seats is the
            // count's, the record's where it held none; the history the poll reads is the point here, and stays the epoch's either way
            Dictionary<string, int> laterOfRecord = world.Country != null && world.Country.UsCongressElections.Count == 1
                ? world.Country.UsCongressElections[0].HouseSeats(CountryId.USA) : PartySystems.InitialSeats(CountryId.USA, laterVintage);
            bool seatedLater = world.SeatsThen != null && laterOfRecord.Count == world.SeatsThen.Count && laterOfRecord.All(kv => world.SeatsThen.TryGetValue(kv.Key, out int n) && n == kv.Value);
            double thenSplit = world.PlainPollThen != null && world.PlainPollThen.ContainsKey("REP") && world.PlainPollThen.ContainsKey("DEM")
                ? world.PlainPollThen["REP"] / (world.PlainPollThen["REP"] + world.PlainPollThen["DEM"]) : double.NaN;
            double laterSplit = electorates.TryGetValue(laterVintage, out var laterRow) ? TwoParty(UsNationalVoteCheck.HouseShares(laterRow.T1)) : double.NaN;
            bool readsEpoch = Math.Abs(thenSplit - t1Split) < 1e-9 && Math.Abs(laterSplit - t1Split) > 1e-6;
            bool byConstruction = atEveRow.T1 > 0 && world.HistoryThenLatest != null && world.HistoryThenLatest.SequenceEqual(UsNationalVoteCheck.HouseShares(atEveRow.T1))
                && world.HistoryThenPrevious.SequenceEqual(UsNationalVoteCheck.HouseShares(atEveRow.T2)) && Same(world.ElectorateThen, atEveRow.Electorate);
            Check(laterChamber != DateTime.MaxValue && world.ThenDay == laterChamber && laterVintage != atEve && seatedLater && readsEpoch && byConstruction,
                F("the same world stepped to {0:yyyy-MM-dd} seats {1}'s day's House (the world's own count of 5 Nov 2024 where it held one, US-16; else the record by date, US-2), and its live poll that day (no record) gives REP {2:0.000000} of the two - House {3}'s split, not House {4}'s {5:0.000000}, which a poll following the seated chamber would give: the chamber moves, the history does not (DECLARED, US-6){6}",
                    world.ThenDay, laterVintage, thenSplit, atEveRow.T1, laterRow.T1, laterSplit, world.ThenError == null ? string.Empty : " - the steps threw: " + world.ThenError));

            // ---- (5) E1's factors: refit on the reference world, held against the stored
            IReadOnlyList<PresidencyOfRecord.CandidateOfRecord> field = PresidencyOfRecord.CandidatesOf(CountryId.USA, 2025);
            var catalog = UsPresidentialReturns.Candidates.Where(c => c.Year == vote.Year).ToArray();
            Check(field.Count == catalog.Length && field.Select(c => c.Surname).SequenceEqual(catalog.Select(c => c.Candidate)),
                F("the field of record is the FEC workbook's {0} columns, as the catalog holds them", catalog.Length));
            var backed = field.Where(c => c.BackingParty != null).ToList();
            string nomineeR = catalog.Where(c => c.Ticket == "R").Select(c => c.Candidate).FirstOrDefault(), nomineeD = catalog.Where(c => c.Ticket == "D").Select(c => c.Candidate).FirstOrDefault();
            Check(backed.Count == 2 && backed.Any(c => c.Surname == nomineeR && c.BackingParty == "REP" && c.PositionUnit == "REP") && backed.Any(c => c.Surname == nomineeD && c.BackingParty == "DEM" && c.PositionUnit == "DEM"),
                F("two nominees stand for the roster, each at the party's position: {0} (REP), {1} (DEM) - NARA's tickets", nomineeR, nomineeD));
            var drift = new List<string>();
            var fits = new Dictionary<string, double>();
            foreach (PresidencyOfRecord.CandidateOfRecord c in backed)
            {
                double fit = PresidentialReferenceWorld.FittedFactor(world, c);
                fits[c.Surname] = fit;
                sb.Append(F("      FIT USA {0} {1} {2:R}  (poll {3:0.000000}, record {4:0.000000})\n", c.Surname, c.BackingParty, fit,
                    poll != null && poll.TryGetValue(c.BackingParty, out double p0) ? p0 : double.NaN, PresidencyOfRecord.ShareOfRecord(CountryId.USA, vote.Year, c.Surname)));
                if (double.IsNaN(fit) || double.IsNaN(c.Factor) || Math.Abs(c.Factor - fit) > FitTolerance * Math.Max(1.0, Math.Abs(fit))) { drift.Add(F("{0} stored {1:R} - the fit {2:0.0000000}", c.Surname, c.Factor, fit)); }
            }
            Check(drift.Count == 0, drift.Count == 0 ? "E1 (US): the stored factors are the fit (the FEC's national shares over the game's poll on the reference world)"
                : "E1 (US): the stored factors are NOT the fit - " + string.Join("; ", drift) + " (the FIT lines above are the refit: perl Tools/us_factor_patch.pl <this log>)");

            // ---- (6) by construction: the field at the stored factors, normalised, gives the record's national shares on that world
            var support = new List<(PresidencyOfRecord.CandidateOfRecord Candidate, double Support)>();
            foreach (PresidencyOfRecord.CandidateOfRecord c in field)
            {
                double s = c.BackingParty != null ? (poll != null && poll.TryGetValue(c.BackingParty, out double pp) ? pp * c.Factor : double.NaN) : PresidencyOfRecord.ShareOfRecord(CountryId.USA, vote.Year, c.Surname);
                support.Add((c, s));
            }
            double total = support.Sum(x => x.Support), worst = 0.0;
            string worstName = null;
            var fieldRows = new List<string>();
            foreach ((PresidencyOfRecord.CandidateOfRecord c, double s) in support)
            {
                double share = 100.0 * s / total, record = 100.0 * PresidencyOfRecord.ShareOfRecord(CountryId.USA, vote.Year, c.Surname), miss = share - record;
                if (double.IsNaN(miss) || Math.Abs(miss) > worst) { worst = double.IsNaN(miss) ? double.PositiveInfinity : Math.Abs(miss); worstName = c.Surname; }
                fieldRows.Add(F("| {0} | {1} | {2} | {3:0.0000} % | {4:0.0000} % | {5:+0.0000;-0.0000} |", PresidencyOfRecord.NameOfRecord(CountryId.USA, vote.Year, c.Surname), c.BackingParty ?? "-",
                    c.BackingParty != null ? F("{0:0.000000}", c.Factor) : "base", record, share, miss));
            }
            Check(worst < ReproductionTolerance, F("by construction: the field at the stored factors gives the FEC's national shares on the reference world - the largest miss {0:0.0000} pp ({1})", worst, worstName ?? "none"));

            // ---- (6b) US-6c (Elias's ruling J3): every other column stands only where on the ballot of record - the jurisdictions whose cell of the FEC's
            // workbook is roman (its note: italics indicate write-in votes); the one column on no ballot, the scattered write-ins, where the workbook reports them
            var standingWrong = new List<string>();
            var onNoBallot = new List<string>();
            var listedCount = new Dictionary<string, int>();
            foreach (PresidencyOfRecord.CandidateOfRecord c in field)
            {
                Dictionary<string, double> standing = PresidencyOfRecord.StandingOfRecord(CountryId.USA, vote.Year, c.Surname);
                var cells = UsPresidentialReturns.CandidateStates.Where(b => b.Year == vote.Year && b.Candidate == c.Surname).ToList();
                var listed = cells.Where(b => b.Listed).Select(b => b.State).ToList();
                if (c.BackingParty != null) { if (standing.Count != 0 || cells.Count != 0) { standingWrong.Add(c.Surname + " (a nominee) holds a ballot row"); } continue; }
                if (listed.Count == 0) { onNoBallot.Add(PresidencyOfRecord.NameOfRecord(CountryId.USA, vote.Year, c.Surname)); }
                var expectedWhere = listed.Count > 0 ? listed : cells.Select(b => b.State).ToList();
                if (expectedWhere.Count == 0 || !standing.Keys.OrderBy(s => s, StringComparer.Ordinal).SequenceEqual(expectedWhere.OrderBy(s => s, StringComparer.Ordinal))) { standingWrong.Add(F("{0} stands in {1}, its ballots of record list {2}", c.Surname, standing.Count, expectedWhere.Count)); }
                listedCount[c.Surname] = listed.Count;
            }
            int Listed(string surname) => listedCount.TryGetValue(surname, out int n) ? n : -1;
            Check(standingWrong.Count == 0 && onNoBallot.Count == 1,
                standingWrong.Count == 0 ? F("every other column stands only where on the ballot of record (J3): Stein {0}, Kennedy {1}, Oliver {2} of 51 jurisdictions; on no ballot only {3}, standing where the workbook reports it", Listed("STEIN"), Listed("KENNEDY"), Listed("OLIVER"), string.Join(", ", onNoBallot))
                    : "the standing is NOT the ballots of record - " + string.Join("; ", standingWrong));

            // ---- (7) a later field: no candidate of record, every share at E1's future factor
            Check(PresidencyOfRecord.CandidatesOf(CountryId.USA, 2029).Count == 0 && PresidentialElection.FutureFactor == 1.0,
                F("a later field holds no candidate of record - its candidates start at factor {0:0.0} (E1, PresidentialElection.FutureFactor)", PresidentialElection.FutureFactor));

            // ---- (8) the naming (R-US2 (a); US-8b, R-US23 as ruled - even-handed): each convention's date held to its saved page; neither nominee named before both are dated
            bool dem = PresidencyOfRecord.TryConventionDate(CountryId.USA, "DEM", out DateTime demFrom);
            bool rep = PresidencyOfRecord.TryConventionDate(CountryId.USA, "REP", out DateTime repFrom);
            string page = ReadPage(NomineePage, out string pageDigest, out bool pageHashed);
            string dateWords = dem ? demFrom.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture) : "-";
            Check(dem && pageHashed && page.Contains(dateWords) && page.Contains(NomineeWords),
                F("the Democratic convention's date of record {0:yyyy-MM-dd}: its saved page ({1}) its SHA-256 in SHA256SUMS.txt, holding \"{2}\" and \"{3}\"", demFrom, Path.GetFileName(NomineePage), dateWords, NomineeWords));
            bool repBilled = rep && repFrom == DateTime.MaxValue;
            string rncDigest = string.Empty;
            if (repBilled)
            {
                Check(RncPage == null, "the Republican convention's date BILLED (gop.com refuses this machine, records_by_date.md G13; the page of the roll call of 15 July 2024 is Elias's browser errand) - no page is named for it");
            }
            else
            {
                string rncWords = rep ? repFrom.ToString("MMM d, yyyy", CultureInfo.InvariantCulture) : "-";   // the RNC's page dates itself in the short month ("Jul 15, 2024")
                bool rncHashed = false;
                string rnc = RncPage != null ? ReadPage(RncPage, out rncDigest, out rncHashed) : string.Empty;
                Check(rep && RncPage != null && rncHashed && rnc.Contains(rncWords) && RncWords != null && rnc.Contains(RncWords),
                    F("the Republican convention's date of record {0:yyyy-MM-dd}: its saved page ({1}) its SHA-256 in SHA256SUMS.txt, holding \"{2}\" and \"{3}\"", repFrom, RncPage ?? "NONE NAMED", rncWords, RncWords ?? "-"));
            }
            DateTime later = demFrom > repFrom ? demFrom : repFrom;
            bool namedDem = PresidencyOfRecord.TryNamedFrom(CountryId.USA, "DEM", out DateTime fromDem), namedRep = PresidencyOfRecord.TryNamedFrom(CountryId.USA, "REP", out DateTime fromRep);
            Check(dem && rep && namedDem && namedRep && fromDem == later && fromRep == later,
                F("even-handed: both nominees are named from one day, the later convention's - {0} for each", later == DateTime.MaxValue ? "never, while the Republican date is billed" : later.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            string harris = PresidencyOfRecord.NameOfRecord(CountryId.USA, vote.Year, nomineeD ?? string.Empty);
            string trump = PresidencyOfRecord.NameOfRecord(CountryId.USA, vote.Year, nomineeR ?? string.Empty);
            string demOnConvention = PresidencyOfRecord.NomineeName(CountryId.USA, "DEM", demFrom);
            string demOnVote = PresidencyOfRecord.NomineeName(CountryId.USA, "DEM", vote), repOnVote = PresidencyOfRecord.NomineeName(CountryId.USA, "REP", vote);
            string demAfter = PresidencyOfRecord.NomineeName(CountryId.USA, "DEM", vote.AddYears(1)), repAfter = PresidencyOfRecord.NomineeName(CountryId.USA, "REP", vote.AddYears(1));
            if (later == DateTime.MaxValue)
            {
                Check(demOnConvention == "the Democratic nominee" && demOnVote == demOnConvention && demAfter == demOnConvention && repOnVote == "the Republican nominee" && repAfter == repOnVote,
                    F("neither named before the count: \"{0}\" from the DNC's own date {1:yyyy-MM-dd} and on the day of the vote, \"{2}\" on the day of the vote - and on every day after (from the count on the count names both - US-8)", demOnVote, demFrom, repOnVote));
            }
            else
            {
                DateTime dayBefore = later.AddDays(-1);
                Check(PresidencyOfRecord.NomineeName(CountryId.USA, "DEM", dayBefore) == "the Democratic nominee" && PresidencyOfRecord.NomineeName(CountryId.USA, "REP", dayBefore) == "the Republican nominee"
                      && PresidencyOfRecord.NomineeName(CountryId.USA, "DEM", later) == harris && PresidencyOfRecord.NomineeName(CountryId.USA, "REP", later) == trump
                      && harris == UsPresidentialReturns.Years.Where(y => y.Year == vote.Year).Select(y => y.NomineeD).FirstOrDefault() && trump == UsPresidentialReturns.Years.Where(y => y.Year == vote.Year).Select(y => y.NomineeR).FirstOrDefault(),
                    F("both named from {0:yyyy-MM-dd} by NARA's names (\"{1}\", \"{2}\"), neither on {3:yyyy-MM-dd}", later, harris, trump, dayBefore));
            }

            // ---- the card's block
            double shiftDem = world.Shift != null && world.Shift.TryGetValue("DEM", out double sd) ? sd : double.NaN;
            var minors = field.Where(c => c.BackingParty == null).ToList();
            string Base(string surname) => F("{0} {1}", PresidencyOfRecord.NameOfRecord(CountryId.USA, vote.Year, surname), Pct(PresidencyOfRecord.ShareOfRecord(CountryId.USA, vote.Year, surname)));
            var named = new[] { "STEIN", "KENNEDY", "OLIVER" }.Where(n => minors.Any(c => c.Surname == n)).ToList();
            double restShare = minors.Where(c => !named.Contains(c.Surname)).Sum(c => PresidencyOfRecord.ShareOfRecord(CountryId.USA, vote.Year, c.Surname));
            (VoteModel.Electorate Electorate, double Place, int T1, int T2) startRow = electorates.TryGetValue(atStart, out var sr) ? sr : default;

            card.Add("### The poll (R-US15, ruled)");
            card.Add("");
            card.Add(F("The USA's poll is {0}. The electorate is held, not fitted - σ {1:0.00} and τ {2:0.00}, each the median of {3}'s (`PartySystems.UsSpreadFrom`), wEcon {4:0.00} - its mean on the segment from DEM's point to REP's where the seated history's last House election falls (0 at DEM, 1 at REP), solved for each chamber of record:",
                PartySystems.HistoryBasis(CountryId.USA), startRow.Electorate.Sigma, startRow.Electorate.Tau, string.Join(", ", PartySystems.UsSpreadFrom), w));
            card.Add("");
            card.Add("| chamber of record (vintage) | history read | T-1 split (R) | loyalty R / D | place | μ econ, μ soc |");
            card.Add("|---|---|---:|---:|---:|---|");
            card.AddRange(rows);
            card.Add("");
            card.Add(F("A game from the USA's start ({0:d MMM yyyy}) seats {1} and its poll reads House {2} against {3} for the whole run: the record seats {4} on {5:d MMM yyyy}, which moves the chamber and not the history - the history is the chamber's seated at the world's epoch (the standing history gap, §767; the reading DECLARED with US-6, and held: the reference world below, stepped on by the game's loop to {6:d MMM yyyy}, seats {7} and its poll still reads House {2} against {3}). The other rows say what a world opened on another epoch would read.",
                start, atStart, startRow.T1, startRow.T2, onDayOfChange, nextSeated, world.ThenDay, laterVintage));
            card.Add("");
            card.Add("### E1's factors - by construction");
            card.Add("");
            card.Add(F("**The reference world** (`PresidentialReferenceWorld`, generalised by country): the epoch set to {0:d MMM yyyy}, the eve of the general election of {1:d MMM yyyy}, before the world is created - as a new game builds one - and one day stepped by the game's loop. It seats {2} and the House elected in {3}; its poll is REP {4}, DEM {5}, the record over the term shifting DEM {6:+0.00;-0.00} pp ({7}).",
                eve, vote, ofRecord.Head ?? "-", WorldClock.ElectionDayOf(CountryId.USA, atEve).Year, poll != null ? Pct(poll["REP"]) : "-", poll != null ? Pct(poll["DEM"]) : "-", 100.0 * shiftDem, EconomicVote.Describe(world.Term)));
            card.Add("");
            card.Add(F("**The factors** (R-US2 (a): the November nominees of record carry them from the start): each nominee's share of the FEC's national total ÷ the party's poll on that world - {0} {1:0.000000}, {2} {3:0.000000}. Every other column of the FEC's workbook carries its national share of record as its base, standing only where on the ballot of record (below): {4}, the rest {5} (every other candidate, None of These Candidates and the scattered write-ins). A later field starts at factor {6:0.0} (E1).",
                PresidencyOfRecord.NameOfRecord(CountryId.USA, vote.Year, nomineeR ?? string.Empty), fits.TryGetValue(nomineeR ?? string.Empty, out double fR) ? fR : double.NaN,
                harris, fits.TryGetValue(nomineeD ?? string.Empty, out double fD) ? fD : double.NaN, string.Join(", ", named.Select(Base)), Pct(restShare), PresidentialElection.FutureFactor));
            card.Add("");
            card.Add(F("**The factors reproduce the 2024 national shares on the reference world by construction**: they are fitted there, so the field's largest miss on it ({0:0.0000} pp, the table below) shows only that the stored factors are still the fit. The acceptance of the US count rests elsewhere - on the states, US-4's layer moving 2020's states to 2024's national shares (*The readings*, above), and on the out-of-sample case, US-5's 2020 predicted from 2016 (*The readings (US-5)*, above).",
                worst));
            card.Add("");
            card.Add(F("**E1's premise for the USA** (RULED, F4): a game played from {0:d MMM yyyy} reaches {1:d MMM yyyy} on its own economy and its own seed vintage - {2} - and on its own play, so its poll on the day is its own and its count is not the record's.",
                start, vote, WorldClock.SeedVintageLine(CountryId.USA)));
            card.Add("");
            card.Add("| candidate | party | factor | share of record (FEC) | the field on the reference world | miss (pp) |");
            card.Add("|---|---|---:|---:|---:|---:|");
            card.AddRange(fieldRows);
            card.Add("");
            card.Add("### The field of record and its naming (R-US2, ruled; R-US23, ruled even-handed)");
            card.Add("");
            card.Add(F("The nominees are named from their conventions' dates of record, and neither before both are dated (R-US23, ruled even-handed): \"the Democratic/Republican nominee\" until then; the withdrawal is not replayed. The Democratic convention's date is {0:d MMM yyyy} - the DNC's release certifying the nominees (`ElectionsData/usa/records_by_date.md` §2a, its page held to its SHA-256 and its date). {1} From the count on, the game's own count (US-8) names each nominee by NARA's name for the ticket (the contest, the oath's government, the Parliament page's president row).",
                demFrom, later == DateTime.MaxValue
                    ? F("The Republican convention's date is BILLED - gop.com refuses this machine (G13); Elias saves the RNC's page for the roll call of 15 July 2024 by browser - so the naming table (`PresidencyOfRecord.NomineeName`, which no screen reads yet) names \"{0}\" and \"{1}\" on every day before the count, to a player of either party as to anyone.", demOnVote, repOnVote)
                    : F("The Republican convention's date is {0:d MMM yyyy} (its saved page held to its SHA-256 and its date), so the naming table (`PresidencyOfRecord.NomineeName`, which no screen reads yet) names {1} and {2} from {3:d MMM yyyy}.", repFrom, harris, trump, later)));
            card.Add("");
            card.Add(F("**Every other candidate stands only where on the ballot of record** (Elias's ruling J3, 2026-10-19, reversing US-6's national standing; built as US-6c): a column stands in the jurisdictions whose ballot listed it - the FEC's workbook prints write-in votes in italics, by its own note, so a roman cell is a listing (`PresidencyOfRecord.StandingOfRecord`; `Tools/us_ballot_standing.pl` prints each column's) - Stein in {0}, Kennedy in {1}, Oliver in {2} of the 51 jurisdictions; {3}, on no ballot, stands where the workbook reports it. The prediction is national and reads each column's national share whole, its write-in votes included, so the factors are the same as with national standing; the count's states spread each column's share over where it stands, in the record's own pattern there (*The count - US-8*).",
                Listed("STEIN"), Listed("KENNEDY"), Listed("OLIVER"), string.Join(", ", onNoBallot)));

            // the stamp: the catalog's digests, the positions, the spreads, the weight, the stored factors and the dated page
            var stamp = new StringBuilder(UsPresidentialReturns.CandidateSourceDigest + UsPresidentialReturns.CandidateStateSourceDigest + UsPresidentialReturns.HouseSourceDigest + UsPresidentialReturns.YearSourceDigest + pageDigest + rncDigest);
            foreach (CountryId id in PartySystems.UsSpreadFrom) { PartySystems.TryElectorate(id, out VoteModel.Electorate e, out double _); stamp.Append(id).Append(e); }
            stamp.Append(string.Join(",", points.Select(p => F("{0}:{1:R}:{2:R}", p.Name, p.Econ, p.Soc)))).Append(w.ToString("R", CultureInfo.InvariantCulture));
            foreach (PresidencyOfRecord.CandidateOfRecord c in backed) { stamp.Append(c.Surname).Append(c.Factor.ToString("R", CultureInfo.InvariantCulture)); }
            return Digest(stamp.ToString());
        }

        /// <summary>The USA's reference world, held with the random streams and the energy state put back after (US-5's eve worlds' discipline), the host destroyed.</summary>
        private static PresidentialReferenceWorld.Result HoldWorld(DateTime? thenTo)
        {
            var host = new GameObject("UsPresidentialFactorCheck");
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                return PresidentialReferenceWorld.Hold(host, CountryId.USA, thenTo);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                EnergyMarket.ResetTurnState();
                SimulationRandom.RestoreState(seedWas, drawsWas);
            }
        }

        /// <summary>A saved page's bytes as text, with whether its SHA-256 is the one its folder's SHA256SUMS.txt lists.</summary>
        private static string ReadPage(string relative, out string digest, out bool hashed)
        {
            byte[] bytes = File.ReadAllBytes(PathOf(relative));
            using (var sha = SHA256.Create()) { digest = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant(); }
            string sums = File.ReadAllText(PathOf(Path.GetDirectoryName(relative).Replace('\\', '/') + "/SHA256SUMS.txt"), Encoding.ASCII);
            string line = digest + " *" + Path.GetFileName(relative);
            hashed = sums.Replace("\r\n", "\n").Split('\n').Contains(line);
            return Encoding.UTF8.GetString(bytes);
        }

        private static double TwoParty(double[] shares) => shares[0] / (shares[0] + shares[1]);

        private static bool Same(VoteModel.Electorate a, VoteModel.Electorate b) => a.MuEcon == b.MuEcon && a.MuSoc == b.MuSoc && a.Sigma == b.Sigma && a.Tau == b.Tau;

        // ---- the US model card's US-6 block (docs/reference/US_ELECTIONS.md) - written by WriteReadings

        private const string CardRelative = "docs/reference/US_ELECTIONS.md";
        private const string CardStamp = "<!-- GENERATED by PoliSim.EditorTools.UsPresidentialFactorCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: ";
        private const string CardEnd = "<!-- END GENERATED -->";

        private static string PathOf(string relative) => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), relative);

        private static string Digest(string text)
        {
            using (var sha = SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLowerInvariant(); }
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

        private static string Pct(double share) => F("{0:0.00} %", 100.0 * share);
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
