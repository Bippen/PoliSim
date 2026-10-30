using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;

// K269 (a) (2026-10-29): Tools/headless_hold/player_book.sh's entry - see the script's header. args: <play|nofrf|unchanged> <players, comma> <years> <seeds, comma>
public static class PlayerBook
{
    static string F(double d) => d.ToString("F2", CultureInfo.InvariantCulture);

    public static void Main(string[] args)
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        UnityEngine.Debug.unityLogger.logEnabled = false;
        string mode = args[0];
        if (mode != "play" && mode != "nofrf" && mode != "unchanged") { System.Console.Error.WriteLine("PLAYER BOOK: the mode is play, nofrf or unchanged"); System.Environment.Exit(2); }
        string[] players = args[1].Split(',');
        int years = int.Parse(args[2], CultureInfo.InvariantCulture);
        string[] seeds = args[3].Split(',');
        var multiplier = typeof(SimulationManager).GetMethod("GetFiscalReactionMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Console.WriteLine("mode,seed,player,year,debt_pct,revenue_pct,multiplier,pit_pct,spending_pct,lines_pct,driverless_pct,caseload_pct,pension_pct,health_pct,interest_pct,primary_pct,unemployment,guard_bn,crisis_phase");
        foreach (string seedText in seeds)
        {
            foreach (string player in players)
            {
                SimulationManager.ResetFiscalReactionPair();
                if (mode == "nofrf") { SimulationManager.SetFiscalReactionPairForSweep(0f, 1f, 1f); }
                int seed = int.Parse(seedText, CultureInfo.InvariantCulture);
                SimulationRandom.Seed(seed);
                using System.IDisposable epoch = SimulationManager.EpochScope();
                CountryId start = (CountryId)System.Enum.Parse(typeof(CountryId), player);
                PoliSim.Elections.WorldClock.ApplyStart(start);
                World world = WorldFactory.CreateDefault();
                var sim = new SimulationManager();
                sim.SetWorld(world);
                sim.PlayerCountryId = start;
                sim.NoPolicyChangeBook = mode == "unchanged";
                Country c = world.GetCountry(start);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 0; year <= years; year++)
                {
                    if (year > 0)
                    {
                        for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                        decisions[start] = PolicyDecision.None();
                        sim.AdvanceTurn(decisions);
                    }
                    FiscalTurnReport r = year > 0 ? sim.GetLastFiscalReport(start) : null;
                    double n = System.Math.Max(1e-6, (double)c.State.NominalGdp);
                    double lines = 0, driverless = 0, caseload = 0, pension = 0, health = 0;
                    foreach (SpendingLine l in c.SpendingLines)
                    {
                        lines += l.Amount;
                        if (SpendingDrivers.Of(l.Category) == SpendingDriver.None) { driverless += l.Amount; } else { caseload += l.Amount; }
                        if (l.Category == SpendingCategory.SocialSecurity) { pension += l.Amount; }
                        if (SpendingDrivers.Of(l.Category) == SpendingDriver.AgeCostIndex) { health += l.Amount; }
                    }
                    double pit = 0;
                    foreach (TaxLine t in c.TaxLines) { if (t.Type == TaxType.IncomeTax) { pit += TaxBases.Revenue(c, t); } }
                    if (year == 0 || year == 5 || year == 10 || year == 30 || year == 50 || year == 100)
                    {
                        foreach (SpendingLine l in c.SpendingLines)
                        {
                            SpendingDriver d = SpendingDrivers.Of(l.Category);
                            System.Console.WriteLine(string.Join(",", "LINE", mode, seedText, player, year.ToString(CultureInfo.InvariantCulture), l.Category.ToString(), d.ToString(),
                                (100.0 * l.Amount / n).ToString("F3", CultureInfo.InvariantCulture), SpendingDrivers.Level(d, c).ToString("G6", CultureInfo.InvariantCulture)));
                        }
                        System.Console.WriteLine(string.Join(",", "MACRO", mode, seedText, player, year.ToString(CultureInfo.InvariantCulture), "realwage", c.State.RealWageIndex.ToString("G6", CultureInfo.InvariantCulture),
                            "price", c.State.PriceLevel.ToString("G6", CultureInfo.InvariantCulture), "gdp", c.State.GDP.ToString("G6", CultureInfo.InvariantCulture),
                            "potential", c.State.PotentialGDP.ToString("G6", CultureInfo.InvariantCulture), "pop", c.State.Population.ToString("G6", CultureInfo.InvariantCulture)));
                    }
                    float m = (float)multiplier.Invoke(sim, new object[] { c });
                    System.Console.WriteLine(string.Join(",", mode, seedText, player, year.ToString(CultureInfo.InvariantCulture),
                        F(c.State.DebtToGdpRatio), r != null ? F(100.0 * r.Revenue / n) : "", m.ToString("F3", CultureInfo.InvariantCulture), F(100.0 * pit / n),
                        r != null ? F(100.0 * r.TotalSpending / n) : "", F(100.0 * lines / n), F(100.0 * driverless / n), F(100.0 * caseload / n), F(100.0 * pension / n), F(100.0 * health / n),
                        r != null ? F(100.0 * r.InterestOnDebt / n) : "", r != null ? F(100.0 * ((double)r.BudgetBalance + r.InterestOnDebt) / n) : "",
                        F(c.State.Unemployment), (c.Crisis != null ? c.Crisis.GuardAbsorbedBillions : 0f).ToString("F1", CultureInfo.InvariantCulture),
                        c.Crisis != null ? c.Crisis.Phase.ToString() : "None"));
                }
            }
        }
        SimulationManager.ResetFiscalReactionPair();
    }
}
