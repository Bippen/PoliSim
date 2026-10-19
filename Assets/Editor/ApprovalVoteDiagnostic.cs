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
using Object = UnityEngine.Object;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// A-LOOP q15 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §3 (4)): <b>the approval term's pass criteria that an Editor harness can prove</b>, on Sweden
    /// (seed 777, Sweden the player's country, every boundary resolved with no policy). Every planted approval move is recorded on the approval ledger as
    /// the game's callers record theirs - an unrecorded harness write fails the ATTRIB audit at the next boundary.
    /// <list type="number">
    /// <item><b>The reference at the start</b>: no mark before the world's first day, so no approval term and the record is the economic vote alone;
    /// after it, one mark for the government of record, at the seed's net approval, which the term reads as A_0.</item>
    /// <item><b>The term's size per point</b>: a planted move of net approval gives shift_gov = b × ΔA ⁄ 100 for the government side, split by the
    /// responsibility vector (each party b × ΔA ⁄ 100 × its economic vote over the government's), summing to shift_gov; the record is the economy's
    /// shift plus the approval term's per party, and its economy part is the economic vote unchanged. The plan's (b) arithmetic printed: at 1.5 % the
    /// government side's term.</item>
    /// <item><b>(c) A_E = A_0 is exactly zero</b>: every approval part is 0, and the record and the shares it shifts are the economy's alone to the bit.</item>
    /// <item><b>(d) Bounds</b>: net approval held in [0, 100] whatever the stock, |shift_gov| ≤ b, shifted shares sum to one, non-negative and finite.</item>
    /// <item><b>A government formed in play</b>: no term on its formation day; the next day's mark is its reference, not the start's.</item>
    /// <item><b>The net approval's composition</b>, played across three boundaries (review 879's M3): the economy stock is zero before the first; at each,
    /// the stock is its own reversion of the previous (exercised from the second) plus the closed period's growth, unemployment and inflation terms as the
    /// ledger recorded them plus its clamp share (zero unless the base, approval + T, met a bound), and the rest of approval moves by everything else - the events,
    /// the tax term's fading, the formula's other terms with the reversion of the rest, and the rest of the clamp - every country; net approval is
    /// approval less the stock, clamped; the ledger's identity holds; the composition printed per country. And the stock is a part of approval + T, not bookkeeping (review 879b's L-3): every country's reversion term implies one neutral from the base the harness observed, at the stock's own rate.</item>
    /// <item><b>The save and the clone</b>: a save cut after the boundary carries the stock and the marks (format 45); the preview's clone carries both,
    /// the marks as its own list.</item>
    /// <item><b>The clamp's share</b> (review 879's M1), by hand on a bare country: at the floor a worse economy never raises net approval, at the
    /// ceiling a better one never lowers it; a tax impulse, outside the base (approval + T, q37's contract), leaves the stock's share as it is; inside [0, 100] no share. With a tax term standing against the bound (review 879b's M-2), approval's own clamp is shared, and the same two hold.</item>
    /// </list>
    /// Prints `APPROVAL VOTE: PASS` and exits 0 when every check holds; each failure is an `APPROVAL VOTE:` error naming its check.
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.ApprovalVoteDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class ApprovalVoteDiagnostic
    {
        private const double Exact = 1e-12;
        private const float PlantedDrop = 10f;
        private const int Boundaries = 3;

        private static int _failures;
        private static StringBuilder _sb;

        public static void Run()
        {
            CheckExit.ArmLogFold();   // the worlds cross a boundary: an ATTRIB during the run fails it
            _failures = 0;
            _sb = new StringBuilder("APPROVAL VOTE: A-LOOP q15 - approval net of its economy terms reaches the vote (Sweden, seed 777)\n");
            _sb.Append(F("    b = {0} per point (USA sourced, every other country transferred: {1}); the economy stock's persistence a boundary {2:R}\n",
                ApprovalVote.PresidentialVoteCoefficient,
                string.Join(", ", ((CountryId[])Enum.GetValues(typeof(CountryId))).Select(id => id + (ApprovalVote.IsTransferred(id) ? " transferred" : " sourced"))),
                ApprovalVote.EconomyStockPersistence));
            try
            {
                ReferenceAtStart();
                SizePerPoint();
                ExactlyZero();
                Bounds();
                FormedInPlay();
                Composition();
                FloorAndCeiling();
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "APPROVAL VOTE: PASS - the reference, the term's size and split, exactly zero at no change, the bounds, a government formed in play, the net approval's composition over three boundaries, the save and the clone, the clamp's share at the floor and the ceiling."
                : F("APPROVAL VOTE: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        // ============================================================ (1) the reference at the start
        private static void ReferenceAtStart()
        {
            _sb.Append("    1. the reference at the start\n");
            (SimulationManager sim, World world, GameObject go) = Open();
            try
            {
                Country se = world.GetCountry(CountryId.Sweden);
                float seed = se.State.ApprovalRating;
                EconomicVote.RecordParts before = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                Check(se.VoteApprovalMarks.Count == 0 && !before.ApprovalReading.HasReference && before.Approval.Count == 0 && SameBits(before.Total, before.Economy),
                    F("(1a) before the first day no mark ({0}) and no approval term: the record is the economic vote alone - {1}", se.VoteApprovalMarks.Count, ApprovalVote.Describe(before.ApprovalReading)));
                Days(sim, 1);
                ApprovalMark mark = se.VoteApprovalMarks.Count > 0 ? se.VoteApprovalMarks[0] : null;
                DateTime? took = EconomicVote.TookOffice(se);
                Check(se.Government != null && se.VoteApprovalMarks.Count == 1 && mark.FormedOn == se.Government.FormedOn && mark.Net == (double)seed && se.ApprovalEconomyStock == 0f,
                    F("(1b) after the first day one mark for the government formed {0:yyyy-MM-dd}: net {1:R} on {2:yyyy-MM-dd}, the seed's approval {3:R} with no economy stock", se.Government?.FormedOn, mark?.Net, mark?.On, seed));
                bool found = took.HasValue && ApprovalVote.TryReference(se, took.Value, out ApprovalMark reference) && ReferenceEquals(reference, mark);
                Check(found, F("(1c) the government took office {0:yyyy-MM-dd}, on or before the mark: the mark is its reference (A_0, the seed's)", took));
                foreach (Country k in world.Countries)
                {
                    if (k.Government != null && k.VoteApprovalMarks.Count != 1) { Check(false, F("(1d) {0} holds {1} marks after one day, one government", k.Id, k.VoteApprovalMarks.Count)); }
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ============================================================ (2) the term's size per point, and its split
        private static void SizePerPoint()
        {
            _sb.Append("    2. the term's size per point, and its split\n");
            WithSwedenAfterOneDay((sim, se) =>
            {
                double b = ApprovalVote.CoefficientFor(CountryId.Sweden);
                Plant(sim, se, se.State.ApprovalRating - PlantedDrop, "planted: approval down");
                EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                ApprovalVote.Reading r = parts.ApprovalReading;
                double expected = b * (r.Net - r.Reference) / 100.0;
                Check(r.HasReference && Math.Abs((r.Net - r.Reference) + PlantedDrop) <= 1e-4 && r.GovernmentShift == expected,
                    F("(2a) net approval {0:F4} against {1:F4}: shift_gov {2:+0.000000;-0.000000} = b x {3:F4} / 100 ({4:+0.0000;-0.0000} pp for the government side)", r.Net, r.Reference, r.GovernmentShift, r.Net - r.Reference, 100.0 * r.GovernmentShift));
                Dictionary<string, double> magnitudes = EconomicVote.Magnitudes(se, sim.CurrentDate);
                double total = magnitudes.Values.Sum(), sum = parts.Approval.Values.Sum();
                bool split = magnitudes.Count > 0 && parts.Approval.Count == magnitudes.Count
                             && magnitudes.All(kv => parts.Approval.TryGetValue(kv.Key, out double a) && Math.Abs(a - r.GovernmentShift * kv.Value / total) <= Exact);
                Check(split && Math.Abs(sum - r.GovernmentShift) <= Exact,
                    F("(2b) split by responsibility (each party's economic vote over the government's {0:0.000}), summing to shift_gov: {1}", total,
                        string.Join(", ", parts.Approval.Select(kv => F("{0} {1:+0.0000;-0.0000} pp ({2:0.0000} pp a point)", kv.Key, 100.0 * kv.Value, b * (magnitudes.TryGetValue(kv.Key, out double m) ? m / total : double.NaN))))));
                Dictionary<string, double> economy = EconomicVote.RecordShiftOf(se, parts.Term.Index, sim.CurrentDate);
                bool sums = parts.Total.Count == economy.Count && economy.All(kv => parts.Total.TryGetValue(kv.Key, out double t) && t == kv.Value + parts.Approval[kv.Key]);
                Check(SameBits(parts.Economy, economy) && sums,
                    F("(2c) the record is the economic vote (unchanged: index {0:0.00}) plus the approval term, per party: {1}", parts.Term.Index,
                        string.Join(", ", parts.Total.Select(kv => F("{0} {1:+0.0000;-0.0000} pp", kv.Key, 100.0 * kv.Value)))));
                Dictionary<string, double> live = EconomicVote.RecordOverTerm(se, sim.CurrentDate, out _);
                Check(SameBits(live, parts.Total), "(2d) the record every live count reads (EconomicVote.RecordOverTerm) is the components' total");

                Plant(sim, se, 1.5f, "planted: approval at 1.5");
                ApprovalVote.ShiftOverTerm(se, sim.CurrentDate, out ApprovalVote.Reading low);
                Check(Math.Abs(low.GovernmentShift - b * (1.5 - low.Reference) / 100.0) <= 1e-9,
                    F("(2e) the plan's (b) arithmetic: at approval 1.5 with no economy stock, against {0:F2}, the government side's term is {1:+0.00;-0.00} pp (b x {2:F2}); the a2_se_big run reads it with its own economy stock netted out", low.Reference, 100.0 * low.GovernmentShift, 1.5 - low.Reference));
            });
        }

        // ============================================================ (3) A_E = A_0: exactly zero
        private static void ExactlyZero()
        {
            _sb.Append("    3. (c) where A_E = A_0 the term is exactly zero\n");
            WithSwedenAfterOneDay((sim, se) =>
            {
                ApprovalMark mark = se.VoteApprovalMarks[0];
                Plant(sim, se, (float)mark.Net, "planted: approval at the reference");
                EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                List<string> keys = se.ParliamentSeats.Keys.ToList();
                double[] shares = Shares(se, keys);
                double[] withRecord = EconomicVote.ApplyRecordShift(keys, shares, parts.Total), economyOnly = EconomicVote.ApplyRecordShift(keys, shares, parts.Economy);
                bool zero = parts.ApprovalReading.HasReference && parts.Approval.Count > 0 && parts.Approval.Values.All(v => v == 0.0);
                bool bits = withRecord.Length == economyOnly.Length && withRecord.Zip(economyOnly, (x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y)).All(same => same);
                Check(zero && SameValues(parts.Total, parts.Economy) && bits,
                    F("(3) net approval {0:R} = the reference {1:R}: every approval part 0, the record and the shifted shares the economy's alone to the bit", parts.ApprovalReading.Net, parts.ApprovalReading.Reference));
            });
        }

        // ============================================================ (4) (d) bounds
        private static void Bounds()
        {
            _sb.Append("    4. (d) the bounds\n");
            WithSwedenAfterOneDay((sim, se) =>
            {
                double b = ApprovalVote.CoefficientFor(CountryId.Sweden);
                List<string> keys = se.ParliamentSeats.Keys.ToList();
                foreach ((float approval, float stock) in new[] { (0f, 20f), (0f, -30f), (100f, -30f), (100f, 40f), (50f, -80f) })
                {
                    Plant(sim, se, approval, "planted: approval at a bound");
                    se.ApprovalEconomyStock = stock;   // the stock is not approval: planting it writes nothing the ledger audits
                    EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                    ApprovalVote.Reading r = parts.ApprovalReading;
                    double[] after = EconomicVote.ApplyRecordShift(keys, Shares(se, keys), parts.Total);
                    double sum = after.Sum();
                    bool sane = after.All(x => x >= 0.0 && !double.IsNaN(x) && !double.IsInfinity(x)) && Math.Abs(sum - 1.0) <= 1e-9;
                    Check(r.Net >= 0.0 && r.Net <= 100.0 && Math.Abs(r.GovernmentShift) <= b + Exact && sane,
                        F("(4) approval {0} with an economy stock {1:+0;-0}: net {2:F2}, shift_gov {3:+0.0000;-0.0000} (|.| <= b = {4}), shifted shares sum {5:R}, none negative or NaN", approval, stock, r.Net, r.GovernmentShift, b, sum));
                }
                se.ApprovalEconomyStock = 0f;
            });
        }

        // ============================================================ (5) a government formed in play
        private static void FormedInPlay()
        {
            _sb.Append("    5. a government formed in play\n");
            WithSwedenAfterOneDay((sim, se) =>
            {
                GovernmentRecord held = se.Government;
                se.Government = GovernmentRecord.OfNamedCabinet(held.PmParty, held.Cabinet, held.Support, sim.CurrentDate, "planted (ApprovalVoteDiagnostic): a government formed in play");
                EconomicVote.RecordParts formationDay = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                Check(EconomicVote.TookOffice(se) == sim.CurrentDate && !formationDay.ApprovalReading.HasReference && formationDay.Approval.Count == 0 && SameBits(formationDay.Total, formationDay.Economy),
                    F("(5a) on its formation day {0:yyyy-MM-dd} no mark is on or after it: no approval term - {1}", sim.CurrentDate, ApprovalVote.Describe(formationDay.ApprovalReading)));
                DateTime formed = sim.CurrentDate;
                Plant(sim, se, se.State.ApprovalRating - 5f, "planted: approval down after the formation");
                Days(sim, 1);
                ApprovalMark own = se.VoteApprovalMarks.FirstOrDefault(m => m.FormedOn == formed);
                EconomicVote.RecordParts next = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                ApprovalVote.Reading r = next.ApprovalReading;
                Check(own != null && se.Government?.FormedOn == formed && own.On == sim.CurrentDate && r.HasReference && r.Reference == own.Net && r.ReferenceOn == own.On && r.Reference != se.VoteApprovalMarks[0].Net
                      && r.GovernmentShift == r.Coefficient * (r.Net - r.Reference) / 100.0,
                    F("(5b) the next day its own mark ({0:yyyy-MM-dd}, net {1:F4}) is the reference, not the start's ({2:F4}) - {3}", own?.On, own?.Net, se.VoteApprovalMarks[0].Net, ApprovalVote.Describe(r)));
            });
        }

        // ============================================================ (6) the net approval's composition, across three boundaries; (7) the save and the clone
        private static void Composition()
        {
            _sb.Append(F("    6. the net approval's composition, played across {0} boundaries\n", Boundaries));
            (SimulationManager sim, World world, GameObject go) = Open();
            try
            {
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                int crossed = 0;
                bool zeroBefore = true;
                var stockBefore = new Dictionary<CountryId, float>();
                var baseObserved = new Dictionary<CountryId, float>();
                for (int d = 0; d < 400 * Boundaries && crossed < Boundaries; d++)
                {
                    if (!sim.AdvanceDay())
                    {
                        if (crossed == 0) { foreach (Country k in world.Countries) { if (k.ApprovalEconomyStock != 0f) { zeroBefore = false; } } }
                        continue;
                    }
                    foreach (Country k in world.Countries) { stockBefore[k.Id] = k.ApprovalEconomyStock; baseObserved[k.Id] = Mathf.Clamp(k.State.ApprovalRating + k.TaxTermStock, 0f, 100f); }
                    sim.AdvanceTurn(decisions);
                    crossed++;
                    if (crossed == 1) { Check(zeroBefore, F("(6a) the first boundary on {0:yyyy-MM-dd}: no economy stock before it, every country", sim.CurrentDate)); }
                    foreach (Country k in world.Countries)
                    {
                        ApprovalAttribution closed = k.ApprovalLedgerLastPeriod;
                        if (closed == null || !closed.Closed || closed.PeriodCloseDate != sim.CurrentDate) { Check(false, F("(6b) {0} has no approval period closed on {1:yyyy-MM-dd}", k.Id, sim.CurrentDate)); continue; }
                        float was = stockBefore[k.Id], economy = closed.GrowthEffect + closed.MiseryUnemployment + closed.MiseryInflation;
                        float share = ApprovalVote.ClampShare(closed, was);
                        float expected = was * ApprovalVote.EconomyStockPersistence + economy + share;
                        // the rest, approval less the stock (less the neutral rating), from the period's open to its close: its move is everything but the stock's own (its reversion, the three terms, its clamp share)
                        float restExpected = (closed.ApprovalAtPeriodOpen - was) + closed.EventSum + closed.TaxTermFading + (closed.FormulaTermSum - economy)
                                             + (1f - ApprovalVote.EconomyStockPersistence) * was + (closed.ClampLoss - share);
                        float rest = closed.ApprovalAtClose - k.ApprovalEconomyStock;
                        float observed = closed.ApprovalAtClose - closed.ApprovalAtPeriodOpen, explained = closed.EventSum + closed.TermSum + closed.ClampLoss;
                        double net = ApprovalVote.NetApproval(k), plain = Math.Max(0.0, Math.Min(100.0, (double)k.State.ApprovalRating - k.ApprovalEconomyStock));
                        Check(Math.Abs(k.ApprovalEconomyStock - expected) <= 1e-4f && Math.Abs(rest - restExpected) <= 0.001f && net == plain && Math.Abs(observed - explained) <= 0.001f,
                            F("(6b) boundary {0} ({1:yyyy-MM-dd}) {2}: the stock {3:+0.0000;-0.0000} = {4:+0.0000;-0.0000} x {5} + growth {6:+0.0000;-0.0000} + unemployment {7:+0.0000;-0.0000} + inflation {8:+0.0000;-0.0000} + clamp share {9:+0.0000;-0.0000}{10}; the rest {11:+0.0000;-0.0000} (expected {12:+0.0000;-0.0000}); approval {13:F4}, net {14:F4}; the ledger's identity observed {15:F4} = explained {16:F4}",
                                crossed, sim.CurrentDate, k.Id, k.ApprovalEconomyStock, was, ApprovalVote.EconomyStockPersistence, closed.GrowthEffect, closed.MiseryUnemployment, closed.MiseryInflation, share,
                                share != 0f ? " (the base met a bound)" : "", rest, restExpected, k.State.ApprovalRating, net, observed, explained));
                    }
                    // (6e) review 879b's L-3: the stock is a part of approval + T, not bookkeeping - the formula's reversion acts on the base the harness
                    // observed (approval + T, as q37 re-clamps it), linearly, at the stock's own rate, toward one neutral: every country's reversion term
                    // implies the same neutral, P + reversion / rate. (A pre-formula approval writer inside AdvanceTurn would break this - it names one.)
                    float rate = 1f - ApprovalVote.EconomyStockPersistence;
                    List<Country> closedHere = world.Countries.Where(k => k.ApprovalLedgerLastPeriod != null && k.ApprovalLedgerLastPeriod.PeriodCloseDate == sim.CurrentDate).ToList();
                    List<float> implied = closedHere.Select(k => baseObserved[k.Id] + k.ApprovalLedgerLastPeriod.Reversion / rate).ToList();
                    float spread = closedHere.Count > 0 ? closedHere.Max(k => baseObserved[k.Id]) - closedHere.Min(k => baseObserved[k.Id]) : 0f;
                    bool oneNeutral = implied.Count > 1 && implied.Max() - implied.Min() <= 0.01f
                                      && closedHere.All(k => Math.Abs(ApprovalVote.BaseBeforeFormula(k.ApprovalLedgerLastPeriod) - baseObserved[k.Id]) <= 1e-3f);
                    Check(oneNeutral && (crossed < 2 || spread > 0.5f),
                        F("(6e) boundary {0}: the reversion acts on approval + T at the stock's rate {1:R} toward one neutral - implied {2:F4} to {3:F4} over bases {4:F2} apart; each base read back from its reversion term matches the observed one",
                            crossed, rate, implied.Count > 0 ? implied.Min() : float.NaN, implied.Count > 0 ? implied.Max() : float.NaN, spread));
                    if (crossed >= 2 && world.Countries.All(k => stockBefore[k.Id] == 0f)) { Check(false, F("(6c) boundary {0}: every stock stood at zero before it - the stock's reversion is not exercised", crossed)); }
                }
                Check(crossed == Boundaries, F("(6d) {0} of {1} boundaries crossed", crossed, Boundaries));
                Country se = world.GetCountry(CountryId.Sweden);
                EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                _sb.Append(F("        (printed) Sweden's record on {0:yyyy-MM-dd}: {1}; economy {2}; approval {3}\n", sim.CurrentDate, ApprovalVote.Describe(parts.ApprovalReading),
                    string.Join(", ", parts.Economy.Select(kv => F("{0} {1:+0.000;-0.000} pp", kv.Key, 100.0 * kv.Value))), string.Join(", ", parts.Approval.Select(kv => F("{0} {1:+0.000;-0.000} pp", kv.Key, 100.0 * kv.Value)))));

                _sb.Append("    7. the save and the clone\n");
                string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null));
                SaveGame cut = SaveGameService.Deserialize(json);
                Country saved = cut.World.GetCountry(CountryId.Sweden);
                Check(cut.SaveVersion == SaveGameService.CurrentSaveVersion && cut.SaveVersion >= 45 && saved.ApprovalEconomyStock == se.ApprovalEconomyStock && SameMarks(saved.VoteApprovalMarks, se.VoteApprovalMarks),
                    F("(7a) a save cut on {0:yyyy-MM-dd} carries format {1}, the economy stock {2:R} and {3} mark(s) by content", sim.CurrentDate, cut.SaveVersion, saved.ApprovalEconomyStock, saved.VoteApprovalMarks?.Count));
                MethodInfo clone = typeof(SimulationManager).GetMethod("ClonePreviewCountry", BindingFlags.NonPublic | BindingFlags.Static);
                var copy = clone?.Invoke(null, new object[] { se }) as Country;
                Check(copy != null && copy.ApprovalEconomyStock == se.ApprovalEconomyStock && SameMarks(copy.VoteApprovalMarks, se.VoteApprovalMarks) && !ReferenceEquals(copy.VoteApprovalMarks, se.VoteApprovalMarks),
                    "(7b) the preview's clone (ClonePreviewCountry) carries the economy stock and the marks as its own list");
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ============================================================ (8) the floor and the ceiling (review 879's M1)
        private static void FloorAndCeiling()
        {
            _sb.Append("    8. the clamp's share (review 879's M1)\n");
            SimulationRandom.Seed(777);
            Country k = WorldFactory.CreateDefault().GetCountry(CountryId.Sweden);   // a bare country: no simulation runs and no period closes on it, so no approval write here is audited
            float speed = 1f - ApprovalVote.EconomyStockPersistence;

            // the stock at -45 and approval held at 0 by the clamp; the reversion (+speed x 50) and the economy (-4, then -5) the formula's only terms
            float AtFloor(float misery, float impulse, out float share)
            {
                var closed = new ApprovalAttribution { Reversion = speed * 50f, MiseryUnemployment = misery, TaxHikePenalty = -impulse, ApprovalAfterFormula = 0f };
                closed.ClampLoss = 0f - closed.FormulaTermSum;   // approval stayed at 0
                k.State.ApprovalRating = 0f;
                k.ApprovalEconomyStock = -45f;
                share = ApprovalVote.ClampShare(closed, k.ApprovalEconomyStock);
                ApprovalVote.AdvanceEconomyStock(k, closed);
                return k.ApprovalEconomyStock;
            }
            float stock4 = AtFloor(-4f, 0f, out float share4); double net4 = ApprovalVote.NetApproval(k);
            float stock5 = AtFloor(-5f, 0f, out float share5); double net5 = ApprovalVote.NetApproval(k);
            float stockTax = AtFloor(-4f, 3f, out float shareTax);
            float expected4 = -45f * ApprovalVote.EconomyStockPersistence - 4f + share4;
            Check(Math.Abs(stock4 - expected4) <= 1e-4f && net5 <= net4 + 1e-4 && Math.Abs(net5 - net4) <= 1e-4 && share4 > 0f && share5 > share4,
                F("(8a) at the floor a worse economy never raises net approval: the economy -4 -> stock {0:F4} (clamp share {1:+0.0000;-0.0000}), net {2:F4}; the economy -5 -> stock {3:F4} (share {4:+0.0000;-0.0000}), net {5:F4} (before the fix the stock took no share: net {6:F4} and {7:F4})",
                    stock4, share4, net4, stock5, share5, net5, -(-45f * ApprovalVote.EconomyStockPersistence - 4f), -(-45f * ApprovalVote.EconomyStockPersistence - 5f)));
            Check(Math.Abs(shareTax - share4) <= 1e-4f && Math.Abs(stockTax - stock4) <= 1e-4f,
                F("(8b) a tax impulse of 3 at the floor is outside the base (approval + T, which TaxTerm.BookFormula keeps as the run without the term): the stock's share {0:+0.0000;-0.0000}, as without it {1:+0.0000;-0.0000}", shareTax, share4));

            float AtCeiling(float growth, out float share)
            {
                var closed = new ApprovalAttribution { Reversion = speed * (50f - 100f), GrowthEffect = growth, ApprovalAfterFormula = 100f };
                closed.ClampLoss = 0f - closed.FormulaTermSum;   // approval stayed at 100
                k.State.ApprovalRating = 100f;
                k.ApprovalEconomyStock = 40f;
                share = ApprovalVote.ClampShare(closed, k.ApprovalEconomyStock);
                ApprovalVote.AdvanceEconomyStock(k, closed);
                return k.ApprovalEconomyStock;
            }
            AtCeiling(4f, out float up4); double ceil4 = ApprovalVote.NetApproval(k);
            AtCeiling(5f, out float up5); double ceil5 = ApprovalVote.NetApproval(k);
            Check(up4 < 0f && ceil5 >= ceil4 - 1e-4 && Math.Abs(ceil5 - ceil4) <= 1e-4,
                F("(8c) at the ceiling a better economy never lowers net approval: growth +4 -> share {0:+0.0000;-0.0000}, net {1:F4}; +5 -> share {2:+0.0000;-0.0000}, net {3:F4}", up4, ceil4, up5, ceil5));

            var inside = new ApprovalAttribution { Reversion = 1f, MiseryUnemployment = -2f, ApprovalAfterFormula = 30f, ClampLoss = 0f };
            Check(ApprovalVote.ClampShare(inside, -10f) == 0f, "(8d) inside [0, 100] the stock takes no share");

            // (8e) review 879b's M-2: a tax term standing against the bound - approval meets it, the base (approval + T) does not
            float WithTerm(float approval, float term, float stock, float growth, float misery, out float share)
            {
                var closed = new ApprovalAttribution { Reversion = speed * (50f - (approval + term)), GrowthEffect = growth, MiseryUnemployment = misery };
                float after = Mathf.Clamp(approval + closed.FormulaTermSum, 0f, 100f);
                closed.ApprovalAfterFormula = after;
                closed.ClampLoss = (after - approval) - closed.FormulaTermSum;
                k.State.ApprovalRating = after;
                k.ApprovalEconomyStock = stock;
                share = ApprovalVote.ClampShare(closed, stock);
                ApprovalVote.AdvanceEconomyStock(k, closed);
                return k.ApprovalEconomyStock;
            }
            WithTerm(0f, 3f, -10f, 0f, -3f, out float floor3); double floorNet3 = ApprovalVote.NetApproval(k);
            WithTerm(0f, 3f, -10f, 0f, -4f, out float floor4); double floorNet4 = ApprovalVote.NetApproval(k);
            Check(floor3 > 0f && floorNet4 <= floorNet3 + 1e-4,
                F("(8e) at the floor with T = +3 (the base at 3 stays inside): the economy -3 -> share {0:+0.0000;-0.0000}, net {1:F4}; -4 -> share {2:+0.0000;-0.0000}, net {3:F4} - a worse economy does not raise net approval (unshared it read 12.5 and 13.5)",
                    floor3, floorNet3, floor4, floorNet4));
            WithTerm(100f, -3f, 10f, 3f, 0f, out float ceil3); double ceilNet3 = ApprovalVote.NetApproval(k);
            WithTerm(100f, -3f, 10f, 4f, 0f, out float ceil4b); double ceilNet4 = ApprovalVote.NetApproval(k);
            Check(ceil3 < 0f && ceilNet4 >= ceilNet3 - 1e-4,
                F("(8e) at the ceiling with T = -3 (the base at 97 stays inside): growth +3 -> share {0:+0.0000;-0.0000}, net {1:F4}; +4 -> share {2:+0.0000;-0.0000}, net {3:F4} - a better economy does not lower net approval",
                    ceil3, ceilNet3, ceil4b, ceilNet4));
        }

        // ============================================================ the harness's hands
        private static (SimulationManager, World, GameObject) Open()
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("APPROVALVOTE");
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            sim.PlayerCountryId = CountryId.Sweden;
            return (sim, world, go);
        }

        /// <summary>A fresh world stepped one day, so the government of record carries its mark.</summary>
        private static void WithSwedenAfterOneDay(Action<SimulationManager, Country> body)
        {
            (SimulationManager sim, World world, GameObject go) = Open();
            try
            {
                Days(sim, 1);
                Country se = world.GetCountry(CountryId.Sweden);
                if (se.VoteApprovalMarks.Count == 0) { Check(false, "the world carries no mark after its first day"); return; }
                body(sim, se);
            }
            finally { Object.DestroyImmediate(go); }
        }

        /// <summary><paramref name="n"/> days of the game, each boundary resolved with no policy.</summary>
        private static void Days(SimulationManager sim, int n)
        {
            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country k in sim.World.Countries) { decisions[k.Id] = PolicyDecision.None(); }
            for (int d = 0; d < n; d++) { if (sim.AdvanceDay()) { sim.AdvanceTurn(decisions); } }
        }

        /// <summary>Sets approval as a writer of the game would, its move recorded on the approval ledger as the game's callers record theirs.</summary>
        private static void Plant(SimulationManager sim, Country c, float approval, string label)
        {
            float before = c.State.ApprovalRating;
            c.State.ApprovalRating = Mathf.Clamp(approval, 0f, 100f);
            ApprovalLedgerRecorder.RecordEvent(c, sim.CurrentDate, label, c.State.ApprovalRating - before);
        }

        /// <summary>The chamber's seat shares as a stand-in share vector, in key order.</summary>
        private static double[] Shares(Country c, List<string> keys)
        {
            double seats = keys.Sum(k => (double)c.ParliamentSeats[k]);
            return keys.Select(k => c.ParliamentSeats[k] / seats).ToArray();
        }

        private static bool SameBits(Dictionary<string, double> a, Dictionary<string, double> b)
            => a != null && b != null && a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out double v) && BitConverter.DoubleToInt64Bits(v) == BitConverter.DoubleToInt64Bits(kv.Value));

        private static bool SameValues(Dictionary<string, double> a, Dictionary<string, double> b)
            => a != null && b != null && a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out double v) && v == kv.Value);

        private static bool SameMarks(List<ApprovalMark> a, List<ApprovalMark> b)
            => a != null && b != null && a.Count == b.Count && a.Zip(b, (x, y) => x.FormedOn == y.FormedOn && x.On == y.On && x.Net == y.Net).All(same => same);

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "        ok   " : "        FAIL ").Append(what).Append('\n');
            if (!ok) { _failures++; Debug.LogError("APPROVAL VOTE: FAIL - " + what); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
