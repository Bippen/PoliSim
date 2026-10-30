using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// A playable country: identity, current economic/political state, the currency zone it
    /// belongs to, and its bilateral trade relationships.
    /// </summary>
    [Serializable]
    public class Country
    {
        public CountryId Id;
        public string Name;
        public EconomyState State;

        /// <summary>
        /// Master Sequence step 9, Step A: the player-facing PUBLISHED view - lagged, and sometimes a
        /// preliminary figure later revised. Deliberately a sibling of State rather than part of it: every
        /// simulation system reads `country.State.X`, so keeping published values out of EconomyState
        /// makes a leak into Okun's Law, the Phillips Curve or the Fiscal Reaction Function a
        /// compile-time impossibility rather than something reviewers must keep noticing across 55 call
        /// sites. See COMPLETED.md section 12 (was STEP_A_LIVE_VALUE_AUDIT.md, consolidated 2026-08-02).
        /// </summary>
        public PublishedData Published = new PublishedData();

        /// <summary>
        /// The standing sovereign rating, set by scheduled review rather than recomputed every turn - see
        /// `CreditRatingSystem.ReviewIfDue` and Elias's A1 ruling (2026-08-02). A derived OUTPUT, sitting
        /// beside Published for the same reason Published sits beside State: it folds into no ceiling and
        /// draws no randomness, and its one reader in the simulation is the debt crisis's trigger at CCC
        /// (MF q33, K141).
        /// </summary>
        public SovereignRatingState Rating = new SovereignRatingState();

        public CurrencyZone CurrencyZone;
        public List<TradePartner> TradePartners = new List<TradePartner>();

        /// <summary>This country's fiscal portfolio - which taxes are implemented and at what rate. See TaxLine and SimulationManager.GetTotalTaxRevenue.</summary>
        public List<TaxLine> TaxLines = new List<TaxLine>();

        /// <summary>
        /// This country's welfare portfolio - which anti-poverty programs are implemented and at what
        /// GenerosityLevel, mirroring TaxLines' implement/adjust/remove pattern exactly. Seeded per
        /// country by WorldFactory.SeedWelfarePrograms (the seed-spread slots of 2026-08-27; every
        /// slot still empty pending sourced figures) - see WelfareProgram and
        /// SimulationManager.GetTotalWelfareCost/ApplyWelfareGenerosityChanges.
        /// </summary>
        public List<WelfareProgram> WelfarePrograms = new List<WelfareProgram>();

        /// <summary>
        /// Playtest 3's seed-spread ruling (2026-08-27): the welfare portfolio AS SEEDED - a snapshot
        /// WorldFactory.SeedWelfarePrograms takes and nothing mutates - the anchor every welfare effect
        /// measures from (MacroSystem.WelfareEffectDelta, SimulationManager.GetTotalWelfareCost). The
        /// sourced baselines already contain each country's real programs, so a program implemented
        /// at seed contributes nothing on the no-policy path and a player's change is booked from the
        /// country's own real position. Empty (the pre-ruling state: delta == live) for a country
        /// seeded before the ruling and for any save that lacks the field.
        /// </summary>
        public List<WelfareProgram> BaselineWelfarePrograms = new List<WelfareProgram>();

        /// <summary>
        /// This country's economic sector breakdown (Manufacturing/Technology/Agriculture/Finance) -
        /// a small proof-of-pattern slice, present for all six countries (unlike TaxLines/
        /// WelfarePrograms, there's no implement/remove - every country has all four sectors always).
        /// See Sector.cs and MacroSystem.ApplySectorEffects.
        /// </summary>
        public List<Sector> Sectors = new List<Sector>();

        /// <summary>
        /// Law system MVP slice: laws this country currently has in force, each a named preset over
        /// the existing dial space rather than a bespoke effect - see LawDefinition/LawCatalog for
        /// what a law IS and ParliamentSystem.GetLawBillDirection/ApplyLawBillResult for how one
        /// reaches this list. A List&lt;EnactedLaw&gt;, not a Dictionary&lt;LawId,bool&gt; - matching this
        /// codebase's dominant "collection of things a country has" shape (TaxLines/Sectors), not
        /// the Dictionary shape reserved for closed structural enums (CabinetMinisters/
        /// ParliamentSeats). A bare bool would discard the provenance (which law, when) this project
        /// has otherwise consistently kept - see DivisionLog, built for exactly this reason. Empty
        /// for every country by default - no law starts enacted. Deliberately NOT in
        /// SimulationManager.ClonePreviewCountry's hand-list, matching Divisions' own omission (CabinetMinisters
        /// rides it since AF-30: the preview reads the chairs) - nothing in the PreviewTurn pipeline ever reads or mutates it, since bill
        /// resolution (the only writer) runs from the day-tick loop, never from a preview.
        /// </summary>
        public List<EnactedLaw> EnactedLaws = new List<EnactedLaw>();

        /// <summary>
        /// This country's four tracked infrastructure types (Roads/Rail/PowerGrid/Broadband) -
        /// present for all six countries always (the same "no implement/remove" idiom Sectors
        /// already established). See InfrastructureAsset.cs and MacroSystem.ApplyInfrastructureCondition.
        /// </summary>
        public List<InfrastructureAsset> InfrastructureAssets = new List<InfrastructureAsset>();

        /// <summary>
        /// This country's detailed spending portfolio (Phase 1: USA only - see CLAUDE.md's "Detailed
        /// Spending Portfolio"). Empty for a country means it still uses the legacy
        /// GovernmentSpendingRate + PolicyDecision's four category-delta fields mechanism unchanged -
        /// see SimulationManager.ApplyDomesticPolicy's hasDetailedSpending branch.
        /// </summary>
        public List<SpendingLine> SpendingLines = new List<SpendingLine>();

        /// <summary>
        /// This country's own tariff policy toward imports, used only when it is not a member of
        /// any trade bloc (bloc members instead apply their bloc's common external/internal rates).
        /// </summary>
        public float BaseTariffRate;

        /// <summary>
        /// NAIRU - the non-accelerating-inflation rate of unemployment, a structural per-country
        /// constant. The Phillips Curve compares actual unemployment against this. See
        /// MacroSystem.ApplyPhillipsCurveInflation.
        /// </summary>
        public float NaturalUnemploymentRate;

        /// <summary>P4-C3 (2026-09-04): the seeded natural rate, the base the enacted LabourInstitutions laws compose on -
        /// NaturalUnemploymentRate is recomputed from this plus the enacted set (SimulationManager.RecomputeStructuralParametersFromEnactedLaws),
        /// never mutated incrementally, the clamp-safe idiom the crime dials taught (COMPOSITION).</summary>

        public float NaturalUnemploymentRateBase;

        /// <summary>P4-C3: the seeded bases of the other structural parameters a law may move (StructuralParameters) - captured once the
        /// seeds are placed (CaptureStructuralBases, at the end of WorldFactory.CreateDefault), composed on by the enacted set.</summary>
        public float ComfortableDebtToGdpPercentBase;
        public float AverageDebtMaturityYearsBase;
        public float RiskPremiumSensitivityBase;

        /// <summary>P5-B3 (2026-09-05): the seed's GDP and the seed level of each tax-base driver (TaxBases.Level, indexed by TaxBaseDriver), captured by CaptureStructuralBases; a base is its sourced share of the seed's GDP times its driver's ratio to these. 0 = not yet referenced (consumption is computed by the first day; TaxBases.Base takes it then).</summary>
        public float RevenueBaseSeedGdp;
        public float[] RevenueBaseSeeds = new float[TaxBases.DriverCount];
        /// <summary>FT-5 (2026-09-07, §372): the income-tax line's rate at the seed - the labour tax the participation term reads its change against; 0 = no such line.</summary>
        public float LaborTaxRateSeed;

        /// <summary>F4-2 (2026-09-13): the average effective rate the income-tax STATUTE yields at the seed - the denominator of
        /// <see cref="TaxSchedule.YieldRatio"/>, captured with the structural bases so the ratio is one at the seed by construction;
        /// 0 where the shape does not respond or the cohorts carry no income, and in a save from before (then taken on first read).</summary>
        public float IncomeTaxSeedAer;

        /// <summary>F4-2: the yield ratio's memo for the current boundary (not saved; rebuilt on first read) - see <see cref="TaxSchedule.Memo"/>.</summary>
        [System.NonSerialized] public TaxSchedule.Memo ScheduleMemo;

        /// <summary>GA-2-11 (2026-10-14): the same memo for the statute as seeded - none of the line's own sub-row rates - which the household burden's
        /// seed side reads (`TaxSchedule.YieldNowAtStatute`); kept apart so the two readings never reset each other. Not saved.</summary>
        [System.NonSerialized] public TaxSchedule.Memo ScheduleMemoStatute;

        /// <summary>P5-B7 (2026-09-05): potential output's factors - the seed's potential, the seed's labour input (PotentialOutput.LabourInput) and the productivity index that compounds daily at the trend (1 at the seed); the labour input as it stood at the last turn, for the derived growth rate. Captured by CaptureStructuralBases; 0 = a save from before this pass, which keeps the old compounding.</summary>
        public float PotentialGdpSeed;
        public float PotentialLabourSeed;
        public float PotentialProductivityIndex;
        public float PotentialLabourAtLastTurn;
        /// <summary>K209 (resp929, sim929): the TREND labour input as it stood at the last turn - the labour force at the core's unemployment, the
        /// natural rate plus the supply excess (MacroSystem.TrendLabourInput) - and the TREND growth of potential it gives with the productivity trend,
        /// percent: the growth the gap form expects (MacroSystem.ExpectedPotentialGrowth), the core's cycle left out. Captured with the structural
        /// bases; the trend growth opens at the seed's potential growth.</summary>
        public float TrendLabourAtLastTurn;
        public float TrendPotentialGrowthRate;

        /// <summary>T-3, form A (Elias, 2026-09-15, `COMPLETED.md` §507): the identity's government consumption over the book's discretionary lines - k = the sourced
        /// share of GDP (general government final consumption expenditure, P3_S13, 2023: GovernmentConsumptionData, generated from MacroData/) × the seed's
        /// nominal GDP ÷ the seed's discretionary lines, captured once by CaptureStructuralBases. The identity reads k × the lines' plan ÷ the price level
        /// (MacroSystem.IdentityGovernmentConsumption), so G is the sourced share at the seed and grows with the lines after it; the book spends the lines
        /// themselves, unscaled. The USA's k is the widest because its lines are the federal budget and its share general government (the row's statement).
        /// 1 where a country has no discretionary lines or no share (the identity reads the book as it did); a save from before carries none - the gate refuses it.</summary>
        public float GovernmentConsumptionScale;

        /// <summary>MF q29 (docs/RULINGS_2026-10-18.md q29): the national-accounts identity's two seed references, so the output gap closes at the
        /// sourced persistence (MacroSystem.ApplyNationalAccounts): the consumption-and-investment propensity at the seed's rate and confidence, and
        /// the seed's government consumption and net exports as a share of the seed's potential. Demand is read against them - a rate, a confidence,
        /// a G or an NX away from the seed's is the impulse; at the seed's the gap decays at the persistence alone. Captured by
        /// CaptureStructuralBases; 0 = a save from before this item, which keeps the old reversion.</summary>
        public float IdentityPropensitySeed;
        public float IdentityAutonomousShareSeed;
        /// <summary>K209: the demand reference, a share of potential - the level of the identity's demand impulse that has become the economy's
        /// own, moved toward each period's impulse at MacroSystem.DemandReferenceSpeed (MacroSystem.CloseDemandReference); the gap form reads
        /// the impulse against it, so a change in demand fades and a drift is absorbed. 0 at the seed.</summary>
        public float DemandReference;

        /// <summary>P5-B6 (2026-09-05): the price level as it stood when the spending lines were last indexed, so the lines carry the year's prices as the ratio now/then (IndexSpendingLines). 1 at the seed; 0 = a save from before this pass (the first index takes the level and applies 1).</summary>
        public float PriceLevelAtLastIndex;
        /// <summary>RF-2 re-formed (2026-09-07, §384): the real wage index as it stood when the spending lines were last indexed, so an AI country's caseload
        /// lines carry the year's real wage growth as the ratio now/then (IndexSpendingLines) - the income index of Socialförsäkringsbalken 58 kap. in the
        /// model's terms. 0 = a save from before: a factor of 1 once.</summary>
        public float RealWageIndexAtLastIndex;
        /// <summary>K271 (2026-10-29): THE AI GOVERNMENT'S RE-INDEXATION of the income-tax thresholds - a multiplier on the statute's own threshold scale
        /// (`TaxSchedule.ThresholdScale`), 1 at the seed. Each boundary an AI book governs (not the USA's), it carries the year's real wage growth, and for a
        /// statute that leaves its thresholds nominal (Poland's) the year's prices too, so the thresholds follow nominal incomes - the no-policy convention of
        /// the EC's DSM and Ageing Report (revenue roughly constant as a share of GDP), [AUTHORED-DRAFT] as an AI government's practice. A book the player
        /// governs leaves it where it stands - the statute as written, its bracket creep included - and so does the USA's (CPI indexing; CBO's current law
        /// documents the creep). 0 = a save from before K271: read as 1, the statute's reading of the build before. The preview's clone carries it (§506).</summary>
        public float IncomeTaxThresholdLift;
        /// <summary>K297 (fisc2, 2026-10-29; the review's M3 and R2-M1): the real growth the energy line's path took over the boundaries the PLAYER indexed - each
        /// boundary the player governs, `SimulationManager.IndexSpendingLines` multiplies it by the driverless real-growth factor it puts on that line; while an AI
        /// government indexes it stands FROZEN (so the levy never jumps at a change of government). `EnergyLedger`'s levy rule carries it in the support's share of the
        /// path, so in either book a billion of support moves 1 ⁄ G_AI of a billion of levy, G_AI the growth over the AI's boundaries: one for one where the player has
        /// indexed every boundary since the seed, less after an AI spell (`EnergyLedger.LevyPassThrough`, which the page prints). 0 = none, read as 1 (a world never
        /// played, and a save from before fisc2). The preview's clone carries it (§506).</summary>
        public float EnergyLevyRealGrowth;
        /// <summary>K271 (2026-10-29): what the AI finance ministry's release restored last boundary, as a share of the lines then
        /// (`AiFinanceMinistry.Written.ReleasedShareOfLines`) - left out of next year's net-expenditure excess (`AiFinanceMinistry.EuExpenditureExcess`), so a
        /// release is not clawed back as overspending. 0 where nothing was released, where the player governs, and in a save from before K271.</summary>
        public float EuReleasedShareOfLines;
        /// <summary>PN-1's driver (2026-09-16, §520): the calendar year the model is in - committed by SimulationManager from its clock (at SetWorld, at a
        /// restore and every day it advances), so a reader that has no clock reads the year the turn is in: the pension line's driver
        /// (SpendingDrivers.Level, StatutoryPensionAge) counts the cohorts at or above the age the statute has IN FORCE this year
        /// (PensionAgeStatute.AgeInForce). The seed year until the clock runs. The preview's clone carries it - the R4-1 hand-list, audited by field (§506).</summary>
        public int CalendarYear = PensionAgeStatute.SeedYear;
        /// <summary>K80 (2026-10-23): the DAY the model is on - committed with CalendarYear by SimulationManager from its clock (SetWorld, a restore, every day),
        /// so a reader with no clock reads the sides each party had declared that day (`NationalElection.BlocOf`). Not saved: every restore commits it again.
        /// MinValue until a clock runs - the reader then takes the world's opening date. The preview's clone carries it.</summary>
        [Newtonsoft.Json.JsonIgnore] public System.DateTime CalendarDay;
        /// <summary>PN-1's DIAL (2026-09-23, §590; DS-3: "a passed bill's figure wins"): the pension age a PASSED budget bill set, in years - negative while the
        /// statute's own figure stands, which is every country at the seed and every AI government (nothing in the AI's rule sets it). Read through
        /// <see cref="PensionAgeStatute.AgeInForce(Country, int)"/>, so the pension line's driver, the payment readout and the row all read one figure. It
        /// persists until a bill sets another or returns the statute's. Additive to the save: a save from before reads the statute's, as it always did.
        /// The preview's clone carries it (the R4-1 hand-list, audited by field, §506).</summary>
        public float PensionAgeOverride = -1f;

        /// <summary>§596 review D1 (2026-09-23): the pension age's participation response, in points of the 15+ rate, that the STATE's rate already carries. When the
        /// response moves (a statute step, a bill's age) the difference is added to the state's rate as a level shift - the people the age keeps in work are in the
        /// labour force the day it moves, and the FT-8 split sees the same step in the actual rate as in the anchor, so it lands on no one's unemployment. Zero at
        /// the seed. Additive to the save: a save from before reads 0 and takes the whole standing response as one level shift on its first day. The preview's clone
        /// carries it (the R4-1 hand-list).</summary>
        public float PensionParticipationApplied;
        /// <summary>The AI finance ministry (§388): the debt ratio at the seed, and at the top of the last two turns - the US rule's trigger reads the ratio above
        /// its seed and rising two years running (the debt-limit logic). 0 = not yet observed; a save from before the ministry seeds itself on first sight.</summary>
        public float DebtRatioSeed;
        public float DebtRatioLastReport;
        public float DebtRatioReportBefore;
        /// <summary>§716: the day the Finance partner last wrote its step (since §755 on the fiscal stance; the boundary's decision or the government's
        /// own budget bill) - it steps at most once in any year (FinancePartner.StepDue). MinValue = never; a save from before loads it so.</summary>
        public System.DateTime FinancePartnerSteppedOn;
        /// <summary>§755 (Elias's ruling A2): the fiscal stance the Finance partner has moved so far, points of GDP (+ = expansion) - the count its next
        /// step is judged against (FinancePartner.Applied), kept for <see cref="FinanceStanceHolder"/> alone: a new holder starts from zero. 0 in a save from before.</summary>
        public float FinanceStanceApplied;
        /// <summary>§755: the party whose stance <see cref="FinanceStanceApplied"/> counts. Null = none yet; a save from before loads it so.</summary>
        public string FinanceStanceHolder;
        /// <summary>§755 (the review's defect 2): the government (its FormedOn) the count was made under - the same holder in a new government starts from zero.
        /// MinValue in a save from before (its count then reads zero once).</summary>
        public System.DateTime FinanceStanceGovernment;
        /// <summary>§755: the stance the PLAYER's party asks while it holds Finance as a partner, points of GDP (+ = expansion) - the Budget page's dial,
        /// within FinancePartner.PlayerTargetLimit. 0 = no change asked; a save from before loads it so.</summary>
        public float FinanceStancePlayerTarget;
        /// <summary>FT-7 (§391): the participation rate as it stood at the last boundary, so the jobs lag reads the year's change in labour supply. 0 = not yet seen.</summary>
        public float ParticipationAtLastBoundary;
        /// <summary>FT-8 (§398): the participation the pyramid implied at the sourced rates by age (ParticipationRateTable.StructuralRate) as it stood at the last
        /// boundary - the year's change in it is the demographic trend the supply shock leaves out of the excess. 0 = not yet seen.</summary>
        public float StructuralParticipationAtLastBoundary;
        /// <summary>FT-8 (§398): the composition rate (UnemploymentRateByAgeTable.CompositionRate) on the seed's pyramid - the base the natural rate's demographic
        /// shift is measured from. 0 = not yet seen; a save from before seeds it on first sight and reads no shift from that day.</summary>
        public float CompositionNaturalRateAtSeed;
        /// <summary>FT-8 (§398): the natural rate the macro core reads - the seeded figure as the labour-institution laws compose it, plus the labour force's
        /// demographic shift (EconomyState.NaturalRateDemographicShift). EVERY gap reader reads THIS - a standing rule since §399 (ONE NATURAL RATE: a natural rate two readers disagree on is not one) - Okun's reversion, the Phillips curve, the Taylor rule, and (§398's measurement forced
        /// the rest) the discouraged-worker, poverty, crime, approval, real-wage, hoarding and rating terms; NaturalUnemploymentRate stays the figure the laws compose and the shift is measured from.
        /// MF-14 (K199, 2026-10-28): never below the country's unemployment floor (<see cref="UnemploymentFloorPercent"/>) - a reform or an older labour force that
        /// composes the natural rate below the floor would otherwise leave unemployment pinned at the floor with a standing positive slack: inflation below target,
        /// the Taylor gap negative and the reform's payoff zero, for ever. Every reader sees max(composed rate, floor), the UI's NAIRU included.</summary>
        public float EffectiveNaturalUnemploymentRate => Math.Max(NaturalUnemploymentRate + (State != null ? State.NaturalRateDemographicShift : 0f), UnemploymentFloorPercent);

        /// <summary>MF-14 (q14; Elias's ruling K138, 2026-10-27): unemployment's floor - the lowest annual rate the OECD's harmonised series records for the
        /// country from 1990 on (from the series' own start where it opens later), read from the generated table (`UnemploymentFloorData.Floors`, written by
        /// `Tools/unemployment_floor_prep.pl` from the stored CSV). A country the table does not hold has a floor of 0.</summary>
        public float UnemploymentFloorPercent => Generated.UnemploymentFloorData.Floors.TryGetValue(Id, out var row) ? row.Floor : 0f;
        public float CollectionEfficiencyBase;
        public float GovernmentSpendingRateBase;
        /// <summary>P4-C3 (2026-09-05, the labour institutions' second reach): the seeded benefit rate per point of unemployment; the labour laws that cut benefit levels or duration compose on it.</summary>
        public float BenefitRatePerUnemployedBase;
        /// <summary>EN-7b (2026-09-15): the statutory electricity tax per retail class, EUR per MWh - as the electricity-tax laws compose it (StructuralParameter.ElectricityTax*),
        /// and its base, the 2023 statute (EnergyLayer.ElectricityTaxBaseEurPerMwh; 0 where the country levies none - the USA). The ledger moves the stack's environmental-tax
        /// component by the difference (EnergyLedger.ElectricityTaxDeltaEurPerMwh); value and base are seeded from one float, so the difference is exactly 0 at the seed.</summary>
        public float ElectricityTaxHouseholds, ElectricityTaxNonHouseholds;
        public float ElectricityTaxHouseholdsBase, ElectricityTaxNonHouseholdsBase;

        /// <summary>P5-C2 (2026-09-05): the health family's seeds and bases (HealthFamily.Seed) - the state it moves is on EconomyState.</summary>
        public HealthSeeds Health = new HealthSeeds();

        /// <summary>P5-C3 (2026-09-06): the education family's seeds and bases (EducationFamily.Seed).</summary>
        public EducationSeeds Education = new EducationSeeds();

        /// <summary>P5-C4 (2026-09-06): the infrastructure family's seeds and bases (InfrastructureFamily.Seed).</summary>
        public InfrastructureSeeds Infrastructure = new InfrastructureSeeds();

        /// <summary>P5-C5 (2026-09-06): the environment family's seeds and bases (EnvironmentFamily.Seed).</summary>
        public EnvironmentSeeds Environment = new EnvironmentSeeds();

        /// <summary>P5-C6 (2026-09-06): the immigration-and-poverty-depth family's seeds and bases (MigrationPovertyFamily.Seed).</summary>
        public MigrationPovertySeeds MigrationPoverty = new MigrationPovertySeeds();

        /// <summary>P5-C7 (2026-09-05, board 9d): this turn's request, allocation and efficiency per portfolio (Effectiveness.Record, from
        /// SimulationManager.ResolveSpendingForTurn); the card's readout, the Budget row's caption and the families read it.</summary>
        public System.Collections.Generic.Dictionary<CabinetPortfolio, PortfolioEffectiveness> Effectiveness = new System.Collections.Generic.Dictionary<CabinetPortfolio, PortfolioEffectiveness>();

        /// <summary>P4-C3: record every structural parameter's seeded value as its base. Called once, after seeding; a save carries the bases. P5-B2: also the seed level of every spending line's driver (SpendingLine.DriverReference), so the first year's index counts the seed-to-year-one change.</summary>
        public void CaptureStructuralBases()
        {
            foreach (SpendingLine line in SpendingLines) { line.DriverReference = SpendingDrivers.Level(SpendingDrivers.Of(line.Category), this); }
            RevenueBaseSeedGdp = State.GDP;   // P5-B3
            // FT-7, the second seat (2026-09-08, §394): the seed potential is RE-SOLVED from the seed's own factors - the output the seed's employment would
            // give at the natural rate, GDP × (1 − NAIRU) ÷ (1 − U at the seed) - and supersedes the typed figure (WorldFactory's potentialGdp: the USA's
            // 33 260 against a GDP of 29 000 was a 14.7 % gap no seed could close, §127). Potential is its factors from here on (P5-B7), from the seed too.
            PotentialGdpSeed = State.GDP * Math.Min(100f, Math.Max(1f, 100f - NaturalUnemploymentRate)) / Math.Min(100f, Math.Max(1f, 100f - State.Unemployment));
            State.PotentialGDP = PotentialGdpSeed;
            // T-3, form A (2026-09-15, §507): the identity's G at the seed is the sourced share of the seed's nominal GDP; k carries it onto the lines, which grow it
            float seedDiscretionaryLines = 0f;
            foreach (SpendingLine line in SpendingLines) { if (!line.IsMandatory) { seedDiscretionaryLines += line.Amount; } }
            GovernmentConsumptionScale = seedDiscretionaryLines > 0f && Generated.GovernmentConsumptionData.SharePct.TryGetValue(Id, out float consumptionSharePct)
                ? (float)(consumptionSharePct / 100.0 * State.NominalGdp / seedDiscretionaryLines)
                : 1f;
            // MF q29: the identity's seed references - the propensity at the seed's rate and confidence, and G + NX over the seed's potential
            IdentityPropensitySeed = Simulation.MacroSystem.IdentityPropensity(this, CurrencyZone.InterestRate, Simulation.MacroSystem.EffectiveConsumerConfidence(this));
            IdentityAutonomousShareSeed = PotentialGdpSeed > 0f
                ? (Simulation.MacroSystem.IdentityGovernmentConsumption(this, seedDiscretionaryLines) + State.TradeBalance) / PotentialGdpSeed
                : 0f;
            PotentialLabourSeed = PotentialOutput.LabourInput(this);
            PotentialProductivityIndex = 1f;
            PotentialLabourAtLastTurn = PotentialLabourSeed;
            TrendLabourAtLastTurn = Simulation.MacroSystem.TrendLabourInput(this);   // K209 (resp929, sim929)
            TrendPotentialGrowthRate = PotentialGrowthRate;
            PriceLevelAtLastIndex = State.PriceLevel;   // P5-B6
            RealWageIndexAtLastIndex = State.RealWageIndex;   // RF-2 re-formed (§384)
            DebtRatioSeed = State.DebtToGdpRatio;   // the AI finance ministry (§388)
            ParticipationAtLastBoundary = State.LaborForceParticipationRate;   // FT-7 (§391)
            // FT-8 (§398): the two references the natural rate reads the labour force through - the structural participation and the composition rate on the seed's pyramid
            float structuralAtSeed = ParticipationRateTable.StructuralRate(this, Simulation.SimulationManager.EpochDate.Year);   // §596; K276: the epoch's year, as the opening rate
            StructuralParticipationAtLastBoundary = float.IsNaN(structuralAtSeed) ? 0f : structuralAtSeed;
            float compositionAtSeed = Cohorts != null ? UnemploymentRateByAgeTable.CompositionRate(Id, Cohorts.Counts, Simulation.SimulationManager.EpochDate.Year) : float.NaN;   // K276 review 1 (L1): weighted by the epoch year's projected participation, as the boundary reads it
            CompositionNaturalRateAtSeed = float.IsNaN(compositionAtSeed) ? 0f : compositionAtSeed;
            State.NaturalRateDemographicShift = 0f;
            RevenueBaseSeeds = new float[TaxBases.DriverCount];
            LaborTaxRateSeed = 0f;   // FT-5
            foreach (TaxLine line in TaxLines) { if (line.Type == TaxType.IncomeTax && line.IsImplemented) { LaborTaxRateSeed = line.Rate; } }
            // F4-2: the seeded rate on the income line is the shift's origin, and the statute's yield over the seed's cohorts is the ratio's denominator
            foreach (TaxLine line in TaxLines) { if (line.Type == TaxType.IncomeTax || line.Type == TaxType.VAT) { line.RateSeed = line.Rate; } }   // §716 (the review's defect 2): VAT's too - then the Finance partner's anchor (§755 retired that read: the partner moves the stance); no revenue site reads VAT's seed
            IncomeTaxSeedAer = (float)TaxSchedule.AverageEffectiveRate(this, 0.0, 1.0, 1.0);
            for (int d = 0; d < TaxBases.DriverCount; d++) { RevenueBaseSeeds[d] = TaxBases.Level((TaxBaseDriver)d, this); }
            NaturalUnemploymentRateBase = NaturalUnemploymentRate;
            ComfortableDebtToGdpPercentBase = ComfortableDebtToGdpPercent;
            AverageDebtMaturityYearsBase = AverageDebtMaturityYears;
            RiskPremiumSensitivityBase = RiskPremiumSensitivity;
            CollectionEfficiencyBase = CollectionEfficiency;
            GovernmentSpendingRateBase = GovernmentSpendingRate;
            BenefitRatePerUnemployedBase = BenefitRatePerUnemployed;   // P4-C3, the labour institutions' second reach
            // EN-7b (2026-09-15): the electricity tax's statute from the generated catalog - a static table, so no seeding order is at stake - value and base from
            // ONE float each, bit-identical (the ledger's branch compares them); 0 where the country levies none (the USA)
            ElectricityTaxHouseholds = ElectricityTaxHouseholdsBase = (float)EnergyLayer.ElectricityTaxBaseEurPerMwh(Id, 0);
            ElectricityTaxNonHouseholds = ElectricityTaxNonHouseholdsBase = (float)EnergyLayer.ElectricityTaxBaseEurPerMwh(Id, 1);
        }

        /// <summary>
        /// Trend/potential GDP growth rate, in percent per turn, used by Okun's Law (actual vs.
        /// potential growth) and to grow PotentialGDP each turn. Recomputed every turn as
        /// BasePotentialGrowthRate plus the combined, separately-ceilinged Infrastructure/Sector
        /// growth adjustments (see MacroSystem.ApplyInfrastructureGrowthEffect /
        /// ApplySectorGrowthEffect) - not itself a structural constant any more, though it behaves
        /// like one absent any active adjustment.
        /// </summary>
        public float PotentialGrowthRate;

        /// <summary>
        /// The country's ORIGINAL, un-adjusted trend GDP growth rate, exactly as seeded in
        /// WorldFactory - never mutated by any policy/condition/performance effect. The productivity trend
        /// is recomputed each turn as the ANCHOR plus the combined infrastructure/sector/education
        /// adjustments, and the anchor is this base through the WEO's horizon and the trend pulled toward the
        /// Ageing Report's / CBO's path after it (K210, ProductivityPull.Anchor, which reads this base as the
        /// seed's distance from that path) - keeping the structural anchor separate from those adjustments is what
        /// lets each adjustment's own combined ceiling be enforced against a fixed reference point
        /// rather than a value the adjustments themselves could otherwise drift.
        /// </summary>
        public float BasePotentialGrowthRate;

        /// <summary>
        /// Accumulated PotentialGrowthRate boost earned from sustained Infrastructure-category
        /// spending increases (see MacroSystem.ApplyInfrastructureGrowthEffect) - a lasting,
        /// ratcheting investment effect, only ever non-negative, clamped to its own
        /// [0, MaxInfrastructureSpendingBoost] range. Combined with the live, non-accumulating
        /// Infrastructure-condition drag under one shared ceiling before being added to
        /// BasePotentialGrowthRate - see "Infrastructure Feedback" in CLAUDE.md.
        /// </summary>
        public float InfrastructureSpendingGrowthAdjustment;

        /// <summary>
        /// Baseline government consumption expenditure, as a percentage of GDP - the structural
        /// share of the G term in GDP = C + I + G + NX. PolicyDecision.TotalDiscretionarySpending
        /// (the sum of the four spending categories) is added on top of this as a discretionary
        /// delta, not used in place of it.
        /// </summary>
        public float GovernmentSpendingRate;

        /// <summary>
        /// Automatic stabilizer: unemployment benefit cost as a percentage of GDP per percentage
        /// point of unemployment (e.g. 0.20 means 1 point of unemployment costs 0.20% of GDP in
        /// benefits) - a structural per-country constant reflecting how generous the welfare state
        /// is. See SimulationManager.GetUnemploymentBenefitCost.
        /// </summary>
        public float BenefitRatePerUnemployed;

        /// <summary>
        /// THE COVERAGE BRIDGE between the four modelled instruments and a whole tax system - re-documented
        /// by D-16 (a), 2026-09-04 (COMPLETED.md §282). Applied as a multiplier in
        /// SimulationManager.ApplyRevenueAndSpending (ActualRevenue = GetTotalTaxRevenue() * CollectionEfficiency),
        /// not inside GetTotalTaxRevenue itself, so that method still returns the instruments' own figure. Solved
        /// per country in WorldFactory as Target / Implied so the default portfolio's actual revenue-to-GDP lands
        /// on that country's calibration target - see WorldFactory's doc comment for the derivation.
        ///
        /// ⚠ WHAT THE NUMBER MEANS DEPENDS ON THE BASE IT IS SOLVED OVER. For the five countries on the sourced
        /// per-country bases (TaxBaseTable: realised revenue over the seeded rate), rate x base already IS
        /// realised revenue - the collection loss is inside the base - so the constant no longer marks anything
        /// down: it EXCEEDS 1, and the excess is the receipts the four instruments do not model (property, excise,
        /// social contributions beyond payroll...). It is COVERAGE, not efficiency, and its old reading
        /// ("how much of the theoretical base is actually collected, 0.0-1.0") is false for the five. For the USA,
        /// on the uniform stand-in bases by F-B's perimeter ruling, the old reading still holds and the value is
        /// below 1. A Finance minister's competence bias still adds to it (CabinetSystem); the floor is 0 and there
        /// is no ceiling - SimulationManager no longer clamps it to 1.
        /// </summary>
        public float CollectionEfficiency = 1f;

        /// <summary>
        /// Overrides SimulationManager.GetInterestOnDebt's base rate (in place of CurrencyZone.
        /// InterestRate) with this country's real blended average interest rate on existing debt,
        /// for a country where today's policy rate is a poor proxy for the average rate across its
        /// entire (long-duration, accumulated-over-many-years) debt stock - see
        /// "Reserve-Currency Debt Interest Treatment" in CLAUDE.md. -1 (the default) means unset -
        /// use CurrencyZone.InterestRate, unchanged behavior.
        /// </summary>
        public float BaseDebtInterestRateOverride = -1f;

        /// <summary>
        /// Scales GetDebtRiskPremium's output before it's added to the effective debt interest rate
        /// (1 = full market exposure, the default - unchanged behavior for every country except a
        /// reserve-currency issuer). A reserve-currency issuer (the USA) doesn't face the same market
        /// risk premium as other sovereigns at an equivalent debt-to-GDP ratio - who holds the debt
        /// and in what currency matters as much as the ratio itself - so its sensitivity is set near
        /// (not exactly) zero. See "Reserve-Currency Debt Interest Treatment" in CLAUDE.md.
        /// </summary>
        public float RiskPremiumSensitivity = 1f;

        /// <summary>Q3 (Master Sequence II step 1, rulings R-Q3a/b, 2026-08-17): the trend growth
        /// rate of labour PRODUCTIVITY, percent per year - the quantity the potential-growth
        /// ledger's sum now IS, per Design B's causal re-rooting: {infrastructure, sector}
        /// adjustments flow through productivity, wages read productivity's own growth, and
        /// PotentialGrowthRate reads productivity at 1:1 through the pipe
        /// (MacroSystem.ApplySectorGrowthEffect writes both, same sum, same clamps - a pure
        /// re-rooting, byte-identical by construction and by bar).
        /// ⚠ -1 is a SENTINEL (the R4-3 pattern): pre-Q3 saves and any read before the
        /// finalizer's first write fall back to PotentialGrowthRate via
        /// <see cref="ProductivityTrendGrowth"/> - bit-for-bit the value the old readers read,
        /// in every ordering. Readers use the property, never the raw field.</summary>
        public float ProductivityTrendGrowthRate = -1f;

        /// <summary>The read-side of the Q3 sentinel - see the field above.</summary>
        public float ProductivityTrendGrowth => ProductivityTrendGrowthRate >= 0f ? ProductivityTrendGrowthRate : PotentialGrowthRate;

        /// <summary>
        /// Step 2 (R-S2d/R-S2e): last CLOSED period's approval attribution - what the trace panel
        /// shows - and the ACCRUING one collecting this period's events until the boundary
        /// formula closes it. Both persisted: the closed one is R-S2e's "one period in the save
        /// shape"; the accruing one rides along so a mid-period save does not silently drop
        /// recorded events (the exact silent-gap class R-S2e's no-case predicted). Null on old
        /// saves and at seed - every reader guards, and the recorder lazily creates the accruing
        /// ledger at first touch. ⚠ NEVER EconomyState fields: the trajectory dump reflects
        /// EconomyState's public fields, and recording is OBSERVATION - it must not change the
        /// dump.
        /// </summary>
        public ApprovalAttribution ApprovalLedgerLastPeriod;
        public ApprovalAttribution ApprovalLedgerAccruing;

        /// <summary>
        /// Step 2's THIRD section (2026-08-25, on the trigger Italy Debt Crisis fired): the debt
        /// stock's attribution, the same closed/accruing pair as the approval ledger above and
        /// under the same rules - both persisted per R-S2e, null on old saves and at seed, every
        /// reader guards, the recorder opens the accruing one at the PRE-write stock on first
        /// touch. Terms accrue by observation one daily slice at a time (the stock moves daily),
        /// close where the FiscalTurnReport closes. ⚠ NEVER EconomyState fields - same reason.
        /// </summary>
        public DebtAttribution FiscalLedgerLastPeriod;
        public DebtAttribution FiscalLedgerAccruing;

        /// <summary>THE MATURITY RATE-LAG (2026-08-17, mechanism-report ruling R4, ruled IN with
        /// its target quantified by the erosion pass): this country's average debt maturity in
        /// YEARS - the stock rolls over at ~1/M per year, so the effective rate it pays reverts
        /// toward the current issuance rate at that speed (see
        /// SimulationManager.AdvanceEffectiveDebtRate). A structural per-country constant, seeded
        /// in WorldFactory from real debt-office data with source and date per line - Sweden's
        /// figure is a TIME-TO-REFIXING steering range, which for a repricing lag is the
        /// mechanism-relevant basis, stated there; Germany's is the batch's one REPORTED [GAP],
        /// seeded [ESTIMATED] with its bound stated.</summary>
        public float AverageDebtMaturityYears = 6f;

        /// <summary>THE MATURITY RATE-LAG's one piece of state: the blended rate this country's
        /// existing debt stock currently pays, in percent - reverting toward
        /// SimulationManager.GetDebtIssuanceRate at 1/AverageDebtMaturityYears per year. K252
        /// (ruled 2026-10-27): WorldFactory seeds it at the national accounts' implied rate
        /// (Generated.DebtRateSeeds), so a new world's stock opens paying what it paid.
        /// ⚠ -1 is a SENTINEL (the R4-3 HousingRateAnchor pattern): a pre-mechanism save
        /// deserializes with this initializer and every reader falls back to the CURRENT issuance
        /// rate - exactly what the old code charged - so old saves behave identically until the
        /// lag has something to lag. AdvanceEffectiveDebtRate initializes it on first advance;
        /// GetInterestOnDebt reads through a non-mutating fallback and never writes it (preview
        /// safety). Rides the World save layer as a public field; snapshotted explicitly in the
        /// round-trip diagnostic - confirmed, not assumed.</summary>
        public float EffectiveDebtInterestRate = -1f;

        /// <summary>
        /// This country's own fiscal-comfort anchor for SimulationManager.GetFiscalReactionMultiplier
        /// - the debt-to-GDP level at which the automatic fiscal reaction (see that method) is
        /// neutral. Above it, the reaction modestly tightens (raises effective revenue); below it, it
        /// loosens. A structural per-country constant, seeded in WorldFactory to match each country's
        /// own real-approximate starting debt-to-GDP ratio - reusing that already-researched figure,
        /// not a separately-tuned new one. See "Fiscal Reaction Function" in CLAUDE.md.
        /// <para>K269 (d) (fisc2, 2026-10-29), what it is: the fiscal reaction's NEUTRAL POINT, [AUTHORED-DRAFT] with the reaction's slope (the opening ratio is a
        /// proxy, no source names it a comfortable level). Since K251 the reaction - and so this anchor - acts in the book the player governs alone (an AI book's
        /// rule is the treaty's or the debt limit's), and the three fiscal-framework laws (LawCatalog's P4-C3 block) move it there. It is no longer drawn: the debt
        /// chart's line is the country's own fiscal framework's sourced figure (`FiscalRules.DebtReferencePercent`, the UI's rule table).</para>
        /// </summary>
        public float ComfortableDebtToGdpPercent = 60f;

        /// <summary>
        /// This country's structural "steady-state" poverty rate - the PovertyRate MacroSystem.
        /// ApplyPovertyRate's baseline computes toward when Unemployment sits at NaturalUnemploymentRate
        /// and Inflation sits at TaylorRule.InflationTarget(Id) (i.e. the gaps that otherwise move the
        /// baseline are both zero). Seeded per country from real OECD relative-poverty-rate data (see
        /// WorldFactory) - the same figure EconomyState.PovertyRate is seeded to, so a new game opens
        /// with PovertyRate already at (or very near) its own baseline rather than an artificial
        /// turn-1 jump, the same "avoid a one-time shock" lesson "Turn-1 GDP Consistency" established.
        /// </summary>
        public float BaselinePovertyRate = 10f;

        /// <summary>ROUND 4 BATCH 1 (C3): the structural youth-unemployment anchor - the target of
        /// ApplyYouthUnemployment's reversion when headline Unemployment sits exactly at NAIRU. Seeded
        /// to the same real Feb 2026 figure EconomyState.YouthUnemployment opens at (the standing
        /// zero-gap-at-start idiom).</summary>
        public float BaselineYouthUnemploymentRate = 15f;

        /// <summary>ROUND 4 BATCH 1 (C3): the sourced life expectancy at birth, 2024, years. MF q29: the calibration target of the 2024 mortality
        /// schedule (MortalitySchedule.Calibration) - the state opens at the derivation at the start's own year, so a start after 2024 opens above
        /// it and one before below it.</summary>
        public float BaselineLifeExpectancy = 80f;

        /// <summary>ROUND 4 BATCH 2 (C2): the structural inequality anchor - ApplyGini's reversion
        /// target when unemployment sits at NAIRU, no welfare program is implemented, income tax is
        /// at its seeded rate and the minimum wage at its own anchor. Seeded to the same real 2024
        /// Eurostat/OECD figure EconomyState.Gini opens at (zero-gap idiom).</summary>
        public float BaselineGini = 30f;

        /// <summary>ROUND 4 BATCH 2 (C2): the income-tax rate this country SEEDED with - the anchor
        /// ApplyGini measures redistribution against, captured once in WorldFactory from the seeded
        /// TaxLine (one authority; TaxLine.Rate itself is player-mutable with no stored seed, which
        /// is exactly why this anchor must exist as its own field). NOT a lever and never mutated
        /// after world creation - the same role BaselineMinimumWagePercentOfMedian plays for the
        /// minimum-wage gap.</summary>
        public float BaselineIncomeTaxRate = 30f;

        /// <summary>
        /// C-N4 (2026-08-31): **every tax line's rate AS SEEDED**, snapshotted by `SeedTaxLines` and never
        /// mutated — the anchor the disposable-income term measures a player's change from.
        ///
        /// <para>⚠ **This is what makes C-N4 SAFE rather than BASELINE, and it is not an accident.** It is
        /// `BaselineWelfarePrograms`' own idiom, adopted for the same reason that field records: the
        /// sourced seeds already contain each country's real tax position, so a country sitting at its
        /// seeded rates must contribute **exactly zero** to the new term, and a player's change is booked
        /// from the country's own real position rather than from an arbitrary zero. The consequence is
        /// that the no-policy trajectory cannot move — and the dump is run to prove that rather than the
        /// reasoning being trusted.</para>
        /// </summary>
        public Dictionary<TaxType, float> BaselineTaxRates = new Dictionary<TaxType, float>();

        /// <summary>A-LOOP q34 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §1): <b>which lines the seed levied - the record</b>, the tax lines
        /// `IsImplemented` at the seed, taken by `WorldFactory.SeedTaxLines` beside `BaselineTaxRates` and never mutated. `BaselineTaxRates` holds EVERY
        /// line's seeded rate, the never-levied lines' placeholders included, so it cannot say what the seed levied (D-32): the household burden
        /// (`MacroSystem.HouseholdBurdenGap`) reads its seed side from this set - a line the seed levied at its seed rate whether or not it is levied
        /// today, a line it did not at zero - so a levy is a rise from zero and a removal is relief.
        /// <para>⚠ <b>"The seed levied it" is the GAME's seed, not the statute</b> (§1 (6)): the excise, property, wealth and stamp-duty lines are
        /// seeded inactive by convention, though Sweden levies excise duties and a municipal property fee in fact; q34 does not re-seed them.</para>
        /// <para>Saved (additive, no format step, ruling A). A save from before carries none (null), and `SimulationManager.RestoreSaveState` reads
        /// it as the lines levied at the load - the older build's own reading, which counted only today's levied lines on both sides, so such a
        /// game's burden loads to the figure it was saved at. Null on a hand-made country: nothing levied at the seed, as its empty
        /// `BaselineTaxRates` already read.</para></summary>
        public List<TaxType> SeedLevied;

        /// <summary>A-LOOP q34: whether the seed levied this line (`SeedLevied`) - false where the set is null. A loop rather than the list's own Contains, so the daily
        /// burden boxes no enum.</summary>
        public bool SeedLevies(TaxType type)
        {
            if (SeedLevied == null) { return false; }
            foreach (TaxType levied in SeedLevied) { if (levied == type) { return true; } }
            return false;
        }

        /// <summary>A-LOOP q37 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §2): <b>the tax term T</b>, approval points the government's tax changes hold
        /// against it - what LANDED of each move's impulse (`TaxTerm.Move`, `TaxTerm.ChargeApproval`, `TaxTerm.BookFormula`), fading by
        /// `TaxTerm.DailyPersistence` a day (`TaxTerm.AdvanceDay`), what fades handed back to approval; approval's reversion reads approval + this.
        /// Positive: a standing penalty; negative: a cut's mirror still standing. Saved (format 44); carried on the preview's clone.</summary>
        public float TaxTermStock;

        /// <summary>A-LOOP q37: <b>the year's booked net per line</b> - the sum, since the fiscal year's opening, of the moves the government made to each
        /// line, in the tax term's one unit (`ParliamentSystem.TaxLevelPoints` on either side of each write: 0 for a line not levied), read by every
        /// impulse (`TaxTerm.Move`). The opening shifted by every move no one booked (the statute's step, indexation, a bill the government does not
        /// bear). Null - every line at its opening - on the fiscal year's first day (`TaxTerm.AdvanceDay`) and until the year's first move. Saved
        /// (format 44); copied on the preview's clone.</summary>
        public Dictionary<TaxType, float> TaxTermNet;

        /// <summary>A-LOOP q5 (`docs/specs/A_LOOP_PLAN.md` §6 (2)): <b>what events still hold of the country's salience above its sourced seed</b>, per issue
        /// (indexed by `IssueId`, share units 0..1): an adverse event's fitted jump lands on its issue (`EventSalience.Land`), is held flat for its class's hold (`SalienceHoldDays`) and then decays back daily
        /// (`EventSalience.AdvanceDay`). The vector the stance model and the campaign read is the seed plus this, clamped to [0, 1]
        /// (`EventSalience.Of`, `EventSalience.ApplyToCampaign`). Null while nothing stands. Saved (format 48 under ruling A: an older save is refused);
        /// copied on the preview's clone.</summary>
        public float[] SalienceShift;

        /// <summary>A-LOOP q5 (review 882b's M-A): the days each issue's shift is still HELD flat before it decays (indexed by `IssueId`), set to the
        /// class's hold by `EventSalience.Land` and counted down by `EventSalience.AdvanceDay`; null with the shift. Saved (format 48); copied on the
        /// preview's clone.</summary>
        public int[] SalienceHoldDays;

        /// <summary>K94 (Elias, 2026-10-24): the step each issue's shift still RAMPS by a day toward its event's peak (indexed by `IssueId`, share units),
        /// set by `EventSalience.Land` for a class whose peak is later than its first wave and applied by `EventSalience.AdvanceDay` in the hold's last
        /// days; zero once the hold ends; null while no ramp has been stored. Saved (format 61); copied on the preview's clone.</summary>
        public float[] SalienceRampPerDay;

        /// <summary>P-I2 stage 1: the country's FIVE-YEAR AGE PYRAMID, 21 bands in millions of persons,
        /// seeded from `PopulationPyramids` (Eurostat demo_pjan 1 Jan 2024 for the EU five, US Census PEP
        /// vintage 2024 for the USA).
        /// <para>⚠ <b>Read-only in this stage, and that is the whole point of landing it alone.</b> Nothing
        /// ages it and nothing in `EconomyState` derives from it yet, so the no-policy trajectory cannot
        /// move — which the dump is run to PROVE rather than the reasoning being trusted. The spec-let's
        /// five collisions (`COMPLETED.md §201` §4) all live in the stage that retires the eight
        /// demographic scalars, and that stage carries its own explained baseline family.</para>
        /// <para>⚠ Not persisted yet, deliberately: it is re-seeded from the sourced table on load, exactly
        /// as `BaselineTaxRates` is. The save-layer bump belongs to the stage that makes it mutable, where
        /// an absent pyramid would mean a different game state rather than a harmless default.</para>
        /// </summary>
        public PopulationCohorts Cohorts;

        /// <summary>ROUND 4 BATCH 3 (C1): whether housing cost overburden is a tracked stat for
        /// this country - TRUE for the EU five ([VERIFIED] Eurostat whole-population figures),
        /// FALSE for the USA, whose sources measure a different threshold on a different income
        /// basis (no comparable figure exists; the recorded ruling gives the USA homeownership as
        /// its primary housing metric instead). The MinimumWageImplemented idiom: a structural
        /// per-country fact, not a lever. Where false, ApplyHousingOverburden early-outs and the
        /// UI draws no overburden row - the asymmetry is deliberate everywhere it appears.</summary>
        public bool TracksHousingOverburden = true;

        /// <summary>ROUND 4 BATCH 3 (C1): structural overburden anchor, % of population - the
        /// reversion target at the epoch policy rate with no HousingAssistance. Meaningless where
        /// TracksHousingOverburden is false (left at 0 for the USA).</summary>
        public float BaselineHousingOverburden = 0f;

        /// <summary>ROUND 4 BATCH 3 (C1): structural homeownership anchor, % of households (OECD
        /// AHD basis). Seeded for all six; the USA's primary housing metric per the ruling.</summary>
        public float BaselineHomeownership = 65f;

        /// <summary>
        /// Whether this country has a statutory minimum wage at all - false for Sweden and Italy,
        /// matching real-world fact (both rely on sector-level collective bargaining instead of a
        /// legal minimum), true for the other four. A structural fact, not a player-togglable
        /// implement/remove switch like TaxLine/WelfareProgram - the player can only adjust the LEVEL
        /// where a statutory minimum already exists. See MinimumWagePercentOfMedian.
        /// </summary>
        public bool MinimumWageImplemented;

        /// <summary>
        /// This country's minimum wage expressed as a percentage of its median wage (the "Kaitz
        /// index" economists commonly use for cross-country comparison, e.g. France ~66%) rather
        /// than an absolute currency amount - keeps it comparable across countries with very
        /// different wage levels. Only meaningful when MinimumWageImplemented is true. Persistent,
        /// player-adjustable via PolicyDecision.MinimumWageOverride (an absolute target, the same
        /// "SET, not delta" idiom as TaxLine.Rate) - see SimulationManager.ApplyMinimumWageChange.
        /// </summary>
        public float MinimumWagePercentOfMedian;

        /// <summary>
        /// This country's minimum wage level (percent of median wage) at the start of the game -
        /// the anchor MacroSystem's minimum-wage employment/poverty effects measure the CURRENT
        /// MinimumWagePercentOfMedian against, not a universal constant (the same "gap versus a
        /// country-specific anchor" idiom ComfortableDebtToGdpPercent/BaselinePovertyRate already
        /// use). Seeded equal to the country's own starting MinimumWagePercentOfMedian, so a fresh
        /// game opens at zero gap (no effect) rather than an artificial turn-1 shock, and so the
        /// effect doesn't double-count against NaturalUnemploymentRate, which already reflects each
        /// country's real structural conditions including its actual minimum wage. 0 for a country
        /// with no statutory minimum wage (MinimumWageImplemented false).
        /// </summary>
        public float BaselineMinimumWagePercentOfMedian;

        /// <summary>
        /// This country's real statutory paid family/parental leave, in weeks (see WorldFactory for
        /// per-country sourcing - USA 0/Sweden 69/Germany 58 are directly confirmed via web search;
        /// France/Italy/Poland are directionally-informed estimates from general knowledge of each
        /// country's real statutory system, not individually confirmed to the same precision).
        /// Persistent, player-adjustable via PolicyDecision.PaidFamilyLeaveWeeksOverride (an absolute
        /// target, the same "SET, not delta" idiom as TaxLine.Rate) - see
        /// SimulationManager.ApplyLaborPolicyChanges.
        /// </summary>
        public float PaidFamilyLeaveWeeks;

        /// <summary>
        /// This country's paid-family-leave level at the start of the game - the anchor
        /// MacroSystem's paid-leave LaborForceParticipationRate/ApprovalRating effects measure the
        /// CURRENT PaidFamilyLeaveWeeks against, not a universal constant (the same "gap versus a
        /// country-specific anchor" idiom MinimumWage's own BaselineMinimumWagePercentOfMedian
        /// already uses). Seeded equal to the country's own starting PaidFamilyLeaveWeeks, so a fresh
        /// game opens at zero gap (no effect) rather than an artificial turn-1 shock.
        /// </summary>
        public float BaselinePaidFamilyLeaveWeeks;

        /// <summary>
        /// This country's overtime/working-hour regulation strictness, 0-100 (0 = unregulated/long
        /// hours allowed, 100 = strict caps; 50 = neutral - a uniform placeholder for every country,
        /// the same reasoning Country.PoliceFundingLevel already uses, since there's no single clean
        /// cross-country comparable "regulation strictness" index). Persistent, player-adjustable via
        /// PolicyDecision.OvertimeRegulationOverride. Its Unemployment effect (see
        /// MacroSystem.GetOvertimeUnemploymentAdjustment) represents ONE side of a genuinely contested
        /// real economic debate (the "work-sharing" argument behind France's 35-hour week) - honestly
        /// simplified, not a settled empirical fact.
        /// </summary>
        public float OvertimeRegulationLevel = 50f;

        /// <summary>
        /// This country's workforce retraining program level, 0-100 (50 = neutral placeholder for
        /// every country - no real cross-country comparable figure exists for this). Persistent,
        /// player-adjustable via PolicyDecision.RetrainingProgramOverride. Reduces Unemployment - the
        /// well-established real economic rationale that retraining eases job transitions.
        /// </summary>
        public float RetrainingProgramLevel = 50f;

        /// <summary>
        /// THE STATUTORY BASE fields (pass 3, the Labor Market law category, coexistence ruling
        /// 2026-08-26): the BILL-OWNED half of each labor dial. Elias ruled the Labor tab KEEPS its
        /// sliders when labor laws ship (the deliberate opposite of the Crime &amp; Justice tab's
        /// read-only conversion), so the two-books problem is solved by splitting each dial in two
        /// books EXPLICITLY: LaborPolicyBill sets these base fields absolutely (same clamps as
        /// before), enacted laws contribute a pure delta sum on top, and
        /// SimulationManager.RecomputeLaborDialsFromEnactedLaws composes
        /// effective = clamp(base + law deltas) into the effective field above/below it - clamped
        /// ONCE at composition, never persisted into either component, so full repeal returns the
        /// effective dial exactly to base and a passed bill never stomps law effects.
        ///
        /// Seeded equal to each dial's own starting value in WorldFactory (zero law offset at seed).
        /// -1 is the "unset" sentinel for OLD SAVES only (a save written before these fields
        /// existed deserializes them at this default): RestoreSaveState adopts the saved dial value
        /// as the base, which is exactly right because no pre-pass-3 save can hold a labor law.
        /// MinimumWagePercentOfMedianBase stays meaningful only where MinimumWageImplemented (0 for
        /// Sweden/Italy, matching the dial itself).
        /// </summary>
        public float MinimumWagePercentOfMedianBase = -1f;
        public float PaidFamilyLeaveWeeksBase = -1f;
        public float OvertimeRegulationBase = -1f;
        public float RetrainingProgramBase = -1f;
        public float FamilyPolicyBase = -1f;
        public float ImmigrationPolicyBase = -1f;

        /// <summary>
        /// This country's structural "steady-state" CrimeIndex - the target MacroSystem.
        /// ApplyCrimeIndex's mean-reversion moves EconomyState.CrimeIndex toward absent any policy
        /// input (the same "avoid a turn-1 shock" anchor idiom BaselinePovertyRate/
        /// BaselineLaborForceParticipationRate - retired at F2 step 5 - used). Seeded per country from a STYLIZED 0-100
        /// scale informed by real relative homicide-rate rankings (see WorldFactory) - not a literal
        /// transformation of any single real indicator, since "crime" as a broad concept has no single
        /// clean cross-country comparable metric the way poverty/labor-participation rates do.
        /// </summary>
        public float BaselineCrimeIndex = 25f;

        /// <summary>
        /// This country's relative police funding effort, 0-100 (50 = neutral/baseline for every
        /// country - a uniform placeholder, since there's no clean real-world cross-country "relative
        /// policing effort" figure to seed differently per country, unlike CrimeIndex itself).
        /// Persistent, player-adjustable via PolicyDecision.PoliceFundingOverride (an absolute target,
        /// the same "SET, not delta" idiom as TaxLine.Rate) - see
        /// SimulationManager.ApplyCrimePolicyChanges. Higher funding reduces CrimeIndex - see
        /// MacroSystem.ApplyCrimeIndex.
        /// </summary>
        public float PoliceFundingLevel = 50f;

        /// <summary>
        /// This country's sentencing policy, 0-100 (0 = lenient/rehabilitation-focused, 100 = harsh/
        /// punitive; 50 = neutral, the same uniform-placeholder reasoning as PoliceFundingLevel).
        /// Persistent, player-adjustable via PolicyDecision.SentencingSeverityOverride. Its effect on
        /// CrimeIndex is deliberately smaller than PoliceFundingLevel's - criminology research
        /// consistently finds the CERTAINTY of enforcement matters more for deterrence than the
        /// SEVERITY of punishment (see MacroSystem.ApplyCrimeIndex's doc comment).
        /// </summary>
        public float SentencingSeverity = 50f;

        /// <summary>
        /// This country's structural "steady-state" incarceration rate (per 100,000 population) -
        /// the target MacroSystem.ApplyPrisonPopulationRate's mean-reversion moves EconomyState.
        /// PrisonPopulationRate toward absent any policy input. Seeded per country from real World
        /// Prison Brief data (see WorldFactory) - the same figure EconomyState.PrisonPopulationRate
        /// is seeded to, so a new game opens already at (or very near) its own baseline.
        /// </summary>
        public float BaselinePrisonPopulationRate = 100f;

        /// <summary>
        /// This country's bail policy, 0-100 (0 = traditional cash bail, 100 = full bail reform/
        /// no-cash-bail; 50 = neutral placeholder for every country - bail-bond systems as such are
        /// most directly analogous to the US context, but this dial is kept universal, the same
        /// uniform-dial idiom PoliceFundingLevel/SentencingSeverity already use). Persistent, player-
        /// adjustable via PolicyDecision.BailReformOverride. Reduces PrisonPopulationRate (bail
        /// reform's primary real-world goal - reducing pretrial detention) and modestly increases
        /// CrimeIndex - a small, HONESTLY CONTESTED effect (see MacroSystem.ApplyCrimeIndex), the
        /// same "flag the real debate, don't pretend it's settled" treatment
        /// OvertimeRegulationLevel's own Unemployment effect already got in "Deeper Labor Market
        /// Policies".
        /// </summary>
        public float BailReformLevel = 50f;

        /// <summary>
        /// This country's drug policy, 0-100 (0 = decriminalized/harm-reduction, 100 = strict
        /// criminalization; 50 = neutral placeholder for every country - no clean cross-country
        /// comparable index exists for this). Persistent, player-adjustable via
        /// PolicyDecision.DrugPolicyOverride. Increases PrisonPopulationRate when stricter - the
        /// well-documented real link between strict drug enforcement and mass incarceration (the US
        /// "war on drugs" being the clearest real-world example) - and modestly increases
        /// ApprovalRating (a "tough on crime" political dynamic, small like PoliceFundingLevel's own
        /// political framing).
        /// </summary>
        public float DrugPolicyLevel = 50f;

        /// <summary>
        /// Round 3 item 3: this country's structural "steady-state" OrganizedCrimeIndex - the target
        /// MacroSystem.ApplyOrganizedCrimeIndex's mean-reversion moves EconomyState.OrganizedCrimeIndex
        /// toward absent any policy input (the same "avoid a turn-1 shock" anchor idiom
        /// BaselineCrimeIndex already uses). Seeded per country informed by the real Global Organized
        /// Crime Index (GI-TOC) - Italy's historic, extremely well-documented organized-crime
        /// organizations (Cosa Nostra, Camorra, 'Ndrangheta) give it high confidence as the clear
        /// highest of the six; Sweden's real, well-documented recent gang-violence surge (the same
        /// fact already informing its elevated BaselineCrimeIndex) justifies its own elevated figure.
        /// The remaining relative ordering (USA/France/Poland/Germany) is a directional, stylized
        /// estimate, not independently confirmed against a specific index-year - see WorldFactory.
        /// </summary>
        public float BaselineOrganizedCrimeIndex = 25f;

        /// <summary>
        /// Round 3 item 3: this country's structural "steady-state" CorruptionIndex - the target
        /// MacroSystem.ApplyCorruptionIndex's mean-reversion moves EconomyState.CorruptionIndex toward
        /// absent any policy input (the same anchor idiom BaselineCrimeIndex already uses). Higher =
        /// MORE corrupt (matching this project's own "higher = worse" convention for CrimeIndex/
        /// PrisonPopulationRate) - seeded as roughly 100 minus the real Transparency International
        /// Corruption Perceptions Index (CPI, itself 0-100 with higher = cleaner), not a literal
        /// year-specific score. Nordic/German clean-government reputation and Italy's comparatively
        /// lower CPI standing among Western European/G7 peers are both real and well-documented, high
        /// confidence; the exact relative ordering of Italy versus Poland specifically is a directional
        /// estimate, not confirmed against one index-year - see WorldFactory.
        /// </summary>
        public float BaselineCorruptionIndex = 30f;

        /// <summary>
        /// Round 3 item 3: this country's judicial system funding level, 0-100 (50 = neutral placeholder
        /// for every country - no clean real-world cross-country "relative judicial funding" figure
        /// exists to seed differently per country, the same uniform-dial idiom PoliceFundingLevel
        /// already uses). Persistent, player-adjustable via PolicyDecision.JudicialFundingOverride.
        /// Reduces OrganizedCrimeIndex (better prosecution capacity disrupts organized-crime networks)
        /// and CorruptionIndex (an independent, well-funded judiciary is a canonical real-world
        /// anti-corruption mechanism), and modestly reduces PrisonPopulationRate (well-funded courts
        /// process cases faster, reducing pretrial-detention backlog - a real, well-documented driver
        /// of high incarceration in underfunded systems) - see MacroSystem.
        /// </summary>
        public float JudicialFundingLevel = 50f;

        /// <summary>
        /// Round 3 item 3: this country's border enforcement strictness, 0-100 (0 = open/lenient, 100
        /// = strict; 50 = neutral placeholder for every country, the same uniform-dial idiom
        /// PoliceFundingLevel already uses). Persistent, player-adjustable via
        /// PolicyDecision.BorderEnforcementOverride. Reduces OrganizedCrimeIndex - stricter enforcement
        /// disrupts cross-border smuggling/trafficking, organized crime's real, well-documented core
        /// activity - see MacroSystem.ApplyOrganizedCrimeIndex. Deliberately scoped to this ONE channel
        /// for this pass, not a new labor-supply/immigration effect (keeping effects routed through
        /// already-proven channels, per this item's own explicit instruction).
        ///
        /// THE COUPLINGS PASS (terminal ruling 2026-08-26) RE-EXAMINED the single-edge status and
        /// DECLINED a second sim edge, reasons recorded: the migration channel belongs to
        /// ImmigrationPolicyLevel (0.1/pt, the anti-double-counting design), and the documented
        /// deterrence elasticity is modest (Angelucci 2012: -0.4..-0.8). The dial DID gain a
        /// budget edge (its enforcement cost lands on a real spending line - see
        /// SimulationManager.ApplyEnforcementCostPressure) and keeps its transitive crime chain.
        /// Single direct edge, ruled honest rather than incomplete.
        /// </summary>
        public float BorderEnforcementLevel = 50f;

        /// <summary>
        /// THE COUPLINGS PASS (2026-08-26): the last dollar amount of dial-driven enforcement cost
        /// applied to this country's justice-routed spending line (Justice, else PublicServices) -
        /// police + judicial dial gaps at their ruled shares plus the incarceration variable cost.
        /// The tracker that lets a STATELESS cost target compose with the STATEFUL line writers:
        /// each boundary applies only the difference from this. Additive save field; old saves
        /// load 0 and self-correct at their first boundary. See
        /// SimulationManager.ApplyEnforcementCostPressure.
        /// </summary>
        public float AppliedJusticeEnforcementCost = 0f;

        /// <summary>The border twin of <see cref="AppliedJusticeEnforcementCost"/> - the last
        /// applied border-enforcement cost on the border-routed line (HomelandSecurity, else
        /// Migration, else PublicServices).</summary>
        public float AppliedBorderEnforcementCost = 0f;

        /// <summary>P4-B3 (2026-09-04): the sector twin of <see cref="AppliedJusticeEnforcementCost"/> - the last applied
        /// sector-support cost (SectorCouplings: subsidies, tax credits and research grants above the neutral dial) on the
        /// support line - each statute budget's Business-and-industry line, the USA's Commerce (SC-1, ruled 2026-09-15). Additive save field; old saves
        /// load 0 and self-correct at their first boundary.</summary>
        public float AppliedSectorSupportCost = 0f;

        /// <summary>EN-7a (2026-09-14): the Energy sector's SUBSIDY as retail intervention's money side - the last applied subsidy cost on the
        /// book's energy line (`SectorCouplings.EnergySupportCostTarget`), where EN-4's levy rule reads the line's move off its path. Zero where
        /// the book carries no energy line (Germany: the subsidy's cost stays in the other sectors' target, `SectorCouplings.SupportCostTarget`). Like the other
        /// three trackers it carries the whole target outside its line's seed band (SC-1, ruled 2026-09-15 - the line is its clamped own path plus the cost),
        /// short of it only where a cut would take the line below zero. Additive save field; old saves load 0.</summary>
        public float AppliedEnergySupportCost = 0f;

        /// <summary>P6-F2 (2026-09-18, §539): the energy fleet's connection queue - build and retire orders by technology, MW, the year placed and the year they
        /// connect or leave (<see cref="Simulation.EnergyFleet"/>). Null or empty on every AI state and at the seed; the landed ones are the fleet's delta,
        /// read off this list every time a reader asks (§544: no table - the fleet is the country's own). Additive save field: a save from before it loads none.</summary>
        public System.Collections.Generic.List<Simulation.EnergyFleet.Order> FleetOrders = null;

        /// <summary>
        /// This country's old-age dependency ratio at the start of play (65+ as a percentage of
        /// 15–64), the fixed anchor every gap-based effect (pension pressure, labor force
        /// participation) measures against - the "avoid a turn-1 shock" idiom BaselineCrimeIndex and
        /// BaselinePovertyRate use. Since F2 step 4 (2026-09-02) it is RE-SEEDED at world creation
        /// from the cohort pyramid walked to the epoch (WorldFactory, CohortDemographics.WalkToEpoch),
        /// so it is exactly the ratio EconomyState.DependencyRatio reads on day one and the gap opens
        /// at zero. The typed per-country figures in WorldFactory (28–40) were the retired scalar
        /// demography's seeds, kept there as the record of what they were; the distance between them
        /// and the pyramid's own ratio is in COMPLETED.md §195.
        /// </summary>
        public float BaselineDependencyRatio = 30f;

        /// <summary>
        /// This country's sourced starting net migration rate (per 1,000 population per year) - the
        /// fixed anchor the gap-based LaborForceParticipationRate effect measures against, the same
        /// idiom every other Baseline field uses. Since F2 step 4 the live NetMigrationRate is a
        /// reading of the cohort substrate (the net migration the publisher's projection assumes,
        /// plus the lever), so a projection assuming more immigration than 2024 had reads as a
        /// positive gap against this sourced anchor - see CohortDemographics.
        /// </summary>
        public float BaselineNetMigrationRate = 1f;

        /// <summary>
        /// Family/childcare policy support intensity, 0-100 (0 = minimal support, 100 = maximal
        /// pro-natalist support; 50 = neutral, the same uniform-dial idiom BorderEnforcementLevel
        /// uses). Persistent, player-adjustable via PolicyDecision.FamilyPolicyOverride - see
        /// SimulationManager.ApplyDemographicPolicyChanges. Since F2 step 4 it reaches the cohort
        /// substrate as a fertility multiplier on the year's births (CohortDemographics.Step), at the
        /// magnitude the retired additive rule had (LaborCouplings.FamilyPolicyBirthRateSensitivity
        /// points per thousand on the natural crude birth rate). Deliberately SMALL: real-world
        /// evidence on pro-natalist policy's effect on fertility is itself small and contested - this
        /// lever nudges the trajectory, it does not reverse a country's demographic direction alone.
        /// </summary>
        public float FamilyPolicyLevel = 50f;

        /// <summary>
        /// Immigration policy openness, 0-100 (0 = maximally restrictive, 100 = maximally open; 50 =
        /// neutral). Persistent, player-adjustable via PolicyDecision.ImmigrationPolicyOverride - see
        /// SimulationManager.ApplyDemographicPolicyChanges. Since F2 step 4 it reaches the cohort
        /// substrate as additive net migration distributed over the sourced immigration age profile
        /// (CohortDemographics.Step), at the magnitude the retired rule had
        /// (LaborCouplings.ImmigrationPolicyNetMigrationSensitivity per thousand). A wider bound than
        /// FamilyPolicyLevel's - immigration is a genuinely more responsive real-world lever than
        /// fertility (visa/asylum/quota policy can move actual migration within a single term).
        /// Its labor-force effect deliberately reuses the EXISTING NetMigrationRate-gap term in
        /// ApplyLaborForceParticipationRate's combined ceiling rather than a second, parallel
        /// immigration-to-labor-force channel - one variable, one downstream channel, structurally.
        /// </summary>
        public float ImmigrationPolicyLevel = 50f;

        /// <summary>
        /// The head of this country's own central bank (the USA, Sweden and Poland), or null for a euro member, whose shared
        /// zone's head is CurrencyZone.Head (the ECB's President). Non-null means CurrencySystem bypasses
        /// PolicyDecision.InterestRateChange entirely and instead moves InterestRate toward TaylorRule.GetSuggestedInterestRate
        /// plus this head's RateBias each turn; the head serves its statute's term (AF-7, CentralBankTerms) -
        /// see FederalReserveSystem and docs/reference/MODEL_REFERENCE.md's "Federal Reserve" section.
        /// </summary>
        public FedChair CurrentFedChair;

        /// <summary>
        /// K15 (Elias's ruling of 2026-10-23; `UsFedConsent`'s class note): the Fed's Vice Chair - an ORIGINAL FICTIONAL person seated with the USA's world, the
        /// one who acts as chair while the chair's term has ended and no successor is confirmed. Null for every other country.
        /// </summary>
        public FedChair FedViceChair;

        /// <summary>K15: true on the days the Vice Chair acts as chair - the chair's term ended, no successor confirmed - set by the day's step
        /// (`SimulationManager.AppointCentralBankHeadsWhoseTermsEnded`); the rate rule then reads the Vice Chair's lean (`FederalReserveSystem.PolicyHead`).</summary>
        public bool FedViceChairActing;

        /// <summary>K16: the player-president's Fed nominee before the Senate, or null (`FedNomination`).</summary>
        public FedNomination FedNominationPending;

        /// <summary>K16: every Senate vote on a Fed nominee the game held for this country, oldest first (`FedSenateVote`); a refusal plays from here as an event.</summary>
        public List<FedSenateVote> FedSenateVotes = new List<FedSenateVote>();

        /// <summary>
        /// This country's sovereign wealth fund, or null (the default - every country) if it doesn't
        /// exist. The player creates/dissolves it via an immediate action (mirrors TaxLine.
        /// IsImplemented's on/off pattern), the same way a non-null CurrentFedChair switches USA's
        /// interest-rate mechanic. USA-first only in this pass - see "Sovereign Wealth Fund" in
        /// CLAUDE.md.
        /// </summary>
        public SovereignWealthFund SovereignWealthFund;

        /// <summary>
        /// Political Systems Overhaul Part A (Cabinet): this country's currently-appointed ministers,
        /// keyed by portfolio - empty for every country by default (the same "doesn't exist until the
        /// player acts" idiom SovereignWealthFund/CurrentFedChair already use). No NPC/player
        /// distinction is needed anywhere CabinetSystem or its three effect-landing call sites read
        /// this dictionary: the Cabinet UI only ever lets the player appoint into their OWN country, so
        /// every other country's dictionary simply stays empty forever, and an empty-dictionary lookup
        /// naturally contributes zero effect - the same structural no-op SovereignWealthFund's null
        /// check already gets for free, just via TryGetValue instead of a null check.
        /// </summary>
        public Dictionary<CabinetPortfolio, CabinetMinister> CabinetMinisters = new Dictionary<CabinetPortfolio, CabinetMinister>();
        /// <summary>P2-5.2 (2026-09-02): the cabinet events LOYALTY's term has produced (resignations, leaks), newest last - the Docket's minister alerts read them.</summary>
        public List<CabinetEventRecord> CabinetEvents = new List<CabinetEventRecord>();
        /// <summary>Elias's ruling F5 ("the check reads every bill lost that day"): the day a bill was last lost - a division the chamber failed, or a veto that
        /// stood - written where the loss happens (`ParliamentSystem.RecordBillLost`) and read by the cabinet's pressure check (`CabinetSystem.UnderPressure`),
        /// so a bill that passes later the same day hides no loss and the division log stays unread by the simulation. MinValue = none; a save from before
        /// loads it so.</summary>
        public System.DateTime BillLostOn;

        /// <summary>PS-6 US-22 (R-US9 as ruled, F8): THE FUNDING HORIZON - the last day the government is funded by an appropriation or a continuing
        /// resolution in force (31 U.S.C. 1341(a)(1): no expenditure "exceeding an amount available in an appropriation"). Seeded once where the US Congress
        /// votes the budget, as the end of the fiscal year in progress (DECLARED: the record's acts in force at the start are not read), and advanced the day a
        /// funding measure becomes law (`UsBudgetProcedure.FundedThrough`). MinValue where no Congress votes the budget. World state, so it rides the save
        /// (format 41) and the preview's clone.</summary>
        public System.DateTime AppropriatedThrough;

        /// <summary>PS-6 US-22: THE LAPSE - the first day of a funding gap (the day after <see cref="AppropriatedThrough"/>), held until the day an appropriation or
        /// a continuing resolution becomes law; its days are said on screen (`UsBudgetProcedure.LapseDays`). MinValue = funded. What a lapse does to the book
        /// and to output is US-22b's (`UsLapse`), read through <see cref="LapseFlowDay"/>.</summary>
        public System.DateTime LapseSince;

        /// <summary>K42 (Elias, 2026-10-23: "Funding restored becomes a saved field"): the day the last lapse ended - the day an appropriation or a continuing
        /// resolution became law while funding was lapsed (`SimulationManager.FundedByLaw`) - which the Desk's lapse chip reads as FUNDING RESTORED on that day
        /// (`UsBudgetProcedure.RestoredWithin`, first drawn in its window). MinValue where no lapse has ended. Saved (format 57), so a game loaded on the day shows it.</summary>
        public System.DateTime LapseRestoredOn;

        /// <summary>PS-6 US-22 (Ask 2's reading (P), PROVISIONAL; it stands, Elias's ruling I13 (2)): the party withholding the continuing resolution - the
        /// player's verb (`SimulationManager.WithholdContinuingResolution`), standing until released or until funding is restored; null for none. An AI party
        /// never withholds.</summary>
        public string ContinuingResolutionWithheldBy;

        /// <summary>PS-6 K75 (Elias's ruling of 2026-10-23, `docs/RULINGS_2026-10-23.md`: "a player-president's own party may withhold the continuing
        /// resolution"): the day the standing withhold was given (`SimulationManager.WithholdContinuingResolution`), MinValue where none stands - for the record
        /// and the log; whether it lapses with the presidency is `ContinuingResolutionWithheldInOffice`'s (the review's L1). It rides the save (format 57) and the
        /// preview's clone.</summary>
        public System.DateTime ContinuingResolutionWithheldOn;

        /// <summary>PS-6 K75 (the review's L1): true where the standing withhold was given while its party held the presidency and the party has not left it since
        /// (`UsBudgetProcedure.NoteWithholderInOffice`) - such a withhold stands through a re-election's second oath; false otherwise, and a withholding party that
        /// takes the presidency lapses its withhold. It rides the save (format 57, additive: false is an earlier build's true state - only an out-party withheld)
        /// and the preview's clone.</summary>
        public bool ContinuingResolutionWithheldInOffice;

        /// <summary>PS-6 K22 (Elias's ruling of 2026-10-23: "a hostile chamber can withhold the raise, as with the CR"): the party withholding the debt-limit
        /// raise - the player's verb (`SimulationManager.WithholdDebtLimitRaise`), offered where the player's party holds a chamber the presidency's party does
        /// not; standing until released, until a raise or a suspension is law, or until the withholding party holds the presidency; null for none. PS-6 K78: or
        /// an AI party holding a chamber against the presidency, by the record's episodes' rule (`SimulationManager.AiWithholdDay`). It rides the save (format 57)
        /// and the preview's clone.</summary>
        public string DebtLimitWithheldBy;

        /// <summary>PS-6 K90 (Elias's ruling of 2026-10-24): true where the player-president answered the binding limit's hold with "let it bind"
        /// (`SimulationManager.LetDebtLimitBind`) - the hold stays released in the episode until something changes (a raise that falls or is vetoed, the AI's
        /// withhold released, the episode closed). It rides the save (additive: false is an earlier build's state) and the preview's clone.</summary>
        public bool DebtLimitLetBind;

        /// <summary>PS-6 US-22b (I13 (1)): the day whose flows run lapsed - armed by the funding tick for the next day while funding is lapsed (`UsLapse.Lapsed`),
        /// MinValue otherwise. World state: it rides the save (format 42) and the preview's clone.</summary>
        public System.DateTime LapseFlowDay;

        /// <summary>PS-6 US-22b (I13 (1)): the non-excepted outlays a lapse has stopped, NOMINAL, held until funding returns and then paid in full (31 U.S.C.
        /// 1341(c); `UsLapse.OutlayToday`). 0 where nothing is owed.</summary>
        public float LapseDeferredOutlays;

        /// <summary>PS-6 US-22b (I13 (1)): the overlay on real output applied today (`UsLapse.ApplyOutput`) - the level the identity produced less what stands
        /// in `EconomyState.GDP` - removed before the next day's identity (`UsLapse.RestoreOutput`). 0 where none is applied.</summary>
        public float LapseOutputWedge;

        /// <summary>PS-6 US-22b (I13 (1), the review's H1): the last lapse's days of lapsed flows, which scale CBO's quarterly levels (`UsLapse.ApplyOutput`); 0
        /// where no lapse is running and no profile is held.</summary>
        public int LapseOutputDays;

        /// <summary>PS-6 US-22b (the review's H1): the first day of the calendar quarter the last lapse ended in - the profile's quarter 0; MinValue while a lapse
        /// runs and where no profile is held.</summary>
        public System.DateTime LapseProfileQuarter;

        /// <summary>MF q26 (ruling q26: "Unserved energy costs output at ACER's VoLL (CEPA 2018)"; K145 for the USA): the output the year's unserved energy
        /// costs, billions of the book's dollars a year at the seed's prices (real, like `EconomyState.GDP`) - set at the boundary from the year's clearing
        /// (`EnergyReliability.SetYear`, inside `EnergyLedger.AdvanceYear`), held for the year. 0 where nothing goes unserved. It rides the save and the preview's
        /// clone. ADDITIVE (no format step, ruling A; the FurloughCountedMonth precedent): a save of an earlier build loads it as 0, which is what that build's
        /// run carried, and the next boundary writes it from the clearing. DECLARED residue: a save of an earlier build cut while its fleet left load unserved
        /// carries no loss until its next boundary.</summary>
        public float UnservedOutputLossRate;

        /// <summary>MF q26: the overlay on real output applied today (`EnergyReliability.ApplyOutput`) - the level the identity produced less what stands in
        /// `EconomyState.GDP` - removed before the next day's identity (`EnergyReliability.RestoreOutput`), as the US lapse's is. 0 where none is applied.
        /// ADDITIVE (no format step): an earlier build laid no overlay, so its saved GDP holds none and 0 is exact.</summary>
        public float UnservedOutputWedge;

        /// <summary>PS-6 US-22c (Elias's ruling J7 (iv)): the first day of the last month whose household-survey reference week a lapse covered whole - that
        /// month's unemployment counts the furloughed as BLS does (`UsLapse.NoteReferenceWeek`, `UsLapse.FurloughPoints`); MinValue where none. It rides the
        /// save and the preview's clone. ADDITIVE (no format step, ruling A): a save of an earlier build carries MinValue, which is every month's value
        /// before this build. DECLARED residue (the review's L4): a save of an earlier build cut in a month whose reference week a lapse covered whole, after
        /// that lapse ended, loads with the month unmarked and publishes it without the furloughed; while the lapse still runs, the next funding tick marks it again.</summary>
        public System.DateTime FurloughCountedMonth;

        /// <summary>PS-6 US-23 (R-US7 (a), F8): the year of the 1 October before which the out-party majority's reconciliation was last tabled
        /// (`UsMajorityReconciliation.YearOf`) - once a fiscal year; 0 where none was. It rides the save and the preview's clone. ADDITIVE (no format step,
        /// ruling A): no earlier build tabled one, so 0 is an earlier save's true state.</summary>
        public int MajorityReconciliationYear;

        /// <summary>PS-6 US-24 (R-US21, I5): THE FEDERAL DEBT LIMIT IN FORCE in the game, nominal USD - the record's at the start (`UsDebtLimit.ReadLaw`), the
        /// game's own debt at a reinstatement, raised by the game's acts after (`UsDebtLimit.Raise`). 0 while suspended or before the law is read. It rides the
        /// save (format 53) and the preview's clone.</summary>
        public double DebtLimitUsd;

        /// <summary>PS-6 US-24: the day the limit in force took effect (or the suspension began); MinValue until the day's bookkeeping reads the law at the start
        /// (`UsDebtLimit.Track`) - and in every country but the USA.</summary>
        public System.DateTime DebtLimitSince;

        /// <summary>PS-6 US-24: the last day of a suspension in force (P.L. 118-5 s401(a)'s "through January 1, 2025" for a start inside it); MinValue where the
        /// limit is not suspended.</summary>
        public System.DateTime DebtLimitSuspendedThrough;

        /// <summary>PS-6 US-24 (R-US21 (iii)): the day the debt subject to limit reached the limit - extraordinary measures from it for
        /// `UsDebtLimit.MeasuresSpanDays`, then the limit binds; MinValue where it is not reached (or a raise closed the episode).</summary>
        public System.DateTime DebtLimitReachedOn;

        /// <summary>PS-6 US-24 (R-US21 (iv)): the day whose flows are cut to revenue - armed by the day tick for the next day while the limit binds and no raise
        /// is law (`UsDebtLimit.CutToday`), MinValue otherwise.</summary>
        public System.DateTime DebtLimitCutFlowDay;

        /// <summary>PS-6 US-24: the non-interest outlays the cut has taken since the measures ran out, NOMINAL billions - the slip's figure; 0 again when a raise
        /// becomes law.</summary>
        public float DebtLimitCutBillions;

        /// <summary>MF q33 (K141-K144): the debt crisis - in the market, in default, or in a programme - and the money the retired clamps used to swallow, booked
        /// (`SovereignCrisis`). Saved with the country; the preview's clone carries its own copy.</summary>
        public SovereignCrisisState Crisis = new SovereignCrisisState();

        /// <summary>PS-6 US-24: the raises the game has made - by Congress's act or, where no Congress of the game votes, the AI-governed USA's (R-US21 (vi)).</summary>
        public int DebtLimitRaises;

        /// <summary>
        /// This country's chamber, seats per REAL party, keyed by the abbreviation that country's own
        /// election authority uses ("S", "SD", "CDU", "PiS", "UG", "REP"...) and summing to
        /// `PartySystems.ChamberSeats(Id)` - 349 Sweden, 630 Germany, 577 France, 400 Italy, 460
        /// Poland, 435 USA.
        ///
        /// W-G1 RE-KEYED THIS FIELD, from a dictionary over four generic fictional archetypes shared
        /// by all six countries, to real parties per country. That is the named save-breaking swap in
        /// `SaveGameService.CurrentSaveVersion`'s own doc comment, and it is why that constant bumped.
        ///
        /// Seeded from `PartySystems.InitialSeats(Id)` - each country's most recent real election -
        /// and CHANGED ONLY BY AN ELECTION, not by a per-turn drift off ApprovalRating. See
        /// `ParliamentSystem.UpdateSeats` for what that replaced and why it could not survive real
        /// parties.
        /// </summary>
        public Dictionary<string, int> ParliamentSeats = new Dictionary<string, int>();

        /// <summary>
        /// PS-6 US-15 (`docs/specs/USA_STAGE_PLAN.md`; the plan's risk 12: the seats, divisions, verdicts, formation, save and preview clone each held one
        /// chamber): <b>THE SECOND CHAMBER</b>, seats per party key beside <see cref="ParliamentSeats"/> - generic, so Italy's Senato (PS-7) seats into the same
        /// field. Empty where the game seats none; where the record seats one (`WorldClock.RecordSeatsSecondChamber` names the countries), it is seated on its
        /// dates in the player's country (`SimulationManager.SeatTheRecordOnItsDate`, from `WorldClock.SecondChamberOfRecord`) - an independent counted with the conference he caucuses with
        /// (R-US10 (a)), a vacant seat nobody's, so the seats sum to the members seated. Changed only where the record seats it; nothing elects it until US-16.
        /// World state, so it rides the save (format 40) and the preview's clone (`SimulationManager.ClonePreviewCountry`).
        /// </summary>
        public Dictionary<string, int> SecondChamberSeats = new Dictionary<string, int>();

        /// <summary>
        /// W-G1: every election this country has held, oldest first.
        ///
        /// ⚠ **IT LIVES ON `Country`, INSIDE `World`, AND THAT IS A DELIBERATE CHOICE ABOUT WHICH
        /// SAVE LAYER IT LANDS IN.** The obvious home was `UiDraftState`, following the
        /// `FedChairCandidates` precedent — but `SaveLoadRoundTripDiagnostic` records in its own
        /// header that "Layer 3 (UI drafts) is structurally out of reach (no OnGUI in batch)", so
        /// that precedent has itself never been machine-proven. The World graph is a layer the
        /// diagnostic already round-trips field by field across 6 countries and 2 seeds. Putting an
        /// election record where the harness can actually see it was worth more than matching a
        /// precedent the harness cannot.
        ///
        /// Before W-G1 an election left only a transient `_pendingElectionResult`, cleared the moment
        /// the player dismissed the reveal — which is why the Docket's calendar marks no past
        /// election. There was nothing to mark.
        /// </summary>
        public List<Elections.ElectionRecord> ElectionHistory = new List<Elections.ElectionRecord>();

        /// <summary>§770 (PS-5 item C4): the presidential elections the game held for this country, oldest first. Who is president on a day is read
        /// from them (`PresidentialElection.PresidentAt`) - the record's president holds until the first seats a successor. Empty where the country
        /// elects no president in two rounds.</summary>
        public List<Elections.PresidentialElection.Contest> PresidentialElections = new List<Elections.PresidentialElection.Contest>();

        /// <summary>SP-7b (Elias's ruling I14, ask 1 (a)): the candidate of record the player stands as, chosen at Poland's presidential start
        /// (`PresidentialStart.Seat`) - the name as the contest prints it; null on every other start. The game's first presidential contest after the start
        /// marks it (`PresidentialElection.PlayerCandidateFor`). Saved (format 50).</summary>
        public string PlayerCandidate;

        /// <summary>SP-7b: the start the player's game opened on, by its key (`PresidentialStart.KeyOf`) - null for the country's default start, which is
        /// every save cut before format 50 (each such save is a Sejm-start game). Saved (format 50).</summary>
        public string PlayerStart;

        /// <summary>SP-7b (I14, ask 2 (2a)): whether the player holds the presidency today - the winner of a contest in which he was the player's candidate,
        /// sworn and not yet succeeded (`PresidentialElection.PlayerHoldsOffice`). Written by the day loop at the day's start (`SimulationManager.NotePlayerPresidency`)
        /// for the readers that hold no date (the cabinet's chairs, the Finance partner); while it holds, the party's levers are the AI's (Art. 132, READING).
        /// Saved (format 50).</summary>
        public bool PlayerHoldsPresidency;

        /// <summary>PS-6 US-7: the US presidential elections the game held for this country, oldest first - each with its electors, its dates, the House's
        /// contingent choice where it was held, and its winner (`UsPresidentialElection.Contest`). The president from an oath is read from them
        /// (`UsPresidentialElection.TryInOffice`), and from the first polling day the record seats no president (`WorldClock.RecordSeatsExecutive`). Empty where
        /// the country elects no president by electors; PS-6 US-8's count adds one on each polling day the day loop counts (`UsPresidentialElection.Hold`). Saved
        /// (format 39) - the states are not, being re-derived from the national shares each contest keeps (`UsPresidentialElection.StatesOf`).</summary>
        public List<Elections.UsPresidentialElection.Contest> UsPresidentialElections = new List<Elections.UsPresidentialElection.Contest>();

        /// <summary>PS-6 US-16: the Congress elections the game held for this country, oldest first - the House by state and the Senate seats it elected
        /// (`UsCongressElection.Contest`), each seated from the 3rd of January after its polling day (`UsCongressElection.TryInOffice`); from then the record
        /// seats no House, and no Senate seat the game elected (`WorldClock.RecordSeatsChamber(Country, DateTime)`). Empty where the game elects no Congress; the
        /// day loop adds one on each presidential polling day it counts (`UsCongressElection.Hold`). Saved (format 49): each state's share, delegation and
        /// unopposed seats are the next count's base (US-11's *In play*), and the seats the game elected cannot be re-derived from the date.</summary>
        public List<Elections.UsCongressElection.Contest> UsCongressElections = new List<Elections.UsCongressElection.Contest>();

        /// <summary>PS-6 US-28 (R-US6 (b)): the player's picks of the party's nominee where the party is term-limited or (US-28b, R-US26 (a)) out of
        /// the White House, one per polling day and party (`UsTermLimit.Pick`); the count reads the pick for the player's party only - an AI party takes the default. Empty where no pick
        /// was made, which is the default: additive under ruling A, so a save from before - which could hold no pick - loads to the same game (no format step).
        /// Each person's elections, which the limit reads, are not stored here: they are derived from the record and <see cref="UsPresidentialElections"/>.</summary>
        public List<Elections.UsTermLimit.NomineePick> UsNomineePicks = new List<Elections.UsTermLimit.NomineePick>();

        /// <summary>
        /// C-D4 (§38, R-CL3): **each party's long-term political capital — what survives an election.**
        ///
        /// <para>Put HERE, beside <see cref="ElectionHistory"/>, for that field's own recorded reason:
        /// the World graph is the layer `SaveLoadRoundTripDiagnostic` round-trips field by field across
        /// six countries and two seeds, so persistence here can be PROVEN. `UiDraftState` cannot be, and
        /// matching a precedent the harness cannot see was worth less than being checkable.</para>
        ///
        /// <para>⚠ Donor and grassroots networks are **specified ABSENT** on
        /// <see cref="Elections.PartyCampaignCapital"/> rather than invented — §38 names them, nothing on
        /// disk sizes them, and a fabricated donor stock is what §0.4 forbids.</para>
        /// </summary>
        public List<Elections.PartyCampaignCapital> PartyCapital = new List<Elections.PartyCampaignCapital>();

        /// <summary>
        /// C-R1/C-R2 (R-CL1, "the player has a party"): **which of this country's real seeded parties the
        /// player leads.** Null where none has been chosen.
        ///
        /// <para>⚠ **INTERIM RULE, and it is DERIVED rather than invented.** The ruling says the player
        /// picks at country selection, and the picker is a Canvas-screen build that is BILLED, not done
        /// (see `COMPLETED.md` §119). Until it exists, selection seats **the largest party in that
        /// country's own seeded chamber** — you are the government, and which party that is comes from the
        /// real returns on disk, not from a choice this code made up. It is an interim DEFAULT, marked as
        /// one, and the first thing the picker replaces.</para>
        ///
        /// <para>World state, so it rides `SaveGame.World` beside `ElectionHistory` and `PartyCapital` —
        /// the layer `SaveLoadRoundTripDiagnostic` can prove, which `UiDraftState` cannot.</para>
        /// </summary>
        public string PlayerPartyAbbrev;

        /// <summary>PS-3a (§628): WHO GOVERNS, stored - the government of record at the start, the formation's after the game's own election; the player's role is read from it. Null before a country's world seats one.</summary>
        public PoliSim.Elections.GovernmentRecord Government;

        /// <summary>
        /// C-R3: **the player's PARTY approval, a NEW ADDITIVE STOCK.**
        ///
        /// <para>⚠ The design constraint that keeps this SAFE rather than BASELINE, and it is the whole
        /// point of the row: **personal approval keeps `EconomyState.ApprovalRating`, its name and every
        /// one of its consumers, untouched.** Party approval is added beside it and nothing reads it into
        /// the simulation, so the no-policy trajectory is predicted byte-identical — and the dump is run
        /// to prove that rather than assert it.</para>
        ///
        /// <para>⚠ **Nothing moves it yet, for C-D4's reason and stated the same way.** A rule coupling
        /// party approval to events would need a coefficient nothing on disk sources, and inventing one to
        /// make a stock look alive is what the standing rules forbid. It opens at the personal rating and
        /// PERSISTS - which is itself the change, since before this there was no such stock at all.</para>
        /// </summary>
        public float PartyApprovalRating = 50f;

        /// <summary>A-LOOP q15 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §3 (2), Elias's ruling J1 Q1 (a)): <b>approval's economy stock</b> - the part of
        /// approval its three economy terms built (the growth gap and the unemployment and inflation misery terms, the approval ledger's
        /// `ApprovalAttribution.GrowthEffect`, `ApprovalAttribution.MiseryUnemployment` and `ApprovalAttribution.MiseryInflation`), carried under approval's
        /// own reversion rate, with its share of the clamp where approval + T (the base its reversion reads) met a bound (`ApprovalVote.AdvanceEconomyStockDay`, each day of approval's daily step - MF-13, K191). The vote reads approval net of it
        /// (`ApprovalVote.NetApproval`), because the economy already votes through the economic vote. Read by the vote alone, never by the simulation.
        /// Saved (format 45); carried on the preview's clone.</summary>
        public float ApprovalEconomyStock;

        /// <summary>A-LOOP q15: <b>net approval as each government found it</b> - one mark the first day the country's daily step sees a stored
        /// government (`ApprovalVote.NoteGovernment`), in the order seen. The approval term's reference is the first mark on or after the day the
        /// government took office (`EconomicVote.TookOffice`), so a government of record that took office before the run reads the start's (the seed's)
        /// and one formed in play reads its own first day. Saved (format 45); copied on the preview's clone.</summary>
        public List<ApprovalMark> VoteApprovalMarks = new List<ApprovalMark>();

        /// <summary>A-LOOP q13 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §4 (2)): <b>the fiscal record the vote reads</b> - the latest closed fiscal years
        /// as the player reads them with nominal GDP at each close, and the open year's flow to date (`FiscalVote.NoteDay`, `FiscalVote.CloseYear`);
        /// Brender &amp; Drazen's two balance variables are formed on it (`FiscalVote.Judge`). Read by the vote alone, never by the simulation. Null
        /// until the first daily step. Saved (format 46); copied on the preview's clone.</summary>
        public FiscalVoteBook FiscalVoteRecord;

        /// <summary>
        /// Rolling numeric history of this country's key tracked stats, for UI graphs - see
        /// StatHistory.cs. Appended once per turn by SimulationManager.AdvanceTurn, kept entirely
        /// separate from the existing turn-activity text log (shown under Statistics -> International since 2026-08-01).
        /// </summary>
        public StatHistory History = new StatHistory();

        /// <summary>
        /// Bounded history of resolved parliamentary divisions - see DivisionRecord.cs. Written by
        /// ParliamentSystem.RecordDivision at the moment each bill resolves, read by the UI only.
        /// **Nothing in the simulation may read it back** - DivisionLog's own doc comment explains why
        /// that constraint is the whole point rather than a style preference. Distinct from History
        /// above (numeric per-turn series) and from the turn-activity text log (UI-side prose).
        /// </summary>
        public DivisionLog Divisions = new DivisionLog();

        public Country() { }

        public Country(
            CountryId id, string name, EconomyState state, CurrencyZone currencyZone, float baseTariffRate = 0f,
            float naturalUnemploymentRate = 4f, float potentialGrowthRate = 2f, float governmentSpendingRate = 20f,
            float benefitRatePerUnemployed = 0.15f)
        {
            Id = id;
            Name = name;
            State = state;
            CurrencyZone = currencyZone;
            BaseTariffRate = baseTariffRate;
            NaturalUnemploymentRate = naturalUnemploymentRate;
            NaturalUnemploymentRateBase = naturalUnemploymentRate;
            PotentialGrowthRate = potentialGrowthRate;
            GovernmentSpendingRate = governmentSpendingRate;
            BenefitRatePerUnemployed = benefitRatePerUnemployed;
        }
    }
}
