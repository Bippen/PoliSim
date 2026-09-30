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
    /// division, carried exactly when the count reaches the majority and the partners accept, and - not carried - the government standing untouched;
    /// (c) THE PLANTED CHAMBER, the CDU and CSU holding a majority alone: the vote carries and the successor's government takes office the same day -
    /// the CDU's chancellor, no caretaker, no declaration's week, the basis naming Art. 67, the chancellor's own party having voted against; (d) the AI:
    /// on the planted chamber, the player's party elsewhere, the CDU moves its own constructive vote by ruling (2)'s rule and installs its government.
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
                    Check(before != null && projected != null && projected.Constructive && projected.Carried == (projected.For >= projected.Needed && projected.PartnersAccept),
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
                    // the review's defects 3 and 4: a sitting cabinet partner drafted into the successor weighs its posts (the FDP stays), and no refusing
                    // party is counted for the successor
                    DivisionSide fdpSide = null; int refusersFor = 0;
                    if (vote2 != null) { foreach (DivisionSide s in vote2.Sides) { if (s.Abbrev == "FDP") { fdpSide = s; } if (vote2.Refusers.Contains(s.Abbrev) && s.Side > 0) { refusersFor++; } } }
                    Check(vote2 != null && refusersFor == 0 && (!vote2.SuccessorSupport.Contains("FDP") && !vote2.SuccessorCabinet.Contains("FDP") || (fdpSide != null && fdpSide.Side < 0 && vote2.Refusers.Contains("FDP") && !formed.Support.Contains("FDP") && !formed.Cabinet.Contains("FDP"))),
                        F("a sitting partner weighs its posts and a refusing party votes no one in: the FDP, drafted {0}, votes {1} ({2}); {3} refuser(s) counted for; the installed government carries no refuser",
                            vote2 != null && vote2.SuccessorCabinet.Contains("FDP") ? "into the cabinet" : vote2 != null && vote2.SuccessorSupport.Contains("FDP") ? "as a supporter" : "not at all",
                            fdpSide == null ? "NOTHING" : fdpSide.Side < 0 ? "against" : fdpSide.Side > 0 ? "FOR" : "abstaining", fdpSide?.Reason, refusersFor));

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
