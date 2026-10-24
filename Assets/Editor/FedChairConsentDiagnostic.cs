using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-29 (`docs/specs/USA_STAGE_PLAN.md`; R-US12 (a), ruled F8) with Elias's rulings K15-K17 of 2026-10-23: <b>THE FED CHAIR BY STATUTE</b> - the
    /// term AF-7 built (`CentralBankTerms.Fed`, held by `CentralBankTermCheck`), the President's nominee and the Senate's consent by a majority
    /// (`UsFedConsent`), the Vice Chair acting while the term has ended unfilled (K15), the nomination before the Senate and its refusal (K16), and the
    /// crossing count (K17). Each claim fails while false:
    /// <list type="bullet">
    /// <item>(a) the sources - 12 U.S.C. 242's consent words and its 4-year term, the Senate's precedent of 21 Nov 2013, and the two confirmations US-29
    /// read (Yellen 2014, Powell 2018), each page to its SHA-256, each tally off the Senate's own XML;</item>
    /// <item>(a2) K15's sources - 12 U.S.C. 242's Vice Chairman "who shall serve in the absence of the Chairman", 12 U.S.C. 244's words, and the Board's two
    /// releases of 2022 (the chair pro tempore; the Vice Chair's resignation before it);</item>
    /// <item>(k) K17's rule re-derived from the saved roll calls - the pooled in-party share and the crossing line through the four votes of the
    /// majority-cloture era, each constant in `UsFedConsent` held to it; the rule reproducing Yellen 2014's other party (against) and Powell 2018's (for);
    /// Voteview and the Senate agreeing on every tally both hold; the six earlier roll calls read out of sample; every outcome of record (a confirmation)
    /// reproduced, where the bloc count misses the divided Senates; and, measured, every Senate the game can seat confirming under the rule;</item>
    /// <item>(l) K16's vote day re-derived - the median of the days from the Senate receiving each nomination (congress.gov) to its vote (the Senate's XML);</item>
    /// <item>(b) the statutory date - the USA's start seats its chair for four years and its Vice Chair (K15) with no lean; with no player the AI president's
    /// nominee is seated on the term's day and not a day before;</item>
    /// <item>(c) a hostile Senate - the record's Senate turned over: the crossing count confirms the AI's nominee on the term's day where the bloc count
    /// would have refused; no counted stream moves;</item>
    /// <item>(d) an equal division - the Vice President of the president's party decides one;</item>
    /// <item>(e) the player-president (K16) - the hold rises under the record's Senate and under the turned-over one; the verb lodges the nominee with its vote
    /// day the record's lag later, the chair's seat unfilled and the hold down while it pends; the Vice Chair acts (K15) and the rate rule reads the Vice
    /// Chair's lean; on the vote day the Senate confirms and seats the nominee for four years; a refusal (planted - the rule refuses in no seated Senate) is
    /// recorded, leaves the Vice Chair acting and raises the hold again;</item>
    /// <item>(e2) the role gate (the review of 2026-10-22, L2): a US player who is not the president gets no hold, the verb refuses in the role gate's words,
    /// and the AI president's keyed nominee goes to the count and is seated;</item>
    /// <item>(f) a CHECK, not a measurement (the review's M4): in every start's world with no player - the trajectory dump's kind - every chair term day of
    /// the century is counted on the unstepped world (the government at creation, the Senate of record at its reach), and every count must confirm under the
    /// crossing count AND under the bloc count it replaces (the K15-K17 review's M-1: the baseline was cut under the bloc count, and the two builds part only
    /// where the bloc would have refused); a term day either refuses fails here by name. It reads no stepped trajectory - the dump diff is the proof.</item>
    /// </list>
    /// </summary>
    public static class FedChairConsentDiagnostic
    {
        private const int Seed = 777;
        private const string FedFolder = "ElectionsData/usa/raw/fed/";

        /// <summary>K17's roll calls: who, Voteview's file, the Senate's XML (null before 1989), congress.gov's nomination record (null where none was fetched),
        /// and whether the vote falls under the majority-cloture precedent of 21 Nov 2013 (the rule's reading).</summary>
        private static readonly (string Who, string Voteview, string Senate, string Nomination, bool Era)[] Rolls =
        {
            ("Volcker 1979", "voteview_RS0960246.json", null, null, false),
            ("Volcker 1983", "voteview_RS0980218.json", null, null, false),
            ("Greenspan 1987", "voteview_RS1000225.json", null, null, false),
            ("Greenspan 1996", "voteview_RS1040778.json", "wayback_senate_vote_104_2_00165_20250515144911.xml", "congressgov_api_nomination_104_PN951.json", false),
            ("Greenspan 2000", "voteview_RS1060380.json", "wayback_senate_vote_106_2_00006_20250523084300.xml", "congressgov_api_nomination_106_PN729.json", false),
            ("Bernanke 2010", "voteview_RS1110413.json", "wayback_senate_vote_111_2_00016_20260613095409.xml", "congressgov_api_nomination_111_PN959.json", false),
            ("Yellen 2014", "voteview_RS1130292.json", "wayback_senate_vote_113_2_00001_20201214112740.xml", "congressgov_api_nomination_113_PN921.json", true),
            ("Powell 2018", "voteview_RS1150344.json", "wayback_senate_vote_115_2_00019_20201213202810.xml", "congressgov_api_nomination_115_PN1353.json", true),
            ("Powell 2022", "voteview_RS1170704.json", "wayback_senate_vote_117_2_00176_20251112233424.xml", "congressgov_api_nomination_117_PN1522.json", true),
            ("Warsh 2026", "voteview_RS1190779.json", "wayback_senate_vote_119_2_00120_20260513205047.xml", "congressgov_api_nomination_119_PN855.json", true),
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== FedChairConsentDiagnostic (PS-6 US-29, K15-K17): the Fed chair's statutory term, the nominee before the Senate, the Vice Chair acting, the crossing count ===\n");
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
            sb.Append(failures == 0 ? "=== FedChairConsentDiagnostic: the chair's term is the statute's, the Vice Chair acts while it is unfilled, and the Senate votes by the crossing count ===" : F("=== FedChairConsentDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static string Words(string html)
        {
            string s = Regex.Replace(html ?? string.Empty, "<[^>]*>", " ");
            s = s.Replace("&nbsp;", " ").Replace("&#160;", " ").Replace("&amp;", "&").Replace("&quot;", "\"");
            return Regex.Replace(s, @"\s+", " ");
        }

        /// <summary>One roll call's XML (the Senate's): the tally it prints, its members' votes counted by party - "D Yea", "R Nay", ... - and its question.</summary>
        private static bool RollCall(string file, out int yeas, out int nays, out Dictionary<string, int> byParty, out string question)
        {
            yeas = nays = -1; byParty = new Dictionary<string, int>(); question = null;
            if (!PresidencyDiagnostic.Page(FedFolder + file, out string xml)) { return false; }
            Match y = Regex.Match(xml, @"<count>\s*<yeas>(\d+)</yeas>\s*<nays>(\d+)</nays>");
            if (y.Success) { yeas = int.Parse(y.Groups[1].Value, CultureInfo.InvariantCulture); nays = int.Parse(y.Groups[2].Value, CultureInfo.InvariantCulture); }
            Match q = Regex.Match(xml, @"<vote_document_text>([^<]*)</vote_document_text>");
            question = q.Success ? q.Groups[1].Value.Trim() : null;
            foreach (Match m in Regex.Matches(xml, @"<member>.*?<party>(\w+)</party>.*?<vote_cast>([^<]*)</vote_cast>.*?</member>", RegexOptions.Singleline))
            {
                string key = m.Groups[1].Value + " " + m.Groups[2].Value.Trim();
                byParty[key] = byParty.TryGetValue(key, out int n) ? n + 1 : 1;
            }
            return yeas >= 0;
        }

        /// <summary>One roll call split by caucus - the president's (in) and the rest (out): yeas, nays and seats (every member listed, voting or not).</summary>
        private sealed class Split
        {
            public int InYea, InNay, InSeats, OutYea, OutNay, OutSeats, Yeas, Nays;
            public DateTime Day;
            public int Margin => InSeats - OutSeats;
            public double Crossing => OutYea + OutNay > 0 ? (double)OutYea / (OutYea + OutNay) : 0.0;
        }

        /// <summary>The Senate's XML split by caucus: every independent with the Democrats (DECLARED: each independent on these roll calls caucused with them).</summary>
        private static Split SenateSplit(string file, bool presidentRepublican)
        {
            if (!PresidencyDiagnostic.Page(FedFolder + file, out string xml)) { return null; }
            var s = new Split();
            Match d = Regex.Match(xml, @"<vote_date>\s*([A-Za-z]+ \d{1,2}, \d{4})");
            if (!d.Success || !DateTime.TryParseExact(d.Groups[1].Value, "MMMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out s.Day)) { return null; }
            foreach (Match m in Regex.Matches(xml, @"<member>.*?<party>(\w+)</party>.*?<vote_cast>([^<]*)</vote_cast>.*?</member>", RegexOptions.Singleline))
            {
                bool inCaucus = (m.Groups[1].Value == "R") == presidentRepublican;
                string cast = m.Groups[2].Value.Trim();
                if (inCaucus) { s.InSeats++; if (cast == "Yea") { s.InYea++; } else if (cast == "Nay") { s.InNay++; } }
                else { s.OutSeats++; if (cast == "Yea") { s.OutYea++; } else if (cast == "Nay") { s.OutNay++; } }
            }
            Match y = Regex.Match(xml, @"<count>\s*<yeas>(\d+)</yeas>\s*<nays>(\d+)</nays>");
            if (y.Success) { s.Yeas = int.Parse(y.Groups[1].Value, CultureInfo.InvariantCulture); s.Nays = int.Parse(y.Groups[2].Value, CultureInfo.InvariantCulture); }
            return s;
        }

        /// <summary>Voteview's record split by caucus (party code 200 Republican; 100 Democratic, 328 independent with them): yeas are cast code 1, nays code 6
        /// (the recorded tally - announced and paired positions are not votes); seats count every member listed but the president's row. The president's party
        /// code is the POTUS row's, 0 where the record carries none.</summary>
        private static Split VoteviewSplit(string file, out int presidentCode, int presidentFallback)
        {
            presidentCode = 0;
            if (!PresidencyDiagnostic.Page(FedFolder + file, out string json)) { return null; }
            var members = Regex.Matches(json, "\\{[^{}]*\"icpsr\": \\d+[^{}]*\\}").Cast<Match>().Select(m => m.Value).ToList();   // a member's keys in any order (Voteview's records from the 115th Congress open with "source")
            string potus = members.FirstOrDefault(m => m.Contains("\"district\": \"POTUS\""));
            if (potus != null) { Match pc = Regex.Match(potus, "\"party_code\": (\\d+)"); if (pc.Success) { presidentCode = int.Parse(pc.Groups[1].Value, CultureInfo.InvariantCulture); } }
            int pres = presidentCode != 0 ? presidentCode : presidentFallback;
            var s = new Split();
            Match d = Regex.Match(json, "\"date\": \"(\\d{4}-\\d{2}-\\d{2})\"");
            if (d.Success) { DateTime.TryParseExact(d.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out s.Day); }
            if (pres == 0) { return d.Success ? s : null; }   // no POTUS row and no fallback yet: the vote's day alone, for the president of record by date
            bool InCaucus(int code) => (code == 200) == (pres == 200);
            foreach (string m in members)
            {
                if (m.Contains("\"district\": \"POTUS\"")) { continue; }
                Match pc = Regex.Match(m, "\"party_code\": (\\d+)");
                if (!pc.Success) { continue; }
                if (InCaucus(int.Parse(pc.Groups[1].Value, CultureInfo.InvariantCulture))) { s.InSeats++; } else { s.OutSeats++; }
            }
            Match block = Regex.Match(json, "\"party_vote_counts\": \\{((?:\\s*\"\\d+\": \\{[^}]*\\},?)*)\\s*\\}");
            if (!block.Success) { return null; }
            foreach (Match p in Regex.Matches(block.Groups[1].Value, "\"(\\d+)\": \\{([^}]*)\\}"))
            {
                bool inCaucus = InCaucus(int.Parse(p.Groups[1].Value, CultureInfo.InvariantCulture));
                foreach (Match c in Regex.Matches(p.Groups[2].Value, "\"(\\d+)\": (\\d+)"))
                {
                    int code = int.Parse(c.Groups[1].Value, CultureInfo.InvariantCulture), n = int.Parse(c.Groups[2].Value, CultureInfo.InvariantCulture);
                    if (code == 1) { if (inCaucus) { s.InYea += n; } else { s.OutYea += n; } s.Yeas += n; }
                    else if (code == 6) { if (inCaucus) { s.InNay += n; } else { s.OutNay += n; } s.Nays += n; }
                }
            }
            return s;
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
                F("(a) Powell, 23 Jan 2018 (115th, 2nd session, No. 19; {0}): {1}-{2}; a REP president's party {3}-{4}, the other conference {5}-{6} - its majority for",
                    pq ?? "?", py, pn, Of(pp, "R Yea"), Of(pp, "R Nay"), Of(pp, "D Yea") + Of(pp, "I Yea"), Of(pp, "D Nay") + Of(pp, "I Nay")));

            // ---- (a2) K15's sources ----
            bool s244 = PresidencyDiagnostic.Page(FedFolder + "us_12usc244_govinfo2024.htm", out string s244Html);
            string s244Text = Words(s244Html);
            Check(s244 && s244Text.Contains("At meetings of the Board the chairman shall preside, and, in his absence, the vice chairman shall preside. In the absence of the chairman and the vice chairman, the Board shall elect a member to act as chairman pro tempore.")
                  && statuteText.Contains("1 of whom shall serve in the absence of the Chairman, as provided in section 244 of this title"),
                "(a2) 12 U.S.C. 244 held to its SHA-256: the vice chairman presides in the chairman's absence, the Board electing a member chairman pro tempore in the absence of both; and 12 U.S.C. 242's Vice Chairman \"shall serve in the absence of the Chairman, as provided in section 244\"");
            bool pro = PresidencyDiagnostic.Page(FedFolder + "federalreserve_pressrelease_other20220204a.htm", out string proHtml);
            bool clar = PresidencyDiagnostic.Page(FedFolder + "federalreserve_pressrelease_other20220110a.htm", out string clarHtml);
            string proText = Words(proHtml), clarText = Words(clarHtml);
            Check(pro && clar && proText.Contains("The Federal Reserve Board on Friday named Jerome H. Powell as Chair Pro Tempore, pending Senate confirmation to a second term as Chair of the Board of Governors.")
                  && proText.Contains("The action, effective February 5,")
                  && clarText.Contains("Richard H. Clarida announces his intention to resign from the Board of Governors of the Federal Reserve System on January 14, 2022"),
                "(a2) the Board's releases held to their SHA-256s: the outgoing chair named Chair Pro Tempore from 5 Feb 2022 \"pending Senate confirmation\", the Vice Chair having resigned on 14 Jan 2022 - 244's election in the absence of both, not a Vice Chair acting (UsFedConsent's class note says so)");

            // ---- (k) K17: the rule re-derived from the roll calls ----
            SimulationManager wsim = UsWorld(hosts, out Country wusa);
            var splits = new Split[Rolls.Length];
            var senate = new Split[Rolls.Length];
            int fitIn = 0, fitInVoting = 0;
            var xs = new List<double>(); var ys = new List<double>();
            for (int i = 0; i < Rolls.Length; i++)
            {
                (string who, string vv, string sx, string _, bool era) = Rolls[i];
                int fallback = 0;
                Split probe = VoteviewSplit(vv, out int potus, 0);
                DateTime vday = probe?.Day ?? DateTime.MinValue;
                if (potus == 0 && vday > DateTime.MinValue && Congress.PresidentOn(wusa, vday, out _, out string recordParty, out _)) { fallback = recordParty == "REP" ? 200 : recordParty == "DEM" ? 100 : 0; }
                splits[i] = potus != 0 ? probe : VoteviewSplit(vv, out _, fallback);
                int pres = potus != 0 ? potus : fallback;
                senate[i] = sx == null || pres == 0 ? null : SenateSplit(sx, pres == 200);
                Check(splits[i] != null && pres != 0 && (sx == null || (senate[i] != null && senate[i].Yeas == splits[i].Yeas && senate[i].Nays == splits[i].Nays)),
                    F("(k) {0}: Voteview's record held to its SHA-256, the president's party {1}{2}; {3}-{4}{5}", who, pres == 200 ? "REP" : pres == 100 ? "DEM" : "NONE",
                        potus != 0 ? " (its POTUS row)" : " (the president of record by date)", splits[i]?.Yeas ?? -1, splits[i]?.Nays ?? -1,
                        sx == null ? " - before the Senate's lists" : senate[i] == null ? " - THE SENATE'S XML UNREAD" : F(" - the Senate's XML {0}-{1}", senate[i].Yeas, senate[i].Nays)));
                if (era && senate[i] != null)
                {
                    fitIn += senate[i].InYea; fitInVoting += senate[i].InYea + senate[i].InNay;
                    xs.Add(senate[i].Margin); ys.Add(senate[i].Crossing);
                }
            }
            double mx = xs.Count > 0 ? xs.Average() : 0, my = ys.Count > 0 ? ys.Average() : 0;
            double sxx = xs.Sum(x => (x - mx) * (x - mx)), sxy = xs.Select((x, i) => (x - mx) * (ys[i] - my)).Sum();
            double slope = sxx > 0 ? sxy / sxx : double.NaN, intercept = my - slope * mx;
            Check(xs.Count == 4 && fitIn == UsFedConsent.InPartyYeas && fitInVoting == UsFedConsent.InPartyVoting
                  && Math.Abs(slope - UsFedConsent.CrossingPerSeatOfMargin) < 5e-6 && Math.Abs(intercept - UsFedConsent.CrossingAtEvenSenate) < 5e-6,
                F("(k) the rule's reading re-derived on the {0} roll calls since 21 Nov 2013: the president's caucus {1} of {2} (UsFedConsent {3} of {4}); the crossing line {5:0.000000} {6:+0.000000;-0.000000} per seat of margin (UsFedConsent {7:0.000000} {8:+0.000000;-0.000000})",
                    xs.Count, fitIn, fitInVoting, UsFedConsent.InPartyYeas, UsFedConsent.InPartyVoting, intercept, slope, UsFedConsent.CrossingAtEvenSenate, UsFedConsent.CrossingPerSeatOfMargin));
            for (int i = 0; i < Rolls.Length; i++)
            {
                Split s = Rolls[i].Era ? senate[i] : splits[i];
                if (s == null) { continue; }
                double predicted = UsFedConsent.CrossingShare(s.Margin);
                int ruleYeas = UsFedConsent.Yeas(s.InSeats, s.OutSeats), sworn = s.InSeats + s.OutSeats;
                bool ruleConfirms = UsSenate.Carries(ruleYeas, sworn - ruleYeas, false), blocConfirms = s.InSeats >= s.OutSeats;   // an equal division decided by the Vice President of the president's party
                bool sameSide = (predicted > 0.5) == (s.Crossing > 0.5);
                string line = F("{0} ({1}): the president's caucus {2} seats to {3} (margin {4:+0;-0;0}); the other caucus crossed {5}-{6} ({7:0.000}), the rule reads {8:0.000} - {9}; the rule counts {10}-{11}, the record {12}-{13} (confirmed); the bloc count {14}",
                    Rolls[i].Who, Rolls[i].Era ? "in the reading" : "out of sample", s.InSeats, s.OutSeats, s.Margin, s.OutYea, s.OutNay, s.Crossing, predicted,
                    sameSide ? "the same side of half" : "THE OTHER SIDE OF HALF", ruleYeas, sworn - ruleYeas, s.Yeas, s.Nays, blocConfirms ? "confirms" : "REFUSES");
                if (Rolls[i].Who == "Yellen 2014") { Check(ruleConfirms && sameSide && predicted < 0.5, "(k) Yellen 2014 reproduced - the other party against: " + line); }
                else if (Rolls[i].Who == "Powell 2018") { Check(ruleConfirms && sameSide && predicted > 0.5, "(k) Powell 2018 reproduced - the other party for: " + line); }
                else { Check(ruleConfirms, "(k) " + line); }
            }
            int blocMisses = Enumerable.Range(0, Rolls.Length).Count(i => (Rolls[i].Era ? senate[i] : splits[i]) is Split b && b.InSeats < b.OutSeats);
            Check(blocMisses > 0, F("(k) every outcome of record is a confirmation and the crossing count reproduces each; the bloc count it replaces misses {0} of {1} (the divided Senates)", blocMisses, Rolls.Length));
            int closest = int.MaxValue, closestOwn = -1;
            bool allConfirm = true;
            for (int seats = 0; seats <= 100; seats++)
            {
                int y = UsFedConsent.Yeas(seats, 100 - seats);
                if (!UsSenate.Carries(y, 100 - y, false)) { allConfirm = false; }
                if (y - (100 - y) < closest) { closest = y - (100 - y); closestOwn = seats; }
            }
            Check(allConfirm, F("(k) MEASURED: under the rule every Senate of 100 confirms the president's nominee, whatever the president's caucus holds (0 to 100 seats) - the narrowest at {0} seats, by {1}; a refusal (K16) is reached only where no count can be held, and the film and (e) plant one", closestOwn, closest));

            // ---- (l) K16: the vote day's lag re-derived ----
            var lags = new List<int>();
            foreach ((string who, string _, string _, string nominationFile, bool _) in Rolls)
            {
                if (nominationFile == null) { continue; }
                int idx = Array.FindIndex(Rolls, r => r.Who == who);
                bool page = PresidencyDiagnostic.Page(FedFolder + nominationFile, out string json);
                Match rd = Regex.Match(json ?? string.Empty, "\"receivedDate\": \"(\\d{4}-\\d{2}-\\d{2})\"");
                DateTime received = DateTime.MinValue;
                bool ok = page && rd.Success && DateTime.TryParseExact(rd.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out received) && senate[idx] != null;
                if (ok) { lags.Add((int)(senate[idx].Day - received).TotalDays); }
                Check(ok, F("(l) {0}: received {1} (congress.gov), voted {2} (the Senate's XML) - {3} days", who, ok ? Day(received) : "?", senate[idx] != null ? Day(senate[idx].Day) : "?", ok ? (senate[idx].Day - received).TotalDays : -1));
            }
            lags.Sort();
            int median = lags.Count % 2 == 1 ? lags[lags.Count / 2] : -1;
            Check(lags.Count == 7 && median == UsFedConsent.NominationToVoteDays,
                F("(l) the median of the {0} lags, {1} days ({2}), is UsFedConsent's NominationToVoteDays ({3})", lags.Count, median, string.Join(", ", lags), UsFedConsent.NominationToVoteDays));

            // ---- (b) the statutory date, with no player: the AI president's nominee confirmed on the term's day ----
            Dictionary<SimulationRandom.Stream, int> before = SimulationRandom.CaptureDrawCounts();
            SimulationManager sim = UsWorld(hosts, out Country usa);
            FedChair seeded = usa.CurrentFedChair;
            Check(seeded != null && seeded.HasTerm() && seeded.TermBegan == SimulationManager.EpochDate.Date && seeded.TermEnds == seeded.TermBegan.AddYears(4),
                F("(b) the USA's start ({0}): the Fed chair {1} seated {2} to {3} - 12 U.S.C. 242's four years", Day(SimulationManager.EpochDate), seeded?.Name ?? "NONE",
                    Day(seeded?.TermBegan ?? DateTime.MinValue), Day(seeded?.TermEnds ?? DateTime.MinValue)));
            Country sweden = sim.World.GetCountry(CountryId.Sweden);
            Check(usa.FedViceChair != null && usa.FedViceChair.RateBias == 0f && !usa.FedViceChair.HasTerm() && !usa.FedViceChairActing && usa.FedNominationPending == null
                  && (sweden == null || sweden.FedViceChair == null),
                F("(b) K15: the USA's start seats a Vice Chair, {0}, at lean {1} with no term of its own, not acting; no other bank has one", usa.FedViceChair?.Name ?? "NONE", usa.FedViceChair?.RateBias ?? float.NaN));
            Check(usa.FedViceChair != null && usa.FedViceChair.RateBias == seeded.RateBias,
                F("(b) the K15-K17 review's L-4: the Vice Chair's lean ({0}) is the seeded chair's ({1}) - NoPolicyCentury's USA, whose term ends unfilled for the century, reads the one where the baseline read the other, so a change to either seed moves it",
                    usa.FedViceChair?.RateBias ?? float.NaN, seeded.RateBias));
            DateTime ends = sim.CurrentDate.Date.AddDays(2);
            seeded.TermEnds = ends;
            FedChair expected = CentralBankTerms.AiNominee(CentralBankTerms.Fed, seeded, ends);
            UsFedConsent.Count friendly = UsFedConsent.Vote(usa, ends);
            Step(sim);
            bool sitsEve = ReferenceEquals(usa.CurrentFedChair, seeded);
            Step(sim);
            FedChair onDay = usa.CurrentFedChair;
            Check(sitsEve && friendly != null && friendly.Confirms && friendly.SenateOfRecord && onDay != null && !ReferenceEquals(onDay, seeded) && onDay.Name == expected.Name
                  && onDay.RateBias == expected.RateBias && onDay.TermBegan == ends && onDay.TermEnds == ends.AddYears(4) && !usa.FedViceChairActing,
                F("(b) the term planted to end {0}: the chair sits on the eve; on the day {1} ({2}) nominates {3} and {4} - seated to {5}, the Vice Chair not acting",
                    Day(ends), friendly?.President ?? "NO PRESIDENT", friendly?.PresidentsParty ?? "?", onDay?.Name ?? "NONE", UsFedConsent.CountWords(friendly), Day(onDay?.TermEnds ?? DateTime.MinValue)));

            // ---- (c) a hostile Senate: the record's Senate turned over ----
            SimulationManager hsim = UsWorld(hosts, out Country husa);
            FedChair sitting = husa.CurrentFedChair;
            DateTime hends = hsim.CurrentDate.Date.AddDays(2);
            sitting.TermEnds = hends;
            Dictionary<string, int> record = WorldClock.SecondChamberOfRecord(CountryId.USA, hends);
            husa.SecondChamberSeats = Swapped(record, "DEM", "REP");
            UsFedConsent.Count hostile = UsFedConsent.Vote(husa, hends);
            FedChair hexpected = CentralBankTerms.AiNominee(CentralBankTerms.Fed, sitting, hends);
            Step(hsim); Step(hsim);
            FedChair hnow = husa.CurrentFedChair;
            Check(hostile != null && hostile.Confirms && !hostile.SenateOfRecord && hostile.OwnSeats < hostile.OtherSeats && hnow != null && !ReferenceEquals(hnow, sitting)
                  && hnow.Name == hexpected.Name && hnow.TermBegan == hends && !husa.FedViceChairActing,
                F("(c) the record's Senate turned over (DEM {0}, REP {1}): on {2} {3} ({4}) nominates and {5} - the other caucus crossing at {6:0.000}, where the bloc count would refuse {7}-{8}; {9} seated on the term's day",
                    Of(husa.SecondChamberSeats, "DEM"), Of(husa.SecondChamberSeats, "REP"), Day(hends), hostile?.President ?? "NO PRESIDENT", hostile?.PresidentsParty ?? "?",
                    UsFedConsent.CountWords(hostile), hostile?.Crossing ?? double.NaN, hostile?.OwnSeats ?? -1, hostile?.OtherSeats ?? -1, hnow?.Name ?? "NONE"));
            husa.SecondChamberSeats = new Dictionary<string, int>();   // the AI USA's own state: no Senate seated - the record's by date counts
            Dictionary<SimulationRandom.Stream, int> after = SimulationRandom.CaptureDrawCounts();
            before.TryGetValue(SimulationRandom.Stream.FederalReserve, out int fedBefore);
            after.TryGetValue(SimulationRandom.Stream.FederalReserve, out int fedAfter);
            Check(fedBefore == fedAfter, F("(c) the nominations move no counted stream - the Federal Reserve stream {0} -> {1} draws", fedBefore, fedAfter));

            // ---- (d) an equal division ----
            Check(UsSenate.Carries(50, 50, true) && !UsSenate.Carries(50, 50, false),
                F("(d) an equal division of the votes is decided by the Vice President of the president's party (an even Senate of seats counts {0}-{1} under the rule, no equal division)",
                    UsFedConsent.Yeas(50, 50), 100 - UsFedConsent.Yeas(50, 50)));

            // ---- (e) the player-president (the instrument's hand: the USA the player's, no party seated - it governs) ----
            SimulationManager psim = UsWorld(hosts, out Country pusa);
            psim.PlayerCountryId = CountryId.USA;
            FedChair own = pusa.CurrentFedChair;
            own.TermEnds = psim.CurrentDate.Date;
            pusa.SecondChamberSeats = WorldClock.SecondChamberOfRecord(CountryId.USA, psim.CurrentDate);
            bool holdFriendly = psim.CentralBankHeadAwaitsPlayer(CountryId.USA);
            pusa.SecondChamberSeats = Swapped(pusa.SecondChamberSeats, "DEM", "REP");
            bool holdHostile = psim.CentralBankHeadAwaitsPlayer(CountryId.USA);
            var pick = new FedChair("K16 probe chair", FedChairPhilosophy.Dovish, "the diagnostic's probe", -1.25f);
            DateTime lodgedOn = psim.CurrentDate.Date;
            bool lodged = psim.AppointCentralBankHead(CountryId.USA, pick, out string lodgeWhy);
            FedNomination nomination = pusa.FedNominationPending;
            Check(holdFriendly && holdHostile && lodged && nomination != null && nomination.NominatedOn == lodgedOn && nomination.VoteOn == lodgedOn.AddDays(UsFedConsent.NominationToVoteDays)
                  && ReferenceEquals(pusa.CurrentFedChair, own) && !psim.CentralBankHeadAwaitsPlayer(CountryId.USA),
                F("(e) K16: the player's USA, the term ended {0}: the hold rises under the record's Senate ({1}) and turned over ({2}); the verb lodges {3} {4} - the vote on {5}, {6} days on - the seat unfilled and the hold down while it pends",
                    Day(own.TermEnds), holdFriendly, holdHostile, nomination?.Nominee?.Name ?? "NOTHING", lodged ? string.Empty : "(REFUSED: " + lodgeWhy + ")",
                    Day(nomination?.VoteOn ?? DateTime.MinValue), UsFedConsent.NominationToVoteDays));
            ProbeViceChair(pusa);
            float rateBefore = pusa.CurrencyZone.InterestRate;
            Step(psim);
            float rule = TaylorRule.GetSuggestedInterestRate(pusa);
            float target = Mathf.Clamp(rule + pusa.FedViceChair.RateBias, CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
            float moved = pusa.CurrencyZone.InterestRate;
            FederalReserveSystem.ApplyFedChairInterestRate(pusa);
            float byVice = moved + (target - moved) * FederalReserveSystem.RateAdjustmentSpeed;
            Check(pusa.FedViceChairActing && ReferenceEquals(FederalReserveSystem.PolicyHead(pusa), pusa.FedViceChair) && UsFedConsent.ActingWords(pusa, psim.CurrentDate) != null
                  && Mathf.Abs(pusa.CurrencyZone.InterestRate - byVice) < 1e-5f,
                F("(e) K15: the next day the Vice Chair acts - \"{0}\" - and the rate rule reads the Vice Chair's lean ({1:+0.00;-0.00;0.00}, the probe's), the rate moving {2:0.0000} -> {3:0.0000} toward {4:0.0000}",
                    UsFedConsent.ActingWords(pusa, psim.CurrentDate) ?? "NO ONE ACTS", pusa.FedViceChair.RateBias, moved, pusa.CurrencyZone.InterestRate, target));
            pusa.CurrencyZone.InterestRate = rateBefore;
            nomination.VoteOn = psim.CurrentDate.Date.AddDays(1);   // the vote brought forward to the next day, so the instrument does not step the lag
            UsFedConsent.Count onVote = UsFedConsent.Vote(pusa, nomination.VoteOn, nomination.NominatingParty);
            // the K15-K17 review's L-5: a save cut inside the window, loaded INTO THE GAME (§557), carries the nomination and the flag and votes on its day as this run does
            bool carried = false; string adoptError = null; Country g = null;
            SimulationRandom.LiveState live = SimulationRandom.CaptureLive();
            try
            {
                SaveGame cut = SaveGameService.Deserialize(SaveGameService.Serialize(SaveGameService.CreateSaveGame(psim, psim.World, CountryId.USA, null)));
                (SimulationManager gsim, PoliSim.UI.GameController _) = PresidencyDiagnostic.Adopt(cut, hosts, out adoptError);
                g = gsim?.World.GetCountry(CountryId.USA);
                FedNomination gn = g?.FedNominationPending;
                carried = gn != null && gn.Nominee?.Name == pick.Name && gn.NominatingParty == nomination.NominatingParty && gn.NominatedOn == nomination.NominatedOn && gn.VoteOn == nomination.VoteOn
                          && g.FedViceChairActing == pusa.FedViceChairActing && g.FedViceChair?.Name == pusa.FedViceChair.Name && g.FedViceChair.RateBias == pusa.FedViceChair.RateBias;
                if (gsim != null) { try { Step(gsim); } finally { EnergyMarket.ResetTurnState(); } }
            }
            finally { SimulationRandom.Reinstate(live); }
            Step(psim);
            FedChair seatedNow = pusa.CurrentFedChair;
            FedSenateVote recorded = pusa.FedSenateVotes.Count > 0 ? pusa.FedSenateVotes[pusa.FedSenateVotes.Count - 1] : null;
            Check(onVote != null && onVote.Confirms && seatedNow != null && seatedNow.Name == pick.Name && seatedNow.TermBegan == nomination.VoteOn && seatedNow.TermEnds == nomination.VoteOn.AddYears(4)
                  && pusa.FedNominationPending == null && !pusa.FedViceChairActing && recorded != null && recorded.Confirmed && recorded.For == onVote.For && recorded.Against == onVote.Against,
                F("(e) K16: on the vote day ({0}), the Senate turned over, {1} - {2} seated to {3}, the vote recorded, the Vice Chair no longer acting",
                    Day(nomination.VoteOn), UsFedConsent.CountWords(onVote), seatedNow?.Name ?? "NONE", Day(seatedNow?.TermEnds ?? DateTime.MinValue)));
            FedSenateVote loadedVote = g?.FedSenateVotes != null && g.FedSenateVotes.Count > 0 ? g.FedSenateVotes[g.FedSenateVotes.Count - 1] : null;
            Check(adoptError == null && carried && loadedVote != null && recorded != null && loadedVote.Confirmed == recorded.Confirmed && loadedVote.For == recorded.For && loadedVote.Against == recorded.Against
                  && g.CurrentFedChair?.Name == pick.Name && g.CurrentFedChair.TermBegan == nomination.VoteOn && g.FedNominationPending == null && !g.FedViceChairActing,
                F("(e) the review's L-5: the save cut inside the window (format {0}) loads into the game{1} with the nomination and the Vice Chair's flag as cut, and on the vote day counts {2} as the continuous run does ({3})",
                    SaveGameService.CurrentSaveVersion, adoptError != null ? " - NOT LOADED: " + adoptError : carried ? string.Empty : " - THE NOMINATION OR THE FLAG NOT CARRIED",
                    loadedVote == null ? "NO VOTE" : loadedVote.For + "-" + loadedVote.Against, recorded == null ? "NO VOTE" : recorded.For + "-" + recorded.Against));
            // the refusal, planted: the rule refuses in no seated Senate ((k)), so a refusing count is handed to the Senate's vote as the film hands it
            seatedNow.TermEnds = psim.CurrentDate.Date;
            var second = new FedChair("K16 second probe", FedChairPhilosophy.Hawkish, "the diagnostic's second probe", 1.25f);
            bool lodgedAgain = psim.AppointCentralBankHead(CountryId.USA, second, out _);
            FedNomination again = pusa.FedNominationPending;
            var refusing = new UsFedConsent.Count { President = "probe", PresidentsParty = again?.NominatingParty, For = 48, Against = 52, OwnSeats = 48, OtherSeats = 52, Confirms = false };
            int votesBefore = pusa.FedSenateVotes.Count;
            FedSenateVote refusal = again == null ? null : UsFedConsent.Resolve(pusa, again, refusing, psim.CurrentDate);
            Check(lodgedAgain && refusal != null && !refusal.Confirmed && pusa.FedSenateVotes.Count == votesBefore + 1 && pusa.FedNominationPending == null && ReferenceEquals(pusa.CurrentFedChair, seatedNow)
                  && pusa.FedViceChairActing && UsFedConsent.StandingRefusal(pusa, psim.CurrentDate) == refusal && psim.CentralBankHeadAwaitsPlayer(CountryId.USA),
                F("(e) K16: a refusal planted on {0} - \"{1}\" - is recorded, the seat stays unfilled with the Vice Chair acting, it stands as the event the Docket shows, and the hold rises again",
                    Day(psim.CurrentDate), UsFedConsent.VoteWords(refusal) ?? "NOT RECORDED"));
            var third = new FedChair("K16 third probe", FedChairPhilosophy.Moderate, "the diagnostic's third probe", 0f);
            bool lodgedThird = psim.AppointCentralBankHead(CountryId.USA, third, out _);
            FedNomination thirdNomination = pusa.FedNominationPending;
            int votesThird = pusa.FedSenateVotes.Count;
            FedSenateVote noCount = thirdNomination == null ? null : UsFedConsent.Resolve(pusa, thirdNomination, null, psim.CurrentDate);
            Check(lodgedThird && thirdNomination != null && noCount == null && ReferenceEquals(pusa.FedNominationPending, thirdNomination) && pusa.FedSenateVotes.Count == votesThird
                  && UsFedConsent.StandingRefusal(pusa, psim.CurrentDate) == null,
                "(e) the review's M-2: a vote with no count (no president or no Senate found) records nothing - no refusal, no 0-0 tally - and the nomination stays before the Senate (the manager logs the defect)");
            // the delta review's L-A: the manager's retry of a null count is bounded - held again on the vote day, released on any later day, the hold rising again.
            // The planted defect's two log lines are muted here (the log fold would read them as this run's failure); what they would say is the manager's.
            System.Reflection.MethodInfo holdVote = typeof(SimulationManager).GetMethod("HoldPendingFedVote", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            bool heldOnDay = false, releasedAfter = false, holdAgain = false;
            if (holdVote != null && thirdNomination != null)
            {
                bool logWas = Debug.unityLogger.logEnabled;
                Debug.unityLogger.logEnabled = false;
                try
                {
                    thirdNomination.VoteOn = psim.CurrentDate.Date;
                    bool votedOnDay = (bool)holdVote.Invoke(psim, new object[] { pusa, thirdNomination, null });
                    heldOnDay = !votedOnDay && ReferenceEquals(pusa.FedNominationPending, thirdNomination) && pusa.FedSenateVotes.Count == votesThird;
                    thirdNomination.VoteOn = psim.CurrentDate.Date.AddDays(-1);
                    bool votedAfter = (bool)holdVote.Invoke(psim, new object[] { pusa, thirdNomination, null });
                    releasedAfter = !votedAfter && pusa.FedNominationPending == null && pusa.FedSenateVotes.Count == votesThird;
                    holdAgain = psim.CentralBankHeadAwaitsPlayer(CountryId.USA);
                }
                finally { Debug.unityLogger.logEnabled = logWas; }
            }
            Check(holdVote != null && heldOnDay && releasedAfter && holdAgain,
                F("(e) the delta review's L-A: a null count on the vote day holds the nomination for one more day ({0}); on a later day it is released with nothing recorded ({1}) and the hold rises again ({2})",
                    heldOnDay, releasedAfter, holdAgain));
            pusa.FedNominationPending = null;
            FedSenateVote standing = UsFedConsent.StandingRefusal(pusa, psim.CurrentDate);
            pusa.CurrentFedChair = CentralBankTerms.Seat(new FedChair("L-1 probe chair", FedChairPhilosophy.Moderate, "the diagnostic's AI-seated probe", 0f), CentralBankTerms.Fed, psim.CurrentDate);
            Check(standing == refusal && UsFedConsent.StandingRefusal(pusa, psim.CurrentDate) == null,
                "(e) the review's L-1: the standing refusal ends when a chair is seated without a vote (the AI's) - the Vice Chair no longer acts, so the Docket shows no refusal");

            // ---- (e3) the second delta review's H-B: a term ending on a turn's LAST day - a follower fork closes that turn with the Vice Chair acting, as the played world ----
            // The played world in the controller's order (GameController.Update): the term ends on the turn's last day, the boundary runs at once, and the hold rises only
            // after it, so the player's pick is lodged on day 0 of the next turn. A control stands for the played world (counterfactual-marked so it answers every OTHER
            // wait each morning as the fork does - the morning answer finds the nomination pending and does nothing), and a follower fork made from it on day 10, with no
            // policy withheld, must hold the control's Fed state AND its zone rate at the close and on every day to the vote day. The Vice Chair's lean is planted at +2,
            // so a fork that seated the AI's no-lean pick before the close (60ae4110's pre-close answer in ShadowBaseline.AdvanceTurn) reads another rate at the close and
            // another Fed state on every day of the window: this fails there.
            SimulationManager lsim = UsWorld(hosts, out Country lusa);
            lsim.PlayerCountryId = CountryId.USA;
            lsim.IsCounterfactual = true;
            DateTime lastDay = lsim.CurrentDate.Date.AddDays(SimulationManager.DaysPerTurn - lsim.DaysIntoTurn);
            FedChair lchair = lusa.CurrentFedChair;
            lchair.TermEnds = lastDay;
            lusa.FedViceChair = lusa.FedViceChair.Copy();
            lusa.FedViceChair.RateBias = 2f;
            ShadowBaseline follower = null;
            string firstPart = null;
            int compared = 0;
            void Compare(string when)
            {
                compared++;
                Country fc = follower.CountryFor(CountryId.USA);
                string a = FedStateOf(lusa) + " rate " + lusa.CurrencyZone.InterestRate.ToString("R", CultureInfo.InvariantCulture) + " day " + Day(lsim.CurrentDate);
                string b = FedStateOf(fc) + " rate " + fc.CurrencyZone.InterestRate.ToString("R", CultureInfo.InvariantCulture) + " day " + Day(follower.CurrentDate);
                if (firstPart == null && a != b) { firstPart = when + ": played [" + a + "], follower [" + b + "]"; }
            }
            DateTime voteOn = DateTime.MinValue;
            try
            {
                var none = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in lsim.World.Countries) { none[k.Id] = PolicyDecision.None(); }
                for (int d = 1; d <= SimulationManager.DaysPerTurn; d++)
                {
                    lsim.AnswerWaitsAsTheAi(CountryId.USA);
                    lsim.AdvanceDay();
                    lsim.AdvanceCountryDayTick(CountryId.USA);
                    if (d < SimulationManager.DaysPerTurn) { lsim.HoldPlayerElection(); }
                    if (d == 10) { follower = new ShadowBaseline(lsim, lsim.World, CountryId.USA, insideTurn: true, dayTick: true, seatAsPlayed: true) { AnswersWaitsAsTheAi = true }; }
                }
                follower.AdvanceTurn();   // the ledger's worlds close first, as GameController.AdvanceTurn runs them
                lsim.AdvanceTurn(none);
                EnergyMarket.BeginTurn(lsim.World);
                lsim.HoldPlayerElection();
                Compare("at the close");
                // the hold rises after the boundary: the pick lodged on day 0 of the next turn, and replayed into the follower at once (ShadowBaseline.Schedule's day 0)
                var hbPick = new FedChair("H-B probe chair", FedChairPhilosophy.Hawkish, "the diagnostic's probe", 0.5f);
                bool lodgedNext = lsim.AppointCentralBankHead(CountryId.USA, hbPick, out _) && lusa.FedNominationPending != null;
                if (lodgedNext)
                {
                    FedNomination n = lusa.FedNominationPending;
                    voteOn = n.VoteOn;
                    UsFedConsent.Lodge(follower.CountryFor(CountryId.USA), hbPick.Copy(), n.NominatingParty, n.NominatedOn);
                }
                for (int d = 1; lodgedNext && lsim.CurrentDate.Date < voteOn && d < SimulationManager.DaysPerTurn; d++)
                {
                    lsim.AnswerWaitsAsTheAi(CountryId.USA);
                    lsim.AdvanceDay();
                    lsim.AdvanceCountryDayTick(CountryId.USA);
                    lsim.HoldPlayerElection();
                    follower.CatchUp(d);
                    Compare("day " + d + " of the next turn");
                }
                Check(lodgedNext && follower.Faulted == null && firstPart == null && lusa.CurrentFedChair?.Name == hbPick.Name && lusa.CurrentFedChair.TermBegan == voteOn,
                    F("(e3) the second delta review's H-B: a term ending on the turn's last day ({0}) closes that turn with the Vice Chair acting in the played world and in a follower fork alike; the pick lodged on day 0 of the next turn is seated on {1} in both - {2} comparisons of the Fed state and the rate, {3}",
                        Day(lastDay), Day(voteOn), compared,
                        follower?.Faulted != null ? "THE FOLLOWER FAULTED: " + follower.Faulted : !lodgedNext ? "THE PICK WAS NOT LODGED" : firstPart ?? "every one equal"));
            }
            finally
            {
                follower?.Dispose();
                EnergyMarket.ResetTurnState();
            }

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
            Check(rpresident && gated && !rhold && rrefused && rwhy != null && rwhy == gateWords && !rwhy.Contains("THE SENATE") && ReferenceEquals(rusa.CurrentFedChair, rsitting)
                  && rusa.FedNominationPending == null,
                F("(e2) the player's USA with {0} seated (the president {1}, {2}), the term ended {3}: no hold ({4}); the verb refuses in the role gate's words - \"{5}\" - and nothing is lodged",
                    outParty, rshown ?? "NONE", rpp ?? "?", Day(rends), rhold, rwhy ?? "NOT REFUSED"));
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
                    UsFedConsent.Count firstCount = null, refusal2 = null;
                    bool blocRefused = false;
                    DateTime refusedOn = DateTime.MinValue;
                    for (DateTime day = first; head != null && day <= horizon; day = CentralBankTerms.TermEnds(CentralBankTerms.Fed, day))
                    {
                        UsFedConsent.Count c = UsFedConsent.Vote(u, day);
                        termDays++;
                        last = day;
                        if (firstCount == null) { firstCount = c; }
                        bool bloc = c != null && (c.OwnSeats > c.OtherSeats || (c.OwnSeats == c.OtherSeats && UsSenate.VicePresidentParty(u, day) == c.PresidentsParty));   // M-1: the old rule
                        if (c == null || !c.Confirms || !bloc) { refusal2 = c; refusedOn = day; blocRefused = c != null && c.Confirms && !bloc; break; }
                    }
                    bool confirmed = head != null && termDays > 0 && refusedOn == DateTime.MinValue;
                    Check(confirmed,
                        F("(f) {0}'s start {1}, no player: the AI USA's Fed chair on {2} term day(s) from {3} to {4} (the statute's four years, to the century's end {5}) - {6}",
                            start, Day(SimulationManager.EpochDate), termDays, Day(first), Day(last), Day(horizon),
                            confirmed ? "every count confirms (the first: " + (firstCount?.President ?? "?") + ", " + (firstCount?.PresidentsParty ?? "?") + ", " + UsFedConsent.CountWords(firstCount) + "), and the bloc count it replaces confirms each too: the AF-7 pick on every AF-7 day of this unstepped reading under either rule (the dump diff is the proof)"
                            : head == null ? "NO FED CHAIR SEATED"
                            : blocRefused ? "the BLOC COUNT REFUSES on " + Day(refusedOn) + " (" + refusal2.OwnSeats + "-" + refusal2.OtherSeats + " seats) where the crossing count confirms: the baseline's chair and this build's part from that day"
                            : "REFUSED on " + Day(refusedOn) + " (" + UsFedConsent.CountWords(refusal2) + "): the Vice Chair acts from that day"));
                }
            }
        }

        /// <summary>The Fed's state as (e3) compares it: the chair, its term, the Vice Chair's flag, the pending nominee and its vote day, the Senate's votes.</summary>
        private static string FedStateOf(Country c)
        {
            if (c == null) { return "NO USA"; }
            var sb = new StringBuilder();
            sb.Append(c.CurrentFedChair?.Name ?? "no chair").Append(' ').Append(c.CurrentFedChair?.TermBegan.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-")
              .Append(" acting ").Append(c.FedViceChairActing ? "yes" : "no")
              .Append(" pending ").Append(c.FedNominationPending == null ? "none" : (c.FedNominationPending.Nominee?.Name ?? "?") + " " + c.FedNominationPending.VoteOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
              .Append(" votes ").Append(c.FedSenateVotes?.Count ?? 0);
            return sb.ToString();
        }

        /// <summary>The Vice Chair given a probe lean in the instrument's own world, so the rate rule's reading of it is visible.</summary>
        private static void ProbeViceChair(Country usa) { if (usa.FedViceChair != null) { usa.FedViceChair = usa.FedViceChair.Copy(); usa.FedViceChair.RateBias = 0.75f; } }

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

        private static int Of(IReadOnlyDictionary<string, int> d, string key) => d != null && d.TryGetValue(key, out int n) ? n : 0;

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
