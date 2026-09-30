using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §705 (round 4 follow-up 4): GERMANY'S FORMATION - the chancellor's election under Art. 63 GG on the parties' dated declarations.
    /// (a) The declarations, dated and sourced (germany/coalition_declarations_2025.md): the CDU's incompatibility resolution of 8 December 2018
    /// (AfD and Linke), the CSU's of 24 July 2023 (AfD), the four 2025 candidacies - each standing from its own day. (b) The Union as one
    /// parliamentary group: a cabinet holds the CDU and the CSU both or neither, and outside a cabinet they vote as one. (c) The 2025 chamber forms
    /// CDU+CSU+SPD led by the CDU's candidate. (d) The procedure, dated, on the game's own path: the round opens the day after the 2025 polling
    /// day, the outgoing government stays in office until the new Bundestag convenes (Art. 69 Abs. 2) and serves on from that day (Abs. 3); the
    /// chancellor is elected on the convening day by a majority of the members (Art. 63 Abs. 2). (e) Planted refusals so no candidate reaches a
    /// majority: the fourteen days (Abs. 3), then the ballot the most votes win (Abs. 4) - no limit of proposals, no extra election. (f) The
    /// reference: the real 2025 result and the government that formed, Merz's CDU+CSU+SPD of 6 May 2025.
    /// </summary>
    public static class GermanFormationDiagnostic
    {
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== GermanFormationDiagnostic (§705): the chancellor's election, Art. 63 GG ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            var hosts = new List<GameObject>();
            DateTime poll25 = new DateTime(2025, 2, 23), poll21 = new DateTime(2021, 9, 26);
            try
            {
                // (a) the declarations, dated
                IReadOnlyList<PoliticalParty> roster = PartySystems.For(CountryId.Germany);
                bool Has(List<RedLine> lines, string a, string b) => lines.Exists(l => roster[l.A].Abbrev == a && roster[l.B].Abbrev == b && l.BlocksSupport && !l.OneWay && l.Kind == RedLineKind.Declared);
                int Declared(List<RedLine> lines) => lines.FindAll(l => l.Kind == RedLineKind.Declared).Count;
                List<RedLine> on25 = DeclaredRedLines.ForDate(CountryId.Germany, roster, poll25);
                List<RedLine> on21 = DeclaredRedLines.ForDate(CountryId.Germany, roster, poll21);
                Check(DeclaredRedLines.IsSourced(CountryId.Germany) && DeclaredRedLines.HasTimeline(CountryId.Germany) && !DeclaredRedLines.CandidacyRefuses(CountryId.Germany),
                    "(a) Germany's declarations are sourced and dated; a candidacy refuses no other candidate's cabinet (K-1f's pairing is Sweden's record, not Germany's)");
                Check(Has(on25, "CDU", "AfD") && Has(on25, "CDU", "Linke") && Has(on25, "CSU", "AfD") && Declared(on25) == 3,
                    F("(a) on 23 Feb 2025 three declared lines stand, symmetric and against support - no cabinet with, no government depending on: CDU-AfD and CDU-Linke (8 Dec 2018), CSU-AfD (24 Jul 2023) - {0} declared beside {1} derived", Declared(on25), on25.Count - Declared(on25)));
                Check(Has(on21, "CDU", "AfD") && Has(on21, "CDU", "Linke") && !Has(on21, "CSU", "AfD"),
                    "(a) on 26 Sep 2021 the CDU's two stand and the CSU's does not yet - each from its own day");
                IReadOnlyList<(string Abbrev, string Candidate, string Basis)> cands = DeclaredRedLines.CandidaciesAt(CountryId.Germany, poll25);
                bool Cand(IReadOnlyList<(string Abbrev, string Candidate, string Basis)> list, string party, string name) { foreach ((string a, string c, string _) in list) { if (a == party && c == name) { return true; } } return false; }
                Check(cands.Count == 4 && Cand(cands, "CDU", "Friedrich Merz") && Cand(cands, "SPD", "Olaf Scholz") && Cand(cands, "AfD", "Alice Weidel") && Cand(cands, "Grune", "Robert Habeck"),
                    F("(a) the 2025 candidacies on polling day: {0}", string.Join(", ", ListOf(cands))));
                int CountOn(int y, int m, int d) => DeclaredRedLines.CandidaciesAt(CountryId.Germany, new DateTime(y, m, d)).Count;
                IReadOnlyList<(string Abbrev, string Candidate, string Basis)> unionOn = DeclaredRedLines.CandidaciesAt(CountryId.Germany, new DateTime(2024, 10, 12));
                Check(CountOn(2024, 10, 11) == 0 && unionOn.Count == 1 && Cand(unionOn, "CDU", "Friedrich Merz") && CountOn(2024, 11, 16) == 1 && CountOn(2024, 11, 17) == 2
                      && CountOn(2024, 11, 24) == 2 && CountOn(2024, 11, 25) == 3 && CountOn(2024, 12, 6) == 3 && CountOn(2024, 12, 7) == 4,
                    "(a) dated, each from the earliest source saved whole: none on 11 Oct 2024; the Union's from 12 Oct (the CSU's page), the Greens' 17 Nov, the SPD's 25 Nov, the AfD's 7 Dec");

                // (b) the Union as one parliamentary group
                int[] seats25 = new int[roster.Count];
                Dictionary<string, int> table25 = PartySystems.InitialSeats(CountryId.Germany, ElectionVintage.Germany2025);
                for (int p = 0; p < roster.Count; p++) { table25.TryGetValue(roster[p].Abbrev, out seats25[p]); }
                int[] joint = ChamberRules.JointMasks(CountryId.Germany, roster, seats25);
                int Bit(string abbrev) { for (int p = 0; p < roster.Count; p++) { if (roster[p].Abbrev == abbrev) { return 1 << p; } } return 0; }
                int cdu = Bit("CDU"), csu = Bit("CSU"), spd = Bit("SPD");
                Check(joint != null && joint.Length == 1 && joint[0] == (cdu | csu) && CoalitionFormation.SplitsJoint(cdu | spd, joint) && !CoalitionFormation.SplitsJoint(cdu | csu | spd, joint) && !CoalitionFormation.SplitsJoint(spd, joint),
                    "(b) one Fraktion (§ 10 Abs. 1 GO-BT): CDU+SPD splits the Union, CDU+CSU+SPD and SPD alone do not");
                int[] csuOut = (int[])seats25.Clone();
                for (int p = 0; p < roster.Count; p++) { if (roster[p].Abbrev == "CSU") { csuOut[p] = 0; } }
                Check(ChamberRules.JointMasks(CountryId.Germany, roster, csuOut) == null,
                    "(b) a Fraktion is formed by the parties seated: with the CSU under the line (no Grundmandat modelled) there is no group, and a CDU cabinet splits nothing");
                int alike = CoalitionFormation.JointAlike(cdu, spd, joint, seats25);
                Check(alike == (cdu | csu) && CoalitionFormation.JointAlike(csu, spd, joint, seats25) == 0,
                    "(b) outside a cabinet the two vote as one, the larger's side: the CDU for carries the CSU; the CSU alone for carries nobody");

                // (c) the 2025 chamber's formation on polling day's declarations
                WorldClock.ApplyStart(CountryId.Germany);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World viewWorld = WorldFactory.CreateDefault();
                Country viewGermany = viewWorld.GetCountry(CountryId.Germany);
                viewGermany.ParliamentSeats = new Dictionary<string, int>(table25);
                GovernmentFormation.View view = GovernmentFormation.ViewOf(viewGermany, DeclarationReading.OfElection(CountryId.Germany, poll25));
                string viewCabinet = Sorted(view.Cabinet.ConvertAll(c => c.Abbrev));
                Check(view.HasGovernment && viewCabinet == Sorted(new List<string> { "CDU", "CSU", "SPD" }) && view.CabinetSeats == 328 && view.Majority == 316 && !view.NegativeRule,
                    F("(c) the 2025 chamber forms {0} on {1} of 630 ({2} a majority of the members) - the record's coalition", viewCabinet, view.CabinetSeats, view.Majority));

                // (d) the procedure on the game's own path: the 2025 election held, the round the day after
                (SimulationManager s1, Country g1) = Open(hosts, "Linke", table25, poll25);
                GovernmentRecord outgoing = g1.Government;
                g1.ElectionHistory.Add(new ElectionRecord { Date = poll25, CountryId = CountryId.Germany.ToString(), Method = ElectionMethod.GermanyNationalProportional });
                Days(s1, 1);
                SpeakerRound r1 = s1.RoundOf(CountryId.Germany);
                Check(s1.RoundsApply(CountryId.Germany) && r1 != null && r1.Bundestag && r1.Phase == 1 && r1.Convenes == new DateTime(2025, 3, 25) && r1.Asked == "CDU" && r1.Order.Count > 0 && r1.Order[0] == "CDU" && !outgoing.Caretaker,
                    F("(d) {0:yyyy-MM-dd}: the chancellor's election opens (Art. 63 GG) - the order {1}; the new Bundestag convenes by {2:yyyy-MM-dd} (Art. 39 Abs. 2: the thirtieth day); the outgoing government still in office",
                        s1.CurrentDate, r1 != null ? string.Join(", ", r1.Order) : "none", r1?.Convenes ?? DateTime.MinValue));
                bool movedDuring = s1.MoveNoConfidence(CountryId.Germany, out string whyNot, out ConfidenceProcedure.MotionVote _);
                Check(!movedDuring && (whyNot ?? string.Empty).Contains("ART. 63"),
                    F("(d) no constructive vote while the chancellor's election runs - the old Bundestag sits, and a successor it elected would never be discharged: {0}", whyNot ?? "MOVED"));
                Days(s1, SpeakerRound.ConsultationDays);
                Check(r1.Stage == RoundStage.VotePending && r1.VoteOn == r1.Convenes && r1.Proposal != null && Sorted(r1.Proposal.CabinetParties) == Sorted(new List<string> { "CDU", "CSU", "SPD" }),
                    F("(d) {0:yyyy-MM-dd}: the CDU forms {1}; the ballot waits for the new Bundestag - {2:yyyy-MM-dd}", s1.CurrentDate, r1.Proposal != null ? string.Join("+", r1.Proposal.CabinetParties) : "nothing", r1.VoteOn));
                while (s1.CurrentDate < r1.Convenes.AddDays(-1)) { Days(s1, 1); }
                bool inOfficeEve = g1.Government == outgoing && !outgoing.Caretaker;
                Days(s1, 1);
                GovernmentRecord elected = g1.Government;
                DivisionRecord ballot = g1.Divisions.Entries.Count > 0 ? g1.Divisions.Entries[g1.Divisions.Entries.Count - 1] : null;
                int forSeats = 0;
                if (ballot?.Sides != null) { foreach (DivisionSide side in ballot.Sides) { if (side.Side > 0) { forSeats += side.Seats; } } }
                Check(inOfficeEve && outgoing.Caretaker && outgoing.Breaks.Exists(b => b.Contains("Art. 69 Abs. 2")),
                    "(d) the eve of the convening the outgoing government is in office; on the day its office ends and it serves on (Art. 69 Abs. 2-3) - "
                    + (outgoing.Breaks.Count > 0 ? outgoing.Breaks[outgoing.Breaks.Count - 1] : "no break recorded"));
                Check(elected != outgoing && !r1.Open && Sorted(elected.Cabinet) == Sorted(new List<string> { "CDU", "CSU", "SPD" }) && elected.PmParty == "CDU" && elected.FormedOn == new DateTime(2025, 3, 25)
                      && (elected.Basis ?? string.Empty).Contains("Art. 63 Abs. 2") && ballot != null && ballot.Motion && ballot.Passed
                      && ballot.Title.StartsWith("Chancellor's election (Art. 63 Abs. 2 GG): Friedrich Merz (CDU)", StringComparison.Ordinal) && ballot.Title.EndsWith("- elected", StringComparison.Ordinal) && forSeats >= 328,
                    F("(d) {0:yyyy-MM-dd}: '{1}' - {2} for, 316 needed; {3} takes office led by {4} ({5})", s1.CurrentDate, ballot?.Title ?? "no ballot", forSeats, string.Join("+", elected.Cabinet), elected.PmParty, elected.Basis));
                // The record's chancellor was elected on 6 May 2025, after 72 days of coalition talks; the game's formateur consults for seven days
                // (premise 7), so its chancellor is elected on the convening day - a readout of the premise, not a claim about the talks.
                foreach (string line in r1.Log) { sb.Append("    log       ").Append(line).Append('\n'); }
                sb.Append(F("    readout   the game elects on {0:yyyy-MM-dd}, the record on 2025-05-06 (Merz, second ballot 325 of 618): the consultation is premise 7's seven days, the record's talks took 72\n", elected.FormedOn));

                // The round rides the save with its Art. 63 fields (no version bump: before §705 no German round could exist in a save).
                (SimulationManager s2, Country g2) = Open(hosts, "Linke", table25, poll25);
                g2.ElectionHistory.Add(new ElectionRecord { Date = poll25, CountryId = CountryId.Germany.ToString(), Method = ElectionMethod.GermanyNationalProportional });
                Days(s2, 1 + SpeakerRound.ConsultationDays);
                Persistence.SaveGame saved = Persistence.SaveGameService.CreateSaveGame(s2, s2.World, CountryId.Germany, null);
                SpeakerRound back = Persistence.SaveGameService.Deserialize(Persistence.SaveGameService.Serialize(saved)).World.GetCountry(CountryId.Germany).Government.Round;
                Check(back != null && back.Bundestag && back.Phase == 1 && back.Convenes == new DateTime(2025, 3, 25) && back.VoteOn == back.Convenes && back.Stage == RoundStage.VotePending,
                    F("(d) the tabled round rides the save: phase {0}, convenes {1:yyyy-MM-dd}, the ballot {2:yyyy-MM-dd}", back?.Phase ?? -1, back?.Convenes ?? DateTime.MinValue, back?.VoteOn ?? DateTime.MinValue));

                // A save made after a pre-§705 German election: the government formed at once, dated the polling day. Loaded, the election is the one
                // that formed it - no chancellor's election reopens (the review's defect 6), no discharge, no second arrival budget.
                (SimulationManager s4, Country g4) = Open(hosts, "Linke", table25, poll25);
                GovernmentRecord formedAtOnce = g4.Government;
                formedAtOnce.FormedOn = poll25;
                g4.ElectionHistory.Add(new ElectionRecord { Date = poll25, CountryId = CountryId.Germany.ToString(), Method = ElectionMethod.GermanyNationalProportional });
                Days(s4, 40);
                Check(s4.RoundOf(CountryId.Germany) == null && g4.Government == formedAtOnce && !formedAtOnce.Caretaker,
                    "(d) a pre-§705 German save (its government formed on the polling day) loads as it was: no chancellor's election reopens, nothing discharged at the convening");

                // (e) planted refusals - no candidate reaches a majority: the fourteen days, then the ballot the most votes win
                (SimulationManager s3, Country g3) = Open(hosts, "Linke", table25, poll25);
                s3.AdvanceDay();
                GovernmentRecord outgoing3 = g3.Government;
                s3.OpenSpeakerRound(g3, ElectionVintage.Germany2025, "for the check (planted refusals)",
                    refusals: new[] { "SPD>CDU", "CDU>SPD", "CDU>Grune", "SPD>AfD", "Grune>AfD", "Linke>AfD" }, electionDay: poll25);
                SpeakerRound r3 = s3.RoundOf(CountryId.Germany);
                DateTime? phase2On = null, pluralityOn = null;
                DateTime secondUntil = DateTime.MinValue;
                for (int d = 0; d < 120 && r3.Open; d++)
                {
                    Days(s3, 1);
                    if (phase2On == null && r3.Phase == 2) { phase2On = s3.CurrentDate; secondUntil = r3.SecondPhaseUntil; }
                    if (pluralityOn == null && r3.Phase == 3) { pluralityOn = s3.CurrentDate; }
                    if (r3.Stage == RoundStage.PlayerAsked) { s3.PassFormation(CountryId.Germany, out string _); }
                    else if (r3.Stage == RoundStage.OfferToPlayer) { s3.AnswerOffer(CountryId.Germany, false, out string _); }
                }
                GovernmentRecord appointed = g3.Government;
                List<DivisionRecord> ballots = g3.Divisions.Entries.FindAll(e => e.Title.StartsWith("Chancellor's election", StringComparison.Ordinal));
                DivisionRecord last = ballots.Count > 0 ? ballots[ballots.Count - 1] : null;
                List<DivisionRecord> inPhase2 = ballots.FindAll(e => e.Title.Contains("Art. 63 Abs. 3"));
                FieldInfo extraDate = typeof(SimulationManager).GetField("_extraElectionDate", BindingFlags.Instance | BindingFlags.NonPublic);
                DateTime extra = extraDate != null ? (DateTime)extraDate.GetValue(s3) : DateTime.MaxValue;
                Check(phase2On == new DateTime(2025, 3, 25) && secondUntil == new DateTime(2025, 4, 8) && inPhase2.Count >= 2 && inPhase2.TrueForAll(e => e.Date <= secondUntil),
                    F("(e) the Bundespräsident's candidate not elected on {0:yyyy-MM-dd}: the fourteen days run to {1:yyyy-MM-dd} (Art. 63 Abs. 3), {2} ballot(s) inside them, each on the day its candidate stood ({3})",
                        phase2On ?? DateTime.MinValue, secondUntil, inPhase2.Count, string.Join(", ", inPhase2.ConvertAll(e => e.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))));
                bool sincere = last != null && last.Sides.Exists(s => (s.Reason ?? string.Empty).Contains("the game's premise: sincere votes"));
                Check(pluralityOn == new DateTime(2025, 4, 9) && !r3.Open && appointed != outgoing3 && last != null
                      && last.Title.StartsWith("Chancellor's election (Art. 63 Abs. 4 GG): Friedrich Merz (CDU) - elected with the most votes", StringComparison.Ordinal)
                      && last.Title.Contains("appointed, not dissolved (the game's premise)") && sincere
                      && Sorted(appointed.Cabinet) == Sorted(new List<string> { "CDU", "CSU" }) && appointed.PmParty == "CDU" && (appointed.Basis ?? string.Empty).Contains("Art. 63 Abs. 4 Satz 3"),
                    F("(e) {0:yyyy-MM-dd}, the day after: '{1}'; {2} takes office led by {3} - the ballot's sides carry the tally's premise ({4})", pluralityOn ?? DateTime.MinValue, last?.Title ?? "no ballot",
                        string.Join("+", appointed.Cabinet), appointed.PmParty, sincere ? "sincere votes" : "NOT STATED"));
                if (last != null) { foreach (DivisionSide s in last.Sides) { sb.Append(F("    side      {0} {1} ({2}): {3}\n", s.Abbrev, s.Side > 0 ? "for" : s.Side < 0 ? "other" : "abstains", s.Seats, s.Reason)); } }
                Check(extra == DateTime.MinValue && !r3.Log.Exists(l => l.Contains("breaks off")) && ballots.Count >= 4,
                    F("(e) {0} ballots and no limit of proposals: the procedure never breaks off to an extra election (the Riksdag's four are RF 6 kap. 5 §, not the Grundgesetz's)", ballots.Count));
                foreach (string line in r3.Log) { sb.Append("    log       ").Append(line).Append('\n'); }

                // (e2) the review's second pass: the player's party passed - it stands no candidate in the ballot the most votes win (defect 2);
                // and on the polling day itself, before the round opens, no constructive vote (A)
                (SimulationManager s5, Country g5) = Open(hosts, "CDU", table25, poll25);   // in opposition to the Scholz government, first in the order
                g5.ElectionHistory.Add(new ElectionRecord { Date = poll25, CountryId = CountryId.Germany.ToString(), Method = ElectionMethod.GermanyNationalProportional });
                bool movedOnPollingDay = s5.MoveNoConfidence(CountryId.Germany, out string pollingWhy, out ConfidenceProcedure.MotionVote _);
                Check(!movedOnPollingDay && s5.ElectionAwaitsRound(CountryId.Germany) && (pollingWhy ?? string.Empty).Contains("ART. 63"),
                    F("(e2) on the polling day, before the round opens, no constructive vote - the new chamber's seats are not the old Bundestag's: {0}", pollingWhy ?? "MOVED"));
                Days(s5, 1);   // the game's own round opens the day after; the planted refusals stand in it
                SpeakerRound r5 = s5.RoundOf(CountryId.Germany);
                r5?.Refusals.AddRange(new[] { "SPD>CDU", "CDU>SPD", "CDU>Grune", "SPD>AfD", "Grune>AfD", "Linke>AfD" });
                bool cduPassed = false;
                for (int d = 0; d < 120 && r5 != null && r5.Open; d++)
                {
                    if (r5.Stage == RoundStage.PlayerAsked) { cduPassed |= s5.PassFormation(CountryId.Germany, out string _); }
                    else if (r5.Stage == RoundStage.OfferToPlayer) { s5.AnswerOffer(CountryId.Germany, false, out string _); }
                    Days(s5, 1);
                }
                if (r5 != null) { foreach (string line in r5.Log) { sb.Append("    log e2    ").Append(line).Append((char)10); } }
                DivisionRecord final5 = g5.Divisions.Entries.FindLast(e => e.Title.Contains("Art. 63 Abs. 4"));
                DivisionSide cduSide = final5?.Sides.Find(s => s.Abbrev == "CDU");
                Check(cduPassed && final5 != null && cduSide != null && !(cduSide.Reason ?? string.Empty).Contains("its own candidate") && !(final5.Title.Contains("Friedrich Merz")),
                    F("(e2) the CDU, the player's party, passed when asked: in the ballot the most votes win it stands no candidate - {0}; {1}", cduSide?.Reason ?? "no side", final5?.Title ?? "no ballot"));

                // (e3) the review's third pass (B, C, D): a CSU player that declined the CDU's offer - the Union votes as one in the ballot the most
                // votes win, and the elected Union chancellor's cabinet holds the Union; nothing strands Germany under a caretaker
                (SimulationManager s6, Country g6) = Open(hosts, "CSU", table25, poll25);
                g6.ElectionHistory.Add(new ElectionRecord { Date = poll25, CountryId = CountryId.Germany.ToString(), Method = ElectionMethod.GermanyNationalProportional });
                Days(s6, 1);
                SpeakerRound r6 = s6.RoundOf(CountryId.Germany);
                r6?.Refusals.AddRange(new[] { "SPD>CDU", "CDU>SPD", "CDU>Grune", "SPD>AfD", "Grune>AfD", "Linke>AfD" });
                bool csuDeclined = false;
                for (int d = 0; d < 120 && r6 != null && r6.Open; d++)
                {
                    if (r6.Stage == RoundStage.PlayerAsked) { s6.PassFormation(CountryId.Germany, out string _); }
                    else if (r6.Stage == RoundStage.OfferToPlayer) { csuDeclined |= s6.AnswerOffer(CountryId.Germany, false, out string _); }
                    Days(s6, 1);
                }
                DivisionRecord final6 = g6.Divisions.Entries.FindLast(e => e.Title.Contains("Art. 63 Abs. 4"));
                DivisionSide csuSide = final6?.Sides.Find(s => s.Abbrev == "CSU");
                Check(csuDeclined && final6 != null && csuSide != null && (csuSide.Reason ?? string.Empty).Contains("votes with its parliamentary group")
                      && Sorted(g6.Government.Cabinet) == Sorted(new List<string> { "CDU", "CSU" }) && g6.Government.PmParty == "CDU" && !g6.Government.Caretaker
                      && r6 != null && r6.Log.Exists(l => l.Contains("sits in") && l.Contains("the game's premise")) && g6.Government.Breaks.Exists(b => b.Contains("a group votes and governs as one")) && (csuSide?.Reason ?? string.Empty).Contains("the game's premise"),
                    F("(e3) the CSU, the player's party, declined the CDU's offer: in the ballot it votes with its group ({0}); {1} - {2} takes office led by {3}, the CSU in it by the group's rule",
                        csuSide?.Reason ?? "no side", final6?.Title ?? "no ballot", string.Join("+", g6.Government.Cabinet), g6.Government.PmParty));

                // (f) the reference: the real result and the government that formed
                bool hasRef = WorldClock.TryReference(CountryId.Germany, poll25, out WorldClock.Reference reference);
                Check(hasRef && reference.Seats["CDU"] == 164 && reference.Seats["CSU"] == 44 && reference.Seats["AfD"] == 152 && reference.Seats["SPD"] == 120 && reference.Seats["Grune"] == 85
                      && reference.Seats["Linke"] == 64 && reference.Seats["SSW"] == 1,
                    "(f) the reference's seats are the 2025 result: CDU 164, CSU 44, AfD 152, SPD 120, Grüne 85, Linke 64, SSW 1");
                Check(hasRef && reference.HeadParty == "CDU" && reference.HeadSurname == "Merz" && reference.HeadFrom == new DateTime(2025, 5, 6) && reference.CabinetOfRecord != null && string.Join("+", reference.CabinetOfRecord) == "CDU+CSU+SPD",
                    F("(f) the government that formed: {0}", hasRef ? reference.GovernmentLine : "none"));
                Check(ElectionNightScreen.SpeakerLine(false, "CDU/CSU", bundestag: true).Contains("Bundespräsident") && ElectionNightScreen.SpeakerLine(false, "CDU/CSU", bundestag: true).Contains("Art. 63"),
                    "(f) the verdict after a German election is the Bundestag's: the Bundespräsident proposes, the Bundestag elects once it convenes");
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); } EnergyMarket.ResetTurnState(); }
            if (failures > 0) { Debug.LogError($"GERMAN FORMATION: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>A cabinet as its parties sorted - the formation lists them by index, the record by rank.</summary>
        private static string Sorted(List<string> parties)
        {
            var copy = new List<string>(parties);
            copy.Sort(string.CompareOrdinal);
            return string.Join("+", copy);
        }

        private static IEnumerable<string> ListOf(IReadOnlyList<(string Abbrev, string Candidate, string Basis)> list)
        {
            foreach ((string a, string c, string _) in list) { yield return c + " (" + a + ")"; }
        }

        /// <summary>A German game on the snap start with the 2025 chamber seated and the clock set to the polling day - the check's jump over the
        /// 109 campaign days GermanCampaignDiagnostic runs whole.</summary>
        private static (SimulationManager, Country) Open(List<GameObject> hosts, string player, Dictionary<string, int> seats, DateTime pollingDay)
        {
            var go = new GameObject("GermanFormationDiagnostic." + player); hosts.Add(go);
            WorldClock.ApplyStart(CountryId.Germany);
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            sim.PlayerCountryId = CountryId.Germany;
            Country germany = world.GetCountry(CountryId.Germany);
            germany.PlayerPartyAbbrev = player;
            germany.ParliamentSeats = new Dictionary<string, int>(seats);
            typeof(SimulationManager).GetProperty("CurrentDate").SetValue(sim, pollingDay);
            return (sim, germany);
        }

        private static void Days(SimulationManager sim, int n) { for (int d = 0; d < n; d++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Germany); } }
    }
}
