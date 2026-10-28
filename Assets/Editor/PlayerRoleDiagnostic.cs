using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-3a (§628): WHO GOVERNS, ASSERTED. At Sweden's start the government of record is Kristersson's M+KD+L with SD's support, INSTALLED,
    /// led by M (the record's own head); by party the four roles read S OPPOSITION, M PRIME MINISTER, KD and L JUNIOR PARTNER, SD SUPPORT; the
    /// one test `PlayerGoverns` is true for M alone (a junior partner's portfolios are PS-3's next part, stated); the AI finance ministry's
    /// gate follows it (a Sweden the player's S does not lead is AI-governed); the record survives a save round trip; Germany's start reads SPD PRIME MINISTER;
    /// Poland's start reads its PiS government of record (§763), Italy's Draghi government of record (K157) is pinned. PS-3b (§629): every start stores a record - the USA's president and party, France's cabinet under its president -
    /// and a country with none FAILS LOUDLY at AtStart and at PlayerGoverns (proved red on §628's build first: Reviews/evidence/2026-09-25_s629_probe_red.txt).
    /// PS-6 US-7, the US rows: the president's party is the head's, the other in opposition; a planted decided contest makes its party the PRESIDENT-ELECT on the days
    /// before the oath and nothing else; the record yields from the game's polling day; the government a president forms at the oath is a presidency of one party.
    /// (On an AI USA - a Sweden player's world, no Senate seated - the roles stay the government's: in opposition.) PS-6 US-19, the US rows re-pinned on US
    /// players' worlds (<see cref="UsRoles"/>): on 12 Mar 2024 a DEM player governs as the president's party, a REP player holds the House majority, may introduce
    /// an ordinary bill and sees it die in the Senate; and the rest of R-US7 (a).
    /// </summary>
    public static class PlayerRoleDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== PlayerRoleDiagnostic (PS-3a, §628): who governs is the player's role ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using System.IDisposable epoch = SimulationManager.EpochScope();
            var go = new GameObject("PlayerRoleDiagnostic");
            try
            {
                // PS-6 US-19: the roles in a presidential system, on US worlds of their own - first, so the streams, the epoch and the energy layer's calibration
                // this check leaves behind are the Sweden world's below, as before (the streams and the epoch are restored after it too)
                UsRoles(Check, sb);

                WorldClock.ApplyStart(CountryId.Sweden);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country sweden = world.GetCountry(CountryId.Sweden);
                DateTime start = WorldClock.StartDate(CountryId.Sweden);

                Check(sweden.Government != null, "the factory stored Sweden's government of record at creation (§629)");
                Check(sim.PlayerGoverns(sweden), "no party seated: the player country is the instrument's hand - the Editor tools' case, stated (§629)");
                sweden.Government = GovernmentRecord.AtStart(sweden, start, world);
                GovernmentRecord g = sweden.Government;
                Check(!g.Provisional && g.PmParty == "M" && string.Join("+", g.Cabinet) == "M+KD+L" && string.Join("+", g.Support) == "SD",
                    F("Sweden's start: {0} led by {1}, support {2}, {3}", string.Join("+", g.Cabinet), g.PmParty, string.Join("+", g.Support), g.Provisional ? "PROVISIONAL" : "of record"));
                foreach ((string party, PlayerRole role, bool governs) in new[] { ("S", PlayerRole.Opposition, false), ("M", PlayerRole.PrimeMinister, true), ("KD", PlayerRole.JuniorPartner, false), ("L", PlayerRole.JuniorPartner, false), ("SD", PlayerRole.Support, false), ("V", PlayerRole.Opposition, false) })
                {
                    sweden.PlayerPartyAbbrev = party;
                    Check(g.RoleOf(party) == role && sim.PlayerGoverns(sweden) == governs, F("{0}: {1}, governs {2}", party, g.RoleOf(party), sim.PlayerGoverns(sweden)));
                }
                Check(g.RoleOf(null) == PlayerRole.None, "no party: no role");

                // The AI ministry's gate follows the role: with S seated, Sweden is AI-governed and the ministry writes on its turn.
                sweden.PlayerPartyAbbrev = "S";
                Check(!sim.PlayerGoverns(sweden), "with S seated the player does not govern Sweden - the AI ministry's gate opens for it");
                Country germany = world.GetCountry(CountryId.Germany);
                Check(!sim.PlayerGoverns(germany), "another country is never the player's to govern");
                // PS-3c (§630): THE ROLE GATE ON THE LEVERS - with S seated (opposition) every bill is refused with the reason; with M seated (the prime minister's party) it is introduced.
                bool refused = !sim.PlayerMayIntroduce(CountryId.Sweden, out string lockedBecause) && !sim.IntroduceLawBill(CountryId.Sweden, new LawBill { LawId = "cash_bail_reform_act", IsRepeal = false });
                Check(refused && lockedBecause == "IN OPPOSITION · THE GOVERNMENT INTRODUCES BILLS · YOUR PARTY VOTES ON THEM", F("S in opposition: a law bill is refused - {0}", lockedBecause ?? "(no reason)"));
                Check(sim.PlayerMayIntroduce(CountryId.Germany, out _), "another country's bills are its own government's - never refused here");
                sweden.PlayerPartyAbbrev = "M";
                Check(sim.PlayerMayIntroduce(CountryId.Sweden, out _) && sim.IntroduceLawBill(CountryId.Sweden, new LawBill { LawId = "cash_bail_reform_act", IsRepeal = false }), "M as the prime minister's party: the same bill is introduced");

                // PS-6 US-19 (the review's L6): the kind gate every Introduce*Bill now asks is the old gate, answer and words, in a country with one chamber - the
                // junior partner and the support party on the budget and the labour bill (the two-argument gate's) and the crime bill (Interior's portfolio)
                foreach (string party in new[] { "KD", "SD", "M", "S" })
                {
                    sweden.PlayerPartyAbbrev = party;
                    var rows = new List<string>();
                    bool same = true;
                    foreach ((Congress.Kind kind, CabinetPortfolio? portfolio) in new (Congress.Kind, CabinetPortfolio?)[] { (Congress.Kind.Budget, null), (Congress.Kind.Labor, null), (Congress.Kind.CrimeJustice, CabinetPortfolio.InteriorJustice) })
                    {
                        bool oldAnswer = portfolio.HasValue ? sim.PlayerMayIntroduce(CountryId.Sweden, portfolio, out string oldWords) : sim.PlayerMayIntroduce(CountryId.Sweden, out oldWords);
                        bool newAnswer = sim.PlayerMayIntroduce(CountryId.Sweden, kind, portfolio, out string newWords);
                        if (oldAnswer != newAnswer || oldWords != newWords) { same = false; }
                        rows.Add(F("{0} {1}{2}", kind, newAnswer ? "allowed" : "refused", newWords == null ? string.Empty : " \"" + newWords + "\""));
                    }
                    Check(same, F("{0} ({1}): the kind gate is the old gate, answer and words - {2}", party, g.RoleOf(party), string.Join("; ", rows)));
                }

                // PS-6 US-19 (the review's M2): a government's bill in a country with one chamber still pays - Congress.GovernmentBears is true for every author
                // there, so a failed vote costs the failed vote's approval and the division marks the day lost (F5), as before
                sweden.PlayerPartyAbbrev = "M";
                var swedishBill = new CrimeJusticePolicyBill { PoliceFunding = sweden.PoliceFundingLevel, SentencingSeverity = Mathf.Clamp(sweden.SentencingSeverity + 15f, 0f, 100f), BailReform = sweden.BailReformLevel, DrugPolicy = sweden.DrugPolicyLevel, JudicialFunding = sweden.JudicialFundingLevel, BorderEnforcement = sweden.BorderEnforcementLevel };
                BillConcern swedishConcern = ParliamentSystem.GetCrimeJusticeBillConcern(sweden, swedishBill);
                bool bearsEverywhere = new[] { "M", "KD", "SD", "S", "V" }.All(p => { sweden.PlayerPartyAbbrev = p; return Congress.GovernmentBears(sweden, ParliamentSystem.GetCrimeJusticeBillConcern(sweden, swedishBill)); });
                sweden.PlayerPartyAbbrev = "M";
                float swedishApproval = sweden.State.ApprovalRating;
                DateTime lostWas = sweden.BillLostOn;
                ParliamentSystem.ApplyCrimeJusticeBillResult(sweden, swedishBill, false, (c, b) => { }, Congress.GovernmentBears(sweden, swedishConcern));
                float swedishCharged = swedishApproval - sweden.State.ApprovalRating;
                ParliamentSystem.RecordDivision(sweden, "Crime & Justice bill (PlayerRoleDiagnostic, a failed government bill)", swedishConcern, false, start);
                bool swedishLost = sweden.BillLostOn == start.Date;
                sweden.State.ApprovalRating = swedishApproval;
                sweden.BillLostOn = lostWas;
                Check(bearsEverywhere && Mathf.Abs(swedishCharged - Mathf.Min(ParliamentSystem.BillFailedApprovalCost, swedishApproval)) < 1e-4f && swedishLost,
                    F("Sweden (one chamber): every author's bill is the government's to bear; M's failed bill costs {0:0.00} of approval (the failed vote's {1:0.0}) and marks the day lost - {2}",
                        swedishCharged, ParliamentSystem.BillFailedApprovalCost, swedishLost ? "as before" : "NOT MARKED"));
                sweden.PlayerPartyAbbrev = "S";

                // The record survives the save round trip (format 28).
                SaveGame save = SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null);
                string json = SaveGameService.Serialize(save);
                SaveGame back = SaveGameService.Deserialize(json);
                Country loaded = back.World.GetCountry(CountryId.Sweden);
                Check(loaded.Government != null && loaded.Government.PmParty == "M" && string.Join("+", loaded.Government.Cabinet) == "M+KD+L" && loaded.Government.RoleOf("S") == PlayerRole.Opposition,
                    "the government rides the save (format 28) and reads the same role after the round trip");

                // The formation's record after an election: on the seated 2022 chamber with 2026's declarations the formation forms SD+M+KD+L (§607) - the record names it and its head.
                GovernmentRecord formed = GovernmentRecord.FromView(sweden, GovernmentFormation.ViewOf(sweden, ElectionVintage.Sweden2026), new DateTime(2026, 9, 13), world: world);
                Check(formed.Cabinet.Count > 0 && formed.PmParty == "M" && formed.RoleOf("S") == PlayerRole.Opposition && !formed.Provisional,
                    F("the formation's record: {0} led by {1} ({2}); S is {3}", string.Join("+", formed.Cabinet), formed.PmParty, formed.Outcome, formed.RoleOf("S")));

                // Germany's start: the government of record led by the chancellor's party.
                Country de = world.GetCountry(CountryId.Germany);
                GovernmentRecord gde = de.Government = GovernmentRecord.AtStart(de, WorldClock.StartDate(CountryId.Germany), world);
                Check(gde.PmParty == "SPD" && gde.Cabinet.Contains("SPD"), F("Germany's start: {0} led by {1}{2}", string.Join("+", gde.Cabinet), gde.PmParty, gde.Provisional ? " (provisional)" : string.Empty));
                Check(gde.Cabinet.Contains("FDP"), "6 November 2024 is the day BEFORE the FDP ministers were dismissed - the record's SPD+Grüne+FDP cabinet still stands (§618)");
                // The other starts, printed: the USA's president and France's cabinet under its president (§629), Poland's PiS government of record (§763), Italy's Draghi (K157).
                foreach (CountryId other in new[] { CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France })
                {
                    Country c = world.GetCountry(other);
                    c.Government = GovernmentRecord.AtStart(c, WorldClock.StartDate(other), world);
                    sb.Append("    ").Append(GovernmentRecord.Describe(other, WorldClock.StartDate(other), c)).Append(Environment.NewLine);
                }
                GovernmentRecord gpl = world.GetCountry(CountryId.Poland).Government;
                Check(gpl != null && !gpl.Provisional && gpl.PmParty == "PiS" && gpl.Cabinet.Contains("PiS"),
                    F("Poland's start (§763, the PiS start): Morawiecki's government of record installed - {0} led by {1}{2}", gpl != null ? string.Join("+", gpl.Cabinet) : "NONE", gpl?.PmParty, gpl != null && gpl.Provisional ? " (provisional)" : string.Empty));
                GovernmentRecord git = world.GetCountry(CountryId.Italy).Government;
                // PS-7 (K157): Draghi's government of record installed - the national-unity parties of the Camera's confidence vote of 18 Feb 2021, derived onto the
                // seated XVIII Camera (WorldClock.ItalyDraghiCabinet / ItalyDraghiSupport); a non-party head, so the cabinet's largest party stands as his (M5S)
                Check(git != null && !git.Provisional && git.Cabinet.SequenceEqual(WorldClock.ItalyDraghiCabinet) && git.Support.SequenceEqual(WorldClock.ItalyDraghiSupport) && git.PmParty == "M5S" && git.Caretaker && git.CaretakerSince == new System.DateTime(2022, 7, 21),
                    F("Italy's start (K157, K207): Draghi's government of record installed, a caretaker from 21 Jul 2022 - {0} led by {1} (the stand-in PM party, declared), support {2}{3}", git != null ? string.Join("+", git.Cabinet) : "NONE", git?.PmParty, git != null ? string.Join("+", git.Support) : "NONE", git != null && git.Provisional ? " (provisional)" : string.Empty));

                // PS-3b (§629): THE EXECUTIVES, AND NO SILENT DEFAULT. Every playable start stores a government of record - the USA's is its president
                // and their party, France's its cabinet under its president - and a country with none FAILS LOUDLY: `AtStart` throws, and `PlayerGoverns`
                // throws on a country whose record was never stored, rather than reading the player as governing. Proved both ways: this block was
                // run against §628's build first (RED: the USA and France stored null and S/DEM/ENS governed by default), then against this one (GREEN).
                foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France })
                {
                    Country c = world.GetCountry(id);
                    Check(c.Government != null && !string.IsNullOrEmpty(c.Government.PmParty), F("{0}: a government of record is stored at its start, led by {1}", id, c.Government?.PmParty ?? "NONE"));
                }
                GovernmentRecord usa = world.GetCountry(CountryId.USA).Government;
                Check(usa != null && usa.PmParty == "DEM" && !usa.Provisional && usa.Kind == WorldClock.ExecutiveKind.Presidency && usa.Executive != null && usa.Executive.Contains("Biden"),
                    F("the USA's start ({0:yyyy-MM-dd}): the president and their party of record - {1}, {2}", WorldClock.StartDate(CountryId.USA), usa?.Executive ?? "NONE", usa?.PmParty ?? "-"));

                // PS-6 US-7: THE PRESIDENCY'S ROLES. At the USA's start the president's party governs - the head's role (the chip reads PRESIDENT) - and REP is in
                // opposition; with no contest held the role on a day is the role toward the government. A contest planted for REP and decided, the oath ahead: on the
                // day after the polling day REP is the PRESIDENT-ELECT - toward the sitting government still in opposition (RoleOf, every test of who governs),
                // DEM still the head's; once the oath's day has come no president-elect waits, and the role is the government's again (the oath that swaps the
                // government is the day loop's - PresidencyDiagnostic). The government a president forms at the oath: a presidency, the party alone, every
                // portfolio its own, no support - and a role it gives: the party PRESIDENT's, every other in opposition.
                Country us = world.GetCountry(CountryId.USA);
                DateTime polled = new DateTime(2024, 11, 5), dayAfter = polled.AddDays(1), oath = UsPresidentialElection.OathDayAfter(polled);
                Check(usa.RoleOf("DEM") == PlayerRole.PrimeMinister && usa.RoleOf("REP") == PlayerRole.Opposition
                      && GovernmentRecord.RoleOn(us, "DEM", dayAfter) == PlayerRole.PrimeMinister && GovernmentRecord.RoleOn(us, "REP", dayAfter) == PlayerRole.Opposition && GovernmentRecord.RoleOn(us, null, dayAfter) == PlayerRole.None,
                    "the USA (US-7): DEM, the president's party, is the head's (PRESIDENT); REP in opposition; with no contest held the role on a day is the role toward the government");
                // the record's 2024 count, read from the catalog (NARA's electors), never typed - the contest planted on it as US-8's count will record one
                var y2024 = Array.Find(PoliSim.Elections.Generated.UsPresidentialReturns.Years, y => y.Year == 2024);
                var planted = new UsPresidentialElection.Contest { PollingDay = polled, TakesOffice = oath, ElectorsAppointed = y2024.Electors, Winner = y2024.NomineeR, WinnerParty = "REP", Line = "planted (PlayerRoleDiagnostic)" };
                planted.Electors.Add(new UsPresidentialElection.Tally { Candidate = y2024.NomineeR, Party = "REP", Votes = y2024.CastR });
                planted.Electors.Add(new UsPresidentialElection.Tally { Candidate = y2024.NomineeD, Party = "DEM", Votes = y2024.CastD });
                us.UsPresidentialElections.Add(planted);
                try
                {
                    Check(GovernmentRecord.RoleOn(us, "REP", dayAfter) == PlayerRole.PresidentElect && usa.RoleOf("REP") == PlayerRole.Opposition && GovernmentRecord.RoleOn(us, "DEM", dayAfter) == PlayerRole.PrimeMinister
                          && GovernmentRecord.RoleOn(us, "REP", oath.AddDays(-1)) == PlayerRole.PresidentElect && GovernmentRecord.RoleOn(us, "REP", oath) == PlayerRole.Opposition,
                        F("a contest planted for REP (decided, the oath {0:yyyy-MM-dd}): REP the PRESIDENT-ELECT from the day after the polling day to the eve of the oath, toward the sitting government in opposition; DEM the head's; on the oath's day no president-elect waits", oath));
                    Check(WorldClock.RecordSeatsExecutive(us, polled.AddDays(-1)) && !WorldClock.RecordSeatsExecutive(us, polled) && WorldClock.RecordSeatsExecutive(CountryId.USA),
                        "the record seats the president in this world up to the eve of the game's polling day, and not from it - the country's gate unchanged (US-7)");
                }
                finally { us.UsPresidentialElections.Remove(planted); }
                GovernmentRecord sworn = GovernmentRecord.OfPresidency("Kamala D. Harris (DEM)", "DEM", oath, UsPresidentialElection.ElectedOutcome, "planted (PlayerRoleDiagnostic)");
                int portfolios = Enum.GetValues(typeof(CabinetPortfolio)).Length;
                Check(sworn.Kind == WorldClock.ExecutiveKind.Presidency && sworn.PmParty == "DEM" && sworn.Cabinet.Count == 1 && sworn.Cabinet[0] == "DEM" && sworn.Support.Count == 0 && sworn.Agreements.Count == 0
                      && sworn.Portfolios.Count == 1 && sworn.Portfolios["DEM"].Count == portfolios && sworn.FormedOn == oath && !sworn.Provisional && sworn.Outcome == UsPresidentialElection.ElectedOutcome
                      && sworn.RoleOf("DEM") == PlayerRole.PrimeMinister && sworn.RoleOf("REP") == PlayerRole.Opposition,
                    F("the government a president forms at the oath: a presidency, {0} alone, all {1} portfolios its own, no support, formed {2:yyyy-MM-dd}; DEM the head's, REP in opposition", sworn.PmParty, portfolios, sworn.FormedOn));
                GovernmentRecord fr = world.GetCountry(CountryId.France).Government;
                Check(fr != null && fr.Provisional && fr.Kind == WorldClock.ExecutiveKind.Cabinet && fr.Executive != null && fr.Executive.Contains("Macron"),
                    F("France's start ({0:yyyy-MM-dd}): Attal's caretaker cabinet under {3} - its parties are GAP G4, so the formation's stand-in {1} led by {2} is PROVISIONAL", WorldClock.StartDate(CountryId.France), fr == null ? "NONE" : string.Join("+", fr.Cabinet), fr?.PmParty ?? "-", fr?.Executive ?? "no president"));
                bool threw = false;
                try { GovernmentRecord.AtStart(sweden, new DateTime(1900, 1, 1)); } catch (InvalidOperationException) { threw = true; }
                Check(threw, "a date with no government of record: AtStart THROWS rather than returning null");
                threw = false;
                sweden.Government = null;
                try { sim.PlayerGoverns(sweden); } catch (InvalidOperationException) { threw = true; }
                Check(threw, "a country whose government was never stored: PlayerGoverns THROWS rather than defaulting the player to governing");
                sweden.Government = g;

                // PS-3d (§631, ruled): France's governing mode is a WHAT-IF - the player's party governs on the 2024 Assembly of record; as an AI country France keeps the provisional stand-in.
                Country france = world.GetCountry(CountryId.France);
                Check(france.Government.Provisional && france.Government.Outcome != "what-if", "France as an AI country: the provisional stand-in stands");
                GovernmentRecord whatIf = GovernmentRecord.WhatIfGoverning(france, "UG", WorldClock.StartDate(CountryId.France), world);
                Check(whatIf.PmParty == "UG" && whatIf.RoleOf("UG") == PlayerRole.PrimeMinister && whatIf.RoleOf("RN") == PlayerRole.Opposition && !whatIf.Provisional && whatIf.Outcome == "what-if" && whatIf.Executive != null && whatIf.Executive.Contains("Macron"),
                    F("France's what-if: {0} governs under {1}; RN is {2}", whatIf.PmParty, whatIf.Executive, whatIf.RoleOf("RN")));
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { UnityEngine.Object.DestroyImmediate(go); EnergyMarket.ResetTurnState(); }
            if (failures > 0) { Debug.LogError($"PLAYER ROLE: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>
        /// PS-6 US-19 (`docs/specs/USA_STAGE_PLAN.md`, "Roles in a presidential system"; R-US7 (a), R-US8 (a)): THE US ROWS, RE-PINNED. Two worlds opened as a new
        /// game opens them (`UsCongressDiagnostic.OpenUsa`: the epoch, the player's country the USA, the record seated by the day loop's own method - the House, the
        /// Senate, the president). The done-when's words: "on 12 Mar 2024 a DEM player governs as the president's party, and a REP player holds the House majority,
        /// may introduce an ordinary bill, and sees it die in the Senate". Beside them: the budget the president's party's alone; a revenue bill the House's; a
        /// party holding the Senate alone introducing there and no revenue bill (planted - no record day reaches it before the game elects Congress); a party
        /// holding neither introducing nothing (the plan's risk 18, stated); PRESIDENT where the game elected the party's candidate; the chip read through the
        /// controller's own `RoleChip`; no US party reads IN OPPOSITION; the revenue reading read off US-18's reconciliation table; R-US8 (a)'s words.
        /// </summary>
        private static void UsRoles(Action<bool, string> Check, StringBuilder sb)
        {
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                using (SimulationManager.EpochScope())
                {
                    var chipHost = new GameObject("PlayerRoleDiagnostic.Chip"); hosts.Add(chipHost);
                    chipHost.AddComponent<SimulationManager>();
                    var controller = chipHost.AddComponent<PoliSim.UI.GameController>();
                    string Chip(SimulationManager s, Country c)
                    {
                        PresidencyDiagnostic.ControllerField("_simulationManager").SetValue(controller, s);
                        PresidencyDiagnostic.ControllerField("_playerCountry").SetValue(controller, c);
                        object read = typeof(PoliSim.UI.GameController).GetMethod("RoleChip", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, null);
                        return read is ValueTuple<string, bool> chip ? chip.Item1 + (chip.Item2 ? " (filled)" : " (outlined)") : "NO CHIP";
                    }
                    string Count(DivisionRecord r) => r == null ? "none" : r.Cloture
                        ? F("{0} for, {1} needed of {2} sworn", r.Sides.Where(s => s.Side > 0).Sum(s => s.Seats), r.Required, r.Sworn)
                        : F("{0}-{1}", r.Sides.Where(s => s.Side > 0).Sum(s => s.Seats), r.Sides.Where(s => s.Side < 0).Sum(s => s.Seats));
                    string Name(int chamber) => Congress.ChamberName(CountryId.USA, chamber);

                    // ---- the revenue reading, read off US-18's reconciliation table (DECLARED: one classification of the bill kinds' revenue - Congress.IsRevenueKind)
                    var kinds = (Congress.Kind[])Enum.GetValues(typeof(Congress.Kind));
                    bool agrees = Congress.IsRevenueKind(Congress.Kind.TaxProgram) && Congress.IsRevenueKind(Congress.Kind.Budget) && !Congress.IsRevenueKind(Congress.Kind.WelfareProgram)
                                  && kinds.All(k => k == Congress.Kind.Budget || !Congress.IsRevenueKind(k) || Congress.IsReconciliationKind(k))
                                  && !Congress.IsRevenueKind(Congress.Kind.Trade) && !Congress.IsRevenueKind(Congress.Kind.Sector)
                                  && kinds.All(k => !Congress.IsRevenue(null, k));
                    Check(agrees, "(US) the revenue reading (Art. I §7 cl. 1) read off the reconciliation table: " + string.Join("; ", kinds.Select(k => k + " " + (Congress.IsRevenueKind(k) ? "REVENUE" : "ordinary")
                        + (k == Congress.Kind.Budget ? " (by content at the Senate)" : Congress.IsReconciliationKind(k) ? " / reconciliation" : " / cloture")))
                        + " - no revenue kind but the budget at cloture; the tariff and the sector bill as the table places them (its asks (a), (b))");

                    // ---- 12 Mar 2024 (the US start): the 118th of record - the House REP, the Senate DEM with its caucus - and Biden of record
                    DateTime start = WorldClock.StartDate(CountryId.USA);
                    (SimulationManager sim, World world, Country usa) = UsCongressDiagnostic.OpenUsa(hosts, start, "DEM");
                    DateTime today = sim.CurrentDate;
                    BillConcern demOrdinary = ParliamentSystem.GetCrimeJusticeBillConcern(usa, UsCongressDiagnostic.CrimeBill(usa, "DEM"));
                    bool demBudget = sim.PlayerMayIntroduce(CountryId.USA, Congress.Kind.Budget, null, out string demBudgetLock);
                    bool demBills = kinds.All(k => sim.PlayerMayIntroduce(CountryId.USA, k, null, out _)) && sim.PlayerMayIntroduce(CountryId.USA, out _);
                    string demChip = Chip(sim, usa);
                    Check(start == new DateTime(2024, 3, 12) && Congress.Applies(usa) && usa.Government.Kind == WorldClock.ExecutiveKind.Presidency && usa.Government.PmParty == "DEM" && !usa.Government.PresidencyIsTheGames()
                          && sim.PlayerGoverns(usa) && GovernmentRecord.RoleOn(usa, "DEM", today) == PlayerRole.PrimeMinister && demChip == "THE PRESIDENT'S PARTY (filled)" && demBudget && demBudgetLock == null && demBills
                          && Congress.OriginOf(usa, demOrdinary, today) == Congress.House,
                        F("(US) {0:yyyy-MM-dd}, a DEM player: {1} of record - DEM governs as the president's party (the chip \"{2}\"): the budget and every bill kind its own to introduce, the administration's bills begun in the {3}",
                            today, usa.Government.Executive, demChip, Name(Congress.OriginOf(usa, demOrdinary, today))));

                    // the review's M2: the president's party's own bill still costs the government - a DEM crime bill, introduced and voted on its day, dies in the
                    // REP House; the failed vote's approval and the day marked lost (F5) are the government's, as every government's own bill always was
                    usa.BillLostOn = DateTime.MinValue;
                    float demApprovalBefore = usa.State.ApprovalRating;
                    int demBefore = usa.Divisions.Entries.Count > 0 ? usa.Divisions.Entries.Last().Number : 0;
                    bool demIntroduced = sim.IntroduceCrimeJusticeBill(CountryId.USA, UsCongressDiagnostic.CrimeBill(usa, "DEM"));
                    for (int d = 0; d < ParliamentSystem.BillDurationDays && sim.GetPendingCrimeJusticeBill(CountryId.USA) != null; d++) { sim.AdvanceCrimeJusticeBillDay(CountryId.USA); }
                    List<DivisionRecord> demWritten = usa.Divisions.Entries.Where(r => r.Number > demBefore).ToList();
                    float demCharged = demApprovalBefore - usa.State.ApprovalRating;
                    Check(demIntroduced && Congress.GovernmentBears(usa, demOrdinary) && demWritten.Count == 1 && demWritten[0].Chamber == Congress.House && !demWritten[0].Passed
                          && Mathf.Abs(demCharged - Mathf.Min(ParliamentSystem.BillFailedApprovalCost, demApprovalBefore)) < 1e-4f && usa.BillLostOn == today.Date,
                        F("(US) the president's party's own bill pays (the review's M2): DEM's crime bill dies in the {0} {1}; the government's approval {2:0.00} -> {3:0.00} (the failed vote's {4:0.0}), the day marked lost {5}",
                            demWritten.Count > 0 ? Name(demWritten[0].Chamber) : "-", Count(demWritten.FirstOrDefault()), demApprovalBefore, usa.State.ApprovalRating, ParliamentSystem.BillFailedApprovalCost,
                            usa.BillLostOn == today.Date ? "yes" : "NO"));

                    // the same chamber under a president of REP's (planted - no record day before the game elects Congress has a party out of the White House holding the
                    // Senate alone): DEM holds the Senate and not the House - SENATE MAJORITY: an ordinary bill begun in the Senate, no revenue bill, no budget
                    GovernmentRecord ofRecord = usa.Government;
                    usa.Government = GovernmentRecord.OfPresidency("a planted president (REP)", "REP", today, "planted", "planted (PlayerRoleDiagnostic)");
                    try
                    {
                        bool senateOrdinary = sim.PlayerMayIntroduce(CountryId.USA, Congress.Kind.CrimeJustice, CabinetPortfolio.InteriorJustice, out _);
                        bool noRevenue = !sim.PlayerMayIntroduce(CountryId.USA, Congress.Kind.TaxProgram, CabinetPortfolio.FinanceTreasury, out string revenueLock);
                        bool noBudget = !sim.PlayerMayIntroduce(CountryId.USA, Congress.Kind.Budget, null, out string senateBudgetLock);
                        BillConcern demTax = ParliamentSystem.GetTaxProgramBillConcern(usa, new TaxProgramBill { Type = usa.TaxLines[0].Type, IsAdd = !usa.TaxLines[0].IsImplemented });
                        int lastBefore = usa.Divisions.Entries.Count > 0 ? usa.Divisions.Entries.Last().Number : 0;
                        usa.BillLostOn = DateTime.MinValue;
                        ParliamentSystem.Divide(usa, "Crime & Justice bill (PlayerRoleDiagnostic, the Senate's majority)", demOrdinary, today, out bool senatePassed);
                        bool senateLossUnmarked = usa.BillLostOn == DateTime.MinValue && !Congress.GovernmentBears(usa, demOrdinary);   // the review's M2: not the government's loss
                        List<DivisionRecord> senateFirst = usa.Divisions.Entries.Where(r => r.Number > lastBefore).ToList();
                        string senateChip = Chip(sim, usa);
                        Check(GovernmentRecord.RoleOn(usa, "DEM", today) == PlayerRole.SenateMajority && senateChip == "SENATE MAJORITY (outlined)" && !sim.PlayerGoverns(usa) && senateOrdinary
                              && noRevenue && revenueLock == "SENATE MAJORITY · A REVENUE BILL BEGINS IN THE HOUSE · YOUR PARTY HOLDS THE SENATE ALONE" && noBudget && senateBudgetLock == "SENATE MAJORITY · THE PRESIDENT PROPOSES THE BUDGET · YOUR PARTY VOTES ON IT"
                              && Congress.OriginOf(usa, demOrdinary, today) == Congress.Senate && demTax.Revenue && Congress.OriginOf(usa, demTax, today) == Congress.House
                              && senateFirst.Count == 1 && senateFirst[0].Chamber == Congress.Senate && senateFirst[0].Origin == Congress.Senate && senateFirst[0].Cloture && !senatePassed && senateLossUnmarked,
                            F("(US) planted, a REP president over the same chamber: DEM holds the Senate alone - the chip \"{0}\"; an ordinary bill may be introduced and begins in the Senate (the gate: {1}, {2}), never reaching the House, and is no loss of the government's ({5}); a revenue bill refused - \"{3}\"; the budget - \"{4}\"",
                                senateChip, senateFirst.Count == 1 ? Name(senateFirst[0].Chamber) : senateFirst.Count + " records", Count(senateFirst.FirstOrDefault()), revenueLock, senateBudgetLock,
                                senateLossUnmarked ? "the day unmarked" : "THE DAY MARKED LOST"));
                    }
                    finally { usa.Government = ofRecord; }

                    // a REP player on the same day: the House's majority - an ordinary bill introduced through the game's own entry point, voted on its day, dies in the Senate
                    usa.PlayerPartyAbbrev = "REP";
                    string repChip = Chip(sim, usa);
                    bool generalRefused = !sim.PlayerMayIntroduce(CountryId.USA, out string generalLock);
                    bool repBudgetRefused = !sim.PlayerMayIntroduce(CountryId.USA, Congress.Kind.Budget, null, out string repBudgetLock);
                    bool repOrdinary = sim.PlayerMayIntroduce(CountryId.USA, Congress.Kind.CrimeJustice, CabinetPortfolio.InteriorJustice, out string repOrdinaryLock);
                    bool repRevenue = sim.PlayerMayIntroduce(CountryId.USA, Congress.Kind.TaxProgram, CabinetPortfolio.FinanceTreasury, out _);
                    bool repTariff = sim.PlayerMayIntroduce(CountryId.USA, Congress.Kind.Trade, CabinetPortfolio.ForeignAffairs, out _);
                    Check(!sim.PlayerGoverns(usa) && usa.Government.RoleOf("REP") == PlayerRole.Opposition && GovernmentRecord.RoleOn(usa, "REP", today) == PlayerRole.HouseMajority
                          && Congress.HoldsHouse(usa, "REP") && !Congress.HoldsSenate(usa, "REP", today) && repChip == "HOUSE MAJORITY (outlined)",
                        F("(US) {0:yyyy-MM-dd}, a REP player: the House's majority, not the president's party - toward the sitting government in opposition (who governs), on the day HOUSE MAJORITY; the chip \"{1}\"", today, repChip));
                    Check(generalRefused && generalLock == "HOUSE MAJORITY · THE PRESIDENT'S ADMINISTRATION GOVERNS · YOUR PARTY INTRODUCES ORDINARY AND REVENUE BILLS IN THE HOUSE"
                          && repBudgetRefused && repBudgetLock == "HOUSE MAJORITY · THE PRESIDENT PROPOSES THE BUDGET · YOUR PARTY VOTES ON IT" && repOrdinary && repOrdinaryLock == null && repRevenue && repTariff,
                        F("(US) the REP House majority may introduce an ordinary bill, a revenue bill and a tariff (R-US8 (a): a bill through Congress); the budget is the president's - \"{0}\"; the government's own acts - \"{1}\"", repBudgetLock, generalLock));
                    CrimeJusticePolicyBill repBill = UsCongressDiagnostic.CrimeBill(usa, "REP");
                    int before = usa.Divisions.Entries.Count > 0 ? usa.Divisions.Entries.Last().Number : 0;
                    usa.BillLostOn = DateTime.MinValue;
                    float repApprovalBefore = usa.State.ApprovalRating;
                    bool introduced = sim.IntroduceCrimeJusticeBill(CountryId.USA, repBill);
                    // PS-6 US-21: REP's bill is on the veto path, so the gate draws defectors (R-US17 rule (c)) - the least recorded crossing planted, so this row holds what it was
                    // written for, not the hash of its title (the keyed draw stops it in the Senate too)
                    using (UsVeto.PlantIndex(0)) { for (int d = 0; d < ParliamentSystem.BillDurationDays && sim.GetPendingCrimeJusticeBill(CountryId.USA) != null; d++) { sim.AdvanceCrimeJusticeBillDay(CountryId.USA); } }
                    List<DivisionRecord> written = usa.Divisions.Entries.Where(r => r.Number > before).ToList();
                    // the review's M2: the House majority's bill is not the government's - its failure leaves the government's approval and its day (F5) untouched
                    BillConcern repConcern = ParliamentSystem.GetCrimeJusticeBillConcern(usa, repBill);
                    Check(introduced && !Congress.GovernmentBears(usa, repConcern) && usa.State.ApprovalRating == repApprovalBefore && usa.BillLostOn == DateTime.MinValue,
                        F("(US) the REP House majority's failed bill charges the government nothing (the review's M2, DECLARED): approval {0:0.00} -> {1:0.00}, the day {2}",
                            repApprovalBefore, usa.State.ApprovalRating, usa.BillLostOn == DateTime.MinValue ? "not marked lost" : "MARKED LOST " + usa.BillLostOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    // and a law it carried would be enacted without the government's enactment cost: the resolution site picks the effects by Congress.GovernmentBears -
                    // each effect read here on a law the catalog offers, its cost the law's own (a law needs cloture, so no REP-only law passes this Senate)
                    LawDefinition law = LawCatalog.All.FirstOrDefault(l => l.EnactmentApprovalCost > 0f && !usa.EnactedLaws.Exists(e => e.LawId == l.Id) && LawCatalog.IsWithinCompetence(world, usa, l));
                    if (law == null) { Check(false, "(US) no unenacted law with an enactment cost for the effects' row - this assertion VERIFIED NOTHING"); }
                    else
                    {
                        MethodInfo borne = typeof(SimulationManager).GetMethod("ApplyLawBillEffects", BindingFlags.Instance | BindingFlags.NonPublic);
                        MethodInfo unborne = typeof(SimulationManager).GetMethod("ApplyLawBillEffectsUnborne", BindingFlags.Instance | BindingFlags.NonPublic);
                        float a0 = usa.State.ApprovalRating;
                        unborne?.Invoke(sim, new object[] { usa, new LawBill { LawId = law.Id, IsRepeal = false } });
                        bool enactedFree = usa.EnactedLaws.Exists(e => e.LawId == law.Id) && usa.State.ApprovalRating == a0;
                        unborne?.Invoke(sim, new object[] { usa, new LawBill { LawId = law.Id, IsRepeal = true } });
                        float a1 = usa.State.ApprovalRating;
                        borne?.Invoke(sim, new object[] { usa, new LawBill { LawId = law.Id, IsRepeal = false } });
                        float paid = a1 - usa.State.ApprovalRating;
                        borne?.Invoke(sim, new object[] { usa, new LawBill { LawId = law.Id, IsRepeal = true } });
                        usa.State.ApprovalRating = a0;
                        Check(borne != null && unborne != null && enactedFree && Mathf.Abs(paid - Mathf.Min(law.EnactmentApprovalCost, a1)) < 1e-4f && !usa.EnactedLaws.Exists(e => e.LawId == law.Id),
                            F("(US) a law ({0}) enacted for a congressional majority costs the government nothing ({1}); enacted for the government it costs {2:0.00} - the law's own {3:0.0}",
                                law.Id, enactedFree ? "approval unmoved" : "APPROVAL MOVED", paid, law.EnactmentApprovalCost));
                    }
                    bool diesInSenate = introduced && sim.GetPendingCrimeJusticeBill(CountryId.USA) == null && written.Count == 2
                                        && written[0].Chamber == Congress.House && written[0].Origin == Congress.House && written[0].Passed && written[0].Motion
                                        && written[1].Chamber == Congress.Senate && written[1].Origin == Congress.House && written[1].Cloture && !written[1].Passed && ReferenceEquals(written[1].OtherChamber, written[0]);
                    Check(diesInSenate,
                        F("(US) the REP ordinary bill (a crime-and-justice bill, REP's own) introduced {0} and voted on its day: the {1} passes it {2}, the {3} stops it at cloture - {4}",
                            introduced ? "through IntroduceCrimeJusticeBill" : "NOT INTRODUCED", written.Count > 0 ? Name(written[0].Chamber) : "-", Count(written.FirstOrDefault()), written.Count > 1 ? Name(written[1].Chamber) : "-", Count(written.Skip(1).FirstOrDefault())));

                    // ---- the 119th of record under Trump of record (UsCongressDiagnostic's day: every seat held): REP the president's party, DEM holding neither
                    (SimulationManager sim119, World _, Country usa119) = UsCongressDiagnostic.OpenUsa(hosts, UsCongressDiagnostic.Day119, "REP");
                    DateTime day119 = sim119.CurrentDate;
                    string partyChip = Chip(sim119, usa119);
                    Check(usa119.Government.PmParty == "REP" && !usa119.Government.PresidencyIsTheGames() && sim119.PlayerGoverns(usa119) && partyChip == "THE PRESIDENT'S PARTY (filled)",
                        F("(US) {0:yyyy-MM-dd}, a REP player: {1} of record - the chip \"{2}\"", day119, usa119.Government.Executive, partyChip));
                    // a president the game elected (planted as the oath installs one): PRESIDENT - "winning makes the player president"
                    GovernmentRecord recordRep = usa119.Government;
                    var y2024 = Array.Find(PoliSim.Elections.Generated.UsPresidentialReturns.Years, y => y.Year == 2024);
                    DateTime oath = UsPresidentialElection.OathDayAfter(new DateTime(2024, 11, 5));
                    usa119.Government = GovernmentRecord.OfPresidency(y2024.NomineeR + " (REP)", "REP", oath, UsPresidentialElection.ElectedOutcome, "planted (PlayerRoleDiagnostic)");
                    string presidentChip;
                    try { presidentChip = Chip(sim119, usa119); }
                    finally { usa119.Government = recordRep; }
                    Check(presidentChip == "PRESIDENT (filled)", F("(US) a presidency the game elected (planted as the oath installs one, {0:yyyy-MM-dd}): the chip \"{1}\"", oath, presidentChip));
                    // DEM in the 119th: neither the presidency nor a chamber - it introduces nothing; the one US role the plan lets read as having nothing to do (risk 18, stated)
                    usa119.PlayerPartyAbbrev = "DEM";
                    string minorityChip = Chip(sim119, usa119);
                    bool ordinaryRefused = !sim119.PlayerMayIntroduce(CountryId.USA, Congress.Kind.CrimeJustice, CabinetPortfolio.InteriorJustice, out string minorityLock);
                    bool nothing = ordinaryRefused && kinds.All(k => !sim119.PlayerMayIntroduce(CountryId.USA, k, null, out _))
                                   && !sim119.IntroduceCrimeJusticeBill(CountryId.USA, UsCongressDiagnostic.CrimeBill(usa119, "DEM"));
                    Check(GovernmentRecord.RoleOn(usa119, "DEM", day119) == PlayerRole.Minority && !Congress.HoldsHouse(usa119, "DEM") && !Congress.HoldsSenate(usa119, "DEM", day119) && minorityChip == "MINORITY (outlined)"
                          && nothing && minorityLock == "MINORITY · " + Congress.HoldsNothingWords,
                        F("(US) {0:yyyy-MM-dd}, a DEM player: neither the presidency nor a chamber - the chip \"{1}\"; every bill refused - \"{2}\" (the plan's risk 18, stated, not padded)", day119, minorityChip, minorityLock));

                    // ---- no US party reads IN OPPOSITION on these days, and only a party holding neither has nothing to introduce
                    var rows = new List<string>();
                    bool noOpposition = true;
                    foreach ((Country c, DateTime on) in new[] { (usa, today), (usa119, day119) })
                    {
                        foreach (string party in new[] { "DEM", "REP" })
                        {
                            PlayerRole role = GovernmentRecord.RoleOn(c, party, on);
                            bool holdsNothing = c.Government.PmParty != party && !Congress.HoldsHouse(c, party) && !Congress.HoldsSenate(c, party, on);
                            if (role == PlayerRole.Opposition || (role == PlayerRole.Minority) != holdsNothing) { noOpposition = false; }
                            rows.Add(F("{0:yyyy-MM-dd} {1} {2}", on, party, role));
                        }
                    }
                    Check(noOpposition, "(US) no US party reads IN OPPOSITION, and MINORITY only where it holds neither the presidency nor a chamber: " + string.Join("; ", rows));
                    sb.Append("    reading   R-US8 (a), stated on the trade bill: \"").Append(Congress.TariffPowersWords).Append("\"\n");
                }
            }
            catch (Exception e) { Check(false, "(US) threw - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                SimulationRandom.RestoreState(seedWas, drawsWas);
                EnergyMarket.ResetTurnState();
            }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
