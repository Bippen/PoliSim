using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §644 (PS-3i-2a, ruled 2026-09-26): AN AI SUPPORTER PAST ITS TOLERANCE WITHDRAWS, THEN MOVES NO CONFIDENCE IF THE MOTION WOULD CARRY AND IT PREFERS
    /// THE SUCCESSOR. Three parts, each saying what it proves.
    /// <para>(1) MEASURED for PS-3i-2c: on Sweden's start chamber with SD out of the support, SD's motion and the Speaker's round by date under three
    /// readings of the declarations - the sitting chamber's election's (as the game reads them, §607, §636), the timeline on the day (§621), and the
    /// timeline's pair lines and candidacies without the election platforms' rules. Asserted: the premise the ruling's question rests on, that the
    /// round as read today seats SD on no date before the election.</para>
    /// <para>(2) MEASURED for PS-3i-2b: the player as M tables every bill that would break SD's dial item; the divisions are printed.</para>
    /// <para>(3) ASSERTED, the ruled chain, on the chamber where the round seats SD (the fixture ConfidenceDiagnostic's cases use: the year-32 count
    /// seated by the 2026 election, the start's government in office): the player as M; the dial stood past its tolerance (the breach stood in, as
    /// PS-3i-2b is open) and read by the day's tracker; SD withdraws past its tolerated share; SD moves and the motion carries; the player's
    /// government asks to be discharged and falls to the successor, which seats SD.</para>
    /// </summary>
    public static class AiMotionReachDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== AiMotionReachDiagnostic (PS-3i-2a, §644): the AI supporter's withdrawal and motion ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            string Describe(GovernmentFormation.View v) => v == null || !v.HasGovernment ? "none" : string.Join("+", v.Cabinet.ConvertAll(c => c.Abbrev)) + (v.Support.Count > 0 ? " with " + string.Join("+", v.Support.ConvertAll(c => c.Abbrev)) : "");
            using IDisposable epoch = SimulationManager.EpochScope();
            var hosts = new List<GameObject>();
            (SimulationManager, Country) Open(string name, DateTime? epoch = null)
            {
                var go = new GameObject(name); hosts.Add(go);
                if (epoch.HasValue) { SimulationManager.SetEpoch(epoch.Value); } else { WorldClock.ApplyStart(CountryId.Sweden); }
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                return (sim, world.GetCountry(CountryId.Sweden));
            }
            try
            {
                Measure();
                Chain();
                RealGame();
                CAsSupport();
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); } EnergyMarket.ResetTurnState(); }
            if (failures > 0) { Debug.LogError($"AI MOTION REACH: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);

            // (1) and (2): the start's chamber.
            void Measure()
            {
                (SimulationManager sim, Country sweden) = Open("AiMotionReachDiagnostic.start");
                GovernmentRecord start = sweden.Government;
                var without = new GovernmentRecord { PmParty = start.PmParty };
                without.Cabinet.AddRange(start.Cabinet);
                foreach (string s in start.Support) { if (s != "SD") { without.Support.Add(s); } }
                sweden.Government = without;
                IReadOnlyList<PoliticalParty> ps = PartySystems.For(CountryId.Sweden);
                var seats = new int[ps.Count];
                for (int p = 0; p < ps.Count; p++) { seats[p] = sweden.ParliamentSeats.TryGetValue(ps[p].Abbrev, out int h) ? h : 0; }
                // §653: as the game reads it - the mid-term reading on today's date (the lines standing today, the platforms held to 2022's election).
                GovernmentFormation.View asRead = GovernmentFormation.ViewOfSitting(sweden, null, sim.CurrentDate);
                bool seatedAsRead = asRead.HasGovernment && asRead.Cabinet.Exists(c => c.Abbrev == "SD");
                ConfidenceProcedure.MotionVote sdAsRead = ConfidenceProcedure.Vote(sweden, "SD", sim.CurrentDate);
                bool ruledSeatsBeforeWeek = false;
                sb.Append(F("    as read   the sitting chamber's election's declarations: SD's motion {0}/{1}, the round {2}\n", sdAsRead.For, sdAsRead.Needed, Describe(asRead)));
                // Every stretch of the timeline between the start and polling day: the start, and each day a dated declaration starts or ends (the second reader).
                sim.TryPlayerPollingDay(out DateTime polling);
                var dates = new SortedSet<DateTime> { sim.CurrentDate.Date };
                foreach (DeclaredRedLines.DatedFact fact in DeclaredRedLines.SwedenTimeline)
                {
                    foreach (DateTime edge in new[] { fact.From, fact.Until })
                    {
                        if (edge > sim.CurrentDate && edge < polling) { dates.Add(edge.Date); }
                    }
                }
                bool platformlessSeatsBeforeWeek = false;
                foreach (DateTime d in dates)
                {
                    GovernmentFormation.View dated = GovernmentFormation.ViewOfSitting(sweden, null, d);
                    ConfidenceProcedure.MotionVote v = ConfidenceProcedure.Vote(sweden, "SD", d);
                    CoalitionResult platformless = CoalitionFormation.Form(seats, GovernmentFormation.Compatibility(ps), DeclaredRedLines.ForDate(CountryId.Sweden, ps, d), true, new List<InOrAgainst>());
                    string pl = platformless.Outcome == CoalitionOutcomeKind.NewElection ? "none"
                        : string.Join("+", CoalitionFormation.Members(platformless.Government.Cabinet, ps.Count).ConvertAll(i => ps[i].Abbrev))
                          + (platformless.Government.Support != 0 ? " with " + string.Join("+", CoalitionFormation.Members(platformless.Government.Support, ps.Count).ConvertAll(i => ps[i].Abbrev)) : "");
                    bool beforeWeek = d < polling.AddDays(-ConfidenceProcedure.ExtraElectionWindowDays);
                    if (beforeWeek && dated.HasGovernment && dated.Cabinet.Exists(c => c.Abbrev == "SD")) { ruledSeatsBeforeWeek = true; }
                    if (beforeWeek && platformless.Outcome != CoalitionOutcomeKind.NewElection && CoalitionFormation.Members(platformless.Government.Cabinet, ps.Count).Exists(i => ps[i].Abbrev == "SD")) { platformlessSeatsBeforeWeek = true; }
                    sb.Append(F("    by date   {0:yyyy-MM-dd}{5}: SD's motion {1}/{2}; the round on the day's timeline {3}; without the platforms' rules {4}\n", d, v.For, v.Needed, Describe(dated), pl, beforeWeek ? "" : " (in the week before polling day)"));
                }
                sb.Append(F("    measured  without the platforms' rules, the round seats SD before the week that takes up no motion: {0}\n", platformlessSeatsBeforeWeek ? "YES" : "no"));
                sweden.Government = start;
                Check(!seatedAsRead && sdAsRead.Carried && !ruledSeatsBeforeWeek,
                    F("(1) PS-3i-2c as ruled (§653: the lines standing on the day, the platforms held to the sitting election): SD's motion carries, and the round ({0}) seats SD on no day before the week that takes up no motion - from the start's chamber an AI motion stays out of reach before the 2026 election", Describe(asRead)));

                sweden.PlayerPartyAbbrev = "M";
                AgreementItem dial = start.AgreementOf("SD")?.Items.Find(i => i.Kind == AgreementItemKind.Dial && i.Dial == AgreementDial.BorderEnforcement);
                if (dial == null) { sb.Append("    measured  SD's agreement holds no border dial item - PS-3i-2b not measured\n"); return; }
                int before = sweden.Divisions.Entries.Count;
                sim.IntroduceCrimeJusticeBill(CountryId.Sweden, new CrimeJusticePolicyBill
                {
                    PoliceFunding = sweden.PoliceFundingLevel, SentencingSeverity = sweden.SentencingSeverity, BailReform = sweden.BailReformLevel,
                    DrugPolicy = sweden.DrugPolicyLevel, JudicialFunding = sweden.JudicialFundingLevel, BorderEnforcement = dial.StartValue - SupportAgreement.DialBreakTolerance - 1f,
                });
                foreach (LawDefinition def in LawCatalog.All)
                {
                    if (def.BorderEnforcementDelta < -SupportAgreement.DialBreakTolerance && LawCatalog.IsWithinCompetence(sim.World, sweden, def)) { sim.IntroduceLawBill(CountryId.Sweden, new LawBill { LawId = def.Id, IsRepeal = false }); }
                }
                for (int day = 0; day < ParliamentSystem.BillDurationDays + 3; day++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); }
                for (int i = before; i < sweden.Divisions.Entries.Count; i++)
                {
                    DivisionRecord d = sweden.Divisions.Entries[i];
                    int yes = 0, no = 0; foreach (DivisionSide side in d.Sides) { if (side.Side > 0) { yes += side.Seats; } else if (side.Side < 0) { no += side.Seats; } }
                    var sides = new List<string>(); foreach (DivisionSide side in d.Sides) { sides.Add(side.Abbrev + (side.Side > 0 ? " for" : side.Side < 0 ? " against" : " abstains")); }
                    sb.Append(F("    measured  {0:yyyy-MM-dd} the player as M tables '{1}' - {2}, {3} for, {4} against: {5} (PS-3i-2b)\n", d.Date, d.Title, d.Passed ? "PASSED" : "FAILED", yes, no, string.Join(", ", sides)));
                }
                sb.Append(F("    measured  SD's dial item after the breaching bills: {0}\n", dial.State));
            }

            // (5) PS-3i-2a (c), ruled 2026-09-29: MEASURED ONCE, ASSERTED NEITHER WAY - after the 2026 election, an M-led cabinet with C as its only
            // support, on the chamber the election seated and the election's own day's declarations (§653); every C demand accepted. Two M-led
            // cabinets: M+KD+L (the outgoing cabinet less SD's role) and M alone. The answers and the investiture's count are logged; nothing is built.
            void CAsSupport()
            {
                (SimulationManager sim3, Country after) = Open("AiMotionReachDiagnostic.csupport", new DateTime(2026, 10, 1));
                after.PlayerPartyAbbrev = "M";
                foreach (string[] cabinet in new[] { new[] { "M", "KD", "L" }, new[] { "M" } })
                {
                    var proposal = new FormationProposal { Formateur = "M" };
                    proposal.CabinetParties.AddRange(cabinet);
                    foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in GovernmentRecord.GamsonPosts(after, proposal.CabinetParties, "M")) { proposal.Posts[kv.Key] = kv.Value; }
                    proposal.Supporters.Add("C");
                    proposal.FreezeTabled(after, sim3.CurrentDate, sim3.World);
                    var keys = new List<string>();
                    foreach (AgreementItem item in proposal.TabledOf(after, "C", sim3.CurrentDate, sim3.World)) { keys.Add(SupportAgreement.KeyOf(item)); }
                    proposal.AcceptedDemands["C"] = keys;
                    ProposalVerdict verdict = Formateur.Answer(after, proposal, sim3.CurrentDate, sim3.World, DeclarationReading.OfElection(CountryId.Sweden, new DateTime(2026, 9, 13)), null, "M");
                    var answers = new List<string>();
                    foreach (PartyAnswer a in verdict.Answers) { answers.Add(a.Party + (a.Accepts ? " accepts" : " refuses (" + a.Reason + ")")); }
                    CoalitionFormation.CabinetEvaluation inv = verdict.Investiture;
                    sb.Append(F("    c-support {0} with C's support, {1} C demand(s) accepted: {2}; investiture {3}; the proposal {4}\n",
                        string.Join("+", cabinet), keys.Count, string.Join("; ", answers.ToArray()),
                        inv == null ? "not reached" : F("cabinet {0} seats, with support {1}, opposed {2} of 349 - {3}", inv.CabinetSeats, inv.SupportedSeats, inv.OpposedSeats, inv.Wins ? "WINS" : "LOSES"),
                        verdict.Passes ? "PASSES" : "FAILS" + (string.IsNullOrEmpty(verdict.Reason) ? string.Empty : " (" + verdict.Reason + ")")));
                    Check(verdict.Answers.Count > 0, F("(5) measured: {0} with C's support answered ({1}) - recorded, asserted neither way", string.Join("+", cabinet), verdict.Passes ? "passes" : "fails"));
                }
            }

            // (4) PS-3i-2a IN A REAL GAME (ruled 2026-09-29, §655: as M, deliver nothing, watch SD withdraw, the motion carry and the government fall), measured
            // on the game's own calendar and chamber - its two preconditions: SD must be a supporter across two budget votes (PS-3i-2b as ruled, §654), and
            // the round must seat SD outside the week before a polling day (PS-3i-2c, §653).
            void RealGame()
            {
                // Before the 2026 election: the real day path from the start, the player as M tabling the standing budget whenever a window opens and
                // delivering nothing; every budget vote the game holds before polling day counted.
                (SimulationManager sim, Country sweden) = Open("AiMotionReachDiagnostic.real");
                sweden.PlayerPartyAbbrev = "M";
                sim.TryPlayerPollingDay(out DateTime polling);
                var noDecisions = new Dictionary<CountryId, PolicyDecision>();
                int budgetVotes = 0, guard = 0;
                while (sim.CurrentDate < polling && guard++ < 400)
                {
                    if (sim.GetPendingBudgetProcess(CountryId.Sweden)) { sim.IntroduceBudgetBill(CountryId.Sweden, new BudgetBill()); }
                    int divisionsBefore = sweden.Divisions.Entries.Count;
                    if (sim.AdvanceDay()) { sim.AdvanceTurn(noDecisions); }
                    sim.AdvanceCountryDayTick(CountryId.Sweden);
                    for (int i = divisionsBefore; i < sweden.Divisions.Entries.Count; i++) { if (sweden.Divisions.Entries[i].Title.StartsWith("Annual budget", StringComparison.Ordinal)) { budgetVotes++; } }
                }
                SupportAgreement sd = sweden.Government?.AgreementOf("SD");
                int owedThrough = 0;
                if (sd != null) { foreach (AgreementItem item in sd.Items) { if (item.State == AgreementState.Owed) { owedThrough = Math.Max(owedThrough, item.BudgetVotesOwed); } } }
                sb.Append(F("    real      {0:yyyy-MM-dd} to polling day {1:yyyy-MM-dd} as M, nothing delivered: {2} budget vote(s) held; SD's owed items waited through at most {3}; SD {4}\n",
                    SimulationManager.EpochDate, polling, budgetVotes, owedThrough, sweden.Government != null && sweden.Government.Support.Contains("SD") ? "still supports" : "no longer supports"));
                Check(budgetVotes < SupportAgreement.BudgetVotesToBreak && sweden.Government != null && sweden.Government.Support.Contains("SD") && sweden.Government.NoConfidenceOn == DateTime.MinValue,
                    F("(4) before the 2026 election the game holds {0} budget vote(s) - fewer than the {1} an owed item waits through: nothing breaks, SD supports on, no motion", budgetVotes, SupportAgreement.BudgetVotesToBreak));

                // After the election: the chamber the 2026 election seated, M proposing M+KD+L with SD's support and every SD demand accepted, read on the
                // election's own day (§653) - SD's answer.
                (SimulationManager sim2, Country after) = Open("AiMotionReachDiagnostic.real2026", new DateTime(2026, 10, 1));
                after.PlayerPartyAbbrev = "M";
                var proposal = new FormationProposal { Formateur = "M" };
                proposal.CabinetParties.AddRange(new[] { "M", "KD", "L" });
                foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in GovernmentRecord.GamsonPosts(after, proposal.CabinetParties, "M")) { proposal.Posts[kv.Key] = kv.Value; }
                proposal.Supporters.Add("SD");
                proposal.FreezeTabled(after, sim2.CurrentDate, sim2.World);
                var keys = new List<string>();
                foreach (AgreementItem item in proposal.TabledOf(after, "SD", sim2.CurrentDate, sim2.World)) { keys.Add(SupportAgreement.KeyOf(item)); }
                proposal.AcceptedDemands["SD"] = keys;
                ProposalVerdict verdict = Formateur.Answer(after, proposal, sim2.CurrentDate, sim2.World, DeclarationReading.OfElection(CountryId.Sweden, new DateTime(2026, 9, 13)), null, "M");
                PartyAnswer sdAnswer = verdict.Answers.Find(a => a.Party == "SD");
                sb.Append(F("    real      after the 2026 election, M+KD+L with SD's support offered every demand: SD {0} - {1}\n", sdAnswer == null ? "not asked" : sdAnswer.Accepts ? "accepts" : "refuses", sdAnswer?.Reason ?? "-"));
                Check(sdAnswer != null && !sdAnswer.Accepts,
                    F("(4) after the 2026 election SD refuses the support role it would need to hold ({0}) - the chain as ruled has no supporter to withdraw", sdAnswer?.Reason ?? "not asked"));
            }

            // (3): the ruled chain, on the chamber where the round seats SD.
            void Chain()
            {
                // §653: the chamber sits after its election - the world opens on 1 October 2026 (the epoch), so the mid-term reading reads its own day.
                (SimulationManager sim, Country sweden) = Open("AiMotionReachDiagnostic.chain", new DateTime(2026, 10, 1));
                sweden.PlayerPartyAbbrev = "M";
                InstallKristersson(sweden, sim);
                sweden.ParliamentSeats.Clear();
                foreach ((string abbrev, int held) in new[] { ("S", 94), ("SD", 63), ("M", 70), ("V", 27), ("C", 24), ("KD", 27), ("MP", 24), ("L", 20) }) { sweden.ParliamentSeats[abbrev] = held; }
                sweden.ElectionHistory.Add(new ElectionRecord { Date = new DateTime(2026, 9, 13), CountryId = CountryId.Sweden.ToString(), Method = ElectionMethod.SwedenTwoTier });
                sweden.PlayerPartyAbbrev = "M";
                GovernmentRecord start = sweden.Government;
                SupportAgreement sd = start.AgreementOf("SD");
                AgreementItem dial = sd?.Items.Find(i => i.Kind == AgreementItemKind.Dial);
                GovernmentFormation.View round = GovernmentFormation.ViewOfSitting(sweden, null, sim.CurrentDate);
                Check(start.PmParty == "M" && start.Support.Contains("SD") && sim.PlayerGoverns(sweden) && dial != null && round.HasGovernment && round.Cabinet.Exists(c => c.Abbrev == "SD"),
                    F("(3) the premise: M leads {0} with SD's support, the player governs, and the round on this chamber would seat SD ({1})", string.Join("+", start.Cabinet), Describe(round)));
                if (dial == null) { return; }

                // The breach stood in on the dial - the dial past its item's tolerance, read by the day's tracker - and, where the share asks for more, law items marked broken.
                if (dial.Dial == AgreementDial.BorderEnforcement) { sweden.BorderEnforcementLevel = dial.StartValue - SupportAgreement.DialBreakTolerance - 1f; } else { sweden.PoliceFundingLevel = dial.StartValue - SupportAgreement.DialBreakTolerance - 1f; }
                int needed = (int)Math.Floor(SupportAgreement.BrokenShareTolerated * sd.Items.Count) + 1;
                foreach (AgreementItem item in sd.Items) { if (needed - 1 <= 0) { break; } if (item.Kind == AgreementItemKind.Law) { item.State = AgreementState.Broken; item.BrokenOn = sim.CurrentDate; needed--; } }
                sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden);
                Check(dial.State == AgreementState.Broken && dial.BrokenOn == sim.CurrentDate, F("(3) {0:yyyy-MM-dd}: the dial at {1:0}, past its item's tolerance - the day's tracker breaks it the same day", dial.BrokenOn, SupportAgreement.DialValue(sweden, dial.Dial)));
                Check(sd.PastTolerance() && !start.Support.Contains("SD") && start.Breaks.Exists(b => b.Contains("SD withdrew its support over")),
                    F("(3) {0} broken of {1}, past the tolerated share ({2}): SD withdraws its support", sd.Count(AgreementState.Broken), sd.Items.Count, SupportAgreement.BrokenShareTolerated));
                DivisionRecord motion = sweden.Divisions.Entries.Count > 0 ? sweden.Divisions.Entries[sweden.Divisions.Entries.Count - 1] : null;
                Check(start.NoConfidenceMover == "SD" && start.NoConfidenceOn == sim.CurrentDate && motion != null && motion.Motion && motion.Passed,
                    F("(3) the same day SD moves no confidence and it carries: {0}", motion?.Title ?? "no division"));
                if (start.NoConfidenceMover != "SD") { return; }
                // §646: the discharge opens the Speaker's round; the player's party is asked first and tables the formation's proposal, and the
                // Riksdag votes on the fourth day.
                bool discharged = sim.AskToBeDischarged(CountryId.Sweden, out string whyNot);
                SpeakerRound roundAfter = sim.RoundOf(CountryId.Sweden);
                string submission = "not asked";
                if (roundAfter != null && roundAfter.Stage == RoundStage.PlayerAsked)
                {
                    bool tabled = sim.SubmitFormation(CountryId.Sweden, sim.DraftProposal(sweden, roundAfter, sweden.PlayerPartyAbbrev), out ProposalVerdict submitted, out string notTabled);
                    var refused = new List<string>();
                    if (submitted != null) { foreach (PartyAnswer a in submitted.Answers) { if (!a.Accepts) { refused.Add(a.Party + " " + a.Reason); } } }
                    submission = tabled ? "tabled" : notTabled + (refused.Count > 0 ? ": " + string.Join("; ", refused) : string.Empty);
                }
                sb.Append("    submitted ").Append(submission).Append('\n');
                for (int d = 0; d < 60 && sweden.Government == start; d++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); }
                Check(discharged && start.Caretaker && sweden.Government != start && sweden.Government.Cabinet.Contains("SD"),
                    F("(3) the player's government asks to be discharged and falls: a caretaker until the Speaker's round installs {0} led by {1}, which seats SD ({2})", string.Join("+", sweden.Government.Cabinet), sweden.Government.PmParty, whyNot ?? "discharged"));
            }
        }

        /// <summary>
        /// §653: the year-32 fixtures sit after the 2026 election (the world opens on 1 October 2026), where no government of record is installed;
        /// the fixture installs Kristersson's shape - M+KD+L with SD's support, SD's demands to M tabled and all accepted - through the round's own
        /// builder (`GovernmentRecord.FromProposal`), so the chain runs on the government it was written for.
        /// </summary>
        private static void InstallKristersson(Country c, SimulationManager s)
        {
            var proposal = new FormationProposal { Formateur = "M" };
            proposal.CabinetParties.AddRange(new[] { "M", "KD", "L" });
            foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in GovernmentRecord.GamsonPosts(c, proposal.CabinetParties, "M")) { proposal.Posts[kv.Key] = kv.Value; }
            proposal.Supporters.Add("SD");
            proposal.FreezeTabled(c, s.CurrentDate, s.World);
            var keys = new List<string>();
            foreach (AgreementItem item in proposal.TabledOf(c, "SD", s.CurrentDate, s.World)) { keys.Add(SupportAgreement.KeyOf(item)); }
            proposal.AcceptedDemands["SD"] = keys;
            c.Government = GovernmentRecord.FromProposal(c, proposal, new[] { "SD" }, CoalitionOutcomeKind.ConfidenceAndSupply, s.CurrentDate,
                "the fixture (§653): M+KD+L with SD's support, standing on 1 October 2026", WorldClock.ExecutiveKind.Cabinet, "Ulf Kristersson (M)", null, s.World);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
