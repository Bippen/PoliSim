using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-29 (`docs/specs/USA_STAGE_PLAN.md`; R-US12 (a), ruled F8): <b>THE FED CHAIR BY STATUTE</b> - the term AF-7 built (`CentralBankTerms.Fed`,
    /// held by `CentralBankTermCheck`), and the half US-29 adds: the President nominates and the Senate confirms by a majority (<see cref="UsFedConsent"/>).
    /// The plan's done-when: "the term ends on its statutory date, and a hostile Senate refuses a nominee by the count". It holds:
    /// <list type="bullet">
    /// <item>(a) the sources - 12 U.S.C. 242's consent words and its 4-year term, the Senate's precedent of 21 Nov 2013 (majority cloture on nominations),
    /// and the two confirmations of record (Yellen 2014, Powell 2018): each page to its SHA-256, each tally read off the Senate's own XML, and the party split
    /// <see cref="UsFedConsent"/>'s summary states read again;</item>
    /// <item>(b) the statutory date - the USA's start seats its Fed chair for 12 U.S.C. 242's four years, and with no player the AI president's nominee is
    /// seated on the term's day and not a day before (the Senate of record by date confirming: a DEM president, a DEM Senate);</item>
    /// <item>(c) a hostile Senate - the same world with the record's Senate turned over refuses the nominee by the count on the term's day, the chair serves
    /// on, and the day the record's Senate stands again the same nominee is confirmed and seated for four years from that day; no counted stream moves;</item>
    /// <item>(d) an equal division - decided by the Vice President of the president's party;</item>
    /// <item>(e) the player-president - the hold rises only where the Senate would confirm; under a hostile Senate the manager's verb refuses with the count and
    /// the chair stays; under the record's Senate the verb seats the pick for four years and the hold ends;</item>
    /// <item>(e2) the role gate (the review of 2026-10-22, L2): a US player who is not the president (the other major party in the player's place) gets no
    /// hold, the verb refuses in the role gate's words (`SimulationManager.PlayerMayIntroduce`'s, never the Senate's), and the AI president's keyed nominee
    /// goes to the count and is seated;</item>
    /// <item>(f) a CHECK, not a measurement (the review's M4): in every start's world with no player - the trajectory dump's kind, where the AI USA's chair runs
    /// - every chair term day of the century (the statute's four years from the seeded term's end, while confirmed) is counted (the president of record,
    /// the Senate of record by date: an AI USA holds no contest and seats no Senate), and every count must confirm, so the AF-7 pick is seated on the AF-7
    /// day and no start's trajectory moves by US-29. A refused term day fails here by name - a later record row (a Senate class, a president's end) that
    /// turned the AI USA's count against its nominee would move the sentinel, and this is the check that says why. The dump diff is still the proof.</item>
    /// </list>
    /// </summary>
    public static class FedChairConsentDiagnostic
    {
        private const int Seed = 777;
        private const string FedFolder = "ElectionsData/usa/raw/fed/";

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== FedChairConsentDiagnostic (PS-6 US-29, R-US12 (a)): the Fed chair's statutory term, the President's nominee, the Senate's consent by majority ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            var hosts = new List<GameObject>();
            try
            {
                using (SimulationManager.EpochScope()) { Measure(Check, sb, hosts); }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                EnergyMarket.ResetTurnState();
                SimulationRandom.RestoreState(seedWas, drawsWas);
            }
            sb.Append(failures == 0 ? "=== FedChairConsentDiagnostic: the chair's term is the statute's, and the Senate confirms by the count ===" : F("=== FedChairConsentDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static string Words(string html)
        {
            string s = Regex.Replace(html ?? string.Empty, "<[^>]*>", " ");
            s = s.Replace("&nbsp;", " ").Replace("&#160;", " ").Replace("&amp;", "&").Replace("&quot;", "\"");
            return Regex.Replace(s, @"\s+", " ");
        }

        /// <summary>One roll call's XML: the tally it prints, and its members' votes counted by party - "D Yea", "R Nay", ...</summary>
        private static bool RollCall(string file, out int yeas, out int nays, out Dictionary<string, int> byParty, out string question)
        {
            yeas = nays = -1; byParty = new Dictionary<string, int>(); question = null;
            if (!PresidencyDiagnostic.Page(FedFolder + file, out string xml)) { return false; }
            Match y = Regex.Match(xml, @"<count>\s*<yeas>(\d+)</yeas>\s*<nays>(\d+)</nays>");
            if (y.Success) { yeas = int.Parse(y.Groups[1].Value, CultureInfo.InvariantCulture); nays = int.Parse(y.Groups[2].Value, CultureInfo.InvariantCulture); }
            Match q = Regex.Match(xml, @"<vote_document_text>([^<]*)</vote_document_text>");
            question = q.Success ? q.Groups[1].Value.Trim() : null;
            foreach (Match m in Regex.Matches(xml, @"<member>.*?<party>(\w)</party>.*?<vote_cast>([^<]*)</vote_cast>.*?</member>", RegexOptions.Singleline))
            {
                string key = m.Groups[1].Value + " " + m.Groups[2].Value.Trim();
                byParty[key] = byParty.TryGetValue(key, out int n) ? n + 1 : 1;
            }
            return yeas >= 0;
        }


        private static void Measure(Action<bool, string> Check, StringBuilder sb, List<GameObject> hosts)
        {
            string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // ---- (a) the sources ----
            bool statute = PresidencyDiagnostic.Page(CentralBankTerms.Fed.Source, out string statuteHtml);
            string statuteText = Words(statuteHtml);
            Check(statute && statuteText.Contains("1 shall be designated by the President, by and with the advice and consent of the Senate, to serve as Chairman of the Board for a term of 4 years")
                  && CentralBankTerms.Fed.TermYears == 4,
                F("(a) 12 U.S.C. 242 held to its SHA-256: the Chairman \"designated by the President, by and with the advice and consent of the Senate ... for a term of 4 years\" - the row's term {0} years", CentralBankTerms.Fed.TermYears));
            bool precedent = PresidencyDiagnostic.Page(FedFolder + "crec_2013-11-21_PgS8413-5.htm", out string crecHtml);
            string crec = Words(crecHtml);
            Check(precedent && crec.Contains("Under the precedent set by the Senate today, November 21, 2013, the threshold for cloture on nominations, not including those to the Supreme Court of the United States, is now a majority.")
                  && crec.Contains("yeas 52, nays 48"),
                "(a) the Congressional Record of 21 Nov 2013 held to its SHA-256: \"the threshold for cloture on nominations, not including those to the Supreme Court ... is now a majority\", sustained 52-48 - so the Fed chair, an executive nomination, is confirmed by a majority");
            bool yellen = RollCall("wayback_senate_vote_113_2_00001_20201214112740.xml", out int yy, out int yn, out Dictionary<string, int> yp, out string yq);
            Check(yellen && yy == 56 && yn == 26 && Of(yp, "D Yea") == 45 && Of(yp, "D Nay") == 0 && Of(yp, "R Yea") == 11 && Of(yp, "R Nay") == 26,
                F("(a) Yellen, 6 Jan 2014 (113th, 2nd session, No. 1; {0}): {1}-{2}; a DEM president's party {3}-{4}, the other party {5}-{6} - its majority against",
                    yq ?? "?", yy, yn, Of(yp, "D Yea") + Of(yp, "I Yea"), Of(yp, "D Nay") + Of(yp, "I Nay"), Of(yp, "R Yea"), Of(yp, "R Nay")));
            bool powell = RollCall("wayback_senate_vote_115_2_00019_20201213202810.xml", out int py, out int pn, out Dictionary<string, int> pp, out string pq);
            Check(powell && py == 84 && pn == 13 && Of(pp, "R Yea") == 44 && Of(pp, "R Nay") == 4 && Of(pp, "D Yea") + Of(pp, "I Yea") == 40 && Of(pp, "D Nay") + Of(pp, "I Nay") == 9,
                F("(a) Powell, 23 Jan 2018 (115th, 2nd session, No. 19; {0}): {1}-{2}; a REP president's party {3}-{4}, the other conference {5}-{6} - its majority for. The bloc count (DECLARED) reproduces Yellen's other party, not Powell's",
                    pq ?? "?", py, pn, Of(pp, "R Yea"), Of(pp, "R Nay"), Of(pp, "D Yea") + Of(pp, "I Yea"), Of(pp, "D Nay") + Of(pp, "I Nay")));

            // ---- (b) the statutory date, with no player: the AI president's nominee confirmed on the term's day ----
            Dictionary<SimulationRandom.Stream, int> before = SimulationRandom.CaptureDrawCounts();
            SimulationManager sim = UsWorld(hosts, out Country usa);
            FedChair seeded = usa.CurrentFedChair;
            Check(seeded != null && seeded.HasTerm() && seeded.TermBegan == SimulationManager.EpochDate.Date && seeded.TermEnds == seeded.TermBegan.AddYears(4),
                F("(b) the USA's start ({0}): the Fed chair {1} seated {2} to {3} - 12 U.S.C. 242's four years", Day(SimulationManager.EpochDate), seeded?.Name ?? "NONE",
                    Day(seeded?.TermBegan ?? DateTime.MinValue), Day(seeded?.TermEnds ?? DateTime.MinValue)));
            DateTime ends = sim.CurrentDate.Date.AddDays(2);
            seeded.TermEnds = ends;
            FedChair expected = CentralBankTerms.AiNominee(CentralBankTerms.Fed, seeded, ends);
            UsFedConsent.Count friendly = UsFedConsent.Vote(usa, ends);
            Step(sim);
            bool sitsEve = ReferenceEquals(usa.CurrentFedChair, seeded);
            Step(sim);
            FedChair onDay = usa.CurrentFedChair;
            Check(sitsEve && friendly != null && friendly.Confirms && friendly.SenateOfRecord && onDay != null && !ReferenceEquals(onDay, seeded) && onDay.Name == expected.Name
                  && onDay.RateBias == expected.RateBias && onDay.TermBegan == ends && onDay.TermEnds == ends.AddYears(4),
                F("(b) the term planted to end {0}: the chair sits on the eve; on the day {1} ({2}) nominates {3} and {4} - seated to {5}",
                    Day(ends), friendly?.President ?? "NO PRESIDENT", friendly?.PresidentsParty ?? "?", onDay?.Name ?? "NONE", UsFedConsent.CountWords(friendly), Day(onDay?.TermEnds ?? DateTime.MinValue)));

            // ---- (c) a hostile Senate: the record's Senate turned over ----
            SimulationManager hsim = UsWorld(hosts, out Country husa);
            FedChair sitting = husa.CurrentFedChair;
            DateTime hends = hsim.CurrentDate.Date.AddDays(2);
            sitting.TermEnds = hends;
            Dictionary<string, int> record = WorldClock.SecondChamberOfRecord(CountryId.USA, hends);
            husa.SecondChamberSeats = Swapped(record, "DEM", "REP");
            UsFedConsent.Count hostile = UsFedConsent.Vote(husa, hends);
            Step(hsim); Step(hsim); Step(hsim);
            bool servesOn = ReferenceEquals(husa.CurrentFedChair, sitting);
            Check(hostile != null && !hostile.Confirms && !hostile.SenateOfRecord && hostile.Against > hostile.For && servesOn,
                F("(c) the record's Senate turned over (DEM {0}, REP {1}): on {2} {3} ({4}) nominates and {5}; a day later {6} still serves",
                    Of(husa.SecondChamberSeats, "DEM"), Of(husa.SecondChamberSeats, "REP"), Day(hends), hostile?.President ?? "NO PRESIDENT", hostile?.PresidentsParty ?? "?",
                    UsFedConsent.CountWords(hostile), husa.CurrentFedChair?.Name ?? "NONE"));
            husa.SecondChamberSeats = new Dictionary<string, int>();   // the AI USA's own state: no Senate seated - the record's by date counts
            FedChair hexpected = CentralBankTerms.AiNominee(CentralBankTerms.Fed, sitting, hends);
            Step(hsim);
            DateTime confirmedOn = hsim.CurrentDate.Date;
            FedChair hnow = husa.CurrentFedChair;
            Check(hnow != null && !ReferenceEquals(hnow, sitting) && hnow.Name == hexpected.Name && hnow.RateBias == hexpected.RateBias && hnow.TermBegan == confirmedOn
                  && hnow.TermEnds == confirmedOn.AddYears(4) && confirmedOn > hends,
                F("(c) the Senate of record by date again: on {0} the same nominee {1} is confirmed - {2} - and seated to {3}, four years from the confirmation",
                    Day(confirmedOn), hnow?.Name ?? "NONE", UsFedConsent.CountWords(UsFedConsent.Vote(husa, confirmedOn)), Day(hnow?.TermEnds ?? DateTime.MinValue)));
            Dictionary<SimulationRandom.Stream, int> after = SimulationRandom.CaptureDrawCounts();
            before.TryGetValue(SimulationRandom.Stream.FederalReserve, out int fedBefore);
            after.TryGetValue(SimulationRandom.Stream.FederalReserve, out int fedAfter);
            Check(fedBefore == fedAfter, F("(c) the nominations move no counted stream - the Federal Reserve stream {0} -> {1} draws", fedBefore, fedAfter));

            // ---- (d) an equal division ----
            husa.SecondChamberSeats = Even(record, "DEM", "REP");
            UsFedConsent.Count tie = UsFedConsent.Vote(husa, confirmedOn);
            Check(tie != null && tie.For == tie.Against && tie.VicePresidentParty == tie.PresidentsParty && tie.Confirms,
                F("(d) an equal division (DEM {0}, REP {1}): {2} - the Vice President's party {3}, the president's {4}", Of(husa.SecondChamberSeats, "DEM"), Of(husa.SecondChamberSeats, "REP"),
                    UsFedConsent.CountWords(tie), tie?.VicePresidentParty ?? "none", tie?.PresidentsParty ?? "?"));
            husa.SecondChamberSeats = new Dictionary<string, int>();

            // ---- (e) the player-president (the instrument's hand: the USA the player's, no party seated - it governs) ----
            SimulationManager psim = UsWorld(hosts, out Country pusa);
            psim.PlayerCountryId = CountryId.USA;
            FedChair own = pusa.CurrentFedChair;
            own.TermEnds = psim.CurrentDate.Date;
            pusa.SecondChamberSeats = WorldClock.SecondChamberOfRecord(CountryId.USA, psim.CurrentDate);
            bool holdFriendly = psim.CentralBankHeadAwaitsPlayer(CountryId.USA);
            pusa.SecondChamberSeats = Swapped(pusa.SecondChamberSeats, "DEM", "REP");
            bool holdHostile = psim.CentralBankHeadAwaitsPlayer(CountryId.USA);
            var pick = new FedChair("US-29 probe chair", FedChairPhilosophy.Dovish, "the diagnostic's probe", -1.25f);
            bool refusedOk = !psim.AppointCentralBankHead(CountryId.USA, pick, out string refused);
            Check(holdFriendly && !holdHostile && refusedOk && ReferenceEquals(pusa.CurrentFedChair, own) && refused != null && refused.Contains("THE SENATE REFUSES"),
                F("(e) the player's USA, the term ended {0}: under the record's Senate the hold rises ({1}); turned over, no hold ({2}) and the verb refuses - \"{3}\" - and {4} serves on",
                    Day(own.TermEnds), holdFriendly, holdHostile, refused ?? "NOT REFUSED", pusa.CurrentFedChair?.Name ?? "NONE"));
            pusa.SecondChamberSeats = WorldClock.SecondChamberOfRecord(CountryId.USA, psim.CurrentDate);
            bool appointed = psim.AppointCentralBankHead(CountryId.USA, pick, out string why);
            FedChair seated = pusa.CurrentFedChair;
            Check(appointed && seated != null && seated.Name == pick.Name && seated.TermBegan == psim.CurrentDate.Date && seated.TermEnds == psim.CurrentDate.Date.AddYears(4)
                  && !psim.CentralBankHeadAwaitsPlayer(CountryId.USA),
                F("(e) under the record's Senate the verb seats the pick {0} to {1} and the hold ends{2}", Day(seated?.TermBegan ?? DateTime.MinValue), Day(seated?.TermEnds ?? DateTime.MinValue),
                    appointed ? string.Empty : " - REFUSED: " + why));

            // ---- (e2) the role gate (the review's L2): the player out of the White House - no hold, the gate's words, the AI's nominee to the count ----
            SimulationManager rsim = UsWorld(hosts, out Country rusa);
            rsim.PlayerCountryId = CountryId.USA;
            bool rpresident = Congress.PresidentOn(rusa, rsim.CurrentDate, out string rshown, out string rpp, out _);
            string outParty = rpp == "REP" ? "DEM" : "REP";
            rusa.PlayerPartyAbbrev = outParty;
            FedChair rsitting = rusa.CurrentFedChair;
            DateTime rends = rsim.CurrentDate.Date;
            rsitting.TermEnds = rends;
            bool gated = !rsim.PlayerMayIntroduce(CountryId.USA, out string gateWords);
            bool rhold = rsim.CentralBankHeadAwaitsPlayer(CountryId.USA);
            bool rrefused = !rsim.AppointCentralBankHead(CountryId.USA, pick, out string rwhy);
            Check(rpresident && gated && !rhold && rrefused && rwhy != null && rwhy == gateWords && !rwhy.Contains("THE SENATE REFUSES") && !rwhy.Contains("THE SENATE CONFIRMS")
                  && ReferenceEquals(rusa.CurrentFedChair, rsitting),
                F("(e2) the player's USA with {0} seated (the president {1}, {2}), the term ended {3}: no hold ({4}); the verb refuses in the role gate's words - \"{5}\" - and {6} serves on",
                    outParty, rshown ?? "NONE", rpp ?? "?", Day(rends), rhold, rwhy ?? "NOT REFUSED", rusa.CurrentFedChair?.Name ?? "NONE"));
            FedChair rexpected = CentralBankTerms.AiNominee(CentralBankTerms.Fed, rsitting, rends);
            Step(rsim);
            DateTime rday = rsim.CurrentDate.Date;
            FedChair rnow = rusa.CurrentFedChair;
            UsFedConsent.Count rcount = UsFedConsent.Vote(rusa, rday);
            Check(rcount != null && rcount.Confirms && rnow != null && !ReferenceEquals(rnow, rsitting) && rnow.Name == rexpected.Name && rnow.RateBias == rexpected.RateBias
                  && rnow.TermBegan == rday && rnow.TermEnds == rday.AddYears(4),
                F("(e2) the next day ({0}) the AI president's keyed nominee {1} goes to the count - {2} - and is seated to {3}",
                    Day(rday), rnow?.Name ?? "NONE", UsFedConsent.CountWords(rcount), Day(rnow?.TermEnds ?? DateTime.MinValue)));

            // ---- (f) CHECKED (the review's M4): the AI USA's chair on every term day of the century, in every start's world with no player ----
            foreach (CountryId start in (CountryId[])Enum.GetValues(typeof(CountryId)))
            {
                using (SimulationManager.EpochScope())
                {
                    WorldClock.ApplyStart(start);
                    SimulationRandom.Seed(Seed);
                    World w = WorldFactory.CreateDefault();
                    Country u = w.GetCountry(CountryId.USA);
                    FedChair head = u.CurrentFedChair;
                    DateTime horizon = SimulationManager.EpochDate.Date.AddYears(NoPolicyCentury.Years);
                    int termDays = 0;
                    DateTime first = head?.TermEnds ?? DateTime.MinValue, last = first;
                    UsFedConsent.Count firstCount = null, refusal = null;
                    DateTime refusedOn = DateTime.MinValue;
                    for (DateTime day = first; head != null && day <= horizon; day = CentralBankTerms.TermEnds(CentralBankTerms.Fed, day))
                    {
                        UsFedConsent.Count c = UsFedConsent.Vote(u, day);
                        termDays++;
                        last = day;
                        if (firstCount == null) { firstCount = c; }
                        if (c == null || !c.Confirms) { refusal = c; refusedOn = day; break; }
                    }
                    bool confirmed = head != null && termDays > 0 && refusedOn == DateTime.MinValue;
                    Check(confirmed,
                        F("(f) {0}'s start {1}, no player: the AI USA's Fed chair on {2} term day(s) from {3} to {4} (the statute's four years, to the century's end {5}) - {6}",
                            start, Day(SimulationManager.EpochDate), termDays, Day(first), Day(last), Day(horizon),
                            confirmed ? "every count confirms (the first: " + (firstCount?.President ?? "?") + ", " + (firstCount?.PresidentsParty ?? "?") + ", " + UsFedConsent.CountWords(firstCount) + "): the AF-7 pick on every AF-7 day, this start's trajectory does not move by US-29"
                            : head == null ? "NO FED CHAIR SEATED"
                            : "REFUSED on " + Day(refusedOn) + " (" + UsFedConsent.CountWords(refusal) + "): the chair serves on and this start's trajectory moves from that day"));
                }
            }
        }

        /// <summary>A default world on the USA's start, the check's seed, with no player: the AI president nominates.</summary>
        private static SimulationManager UsWorld(List<GameObject> hosts, out Country usa)
        {
            WorldClock.ApplyStart(CountryId.USA);
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("FedChairConsentDiagnostic"); hosts.Add(go);
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            usa = world.GetCountry(CountryId.USA);
            return sim;
        }

        private static readonly Dictionary<CountryId, PolicyDecision> NoDecisions = new Dictionary<CountryId, PolicyDecision>();

        private static void Step(SimulationManager sim)
        {
            if (sim.AdvanceDay()) { sim.AdvanceTurn(NoDecisions); }
        }

        /// <summary>The seats with two parties' counts exchanged - the record's Senate turned over.</summary>
        private static Dictionary<string, int> Swapped(IReadOnlyDictionary<string, int> seats, string a, string b)
        {
            var d = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> kv in seats) { d[kv.Key] = kv.Key == a ? Of(seats, b) : kv.Key == b ? Of(seats, a) : kv.Value; }
            return d;
        }

        /// <summary>The seats with two parties' sum split evenly between them (the remainder to neither, left vacant).</summary>
        private static Dictionary<string, int> Even(IReadOnlyDictionary<string, int> seats, string a, string b)
        {
            int half = (Of(seats, a) + Of(seats, b)) / 2;
            var d = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> kv in seats) { d[kv.Key] = kv.Key == a || kv.Key == b ? half : kv.Value; }
            return d;
        }

        private static int Of(IReadOnlyDictionary<string, int> d, string key) => d != null && d.TryGetValue(key, out int n) ? n : 0;

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
