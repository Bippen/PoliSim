using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §698 (PS-4): **THE CONSTRUCTIVE VOTE OF NO CONFIDENCE, ART. 67 GG.** On Germany's own start (the 20th Bundestag, Scholz's SPD+Grüne minority):
    /// (a) the rules - the Bundestag's constructive vote, the Riksdag's unchanged, the other four unsourced; (b) the record's chamber - the player's CDU
    /// projects and moves its constructive vote: the successor's government, the count against a majority of the members, the motion recorded as a
    /// division, elected exactly when the count reaches a majority of the members (§754: the vote is on the person), and - not elected - the government standing untouched;
    /// (c) THE PLANTED CHAMBER, the CDU and CSU holding a majority alone: the vote carries and the successor's government takes office the same day -
    /// the CDU's chancellor, no caretaker, no declaration's week, the basis naming Art. 67, the chancellor's own party having voted against; (d) the AI:
    /// on the planted chamber, the player's party elsewhere, the CDU moves its own constructive vote by ruling (2)'s rule and installs its government;
    /// (f) §754 (Elias's ruling A4: "the constructive vote is a vote on the successor ... There is no separate vote against the incumbent"): every party
    /// votes on the person, and a successor whose drafted government does not hold is still elected by a majority of the members.
    /// </summary>
    public static class ConstructiveVoteDiagnostic
    {
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== ConstructiveVoteDiagnostic (§698): Art. 67 GG - the motion is the successor's election ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            (SimulationManager, Country) Open(string name, string playerParty)
            {
                var go = new GameObject(name); hosts.Add(go);
                WorldClock.ApplyStart(CountryId.Germany);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Germany;
                Country de = world.GetCountry(CountryId.Germany);
                de.PlayerPartyAbbrev = playerParty;
                return (sim, de);
            }
            void Plant(Country de)
            {
                // a majority for the CDU and CSU alone - the rest of the chamber cut to match (736 members, 369 needed)
                var seats = new Dictionary<string, int> { { "CDU", 330 }, { "CSU", 60 }, { "SPD", 150 }, { "Grune", 80 }, { "FDP", 40 }, { "AfD", 50 }, { "Linke", 25 }, { "SSW", 1 }, { "BSW", 0 } };
                foreach (KeyValuePair<string, int> kv in seats) { de.ParliamentSeats[kv.Key] = kv.Value; }
            }
            try
            {
                using (SimulationManager.EpochScope())
                {
                    // (a) the rules
                    Check(ConfidenceProcedure.RulesOf(CountryId.Germany) == ConfidenceProcedure.Rules.Bundestag && ConfidenceProcedure.RulesOf(CountryId.Sweden) == ConfidenceProcedure.Rules.Riksdag
                          && ConfidenceProcedure.RulesOf(CountryId.Poland) == ConfidenceProcedure.Rules.Unsourced && ConfidenceProcedure.RulesOf(CountryId.USA) == ConfidenceProcedure.Rules.Unsourced,
                        "the rules: the Bundestag's constructive vote, the Riksdag's unchanged, the other four unsourced");

                    // (b) the record's chamber
                    (SimulationManager sim, Country de) = Open("ConstructiveVoteDiagnostic real", "CDU");
                    GovernmentRecord before = de.Government;
                    ConfidenceProcedure.MotionVote projected = sim.ProjectConstructiveVote(CountryId.Germany);
                    Check(before != null && projected != null && projected.Constructive && projected.Carried == (projected.For >= projected.Needed),
                        F("on the start ({0} leading {1}), the CDU's constructive vote projected: {2} of {3} members for, {4} needed - {5}; the successor's government {6}{7}{8}",
                            before?.PmParty, before != null ? string.Join("+", before.Cabinet) : "none", projected?.For, projected?.Members, projected?.Needed,
                            projected != null && projected.Carried ? "ELECTED" : "not elected", projected != null ? string.Join("+", projected.SuccessorCabinet) : "none",
                            projected != null && projected.SuccessorSupport.Count > 0 ? " with " + string.Join("+", projected.SuccessorSupport) : string.Empty,
                            projected != null && projected.Refusal != null ? "; refused by " + projected.Refusal : string.Empty));
                    int divisions = de.Divisions.Entries.Count;
                    bool moved = sim.MoveNoConfidence(CountryId.Germany, out string refused, out ConfidenceProcedure.MotionVote vote);
                    DivisionRecord last = de.Divisions.Entries.Count > divisions ? de.Divisions.Entries[de.Divisions.Entries.Count - 1] : null;
                    Check(moved && vote != null && last != null && last.Motion && last.Title.Contains("Art. 67"),
                        "the CDU moves it: taken up, recorded as a division - " + (last != null ? last.Title : refused ?? "NOTHING RECORDED"));
                    if (vote != null && !vote.Carried)
                    {
                        Check(ReferenceEquals(de.Government, before) && de.Government.PmParty == before.PmParty && !de.Government.Caretaker && de.Government.NoConfidenceOn == DateTime.MinValue,
                            "not carried: the government stands, untouched - no declaration, no week, no caretaker");
                    }
                    else
                    {
                        sb.Append("    read      ⚠ the record's chamber CARRIES the CDU's constructive vote - read against Germany's declarations, which are not sourced (the formation's lines are derived)\n");
                    }

                    // (c) the planted chamber
                    (SimulationManager sim2, Country de2) = Open("ConstructiveVoteDiagnostic planted", "CDU");
                    Plant(de2);
                    GovernmentRecord outgoing = de2.Government;
                    bool moved2 = sim2.MoveNoConfidence(CountryId.Germany, out string refused2, out ConfidenceProcedure.MotionVote vote2);
                    GovernmentRecord formed = de2.Government;
                    DivisionSide pmSide = null;
                    if (vote2 != null) { foreach (DivisionSide s in vote2.Sides) { if (s.Abbrev == outgoing.PmParty) { pmSide = s; } } }
                    Check(moved2 && vote2 != null && vote2.Carried && !ReferenceEquals(formed, outgoing) && formed.PmParty == "CDU" && !formed.Caretaker
                          && formed.NoConfidenceOn == DateTime.MinValue && formed.Basis != null && formed.Basis.Contains("Art. 67") && pmSide != null && pmSide.Side < 0,
                        F("THE PLANTED CHAMBER (CDU 330 + CSU 60 of 736): {6}, {0} of {1} for the successor's {7}; in office after it: {2} led by {3}, no caretaker, the basis naming Art. 67; the chancellor's party ({4}) voted {5}{8}",
                            vote2?.For, vote2?.Members, formed != null ? string.Join("+", formed.Cabinet) : "none", formed?.PmParty, outgoing?.PmParty, pmSide == null ? "NOTHING" : pmSide.Side < 0 ? "against" : "FOR",
                            vote2 != null && vote2.Carried ? "carried" : "NOT CARRIED", vote2 != null ? string.Join("+", vote2.SuccessorCabinet) + (vote2.SuccessorSupport.Count > 0 ? " with " + string.Join("+", vote2.SuccessorSupport) : string.Empty) : "none",
                            vote2 != null && vote2.Refusal != null ? " - refusals: " + vote2.Refusal : string.Empty));
                    // the review's defects 3 and 4: a sitting cabinet partner drafted into the successor weighs its posts (the FDP stays), and the installed
                    // government seats no refusing party (§754: a refuser is no longer counted out of the vote - it votes on the person like any party)
                    DivisionSide fdpSide = null; int refusersSeated = 0;
                    if (vote2 != null) { foreach (DivisionSide s in vote2.Sides) { if (s.Abbrev == "FDP") { fdpSide = s; } } foreach (string r in vote2.Refusers) { if (formed.Cabinet.Contains(r) || formed.Support.Contains(r)) { refusersSeated++; } } }
                    Check(vote2 != null && refusersSeated == 0 && (!vote2.SuccessorSupport.Contains("FDP") && !vote2.SuccessorCabinet.Contains("FDP") || (fdpSide != null && fdpSide.Side < 0 && vote2.Refusers.Contains("FDP") && !formed.Support.Contains("FDP") && !formed.Cabinet.Contains("FDP"))),
                        F("a sitting partner weighs its posts: the FDP, drafted {0}, votes {1} ({2}); {3} refuser(s) seated in the installed government",
                            vote2 != null && vote2.SuccessorCabinet.Contains("FDP") ? "into the cabinet" : vote2 != null && vote2.SuccessorSupport.Contains("FDP") ? "as a supporter" : "not at all",
                            fdpSide == null ? "NOTHING" : fdpSide.Side < 0 ? "against" : fdpSide.Side > 0 ? "FOR" : "abstaining", fdpSide?.Reason, refusersSeated));

                    // (f) §754 (ruling A4): every party votes on the PERSON - each side's reason is the person ballot's, the vote listed in full
                    string[] personReasons = { "its candidate", "parliamentary group", "elects him", "does not elect", "abstains", "coalition's word" };
                    int personSides = 0, sincere = 0;
                    if (vote2 != null)
                    {
                        foreach (DivisionSide s in vote2.Sides)
                        {
                            bool person = false;
                            foreach (string r in personReasons) { if (s.Reason != null && s.Reason.Contains(r)) { person = true; } }
                            if (person) { personSides++; }
                            if (s.Reason != null && s.Reason.Contains("sincere votes")) { sincere++; }
                            sb.Append(F("    side      {0} {1}: {2} - {3}\n", s.Abbrev, s.Seats, s.Side > 0 ? "elects" : s.Side < 0 ? "does not elect" : "abstains", s.Reason));
                        }
                    }
                    Check(vote2 != null && personSides == vote2.Sides.Count && sincere > 0,
                        F("§754: every party votes on the person - {0} of {1} sides carry the person ballot's reason, {2} by the sincere rule (a refusal, or the comparison - which the Linke case below decides)", personSides, vote2?.Sides.Count, sincere));

                    // (f) §754: elected by a majority of the members whatever the partners answer - the CDU's candidate with the sitting chancellor's Greens
                    // drafted into his cabinet (they stay where they sit, so that government does not hold): elected all the same, on the CDU's and CSU's 390
                    var crafted = new FormationProposal { Formateur = "CDU" };
                    crafted.CabinetParties.Add("CDU"); crafted.CabinetParties.Add("CSU"); crafted.CabinetParties.Add("Grune");
                    (SimulationManager sim6, Country de6) = Open("ConstructiveVoteDiagnostic crafted", "SSW");
                    Plant(de6);
                    var craftedRound = new SpeakerRound { MidTerm = true, OpenedOn = sim6.CurrentDate };
                    CoalitionFormation.Chamber craftedChamber = Formateur.ChamberOf(de6, sim6.RoundReading(de6, craftedRound), null, out IReadOnlyList<PoliticalParty> craftedParties);
                    ProposalVerdict craftedVerdict = Formateur.Answer(de6, crafted, sim6.CurrentDate, sim6.World, sim6.RoundReading(de6, craftedRound), null, de6.PlayerPartyAbbrev);
                    ConfidenceProcedure.MotionVote craftedVote = ConfidenceProcedure.ConstructiveVote(de6, "CDU", crafted, craftedVerdict, craftedChamber, craftedParties);
                    Check(craftedVote.Carried && !craftedVote.PartnersAccept && craftedVote.For >= craftedVote.Needed,
                        F("§754: CDU+CSU+Grüne drafted on the planted chamber - partners {0} ({1}); {2} of {3} for, {4} needed: {5}",
                            craftedVote.PartnersAccept ? "ACCEPT" : "refuse", craftedVote.Refusal ?? "none", craftedVote.For, craftedVote.Members, craftedVote.Needed, craftedVote.Carried ? "elected all the same" : "NOT ELECTED"));

                    // (f) the review's defect 1: a sitting partner drafted into a successor's government that does NOT hold never elects him - the FDP and the
                    // Greens (Scholz's partners) drafted beside the Union, the Greens refusing: he would govern without them, so neither votes its chancellor out
                    // planted: a large FDP (200) beside a small Union (100 + 20), the sitting cabinet diluted, so the FDP's posts in the draft are worth more - it
                    // would LEAVE, so only the draft's not holding keeps it from electing (the defect's own path, not the staying one)
                    (SimulationManager sim8, Country de8) = Open("ConstructiveVoteDiagnostic defect 1", "SSW");
                    foreach (KeyValuePair<string, int> kv in new Dictionary<string, int> { { "CDU", 100 }, { "CSU", 20 }, { "SPD", 150 }, { "Grune", 80 }, { "FDP", 200 }, { "AfD", 50 }, { "Linke", 25 }, { "SSW", 1 }, { "BSW", 0 } }) { de8.ParliamentSeats[kv.Key] = kv.Value; }
                    de8.Government.Cabinet.Add("AfD"); de8.Government.Cabinet.Add("Linke");   // and the sitting cabinet diluted - its cohesion and the FDP's share both fall
                    var round8 = new SpeakerRound { MidTerm = true, OpenedOn = sim8.CurrentDate };
                    CoalitionFormation.Chamber chamber8 = Formateur.ChamberOf(de8, sim8.RoundReading(de8, round8), null, out IReadOnlyList<PoliticalParty> parties8);
                    var crafted2 = new FormationProposal { Formateur = "CDU" };
                    foreach (string k in new[] { "CDU", "CSU", "FDP", "Grune" }) { crafted2.CabinetParties.Add(k); }
                    ProposalVerdict verdict2 = Formateur.Answer(de8, crafted2, sim8.CurrentDate, sim8.World, sim8.RoundReading(de8, round8), null, de8.PlayerPartyAbbrev);
                    ConfidenceProcedure.MotionVote vote8 = ConfidenceProcedure.ConstructiveVote(de8, "CDU", crafted2, verdict2, chamber8, parties8);
                    DivisionSide fdp8 = null, grune8 = null;
                    foreach (DivisionSide s in vote8.Sides) { if (s.Abbrev == "FDP") { fdp8 = s; } if (s.Abbrev == "Grune") { grune8 = s; } }
                    Check(!vote8.PartnersAccept && fdp8 != null && fdp8.Side < 0 && fdp8.Reason.Contains("does not hold") && grune8 != null && grune8.Side < 0,
                        F("the review's defect 1: CDU+CSU+FDP+Grüne drafted, partners {0} - the FDP {1} ({2}), the Greens {3}", vote8.PartnersAccept ? "ACCEPT" : "refuse",
                            fdp8 == null ? "NOTHING" : fdp8.Side < 0 ? "does not elect" : "ELECTS", fdp8?.Reason, grune8 == null ? "NOTHING" : grune8.Side < 0 ? "do not elect" : "ELECT"));

                    // (f) elected, the draft not holding: the government is his party and its group alone (GroupAlone, Art. 63's rule), installed by Art. 67 - and
                    // a player's party seated only as the group's partner is recorded on it. Run through the two private steps (no planted chamber found makes
                    // MoveNoConfidence's own draft fail to hold - the redraft drops a staying partner - so the steps are driven directly)
                    (SimulationManager sim9, Country de9) = Open("ConstructiveVoteDiagnostic alone", "CSU");
                    Plant(de9);
                    var round9 = new SpeakerRound { MidTerm = true, OpenedOn = sim9.CurrentDate };
                    MethodInfo groupAlone = typeof(SimulationManager).GetMethod("GroupAlone", BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo install = typeof(SimulationManager).GetMethod("InstallSuccessor", BindingFlags.Instance | BindingFlags.NonPublic);
                    object[] aloneArgs = { de9, round9, "CDU", null, null };
                    var alone = groupAlone?.Invoke(sim9, aloneArgs) as FormationProposal;
                    var aloneVerdict = aloneArgs[3] as ProposalVerdict;
                    bool seated = aloneArgs[4] is bool b && b;
                    craftedVote.SeatedByGroup = seated;
                    GovernmentRecord before9 = de9.Government;
                    if (alone != null && aloneVerdict?.Investiture != null) { install?.Invoke(sim9, new object[] { de9, alone, aloneVerdict, craftedVote }); }
                    GovernmentRecord formed9 = de9.Government;
                    bool noted = formed9 != null && formed9.Breaks.Exists(x => x.Contains("parliamentary group's partner"));
                    Check(alone != null && !ReferenceEquals(formed9, before9) && formed9.PmParty == "CDU" && formed9.Cabinet.Count == 2 && formed9.Cabinet.Contains("CSU") && seated && noted && formed9.Basis.Contains("Art. 67"),
                        F("elected, the draft not holding: {0} led by {1} takes office (the basis naming Art. 67); the CSU player seated as the group's partner {2}",
                            formed9 != null ? string.Join("+", formed9.Cabinet) : "none", formed9?.PmParty, noted ? "- recorded on the government" : "- NOT RECORDED"));

                    // (f) §754: the SINCERE comparison decides where a party sits in neither government and refuses neither person - planted: the sitting
                    // chancellor the Linke's, governing alone, so the SPD, the Greens and the FDP sit outside both; each elects the CDU's candidate only where
                    // it is nearer him than the Linke's chancellor (the formation's compatibility), and its seats count exactly so
                    (SimulationManager sim7, Country de7) = Open("ConstructiveVoteDiagnostic sincere", "SSW");
                    Plant(de7);
                    de7.Government.PmParty = "Linke";
                    de7.Government.Cabinet.Clear(); de7.Government.Cabinet.Add("Linke");
                    de7.Government.Support.Clear();
                    var round7 = new SpeakerRound { MidTerm = true, OpenedOn = sim7.CurrentDate };
                    var draft7 = new FormationProposal { Formateur = "CDU" };
                    draft7.CabinetParties.Add("CDU"); draft7.CabinetParties.Add("CSU");
                    CoalitionFormation.Chamber chamber7 = Formateur.ChamberOf(de7, sim7.RoundReading(de7, round7), null, out IReadOnlyList<PoliticalParty> parties7);
                    ProposalVerdict verdict7 = Formateur.Answer(de7, draft7, sim7.CurrentDate, sim7.World, sim7.RoundReading(de7, round7), null, de7.PlayerPartyAbbrev);
                    ConfidenceProcedure.MotionVote vote7 = ConfidenceProcedure.ConstructiveVote(de7, "CDU", draft7, verdict7, chamber7, parties7);
                    int I7(string k) { for (int p = 0; p < parties7.Count; p++) { if (parties7[p].Abbrev == k) { return p; } } return -1; }
                    int sincereRight = 0, sincereChecked = 0, forSum = 0;
                    foreach (DivisionSide s in vote7.Sides)
                    {
                        if (s.Side > 0) { forSum += s.Seats; }
                        sb.Append(F("    side      {0} {1}: {2} - {3}\n", s.Abbrev, s.Seats, s.Side > 0 ? "elects" : s.Side < 0 ? "does not elect" : "abstains", s.Reason));
                        if (s.Reason == null || !s.Reason.Contains("nearer")) { continue; }
                        sincereChecked++;
                        int p = I7(s.Abbrev), m = I7("CDU"), c = I7("Linke");
                        bool nearerSuccessor = chamber7.Compatibility[p, m] > chamber7.Compatibility[p, c];
                        if ((s.Side > 0) == nearerSuccessor) { sincereRight++; }
                    }
                    Check(sincereChecked > 0 && sincereRight == sincereChecked && forSum == vote7.For && vote7.Carried == (vote7.For >= vote7.Needed),
                        F("§754: the Linke's chancellor planted - {0} part(ies) outside both governments vote by the comparison, each as its compatibility has it ({1} of {0}); {2} of {3} for the CDU's candidate, {4} needed - {5}",
                            sincereChecked, sincereRight, vote7.For, vote7.Members, vote7.Needed, vote7.Carried ? "elected" : "not elected"));

                    // (d) the AI moves it - the planted chamber, the player's party the SSW; the government formed the day before, so the AI weighs today
                    (SimulationManager sim3, Country de3) = Open("ConstructiveVoteDiagnostic ai", "SSW");
                    Plant(de3);
                    GovernmentRecord outgoing3 = de3.Government;
                    outgoing3.FormedOn = sim3.CurrentDate.AddDays(-1);
                    PlayerRole sswBefore = outgoing3.RoleOf("SSW");
                    MethodInfo tryAi = typeof(SimulationManager).GetMethod("TryAiMotion", BindingFlags.Instance | BindingFlags.NonPublic);
                    tryAi?.Invoke(sim3, new object[] { de3 });
                    Check(tryAi != null && !ReferenceEquals(de3.Government, outgoing3) && de3.Government.PmParty == "CDU" && de3.Government.Basis.Contains("Art. 67"),
                        F("the AI: on the planted chamber the CDU moves its own constructive vote and installs {0} led by {1}", de3.Government != null ? string.Join("+", de3.Government.Cabinet) : "none", de3.Government?.PmParty));
                    Check(de3.Government.RoleOf("SSW") == sswBefore && !de3.Government.Cabinet.Contains("SSW") && !de3.Government.Support.Contains("SSW"),
                        F("the review's defect 1: an AI successor never seats the player's party unasked - the SSW {0} before and {1} after", sswBefore, de3.Government.RoleOf("SSW")));
                    // a weekday that is not Monday and not the day after a formation: the AI does not weigh (the stated cadence)
                    (SimulationManager sim5, Country de5) = Open("ConstructiveVoteDiagnostic cadence", "SSW");
                    Plant(de5);
                    GovernmentRecord outgoing5 = de5.Government;
                    outgoing5.FormedOn = sim5.CurrentDate.AddDays(-30);
                    tryAi?.Invoke(sim5, new object[] { de5 });
                    Check(sim5.CurrentDate.DayOfWeek != DayOfWeek.Monday && ReferenceEquals(de5.Government, outgoing5),
                        F("the cadence: on {0:yyyy-MM-dd}, a {1} a month after the government formed, the AI weighs nothing - Mondays and the day after a formation only", sim5.CurrentDate, sim5.CurrentDate.DayOfWeek));

                    // (e) the review's defect 2: the player chancellor's open budget window closes when an AI successor takes office
                    (SimulationManager sim4, Country de4) = Open("ConstructiveVoteDiagnostic window", "SPD");
                    Plant(de4);
                    de4.Government.FormedOn = sim4.CurrentDate.AddDays(-1);
                    FieldInfo pendingField = typeof(SimulationManager).GetField("_pendingBudgetProcessByCountry", BindingFlags.Instance | BindingFlags.NonPublic);
                    var pending = pendingField?.GetValue(sim4) as HashSet<CountryId>;
                    pending?.Add(CountryId.Germany);
                    bool openBefore = sim4.GetPendingBudgetProcess(CountryId.Germany);
                    tryAi?.Invoke(sim4, new object[] { de4 });
                    Check(openBefore && de4.Government.PmParty == "CDU" && !sim4.GetPendingBudgetProcess(CountryId.Germany),
                        F("the review's defect 2: the SPD player's open budget window (open {0}) is closed when the CDU's successor takes office (the government {1}; the window {2})",
                            openBefore, de4.Government.PmParty, sim4.GetPendingBudgetProcess(CountryId.Germany) ? "STILL OPEN" : "closed"));
                }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw: ").Append((e.InnerException ?? e).GetType().Name).Append(": ").Append((e.InnerException ?? e).Message).Append('\n'); }
            finally { foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); } EnergyMarket.ResetTurnState(); }
            sb.Append(failures == 0 ? "    CLEAN\n" : F("    {0} failure(s)\n", failures));
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
