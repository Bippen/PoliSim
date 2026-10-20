using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoliSim.Data;
using PoliSim.Simulation;

namespace PoliSim.Persistence
{
    /// <summary>Thrown for every refused or failed load, always with a message written for the
    /// player-facing surface that will eventually show it - never a bare stack trace.</summary>
    public class SaveLoadException : Exception
    {
        public SaveLoadException(string message) : base(message) { }
    }

    /// <summary>
    /// Serialization, version gate, atomic file IO, and the zone-identity assert - the whole
    /// mechanism half of item 8 (the mechanism report in CLAUDE.md is the reference; the shapes are
    /// in SaveGame.cs). Engine-free except for <see cref="DefaultSaveDirectory"/>, so the Editor
    /// diagnostic can run all of it in batch mode against in-memory strings and temp paths.
    /// </summary>
    public static class SaveGameService
    {
        /// <summary>
        /// The format epoch. Ruling A (2026-08-16): additive model changes do NOT bump this -
        /// MissingMemberHandling.Ignore plus field defaults absorb them for free. A model SWAP that
        /// re-keys or replaces persisted state (item 10's political overhaul is the named case:
        /// ParliamentSeats is keyed by PartyArchetype, which it retires) BUMPS this, and the loader
        /// refuses the older save with a plain message. No migration machinery pre-release.
        /// </summary>
        // W-G1 (2026-08-30) bumped this 1 -> 2: `Country.ParliamentSeats` was re-keyed from
        // PartyArchetype (four shared fictional archetypes) to the real parties of each country.
        // This is precisely the "model SWAP that re-keys persisted state" the paragraph above names,
        // and older saves are refused with a plain message rather than migrated.
        //
        // C-D4 (2026-08-31) bumps this 2 -> 3: `Country.PartyCapital` adds a per-party long-term
        // political-capital record (§38, R-CL3) to the persisted World graph. ⚠ Strictly the field is
        // ADDITIVE and an older save would load with an empty list rather than fail - the same shape as
        // C-C2's incoming-budget-window flag, which deliberately did NOT bump. It bumps anyway, and the
        // reason is that an empty list is not a harmless default here: it would silently mean "no party
        // holds any political capital", which is a DIFFERENT game state from "every party opens at 50",
        // and the next election's carry-over would read it as such. R-CL3 ruled the bump in for exactly
        // that reason. Older saves are refused with a plain message; no migration machinery pre-release.
        //
        // C-R2/C-R3 (2026-08-31) bump this 3 -> 4: `Country.PlayerPartyAbbrev` and
        // `Country.PartyApprovalRating` add the player's PARTY and its own approval stock to the
        // persisted World graph (R-CL1). Same reasoning as the 2 -> 3 bump: an absent party is not a
        // harmless default, it is a DIFFERENT game state - "this save was played without a party" -
        // and selection would silently seat one on load. Older saves refused plainly; no migration.
        // 22: PN-3b (2026-09-22, §577) - the wage bill's base is PotentialOutput.LabourInput × the real wage (the 15+ population at the state's rate, employed) where it was the 20–64 cohort × that same 15+ rate: a save from before carries the WINDOW's level as the driver's seed in RevenueBaseSeeds, and the re-formed level read against it would price every wage-taxed base at the ratio of the 15+ population to the 20–64 cohort on the first day - about half as much again. 21: FT-10 · P-A (2026-09-21, §545) - the levy rule reads K, the seed's support line over the seed's levy (EnvironmentSeeds.EnergySupportLineToLevySeed), captured at the seed: a save from before carries none, so its levy would stand at the seed whatever the line did, and its saved path was run on the compounding scale. 20: P6-E1's yield (2026-09-18, §538) - the seed AER is captured with the credit and Sweden's lever seeds at the credited average; a 19 save carries the uncredited seed AER and would read its yield ratio at 0.74. 19: PN-3 (2026-09-16, §522) - potential's labour input is the 15+ population at the state's participation rate, employed (PotentialOutput.LabourForce), and the seed's labour input (PotentialLabourSeed) is captured in that form: a save from before carries the 20–64 window's seed, which the re-formed input would read as a labour ratio near 1.5 on load; 18: PN-1's driver (2026-09-16, §520) - the pension line's driver is the cohort at the statutory age in force (SpendingDrivers.StatutoryPensionAge) and the country carries the calendar year it reads (CalendarYear): a save from before carries the line's driver reference as the 65+ cohort, which the first index would read against the statutory cohort as a one-off fall of up to a seventh of the line; 17: F4-5 (2026-09-16) - the income lever of the four bracketed schedules is seeded at the schedule's own average rate on the income it taxes (Germany 26.92, France 17.39, Poland 12.36, the USA 17.10) and each base re-derived for it, the USA's on the federal series: a save from before carries a line anchored on the statutory TOP rate, which the re-derived base would read as up to two and a half times the revenue; 16: F4-3 (2026-09-15) - Sweden's income-tax line is seeded at the municipal layer's 32.38 and its base re-derived for it (TaxBaseTable): a save from before carries a line anchored on the blended 52, which the re-derived base would read as 60 % more revenue; 15: T-3 form A (2026-09-15) - the country carries k, the identity's government consumption over its lines (GovernmentConsumptionScale): a save from before carries none, so its identity would read no government at all; 14: EN-7b (2026-09-15) - the electricity tax's statute and its base per class ride the country (a save from before carries neither, so a law would compose on zero and the stack would read no statute); 13: CL-2 (2026-09-13) - stories break in the live campaign and the player's answers ride the record (ScandalAnswers): a campaign saved before would replay under the live rate onto stories its run never drew; 12: F4-4 (2026-09-13) - the income line carries its own rates per sub-row (BracketRates) and the drafts their sub-row entries; 11: F4-2 (2026-09-13) - the income tax line carries its seeded rate (RateSeed) and the country the statute's seed yield (IncomeTaxSeedAer); 10: F4-1 (2026-09-12) - the pyramid carries its income dimension (IncomeMedian, IncomeSigma, unit, concept); 9: CL-1 (2026-09-12) - the run-up on the campaign record (PreCampaignDays, PreCampaignDaysStepped, a queued hire's Role); 5: F2 step 4 (the substrate is the demography); 6: C-R4b step 3 (the player campaign's replay record); 7: EN-4c (the carbon tax's rate is the country's currency per tonne - a saved 30 would read as 30 kronor); 8: EN-4d (the taxed tonnes are transport's - a saved emissions reference of power + transport would read the carbon base at a fraction of itself on load, and the saved fiscal path was run on the wider base)
        // NOT bumped for GA-8-1 (§816, 2026-10-14) under ruling A: `TaxLine.PassedForYear` is additive - 0 in an older save, and 0 means the carbon
        // statute governs at the next boundary, as the older build did. What such a save keeps, not migrated: a carbon figure passed in the year it
        // was saved is overwritten once more at the next boundary; Germany's carbon rate carries the old clock's lag past the schedule (a Swedish
        // start's turn 1 holds 45 and carries it; from turn 2 it sits two or three price ratios low, a German, US or French start's from turn 3
        // one ratio low, an Italian start's from turn 4 one ratio high); and every levied tax line holds the draft the Budget page used to write
        // every frame (GA-8-2), so a line a boundary has moved since (the Finance partner's step) is named and set back by the next bill - nothing
        // marks those drafts, so they cannot be told from a move on load.
        // Nor for AF-36 (2026-10-19, Elias's ruling q36) under ruling A: `BudgetBill.CarriedTaxLines`, `TaxLine.CarriesPassed` and `UiDraftState.TaxCarryDrafts` are additive -
        // empty in an older save, a pending bill carrying nothing and no carry drafted, as that build had none. What such a save keeps, not migrated:
        // a carbon stamp (`TaxLine.PassedForYear`) left standing after the statute took its figure back, which the statute's step now clears - the
        // slip, the Desk and the player's draft read it as the statute's figure (`CarbonRateStatute.PassedFigureStands`, so no carry of it is
        // drafted), and the next boundary that moves the line clears it.
        // Nor for GA-11-1 (§817, 2026-10-14) - no field changes: an older save carries the stale Gini anchor (`Country.BaselineIncomeTaxRate` at the
        // income line's literal - France and Germany 45, Poland 32, the USA 37, Sweden 32.38), nothing re-takes it on load, and that game's Gini goes
        // on drifting toward the displaced target.
        // Nor for GA-1-3 (§818, 2026-10-14) - no field changes: a save cut during a run-up or a campaign in a session that had loaded before carries
        // the stream counts the old shadow froze; its first load logs the CAMPAIGN REPLAY alarm once, and a frozen run-up count makes the next
        // cycle's run-up draw numbers it has drawn.
        // Nor for GA-3-4 (§819, 2026-10-14) under ruling A: `GovernmentRecord.OfRecord` is additive - false in an older save, whose government of
        // record still says "of record", which the economic vote reads as before. What such a save keeps, not migrated: one cut after a supporter
        // withdrew or a partner left carries the label the exit left standing ("of record", a majority the seats no longer give), and nothing
        // relabels on load - it stands until the next exit or the next government.
        // Nor for GA-0-14 / GA-0-40 (§825, 2026-10-14) under ruling A: the challenge scenarios are retired and `UiDraftState` drops its two
        // scenario keys (`Scenario`, `ScenarioVerdictPending`), which an older save's reader reads past. What such a save keeps, not migrated:
        // the scenario's start, baked into its world (Sweden's debt and the fund it was handed; Italy's debt at 165 % of GDP); a pending
        // verdict is dropped, the run going on as free play; a verdict already closed keeps its game over. `RestoreFromSave` names the scenario.
        // Nor for PS-6 US-28 (2026-10-16) under ruling A: `Country.UsNomineePicks`, the player's pick of a term-limited party's nominee (`UsTermLimit.Pick`), is
        // additive - an older save carries none, and none is the default (the Vice President), the only state a build before it could hold; US-28b (2026-10-18, R-US26 (a))
        // widens the same list to the out-party's pick, its default unnamed - still additive, no format step; each person's
        // elections, which the limit reads, are derived from the record and the contests already saved, never stored. Nothing is kept unmigrated.
        // Nor for PS-6 US-33 (2026-10-17) under ruling A: `SaveGame.OwnNominees`, the player's own nominee at the head of a major ticket (`OwnNominees`), and
        // `UsPresidentialElection.Tally.OwnNominee`, which marks the line it stood on, are additive - an older save carries neither, and neither is what such a
        // save means: no own nominee (the nominee of record stands, every party at its roster point), the only state a build before them could hold; the
        // created parties' precedent (§671). The migration is that reading, held by UsOwnNomineeDiagnostic on a save with both keys stripped. Nothing is kept unmigrated.
        // Nor for PS-6 US-21b (2026-10-18) under ruling A: the player-president's docket - `DocketEntry` on each pending bill (`Docket`; a budget's `ActsHeld`,
        // `ActsLaw`, `ActsFallen`) - is additive. An older save carries none, and none is what it means: before the docket a player-president answered the
        // day a bill was presented, so no bill of such a save was ever held. Nothing is kept unmigrated. THE OTHER DIRECTION IS NOT SAFE, and no format step
        // guards it: a format-41 save cut by this build with a bill held, opened in an older build, drops the docket - the held bill counts down again from 0
        // and is voted afresh the next day, and a budget whose acts were held votes them all again, a fallen act charged a second time.
        // Nor for GA-1-2 (§821, 2026-10-14) under ruling A: `UiDraftState.CabinetShortlists` is additive - an older save carries none. What such
        // a save keeps, not migrated: one cut between a cabinet Search and its appointment loads with no shortlist, and its next Search draws the
        // cabinet stream again (GA-1-2's divergence). GA-1-4 changes no saved field.
        // Nor for AF-8 (ruling q8, 2026-10-19) under ruling A: `GovernmentRecord.Sejm`, Poland's formation after a Sejm election (Konstytucja Art. 162, 154), is
        // additive - an older save carries none, and none is what it means: before AF-8 a Polish night installed the formation's government at once, so no
        // such save holds a procedure in progress. What such a save keeps, not migrated: one cut between a Polish polling day and the record's 13 Dec 2023
        // holds the government the old night installed, from polling day. The other direction drops a procedure in progress (the outgoing cabinet governs on).
        // Nor for A-LOOP q34 (2026-10-19) under ruling A: `Country.SeedLevied`, which lines the seed levied (the burden's seed side), is additive - an older
        // save carries none, and `SimulationManager.RestoreSaveState` reads it as the lines levied at the load, the older build's own reading (its burden
        // counted only today's levied lines on both sides), so the game loads to the burden it was saved at. What such a save keeps, not migrated: a line
        // levied or removed before it was cut reads as the seed's (no gap from it); a levy or a removal after the load opens the gap. The other direction
        // drops the set, and the older build reads its burden as it always did.
        public const int CurrentSaveVersion = 50;   // 50: SP-7b (2026-10-20) - Country.PlayerCandidate (the candidate of record the player stands as at Poland's presidential start), Country.PlayerStart (the start's key - null for the country's default start), Country.PlayerHoldsPresidency (the player-president's office, which the role gate, the party's own verbs, the chairs and the Finance partner read), PresidentialElection.Contest.PlayerCandidate (the player's candidate in a contest) and DocketEntry.Bill (the kind of bill a Polish term is read by, J6 (iii)). A v49 save carries none, and every v49 save is a Sejm-start game with no candidate, so additive defaults would load it to the same game; the step is the plan's (POLAND_PRESIDENTIAL_START_PLAN.md, its item 9: one format step carrying the candidate, the start's key and the office), CONSERVATIVE under ruling A. Its migration is ruling A's: a v49 save is refused with the plain message, and the play saves are re-staged   // 49: PS-6 US-16 (2026-10-20) - Country.UsCongressElections (the House and the Senate seats the game elected on each presidential polling day, each state's share, delegation and unopposed seats the next count's base) and, on each UsPresidentialElection.Contest, TieSeed (US-8c: the seed its exact ties were drawn on, which StatesOf re-derives the count's calls with) and SenateChoice, Acting, ActingParty, ActingAs (R-US22: the Senate's choice of the Vice President, and who acts as President from an undecided contest's oath). A v48 save cut past 5 Nov 2024 carries a presidential count and no Congress count, so additive defaults would load a world that never elected its Congress: on 3 Jan 2025 the record would seat its own 119th over the House and Senate the continuous run elected, and a later count would swing from the record's base, not the game's. Its migration is ruling A's: a v48 save is refused with the plain message, and the play saves are re-staged   // 48: A-LOOP q5 (2026-10-19) - Country.SalienceShift (what events still hold of the country's salience above its sourced seed, held then decaying), Country.SalienceHoldDays (the days each issue's jump is still held flat before it decays) and PlayerCampaignRecord.SalienceShift (that shift as the campaign's window opened, which every staging of the campaign reads so a load replays the same campaign). A v47 save carries neither: additive defaults would load the shift at null - a game cut while an event's jump stands would read every issue at its seed from the load, while the continuous run of this build carries the jump for months (its fitted half-life), and the AI's bill votes and the campaign's relevance part between the two; and a v47 save was cut by a build whose events moved no salience at all. Its migration is ruling A's: a v47 save is refused with the plain message, and the play saves are re-staged   // 47: A-LOOP q24 5a (2026-10-19) - no field is added: the campaign's stored state (PlayerCampaignRecord - its queue, the run-up's days stepped) no longer MEANS what it did, because a campaign restores by replaying that queue through the model and the model changed under it - the campaign moves shares by sourced CACEs times coverage, not compatibility points, and the AI scores in share points, so a v46 save cut mid-campaign would replay to a different campaign than the one saved (`AssertReplayLandedOn` logs it); and in the run-up a queued OFFICE is now refused (out until 5b) and a queued TV BUY is now charged its price, so a v46 run-up replays to a different chest. Its migration is ruling A's: a v46 save is refused with the plain message, and the play saves are re-staged
        // 46: A-LOOP q13 (2026-10-19) - Country.FiscalVoteRecord (the closed fiscal years the vote reads, each the year as the player reads it with nominal GDP at its close, and the open year's flow to date with its days - Brender & Drazen's two balance variables are formed on it). A v45 save carries none, and nothing in it rebuilds the book: the annual balance series has no GDP beside it and the fiscal period's accrual is not on the country, so additive defaults would load an empty book - the government in office at the load would carry no fiscal term at its next count (and a short one for four years after) while the continuous run carries the whole. Its migration is ruling A's: a v45 save is refused with the plain message, and the play saves are re-staged   // 45: A-LOOP q15 (2026-10-19) - Country.ApprovalEconomyStock (the part of approval its three economy terms built, which the vote reads approval net of) and Country.VoteApprovalMarks (net approval as each government found it, the approval term's reference). A v44 save carries neither: additive defaults would load the stock at zero - a game cut after a recession would read its whole economic drag on approval into the vote a second time - and no mark, so the government in office at the load would carry no approval term at its next count while the continuous run carries one. Its migration is ruling A's: a v44 save is refused with the plain message, and the play saves are re-staged   // 44: A-LOOP q37 (2026-10-19) - Country.TaxTermStock (the tax term T, the approval the government's tax changes still hold against it, fading) and Country.TaxTermNet (each line's booked net since the fiscal year's opening, which every later move in the year is read against). A v43 save carries neither: additive defaults would load T at zero - a game cut after a rise would forgive its standing penalty at the load, and approval would never be handed back what the old build's charge took - and the year's net would start empty at the load, so a cut and its restore in the same fiscal year across the load would no longer net. Its migration is ruling A's: a v43 save is refused with the plain message, and the play saves are re-staged   // 43: AF-7 (2026-10-19) - FedChair.TermBegan and TermEnds (each central bank head's statutory term, `CentralBankTerms`) and CurrencyZone.Head (the ECB's President for the euro members): a v42 save's heads carry no term, and a head with none never ends (a harness's hand-made head), so a v42 game would keep its heads forever where the continuous run replaces them on the statute's date; any default invents a term the run never had. Its migration is ruling A's: a v42 save is refused with the plain message, and the play saves are re-staged   // 42: PS-6 US-22b (2026-10-18) - Country.LapseFlowDay, LapseDeferredOutlays, LapseOutputWedge, LapseOutputDays and LapseProfileQuarter (the lapse's economics, Elias's ruling I13 (1)): a save cut mid-lapse must carry the outlays a lapse stopped (paid in full when funding returns), the overlay on output the next day removes and the quarterly profile still to run, or the loaded run and the continuous one part. A v41 save stopped nothing (under v41 a lapse moved no figure), so additive defaults would load it - but a v41 save cut mid-lapse would then count its lapse from the load, the days before it unstopped and unscaled; the step is CONSERVATIVE under ruling A (the review's L4), not forced. Its migration is ruling A's: a v41 save is refused with the plain message, and the play saves are re-staged   // 41: PS-6 US-22 (2026-10-17) - Country.AppropriatedThrough, LapseSince and ContinuingResolutionWithheldBy (the funding horizon, the lapse and the player's withheld continuing resolution) and BudgetBill.ContinuingResolution (a pending continuing resolution): the plan's step (US-22, section K: "a step to 41 (recommended)"). A v40 save holds no funding horizon, and any default invents a funding state the continuous run never had - seeding the end of the fiscal year in progress would shut a v40 US save cut on 2024-06-01 down on 1 Oct 2024, since no request-day tabling happened under v40's calendar; the lapse decision reads the horizon (US-7's step-39 argument). Its migration is ruling A's: a v40 save is refused with the plain message, and the play saves are re-staged   // 40: PS-6 US-15 (2026-10-17) - Country.SecondChamberSeats, the second chamber: the USA's Senate of record, seated on its dates in the player's country, and generic, so Italy's Senato (PS-7) seats into it too. The plan names the step (US-15: "the save carrying it - a format step, its migration, and the field in ClonePreviewCountry's hand-list"); its migration is ruling A's - a v39 save is refused with the plain message, and the play saves are re-staged. Strictly the USA's Senate of record would be put right as a v39 save loads, as US-2's House is (held as state: SimulationManager.SeatTheRecordOnItsDate runs in RestoreInto, so a v40 save's Senate is checked against the record as it loads too); the step is taken for the stored field, which a second chamber the game elects (US-16's classes, PS-7's Senato) cannot re-derive from the date   // 39: PS-6 US-7 (2026-10-16) - Country.UsPresidentialElections, the US presidential elections the game held (the polling day, the electors, the electors' meeting, the count before Congress, the House's contingent choice, the winner and the oath's day), and SimulationPendingState.IncomingBudgetWindowOpenNow (which open budget process is the arrival window - the oath reads it, so a load must keep it). The INTENT is one step for US-7 and US-8 together (the sweep's plan): from US-8 a v38 save cut past a US polling day would carry an election never counted and seat the record's president over the game's own; under US-7 alone nothing in play holds a contest (a diagnostic plants them), so for the contests a v38 save would load to the same state, and the step is taken here for the pair. NOT CARRIED: the count's per-state results - a second in-game US count swings from the previous election's states (R-US14 (a), R-US11 (a)) and will need them in the save, so that count may owe a step of its own. US-8 (2026-10-16) answers it: NO STEP - each contest keeps the national shares its count read (the two nominees and every other candidate together), from which UsPresidentialElection.StatesOf re-derives the states exactly (the same function on the same doubles; UsPresidentialElectionDiagnostic holds it), and before US-26 a later count swings from the record's states, not the game's (R-US11 (a) is US-26's to build, with its own step if it stores more)   // 38: PS-5 item C4 (2026-10-04, §770) - Country.PresidentialElections, the presidential elections the game held: a v37 save carries none, and one past the game's first vote would seat the president of record over the game's own (one between the rounds would lose its run-off)   // 37: PS-3i-2b (2026-09-29, §654) - AgreementItem.BudgetVotesOwed, the budget votes an owed item has waited through: a v36 save carries none and every owed item would restart its count   // 36: PS-3i-2c (2026-09-29, §653) - SpeakerRound.ReadsOn / MidTerm, what a round reads (an election's polling day, or the mid-term reading): a v35 round carries neither and would read its vintage whole, a different round   // 35: the formateur (2026-09-26, §646) - GovernmentRecord.Round, the Speaker's round (who is asked, the proposal on the table, its vote's day, the rejections): a v34 save carries no round, so one saved mid-round could not exist, and a v34 caretaker's pending procedure (§640) would load under the round's rules without the round   // 34: PS-3k (2026-09-25, §638) - PlayerCampaignRecord.RecordShift, the government's record judged at the campaign's opening; a v33 save made mid-campaign would replay its campaign without the record and count the election so, silently   // 33: PS-3i (2026-09-25, §636) - GovernmentRecord.NoConfidenceOn/Mover/Caretaker, the pending extra election   // 32: PS-3h (2026-09-25, §635) - GovernmentRecord.Agreements (the support agreements' items)   // 31: PS-3g (2026-09-25, §634) - GovernmentRecord.Portfolios by party   // 30: PS-3f (2026-09-25, §633) - DivisionRecord.Contest (a budget division's two proposals) and GovernmentRecord.Breaks   // 29: PS-3e (2026-09-25, §632) - the government's budget bill and the tabled alternative ride the pending state (BudgetBill.GovernmentBill/TabledBy, PendingBudgetAlternatives)   // 28: PS-3b (2026-09-25, §629) - GovernmentRecord.Kind and Executive (the president of record); a v27 record has neither and the USA's would read as a cabinet   // 27: PS-3a (2026-09-25, §628) - Country.Government, who governs, rides the World; a v26 country has none and the world would re-seat it as the player's   // 26: PS-2 / CL-4 (2026-09-25, §619) - an election is held on the country's real polling day: ElectionRecord.Date and the pending verdict's date; a v25 save's records carry a turn only   // 25: PS-1 (2026-09-25, §618) - the save carries its epoch (the chosen country's start); a v24 save has none and would restore its clock against whatever epoch the process holds   // 24: K-1 part (3) (2026-09-24, §604) - the epoch is 1 October 2026; a v23 save's date and turn were cut against 1 January, so its turn boundaries, election date and calendar year would read wrong under the new epoch
        // 23: K-1 part (1) (2026-09-23) - the seeded chamber is the Riksdag elected 2026-09-13; a v22 save embeds the 2022 chamber and its capital baselines, which the loader would keep (ParliamentSystem only fills an empty chamber) - a different game

        /// <summary>
        /// One settings object, built fresh per call (JsonSerializerSettings is mutable; sharing a
        /// static instance invites a distant caller to reconfigure everyone else's serialization).
        ///
        /// - **PreserveReferencesHandling.Objects** is hazard 1: Germany/France/Italy hold ONE
        ///   CurrencyZone instance, and only $id/$ref keeps it one object through a round trip.
        ///   Objects, not All: no list in the graph is shared, and array tracking would only add
        ///   noise to the populate-into-readonly path.
        /// - **MissingMemberHandling.Ignore** + TypeNameHandling.None are the version-tolerance
        ///   posture (additive drift free, no $type coupling to type names).
        /// - **ObjectCreationHandling stays Auto** deliberately: every readonly collection in the
        ///   graph is initialized EMPTY at declaration, so populate-in-place is correct - and the
        ///   round-trip diagnostic asserts collection counts precisely because this reasoning is a
        ///   claim about the graph, not about Json.NET (hazard 3).
        /// </summary>
        public static JsonSerializerSettings BuildSettings()
        {
            return new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                Formatting = Formatting.Indented,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
            };
        }

        /// <summary>
        /// Builds the root save object from the live game. <paramref name="ui"/> is null for saves
        /// written by batch tools. <paramref name="savedAtUtc"/> exists so the diagnostic can pin the
        /// one nondeterministic field and compare whole serialized saves for equality; real callers
        /// omit it.
        /// </summary>
        public static SaveGame CreateSaveGame(SimulationManager sim, World world, CountryId playerCountryId, UiDraftState ui, DateTime? savedAtUtc = null)
        {
            return new SaveGame
            {
                SaveVersion = CurrentSaveVersion,
                SavedAtUtc = savedAtUtc ?? DateTime.UtcNow,
                PlayerCountryId = playerCountryId,
                MasterSeed = SimulationRandom.MasterSeed,
                RngDrawCounts = SimulationRandom.CaptureDrawCounts(),
                CurrentTurn = sim.CurrentTurn,
                CurrentDate = sim.CurrentDate,
                EpochDate = SimulationManager.EpochDate,   // PS-1 (§618): the world's start travels with the game
                CurrencyZoneGroups = CaptureZoneGroups(world),
                World = world,
                CreatedParties = PoliSim.Data.CreatedParties.All(),   // §671 (SP-3)
                OwnNominees = PoliSim.Data.OwnNominees.All(),   // PS-6 US-33 (SP-6a)
                Sim = sim.CaptureSaveState(),
                PlayerCampaign = sim.CampaignRecord,
                Ui = ui
            };
        }

        /// <summary>The zone-identity partition as it actually exists in the live graph: one inner
        /// list per distinct CurrencyZone INSTANCE (reference identity, not value equality - two
        /// zones at the same rate are still two zones).</summary>
        public static List<List<CountryId>> CaptureZoneGroups(World world)
        {
            var groups = new List<List<CountryId>>();
            var zoneToGroup = new Dictionary<CurrencyZone, List<CountryId>>();
            foreach (Country country in world.Countries)
            {
                if (country.CurrencyZone == null)
                {
                    continue;
                }

                if (!zoneToGroup.TryGetValue(country.CurrencyZone, out List<CountryId> group))
                {
                    group = new List<CountryId>();
                    zoneToGroup[country.CurrencyZone] = group;
                    groups.Add(group);
                }

                group.Add(country.Id);
            }

            return groups;
        }

        public static string Serialize(SaveGame save)
        {
            return JsonConvert.SerializeObject(save, BuildSettings());
        }

        /// <summary>
        /// The version gate runs BEFORE the full deserialize, against the raw parse - a save from
        /// after a model swap may not even deserialize into today's types, and the player deserves
        /// the version message rather than whatever exception that would produce. Then hazard 1's
        /// restore assert runs on the result, so no caller can obtain a world with a silently split
        /// currency zone.
        /// </summary>
        public static SaveGame Deserialize(string json)
        {
            int version;
            string retiredScenario;
            try
            {
                JObject root = JObject.Parse(json);
                version = root.Value<int?>("SaveVersion") ?? -1;
                // GA-0-14 / GA-0-40 (§825): the challenge scenarios are retired and the save's UI layer no longer has their keys - a save that
                // carried one's progress loads as free play, and its id rides out here so the load can say so
                retiredScenario = root["Ui"] is JObject ui && ui["Scenario"] is JObject scenario ? (scenario["ScenarioId"] as JValue)?.Value as string : null;
            }
            catch (JsonException)
            {
                throw new SaveLoadException("This file is not an Incumbent save (it does not parse as one).");   // the player-facing name (\xC2\xA7651); this file stays engine-free
            }

            if (version != CurrentSaveVersion)
            {
                throw new SaveLoadException(
                    $"This save uses format version {version}; this build reads version {CurrentSaveVersion}. " +
                    "Pre-release saves do not carry across simulation-model changes - start a new game. (Ruling A, 2026-08-16.)");
            }

            SaveGame save = JsonConvert.DeserializeObject<SaveGame>(json, BuildSettings());
            if (save?.World == null || save.World.Countries == null || save.World.Countries.Count == 0)
            {
                throw new SaveLoadException("This save parsed but carries no world - it is truncated or corrupt.");
            }

            AssertZoneIdentity(save);
            save.RetiredScenarioId = retiredScenario;
            return save;
        }

        /// <summary>
        /// Hazard 1's assert: the restored graph must reproduce the saved zone partition as
        /// REFERENCE identity - every country in a recorded group shares literally one CurrencyZone
        /// object, and no two distinct groups share one. Throws rather than warns: a world that
        /// fails this looks healthy and simulates wrong (a Eurozone rate change stops reaching two
        /// of its three members), which is the worst available failure shape.
        /// </summary>
        public static void AssertZoneIdentity(SaveGame save)
        {
            var seenZones = new List<CurrencyZone>();
            foreach (List<CountryId> group in save.CurrencyZoneGroups)
            {
                CurrencyZone shared = null;
                foreach (CountryId id in group)
                {
                    Country country = save.World.GetCountry(id);
                    if (country?.CurrencyZone == null)
                    {
                        throw new SaveLoadException($"Corrupt save: {id} lost its currency zone in the round trip.");
                    }

                    if (shared == null)
                    {
                        shared = country.CurrencyZone;
                    }
                    else if (!ReferenceEquals(shared, country.CurrencyZone))
                    {
                        throw new SaveLoadException(
                            $"Corrupt save: the shared currency zone containing {group[0]} restored as more than one object - " +
                            "a rate change would no longer reach every member. Refusing the load.");
                    }
                }

                foreach (CurrencyZone earlier in seenZones)
                {
                    if (ReferenceEquals(earlier, shared))
                    {
                        throw new SaveLoadException("Corrupt save: two distinct currency zones restored as one object.");
                    }
                }

                seenZones.Add(shared);
            }
        }

        /// <summary>
        /// The one restore orchestrator, used by BOTH real load and the diagnostic - RNG restore and
        /// manager restore travel together on purpose (the AdvanceCountryDayTick lesson: two callers
        /// each carrying the list is how the list drifts). The caller still owns re-resolving its own
        /// references INTO the restored world by id (hazard 2) - see GameController.RestoreFromSave.
        /// </summary>
        public static void RestoreInto(SimulationManager sim, SaveGame save) => RestoreInto(sim, save, seatsPlayer: true);

        /// <summary>LEDGER-1: <see cref="RestoreInto(SimulationManager, SaveGame)"/>, where <paramref name="seatsPlayer"/> false restores the world with NO player
        /// seated - the impact ledger's fork of a world that seats none (a harness's), which must be a copy of that world: a save always names a player country
        /// (its header's), and seated, the copy ran the player's paths (the record's chamber, the player's elections) the world it copied never ran.</summary>
        public static void RestoreInto(SimulationManager sim, SaveGame save, bool seatsPlayer)
        {
            // §671 (SP-3): the save's created parties replace the process's, before anything reads a roster - a refused one is logged, never dropped silently
            PoliSim.Data.CreatedParties.Clear();
            foreach (PoliSim.Data.CreatedParty party in save.CreatedParties ?? new List<PoliSim.Data.CreatedParty>())
            {
                if (!PoliSim.Data.CreatedParties.TryRegister(party, PoliSim.Data.PartySystems.RealRoster(party.Country), out string refused)) { UnityEngine.Debug.LogError($"SAVE: the created party '{party.Key}' was refused on load - {refused}"); }
            }
            // PS-6 US-33 (SP-6a): the save's own nominee replaces the process's, before anything reads a poll - a refused one is logged, never dropped silently
            PoliSim.Data.OwnNominees.Clear();
            foreach (PoliSim.Data.OwnNominee nominee in save.OwnNominees ?? new List<PoliSim.Data.OwnNominee>())
            {
                if (!PoliSim.Data.OwnNominees.TryRegister(nominee, out string refused)) { UnityEngine.Debug.LogError($"SAVE: the own nominee '{nominee?.Name}' was refused on load - {refused}"); }
            }
            SimulationRandom.RestoreState(save.MasterSeed, save.RngDrawCounts);
            SimulationManager.SetEpoch(save.EpochDate);   // PS-1 (§618): the save's own start, before the clock is restored against it
            sim.RestoreSaveState(save.World, save.CurrentTurn, save.CurrentDate, save.Sim);
            // C-R4b step 3: the campaign is REPLAYED from its record, after the streams are at the save's
            // counts (the replay rewinds three of them to the campaign's start and re-steps to here).
            sim.PlayerCountryId = seatsPlayer ? save.PlayerCountryId : (CountryId?)null;
            sim.RestoreCampaign(save.PlayerCampaign, save.MasterSeed, save.RngDrawCounts);
            // PS-6 US-2: what the game does not elect is the record's on the save's date - a save cut before US-2 is put right as it loads, before any
            // screen or fork reads it; a save from a build with US-2 already holds it, and nothing moves
            sim.SeatTheRecordOnItsDate();
        }

        /// <summary>Atomic: serialize beside the target, then swap - a crash mid-save must never
        /// destroy the previous save, or this system reintroduces the exact loss it exists to
        /// prevent. The previous save survives as .bak.</summary>
        public static void SaveToFile(string path, SaveGame save)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tmp = path + ".tmp";
            File.WriteAllText(tmp, Serialize(save));
            if (File.Exists(path))
            {
                File.Replace(tmp, path, path + ".bak");
            }
            else
            {
                File.Move(tmp, path);
            }
        }

        public static SaveGame LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new SaveLoadException($"No save file at {path}.");
            }

            return Deserialize(File.ReadAllText(path));
        }

        /// <summary>Outside the repository by construction (the repository-weight finding made that a
        /// requirement): %USERPROFILE%\AppData\LocalLow\DWELOP Games\Incumbent\saves on Windows (§693: the studio name; the folder is named by
        /// Unity's companyName and productName). Every save the game or a tool reads or writes goes through here, so the one-time migration from the
        /// old identity's folder (<see cref="SaveMigration"/>, DefaultCompany\PoliSim) runs before the first access.</summary>
        public static string DefaultSaveDirectory
        {
            get
            {
                SaveMigration.EnsureOnce();
                return Path.Combine(UnityEngine.Application.persistentDataPath, "saves");
            }
        }

        public static string DefaultSlotPath => Path.Combine(DefaultSaveDirectory, "slot1.json");

        /// <summary>One save file as the saves MENU sees it - the root header fields read without a
        /// full deserialize. An unreadable or wrong-version file still LISTS (Compatible=false with
        /// the reason), because a save that silently vanishes from the menu reads as data loss.</summary>
        public class SaveHeader
        {
            public string Path;
            public string Name;
            public bool Compatible;
            public int Version = -1;
            public CountryId PlayerCountryId;
            public int Turn;
            public DateTime Date;
            public DateTime SavedAtUtc;
            public string Error;
        }

        /// <summary>
        /// Every *.json in <paramref name="directory"/> as a header row, newest save first. Parses
        /// the whole file (JObject has no partial mode) but touches only root fields - a handful of
        /// ~1 MB parses on menu open, never per frame; the menu caches the result and refreshes only
        /// after its own save/delete actions. `.tmp`/`.bak` siblings are deliberately not listed -
        /// the .bak story is the menu's footer text, not a second row per save.
        /// </summary>
        public static List<SaveHeader> ListSaves(string directory)
        {
            var headers = new List<SaveHeader>();
            if (!Directory.Exists(directory))
            {
                return headers;
            }

            foreach (string path in Directory.GetFiles(directory, "*.json"))
            {
                var header = new SaveHeader
                {
                    Path = path,
                    Name = System.IO.Path.GetFileNameWithoutExtension(path)
                };
                headers.Add(header);
                try
                {
                    JObject root = JObject.Parse(File.ReadAllText(path));
                    header.Version = root.Value<int?>("SaveVersion") ?? -1;
                    if (header.Version != CurrentSaveVersion)
                    {
                        header.Error = $"format {header.Version} - this build reads {CurrentSaveVersion}";
                        continue;
                    }

                    header.PlayerCountryId = (CountryId)System.Enum.Parse(typeof(CountryId), root.Value<string>("PlayerCountryId") ?? "", true);
                    header.Turn = root.Value<int?>("CurrentTurn") ?? 0;
                    header.Date = root.Value<DateTime?>("CurrentDate") ?? default;
                    header.SavedAtUtc = root.Value<DateTime?>("SavedAtUtc") ?? default;
                    header.Compatible = true;
                }
                catch (Exception e)
                {
                    header.Error = e is JsonException ? "not a readable save" : e.Message;
                }
            }

            headers.Sort((a, b) => b.SavedAtUtc.CompareTo(a.SavedAtUtc));
            return headers;
        }

        /// <summary>Deletes a save and its .bak/.tmp siblings together - stated in the menu's footer,
        /// so "delete" never leaves a ghost backup the player believes is gone.</summary>
        public static void DeleteSave(string path)
        {
            foreach (string target in new[] { path, path + ".bak", path + ".tmp" })
            {
                if (File.Exists(target))
                {
                    File.Delete(target);
                }
            }
        }

        /// <summary>A player-typed save name reduced to a safe filename stem - invalid filename
        /// characters and dots stripped (dots would collide with the .bak/.tmp sibling scheme),
        /// whitespace collapsed. Empty result = not saveable under this name.</summary>
        public static string SanitizeSaveName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "";
            }

            var builder = new System.Text.StringBuilder(name.Length);
            foreach (char c in name.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ')
                {
                    builder.Append(c == ' ' ? '_' : c);
                }
            }

            return builder.ToString();
        }
    }
}
