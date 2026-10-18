using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
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
    /// PS-6 US-33 (`docs/specs/USA_STAGE_PLAN.md`, "Your own nominee (SP-6a)"; R-US4 (a), R-US6 (b), R-US2 (a), E1; R-US15 (iii) printed, not acted on): THE PLAYER'S
    /// OWN NOMINEE AT THE HEAD OF THE DEM OR REP TICKET (<see cref="OwnNominees"/>). The plan's done-when, as this diagnostic holds it - each world built as a new game
    /// builds one (the epoch set before the world is created) and stepped by the game's own loop:
    /// <list type="bullet">
    /// <item>(a) THE RANGE: the tickets are the two parties the Global Party Survey 2019 places, each range the first to the third quartile of its experts'
    /// placements on V4 and V6 (the generated rows, re-read here cell by cell), the party's own point inside it; offered for the USA alone. The two alternatives
    /// put to Elias (the whole spread; the mean within one standard deviation) are printed from the same rows.</item>
    /// <item>(b) THE RULES: a point outside the range is brought inside it; a nominee with no name, a person of record (every catalog nominee, running mate and
    /// president of record, read by surname and first initial - "Donald Trump", "Joe Biden", "J.D. Vance" among the variants), the unnamed candidate's form (a curly
    /// apostrophe too), a point outside the range, a ticket the survey does not place, or a country that offers none is refused and nothing is kept; a surname of
    /// record under another initial stands.</item>
    /// <item>(c) THE MOVED POINT IN THE POLL (the start's vintage): the party's point is the nominee's - at the party's own point the poll is bit-identical to the
    /// poll with no nominee, off it the split moves; a FRESH solve (the cache dropped) with a nominee off its point puts the electorate on the roster's segment, the electorate
    /// before exactly; the registry leaves nothing behind.</item>
    /// <item>(d) THE 2024 FIELD AT FACTOR 1.0 (the count's own path, a planted poll): the nominee stands for its party in place of the nominee of record, its tally
    /// marked, at the party's poll × 1.0 against the other nominee's fitted factor; with no own nominee the count is the same contest whichever empty list it is
    /// handed - the default game's.</item>
    /// <item>(e) A US GAME STARTED WITH ITS OWN NOMINEE (12 Mar 2024), stepped by the game's loop through the count to the day after the oath: the count held once,
    /// fielding the nominee, its shares exactly the field of the poll the day loop read - a poll that reads the nominee's position (it differs from the poll at
    /// the party's own point); ONE AUTHORITY - with the player re-seated in DEM, REP's nominee still heads the field and REP's point is still its; the
    /// winner sworn as the person the count names.</item>
    /// <item>(f) THE PERSON THE TERM LIMIT READS (planted counts): the nominee elected in 2024 is the president within the limit in 2028 (one election, the game's),
    /// its ticket's running mate the party's own, unnamed; elected again in 2028 it cannot stand in 2032; a DEM nominee beaten in 2024 leaves Trump elected twice
    /// (2016 by the record, 2024 by the game) and is not fielded by the party out of the White House (R-US26, asked).</item>
    /// <item>(g) THE SAVE: a save cut on the eve carries the nominee and loads INTO THE GAME (§557) - the registry restored, the count fielding it; the contest's
    /// marked tally survives the serialiser; a save with both new keys stripped (every save before US-33) loads with none, and its count fields the nominee of
    /// record - additive under ruling A, no format step.</item>
    /// <item>(h) R-US15 (iii), PRINTED: how far the DEM/REP split moves when the party's point moves to the nominee's, under the held spread (σ and τ the four
    /// countries' median) - the idle poll and the count over each ticket's range and at the corners of each alternative range (ask 1), the factor's own share of the move,
    /// and the 2028 idle poll with a beaten party's point kept at its nominee's (ask 2). No figure is judged: the spread's choice is Elias's (a new ask, with this
    /// table).</item>
    /// </list>
    /// No card block: the readings are this check's lines.
    /// </summary>
    public static class UsOwnNomineeDiagnostic
    {
        private const int Seed = 777;
        private static readonly DateTime Polled2024 = new DateTime(2024, 11, 5);
        private static readonly DateTime Polled2028 = new DateTime(2028, 11, 7);
        private static readonly DateTime Polled2032 = new DateTime(2032, 11, 2);
        private static readonly DateTime Oath2025 = new DateTime(2025, 1, 20);

        private const string Hale = "Jordan Hale", Reyes = "Maria Reyes";
        private const string Trump = "Donald J. Trump", Harris = "Kamala D. Harris", Vance = "JD Vance";

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsOwnNomineeDiagnostic (PS-6 US-33): the player's own nominee at the head of the DEM or REP ticket ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            bool hadBefore = OwnNominees.Any(CountryId.USA);
            try { Measure(sb, Check); }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }
            Check(OwnNominees.Any(CountryId.USA) == hadBefore, "the process's own nominees are put back as they were (its scope)");
            sb.Append(failures == 0 ? "=== UsOwnNomineeDiagnostic: the nominee is the player's, the party's point is the nominee's, the person is the limit's ===" : F("=== UsOwnNomineeDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>The random streams, the energy state and the regional derivation a prediction leaves are put back after, the epoch and the own nominees by their
        /// scopes, and every host destroyed (TermLimitDiagnostic's discipline).</summary>
        private static void Measure(StringBuilder sb, Action<bool, string> Check)
        {
            var hosts = new List<GameObject>();
            string dir = Path.Combine(Application.temporaryCachePath, "own_nominee_" + Guid.NewGuid().ToString("N"));
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
                using (SimulationManager.EpochScope())
                using (OwnNominees.Scope())
                {
                    OwnNominees.Clear();
                    MeasureAll(sb, Check, hosts, dir);
                }
            }
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
        }

        private static OwnNominee Nominee(string party, string name, float econ, float soc) =>
            new OwnNominee { Country = CountryId.USA, Party = party, Name = name, LrEcon = econ, Galtan = soc };

        private static UsTermLimit.Nominee Own(string party, string name) =>
            new UsTermLimit.Nominee { Party = party, Person = name, Default = OfRecordName(party), Own = true, Basis = "planted" };

        private static string OfRecordName(string party)
        {
            foreach (PresidencyOfRecord.CandidateOfRecord c in PresidencyOfRecord.CandidatesOf(CountryId.USA, 2025)) { if (c.BackingParty == party) { return PresidencyOfRecord.NameOfRecord(CountryId.USA, 2024, c.Surname); } }
            return null;
        }

        private static double FactorOfRecord(string party)
        {
            foreach (PresidencyOfRecord.CandidateOfRecord c in PresidencyOfRecord.CandidatesOf(CountryId.USA, 2025)) { if (c.BackingParty == party) { return c.Factor; } }
            return double.NaN;
        }

        private static PoliticalParty RosterParty(string key) => PartySystems.RealRoster(CountryId.USA).First(p => p.Abbrev == key);

        /// <summary>The REP share of the two parties in a poll, in per cent.</summary>
        private static double RepOfTwo(IReadOnlyDictionary<string, double> poll) => 100.0 * poll["REP"] / (poll["REP"] + poll["DEM"]);

        private static Dictionary<string, double> PollWith(OwnNominee nominee)
        {
            using (OwnNominees.Preview(nominee))
            {
                NationalElection.TryPredictShares(CountryId.USA, out Dictionary<string, double> poll);
                return poll;
            }
        }

        private static bool SamePoll(IReadOnlyDictionary<string, double> a, IReadOnlyDictionary<string, double> b) =>
            a != null && b != null && a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out double v) && v.Equals(kv.Value));

        private static bool SameCount(UsPresidentialElection.Contest a, UsPresidentialElection.Contest b) => a != null && b != null && PresidencyDiagnostic.SameContest(a, b);

        private static void MeasureAll(StringBuilder sb, Action<bool, string> Check, List<GameObject> hosts, string dir)
        {
            string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string Won(IEnumerable<UsTermLimit.Election> es) => "[" + string.Join(", ", es.Select(e => F("{0} {1} ({2}, {3})", e.PollingDay.Year, e.Person, e.Party, e.InGame ? "the game's" : "the record's"))) + "]";

            // ---- (a) the range, re-read from the survey's rows cell by cell
            List<string> tickets = OwnNominees.Tickets(CountryId.USA);
            Check(tickets.SequenceEqual(new[] { "REP", "DEM" }), F("(a) the tickets offered: [{0}] - the roster's parties the Global Party Survey 2019 places on both axes", string.Join(", ", tickets)));
            foreach (string party in new[] { "REP", "DEM" })
            {
                UsGps2019Row(party, "V4_Scale", out string[] econ);
                UsGps2019Row(party, "V6_Scale", out string[] soc);
                bool ranged = OwnNominees.TryRangeOf(CountryId.USA, party, out OwnNominees.Range r);
                float P(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
                PoliticalParty roster = RosterParty(party);
                Check(ranged && econ != null && soc != null && r.EconLo == P(econ[1]) && r.EconHi == P(econ[3]) && r.SocLo == P(soc[1]) && r.SocHi == P(soc[3])
                      && r.EconExperts == int.Parse(econ[0], CultureInfo.InvariantCulture) && r.SocExperts == int.Parse(soc[0], CultureInfo.InvariantCulture) && r.Holds(roster.LrEcon, roster.Galtan),
                    F("(a) {0}'s range, the first to the third quartile of its experts' placements: economic (V4, {1} experts) {2:0.00} to {3:0.00}, GAL-TAN (V6, {4} experts) {5:0.00} to {6:0.00}; the party's own point ({7:0.00}, {8:0.00}) inside it",
                        party, r.EconExperts, r.EconLo, r.EconHi, r.SocExperts, r.SocLo, r.SocHi, roster.LrEcon, roster.Galtan));
                if (econ != null && soc != null)
                {
                    double eMean = double.Parse(econ[5], CultureInfo.InvariantCulture), eSd = double.Parse(econ[6], CultureInfo.InvariantCulture);
                    double sMean = double.Parse(soc[5], CultureInfo.InvariantCulture), sSd = double.Parse(soc[6], CultureInfo.InvariantCulture);
                    sb.Append(F("    read      (a) {0}'s alternatives, put to Elias: the whole spread - economic {1} to {2}, GAL-TAN {3} to {4}; the mean within one sd - economic {5:0.00} to {6:0.00}, GAL-TAN {7:0.00} to {8:0.00}\n",
                        party, econ[2], econ[4], soc[2], soc[4], eMean - eSd, eMean + eSd, sMean - sSd, sMean + sSd));
                }
            }
            var offeredElsewhere = ((CountryId[])Enum.GetValues(typeof(CountryId))).Where(id => id != CountryId.USA && (OwnNominees.Offered(id) || OwnNominees.Tickets(id).Count > 0)).ToList();
            Check(offeredElsewhere.Count == 0 && OwnNominees.Offered(CountryId.USA), F("(a) offered for the USA alone (a president chosen by electors){0}", offeredElsewhere.Count > 0 ? "; NOT: " + string.Join(", ", offeredElsewhere) : string.Empty));

            // ---- (b) the rules
            {
                OwnNominees.TryRangeOf(CountryId.USA, "REP", out OwnNominees.Range rep);
                OwnNominee low = Nominee("REP", Hale, 0f, 0f), high = Nominee("REP", Hale, 10f, 10f);
                OwnNominees.Clamp(low); OwnNominees.Clamp(high);
                Check(low.LrEcon == rep.EconLo && low.Galtan == rep.SocLo && high.LrEcon == rep.EconHi && high.Galtan == rep.SocHi,
                    F("(b) a point outside the range is brought inside it: (0, 0) to ({0:0.00}, {1:0.00}), (10, 10) to ({2:0.00}, {3:0.00})", low.LrEcon, low.Galtan, high.LrEcon, high.Galtan));
                var refusals = new List<(string What, OwnNominee N)>
                {
                    ("no name", Nominee("REP", "  ", rep.EconLo, rep.SocLo)),
                    ("the president of record", Nominee("REP", Trump, rep.EconLo, rep.SocLo)),
                    ("a running mate of record, in other case", Nominee("REP", "jd vance", rep.EconLo, rep.SocLo)),
                    ("the 2024 Democratic nominee", Nominee("DEM", Harris, 3.5f, 2.5f)),
                    ("a running mate of record", Nominee("DEM", "Tim Walz", 3.5f, 2.5f)),
                    ("the 2020 winner", Nominee("DEM", "Joseph R. Biden Jr.", 3.5f, 2.5f)),
                    ("the 2012 winner", Nominee("DEM", "Barack Obama", 3.5f, 2.5f)),
                    ("the unnamed candidate's form", Nominee("REP", "Republican Party's candidate (2028)", rep.EconLo, rep.SocLo)),
                    // the review's L1: the forms a player types for a person of record - read by surname and first initial, the dots and spaces folded
                    ("the president of record without his initial", Nominee("REP", "Donald Trump", rep.EconLo, rep.SocLo)),
                    ("the president of record with two spaces", Nominee("REP", "Donald  J. Trump", rep.EconLo, rep.SocLo)),
                    ("the president of record's surname alone", Nominee("REP", "Trump", rep.EconLo, rep.SocLo)),
                    ("a running mate of record with dotted initials", Nominee("REP", "J.D. Vance", rep.EconLo, rep.SocLo)),
                    ("the 2024 Democratic nominee without her initial", Nominee("DEM", "Kamala Harris", 3.5f, 2.5f)),
                    ("the 2020 winner by his short name", Nominee("DEM", "Joe Biden", 3.5f, 2.5f)),
                    ("the 2020 winner in lower case, the suffix undotted", Nominee("DEM", "joseph r. biden jr", 3.5f, 2.5f)),
                    ("the 2016 Democratic nominee with her middle name", Nominee("DEM", "Hillary Rodham Clinton", 3.5f, 2.5f)),
                    ("the 2012 winner with a middle initial", Nominee("DEM", "Barack H. Obama", 3.5f, 2.5f)),
                    ("the unnamed candidate's form with a curly apostrophe", Nominee("REP", "Republican Party’s candidate (2028)", rep.EconLo, rep.SocLo)),
                    ("a point outside the range", Nominee("REP", Hale, rep.EconLo - 0.5f, rep.SocLo)),
                    ("a ticket the survey does not place", Nominee("IND", Hale, 5f, 5f)),
                    ("a country that offers none", new OwnNominee { Country = CountryId.Sweden, Party = "S", Name = Hale, LrEcon = 3f, Galtan = 3f }),
                };
                var wrong = new List<string>();
                foreach ((string what, OwnNominee n) in refusals)
                {
                    bool refused = !OwnNominees.TryRegister(n, out string why);
                    if (!refused || string.IsNullOrEmpty(why) || OwnNominees.Any(n.Country)) { wrong.Add(what); }
                    sb.Append(F("    read      (b) refused - {0}: \"{1}\"\n", what, why));
                }
                Check(wrong.Count == 0, F("(b) every refusal refuses, says why and keeps nothing{0}", wrong.Count > 0 ? "; NOT: " + string.Join(", ", wrong) : string.Empty));
                bool accepted = OwnNominees.TryRegister(Nominee("REP", "  Jordan   Hale ", rep.EconLo, rep.SocLo), out string acceptedWhy);
                Check(accepted && OwnNominees.TryOf(CountryId.USA, out OwnNominee kept) && kept.Name == Hale,
                    F("(b) a new person inside the range stands, the name folded to one space between words: {0}", accepted ? "\"" + Hale + "\"" : acceptedWhy));
                OwnNominees.Clear();
                // the reading's edge, stated (the review's L1): a surname of record under another initial is another person
                bool otherInitial = OwnNominees.TryRegister(Nominee("DEM", "Jordan Harris", 3.5f, 2.5f), out string otherWhy);
                Check(otherInitial && !UsTermLimit.IsPersonOfRecord("Jordan Harris"),
                    F("(b) a surname of record under another given initial reads as another person: \"Jordan Harris\" stands{0}", otherInitial ? string.Empty : " - refused: " + otherWhy));
                OwnNominees.Clear();
            }

            // ---- (c) the moved point in the poll, on the start's vintage
            WorldClock.ApplyStart(CountryId.USA);
            {
                PartySystems.TryElectorate(CountryId.USA, out VoteModel.Electorate before, out double _);
                NationalElection.TryPredictShares(CountryId.USA, out Dictionary<string, double> none);
                foreach (string party in new[] { "REP", "DEM" })
                {
                    PoliticalParty roster = RosterParty(party);
                    OwnNominees.TryRangeOf(CountryId.USA, party, out OwnNominees.Range r);
                    Dictionary<string, double> atParty = PollWith(Nominee(party, Hale, roster.LrEcon, roster.Galtan));
                    float offEcon = party == "REP" ? r.EconLo : r.EconHi, offSoc = party == "REP" ? r.SocLo : r.SocHi;   // the corner toward the other party
                    Dictionary<string, double> moved = PollWith(Nominee(party, Hale, offEcon, offSoc));
                    Check(SamePoll(atParty, none), F("(c) {0}: the nominee on the party's own point ({1:0.00}, {2:0.00}) - the poll bit-identical to the poll with no nominee (REP of the two {3:0.000000} %)", party, roster.LrEcon, roster.Galtan, RepOfTwo(atParty)));
                    Check(moved != null && !moved[party].Equals(none[party]),
                        F("(c) {0}: the nominee at ({1:0.00}, {2:0.00}), the range's corner toward the other party - the party's poll {3:0.000000} % against {4:0.000000} % at its own point: the poll reads the nominee's position",
                            party, offEcon, offSoc, 100.0 * (moved?[party] ?? double.NaN), 100.0 * none[party]));
                }
                // the review's L7: a FRESH solve - the vintage's cached electorate dropped first, so the solver runs with a nominee off its party's point - places
                // the mean on the roster's DEM-REP segment and equals the electorate before: the solver reads the roster's points, never the nominee's
                {
                    ElectionVintage vintage = WorldClock.Resolve(CountryId.USA, ElectionVintage.Seated);
                    var cache = typeof(PartySystems).GetField("UsSolved", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as System.Collections.IDictionary;
                    OwnNominees.TryRangeOf(CountryId.USA, "REP", out OwnNominees.Range off);
                    VoteModel.Electorate fresh = default;
                    double place = double.NaN;
                    bool solved = false;
                    using (OwnNominees.Preview(Nominee("REP", Hale, off.EconLo, off.SocLo)))
                    {
                        if (cache != null) { lock (cache) { cache.Clear(); } }
                        solved = OwnNominees.TryPointOf(CountryId.USA, "REP", out double _, out double _) && PartySystems.UsElectorate(vintage, out fresh, out place);
                    }
                    PoliticalParty repRoster = RosterParty("REP"), demRoster = RosterParty("DEM");
                    double demE = demRoster.LrEcon, repE = repRoster.LrEcon, demS = demRoster.Galtan, repS = repRoster.Galtan;
                    double alongE = demE + place * (repE - demE), alongS = demS + place * (repS - demS);
                    Check(cache != null && solved && fresh.MuEcon.Equals(alongE) && fresh.MuSoc.Equals(alongS)
                          && fresh.MuEcon.Equals(before.MuEcon) && fresh.MuSoc.Equals(before.MuSoc) && fresh.Sigma.Equals(before.Sigma) && fresh.Tau.Equals(before.Tau),
                        F("(c) a fresh solve with REP's nominee at ({0:0.00}, {1:0.00}) - the cache dropped first - puts the electorate on the roster's DEM-REP segment at {2:0.000000}: mean ({3:0.000000}, {4:0.000000}), σ {5:0.00}, τ {6:0.00}, the electorate before exactly - the moved point is the party's alone",
                            off.EconLo, off.SocLo, place, fresh.MuEcon, fresh.MuSoc, fresh.Sigma, fresh.Tau));
                }
                NationalElection.TryPredictShares(CountryId.USA, out Dictionary<string, double> after);
                Check(SamePoll(after, none) && !OwnNominees.Any(CountryId.USA), "(c) the previews leave nothing behind: the poll after them is the poll before them, and no nominee is registered");
            }

            // ---- (d) the 2024 field at factor 1.0, through the count's own path on a planted poll
            var repPoll = new Dictionary<string, double> { { "REP", 0.52 }, { "DEM", 0.48 } };
            double minors = PresidencyOfRecord.CandidatesOf(CountryId.USA, 2025).Where(c => c.BackingParty == null).Sum(c => { double s = PresidencyOfRecord.ShareOfRecord(CountryId.USA, 2024, c.Surname); return double.IsNaN(s) || s <= 0.0 ? 0.0 : s; });
            UsPresidentialElection.Contest ofRecord = UsPresidentialElection.Count(CountryId.USA, Polled2024, repPoll, null, out ElectoralCollege.StateCall[] _, out string whyRecord);
            UsPresidentialElection.Contest ownCount = UsPresidentialElection.Count(CountryId.USA, Polled2024, repPoll, new[] { Own("REP", Hale) }, out ElectoralCollege.StateCall[] _, out string whyOwn);
            {
                UsPresidentialElection.Tally rep = ownCount?.Electors.FirstOrDefault(t => t.Party == "REP"), dem = ownCount?.Electors.FirstOrDefault(t => t.Party == "DEM");
                double fD = FactorOfRecord("DEM");
                double expectRep = 100.0 * 0.52 * PresidentialElection.FutureFactor / (0.52 * PresidentialElection.FutureFactor + 0.48 * fD + minors);
                double expectDem = 100.0 * 0.48 * fD / (0.52 * PresidentialElection.FutureFactor + 0.48 * fD + minors);
                Check(rep != null && dem != null && rep.Candidate == Hale && rep.OwnNominee && !dem.OwnNominee && dem.Candidate == Harris
                      && Math.Abs(rep.Share - expectRep) < 1e-9 && Math.Abs(dem.Share - expectDem) < 1e-9,
                    F("(d) the field of record with REP's own nominee (REP 52 / DEM 48 planted): {0} for REP at the party's poll × {1:0.0} - {2:0.000000} % (expected {3:0.000000}), {4} for DEM at the fitted {5:0.000000} - {6:0.000000} % (expected {7:0.000000}); the tally marked: {8}",
                        rep?.Candidate, PresidentialElection.FutureFactor, rep?.Share ?? double.NaN, expectRep, dem?.Candidate, fD, dem?.Share ?? double.NaN, expectDem, rep?.OwnNominee));
                Check(ownCount != null && ownCount.Line.Contains(Hale + " stands for REP in place of the nominee of record, " + Trump + " - the player's own nominee, at the future factor, 1.0"),
                    F("(d) the count's line says who stood in place of whom: \"{0}\"", ownCount?.Line ?? whyOwn));
                UsPresidentialElection.Contest ownDem = UsPresidentialElection.Count(CountryId.USA, Polled2024, repPoll, new[] { Own("DEM", Reyes) }, out ElectoralCollege.StateCall[] _, out string _);
                UsPresidentialElection.Tally demOwn = ownDem?.Electors.FirstOrDefault(t => t.Party == "DEM"), repOfRecord = ownDem?.Electors.FirstOrDefault(t => t.Party == "REP");
                Check(demOwn != null && demOwn.Candidate == Reyes && demOwn.OwnNominee && repOfRecord != null && repOfRecord.Candidate == Trump && !repOfRecord.OwnNominee,
                    F("(d) DEM's own nominee in its place the same way: {0} for DEM, {1} for REP - \"{2}\"", demOwn?.Candidate, repOfRecord?.Candidate, ownDem?.Line));
                // the default game: no own nominee - the count is the same contest whichever empty list it is handed, and the field of record's nominees are named by NARA
                UsPresidentialElection.Contest empty = UsPresidentialElection.Count(CountryId.USA, Polled2024, repPoll, new List<UsTermLimit.Nominee>(), out ElectoralCollege.StateCall[] _, out string _);
                UsPresidentialElection.Contest pure = UsPresidentialElection.Count(CountryId.USA, Polled2024, repPoll, out ElectoralCollege.StateCall[] _, out string _);
                Check(SameCount(ofRecord, empty) && SameCount(ofRecord, pure) && ofRecord.Electors.All(t => !t.OwnNominee) && !ofRecord.Line.Contains("own nominee")
                      && ofRecord.Electors.Any(t => t.Party == "REP" && t.Candidate == Trump),
                    F("(d) with no own nominee the count is the field of record as US-8 built it - one contest for null, an empty list and the no-list overload, no tally marked: \"{0}\"", ofRecord?.Line ?? whyRecord));
            }

            // ---- (e) a US game started with its own nominee, stepped by the game's loop through the count to the day after the oath
            string saveEve = Path.Combine(dir, "eve.json");
            {
                OwnNominees.TryRangeOf(CountryId.USA, "REP", out OwnNominees.Range rep);
                OwnNominee hale = Nominee("REP", Hale, rep.EconLo, rep.SocLo + 0.5f * (rep.SocHi - rep.SocLo));
                bool registered = OwnNominees.TryRegister(hale, out string refusedWhy);
                Check(registered, F("(e) the nominee registered before the world is built, as the flow's START does: {0} at ({1:0.00}, {2:0.00}) heads the REP ticket{3}", Hale, hale.LrEcon, hale.Galtan, registered ? string.Empty : " - refused: " + refusedWhy));
                WorldClock.ApplyStart(CountryId.USA);
                (SimulationManager sim, World w, Country usa) = Open(hosts, CountryId.USA, "REP");
                DateTime start = sim.CurrentDate;
                UsTermLimit.Nominee before = UsTermLimit.NomineeOf(usa, Polled2024, "REP");
                Check(before != null && before.Own && before.Person == Hale && before.Default == Trump && UsTermLimit.NomineesFor(usa, Polled2024).Count == 1,
                    F("(e) from the start ({0}) the field of record's nominee for REP is the player's own: {1} in place of {2} - {3}", Day(start), before?.Person, before?.Default, before?.Basis));
                // the review's L2: ONE AUTHORITY - with the player re-seated in DEM (the film's bills staging re-seats the prime minister's party), the registry still
                // fields REP's own nominee and still moves REP's point: the field and the poll agree, and DEM's field is the record's
                usa.PlayerPartyAbbrev = "DEM";
                List<UsTermLimit.Nominee> reseated = UsTermLimit.NomineesFor(usa, Polled2024);
                bool pointMoved = OwnNominees.TryPointOf(CountryId.USA, "REP", out double _, out double _);
                UsPresidentialElection.Contest reseatedCount = UsPresidentialElection.Count(CountryId.USA, Polled2024, repPoll, reseated, out ElectoralCollege.StateCall[] _, out string _);
                bool demOfRecord = UsTermLimit.NomineeOf(usa, Polled2024, "DEM") == null;
                usa.PlayerPartyAbbrev = "REP";
                Check(reseated.Count == 1 && reseated[0].Own && reseated[0].Party == "REP" && reseated[0].Person == Hale && pointMoved && demOfRecord
                      && reseatedCount != null && reseatedCount.Electors.Any(t => t.Party == "REP" && t.Candidate == Hale && t.OwnNominee),
                    F("(e) one authority: with the player re-seated in DEM, REP's own nominee still heads the field ({0}) and REP's point is still the nominee's ({1}); DEM's field is the record's - the count fields {2}",
                        reseated.Count == 1 ? reseated[0].Person : reseated.Count + " nominee(s)", pointMoved, reseatedCount?.Electors.FirstOrDefault(t => t.Party == "REP")?.Candidate ?? "no count"));
                bool refusedPick = !UsTermLimit.Pick(usa, Polled2024, "REP", Vance, out string pickWhy);
                Check(refusedPick && pickWhy.StartsWith("the player's own nominee stands that day", StringComparison.Ordinal) && usa.UsNomineePicks.Count == 0,
                    F("(e) the term limit's pick does not reach the own nominee's day: \"{0}\"", pickWhy));
                Dictionary<string, double> pollRead = null, pollAtParty = null;
                int held = 0;
                for (int d = 0; d < 400 && sim.CurrentDate < Oath2025.AddDays(1); d++)
                {
                    if (sim.CurrentDate == Polled2024.AddDays(-1))
                    {
                        SaveGameService.SaveToFile(saveEve, SaveGameService.CreateSaveGame(sim, w, CountryId.USA, null));
                        // the polling day, the day loop's own order: the count is the last thing AdvanceDay does, so the poll read right after it is the poll it read
                        bool boundary = sim.AdvanceDay();
                        if (UsPresidentialElection.Held(usa, sim.CurrentDate)) { held++; }
                        PoliticalParty roster = RosterParty("REP");
                        using (OwnNominees.Preview(Nominee("REP", Hale, roster.LrEcon, roster.Galtan)))
                        {
                            NationalElection.TryPredictShares(CountryId.USA, out pollAtParty, EconomicVote.RecordOverTerm(usa, sim.CurrentDate, out _), on: sim.CurrentDate);
                        }
                        // last, so the regional derivation the day's tick reads is the one the count's own prediction left
                        NationalElection.TryPredictShares(CountryId.USA, out pollRead, EconomicVote.RecordOverTerm(usa, sim.CurrentDate, out _), on: sim.CurrentDate);
                        if (sim.PlayerCountryId.HasValue) { sim.AdvanceCountryDayTick(sim.PlayerCountryId.Value); }
                        if (boundary) { TurnOf(sim, w); }
                        continue;
                    }
                    Step(sim, w);
                }
                UsPresidentialElection.Contest played = usa.UsPresidentialElections.FirstOrDefault(c => c.PollingDay == Polled2024);
                UsPresidentialElection.Tally playedRep = played?.Electors.FirstOrDefault(t => t.Party == "REP"), playedDem = played?.Electors.FirstOrDefault(t => t.Party == "DEM");
                UsPresidentialElection.TryField(CountryId.USA, Polled2024, pollRead, UsTermLimit.NomineesFor(usa, Polled2024), out List<UsPresidentialElection.Tally> field, out string _);
                UsPresidentialElection.Tally fieldRep = field.FirstOrDefault(t => t.Party == "REP"), fieldDem = field.FirstOrDefault(t => t.Party == "DEM");
                Check(held == 1 && usa.UsPresidentialElections.Count == 1 && playedRep != null && playedRep.Candidate == Hale && playedRep.OwnNominee
                      && fieldRep != null && fieldDem != null && playedRep.Share.Equals(fieldRep.Share) && playedDem.Share.Equals(fieldDem.Share),
                    F("(e) the count held once on {0}, fielding {1} for REP at {2:0.000000} % - exactly the field of the poll the day loop read (REP {3:0.000000} %, DEM {4:0.000000} % of the poll): \"{5}\"",
                        Day(Polled2024), playedRep?.Candidate, playedRep?.Share ?? double.NaN, 100.0 * (pollRead?["REP"] ?? double.NaN), 100.0 * (pollRead?["DEM"] ?? double.NaN), played?.Line ?? "no count"));
                Check(pollRead != null && pollAtParty != null && !pollRead["REP"].Equals(pollAtParty["REP"]),
                    F("(e) the 2024 count read the nominee's position through the poll: REP of the two {0:0.000000} % with the nominee at ({1:0.00}, {2:0.00}), {3:0.000000} % at the party's own point the same day",
                        pollRead != null ? RepOfTwo(pollRead) : double.NaN, hale.LrEcon, hale.Galtan, pollAtParty != null ? RepOfTwo(pollAtParty) : double.NaN));
                bool sworn = UsTermLimit.TryPresidentOn(usa, Oath2025.AddDays(1), out string person, out string party, out string shownAs);
                string expected = played != null && played.Decided() ? UsTermLimit.WinnerOf(played) : null;
                Check(played != null && played.Decided() && sworn && person == expected && (played.WinnerParty != "REP" || person == Hale) && (played.WinnerParty != "DEM" || person == Harris),
                    F("(e) the winner sworn on {0} is the person the count names: {1} ({2}), shown as {3}", Day(Oath2025), person, party, shownAs));
                sb.Append(F("    read      (e) the game from {0} with its own nominee counts its own - {1}\n", Day(start), played?.Line ?? "no count"));
                EnergyMarket.ResetTurnState();
                OwnNominees.Clear();
            }

            // ---- (f) the person the term limit reads, on planted counts
            {
                Check(ownCount != null && ownCount.Decided() && ownCount.WinnerParty == "REP" && UsTermLimit.WinnerOf(ownCount) == Hale,
                    F("(f) the planted 2024 count with REP's own nominee elects {0} (the person {1})", ownCount?.Winner, ownCount != null ? UsTermLimit.WinnerOf(ownCount) : null));
                SimulationManager.SetEpoch(Polled2028.AddDays(-1));
                (SimulationManager sim, World w, Country usa) = Open(hosts, CountryId.USA, "REP");
                usa.UsPresidentialElections.Add(Copy(ownCount));
                List<UsTermLimit.Election> hale = UsTermLimit.ElectionsOf(usa, Hale, Polled2028), trump = UsTermLimit.ElectionsOf(usa, Trump, Polled2028);
                UsTermLimit.Nominee rep = UsTermLimit.NomineeOf(usa, Polled2028, "REP");
                bool vice = UsTermLimit.TryVicePresidentOn(usa, Polled2028, out string vp, out string vpParty, out string vpBasis);
                List<string> people = UsTermLimit.PartyPeople(usa, "REP", Polled2028);
                Check(hale.Count == 1 && hale[0].InGame && hale[0].PollingDay == Polled2024 && UsTermLimit.Eligible(usa, Hale, Polled2028) && trump.Count == 1 && !trump[0].InGame,
                    F("(f) each person's elections: {0} {1}; {2} {3} - the game's 2024 is the nominee's, never NARA's nominee's", Hale, Won(hale), Trump, Won(trump)));
                Check(rep != null && !rep.Own && !rep.TermLimited && rep.Person == Hale && rep.Basis.Contains("elected 2024"),
                    F("(f) 2028: REP fields its president, {0}, within the limit - {1}", rep?.Person, rep?.Basis));
                Check(vice && vp == "Republican Party's candidate for Vice President (2024)" && vpParty == "REP" && people.Contains(Hale) && !people.Contains(Vance),
                    F("(f) the nominee's ticket was the game's: its running mate the party's own, unnamed - {0} ({1}); the party's people {2}", vp, vpBasis, "[" + string.Join("; ", people) + "]"));
                for (int d = 0; d < 2 && sim.CurrentDate < Polled2028; d++) { Step(sim, w); }
                UsPresidentialElection.Contest second = usa.UsPresidentialElections.FirstOrDefault(c => c.PollingDay == Polled2028);
                Check(second != null && second.Electors.Any(t => t.Party == "REP" && t.Candidate == Hale && !t.OwnNominee),
                    F("(f) the game's own 2028 count fields {0} for REP as the president, not as an own nominee: \"{1}\"", Hale, second?.Line ?? "no count"));
                EnergyMarket.ResetTurnState();

                // elected again in 2028, barred in 2032
                UsPresidentialElection.Contest win2028 = UsPresidentialElection.Count(CountryId.USA, Polled2028, repPoll, UsTermLimit.NomineesFor(usa, Polled2028), out ElectoralCollege.StateCall[] _, out string why2028);
                SimulationManager.SetEpoch(Polled2032.AddDays(-1));
                (SimulationManager _, World _, Country later) = Open(hosts, CountryId.USA, "REP");
                later.UsPresidentialElections.Add(Copy(ownCount));
                if (win2028 != null) { later.UsPresidentialElections.Add(Copy(win2028)); }
                UsTermLimit.Nominee rep2032 = UsTermLimit.NomineeOf(later, Polled2032, "REP");
                Check(win2028 != null && win2028.WinnerParty == "REP" && UsTermLimit.WinnerOf(win2028) == Hale && rep2032 != null && rep2032.TermLimited && rep2032.Barred == Hale
                      && rep2032.BarredBecause == "elected 2024 and 2028" && rep2032.ViceDefault && rep2032.Person == "Republican Party's candidate for Vice President (2024)"
                      && !UsTermLimit.EligibleFor(later, "REP", Polled2032).Contains(Hale),
                    F("(f) elected again in 2028 ({0}), {1} cannot stand in 2032 - {2} ({3}); REP fields {4}, the 2024 ticket's running mate kept on the re-elected 2028 ticket (R-US27 (ii), ruled) - {5}",
                        win2028?.Winner ?? why2028, Hale, rep2032?.Barred, rep2032?.BarredBecause, rep2032?.Person, rep2032?.Basis));
                EnergyMarket.ResetTurnState();

                // a DEM nominee beaten in 2024
                UsPresidentialElection.Contest beaten = UsPresidentialElection.Count(CountryId.USA, Polled2024, repPoll, new[] { Own("DEM", Reyes) }, out ElectoralCollege.StateCall[] _, out string _);
                SimulationManager.SetEpoch(Polled2028.AddDays(-1));
                (SimulationManager _, World _, Country demWorld) = Open(hosts, CountryId.USA, "DEM");
                if (beaten != null) { demWorld.UsPresidentialElections.Add(Copy(beaten)); }
                List<UsTermLimit.Election> trumpAfter = UsTermLimit.ElectionsOf(demWorld, Trump, Polled2028);
                UsTermLimit.Nominee rep2028 = UsTermLimit.NomineeOf(demWorld, Polled2028, "REP"), dem2028 = UsTermLimit.NomineeOf(demWorld, Polled2028, "DEM");
                Check(beaten != null && beaten.WinnerParty == "REP" && UsTermLimit.WinnerOf(beaten) == Trump && trumpAfter.Count == 2 && rep2028 != null && rep2028.TermLimited && rep2028.Person == Vance
                      && dem2028 != null && dem2028.Person == "Democratic Party's candidate (2028)" && UsTermLimit.PartyPeople(demWorld, "DEM", Polled2028).Contains(Reyes) && UsTermLimit.Eligible(demWorld, Reyes, Polled2028) && dem2028.OutParty && UsTermLimit.EligibleFor(demWorld, "DEM", Polled2028).Contains(Reyes),
                    F("(f) a DEM nominee beaten in 2024 ({0}): {1} elected {2} cannot stand, REP fields {3} - {4}; DEM, out of the White House, fields \"{5}\" by default, {6} among its people, eligible, and the DEM player's to pick (R-US26 (a), ruled)",
                        beaten?.Winner, Trump, Won(trumpAfter), rep2028?.Person, rep2028?.Basis, dem2028?.Person, Reyes));
                EnergyMarket.ResetTurnState();
            }

            // ---- (g) the save: the eve's save carries the nominee and loads into the game; a save from before US-33 loads with none
            {
                SaveGame save = File.Exists(saveEve) ? SaveGameService.LoadFromFile(saveEve) : null;
                bool carried = save != null && save.OwnNominees.Count == 1 && save.OwnNominees[0].Name == Hale && save.OwnNominees[0].Party == "REP";
                Check(carried, F("(g) the save cut on the eve of {0} carries the own nominee: {1}", Day(Polled2024), save == null ? "no save was cut" : string.Join("; ", save.OwnNominees.Select(n => F("{0} ({1}) at ({2:0.00}, {3:0.00})", n.Name, n.Party, n.LrEcon, n.Galtan)))));
                if (save != null)
                {
                    OwnNominees.Clear();
                    (SimulationManager adopted, PoliSim.UI.GameController _) = PresidencyDiagnostic.Adopt(save, hosts, out string error);
                    if (adopted == null) { Check(false, F("(g) the eve's save: {0}", error)); }
                    else
                    {
                        try
                        {
                            bool restored = carried && OwnNominees.TryOf(CountryId.USA, out OwnNominee back) && back.Name == Hale && back.LrEcon.Equals(save.OwnNominees[0].LrEcon) && back.Galtan.Equals(save.OwnNominees[0].Galtan);
                            Country u = adopted.World.GetCountry(CountryId.USA);
                            Step(adopted, adopted.World);
                            UsPresidentialElection.Contest loaded = u.UsPresidentialElections.FirstOrDefault(c => c.PollingDay == Polled2024);
                            Check(restored && loaded != null && loaded.Electors.Any(t => t.Party == "REP" && t.Candidate == Hale && t.OwnNominee),
                                F("(g) loaded INTO THE GAME (format {0}): the own nominee restored ({1}), and stepped to {2} the count fields it - \"{3}\"", save.SaveVersion, restored, Day(adopted.CurrentDate), loaded?.Line ?? "no count"));
                            UsPresidentialElection.Contest copied = loaded == null ? null : Copy(loaded);
                            Check(copied != null && PresidencyDiagnostic.SameContest(copied, loaded) && UsTermLimit.PersonOf(copied, copied.Electors.First(t => t.Party == "REP")) == Hale,
                                "(g) the contest's marked tally survives the save's serialiser, and the person it names is the nominee");
                        }
                        finally { EnergyMarket.ResetTurnState(); }
                    }
                    // a save from before US-33: both new keys stripped - it loads with no own nominee, and its count fields the nominee of record
                    string json = File.ReadAllText(saveEve);
                    string stripped = Regex.Replace(Regex.Replace(json, ",\\s*\"OwnNominees\"\\s*:\\s*\\[[^\\]]*\\]", string.Empty), ",\\s*\"OwnNominee\"\\s*:\\s*(true|false)", string.Empty);
                    SaveGame older = SaveGameService.Deserialize(stripped);
                    OwnNominees.TryRegister(Nominee("REP", Hale, 8.0f, 7.5f), out string _);   // a process holding one - the load must replace it with the save's none
                    (SimulationManager olderSim, PoliSim.UI.GameController _) = PresidencyDiagnostic.Adopt(older, hosts, out string olderError);
                    if (olderSim == null) { Check(false, F("(g) the stripped save: {0}", olderError)); }
                    else
                    {
                        try
                        {
                            bool noneAfterLoad = !OwnNominees.Any(CountryId.USA);
                            Country u = olderSim.World.GetCountry(CountryId.USA);
                            Step(olderSim, olderSim.World);
                            UsPresidentialElection.Contest loaded = u.UsPresidentialElections.FirstOrDefault(c => c.PollingDay == Polled2024);
                            Check(!stripped.Contains("\"OwnNominees\"") && older.OwnNominees != null && older.OwnNominees.Count == 0 && noneAfterLoad
                                  && loaded != null && loaded.Electors.Any(t => t.Party == "REP" && t.Candidate == Trump && !t.OwnNominee),
                                F("(g) a save with no own-nominee keys - every save before US-33 - loads with none (the process's replaced) and its count fields the nominee of record: \"{0}\" (additive under ruling A: no format step)", loaded?.Line ?? "no count"));
                        }
                        finally { EnergyMarket.ResetTurnState(); }
                    }
                    OwnNominees.Clear();
                }
            }

            // ---- (h) R-US15 (iii): the moved point's response under the held spread - printed, not judged
            WorldClock.ApplyStart(CountryId.USA);
            {
                PartySystems.TryElectorate(CountryId.USA, out VoteModel.Electorate e, out double _);
                NationalElection.TryPredictShares(CountryId.USA, out Dictionary<string, double> idle);
                double baseRep = RepOfTwo(idle);
                sb.Append(F("    read      (h) the held spread on the start's vintage ({0}): σ {1:0.00}, τ {2:0.00}, the electorate's mean ({3:0.000}, {4:0.000}); the idle poll REP of the two {5:0.0000} %\n",
                    WorldClock.SeatedVintage(CountryId.USA, WorldClock.StartDate(CountryId.USA)), e.Sigma, e.Tau, e.MuEcon, e.MuSoc, baseRep));
                UsPresidentialElection.Contest idleOfRecord = UsPresidentialElection.Count(CountryId.USA, Polled2024, idle, null, out ElectoralCollege.StateCall[] _, out string _);
                bool finite = idleOfRecord != null;
                foreach (string party in new[] { "REP", "DEM" })
                {
                    PoliticalParty roster = RosterParty(party);
                    OwnNominees.TryRangeOf(CountryId.USA, party, out OwnNominees.Range r);
                    UsPresidentialElection.Contest atOwnPoint = UsPresidentialElection.Count(CountryId.USA, Polled2024, PollWith(Nominee(party, Hale, roster.LrEcon, roster.Galtan)), new[] { Own(party, Hale) }, out ElectoralCollege.StateCall[] _, out string _);
                    double FactorMove(UsPresidentialElection.Contest c) => c == null || idleOfRecord == null ? double.NaN : c.Electors.First(t => t.Party == "REP").Share - idleOfRecord.Electors.First(t => t.Party == "REP").Share;
                    sb.Append(F("    read      (h) {0}: the factor alone - the nominee on the party's own point ({1:0.00}, {2:0.00}) at {3:0.0} in place of the fitted {4:0.000000}: REP's count share {5:+0.0000;-0.0000} pp, electors REP {6} / DEM {7} (the field of record: {8} / {9})\n",
                        party, roster.LrEcon, roster.Galtan, PresidentialElection.FutureFactor, FactorOfRecord(party), FactorMove(atOwnPoint),
                        atOwnPoint?.ElectorsOf("REP") ?? -1, atOwnPoint?.ElectorsOf("DEM") ?? -1, idleOfRecord?.ElectorsOf("REP") ?? -1, idleOfRecord?.ElectorsOf("DEM") ?? -1));
                    double widest = 0.0;
                    float[] econs = { r.EconLo, 0.5f * (r.EconLo + r.EconHi), r.EconHi }, socs = { r.SocLo, 0.5f * (r.SocLo + r.SocHi), r.SocHi };
                    foreach (float soc in socs)
                    {
                        foreach (float econ in econs)
                        {
                            Dictionary<string, double> moved = PollWith(Nominee(party, Hale, econ, soc));
                            UsPresidentialElection.Contest counted = UsPresidentialElection.Count(CountryId.USA, Polled2024, moved, new[] { Own(party, Hale) }, out ElectoralCollege.StateCall[] _, out string _);
                            double split = RepOfTwo(moved), move = split - baseRep;
                            finite &= !double.IsNaN(split) && !double.IsInfinity(split) && counted != null;
                            widest = Math.Max(widest, Math.Abs(move));
                            sb.Append(F("    read      (h) {0} at ({1:0.00}, {2:0.00}): the poll's REP of the two {3:0.0000} % ({4:+0.0000;-0.0000} pp from the party's point); the count {5} REP / {6} DEM electors, REP {7:0.0000} % of the vote\n",
                                party, econ, soc, split, move, counted?.ElectorsOf("REP") ?? -1, counted?.ElectorsOf("DEM") ?? -1, counted?.Electors.First(t => t.Party == "REP").Share ?? double.NaN));
                        }
                    }
                    sb.Append(F("    read      (h) {0}: the widest move of the split over its range, {1:0.0000} pp - the moved point's response under the held spread (R-US15 (iii): the spread's choice stays Elias's)\n", party, widest));

                    // the review's L5 (ask 1): the same response at the corners of each alternative range - the whole spread, and the mean within one sd -
                    // probed past today's range (an instrument's probe, `OwnNominees.ProbeAt`), so the ask carries what a wider range would do
                    UsGps2019Row(party, "V4_Scale", out string[] ve);
                    UsGps2019Row(party, "V6_Scale", out string[] vs);
                    if (ve == null || vs == null) { finite = false; continue; }
                    float Cell(string c) => float.Parse(c, NumberStyles.Float, CultureInfo.InvariantCulture);
                    double eMean = double.Parse(ve[5], CultureInfo.InvariantCulture), eSd = double.Parse(ve[6], CultureInfo.InvariantCulture);
                    double sMean = double.Parse(vs[5], CultureInfo.InvariantCulture), sSd = double.Parse(vs[6], CultureInfo.InvariantCulture);
                    var boxes = new (string Name, float ELo, float EHi, float SLo, float SHi)[]
                    {
                        ("the whole spread", Cell(ve[2]), Cell(ve[4]), Cell(vs[2]), Cell(vs[4])),
                        ("the mean within one sd", (float)(eMean - eSd), (float)(eMean + eSd), (float)Math.Max(0.0, sMean - sSd), (float)Math.Min(10.0, sMean + sSd)),
                    };
                    foreach ((string box, float eLo, float eHi, float sLo, float sHi) in boxes)
                    {
                        double boxWidest = 0.0;
                        foreach ((float econ, float soc) in new[] { (eLo, sLo), (eLo, sHi), (eHi, sLo), (eHi, sHi) })
                        {
                            Dictionary<string, double> probed;
                            using (OwnNominees.ProbeAt(CountryId.USA, party, econ, soc))
                            {
                                bool placed = OwnNominees.TryPointOf(CountryId.USA, party, out double _, out double _);
                                NationalElection.TryPredictShares(CountryId.USA, out probed);
                                finite &= placed;
                            }
                            UsPresidentialElection.Contest counted = UsPresidentialElection.Count(CountryId.USA, Polled2024, probed, new[] { Own(party, Hale) }, out ElectoralCollege.StateCall[] _, out string _);
                            double split = RepOfTwo(probed), move = split - baseRep;
                            finite &= !double.IsNaN(split) && !double.IsInfinity(split) && counted != null;
                            boxWidest = Math.Max(boxWidest, Math.Abs(move));
                            sb.Append(F("    read      (h) {0} under {1}, at ({2:0.00}, {3:0.00}): the poll's REP of the two {4:0.0000} % ({5:+0.0000;-0.0000} pp from the party's point); the count {6} REP / {7} DEM electors, REP {8:0.0000} % of the vote\n",
                                party, box, econ, soc, split, move, counted?.ElectorsOf("REP") ?? -1, counted?.ElectorsOf("DEM") ?? -1, counted?.Electors.First(t => t.Party == "REP").Share ?? double.NaN));
                        }
                        sb.Append(F("    read      (h) {0} under {1}: the widest move of the split at its corners, {2:0.0000} pp (ask 1, the range statistic)\n", party, box, boxWidest));
                    }
                }

                // the review's L5 (ask 2): the point KEPT after a 2024 loss - the 2028 idle poll (no record shift: the loss enters the poll only through the
                // economic vote's record, which the idle poll leaves out) with the beaten party at its nominee's point against its roster point, and the 2028
                // count on each (the field unnamed, every nominee at the future factor)
                SimulationManager.SetEpoch(Polled2028.AddDays(-1));
                NationalElection.TryPredictShares(CountryId.USA, out Dictionary<string, double> idle2028);
                UsPresidentialElection.Contest roster2028 = UsPresidentialElection.Count(CountryId.USA, Polled2028, idle2028, null, out ElectoralCollege.StateCall[] _, out string _);
                foreach (string party in new[] { "REP", "DEM" })
                {
                    OwnNominees.TryRangeOf(CountryId.USA, party, out OwnNominees.Range r);
                    float keptEcon = party == "REP" ? r.EconLo : r.EconHi, keptSoc = party == "REP" ? r.SocLo : r.SocHi;   // the corner toward the other party, as (c)
                    Dictionary<string, double> kept = PollWith(Nominee(party, Hale, keptEcon, keptSoc));
                    UsPresidentialElection.Contest keptCount = UsPresidentialElection.Count(CountryId.USA, Polled2028, kept, null, out ElectoralCollege.StateCall[] _, out string _);
                    finite &= kept != null && keptCount != null && roster2028 != null;
                    sb.Append(F("    read      (h) ask 2 - {0}'s nominee beaten in 2024, its point kept at ({1:0.00}, {2:0.00}): the 2028 idle poll's REP of the two {3:0.0000} % against {4:0.0000} % at the roster's point ({5:+0.0000;-0.0000} pp); the 2028 count {6} REP / {7} DEM against {8} / {9}\n",
                        party, keptEcon, keptSoc, kept != null ? RepOfTwo(kept) : double.NaN, RepOfTwo(idle2028), kept != null ? RepOfTwo(kept) - RepOfTwo(idle2028) : double.NaN,
                        keptCount?.ElectorsOf("REP") ?? -1, keptCount?.ElectorsOf("DEM") ?? -1, roster2028?.ElectorsOf("REP") ?? -1, roster2028?.ElectorsOf("DEM") ?? -1));
                }
                Check(finite, "(h) the response is printed for every point of both ranges, the alternative ranges' corners and the 2028 poll with the point kept - finite, and a count for each (no figure is judged: the spread is held, R-US15 (iii) is Elias's to rule; the range and the point's reverting are asked)");
            }
        }

        /// <summary>A row of the survey's extract as its cells, read from the generated rows: [experts, q1, min, q3, max, mean, sd]; null where it is not there.</summary>
        private static void UsGps2019Row(string party, string item, out string[] cells)
        {
            cells = null;
            foreach (PoliSim.Data.Generated.UsGps2019.Row r in PoliSim.Data.Generated.UsGps2019.Rows)
            {
                if (r.Party == party && r.Item == item) { cells = new[] { r.Experts, r.Q1, r.Min, r.Q3, r.Max, r.Mean, r.Sd }; }
            }
        }

        private static UsPresidentialElection.Contest Copy(UsPresidentialElection.Contest c) =>
            JsonConvert.DeserializeObject<UsPresidentialElection.Contest>(JsonConvert.SerializeObject(c, SaveGameService.BuildSettings()), SaveGameService.BuildSettings());

        /// <summary>A world as a new game builds one, the epoch already set: seeded, the player's country and party seated.</summary>
        private static (SimulationManager Sim, World World, Country Usa) Open(List<GameObject> hosts, CountryId player, string party)
        {
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("UsOwnNomineeDiagnostic"); hosts.Add(go);
            var s = go.AddComponent<SimulationManager>();
            s.SetWorld(w);
            s.PlayerCountryId = player;
            w.GetCountry(player).PlayerPartyAbbrev = party;
            return (s, w, w.GetCountry(CountryId.USA));
        }

        /// <summary>One day in the controller's order (GameController: AdvanceDay, the player's day tick, the boundary's turn).</summary>
        private static void Step(SimulationManager s, World w)
        {
            bool boundary = s.AdvanceDay();
            if (s.PlayerCountryId.HasValue) { s.AdvanceCountryDayTick(s.PlayerCountryId.Value); }
            if (boundary) { TurnOf(s, w); }
        }

        private static void TurnOf(SimulationManager s, World w)
        {
            var none = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country c in w.Countries) { none[c.Id] = PolicyDecision.None(); }
            s.AdvanceTurn(none);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
