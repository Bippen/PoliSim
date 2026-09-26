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
    /// §646 (the formateur, POLITICAL_SYSTEM_SPEC.md §5.3: premises 1 and 4-8, the builder's R1-R8): THE SPEAKER'S ROUND, dated, on Sweden's start
    /// chamber and 2022's declarations. (1) An AI party asked: its proposal tabled when the consultation ends, the vote on the fourth day after, the
    /// government it installs carrying the proposal's posts, supporters and accepted demands, the vote recorded as a division. (2) The offer to the
    /// player's party (premise 5): declined, the party stays out and the asked party proposes without it or the Speaker moves on; accepted, the
    /// proposal is tabled with the player in it. (3) The player's party asked (premise 1): a proposal a partner refuses is not tabled (premise 3:
    /// revise and re-offer); a pass moves the Speaker to the next party, nothing counted. The limit and the loop to an extra election are
    /// ConfidenceDiagnostic's part 4.
    /// </summary>
    public static class SpeakerRoundDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== SpeakerRoundDiagnostic (§646): the Speaker's round, dated ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            var hosts = new List<GameObject>();
            (SimulationManager, Country) Open(string player)
            {
                var go = new GameObject("SpeakerRoundDiagnostic." + player); hosts.Add(go);
                WorldClock.ApplyStart(CountryId.Sweden);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country sweden = world.GetCountry(CountryId.Sweden);
                sweden.PlayerPartyAbbrev = player;
                sim.OpenSpeakerRound(sweden, ElectionVintage.Sweden2022, "for the check");
                return (sim, sweden);
            }
            void Days(SimulationManager sim, int n) { for (int d = 0; d < n; d++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); } }
            try
            {
                // (1) an AI party asked, S the player in opposition.
                (SimulationManager s1, Country c1) = Open("S");
                GovernmentRecord outgoing = c1.Government;
                SpeakerRound r1 = s1.RoundOf(CountryId.Sweden);
                DateTime opened = s1.CurrentDate;
                Check(r1 != null && r1.Order.Count > 0 && r1.Order[0] == "M" && r1.Asked == "M" && r1.Stage == RoundStage.Consulting && outgoing.Caretaker,
                    F("(1) the round opens: the order {0} (the formation's prime-minister party first, then the declared candidacies); M asked; the outgoing government a caretaker", r1 != null ? string.Join(", ", r1.Order) : "none"));
                Days(s1, SpeakerRound.ConsultationDays - 1);
                bool stillConsulting = r1.Stage == RoundStage.Consulting;
                Days(s1, 1);
                Check(stillConsulting && r1.Stage == RoundStage.VotePending && s1.CurrentDate == opened.AddDays(SpeakerRound.ConsultationDays) && r1.VoteOn == s1.CurrentDate.AddDays(SpeakerRound.VoteDays),
                    F("(1) the consultation ends on its seventh day ({0:yyyy-MM-dd}) and M tables {1}; the Riksdag votes on {2:yyyy-MM-dd} (RF 6 kap. 4 §)", s1.CurrentDate, r1.Proposal != null ? string.Join("+", r1.Proposal.CabinetParties) : "nothing", r1.VoteOn));
                // The tabled round rides the save (format 35), its proposal with it; and a demand delivered while the proposal waits on its vote changes
                // nothing - the demands as tabled are frozen on the proposal (the reader, §646).
                Persistence.SaveGame saved = Persistence.SaveGameService.CreateSaveGame(s1, s1.World, CountryId.Sweden, null);
                SpeakerRound back = Persistence.SaveGameService.Deserialize(Persistence.SaveGameService.Serialize(saved)).World.GetCountry(CountryId.Sweden).Government.Round;
                Check(back != null && back.Stage == RoundStage.VotePending && back.Proposal != null && string.Join("+", back.Proposal.CabinetParties) == string.Join("+", r1.Proposal.CabinetParties)
                      && back.Proposal.PostsOf("M") == r1.Proposal.PostsOf("M") && back.Proposal.Tabled.Count == r1.Proposal.Tabled.Count && back.VoteOn == r1.VoteOn,
                    F("(1) the tabled round rides the save (format 35): the proposal, its posts and its frozen demands, the vote's day {0:yyyy-MM-dd}", back?.VoteOn ?? DateTime.MinValue));
                string delivered = null;
                if (r1.Proposal.Tabled.TryGetValue("SD", out List<AgreementItem> sdTabled)) { AgreementItem law = sdTabled.Find(i => i.Kind == AgreementItemKind.Law); if (law != null) { c1.EnactedLaws.Add(new EnactedLaw { LawId = law.LawId }); delivered = law.LawId; } }
                Days(s1, SpeakerRound.VoteDays);
                GovernmentRecord installed = c1.Government;
                Check(delivered != null && installed != outgoing && installed.Support.Contains("SD"), F("(1) SD's demand '{0}' enacted while the proposal waited: SD still supports - the tabled demands were frozen", delivered ?? "none"));
                SupportAgreement sdAgreement = installed.AgreementOf("SD");
                DivisionRecord vote = c1.Divisions.Entries.Count > 0 ? c1.Divisions.Entries[c1.Divisions.Entries.Count - 1] : null;
                Check(installed != outgoing && !r1.Open && string.Join("+", installed.Cabinet) == "M+KD+L" && installed.Support.Contains("SD") && installed.PmParty == "M" && installed.FormedOn == s1.CurrentDate
                      && installed.Portfolios.Count > 0 && sdAgreement != null && sdAgreement.Items.Count > 0 && vote != null && vote.Motion && vote.Passed && vote.Title.StartsWith("Investiture", StringComparison.Ordinal),
                    F("(1) {0:yyyy-MM-dd}: the Riksdag approves; {1} with {2} takes office led by M, its posts {3}, SD's agreement of {4} demand(s); the vote a division ({5})",
                        s1.CurrentDate, string.Join("+", installed.Cabinet), string.Join("+", installed.Support), installed.PortfoliosOf("KD"), sdAgreement?.Items.Count ?? 0, vote?.Title ?? "none"));

                // (2) the offer: KD the player - M's proposal seats KD.
                (SimulationManager s2, Country c2) = Open("KD");
                SpeakerRound r2 = s2.RoundOf(CountryId.Sweden);
                Days(s2, SpeakerRound.ConsultationDays);
                Check(r2.Stage == RoundStage.OfferToPlayer && r2.Proposal != null && r2.Proposal.CabinetParties.Contains("KD"),
                    F("(2) M's proposal seats KD, the player's party: the offer waits on the player ({0} post(s) in {1})", r2.Proposal?.PostsOf("KD") ?? 0, r2.Proposal != null ? string.Join("+", r2.Proposal.CabinetParties) : "none"));
                s2.AnswerOffer(CountryId.Sweden, false, out string _);
                // On this chamber M cannot pass a cabinet without KD's seats (the chain of the round's own evaluator): the Speaker moves on, no vote, nothing counted.
                bool movedOrTabledWithout = r2.Stage == RoundStage.Consulting && r2.Asked != "M" && r2.Rejections == 0;
                Check(r2.Declines.Contains("KD>M") && movedOrTabledWithout,
                    F("(2) KD declines: it stays in opposition, and {0}", r2.Stage == RoundStage.VotePending ? "M tables " + string.Join("+", r2.Proposal.CabinetParties) + " without it" : "M cannot form without KD's seats - the Speaker asks " + r2.Asked));
                (SimulationManager s3, Country c3) = Open("KD");
                SpeakerRound r3 = s3.RoundOf(CountryId.Sweden);
                Days(s3, SpeakerRound.ConsultationDays);
                s3.AnswerOffer(CountryId.Sweden, true, out string _);
                Days(s3, SpeakerRound.VoteDays);
                Check(c3.Government.Cabinet.Contains("KD") && c3.Government.PmParty == "M" && !r3.Open, F("(2) KD accepts: the proposal is tabled and approved - {0}, KD holding {1}", string.Join("+", c3.Government.Cabinet), c3.Government.PortfoliosOf("KD")));

                // (3) the player's party asked: M the player.
                (SimulationManager s4, Country c4) = Open("M");
                SpeakerRound r4 = s4.RoundOf(CountryId.Sweden);
                Check(r4.Stage == RoundStage.PlayerAsked && r4.Asked == "M", "(3) M the player's party, first in the order: the Speaker asks it - the formation sheet, the clock waiting on the player");
                FormationProposal stingy = s4.DraftProposal(c4, r4, "M");
                if (stingy.Posts.TryGetValue("KD", out List<CabinetPortfolio> kdPosts) && kdPosts.Count > 0) { stingy.Posts["M"].AddRange(kdPosts); stingy.Posts["KD"] = new List<CabinetPortfolio>(); }
                bool tabledStingy = s4.SubmitFormation(CountryId.Sweden, stingy, out ProposalVerdict stingyVerdict, out string refusal);
                PartyAnswer kdAnswer = stingyVerdict?.Answers.Find(a => a.Party == "KD");
                Check(!tabledStingy && r4.Stage == RoundStage.PlayerAsked && kdAnswer != null && !kdAnswer.Accepts,
                    F("(3) M offers KD no post: KD refuses and the proposal is not tabled ({0}) - revise and offer again", refusal));
                Check(s4.PassFormation(CountryId.Sweden, out string _) && r4.Asked != "M" && r4.Rejections == 0 && r4.Stage == RoundStage.Consulting,
                    F("(3) M passes: the Speaker asks {0}, and nothing is counted against the limit", r4.Asked));

                // (4) an election held while a round is open ends it; the new election's round opens on the next day (the reader, §646).
                (SimulationManager s5, Country c5) = Open("S");
                SpeakerRound before = s5.RoundOf(CountryId.Sweden);
                DateTime heldOn = s5.CurrentDate;
                c5.ElectionHistory.Add(new ElectionRecord { Date = heldOn, CountryId = CountryId.Sweden.ToString(), Method = ElectionMethod.SwedenTwoTier });
                Days(s5, 1);
                SpeakerRound after = s5.RoundOf(CountryId.Sweden);
                Check(before != null && !before.Open && after != null && after != before && after.Occasion.Contains(heldOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    F("(4) the election of {0:yyyy-MM-dd} ends the open round, and its own round opens the day after: {1}", heldOn, after?.Occasion ?? "none"));

                // (5) an election held after an extra election was ordered, and before it: that election opens the round and the order lapses (the second
                // reader, §646 - waiting on, the government would govern the new chamber undischarged). The order is set as the verb sets it.
                (SimulationManager s6, Country c6) = Open("S");
                c6.Government.Round.Stage = RoundStage.Concluded;
                System.Reflection.FieldInfo extraDate = typeof(SimulationManager).GetField("_extraElectionDate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                System.Reflection.FieldInfo extraOn = typeof(SimulationManager).GetField("_extraElectionOrderedOn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                extraDate?.SetValue(s6, s6.CurrentDate.AddDays(60));
                extraOn?.SetValue(s6, s6.CurrentDate);
                Days(s6, 2);
                DateTime ordinary = s6.CurrentDate;
                c6.ElectionHistory.Add(new ElectionRecord { Date = ordinary, CountryId = CountryId.Sweden.ToString(), Method = ElectionMethod.SwedenTwoTier });
                Days(s6, 1);
                SpeakerRound lapsed = s6.RoundOf(CountryId.Sweden);
                Check(extraDate != null && lapsed != null && lapsed.Occasion.Contains(ordinary.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) && s6.ExtraElectionDate == DateTime.MinValue,
                    F("(5) an election held before the ordered extra election opens the round ({0}), and the order lapses", lapsed?.Occasion ?? "none"));
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); } EnergyMarket.ResetTurnState(); }
            if (failures > 0) { Debug.LogError($"SPEAKER ROUND: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
