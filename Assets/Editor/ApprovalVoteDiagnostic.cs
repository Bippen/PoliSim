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
    /// <item><b>The net approval's composition</b>, played day by day across three boundaries (review 879's M3; on approval's daily step since MF-13,
    /// K191): the economy stock is zero before the world's first day; on every country-day the clamp took nothing, the stock is its own daily persistence
    /// on yesterday's plus the day's growth, unemployment and inflation terms as the ledger recorded them (a clamped day's share is section 8's); at each
    /// boundary net approval is approval less the stock, clamped, and the ledger's identity holds. And the stock is a part of approval + T, not
    /// bookkeeping (review 879b's L-3): every country-day's reversion is the stock's own rate times the neutral 50 less the base the day opened on.</item>
    /// <item><b>The save and the clone</b>: a save cut after the boundary carries the stock and the marks (format 45); the preview's clone carries both,
    /// the marks as its own list.</item>
    /// <item><b>The clamp's share</b> (review 879's M1), one day of approval's daily step by hand on a bare country: at the floor a worse economy never raises
    /// net approval, at the ceiling a better one never lowers it; a tax impulse through the boundary formula leaves the stock untouched; inside [0, 100] no share. With a tax term standing against the bound (review 879b's M-2), approval's own clamp is shared, and the same two hold.</item>
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
                ? "APPROVAL VOTE: PASS - the reference, the term's size and split, the RECORD line's own arithmetic (K60), exactly zero at no change, the bounds, a government formed in play, the net approval's composition over three boundaries, the save and the clone, the clamp's share at the floor and the ceiling."
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
                Check(se.Government != null && se.VoteApprovalMarks.Count == 1 && mark.FormedOn == se.Government.FormedOn && mark.Net == (double)seed,   // MF-13 (K191): the mark is taken before the day's approval step, so it reads the seed - the step then moves approval and the stock
                    F("(1b) after the first day one mark for the government formed {0:yyyy-MM-dd}: net {1:R} on {2:yyyy-MM-dd}, the seed's approval {3:R} with no economy stock (the mark taken before the day's approval step)", se.Government?.FormedOn, mark?.Net, mark?.On, seed));
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
                bool sums = parts.Total.Count == economy.Count && economy.All(kv => parts.Total.TryGetValue(kv.Key, out double t)
                    && t == (parts.Fiscal.TryGetValue(kv.Key, out double fiscal) ? (kv.Value + parts.Approval[kv.Key]) + fiscal : kv.Value + parts.Approval[kv.Key]));   // q13's fiscal term after them, where the book carries one (K62)
                Check(SameBits(parts.Economy, economy) && sums,
                    F("(2c) the record is the economic vote (unchanged: index {0:0.00}) plus the approval term (and q13's fiscal term where one is carried), per party: {1}", parts.Term.Index,
                        string.Join(", ", parts.Total.Select(kv => F("{0} {1:+0.0000;-0.0000} pp", kv.Key, 100.0 * kv.Value)))));
                // K60 (2026-10-23): q15's pass on the RECORD line - the line names the approval term WITH its economy stock, and the size it prints is
                // b x the net change it prints, within 0.01 pp (the pass Tools/audit/aloop_record_check.pl applies to every count of an audit run)
                string line = ApprovalVote.Describe(r);
                System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(line,
                    @"approval net of its economy terms (-?[0-9.]+) \(approval (-?[0-9.]+), its economy stock ([-+][0-9.]+)\) against (-?[0-9.]+) when .*?: b ([0-9.]+).*?-> the government side ([-+][0-9.]+) pp");
                double P(int g) => double.Parse(m.Groups[g].Value, CultureInfo.InvariantCulture);
                double printedSize = m.Success ? P(6) : double.NaN, fromPrinted = m.Success ? P(5) * (P(1) - P(4)) : double.NaN;
                Check(m.Success && P(5) == 0.16 && b == 0.16 && P(5) == ApprovalVote.CoefficientFor(CountryId.Sweden) && Math.Abs(printedSize - fromPrinted) <= 0.01 && Math.Abs(P(1) - Math.Max(0.0, Math.Min(100.0, P(2) - P(3)))) <= 0.02,
                    F("(2f) K60: the RECORD line names the approval term with its economy stock and b at 0.16, and its printed size {0:+0.00;-0.00} pp is b x the printed net change ({1:+0.0000;-0.0000} pp) within 0.01 pp - \"{2}\"", printedSize, fromPrinted, line));
                Dictionary<string, double> live = EconomicVote.RecordOverTerm(se, sim.CurrentDate, out _);
                Check(SameBits(live, parts.Total), "(2d) the record every live count reads (EconomicVote.RecordOverTerm) is the components' total");

                se.ApprovalEconomyStock = 0f;   // MF-13 (K191): the stock advances daily, so the case sets the "no economy stock" its premise names
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
                se.ApprovalEconomyStock = 0f;   // MF-13 (K191): the stock advances daily from day 1, so net approval equals the planted approval only with it at zero
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
        // MF-13 (K191): the economy stock moves on approval's daily step, so its composition is asserted day by day - each day's stock against the
        // day's own recorded economy terms, each day's reversion against the base the day opened on - and the period's ledger identity at each boundary
        private static void Composition()
        {
            _sb.Append(F("    6. the net approval's composition, played day by day across {0} boundaries\n", Boundaries));
            (SimulationManager sim, World world, GameObject go) = Open();
            try
            {
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                Check(world.Countries.All(k => k.ApprovalEconomyStock == 0f), "(6a) no economy stock before the world's first day, every country");
                float speed = 1f - ApprovalVote.EconomyStockPersistence;
                int crossed = 0, stockDays = 0, clampDays = 0, stockMisses = 0, reversionDays = 0, reversionMisses = 0;
                string firstStockMiss = null, firstReversionMiss = null;
                for (int d = 0; d < 400 * Boundaries && crossed < Boundaries; d++)
                {
                    var before = new Dictionary<CountryId, (float Stock, float Economy, float Reversion, float Clamp, float Base)>();
                    foreach (Country k in world.Countries)
                    {
                        ApprovalAttribution l = k.ApprovalLedgerAccruing;
                        before[k.Id] = (k.ApprovalEconomyStock, l != null ? l.GrowthEffect + l.MiseryUnemployment + l.MiseryInflation : 0f, l != null ? l.Reversion : 0f,
                            l != null ? l.ClampLoss : 0f, Mathf.Clamp(k.State.ApprovalRating + k.TaxTermStock, 0f, 100f));
                    }
                    bool boundary = sim.AdvanceDay();
                    foreach (Country k in world.Countries)
                    {
                        ApprovalAttribution l = k.ApprovalLedgerAccruing;
                        if (l == null) { continue; }
                        var b = before[k.Id];
                        float economyDay = l.GrowthEffect + l.MiseryUnemployment + l.MiseryInflation - b.Economy;
                        float clampDay = l.ClampLoss - b.Clamp;
                        float reversionDay = l.Reversion - b.Reversion;
                        // (6b) the day's stock: its own persistence on yesterday's, plus the day's three economy terms as the ledger recorded them - on every
                        // day the clamp took nothing (a day's float rounding is in ClampLoss too, far under the bar); a clamped day is counted, its share
                        // asserted by hand in section 8
                        if (Math.Abs(clampDay) > 1e-4f) { clampDays++; }
                        else
                        {
                            stockDays++;
                            float expected = b.Stock * ApprovalVote.EconomyStockPersistence + economyDay;
                            if (Math.Abs(k.ApprovalEconomyStock - expected) > 1e-4f)
                            {
                                stockMisses++;
                                firstStockMiss = firstStockMiss ?? F("{0} {1:yyyy-MM-dd}: stock {2:R}, expected {3:R} x {4:R} + {5:R}", k.Id, sim.CurrentDate, k.ApprovalEconomyStock, b.Stock, ApprovalVote.EconomyStockPersistence, economyDay);
                            }
                        }
                        // (6e) the day's reversion acts on approval + T as the day opened (the tax term's fading moves approval and T together, so the base
                        // is yesterday's), at the stock's own rate, toward the neutral 50 - the stock is a part of the base, not bookkeeping (review 879b's L-3)
                        reversionDays++;
                        float wantReversion = speed * (50f - b.Base);
                        if (Math.Abs(reversionDay - wantReversion) > 1e-4f)
                        {
                            reversionMisses++;
                            firstReversionMiss = firstReversionMiss ?? F("{0} {1:yyyy-MM-dd}: reversion {2:R}, expected {3:R} x (50 - {4:R})", k.Id, sim.CurrentDate, reversionDay, speed, b.Base);
                        }
                    }
                    if (!boundary) { continue; }
                    sim.AdvanceTurn(decisions);
                    crossed++;
                    foreach (Country k in world.Countries)
                    {
                        ApprovalAttribution closed = k.ApprovalLedgerLastPeriod;
                        if (closed == null || !closed.Closed || closed.PeriodCloseDate != sim.CurrentDate) { Check(false, F("(6f) {0} has no approval period closed on {1:yyyy-MM-dd}", k.Id, sim.CurrentDate)); continue; }
                        float observed = closed.ApprovalAtClose - closed.ApprovalAtPeriodOpen, explained = closed.EventSum + closed.TermSum + closed.ClampLoss;
                        double net = ApprovalVote.NetApproval(k), plain = Math.Max(0.0, Math.Min(100.0, (double)k.State.ApprovalRating - k.ApprovalEconomyStock));
                        Check(net == plain && Math.Abs(observed - explained) <= 0.001f,
                            F("(6f) boundary {0} ({1:yyyy-MM-dd}) {2}: the period's economy terms growth {3:+0.0000;-0.0000}, unemployment {4:+0.0000;-0.0000}, inflation {5:+0.0000;-0.0000}; the stock {6:+0.0000;-0.0000}; approval {7:F4}, net {8:F4}; the ledger's identity observed {9:F4} = explained {10:F4}",
                                crossed, sim.CurrentDate, k.Id, closed.GrowthEffect, closed.MiseryUnemployment, closed.MiseryInflation, k.ApprovalEconomyStock, k.State.ApprovalRating, net, observed, explained));
                    }
                }
                Check(stockMisses == 0 && stockDays > 0,
                    F("(6b) the stock is its own persistence ({0:R} a day) on yesterday's plus the day's growth, unemployment and inflation terms, on {1} country-days the clamp took nothing ({2} clamped country-days asserted in section 8){3}",
                        ApprovalVote.EconomyStockPersistence, stockDays, clampDays, stockMisses > 0 ? F("; {0} missed, the first {1}", stockMisses, firstStockMiss) : ""));
                Check(reversionMisses == 0 && reversionDays > 0,
                    F("(6e) the day's reversion is {0:R} x (50 - the base the day opened on), on {1} country-days{2}", speed, reversionDays, reversionMisses > 0 ? F("; {0} missed, the first {1}", reversionMisses, firstReversionMiss) : ""));
                Check(world.Countries.Any(k => k.ApprovalEconomyStock != 0f), "(6c) an economy stock stands somewhere after the run - the stock's persistence is exercised");
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

        // ============================================================ (8) the floor and the ceiling (review 879's M1), on the daily step (MF-13, K191)
        private static void FloorAndCeiling()
        {
            _sb.Append("    8. the clamp's share (review 879's M1), one day of approval's daily step by hand\n");
            SimulationRandom.Seed(777);
            Country k = WorldFactory.CreateDefault().GetCountry(CountryId.Sweden);   // a bare country: no simulation runs and no period closes on it, so no approval write here is audited
            float speed = 1f - ApprovalVote.EconomyStockPersistence;

            // One day by hand: approval and T planted, the stock planted, the day's reversion speed x (50 - (approval + T)) and the economy term the
            // step's only terms; approval clamped as the daily step clamps it; the stock advanced as the daily step advances it. The economy term is
            // sized against the live reversion so the step is the case's own (the floor: -1.5, then -2.5; the ceiling +1.5, then +2.5; with a tax term
            // standing against the bound: -0.65 and -1.65, +0.65 and +1.65) - at the pre-MF-13 once-a-turn speed 0.05 these were the economy -4/-5,
            // +4/+5, -3/-4 and +3/+4.
            float Day(float approval, float term, float stock, float economy, out float share)
            {
                float baseBefore = approval + term;
                float step = speed * (50f - baseBefore) + economy;
                float after = Mathf.Clamp(approval + step, 0f, 100f);
                float clampLoss = (after - approval) - step;
                k.State.ApprovalRating = after;
                k.ApprovalEconomyStock = stock;
                share = ApprovalVote.ClampShare(stock, economy, baseBefore, step, after, clampLoss);
                ApprovalVote.AdvanceEconomyStockDay(k, economy, baseBefore, step, after, clampLoss);
                return k.ApprovalEconomyStock;
            }

            float floorReversion = speed * 50f;
            float floorMisery4 = -(floorReversion + 1.5f), floorMisery5 = -(floorReversion + 2.5f);
            float stock4 = Day(0f, 0f, -45f, floorMisery4, out float share4); double net4 = ApprovalVote.NetApproval(k);
            float stock5 = Day(0f, 0f, -45f, floorMisery5, out float share5); double net5 = ApprovalVote.NetApproval(k);
            float expected4 = -45f * ApprovalVote.EconomyStockPersistence + floorMisery4 + share4;
            Check(Math.Abs(stock4 - expected4) <= 1e-4f && net5 <= net4 + 1e-4 && Math.Abs(net5 - net4) <= 1e-4 && share4 > 0f && share5 > share4,
                F("(8a) at the floor a worse economy never raises net approval: the economy {0:F4} -> stock {1:F4} (clamp share {2:+0.0000;-0.0000}), net {3:F4}; the economy {4:F4} -> stock {5:F4} (share {6:+0.0000;-0.0000}), net {7:F4} (before the fix the stock took no share: net {8:F4} and {9:F4})",
                    floorMisery4, stock4, share4, net4, floorMisery5, stock5, share5, net5, -(-45f * ApprovalVote.EconomyStockPersistence + floorMisery4), -(-45f * ApprovalVote.EconomyStockPersistence + floorMisery5)));

            // (8b) the boundary formula's one-off step (a tax impulse) is no move of the stock's: approval at the floor, the stock at -45, an impulse of 3
            k.State.ApprovalRating = 0f;
            k.ApprovalEconomyStock = -45f;
            MacroSystem.ApplyApprovalRating(k, PolicyDecision.None(), 3f, 0f);
            Check(k.ApprovalEconomyStock == -45f && k.State.ApprovalRating == 0f,
                F("(8b) a tax impulse of 3 at the floor, through the boundary formula: approval {0}, the stock {1:R} - untouched (the stock moves on the daily step alone)", k.State.ApprovalRating, k.ApprovalEconomyStock));

            float ceilingGrowth4 = -(speed * (50f - 100f)) + 1.5f, ceilingGrowth5 = -(speed * (50f - 100f)) + 2.5f;
            Day(100f, 0f, 40f, ceilingGrowth4, out float up4); double ceil4 = ApprovalVote.NetApproval(k);
            Day(100f, 0f, 40f, ceilingGrowth5, out float up5); double ceil5 = ApprovalVote.NetApproval(k);
            Check(up4 < 0f && ceil5 >= ceil4 - 1e-4 && Math.Abs(ceil5 - ceil4) <= 1e-4,
                F("(8c) at the ceiling a better economy never lowers net approval: growth {0:+0.0000;-0.0000} -> share {1:+0.0000;-0.0000}, net {2:F4}; {3:+0.0000;-0.0000} -> share {4:+0.0000;-0.0000}, net {5:F4}", ceilingGrowth4, up4, ceil4, ceilingGrowth5, up5, ceil5));

            Check(ApprovalVote.ClampShare(-10f, -2f, 48f, -1f, 30f, 0f) == 0f, "(8d) inside [0, 100] the stock takes no share");

            // (8e) review 879b's M-2: a tax term standing against the bound - approval meets it, the base (approval + T) does not
            float termFloorMisery3 = -(speed * (50f - 3f)) - 0.65f, termFloorMisery4 = -(speed * (50f - 3f)) - 1.65f;
            float termCeilingGrowth3 = -(speed * (50f - 97f)) + 0.65f, termCeilingGrowth4 = -(speed * (50f - 97f)) + 1.65f;
            Day(0f, 3f, -10f, termFloorMisery3, out float floor3); double floorNet3 = ApprovalVote.NetApproval(k);
            Day(0f, 3f, -10f, termFloorMisery4, out float floor4); double floorNet4 = ApprovalVote.NetApproval(k);
            Check(floor3 > 0f && floorNet4 <= floorNet3 + 1e-4,
                F("(8e) at the floor with T = +3 (the base at 3 stays inside): the economy {0:F4} -> share {1:+0.0000;-0.0000}, net {2:F4}; {3:F4} -> share {4:+0.0000;-0.0000}, net {5:F4} - a worse economy does not raise net approval",
                    termFloorMisery3, floor3, floorNet3, termFloorMisery4, floor4, floorNet4));
            Day(100f, -3f, 10f, termCeilingGrowth3, out float ceil3); double ceilNet3 = ApprovalVote.NetApproval(k);
            Day(100f, -3f, 10f, termCeilingGrowth4, out float ceil4b); double ceilNet4 = ApprovalVote.NetApproval(k);
            Check(ceil3 < 0f && ceilNet4 >= ceilNet3 - 1e-4,
                F("(8e) at the ceiling with T = -3 (the base at 97 stays inside): growth {0:+0.0000;-0.0000} -> share {1:+0.0000;-0.0000}, net {2:F4}; {3:+0.0000;-0.0000} -> share {4:+0.0000;-0.0000}, net {5:F4} - a better economy does not lower net approval",
                    termCeilingGrowth3, ceil3, ceilNet3, termCeilingGrowth4, ceil4b, ceilNet4));
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
