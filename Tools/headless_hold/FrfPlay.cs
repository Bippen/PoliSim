using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;

// K251 (2026-10-29): Tools/headless_hold/frf_play.sh's entry - see the script's header. args: <on|off> <none|player> <years> <seed>
public static class FrfPlay
{
    public static void Main(string[] args)
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        UnityEngine.Debug.unityLogger.logEnabled = false;
        PoliSim.Data.PensionBenefitRatio.ProbeSuspended = System.Environment.GetEnvironmentVariable("PBR_PROBE") == "1";   // pbr (the prereg's (A0) 4): PBR_PROBE=1 holds the benefit ratio flat - the neutrality run
        string mode = args[0], player = args[1];
        int years = int.Parse(args[2], CultureInfo.InvariantCulture), seed = int.Parse(args[3], CultureInfo.InvariantCulture);
        if (mode == "off") { SimulationManager.SetFiscalReactionPairForSweep(0f, 1f, 1f); }
        else if (mode != "on") { System.Console.Error.WriteLine("FRF PLAY: the mode is on or off"); System.Environment.Exit(2); }
        SimulationRandom.Seed(seed);
        using System.IDisposable epoch = SimulationManager.EpochScope();
        CountryId start = player == "none" ? CountryId.Sweden : (CountryId)System.Enum.Parse(typeof(CountryId), player);
        PoliSim.Elections.WorldClock.ApplyStart(start);
        World world = WorldFactory.CreateDefault();
        var sim = new SimulationManager();
        sim.SetWorld(world);
        if (player != "none") { sim.PlayerCountryId = start; }
        var decisions = new Dictionary<CountryId, PolicyDecision>();
        foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
        var multiplier = typeof(SimulationManager).GetMethod("GetFiscalReactionMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Console.WriteLine("mode,player,year,country,debt_pct,revenue_pct,balance_pct,primary_pct,multiplier,rating,crisis_phase,crises,guard_absorbed_bn");
        for (int year = 1; year <= years; year++)
        {
            for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
            if (player != "none") { decisions[start] = PolicyDecision.None(); }
            sim.AdvanceTurn(decisions);
            foreach (Country c in world.Countries)
            {
                FiscalTurnReport r = sim.GetLastFiscalReport(c.Id);
                double n = System.Math.Max(1e-6, (double)c.State.NominalGdp);
                float balance = r != null ? r.FlowBalance : 0f, revenue = r != null ? r.Revenue : 0f, interest = r != null ? r.InterestOnDebt : 0f;
                float m = (float)multiplier.Invoke(sim, new object[] { c });
                System.Console.WriteLine(string.Join(",", mode, player, year.ToString(CultureInfo.InvariantCulture), c.Id.ToString(),
                    c.State.DebtToGdpRatio.ToString("F2", CultureInfo.InvariantCulture), (revenue / n * 100).ToString("F2", CultureInfo.InvariantCulture),
                    (balance / n * 100).ToString("F2", CultureInfo.InvariantCulture), ((balance + interest) / n * 100).ToString("F2", CultureInfo.InvariantCulture),
                    m.ToString("F3", CultureInfo.InvariantCulture), c.Rating.Rating.ToString(), c.Crisis != null ? c.Crisis.Phase.ToString() : "None",
                    (c.Crisis != null ? c.Crisis.Crises : 0).ToString(CultureInfo.InvariantCulture), (c.Crisis != null ? c.Crisis.GuardAbsorbedBillions : 0f).ToString("F1", CultureInfo.InvariantCulture)));
            }
        }
    }
}
