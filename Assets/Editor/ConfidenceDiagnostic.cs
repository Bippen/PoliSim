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
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { UnityEngine.Object.DestroyImmediate(go3); EnergyMarket.ResetTurnState(); }

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
