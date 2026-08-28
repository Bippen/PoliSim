using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;

namespace PoliSim.Persistence
{
    /// <summary>
    /// The whole persisted game: one root object, three captures (Master Sequence item 8, built
    /// 2026-08-16 on the mechanism report in CLAUDE.md - read that first, it names the five hazards
    /// this shape exists to survive).
    ///
    /// Layer 1 is <see cref="World"/> serialized close to as-is (Decision 1, Newtonsoft over a DTO
    /// mirror). Layer 2 is <see cref="Sim"/>, SimulationManager's explicit capture of its private
    /// pending state. Layer 3 is <see cref="Ui"/>, GameController's draft capture - null for a save
    /// written by a batch tool, which has no UI; a loader must tolerate that.
    ///
    /// <see cref="CurrencyZoneGroups"/> records which countries shared one CurrencyZone INSTANCE at
    /// save time - the restore assert's expectation. Written from the object graph itself rather
    /// than assumed from seed data, so a future zone configuration carries its own truth.
    /// </summary>
    public class SaveGame
    {
        /// <summary>First property on purpose: the loader version-checks this before attempting a
        /// full deserialize (ruling A, 2026-08-16: an incompatible save is REFUSED with a plain
        /// message, never migrated pre-release - see SaveGameService.CurrentSaveVersion for when to
        /// bump).</summary>
        public int SaveVersion;

        public DateTime SavedAtUtc;
        public CountryId PlayerCountryId;

        /// <summary>The RNG root plus every stream's position - SimulationRandom's own already-built,
        /// diagnostic-proven restore surface. Streams absent from the dictionary have taken zero
        /// draws; a save written before a new Stream member existed still loads (append-only rule).</summary>
        public int MasterSeed;
        public Dictionary<SimulationRandom.Stream, int> RngDrawCounts = new Dictionary<SimulationRandom.Stream, int>();

        public int CurrentTurn;
        public DateTime CurrentDate;

        /// <summary>Countries sharing one CurrencyZone instance at save time, one inner list per
        /// distinct instance (singletons included, so the statement is total). Hazard 1's recorded
        /// expectation: SaveGameService.AssertZoneIdentity verifies the restored graph reproduces
        /// exactly this partition as REFERENCE identity, because the failure mode - the Eurozone
        /// splitting into per-country zones - runs wrong-but-plausible forever.</summary>
        public List<List<CountryId>> CurrencyZoneGroups = new List<List<CountryId>>();

        public World World;
        public SimulationPendingState Sim;
        public UiDraftState Ui;
    }

    /// <summary>A pending cabinet decision as a NAMED pair - the in-memory shape is a ValueTuple,
    /// which would round-trip as anonymous Item1/Item2; the capture layer exists anyway, so it
    /// carries names (mechanism report, hazard 5).</summary>
    public class PendingCabinetDecisionRecord
    {
        public CabinetPortfolio Portfolio;
        public CabinetDecision Decision;
    }

    /// <summary>
    /// SimulationManager's private pending state, captured explicitly (the 2026-08-01 implementation
    /// note: an explicit reviewable surface, so a new pending-bill type nobody wired into the save
    /// reads as an obvious omission here rather than silently half-persisting). One member per
    /// private structure, same names minus the underscore - 15 in all as of 2026-08-24 (law system
    /// MVP slice added PendingLawBills).
    /// </summary>
    public class SimulationPendingState
    {
        public Dictionary<CountryId, SimulationManager.FiscalPeriod> FiscalPeriods = new Dictionary<CountryId, SimulationManager.FiscalPeriod>();
        public List<CountryId> PendingBudgetProcess = new List<CountryId>();
        public Dictionary<CountryId, BudgetBill> PendingBudgetBills = new Dictionary<CountryId, BudgetBill>();
        public Dictionary<CountryId, Dictionary<TaxType, TaxProgramBill>> PendingTaxProgramBills = new Dictionary<CountryId, Dictionary<TaxType, TaxProgramBill>>();
        public Dictionary<CountryId, Dictionary<WelfareProgramType, WelfareProgramBill>> PendingWelfareProgramBills = new Dictionary<CountryId, Dictionary<WelfareProgramType, WelfareProgramBill>>();
        public Dictionary<CountryId, LaborPolicyBill> PendingLaborBills = new Dictionary<CountryId, LaborPolicyBill>();
        public Dictionary<CountryId, CrimeJusticePolicyBill> PendingCrimeJusticeBills = new Dictionary<CountryId, CrimeJusticePolicyBill>();
        public Dictionary<CountryId, SectorPolicyBill> PendingSectorBills = new Dictionary<CountryId, SectorPolicyBill>();
        public Dictionary<CountryId, TradePolicyBill> PendingTradeBills = new Dictionary<CountryId, TradePolicyBill>();
        public Dictionary<CountryId, SwfDrawdownBill> PendingSwfDrawdownBills = new Dictionary<CountryId, SwfDrawdownBill>();
        /// <summary>Law system MVP slice: nested CountryId -&gt; LawId, mirroring PendingTaxProgramBills' own shape - see SimulationManager._pendingLawBillsByCountry's doc comment.</summary>
        public Dictionary<CountryId, Dictionary<string, LawBill>> PendingLawBills = new Dictionary<CountryId, Dictionary<string, LawBill>>();
        public Dictionary<CountryId, ForeignPolicyMeeting> PendingForeignPolicyMeetings = new Dictionary<CountryId, ForeignPolicyMeeting>();
        public Dictionary<CountryId, List<PendingCabinetDecisionRecord>> PendingCabinetDecisions = new Dictionary<CountryId, List<PendingCabinetDecisionRecord>>();
        public Dictionary<CountryId, FiscalTurnReport> LastFiscalReports = new Dictionary<CountryId, FiscalTurnReport>();
        public Dictionary<CountryId, EconomicEvent> LastEvents = new Dictionary<CountryId, EconomicEvent>();
    }

    /// <summary>
    /// GameController's draft layer (layer 3) - the unintroduced slider values whose loss WAS the
    /// original 5c incident, plus the interrupt state that happens to live controller-side (the Fed
    /// Chair candidate set, the pending election result, game over) and the signing ceremony's
    /// division high-water mark. Deliberately NOT here: scroll positions and selected tab/category
    /// (navigation), the preview cache (recomputes), and the live GameSpeed - the speed VALUE is
    /// recorded, but a load always resumes PAUSED (see GameController.RestoreUiDrafts for why).
    /// </summary>
    public class UiDraftState
    {
        public Dictionary<TaxType, float> TaxRateInputs = new Dictionary<TaxType, float>();
        public Dictionary<WelfareProgramType, float> WelfareGenerosityInputs = new Dictionary<WelfareProgramType, float>();
        public Dictionary<SectorType, float> SectorSubsidyInputs = new Dictionary<SectorType, float>();
        public Dictionary<SectorType, float> SectorRegulationInputs = new Dictionary<SectorType, float>();
        public Dictionary<SectorType, float> SectorTaxCreditInputs = new Dictionary<SectorType, float>();
        public Dictionary<SectorType, float> SectorResearchGrantsInputs = new Dictionary<SectorType, float>();
        public Dictionary<SectorType, float> SectorDeregulationInputs = new Dictionary<SectorType, float>();
        public Dictionary<SpendingCategory, float> SpendingLineInputs = new Dictionary<SpendingCategory, float>();
        public Dictionary<CountryId, float> PartnerTariffInputs = new Dictionary<CountryId, float>();

        public bool? SwfExistsDraft;
        public float SwfDrawdownPercentInput;
        public float? SwfContributionRateInput;
        public float? SwfDomesticAllocationInput;
        public float? SwfEquitiesWeightInput;
        public float? SwfBondsWeightInput;
        public float? SwfInfrastructureWeightInput;
        public float? SwfRealEstateWeightInput;

        public float InterestRateChangeInput;
        public float? TariffRateInput;
        public float? MinimumWageInput;
        public float? PaidFamilyLeaveWeeksInput;
        public float? OvertimeRegulationInput;
        public float? RetrainingProgramInput;
        public float? PoliceFundingInput;
        public float? SentencingSeverityInput;
        public float? BailReformInput;
        public float? DrugPolicyInput;
        public float? JudicialFundingInput;
        public float? BorderEnforcementInput;
        public float? FamilyPolicyInput;
        public float? ImmigrationPolicyInput;

        public bool IsGameOver;
        public string GameOverReason;
        public ElectionResult PendingElectionResult;
        public int PendingElectionTurn;

        /// <summary>
        /// STEP 3 (R-S3e's "one id plus counters"): the active scenario's id and objective progress,
        /// or null in free play - which is also what an old save deserializes to, so a pre-scenario
        /// save loads as the free-play game it was (the append-only rule this shape already relies on
        /// for RNG streams). The DEFINITION is never persisted: it is looked up by id from
        /// `ScenarioLibrary` at load, the same "record values, not formulas" principle
        /// `FiscalTurnReport` states - so authored content can be corrected without invalidating a
        /// save, while a REMOVED scenario id is refused rather than silently dropped.
        /// </summary>
        public ScenarioProgress Scenario;

        /// <summary>Set while the verdict screen is the live takeover, so a save taken on it reopens
        /// on it rather than dropping the player back into a finished world with no explanation.</summary>
        public bool ScenarioVerdictPending;

        /// <summary>GameController.GameSpeed as an int - the enum is private to the controller and
        /// stays that way; the value is recorded for completeness while a load resumes Paused.</summary>
        public int GameSpeedValue;

        public List<FedChair> FedChairCandidates;
        public int FedChairCandidatesForTurn = -1;

        /// <summary>The signing ceremony's high-water mark by DivisionRecord.Number - omitted, a load
        /// replays every ceremony still inside the 24-entry log.</summary>
        public int SeenDivisionNumber;

        public float PrevGdp;
        public float LastGrowthPercent;

        /// <summary>
        /// UI v3.0 (2026-08-28, V3-R2): the player's per-screen fold overrides, keyed by
        /// GameController.ShellScreenKey and valued as (int)ShellFoldState. Null on every save written
        /// before v3.0 and on batch-written saves, which restores to "no override" - every screen at
        /// its default. Navigation itself (the selected tab) stays unpersisted per the rule above; the
        /// fold is a standing choice ABOUT a screen, not a place in it, which is why it rides here.
        /// </summary>
        public Dictionary<string, int> ShellFoldOverrides;
    }
}
