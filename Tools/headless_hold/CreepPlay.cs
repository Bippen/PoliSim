using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;

// K275 (2026-10-29): Tools/headless_hold/creep_play.sh's entry - see the script's header. args: <none|player> <years> <seed>
public static class CreepPlay
{
    static TaxLine Find(Country c) { foreach (TaxLine l in c.TaxLines) { if (l.Type == TaxType.IncomeTax) { return l; } } return null; }
    static string F(double d) => d.ToString("F4", CultureInfo.InvariantCulture);

    public static void Main(string[] args)
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        UnityEngine.Debug.unityLogger.logEnabled = false;
        string player = args[0];
        int years = int.Parse(args[1], CultureInfo.InvariantCulture), seed = int.Parse(args[2], CultureInfo.InvariantCulture);
        SimulationRandom.Seed(seed);
        using System.IDisposable epoch = SimulationManager.EpochScope();
        CountryId start = player == "none" ? CountryId.Sweden : (CountryId)System.Enum.Parse(typeof(CountryId), player);
        PoliSim.Elections.WorldClock.ApplyStart(start);
        World world = WorldFactory.CreateDefault();
        var sim = new SimulationManager();
        sim.SetWorld(world);
        if (player != "none") { sim.PlayerCountryId = start; }
        foreach (Country c in world.Countries) { TaxLine l = Find(c); if (l == null) continue; System.Console.WriteLine($"SEED {c.Id} seed {l.RateSeed:F3} aer {c.IncomeTaxSeedAer:F3} top {TaxSchedule.StatuteTopMarginalRate(c.Id):F3} bound {F(TaxSchedule.CreepBound(c, l))} creep {TaxSchedule.StatuteCreep(c, l):R} factor {TaxSchedule.CreepCapFactor(c, l):R}"); }
        var decisions = new Dictionary<CountryId, PolicyDecision>();
        foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
        double aiMax = 0; string aiMaxAt = ""; int aiBinds = 0;
        for (int year = 1; year <= years; year++)
        {
            for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
            if (player != "none") { decisions[start] = PolicyDecision.None(); }
            sim.AdvanceTurn(decisions);
            foreach (Country c in world.Countries)
            {
                TaxLine l = Find(c); if (l == null || !TaxSchedule.Responds(c.Id)) continue;
                double creep = TaxSchedule.StatuteCreep(c, l), bound = TaxSchedule.CreepBound(c, l);
                double unc = TaxSchedule.YieldRatioUncapped(c, l, l.Rate), cap = TaxSchedule.YieldRatio(c, l, l.Rate);
                FiscalTurnReport r = sim.GetLastFiscalReport(c.Id);
                double n = System.Math.Max(1e-6, (double)c.State.NominalGdp);
                bool isPlayer = player != "none" && c.Id == start;
                if (!isPlayer && TaxSchedule.CreepCapped(c.Id)) { if (creep / bound > aiMax) { aiMax = creep / bound; aiMaxAt = c.Id + " y" + year; } if (TaxSchedule.CreepCapFactor(c, l) < 1.0) aiBinds++; }
                if (isPlayer || (player == "none" && (year % 10 == 0)))
                    System.Console.WriteLine($"Y{year} {c.Id} rate {l.Rate:F2} inc {F(TaxSchedule.IncomeScale(c))} thr {F(TaxSchedule.ThresholdScale(c))} creep {F(creep)} bound {F(bound)} uncapped {F(unc)} capped {F(cap)} take {F(l.RateSeed * cap)} eff {TaxSchedule.EffectiveRate(c, l, l.Rate):F2} effUncapped {TaxSchedule.EffectiveRateUncapped(c, l, l.Rate):F2} participation {MacroSystem.LaborTaxParticipationTerm(c):F3} PITgdp {F(100.0 * TaxBases.Revenue(c, l) / n)} rev {F(r != null ? 100.0 * r.Revenue / n : 0)} debt {F(c.State.DebtToGdpRatio)}");
                if (isPlayer && (year == 1 || year == 40 || year == 100))
                {
                    TaxLine up = l.Clone(); TaxLine dn = l.Clone();
                    double c0 = TaxSchedule.YieldRatio(c, l, l.Rate), cu = TaxSchedule.YieldRatio(c, up, l.Rate + 1f), cd = TaxSchedule.YieldRatio(c, dn, l.Rate - 1f);
                    double u0 = TaxSchedule.YieldRatioUncapped(c, l, l.Rate), uu = TaxSchedule.YieldRatioUncapped(c, up, l.Rate + 1f), ud = TaxSchedule.YieldRatioUncapped(c, dn, l.Rate - 1f);
                    System.Console.WriteLine($"  PROBE y{year} rise capped {cu / c0:R} uncapped {uu / u0:R} | cut capped {cd / c0:R} uncapped {ud / u0:R} | kink capped {(cu - c0) / (c0 - cd):R} uncapped {(uu - u0) / (u0 - ud):R}");
                    double s0 = l.RateSeed - c.IncomeTaxSeedAer, e0 = TaxSchedule.EffectiveRate(c, l, l.Rate) - s0, f0 = TaxSchedule.EffectiveRateUncapped(c, l, l.Rate) - s0;
                    System.Console.WriteLine($"  PROBE y{year} effective rate: rise capped {(TaxSchedule.EffectiveRate(c, l, l.Rate + 1f) - s0) / e0:R} uncapped {(TaxSchedule.EffectiveRateUncapped(c, l, l.Rate + 1f) - s0) / f0:R} | cut capped {(TaxSchedule.EffectiveRate(c, l, l.Rate - 1f) - s0) / e0:R} uncapped {(TaxSchedule.EffectiveRateUncapped(c, l, l.Rate - 1f) - s0) / f0:R}");
                    List<TaxSchedule.SubRow> rows = TaxSchedule.SubRows(TaxSchedule.Of(c.Id));
                    int top = -1; for (int i = 0; i < rows.Count; i++) { if (rows[i].Adjustable && (rows[i].Kind == TaxSubRowKind.Band || rows[i].Kind == TaxSubRowKind.Ramp)) top = i; }
                    var z = new float[rows.Count]; for (int i = 0; i < z.Length; i++) z[i] = -1f; z[top] = 0f;
                    TaxLine zt = l.Clone(); zt.BracketRates = z;
                    double cz = TaxSchedule.YieldRatio(c, zt, l.Rate), uz = TaxSchedule.YieldRatioUncapped(c, zt, l.Rate);
                    System.Console.WriteLine($"  PROBE y{year} top band 0: capped {cz:R} (x{cz / c0:R}) uncapped {uz:R} (x{uz / u0:R}) revenue {TaxSchedule.RateSeedOf(l) / 100.0 * TaxBases.Base(c, TaxType.IncomeTax) * cz:F3} bn against {TaxBases.Revenue(c, l):F3} bn standing");
                }
            }
        }
        System.Console.WriteLine($"AI max creep/bound {aiMax:F4} at {aiMaxAt}; AI binds {aiBinds}");
    }
}
