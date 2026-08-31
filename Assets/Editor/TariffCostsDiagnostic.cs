using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PASS 6 MEASUREMENT (tariff costs, 2026-08-27): the free lever pass 5 measured, re-run under the
    /// three cost forces - and, at the plumbing commit with every TradeCosts constant at 0, under the
    /// old books, so the two logs read side by side as "old books vs new".
    ///
    /// Each of the six countries in turn sets the 50% cap on every partner override at t1 (the
    /// pass-5 idiom: PolicyDecision.PartnerTariffOverrides, which the harness applies WITHOUT a
    /// vote - the numbers are the economics "as if passed", said here so the vote line below is read
    /// as the separate thing it is) and runs 30 turns against one shared same-seed no-policy control.
    /// Per overrider: the take, inflation and expectations around the year the wedge lands (the
    /// look-through's own check: the bump prints for one year and returns), the trade balance, GDP,
    /// unemployment, approval and the debt path; per partner: the mirrored take, THEIR own
    /// pass-through inflation (a trade war costs the retaliator too), approval, GDP and debt - the
    /// same partner series pass 5's instrument recorded, so old-vs-new is like-for-like. The
    /// pass-through constant's stakes are measured at 1.0 and at 0.5 (TradeCosts.
    /// PassThroughMeasurementScale) rather than asserted. A removal case (Sweden at the cap from t1,
    /// reset before t3) asserts the clamp-safe look-through identity at every event-free boundary:
    /// expectations adapt toward the print NET of the pass-through that actually landed, so a tariff
    /// cut whose negative wedge floors the print at 0 cannot ratchet expectations up. Then the vote:
    /// the overrides-only bill's direction and verdict at the seed parliament.
    ///
    /// Folded asserts (exit code): no retaliation on any of the 20 directed links of a fresh world and
    /// no pass-through at the control run's first boundary (the no-override path is inert at BOTH
    /// commits); with the forces on, every partner mirrors the cap, the wedge prints, and the
    /// overrides-only bill has nonzero direction for all six.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.TariffCostsDiagnostic.Run -logFile &lt;path&gt;`, or from the menu.
    /// </summary>
    public static class TariffCostsDiagnostic
    {
        [MenuItem("PoliSim/Run Tariff Costs Measurement (pass 6)")]
        private static void RunFromMenu() => Run();

        private const int Seed = 777;
        private const int Turns = 30;
        private const int RemovalResetTurn = 3;
        private const float CapRatePercent = 50f; // SimulationManager.MaxBaseTariffRate, private there
        private const float ExpectationsAdaptationSpeed = 0.5f; // MacroSystem's, private there
        private const float Tolerance = 1e-3f;
        private static readonly float[] PassThroughScales = { 1f, 0.5f };
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private sealed class TurnSnapshot
        {
            public float Gdp, TradeBalance, Inflation, Expectations, Unemployment, Approval, DebtRatio, Rate, TariffRevenue, PassThroughPp;
            public bool EventFired;
        }

        private sealed class RunRecord
        {
            public readonly Dictionary<(int Turn, CountryId Id), TurnSnapshot> Snapshots = new Dictionary<(int, CountryId), TurnSnapshot>();
            public readonly Dictionary<CountryId, Dictionary<string, float>> EndFields = new Dictionary<CountryId, Dictionary<string, float>>();
            public readonly Dictionary<CountryId, float> PartnerRateOnOverriderAtT1 = new Dictionary<CountryId, float>();
            public readonly List<(int Turn, float ExpectationsBefore)> ExpectationsBefore = new List<(int, float)>();
            public TurnSnapshot At(int turn, CountryId id) => Snapshots[(turn, id)];
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            System.Threading.Thread.CurrentThread.CurrentCulture = Inv;
            bool ok = true;
            Debug.Log($"TARIFFCOSTS: === forces at this build: pass-through {TradeCosts.ImportPricePassThrough:F2}, retaliation mirror {TradeCosts.RetaliationMirrorFraction:F2}, override direction weight {TradeCosts.OverrideDirectionWeight:F2} (all 0 = the pre-pass-6 books) ===");

            ok &= AssertFreshWorldInert();

            TradeCosts.PassThroughMeasurementScale = 1f;
            RunRecord control = RunWorld(null);
            ok &= AssertControlFirstBoundaryInert(control);

            foreach (float scale in PassThroughScales)
            {
                if (scale != 1f && TradeCosts.ImportPricePassThrough <= 0f)
                {
                    Debug.Log($"TARIFFCOSTS: pass-through scale {scale:F1} skipped - the constant is 0 at this build, the scale has nothing to scale.");
                    continue;
                }

                TradeCosts.PassThroughMeasurementScale = scale;
                Debug.Log($"TARIFFCOSTS: === every partner override at {CapRatePercent:F0}% from t1, seed {Seed}, {Turns} turns, pass-through scale {scale:F1} (effective {TradeCosts.ImportPricePassThrough * scale:F2}) ===");
                foreach (CountryId overrider in AllCountries())
                {
                    RunRecord exploit = RunWorld(overrider);
                    ok &= ReportOverrider(overrider, control, exploit, scale);
                }
            }

            TradeCosts.PassThroughMeasurementScale = 1f;
            ok &= ReportRemovalCase(control);
            ok &= ReportVotes();

            Debug.Log(ok ? "TARIFFCOSTS: PASS - every folded assert held." : "TARIFFCOSTS: FAIL - an assert above did not hold.");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static IEnumerable<CountryId> AllCountries() => (CountryId[])System.Enum.GetValues(typeof(CountryId));

        /// <summary>The no-override path at seed: no link carries a retaliatory term and the public rate equals the country's own rate on all 20 directed links - true at the plumbing commit AND at the build.</summary>
        private static bool AssertFreshWorldInert()
        {
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            bool ok = true;
            int links = 0;
            foreach (Country c in world.Countries)
            {
                foreach (TradePartner link in c.TradePartners)
                {
                    Country partner = world.GetCountry(link.PartnerId);
                    links++;
                    float retaliation = TradeSystem.GetRetaliatoryTariffRate(partner, c, world.TradeBlocs);
                    float rate = TradeSystem.GetTariffRate(partner, c, world.TradeBlocs);
                    float own = TradeSystem.GetOwnTariffRate(partner, c, world.TradeBlocs);
                    if (retaliation != 0f || rate != own)
                    {
                        ok = false;
                        Debug.LogError($"TARIFFCOSTS INERT: {partner.Id} charges {c.Id} {rate:F3} (own {own:F3}, retaliation {retaliation:F3}) with NO override anywhere - the no-override path is not inert.");
                    }
                }
            }

            Debug.Log($"TARIFFCOSTS INERT: fresh world, {links} directed links - retaliation 0 and rate == own on every one: {(ok ? "OK" : "FAILED")}.");
            return ok;
        }

        private static bool AssertControlFirstBoundaryInert(RunRecord control)
        {
            bool ok = true;
            foreach (CountryId id in AllCountries())
            {
                float pp = control.At(1, id).PassThroughPp;
                if (pp != 0f)
                {
                    ok = false;
                    Debug.LogError($"TARIFFCOSTS INERT: {id} carries a pass-through of {pp:F5} pp at the control's first boundary with no tariff change anywhere.");
                }
            }

            Debug.Log($"TARIFFCOSTS INERT: control run, boundary 1 - the six pass-through figures are all 0: {(ok ? "OK" : "FAILED")}.");
            return ok;
        }

        private static RunRecord RunWorld(CountryId? overrider, int resetAtTurn = -1)
        {
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"TariffCosts_{(overrider.HasValue ? overrider.Value.ToString() : "control")}");
            var record = new RunRecord();
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                for (int turn = 1; turn <= Turns; turn++)
                {
                    foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                    if (overrider.HasValue && turn == 1)
                    {
                        Country actor = world.GetCountry(overrider.Value);
                        var overrides = new Dictionary<CountryId, float>();
                        foreach (TradePartner link in actor.TradePartners) { overrides[link.PartnerId] = CapRatePercent; }
                        decisions[overrider.Value] = new PolicyDecision { PartnerTariffOverrides = overrides };
                    }

                    if (overrider.HasValue && turn == resetAtTurn)
                    {
                        // The un-voted "Reset to Default" click, exactly as GameController performs it.
                        Country actor = world.GetCountry(overrider.Value);
                        foreach (TradePartner link in actor.TradePartners) { link.PlayerTariffOverride = -1f; }
                    }

                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }

                    if (overrider.HasValue)
                    {
                        record.ExpectationsBefore.Add((turn, world.GetCountry(overrider.Value).State.InflationExpectations));
                    }

                    sim.AdvanceTurn(decisions);

                    foreach (Country c in world.Countries)
                    {
                        FiscalTurnReport r = sim.GetLastFiscalReport(c.Id);
                        record.Snapshots[(turn, c.Id)] = new TurnSnapshot
                        {
                            Gdp = c.State.GDP,
                            TradeBalance = c.State.TradeBalance,
                            Inflation = c.State.Inflation,
                            Expectations = c.State.InflationExpectations,
                            Unemployment = c.State.Unemployment,
                            Approval = c.State.ApprovalRating,
                            DebtRatio = c.State.DebtToGdpRatio,
                            Rate = c.CurrencyZone.InterestRate,
                            TariffRevenue = r != null ? r.TariffRevenue : 0f,
                            PassThroughPp = r != null ? r.TariffPassThroughPp : 0f,
                            EventFired = sim.GetLastEvent(c.Id) != null
                        };
                    }

                    if (overrider.HasValue && turn == 1)
                    {
                        Country actor = world.GetCountry(overrider.Value);
                        foreach (TradePartner link in actor.TradePartners)
                        {
                            Country partner = world.GetCountry(link.PartnerId);
                            record.PartnerRateOnOverriderAtT1[link.PartnerId] = TradeSystem.GetTariffRate(partner, actor, world.TradeBlocs);
                        }
                    }
                }

                FieldInfo[] fields = typeof(EconomyState).GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (Country c in world.Countries)
                {
                    var values = new Dictionary<string, float>();
                    foreach (FieldInfo f in fields)
                    {
                        if (f.FieldType == typeof(float)) { values[f.Name] = (float)f.GetValue(c.State); }
                    }
                    record.EndFields[c.Id] = values;
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            return record;
        }

        private static bool ReportOverrider(CountryId id, RunRecord control, RunRecord exploit, float scale)
        {
            bool ok = true;
            TurnSnapshot c1 = control.At(1, id), e1 = exploit.At(1, id);
            TurnSnapshot c2 = control.At(2, id), e2 = exploit.At(2, id);
            TurnSnapshot c3 = control.At(3, id), e3 = exploit.At(3, id);
            TurnSnapshot cEnd = control.At(Turns, id), eEnd = exploit.At(Turns, id);

            Debug.Log($"TARIFFCOSTS EXPLOIT[{id}] scale {scale:F1}: take/turn {eEnd.TariffRevenue:F2} vs {cEnd.TariffRevenue:F2} control | " +
                      $"pass-through printed t2 {e2.PassThroughPp:+0.00;-0.00} pp (t1 {e1.PassThroughPp:+0.00;-0.00}, t3 {e3.PassThroughPp:+0.00;-0.00}) | " +
                      $"inflation t1 {e1.Inflation:F2} (ctrl {c1.Inflation:F2}) t2 {e2.Inflation:F2} (ctrl {c2.Inflation:F2}) t3 {e3.Inflation:F2} (ctrl {c3.Inflation:F2}) | " +
                      $"expectations t1 {e1.Expectations:F2} t2 {e2.Expectations:F2} t3 {e3.Expectations:F2} (ctrl t3 {c3.Expectations:F2}) | " +
                      $"trade balance t1 {e1.TradeBalance:F1} vs {c1.TradeBalance:F1} | rate t3 {e3.Rate:F2} vs {c3.Rate:F2}");
            Debug.Log($"TARIFFCOSTS EXPLOIT[{id}] scale {scale:F1}: GDP t2 {(e2.Gdp / c2.Gdp - 1f) * 100f:+0.00;-0.00}% t{Turns} {(eEnd.Gdp / cEnd.Gdp - 1f) * 100f:+0.00;-0.00}% | " +
                      $"unemployment t2 {e2.Unemployment - c2.Unemployment:+0.00;-0.00} t{Turns} {eEnd.Unemployment - cEnd.Unemployment:+0.00;-0.00} | " +
                      $"approval t2 {e2.Approval - c2.Approval:+0.0;-0.0} t{Turns} {eEnd.Approval - cEnd.Approval:+0.0;-0.0} | " +
                      $"debt-to-GDP t10 {control.At(10, id).DebtRatio:F1} -> {exploit.At(10, id).DebtRatio:F1}, t20 {control.At(20, id).DebtRatio:F1} -> {exploit.At(20, id).DebtRatio:F1}, " +
                      $"t30 {cEnd.DebtRatio:F1} -> {eEnd.DebtRatio:F1} (delta t30 {eEnd.DebtRatio - cEnd.DebtRatio:+0.0;-0.0} ratio-points)");

            var moved = new List<string>();
            foreach (KeyValuePair<string, float> kvp in control.EndFields[id])
            {
                if (exploit.EndFields[id][kvp.Key] != kvp.Value) { moved.Add(kvp.Key); }
            }
            Debug.Log($"TARIFFCOSTS EXPLOIT[{id}] scale {scale:F1}: {moved.Count} of {control.EndFields[id].Count} EconomyState fields moved at t{Turns}: {string.Join(", ", moved)}");

            SimulationRandom.Seed(Seed);
            World seedWorld = WorldFactory.CreateDefault();
            Country actor = seedWorld.GetCountry(id);
            foreach (TradePartner link in actor.TradePartners)
            {
                CountryId p = link.PartnerId;
                TurnSnapshot pc2 = control.At(2, p), pe2 = exploit.At(2, p);
                TurnSnapshot pcEnd = control.At(Turns, p), peEnd = exploit.At(Turns, p);
                float rateOnUs = exploit.PartnerRateOnOverriderAtT1[p];
                Debug.Log($"TARIFFCOSTS PARTNER[{id}->{p}] scale {scale:F1}: charges us {rateOnUs:F2}% at t1 | take/turn {peEnd.TariffRevenue:F2} vs {pcEnd.TariffRevenue:F2} (delta {peEnd.TariffRevenue - pcEnd.TariffRevenue:+0.00;-0.00}) | " +
                          $"own pass-through t2 {pe2.PassThroughPp:+0.00;-0.00} pp, inflation t2 {pe2.Inflation:F2} vs {pc2.Inflation:F2} | " +
                          $"approval t{Turns} {peEnd.Approval - pcEnd.Approval:+0.0;-0.0} | GDP t{Turns} {pcEnd.Gdp:F1} -> {peEnd.Gdp:F1} ({(peEnd.Gdp / pcEnd.Gdp - 1f) * 100f:+0.00;-0.00}%) | " +
                          $"debt-to-GDP t{Turns} {pcEnd.DebtRatio:F1} -> {peEnd.DebtRatio:F1} ({peEnd.DebtRatio - pcEnd.DebtRatio:+0.0;-0.0} ratio-points)");

                if (TradeCosts.RetaliationMirrorFraction > 0f && Mathf.Abs(rateOnUs - CapRatePercent) > Tolerance)
                {
                    ok = false;
                    Debug.LogError($"TARIFFCOSTS MIRROR: {p} charges {id} {rateOnUs:F3}% at t1 - the mirror is on but the cap was not mirrored.");
                }
            }

            if (TradeCosts.ImportPricePassThrough > 0f && e2.PassThroughPp <= Tolerance)
            {
                ok = false;
                Debug.LogError($"TARIFFCOSTS WEDGE: {id}'s pass-through at t2 is {e2.PassThroughPp:F4} pp with the constant on - the wedge did not print.");
            }

            return ok;
        }

        /// <summary>Sweden at the cap from t1, the un-voted reset before t3: the clamp-safe look-through identity at every event-free boundary, and the path printed so a ratchet would be visible.</summary>
        private static bool ReportRemovalCase(RunRecord control)
        {
            bool ok = true;
            RunRecord removal = RunWorld(CountryId.Sweden, RemovalResetTurn);
            CountryId id = CountryId.Sweden;
            for (int turn = 1; turn <= 6; turn++)
            {
                TurnSnapshot s = removal.At(turn, id);
                TurnSnapshot c = control.At(turn, id);
                float before = removal.ExpectationsBefore[turn - 1].ExpectationsBefore;
                float target = s.Inflation - s.PassThroughPp;
                float predicted = before + (target - before) * ExpectationsAdaptationSpeed;
                string identity;
                if (s.EventFired)
                {
                    identity = "identity skipped (a boundary event shocked inflation after expectations adapted)";
                }
                else
                {
                    bool holds = Mathf.Abs(s.Expectations - predicted) < Tolerance;
                    ok &= holds;
                    identity = holds ? "identity holds" : $"IDENTITY BROKEN (predicted {predicted:F4})";
                    if (!holds) { Debug.LogError($"TARIFFCOSTS REMOVAL: t{turn} expectations {s.Expectations:F4} vs predicted {predicted:F4} from before {before:F4}, print {s.Inflation:F4}, applied {s.PassThroughPp:F4}."); }
                }

                Debug.Log($"TARIFFCOSTS REMOVAL[Sweden] t{turn}: take {s.TariffRevenue:F2} | applied pass-through {s.PassThroughPp:+0.00;-0.00} pp | inflation print {s.Inflation:F2} (ctrl {c.Inflation:F2}) | expectations {before:F2} -> {s.Expectations:F2} (ctrl {c.Expectations:F2}) | {identity}");
            }

            return ok;
        }

        /// <summary>The overrides-only bill (every partner at the cap, the base rate restated) at the seed parliament: direction and verdict, per country.</summary>
        private static bool ReportVotes()
        {
            bool ok = true;
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            foreach (Country c in world.Countries)
            {
                var bill = new TradePolicyBill { NewBaseTariffRate = c.BaseTariffRate };
                foreach (TradePartner link in c.TradePartners) { bill.PartnerTariffOverrides[link.PartnerId] = CapRatePercent; }
                float direction = ParliamentSystem.GetTradeBillDirection(c, bill, world);
                bool passes = ParliamentSystem.WouldBillPass(c, direction);
                Debug.Log($"TARIFFCOSTS VOTE[{c.Id}]: overrides-only bill, every partner at {CapRatePercent:F0}% - direction {direction:+0.00;-0.00} points of average tariff -> {(passes ? "WOULD PASS" : "WOULD FAIL")} at the seed parliament{(Mathf.Approximately(direction, 0f) ? " (uncontested: direction 0 auto-passes)" : "")}.");
                if (TradeCosts.OverrideDirectionWeight > 0f && Mathf.Approximately(direction, 0f))
                {
                    ok = false;
                    Debug.LogError($"TARIFFCOSTS VOTE: {c.Id}'s overrides-only bill has direction 0 with the override weight on.");
                }
            }

            return ok;
        }
    }
}
