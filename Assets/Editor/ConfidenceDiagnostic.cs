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
    /// PS-3i (§636): CONFIDENCE AND COLLAPSE, ASSERTED - the spec's stage-3 "done when": a supporter's withdrawal can bring a government down by the country's
    /// own procedure. At Sweden's start SD (the Tidö support party) cannot move no confidence while it supports; it withdraws, moves the motion, and the vote
    /// is the chamber's by its declared lines (13 kap. 4 §: more than half of the members); carried, the government's week runs, the Speaker discharges it at
    /// the week's end into a caretaker (6 kap. 7 §, 9 §) and the round on the sitting chamber forms the next government or orders an extra election (6 kap. 5 §).
    /// Both ways around the edges: the prime minister's party, a junior partner and a support party move nothing; a party under a tenth of the members moves
    /// nothing; a caretaker faces no motion; the player's government ordering an extra election within the week is not discharged, and the polling day moves.
    /// Ruling (3) (§640): the procedure resumes after an election that forms no government while a caretaker serves. Ruling (2) (§641): an AI party moves
    /// no confidence only when the motion would carry and the round would seat it; a supporter past its agreement's tolerance withdraws and is the first mover asked (PS-3i-2a, §644).
    /// </summary>
    public static class ConfidenceDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== ConfidenceDiagnostic (PS-3i, §636): confidence and collapse by the Riksdag's procedure ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();

            // 1. The done-when: SD withdraws and brings the government down.
            var go = new GameObject("ConfidenceDiagnostic");
            try
            {
                (SimulationManager sim, Country sweden) = Open(go);
                sweden.PlayerPartyAbbrev = "SD";
                Check(!sim.MoveNoConfidence(CountryId.Sweden, out string refused, out _) && refused == "WITHDRAW YOUR SUPPORT FIRST", F("SD in support moves nothing: {0}", refused));
                Check(sim.WithdrawSupport(CountryId.Sweden, out refused), "SD withdraws its support");
                Check(sim.MoveNoConfidence(CountryId.Sweden, out refused, out ConfidenceProcedure.MotionVote vote) && vote != null, F("SD, now in opposition, moves no confidence ({0})", refused ?? "taken up"));
                if (vote != null)
                {
                    sb.Append("    vote      ").Append(vote.Title()).Append('\n');
                    foreach (DivisionSide side in vote.Sides) { sb.Append("      ").Append(side.Abbrev).Append(' ').Append(side.Seats).Append(side.Side > 0 ? " FOR" : side.Side < 0 ? " AGAINST" : " ABSTAINS").Append(" - ").Append(side.Reason).Append('\n'); }
                    Check(vote.Carried, F("the motion carries: {0} of {1} members, {2} needed", vote.For, vote.Members, vote.Needed));
                }
                Persistence.SaveGame carriedSave = Persistence.SaveGameService.CreateSaveGame(sim, sim.World, CountryId.Sweden, null);
                Persistence.SaveGame carriedBack = Persistence.SaveGameService.Deserialize(Persistence.SaveGameService.Serialize(carriedSave));
                GovernmentRecord carriedRecord = carriedBack.World.GetCountry(CountryId.Sweden).Government;
                Check(carriedRecord.NoConfidenceOn == sim.CurrentDate && carriedRecord.NoConfidenceMover == "SD" && carriedBack.World.GetCountry(CountryId.Sweden).Divisions.Entries.Exists(e => e.Motion && e.Passed),
                    "the carried motion rides the save: its date, its mover, its division marked a motion (format 33)");
                GovernmentRecord fallen = sweden.Government;
                string fallenCabinet = string.Join("+", fallen.Cabinet);
                Check(fallen.NoConfidenceOn == sim.CurrentDate && !fallen.Caretaker, "the declaration is recorded; the government's week runs - not yet discharged");
                for (int d = 0; d < ConfidenceProcedure.ExtraElectionWindowDays + 1; d++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); }
                GovernmentRecord after = sweden.Government;
                bool reformed = after != fallen && !after.Caretaker;
                bool extra = after == fallen && fallen.Caretaker && sim.ExtraElectionDate != DateTime.MinValue;
                Check(reformed || extra, F("the week ran out: {0}", reformed ? "the Speaker's round formed " + string.Join("+", after.Cabinet) + " led by " + after.PmParty + (after.Support.Count > 0 ? " with " + string.Join("+", after.Support) : string.Empty) + " (the fallen " + fallenCabinet + ")" : extra ? "no government formed - an extra election on " + sim.ExtraElectionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "NOTHING FOLLOWED"));
                Check(fallen.Caretaker, "the fallen government was discharged into a caretaker (6 kap. 9 §)");
                Check(!reformed || !after.Support.Contains("SD"), "the round does not hand SD's support back to the prime minister it brought down");
                if (reformed)
                {
                    Check(after.StandingRefusals.Contains("SD>" + fallen.PmParty), "SD's refusal stands on the government the round formed, until the next election (§641)");
                    Persistence.SaveGame refusedSave = Persistence.SaveGameService.CreateSaveGame(sim, sim.World, CountryId.Sweden, null);
                    Check(Persistence.SaveGameService.Deserialize(Persistence.SaveGameService.Serialize(refusedSave)).World.GetCountry(CountryId.Sweden).Government.StandingRefusals.Contains("SD>" + fallen.PmParty), "the standing refusal rides the save");
                    for (int d = 0; d < 14; d++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); }
                    Check(after.NoConfidenceOn == DateTime.MinValue && sweden.Government.PmParty != fallen.PmParty, "two weeks on: no AI motion restored the prime minister SD brought down");
                }
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { UnityEngine.Object.DestroyImmediate(go); EnergyMarket.ResetTurnState(); }

            // 2. The edges.
            var go2 = new GameObject("ConfidenceDiagnostic.2");
            try
            {
                (SimulationManager sim, Country sweden) = Open(go2);
                sweden.PlayerPartyAbbrev = "M";
                Check(!sim.MoveNoConfidence(CountryId.Sweden, out string refused, out _) && refused == "THE GOVERNMENT MOVES NO MOTION AGAINST ITSELF", F("the prime minister's party moves nothing: {0}", refused));
                sweden.PlayerPartyAbbrev = "KD";
                Check(!sim.MoveNoConfidence(CountryId.Sweden, out refused, out _) && refused == "LEAVE THE GOVERNMENT FIRST", F("a junior partner moves nothing: {0}", refused));
                Check(sim.LeaveGovernment(CountryId.Sweden, out refused) && !sweden.Government.Cabinet.Contains("KD") && sweden.Government.RoleOf("KD") == PlayerRole.Opposition, "KD leaves the government - out of the cabinet, in opposition");
                int held = 0; foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in sweden.Government.Portfolios) { held += kv.Value.Count; }
                Check(held == 6 && !sweden.Government.Portfolios.ContainsKey("KD"), "the six portfolios re-apportioned among those who stay");
                Check(!sim.MoveNoConfidence(CountryId.Sweden, out refused, out _) && refused.StartsWith("A MOTION NEEDS A TENTH", StringComparison.Ordinal), F("KD, under a tenth of the members, moves nothing: {0}", refused));

                // The player's government answers a declaration with an extra election within the week: no discharge, the polling day moves.
                sweden.PlayerPartyAbbrev = "M";
                sweden.Government.NoConfidenceOn = sim.CurrentDate;
                DateTime ordinary = default;
                Check(sim.OrderExtraElection(CountryId.Sweden, out refused) && sim.ExtraElectionDate != DateTime.MinValue && sim.ExtraElectionDate.DayOfWeek == DayOfWeek.Sunday && sim.ExtraElectionDate <= sim.CurrentDate.AddMonths(3), F("M orders an extra election within the week: {0:yyyy-MM-dd} (a Sunday within three months)", sim.ExtraElectionDate));
                Check(sim.TryPlayerPollingDay(out ordinary) && ordinary == sim.ExtraElectionDate, "the extra election is the next polling day");
                Check(sweden.Government.NoConfidenceOn == DateTime.MinValue && !sweden.Government.Caretaker, "an extra election ordered - no discharge follows (6 kap. 7 §)");
                sweden.PlayerPartyAbbrev = "S";
                Check(!sim.MoveNoConfidence(CountryId.Sweden, out refused, out _) && refused.StartsWith("NO MOTION IS TAKEN UP BETWEEN", StringComparison.Ordinal), F("no motion between an extra election's decision and the new Riksdag: {0}", refused));
                sweden.Government.Caretaker = true;
                Check(!sim.MoveNoConfidence(CountryId.Sweden, out refused, out _), "no motion against a caretaker");

                // The record and the pending election ride the save.
                Persistence.SaveGame save = Persistence.SaveGameService.CreateSaveGame(sim, sim.World, CountryId.Sweden, null);
                Persistence.SaveGame back = Persistence.SaveGameService.Deserialize(Persistence.SaveGameService.Serialize(save));
                Check(back.World.GetCountry(CountryId.Sweden).Government.Caretaker && back.Sim.ExtraElectionDate == sim.ExtraElectionDate && back.Sim.ExtraElectionOrderedOn == sim.CurrentDate, "the caretaker and the extra election ride the save (format 33)");
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { UnityEngine.Object.DestroyImmediate(go2); EnergyMarket.ResetTurnState(); }

            // 3. An extra election inside the ordinary election's run-up (the reader, s636): the campaign state dated to it ends with it, and the
            //    ordinary run-up re-begins on its own record - walked day by day into the ordinary campaign, with no throw.
            var go3 = new GameObject("ConfidenceDiagnostic.3");
            try
            {
                (SimulationManager sim, Country sweden) = Open(go3);
                sweden.PlayerPartyAbbrev = "M";
                sweden.Government.NoConfidenceOn = sim.CurrentDate;
                Check(sim.OrderExtraElection(CountryId.Sweden, out string refused), F("M orders an extra election in the run-up: {0:yyyy-MM-dd}", sim.ExtraElectionDate));
                DateTime extraDay = sim.ExtraElectionDate;
                Check(sim.TryPlayerPollingDay(out DateTime ordinaryBefore) && ordinaryBefore == extraDay, "the extra election is the next polling day");
                int days = 0; string threw = null;
                try { for (; days < 240 && threw == null; days++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); } }
                catch (Exception e) { threw = e.GetType().Name + " on " + sim.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ": " + e.Message; }
                Check(threw == null, F("walked {0} days past the extra election with no throw{1}", days, threw != null ? " - " + threw : string.Empty));
                Check(sim.ExtraElectionDate == DateTime.MinValue && sim.TryPlayerPollingDay(out DateTime ordinaryAfter) && ordinaryAfter > extraDay, "after the extra election the ordinary polling day is next again");
                Check(sim.CampaignRecord == null || sim.CampaignRecord.ElectionDate != extraDay, F("no campaign record is left dated to the extra election ({0})", sim.CampaignRecord != null ? sim.CampaignRecord.ElectionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "none"));
                Check(sweden.Government.NoConfidenceOn == DateTime.MinValue, "no AI motion fired in the 240 days walked (on the start's chamber the round seats no mover whose motion carries, and nothing is broken - §641, §644)");
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { UnityEngine.Object.DestroyImmediate(go3); EnergyMarket.ResetTurnState(); }

            // 4. PS-3i ruling (3) (2026-09-25): after an election that forms no government the appointment procedure restarts as RF 6 kap. 5 § requires -
            //    resumed once per election, an extra election within three months, the loop turning until a government forms or an ordinary election
            //    falls within the three months. An election that formed none is one that left the caretaker's record in place (the game installs any
            //    government that forms), so the check records each election as held and leaves the record as it is.
            var go4 = new GameObject("ConfidenceDiagnostic.4");
            try
            {
                (SimulationManager sim, Country sweden) = Open(go4);
                sweden.PlayerPartyAbbrev = "S";
                GovernmentRecord care = sweden.Government;
                care.Caretaker = true;
                care.CaretakerSince = sim.CurrentDate;
                void HoldFormingNone() => sweden.ElectionHistory.Add(new ElectionRecord { Date = sim.CurrentDate, CountryId = CountryId.Sweden.ToString(), Method = ElectionMethod.SwedenTwoTier });
                void Day() { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); }
                // An election BEFORE the discharge is not one the procedure waits for.
                sweden.ElectionHistory.Add(new ElectionRecord { Date = sim.CurrentDate.AddDays(-1), CountryId = CountryId.Sweden.ToString(), Method = ElectionMethod.SwedenTwoTier });
                Day();
                Check(sim.ExtraElectionDate == DateTime.MinValue && care.ProcedureResumedAfter == DateTime.MinValue, "a caretaker with only an election before its discharge: nothing resumes");
                var ordered = new List<DateTime>();
                string ordinaryServes = null;
                for (int round = 0; round < 6 && ordinaryServes == null; round++)
                {
                    DateTime heldOn = sim.CurrentDate;
                    HoldFormingNone();
                    Day();
                    Check(care.ProcedureResumedAfter == heldOn, F("the election of {0:yyyy-MM-dd} resumed the procedure the day after", heldOn));
                    if (sim.ExtraElectionDate == DateTime.MinValue)
                    {
                        ordinaryServes = care.Breaks.Find(b => b.Contains("the procedure resumed") && b.Contains("serves"));
                        break;
                    }
                    DateTime next = sim.ExtraElectionDate;
                    Check(next > heldOn && next <= heldOn.AddMonths(ConfidenceProcedure.ExtraElectionMonths).AddDays(1) && next.DayOfWeek == DayOfWeek.Sunday, F("  and ordered an extra election on {0:yyyy-MM-dd} - a Sunday within three months", next));
                    ordered.Add(next);
                    Day();
                    Check(sim.ExtraElectionDate == next, "  resumed once per election - the next day orders nothing more");
                    int guard = 0;
                    while (sim.CurrentDate < next && guard++ < 120) { Day(); }
                    Check(sim.CurrentDate == next, F("  walked to the extra election's polling day ({0:yyyy-MM-dd})", sim.CurrentDate));
                }
                Check(ordered.Count >= 2, F("the loop turned: {0} extra election(s) ordered in a row ({1})", ordered.Count, string.Join(", ", ordered.ConvertAll(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))));
                Check(ordinaryServes != null, F("until the ordinary election fell within three months and served: {0}", ordinaryServes ?? "NEVER"));
                // Once per election, where the field alone guards it: after the ordinary-serves break no extra election is pending, so only
                // ProcedureResumedAfter keeps the next days from resuming the same election again.
                int breaks = care.Breaks.Count;
                Day(); Day();
                Check(care.Breaks.Count == breaks && sim.ExtraElectionDate == DateTime.MinValue, "the same election resumes the procedure once - two more days add nothing (the field's own guard)");
                Persistence.SaveGame save = Persistence.SaveGameService.CreateSaveGame(sim, sim.World, CountryId.Sweden, null);
                Persistence.SaveGame back = Persistence.SaveGameService.Deserialize(Persistence.SaveGameService.Serialize(save));
                Check(back.World.GetCountry(CountryId.Sweden).Government.ProcedureResumedAfter == care.ProcedureResumedAfter, "the last resumption rides the save, so a load resumes no election twice");
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { UnityEngine.Object.DestroyImmediate(go4); EnergyMarket.ResetTurnState(); }

            // 5. PS-3i ruling (2) (2026-09-25, §641): AI parties move no confidence only when the motion would carry and the mover prefers the government the
            //    formation says would follow - no doomed motions; a supporter moves nothing - past its tolerance it withdraws first (PS-3i-2a, §644).
            //    Every case asserts both premises of what it shows, and one invariant rides every day walked: a motion an AI party moved carried and the
            //    round it was weighed against seats the mover.
            (SimulationManager, Country) Fixture(GameObject host, string player, bool yearThirtyTwo, bool sdAggrieved, string[] cabinetOverride = null, int brokenItems = -1)
            {
                (SimulationManager s, Country c) = Open(host);
                c.PlayerPartyAbbrev = player;
                if (yearThirtyTwo)
                {
                    // The pinned film's year-32 count, sitting as if seated by the 13 September 2026 election (its record added, so the round reads that
                    // election's declarations through SittingVintage), with the start's government still in office - a government the round would not form.
                    c.ParliamentSeats.Clear();
                    foreach ((string abbrev, int held) in new[] { ("S", 94), ("SD", 63), ("M", 70), ("V", 27), ("C", 24), ("KD", 27), ("MP", 24), ("L", 20) }) { c.ParliamentSeats[abbrev] = held; }
                    c.ElectionHistory.Add(new ElectionRecord { Date = new DateTime(2026, 9, 13), CountryId = CountryId.Sweden.ToString(), Method = ElectionMethod.SwedenTwoTier });
                }
                if (cabinetOverride != null)
                {
                    c.Government.Cabinet.Clear(); c.Government.Cabinet.AddRange(cabinetOverride); c.Government.Support.Clear(); c.Government.Agreements.Clear();
                    c.Government.PmParty = cabinetOverride[0]; c.Government.AllocatePortfolios(c);
                }
                SupportAgreement a = c.Government.AgreementOf("SD");
                // PS-3i-2a (§644): aggrieved means PAST the tolerance - one more broken item than SD tolerates - unless a case names its count.
                int breaks = brokenItems >= 0 ? brokenItems : sdAggrieved && a != null ? (int)Math.Floor(SupportAgreement.BrokenShareTolerated * a.Items.Count) + 1 : 0;
                if (a != null) { for (int i = 0; i < breaks && i < a.Items.Count; i++) { a.Items[i].State = AgreementState.Broken; a.Items[i].BrokenOn = s.CurrentDate; } }
                return (s, c);
            }
            // The round and the vote as the manager reads them - the sitting chamber's election's declarations.
            string Round(Country c) { GovernmentFormation.View v = GovernmentFormation.ViewOfSitting(c, GovernmentFormation.RefusalLines(c.Id, c.Government.StandingRefusals)); return v.HasGovernment ? string.Join("+", v.Cabinet.ConvertAll(x => x.Abbrev)) : "none"; }
            ConfidenceProcedure.MotionVote VoteOn(Country c, string mover) => ConfidenceProcedure.Vote(c, mover);
            bool Seats(Country c, string party) => ("+" + Round(c) + "+").Contains("+" + party + "+");
            void Walk(SimulationManager s, Country c, int days, string label)
            {
                for (int d = 0; d < days; d++)
                {
                    GovernmentRecord before = c.Government;
                    string roundBefore = before.Caretaker || before.NoConfidenceOn != DateTime.MinValue ? null : Round(c);
                    s.AdvanceDay(); s.AdvanceCountryDayTick(CountryId.Sweden);
                    GovernmentRecord g = c.Government;
                    if (g != before || g.NoConfidenceOn != s.CurrentDate || g.NoConfidenceMover == null || g.NoConfidenceMover == c.PlayerPartyAbbrev) { continue; }
                    DivisionRecord last = c.Divisions.Entries[c.Divisions.Entries.Count - 1];
                    Check(last.Motion && last.Passed && roundBefore != null && ("+" + roundBefore + "+").Contains("+" + g.NoConfidenceMover + "+"),
                        F("{0}: the AI motion by {1} carried, and the round it was weighed against ({2}) seats it", label, g.NoConfidenceMover, roundBefore ?? "none"));
                }
            }

            var hosts = new List<GameObject>();
            try
            {
                // (a) the start, S in opposition, nothing broken: no AI party moves - with its premise, that no candidate over the tenth carries.
                var ga = new GameObject("ConfidenceDiagnostic.5a"); hosts.Add(ga);
                (SimulationManager sa, Country ca) = Fixture(ga, "S", yearThirtyTwo: false, sdAggrieved: false);
                var premiseA = new List<string>();
                foreach (PoliticalParty party in PartySystems.For(CountryId.Sweden))
                {
                    if (party.Abbrev == ca.PlayerPartyAbbrev || ca.Government.Cabinet.Contains(party.Abbrev) || !ConfidenceProcedure.CanBeTakenUp(ca, party.Abbrev, out int _, out int _)) { continue; }
                    premiseA.Add(party.Abbrev + (VoteOn(ca, party.Abbrev).Carried && Seats(ca, party.Abbrev) ? " CARRIES AND SEATED" : " no"));
                }
                Walk(sa, ca, 3, "(a)");
                Check(ca.Government.NoConfidenceOn == DateTime.MinValue && !premiseA.Exists(x => x.EndsWith("SEATED", StringComparison.Ordinal)),
                    F("(a) the start, nothing broken: no AI party moves - and none over the tenth both carries and would be seated [{0}]", string.Join(", ", premiseA)));

                // (b0) PS-3i-2a (§644): the tolerance's arithmetic - an agreement exactly at the tolerated share is not past it, one item more is; and an
                // empty agreement never is. Sized from the share so the "at" case is never empty.
                int size = (int)Math.Ceiling(1f / SupportAgreement.BrokenShareTolerated);
                var probe = new SupportAgreement { Supporter = "SD" };
                for (int i = 0; i < size; i++) { probe.Items.Add(new AgreementItem { Kind = AgreementItemKind.Law, State = AgreementState.Owed }); }
                int atShare = (int)Math.Floor(SupportAgreement.BrokenShareTolerated * size);
                for (int i = 0; i < atShare; i++) { probe.Items[i].State = AgreementState.Broken; }
                bool atIsWithin = !probe.PastTolerance();
                probe.Items[atShare].State = AgreementState.Broken;
                Check(atShare > 0 && atIsWithin && probe.PastTolerance() && !new SupportAgreement().PastTolerance(),
                    F("(b0) the tolerance: {0} broken of {1} is within the tolerated share ({2}), {3} is past it, an empty agreement never is", atShare, size, SupportAgreement.BrokenShareTolerated, atShare + 1));

                // (b0, live) the manager reads the tolerance, not any broken item: SD's agreement widened with owed items until one broken item is exactly
                // at the tolerated share, on the chamber where the round would seat SD - SD stays a supporter and moves nothing.
                var gb0 = new GameObject("ConfidenceDiagnostic.5b0"); hosts.Add(gb0);
                (SimulationManager sb0, Country cb0) = Fixture(gb0, "S", yearThirtyTwo: true, sdAggrieved: false, brokenItems: 1);
                SupportAgreement sdAgreement = cb0.Government.AgreementOf("SD");
                while (sdAgreement != null && sdAgreement.PastTolerance()) { sdAgreement.Items.Add(new AgreementItem { Kind = AgreementItemKind.Law, LawId = "tolerance_probe", Name = "an owed probe item", State = AgreementState.Owed }); }
                bool seatsB0 = Seats(cb0, "SD");
                Walk(sb0, cb0, 3, "(b0)");
                Check(sdAgreement != null && sdAgreement.Count(AgreementState.Broken) == 1 && seatsB0 && cb0.Government.Support.Contains("SD") && cb0.Government.NoConfidenceOn == DateTime.MinValue,
                    F("(b0) live: one broken item of {0}, within the tolerated share, the round would seat SD - SD stays a supporter and moves nothing", sdAgreement?.Items.Count ?? 0));

                // (b) SD past its tolerance on the start's chamber: it withdraws (PS-3i-2a) - but the round forms a government
                // without SD in its cabinet, so its motion, which would carry, is not moved: nothing for nothing.
                var gb = new GameObject("ConfidenceDiagnostic.5b"); hosts.Add(gb);
                (SimulationManager sb2, Country cb) = Fixture(gb, "S", yearThirtyTwo: false, sdAggrieved: true);
                ConfidenceProcedure.MotionVote wouldB = VoteOn(cb, "SD");
                bool seatedB = Seats(cb, "SD");
                Walk(sb2, cb, 3, "(b)");
                Check(wouldB.Carried && !seatedB && cb.Government.NoConfidenceOn == DateTime.MinValue && !cb.Government.Support.Contains("SD") && cb.Government.Breaks.Exists(x => x.Contains("SD withdrew its support over")),
                    F("(b) SD past its tolerance withdraws; its motion would carry ({0} of {1}) but the round forms {2}: no motion for nothing", wouldB.For, wouldB.Members, Round(cb)));

                // (c0) the year-32 chamber, where the round would seat SD and its motion would carry - but nothing is broken: a supporter moves nothing.
                var gc0 = new GameObject("ConfidenceDiagnostic.5c0"); hosts.Add(gc0);
                (SimulationManager sc0, Country cc0) = Fixture(gc0, "S", yearThirtyTwo: true, sdAggrieved: false);
                bool carriesC0 = VoteOn(cc0, "SD").Carried, seatsC0 = Seats(cc0, "SD");
                Walk(sc0, cc0, 3, "(c0)");
                Check(carriesC0 && seatsC0 && cc0.Government.NoConfidenceOn == DateTime.MinValue, "(c0) SD's motion would carry and the round would seat it, but nothing it was promised is broken: a supporter moves nothing");

                // (c1) the same, SD's agreement broken - but SD is the player's party: the AI never moves for the player.
                var gc1 = new GameObject("ConfidenceDiagnostic.5c1"); hosts.Add(gc1);
                (SimulationManager sc1, Country cc1) = Fixture(gc1, "SD", yearThirtyTwo: true, sdAggrieved: true);
                Walk(sc1, cc1, 3, "(c1)");
                Check(cc1.Government.NoConfidenceOn == DateTime.MinValue && cc1.Government.Support.Contains("SD"), "(c1) the aggrieved supporter is the player's party: no AI motion is moved in its name");

                // (d) doomed: S governs alone on the year-32 chamber; the round would seat M and SD, but neither motion carries - no motion.
                var gd = new GameObject("ConfidenceDiagnostic.5d"); hosts.Add(gd);
                (SimulationManager sd2, Country cd) = Fixture(gd, "V", yearThirtyTwo: true, sdAggrieved: false, cabinetOverride: new[] { "S" });
                ConfidenceProcedure.MotionVote mVote = VoteOn(cd, "M"), sdVote = VoteOn(cd, "SD");
                bool seatsM = Seats(cd, "M"), seatsSd = Seats(cd, "SD");
                Walk(sd2, cd, 3, "(d)");
                Check(seatsM && seatsSd && !mVote.Carried && !sdVote.Carried && cd.Government.NoConfidenceOn == DateTime.MinValue,
                    F("(d) the round would seat M and SD ({0}), but their motions would not carry ({1} and {2} of {3} needed): no doomed motion", Round(cd), mVote.For, sdVote.For, mVote.Needed));

                // (c) the natural mover: the year-32 chamber, SD's agreement broken - SD withdraws, moves, carries, and the round forms what it was weighed on.
                var gc = new GameObject("ConfidenceDiagnostic.5c"); hosts.Add(gc);
                (SimulationManager sc, Country cc) = Fixture(gc, "S", yearThirtyTwo: true, sdAggrieved: true);
                GovernmentRecord start = cc.Government;
                string promised = Round(cc);
                Check(Seats(cc, "SD"), F("(c) the round on this chamber would seat SD: {0}", promised));
                Walk(sc, cc, 1, "(c)");
                DivisionRecord motion = cc.Divisions.Entries[cc.Divisions.Entries.Count - 1];
                Check(start.NoConfidenceMover == "SD" && !start.Support.Contains("SD") && motion.Motion && motion.Passed && motion.Sides.Exists(x => x.Abbrev == "SD" && x.Side > 0),
                    "(c) SD, its agreement broken, withdraws and moves no confidence - the natural mover; the motion carried, recorded as a division marked a motion, SD for it");
                Check(start.Breaks.Exists(b => b.Contains("SD withdrew its support over")), "(c) the withdrawal is recorded with its broken items");
                Walk(sc, cc, ConfidenceProcedure.ExtraElectionWindowDays + 1, "(c)");
                Check(cc.Government != start && string.Join("+", cc.Government.Cabinet) == string.Join("+", promised.Split('+')) , F("(c) the week ran out and the round formed {0} - the government SD was promised ({1})", string.Join("+", cc.Government.Cabinet), promised));
                Check(start.Caretaker, "(c) the fallen government was discharged into a caretaker");

                // (e) the week before the next polling day: no motion is taken up - its week would end across an election.
                var ge = new GameObject("ConfidenceDiagnostic.5e"); hosts.Add(ge);
                (SimulationManager se, Country ce) = Fixture(ge, "S", yearThirtyTwo: true, sdAggrieved: true);
                se.TryPlayerPollingDay(out DateTime polling);
                int guardDays = 0; while (se.CurrentDate < polling.AddDays(-ConfidenceProcedure.ExtraElectionWindowDays) && guardDays++ < 400) { se.AdvanceDay(); }
                ce.ParliamentSeats.Clear();
                foreach ((string abbrev, int held) in new[] { ("S", 94), ("SD", 63), ("M", 70), ("V", 27), ("C", 24), ("KD", 27), ("MP", 24), ("L", 20) }) { ce.ParliamentSeats[abbrev] = held; }
                Check(ce.Government.Support.Contains("SD") && ce.Government.AgreementOf("SD").Count(AgreementState.Broken) > 0, "(e) SD is still an aggrieved supporter on the guard's first day");
                se.AdvanceCountryDayTick(CountryId.Sweden);
                Check(ce.Government.NoConfidenceOn == DateTime.MinValue && Seats(ce, "SD") && VoteOn(ce, "SD").Carried,
                    F("(e) {0:yyyy-MM-dd}, within a week of polling day {1:yyyy-MM-dd}: SD's motion would carry and the round would seat it, and none is taken up", se.CurrentDate, polling));

                // (e+) the positive control: the same fixture a day earlier, outside the guard's week - SD moves.
                var ge2 = new GameObject("ConfidenceDiagnostic.5e2"); hosts.Add(ge2);
                (SimulationManager se2, Country ce2) = Fixture(ge2, "S", yearThirtyTwo: true, sdAggrieved: true);
                se2.TryPlayerPollingDay(out DateTime polling2);
                int guard2 = 0; while (se2.CurrentDate < polling2.AddDays(-ConfidenceProcedure.ExtraElectionWindowDays - 2) && guard2++ < 400) { se2.AdvanceDay(); }
                ce2.ParliamentSeats.Clear();
                foreach ((string abbrev, int held) in new[] { ("S", 94), ("SD", 63), ("M", 70), ("V", 27), ("C", 24), ("KD", 27), ("MP", 24), ("L", 20) }) { ce2.ParliamentSeats[abbrev] = held; }
                se2.AdvanceDay(); se2.AdvanceCountryDayTick(CountryId.Sweden);
                Check(ce2.Government.NoConfidenceMover == "SD", F("(e+) {0:yyyy-MM-dd}, eight days before polling day: the same motion is moved - the guard is the week, nothing else", se2.CurrentDate));

                // (g) a plain opposition mover: SD outside the government with no agreement, the year-32 chamber - its motion carries and the round seats it.
                var gg = new GameObject("ConfidenceDiagnostic.5g"); hosts.Add(gg);
                (SimulationManager sg, Country cg) = Fixture(gg, "S", yearThirtyTwo: true, sdAggrieved: false, cabinetOverride: new[] { "M", "KD", "L" });
                bool carriesG = VoteOn(cg, "SD").Carried, seatsG = Seats(cg, "SD");
                Walk(sg, cg, 1, "(g)");
                Check(carriesG && seatsG && cg.Government.NoConfidenceMover == "SD", "(g) SD in opposition: its motion carries and the round would seat it - it moves, as the opposition may");

                // (f) the player's government answers an AI motion: the clock's hold is the controller's; the verb that ends it here - asked to be discharged.
                var gf = new GameObject("ConfidenceDiagnostic.5f"); hosts.Add(gf);
                (SimulationManager sf, Country cf) = Fixture(gf, "M", yearThirtyTwo: true, sdAggrieved: true);
                Walk(sf, cf, 1, "(f)");
                GovernmentRecord mine = cf.Government;
                Check(mine.NoConfidenceMover == "SD" && sf.PlayerGoverns(cf), "(f) SD's motion carries against the player's own government (M leads)");
                Check(sf.AskToBeDischarged(CountryId.Sweden, out string whyNot) && mine.Caretaker && cf.Government != mine, F("(f) the prime minister asks to be discharged (6 kap. 8 §): discharged at once, the round forms {0}", string.Join("+", cf.Government.Cabinet)));
                Check(!sf.AskToBeDischarged(CountryId.Sweden, out whyNot), F("(f) no second discharge: {0}", whyNot));
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { foreach (GameObject h in hosts) { UnityEngine.Object.DestroyImmediate(h); } EnergyMarket.ResetTurnState(); }

            Check(ConfidenceProcedure.RulesOf(CountryId.Germany) == ConfidenceProcedure.Rules.Unsourced, "Germany's rules are not yet modelled - no motion is taken up there");
            Check(ConfidenceProcedure.ExtraElectionDay(new DateTime(2026, 3, 4)) == new DateTime(2026, 5, 31), "the extra election's day: the Sunday on or before three months (4 Mar -> 4 Jun is a Thursday -> 31 May)");

            if (failures > 0) { Debug.LogError($"CONFIDENCE: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static (SimulationManager, Country) Open(GameObject go)
        {
            WorldClock.ApplyStart(CountryId.Sweden);
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            sim.PlayerCountryId = CountryId.Sweden;
            return (sim, world.GetCountry(CountryId.Sweden));
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
