using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Elias's ruling F5 ("(b): the check reads every bill lost that day"): THE CABINET'S PRESSURE CHECK READS EVERY BILL LOST THAT DAY. Until F5,
    /// `CabinetSystem.UnderPressure` read the division log's newest entry, so on a day several bills resolve a later one that passed hid an earlier
    /// fall - since §773 a Polish budget records an act per statute part on one day, the fund act always passed and voted last. Now a loss is written
    /// where it happens (`ParliamentSystem.RecordBillLost`: a division the chamber fails, a veto that stands) and the check reads that record; the log
    /// is read by nothing in the simulation. Checked: (a) nothing lost and approval above the floor - no pressure; (b) a failed bill puts the
    /// government under pressure that day and no other, and a bill passed after it, or a motion, hides nothing; (c) §773's own case through the game's
    /// own path - a budget whose tax act is vetoed (the draw PLANTED to veto) and whose fund act, passed and voted last, is the day's newest division -
    /// is under pressure; (d) a vote on a prime minister the chamber rejects, through the game's own writer (`RecordInvestiture`), is not a bill lost (PS-3i,
    /// §636); (g) a bill passed alone is not a bill lost; (h) a failed bill the log alone holds moves nothing - the log is unread; (e) the approval floor
    /// still reads alone; (f) the gate's own record - a veto that stands is a bill lost, a veto the Sejm overrides, a statute the President signs and a
    /// statute never at risk (its backing party for it, or uncontested) are not (the draw PLANTED each way).
    /// </summary>
    public static class CabinetPressureDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== CabinetPressureDiagnostic (Elias's ruling F5): the pressure check reads every bill lost that day ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var host = new GameObject("CabinetPressureDiagnostic");
            try
            {
                using (SimulationManager.EpochScope())
                {
                    World world = WorldFactory.CreateDefault();
                    var sim = host.AddComponent<SimulationManager>();
                    sim.SetWorld(world);
                    sim.PlayerCountryId = CountryId.Poland;
                    Country pl = world.GetCountry(CountryId.Poland);
                    pl.PlayerPartyAbbrev = "KO";
                    // PLANTED: a chamber KO carries alone, PiS opposing (PresidentialVetoDiagnostic's), and approval above the floor - so only a loss puts the
                    // government under pressure in (a) to (d)
                    foreach (KeyValuePair<string, int> kv in new Dictionary<string, int> { { "PiS", 194 }, { "KO", 262 }, { "TD", 0 }, { "NL", 0 }, { "Konf", 4 } }) { pl.ParliamentSeats[kv.Key] = kv.Value; }
                    pl.State.ApprovalRating = Mathf.Max(pl.State.ApprovalRating, CabinetSystem.PressureApprovalFloor + 10f);
                    DateTime today = sim.CurrentDate;
                    string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                    // (a)
                    Check(pl.BillLostOn == DateTime.MinValue && !CabinetSystem.UnderPressure(pl, today), F("(a) on {0}, nothing lost and approval above the floor ({1:0.#} against {2:0.#}) - no pressure", Day(today), pl.State.ApprovalRating, CabinetSystem.PressureApprovalFloor));

                    // (b) a failed bill, then a passed one and a motion the same day; the next day
                    var concern = new BillConcern { Direction = 1f }.Add(StanceAxis.SpendVsTax, 1f);
                    ParliamentSystem.RecordDivision(pl, "Enact: a planted statute the chamber fails", concern, false, today);
                    ParliamentSystem.RecordDivision(pl, "Enact: a planted statute the chamber passes", concern, true, today);
                    DivisionRecord motion = pl.Divisions.Append("A planted motion", today, 0f, true, 0f, 0, new List<DivisionSide>());
                    motion.Motion = true;
                    Check(CabinetSystem.UnderPressure(pl, today) && pl.Divisions.Entries[pl.Divisions.Entries.Count - 1].Passed,
                        F("(b) a bill lost on {0} puts the government under pressure that day - a bill passed after it and a motion after that hide nothing (the log's newest entry is a passed motion)", Day(today)));
                    Check(!CabinetSystem.UnderPressure(pl, today.AddDays(1)), F("(b) the loss is the day's alone - on {0} there is no pressure", Day(today.AddDays(1))));

                    // (d) a vote lost that is not a bill (PS-3i, §636), through the game's own writer: an investiture the chamber rejects, recorded today as the
                    // formation records one (the review's finding: a check appending to the log itself could not fail)
                    pl.BillLostOn = DateTime.MinValue;
                    System.Reflection.MethodInfo investiture = typeof(SimulationManager).GetMethod("RecordInvestiture", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (investiture != null) { investiture.Invoke(sim, new object[] { pl, new ProposalVerdict(), "A planted investiture the chamber rejects", false }); }
                    DivisionRecord newestVote = pl.Divisions.Entries[pl.Divisions.Entries.Count - 1];
                    // the writer alone: (b)'s failed bill shares this day in the log, and whether the check reads the log is (h)'s to show
                    Check(investiture != null && newestVote.Motion && !newestVote.Passed && newestVote.Date == today && pl.BillLostOn == DateTime.MinValue,
                        F("(d) an investiture the chamber rejects on {0}, recorded by the game's own writer, is not a bill lost (PS-3i, §636) - no loss written", Day(today)));

                    // (g) a bill passed alone is not a bill lost (the review's finding: a RecordDivision writing the loss on every division passed every case)
                    DateTime passDay = today.AddDays(3);
                    pl.BillLostOn = DateTime.MinValue;   // the case's own state, as every other case resets it
                    ParliamentSystem.RecordDivision(pl, "Enact: a planted statute the chamber passes alone", concern, true, passDay);
                    Check(pl.BillLostOn == DateTime.MinValue && !CabinetSystem.UnderPressure(pl, passDay), F("(g) a bill passed alone on {0} is not a bill lost - no loss written, no pressure", Day(passDay)));

                    // (h) the log alone moves nothing (the review's finding: "the division log unread" held to account): a failed bill appended to the log
                    // and to nothing else, on a fresh day - the reader before F5 would have read it as the day's loss
                    DateTime logOnlyDay = today.AddDays(4);
                    pl.Divisions.Append("A planted fall the log alone holds", logOnlyDay, 0f, false, 0f, 0, new List<DivisionSide>());
                    DivisionRecord logOnly = pl.Divisions.Entries[pl.Divisions.Entries.Count - 1];
                    Check(!logOnly.Passed && !logOnly.Motion && !CabinetSystem.UnderPressure(pl, logOnlyDay),
                        F("(h) a failed bill the log alone holds on {0} moves nothing - the pressure check reads no log", Day(logOnlyDay)));

                    // (f) the gate's own record (F5): a veto that stands is a bill lost that day; a veto the Sejm overrides, and a statute the President signs,
                    // are not - each through the gate itself, the draw PLANTED to veto or to sign
                    System.Reflection.MethodInfo gate = typeof(SimulationManager).GetMethod("PresidentialVetoGate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    DivisionSide Side(string party, int seats, int side) => new DivisionSide { Abbrev = party, Seats = seats, Side = side };
                    DateTime GateLoss(List<DivisionSide> sides, double planted, out bool stood)
                    {
                        pl.BillLostOn = DateTime.MinValue;
                        DivisionRecord passage = pl.Divisions.Append("Enact: a planted statute at the gate", sim.CurrentDate, 0.5f, true, 1f, 0, new List<DivisionSide>(sides));
                        using (PresidentialVeto.PlantDraw(planted)) { stood = (bool)gate.Invoke(sim, new object[] { pl, passage, true, PresidentialVeto.Act.OrdinaryStatute }); }
                        return pl.BillLostOn;
                    }
                    var pisAgainst = new List<DivisionSide> { Side("PiS", 194, -1), Side("KO", 262, 1), Side("Konf", 4, -1) };
                    var overridable = new List<DivisionSide> { Side("PiS", 150, -1), Side("KO", 200, 1), Side("TD", 80, 1), Side("NL", 30, 1) };
                    var pisFor = new List<DivisionSide> { Side("PiS", 194, 1), Side("KO", 262, 1) };
                    DateTime lostOnVeto = DateTime.MaxValue, lostOnOverride = DateTime.MaxValue, lostOnSigned = DateTime.MaxValue, lostOnBacked = DateTime.MaxValue, lostOnUncontested = DateTime.MaxValue;
                    bool stoodVeto = true, stoodOverride = false, stoodSigned = false, stoodBacked = false, stoodUncontested = false;
                    if (gate != null)
                    {
                        lostOnVeto = GateLoss(pisAgainst, 0.0, out stoodVeto);
                        lostOnOverride = GateLoss(overridable, 0.0, out stoodOverride);
                        lostOnSigned = GateLoss(pisAgainst, 1.0, out stoodSigned);
                        lostOnBacked = GateLoss(pisFor, 0.0, out stoodBacked);                              // never at risk: the backing party votes for it
                        lostOnUncontested = GateLoss(new List<DivisionSide>(), 0.0, out stoodUncontested);   // never at risk: the fund act's shape, no sides
                    }
                    Check(gate != null && !stoodVeto && lostOnVeto == sim.CurrentDate.Date && stoodOverride && lostOnOverride == DateTime.MinValue && stoodSigned && lostOnSigned == DateTime.MinValue
                          && stoodBacked && lostOnBacked == DateTime.MinValue && stoodUncontested && lostOnUncontested == DateTime.MinValue,
                        F("(f) the gate's own record on {0}: a veto that stands is a bill lost that day; a veto the Sejm overrides, a statute the President signs, and a statute never at risk (its backing party for it, or uncontested) are not - even with the draw planted to veto", Day(sim.CurrentDate)));

                    // (c) §773's own case through the game's own path: a rate move the Sejm passes, at risk of a veto no override would carry, in its own tax
                    // act, beside a fund created - the tax act vetoed (the draw PLANTED to veto), the fund act passed and voted last
                    TaxLine moved = null; float newRate = 0f;
                    foreach (TaxLine line in pl.TaxLines)
                    {
                        if (!line.IsImplemented || moved != null) { continue; }
                        foreach (float step in new[] { -2f, 2f })
                        {
                            var probe = new BudgetBill(); probe.TaxLines[line.Type] = line.Rate + step;
                            BillConcern rateConcern = ParliamentSystem.GetBudgetBillConcern(pl, probe.TaxActPart());
                            if (!ParliamentSystem.WouldBillPass(pl, rateConcern)) { continue; }
                            var sides = new List<DivisionSide>();
                            foreach (PartyStance s in StanceModel.Stances(pl, rateConcern)) { sides.Add(new DivisionSide { Abbrev = s.Party.Abbrev, Seats = s.Seats, Side = s.Side }); }
                            PresidentialVeto.Outcome projected = PresidentialVeto.Decide(CountryId.Poland, pl.PresidentialElections, sim.CurrentDate, PresidentialVeto.Act.OrdinaryStatute, sides);
                            if (projected != null && projected.AtRisk && !projected.OverrideCarries) { moved = line; newRate = line.Rate + step; break; }
                        }
                    }
                    Check(moved != null, F("(c) on the planted chamber a rate move the Sejm passes, at risk of a veto no override would carry - {0}",
                        moved != null ? moved.Type + " " + moved.Rate.ToString("0.##", CultureInfo.InvariantCulture) + " -> " + newRate.ToString("0.##", CultureInfo.InvariantCulture) : "NONE"));
                    if (moved != null)
                    {
                        pl.BillLostOn = DateTime.MinValue;
                        SovereignWealthFund fundBefore = pl.SovereignWealthFund;
                        pl.SovereignWealthFund = null;
                        var bill = new BudgetBill { SwfShouldExist = true, SwfContributionRatePercent = 1f, SwfDomesticAllocationPercent = 50f, SwfEquitiesWeight = 40f, SwfBondsWeight = 30f, SwfInfrastructureWeight = 15f, SwfRealEstateWeight = 15f };
                        bill.TaxLines[moved.Type] = newRate;
                        bill.SpendingPercentChanges[pl.SpendingLines.First(l => l.Amount > 0f).Category] = 5f;
                        bool introduced = sim.IntroduceBudgetBill(CountryId.Poland, bill);
                        int before = pl.Divisions.Entries.Count;
                        using (PresidentialVeto.PlantDraw(0.0)) { for (int day = 0; day < 400 && sim.GetPendingBudgetBill(CountryId.Poland) != null; day++) { sim.AdvanceBudgetBillDay(CountryId.Poland); } }
                        List<DivisionRecord> added = pl.Divisions.Entries.GetRange(before, pl.Divisions.Entries.Count - before);
                        pl.SovereignWealthFund = fundBefore;
                        DivisionRecord newest = added.Count > 0 ? added[added.Count - 1] : null;
                        bool vetoStood = added.Any(d => d.Title.StartsWith("Vetoed by the President", StringComparison.Ordinal) && d.Title.Contains("Tax act: ") && !d.Passed);
                        bool hiddenUnderTheOldReader = newest != null && newest.Passed && newest.Title.StartsWith("Fund act: ", StringComparison.Ordinal);
                        DateTime resolved = newest?.Date ?? sim.CurrentDate;
                        Check(introduced && vetoStood && hiddenUnderTheOldReader && CabinetSystem.UnderPressure(pl, resolved) && pl.BillLostOn == resolved.Date,
                            F("(c) §773's case: the tax act vetoed, the fund act passed and voted last - the day's newest division a passed act, the fall still read: under pressure on {0}; recorded: {1}",
                                Day(resolved), string.Join(" | ", added.Select(d => (d.Motion ? "[motion] " : "") + d.Title))));
                    }

                    // (e) the floor alone
                    pl.State.ApprovalRating = CabinetSystem.PressureApprovalFloor - 1f;
                    Check(CabinetSystem.UnderPressure(pl, today.AddDays(30)), F("(e) approval below the floor ({0:0.#}) - under pressure with nothing lost that day", pl.State.ApprovalRating));
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n'); }
            finally { UnityEngine.Object.DestroyImmediate(host); }

            sb.Append(failures == 0 ? "=== CabinetPressureDiagnostic: every bill lost that day is read ===" : F("=== CabinetPressureDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
