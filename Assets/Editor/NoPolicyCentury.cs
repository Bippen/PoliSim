using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **One no-policy century per player, run once per process and read by every diagnostic that needs it (2026-09-17,
    /// `COMPLETED.md` §525).**
    ///
    /// <para><b>Why it exists.</b> `HealthTrendDiagnostic` and `InfrastructureReadoutDiagnostic` each advanced the SAME six
    /// worlds - seed 777, the default world, each country in turn as the player, no decision on any turn, a hundred years -
    /// and each ran Sweden's first twenty years again on its own, so most of each diagnostic's simulated years were a copy
    /// of the other's (the cost measured in §525). The loop here is the one both ran, verbatim, and it records every year's
    /// readouts both of them read, so the second reader gets the first reader's run instead of a copy of it.</para>
    ///
    /// <para>⚠ <b>The shared run is checked against an independent one.</b> `InfrastructureReadoutDiagnostic` still advances
    /// Sweden's untouched twenty years by itself and fails when that differs by a bit from the shared run's year twenty - a
    /// defect in this cache, or state leaking between the diagnostics that ran in between, cannot pass silently.</para>
    /// </summary>
    public static class NoPolicyCentury
    {
        /// <summary>The seed both diagnostics seeded with.</summary>
        public const int Seed = 777;

        /// <summary>The horizon the longer reader asks for; a shorter reader reads a prefix of it.</summary>
        public const int Years = 100;

        /// <summary>One year's readouts of the player's country, taken after that year's turn.</summary>
        public sealed class Year
        {
            /// <summary>MF q32: the debt ratio after the year's turn, per cent of GDP, the crisis phase then and the crises entered so far
            /// (`DebtPathHoldDiagnostic`).</summary>
            public float DebtToGdpRatio;
            public CrisisPhase CrisisPhase;
            public int Crises;
            public float RoadQuality;
            public float TreatableMortality;
            public float TrendIndex;
            public float DeathRate;
            public float Population;
            public float NetMigrationRate;
            public float LifeExpectancy;
            /// <summary>K58: approval after the year's turn - the pressure floor's reading for an idle governing year (`AverageMinisterLeakMeasure`).</summary>
            public float ApprovalRating;
            /// <summary>MF q29: the fractional calendar year at the record (MortalitySchedule.YearOf the date), and the year's span - its midpoint, its real GDP
            /// growth and its potential growth, per cent, against the record before it (the opening state for year one).</summary>
            public double CalendarYear;
            public double SpanMidYear;
            public double RealGrowth;
            public double PotentialGrowth;
            /// <summary>K210: the growth of potential's productivity index over the span, per cent (Country.PotentialProductivityIndex - the trend the
            /// ledger compounded, its pulled anchor and its adjustments together); the labour part is the potential growth net of it.</summary>
            public double ProductivityGrowth;
            /// <summary>K210 (the review's L5): the productivity ledger as the year's boundary left it, read at the boundary itself (SimulationManager's
            /// "spending" ledger point, straight after MacroSystem.ApplySectorGrowthEffect): the trend it wrote, the three adjustments it read
            /// (infrastructure, sector, education) and the boundary's day - so the live loop's anchor is checked against ProductivityPull.Anchor.
            /// LedgerDay is default where the boundary never reached that point.</summary>
            public float LedgerTrend;
            public float LedgerInfrastructure;
            public float LedgerSector;
            public float LedgerEducation;
            public System.DateTime LedgerDay;
            /// <summary>K210 (review 2's N-L4): the clock's own day at that ledger point - held equal to LedgerDay, so the date the boundary passes is the day it is.</summary>
            public System.DateTime LedgerClockDay;
            /// <summary>MF q29: the output gap at the record, GDP / potential - 1, and the mortality multiplier.</summary>
            public double OutputGap;
            public float MortalityMultiplier;
            /// <summary>fisc2 (2026-10-29): the period life expectancy the schedule gives at the record WITHOUT the health family's treatable excess - the multiplier
            /// and the projection's improvement alone (`MortalitySchedule.LifeExpectancyAtBirth` with a zero excess); NaN without a schedule. `LongRunAnchorsDiagnostic` (d3)
            /// reads it to tell the treatable term's part of a gain from the rest.</summary>
            public double LifeExpectancyWithoutTreatable = double.NaN;
            /// <summary>MF q28: the two waits after the year's turn (-1 where absent) and the real health spending per age-cost unit against its seed that they
            /// read (`HospitalWaitsDiagnostic`).</summary>
            public float WaitCataractDays;
            public float WaitKneeDays;
            public float SpendPerAgeCostRatio;
            /// <summary>K218: the closed year's fiscal report against the nominal GDP at the record, per cent - the primary balance (the year's balance plus its
            /// interest, positive a surplus; a fund contribution and a one-off ride inside it) and the interest - and the nominal GDP's growth over the
            /// span, per cent (`DebtPathHoldDiagnostic` decomposes the debt change with them).</summary>
            public float PrimaryBalancePercent;
            public float InterestPercent;
            public double NominalGrowth;
            /// <summary>K275: the player's income tax after the year's turn (`IncomeTaxCreepCapDiagnostic`) - the yield ratio the revenue reads, the ratio form
            /// without the creep's cap, the statute's creep, its bound and the lever's seeded rate; 1 (0 for the seed rate) where the country has no income line.</summary>
            public double IncomeTaxRatio = 1.0;
            public double IncomeTaxRatioUncapped = 1.0;
            public double IncomeTaxCreep = 1.0;
            public double IncomeTaxCreepBound = 1.0;
            public float IncomeTaxSeedRate;
            /// <summary>K275 (review M1): the seed yield, and the flat-equivalent rate the behavioural terms read (`TaxSchedule.EffectiveRate`) capped and uncapped, %.</summary>
            public float IncomeTaxSeedAer;
            public float IncomeTaxEffective, IncomeTaxEffectiveUncapped;
            /// <summary>K275: the player's own moves read at this year's position, at <see cref="IncomeTaxProbeYears"/> only (NaN elsewhere) - the ratio with
            /// the lever one point up and one down, the top adjustable band one point up on its own dial and the same band at 0, each capped (the revenue's
            /// reading) and uncapped (the ratio form's). Read on clones of the line, so the run's own line is never touched.</summary>
            public double TaxRiseCapped = double.NaN, TaxRiseUncapped = double.NaN, TaxCutCapped = double.NaN, TaxCutUncapped = double.NaN;
            public double TaxBandCapped = double.NaN, TaxBandUncapped = double.NaN, TaxTopZeroCapped = double.NaN, TaxTopZeroUncapped = double.NaN;
            /// <summary>K275 (review M1): the same three moves read on the flat-equivalent rate, capped and uncapped (NaN outside the probe years).</summary>
            public float EffRiseCapped = float.NaN, EffRiseUncapped = float.NaN, EffCutCapped = float.NaN, EffCutUncapped = float.NaN, EffBandCapped = float.NaN, EffBandUncapped = float.NaN;
            /// <summary>K275: the AI books of the same world after the year's turn - how many of them the creep's cap bound, and the largest creep among them.</summary>
            public int AiCreepBinds;
            public double AiCreepMax;
            /// <summary>K276: the labour force after the year's turn (PotentialOutput.LabourForce - the 15+ population at the state's rate, the pyramid's
            /// units), the labour force aged 20-64 at the year's projected rates by age (Σ band × ParticipationRateTable.RatesAt over 20-64, no lever),
            /// and the state's participation rate - read by `ParticipationCohortDiagnostic`'s labour-force hold.</summary>
            public double LabourForce;
            public double LabourForce2064;
            public float Participation;
            /// <summary>K276 review 1 (M2): the model's own labour force aged 20-74 on the law the source projects - the state's labour force scaled to the
            /// 20-74 perimeter: (the state's rate / the structural rate with the pension age's response) × Σ band × the year's projected rate over 20-24 to
            /// 70-74 (<see cref="LabourForce2074At"/>). Read by `ParticipationCohortDiagnostic`'s (c) for the EU five.</summary>
            public double LabourForce2074;
            /// <summary>K276 review 1: the structural rate (ParticipationRateTable.StructuralRate, with the pension age's response) and the 15+ population, the
            /// pyramid's units - the attribution beside `ParticipationCohortDiagnostic`'s (c): the labour force is the population × the state's rate, and the
            /// state's rate is the anchor × (the state's rate / the anchor).</summary>
            public float Structural;
            public double Population15;
            /// <summary>K299 (K290's follow-up): the pyramid after the year's turn, a copy (millions by band), and the substrate year it stands at
            /// (CohortDemographics.SubstrateYear at the turn count after the boundary: the year the boundary's commit stepped it to) - read by
            /// `ParticipationCohortDiagnostic`'s (d), the USA's population against CBO's, by the pyramid's own year.</summary>
            public float[] Pyramid;
            public int SubstrateYear;
            /// <summary>K276 review 1: what holds the state's rate off its anchor, points of the 15+ rate - the unemployment rate less the effective natural rate
            /// (the discouraged-worker term reads it), the labour tax's, the health's and the education's participation terms - the attribution beside (c).</summary>
            public float UnemploymentGap, LabourTaxTerm, HealthTerm, EducationTerm;
            /// <summary>pbr (K314): the pension line's share of nominal GDP after the year's turn, per cent; the statutory headcount (PensionPayment.PensionersMillions at
            /// the age in force) and the 65+ population, the pyramid's units; the country's calendar year (Country.CalendarYear, the year the boundary indexed into) -
            /// read by `PensionBenefitRatioDiagnostic`'s (A1) and (A2). Read only.</summary>
            public double PensionSharePercent = double.NaN, StatutoryPensioners = double.NaN, Elderly65 = double.NaN;
            public int CountryCalendarYear;
        }

        /// <summary>pbr: the four pension readings of <see cref="Year"/> from a country as it stands. Read only.</summary>
        internal static void RecordPension(Country c, Year y)
        {
            double pension = 0;
            foreach (SpendingLine l in c.SpendingLines) { if (l.Category == SpendingCategory.SocialSecurity) { pension += l.Amount; } }
            y.PensionSharePercent = 100.0 * pension / System.Math.Max(1e-6, (double)c.State.NominalGdp);
            y.StatutoryPensioners = c.Cohorts != null && PensionAgeStatute.Has(c.Id) ? PensionPayment.PensionersMillions(c, PensionAgeStatute.AgeInForce(c, c.CalendarYear)) : double.NaN;
            y.Elderly65 = c.Cohorts != null ? c.Cohorts.InAgeRange(65, 999) : double.NaN;
            y.CountryCalendarYear = c.CalendarYear;
        }

        /// <summary>K275: the years the player's own moves are probed at - the first (the creep inside its bound everywhere) and the two the diagnostic holds.</summary>
        public static readonly int[] IncomeTaxProbeYears = { 1, 40, 100 };   // [AUTHORED] CONVENTION: the first year and the two the diagnostic holds

        /// <summary>K275: the player's income-tax readings into a year's record, and the AI books' creep - every read on the memoised yield, the probes on clones.</summary>
        private static void RecordIncomeTax(World world, Country player, int year, Year y)
        {
            foreach (Country k in world.Countries)
            {
                TaxLine line = IncomeLine(k);
                if (line == null || !TaxSchedule.Responds(k.Id)) { continue; }
                if (!ReferenceEquals(k, player))
                {
                    if (TaxSchedule.CreepCapped(k.Id)) { y.AiCreepMax = System.Math.Max(y.AiCreepMax, TaxSchedule.StatuteCreep(k, line)); if (TaxSchedule.CreepCapFactor(k, line) < 1.0) { y.AiCreepBinds++; } }
                    continue;
                }
                y.IncomeTaxRatio = TaxSchedule.YieldRatio(k, line, line.Rate);
                y.IncomeTaxRatioUncapped = TaxSchedule.YieldRatioUncapped(k, line, line.Rate);
                y.IncomeTaxCreep = TaxSchedule.StatuteCreep(k, line);
                y.IncomeTaxCreepBound = TaxSchedule.CreepBound(k, line);
                y.IncomeTaxSeedRate = TaxSchedule.RateSeedOf(line);
                y.IncomeTaxSeedAer = k.IncomeTaxSeedAer;
                y.IncomeTaxEffective = TaxSchedule.EffectiveRate(k, line, line.Rate); y.IncomeTaxEffectiveUncapped = TaxSchedule.EffectiveRateUncapped(k, line, line.Rate);
                if (System.Array.IndexOf(IncomeTaxProbeYears, year) < 0) { continue; }
                y.TaxRiseCapped = TaxSchedule.YieldRatio(k, line, line.Rate + 1f); y.TaxRiseUncapped = TaxSchedule.YieldRatioUncapped(k, line, line.Rate + 1f);
                y.TaxCutCapped = TaxSchedule.YieldRatio(k, line, line.Rate - 1f); y.TaxCutUncapped = TaxSchedule.YieldRatioUncapped(k, line, line.Rate - 1f);
                y.EffRiseCapped = TaxSchedule.EffectiveRate(k, line, line.Rate + 1f); y.EffRiseUncapped = TaxSchedule.EffectiveRateUncapped(k, line, line.Rate + 1f);
                y.EffCutCapped = TaxSchedule.EffectiveRate(k, line, line.Rate - 1f); y.EffCutUncapped = TaxSchedule.EffectiveRateUncapped(k, line, line.Rate - 1f);
                List<TaxSchedule.SubRow> rows = TaxSchedule.SubRows(TaxSchedule.Of(k.Id));
                int top = -1;
                for (int i = 0; i < rows.Count; i++) { if (rows[i].Adjustable && (rows[i].Kind == TaxSubRowKind.Band || rows[i].Kind == TaxSubRowKind.Ramp)) { top = i; } }
                if (top >= 0)
                {
                    TaxLine band = line.Clone(); band.BracketRates = OneRow(rows.Count, top, (float)rows[top].StatuteRate + 1f, line.BracketRates);
                    y.TaxBandCapped = TaxSchedule.YieldRatio(k, band, band.Rate); y.TaxBandUncapped = TaxSchedule.YieldRatioUncapped(k, band, band.Rate);
                    y.EffBandCapped = TaxSchedule.EffectiveRate(k, band, band.Rate); y.EffBandUncapped = TaxSchedule.EffectiveRateUncapped(k, band, band.Rate);
                    TaxLine zero = line.Clone(); zero.BracketRates = OneRow(rows.Count, top, 0f, line.BracketRates);
                    y.TaxTopZeroCapped = TaxSchedule.YieldRatio(k, zero, zero.Rate); y.TaxTopZeroUncapped = TaxSchedule.YieldRatioUncapped(k, zero, zero.Rate);
                }
            }
        }

        private static float[] OneRow(int count, int row, float rate, float[] standing)
        {
            var rates = new float[count];
            for (int i = 0; i < count; i++) { rates[i] = standing != null && i < standing.Length ? standing[i] : -1f; }
            rates[row] = rate;
            return rates;
        }

        private static TaxLine IncomeLine(Country c)
        {
            if (c.TaxLines == null) { return null; }
            foreach (TaxLine l in c.TaxLines) { if (l.Type == TaxType.IncomeTax && l.IsImplemented) { return l; } }
            return null;
        }

        private static readonly Dictionary<CountryId, Year[]> Runs = new Dictionary<CountryId, Year[]>();
        private static readonly Dictionary<CountryId, Year> Openings = new Dictionary<CountryId, Year>();
        private static readonly Dictionary<CountryId, Year[]> UnchangedRuns = new Dictionary<CountryId, Year[]>();
        private static readonly Dictionary<CountryId, Year> UnchangedOpenings = new Dictionary<CountryId, Year>();

        /// <summary>pbr: the no-policy-change run's opening - its pension readings before the first day (<see cref="RecordPension"/>), the base of (A1)'s index.</summary>
        public static Year UnchangedOpening(CountryId player, int years)
        {
            Unchanged(player, years);
            return UnchangedOpenings[player];
        }

        /// <summary>MF q29: the player's country as the century opened, before its first day - the base of year one's growth and of the run's life-expectancy gain.</summary>
        public static Year Opening(CountryId player)
        {
            For(player);
            return Openings[player];
        }

        /// <summary>The century with <paramref name="player"/> as the player - element <c>y - 1</c> is year <c>y</c> - run on the first ask and handed out after.</summary>
        public static IReadOnlyList<Year> For(CountryId player)
        {
            if (!Runs.TryGetValue(player, out Year[] run))
            {
                run = Advance(player);
                Runs[player] = run;
            }

            return run;
        }

        /// <summary>
        /// K218 (2026-10-28): THE NO-POLICY-CHANGE RUN - the same loop, seed and start as <see cref="For"/>, with <c>SimulationManager.NoPolicyChangeBook</c> set:
        /// the player's country runs the AI's indexation with no ministry, no Finance partner and no fiscal reaction - the premise CBO's extended baseline (current law) and the EC DSM (the structural primary balance unchanged, the cost of
        /// ageing on top) project under. Read by `DebtPathHoldDiagnostic` alone; the shared century, which every other reader takes, is the player's own
        /// idle book and is not changed by it. Run on the first ask for at least <paramref name="years"/> years and handed out after (a longer ask runs again).
        /// </summary>
        public static IReadOnlyList<Year> Unchanged(CountryId player, int years)
        {
            if (!UnchangedRuns.TryGetValue(player, out Year[] run) || run.Length < years)
            {
                run = Advance(player, years, noPolicyChange: true);
                UnchangedRuns[player] = run;
            }

            return run;
        }

        /// <summary>K276: the labour force aged 20-64 at the country's year's projected rates by age - Σ band × rate over the bands 20-24 to 60-64, the
        /// pyramid's units; NaN without a pyramid or a table. Read only.</summary>
        public static double LabourForce2064At(Country c)
        {
            float[] rates = ParticipationRateTable.RatesAt(c.Id, c.CalendarYear);
            if (rates == null || c.Cohorts == null) { return double.NaN; }
            double sum = 0;
            for (int b = 4; b <= 12; b++) { sum += (double)c.Cohorts.Counts[b] * rates[b]; }
            return sum;
        }

        /// <summary>K276 review 1 (M2): the model's own labour force aged 20-74, the pyramid's units, on the law the source projects - the state's rate over the
        /// structural rate (with the pension age's response: the levers', the cycle's and the reversion's departure from the anchor) times the anchor's own
        /// active people aged 20-74 at the year's projected rates (without the response: the projection's law - past an indexed statute's horizon the
        /// response is the carried statute's shortfall against the age the projection assumed). NaN without a pyramid, a table or a structural rate. Read only.</summary>
        public static double LabourForce2074At(Country c)
        {
            float[] rates = ParticipationRateTable.RatesAt(c.Id, c.CalendarYear);
            float structural = ParticipationRateTable.StructuralRate(c);
            if (rates == null || c.Cohorts == null || float.IsNaN(structural) || structural <= 0f) { return double.NaN; }
            double sum = 0;
            for (int b = 4; b <= 14; b++) { sum += (double)c.Cohorts.Counts[b] * rates[b]; }
            return sum * c.State.LaborForceParticipationRate / structural;
        }

        /// <summary>fisc2: <see cref="Year.LifeExpectancyWithoutTreatable"/> - the schedule's life expectancy at <paramref name="year"/> with the state's multiplier and no treatable excess. Read only.</summary>
        public static double WithoutTreatable(Country c, double year)
        {
            if (!MortalitySchedule.Has(c.Id)) { return double.NaN; }
            return MortalitySchedule.LifeExpectancyAtBirth(MortalitySchedule.TrendSchedule(c.Id, year), MortalitySchedule.Calibration(c.Id, c.BaselineLifeExpectancy) * c.State.MortalityMultiplier, 0.0);
        }

        /// <summary>The loop the two diagnostics ran for an untouched country, recording every year - the shared century, the player's own book for <see cref="Years"/>.</summary>
        private static Year[] Advance(CountryId player) => Advance(player, Years, noPolicyChange: false);

        /// <summary>K218: the same loop for <paramref name="horizon"/> years, in the no-policy-change book where <paramref name="noPolicyChange"/> is set.</summary>
        private static Year[] Advance(CountryId player, int horizon, bool noPolicyChange)
        {
            SimulationRandom.Seed(Seed);
            using System.IDisposable epoch = SimulationManager.EpochScope();   // PS-1 (§618): the century opens on the player's own start, as the game does, and the epoch is put back after
            PoliSim.Elections.WorldClock.ApplyStart(player);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("NOPOLICYCENTURY");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = player;
                sim.NoPolicyChangeBook = noPolicyChange;   // K218: false for the shared century (the player's own idle book)
                Country c = world.GetCountry(player);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var years = new Year[horizon];
                // K210 (the review's L5): read the ledger at the boundary, for this world's player only - the hook is static, read-only and removed below
                float ledgerTrend = 0f, ledgerInfrastructure = 0f, ledgerSector = 0f, ledgerEducation = 0f;
                System.DateTime ledgerDay = default, ledgerClockDay = default;
                System.Reflection.MethodInfo sectorAdjustment = typeof(MacroSystem).GetMethod("GetSectorGrowthAdjustment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                System.Reflection.PropertyInfo comingBoundary = typeof(SimulationManager).GetProperty("ComingBoundaryDate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                System.Action<Country, string> ledger = (k, point) =>
                {
                    if (!ReferenceEquals(k, c) || point != "spending") { return; }
                    ledgerTrend = k.ProductivityTrendGrowthRate;
                    ledgerInfrastructure = MacroSystem.ApplyInfrastructureGrowthEffect(k);
                    ledgerSector = sectorAdjustment != null ? (float)sectorAdjustment.Invoke(null, new object[] { k }) : float.NaN;
                    ledgerEducation = EducationFamily.ProductivityTrendTerm(k);
                    ledgerDay = comingBoundary != null ? (System.DateTime)comingBoundary.GetValue(sim) : default;   // the date the boundary passed the ledger
                    ledgerClockDay = sim.CurrentDate;
                };
                SimulationManager.BoundaryLedger += ledger;
                try
                {
                    if (!noPolicyChange) Openings[player] = new Year { LifeExpectancy = c.State.LifeExpectancy, CalendarYear = MortalitySchedule.YearOf(sim.CurrentDate), OutputGap = c.State.GDP / System.Math.Max(1e-6, (double)c.State.PotentialGDP) - 1.0, MortalityMultiplier = c.State.MortalityMultiplier, LifeExpectancyWithoutTreatable = WithoutTreatable(c, MortalitySchedule.YearOf(sim.CurrentDate)) };
                    if (noPolicyChange) { var opening = new Year(); RecordPension(c, opening); UnchangedOpenings[player] = opening; }   // pbr: (A1)'s base
                    double nominalBefore = c.State.NominalGdp;   // K218
                    double gdpBefore = c.State.GDP, potentialBefore = c.State.PotentialGDP, productivityBefore = c.PotentialProductivityIndex, yearBefore = MortalitySchedule.YearOf(sim.CurrentDate);
                    for (int year = 1; year <= horizon; year++)
                    {
                        for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                        PolicyDecision d = PolicyDecision.None();
                        decisions[player] = d;
                        ledgerDay = default;
                        sim.AdvanceTurn(decisions);
                        FiscalTurnReport report = sim.GetLastFiscalReport(player);   // K218: the closed year's book, read only
                        double nominal = System.Math.Max(1e-6, (double)c.State.NominalGdp);
                        years[year - 1] = new Year
                        {
                            DebtToGdpRatio = c.State.DebtToGdpRatio,
                            CrisisPhase = c.Crisis != null ? c.Crisis.Phase : CrisisPhase.None,
                            Crises = c.Crisis != null ? c.Crisis.Crises : 0,
                            RoadQuality = c.State.RoadQuality,
                            TreatableMortality = c.State.TreatableMortality,
                            TrendIndex = c.Health.TrendIndex,
                            DeathRate = c.State.DeathRate,
                            Population = c.State.Population,
                            NetMigrationRate = c.State.NetMigrationRate,
                            LifeExpectancy = c.State.LifeExpectancy,
                            LifeExpectancyWithoutTreatable = WithoutTreatable(c, MortalitySchedule.YearOf(sim.CurrentDate)),
                            ApprovalRating = c.State.ApprovalRating,
                            CalendarYear = MortalitySchedule.YearOf(sim.CurrentDate),
                            SpanMidYear = 0.5 * (yearBefore + MortalitySchedule.YearOf(sim.CurrentDate)),
                            RealGrowth = (c.State.GDP / System.Math.Max(1e-6, gdpBefore) - 1.0) * 100.0,
                            PotentialGrowth = (c.State.PotentialGDP / System.Math.Max(1e-6, potentialBefore) - 1.0) * 100.0,
                            ProductivityGrowth = (c.PotentialProductivityIndex / System.Math.Max(1e-12, productivityBefore) - 1.0) * 100.0,
                            OutputGap = c.State.GDP / System.Math.Max(1e-6, (double)c.State.PotentialGDP) - 1.0,
                            MortalityMultiplier = c.State.MortalityMultiplier,
                            WaitCataractDays = c.State.WaitCataractDays,
                            WaitKneeDays = c.State.WaitKneeDays,
                            SpendPerAgeCostRatio = c.Health != null && c.Health.SpendPerAgeCostSeed > 0f ? HealthFamily.SpendPerAgeCost(c) / c.Health.SpendPerAgeCostSeed : 1f,
                            LedgerTrend = ledgerTrend,
                            LedgerInfrastructure = ledgerInfrastructure,
                            LedgerSector = ledgerSector,
                            LedgerEducation = ledgerEducation,
                            LedgerDay = ledgerDay,
                        LedgerClockDay = ledgerClockDay,
                            PrimaryBalancePercent = report != null ? (float)(100.0 * ((double)report.BudgetBalance + report.InterestOnDebt) / nominal) : float.NaN,
                            InterestPercent = report != null ? (float)(100.0 * report.InterestOnDebt / nominal) : float.NaN,
                            NominalGrowth = (nominal / System.Math.Max(1e-6, nominalBefore) - 1.0) * 100.0,
                            LabourForce = PotentialOutput.LabourForce(c),
                            LabourForce2064 = LabourForce2064At(c),
                            LabourForce2074 = LabourForce2074At(c),
                            Structural = ParticipationRateTable.StructuralRate(c),
                            Population15 = PotentialOutput.Population15Plus(c),
                            Pyramid = c.Cohorts != null ? (float[])c.Cohorts.Counts.Clone() : null,   // K299
                            SubstrateYear = CohortDemographics.SubstrateYear(sim.CurrentTurn),
                            UnemploymentGap = c.State.Unemployment - c.EffectiveNaturalUnemploymentRate,
                            LabourTaxTerm = MacroSystem.LaborTaxParticipationTerm(c),
                            HealthTerm = HealthFamily.ParticipationTerm(c),
                            EducationTerm = EducationFamily.ParticipationTerm(c),
                            Participation = c.State.LaborForceParticipationRate,
                        };
                        RecordIncomeTax(world, c, year, years[year - 1]);   // K275
                        RecordPension(c, years[year - 1]);   // pbr: read only
                        nominalBefore = nominal;
                        gdpBefore = c.State.GDP; potentialBefore = c.State.PotentialGDP; productivityBefore = c.PotentialProductivityIndex; yearBefore = MortalitySchedule.YearOf(sim.CurrentDate);
                    }
                }
                finally { SimulationManager.BoundaryLedger -= ledger; }

                return years;
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
