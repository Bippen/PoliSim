using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using PoliSim.UI;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// D-ST (boards 23a-23c, `COMPLETED.md` §701): **THE STATISTICS SHEET'S RULES, ASSERTED AS FUNCTIONS AND ON THE SEED WORLD.**
    /// (a) The Δ prints in the reading's own unit - money in money, pp for a percentage, a plain number for a score - never a relative per cent of a
    /// rate: the built screen's unemployment Δ −27.8 % (a relative change of a rate) must now read Δ −2.2 pp; a FLAT move prints its zero unsigned;
    /// the true minus. (b) A held seed's leading run is not a history: approval's and the trade balance's seeds are the seed world's own values
    /// (asserted on every country, so a changed seed fails here, not on a chart), and the first live point is found. (c) An axis spans at least four
    /// printed steps (poverty's 9.0 · 9.0). (d) The sector bar sums to GDP: Sweden's eight are 43.3 %, OTHER 56.7 %. (e) GDP growth is read off the
    /// kept series and is null - ABSENT - until it holds a year; the card's line is the kept series', not a controller's default. (f) The slip book
    /// holds every anchor the sheet draws on Sweden's seed world, and every term a slip marks has its slip.
    /// </summary>
    public static class StatsSheetCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== StatsSheetCheck (D-ST): the Δ in the reading's unit, held seeds, axis span, the sector remainder, growth off the series, the slip book ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
            try
            {
                // (a) THE Δ IN ITS OWN UNIT
                string unemployment = StatsReadings.DeltaText(8.0f, 5.8f, ReadingUnit.Percent, null);
                string approval = StatsReadings.DeltaText(50.0f, 46.1f, ReadingUnit.Score, null);
                string poverty = StatsReadings.DeltaText(9.00f, 9.04f, ReadingUnit.Percent, null);
                string gdp = StatsReadings.DeltaText(620f, 671f, ReadingUnit.Money, MoneyUnit.Billions);
                string balance = StatsReadings.DeltaText(0f, -2.92f, ReadingUnit.Money, MoneyUnit.Billions);
                string rate = StatsReadings.DeltaText(2.50f, 2.25f, ReadingUnit.Percent, null, decimals: 2);
                Check(unemployment == "Δ −2.2 pp", F("a rate's change in points: 8.0 → 5.8 prints '{0}' (the built screen printed a relative change, −27.8 %)", unemployment));
                Check(approval == "Δ −3.9", F("a score's change as a plain number: 50.0 → 46.1 prints '{0}'", approval));
                Check(poverty == "Δ 0.0 pp" && StatsReadings.IsFlat(0.04f) && !StatsReadings.IsFlat(0.06f),
                    F("a FLAT move - under half its printed step - prints its zero unsigned: 9.00 → 9.04 prints '{0}' (the build had inked it red)", poverty));
                Check(gdp.StartsWith("Δ +US$", StringComparison.Ordinal) && balance.StartsWith("Δ −US$", StringComparison.Ordinal),
                    F("money in its money, the true minus: '{0}', '{1}'", gdp, balance));
                Check(rate == "Δ −0.25 pp", F("a policy rate at its two decimals: 2.50 → 2.25 prints '{0}'", rate));

                // (b) HELD SEEDS
                Check(StatsReadings.FirstLiveIndex(new List<float> { 50f, 50f, 50f, 48.2f, 47f }, 50f) == 3
                    && StatsReadings.FirstLiveIndex(new List<float> { 49f, 50f }, 50f) == 0
                    && StatsReadings.FirstLiveIndex(new List<float> { 50f, 50f }, 50f) == 2
                    && StatsReadings.FirstLiveIndex(new List<float> { 50f, 50f }, null) == 0,
                    "the first live point: after the seed's leading run; 0 where the series does not open on it; the count where it has not gone live; 0 with no seed");
                using (SimulationManager.EpochScope())
                {
                    SimulationRandom.Seed(777);
                    EnergyMarket.ResetCalibration();
                    World world = WorldFactory.CreateDefault();
                    int seeded = 0;
                    var wrong = new List<string>();
                    foreach (Country c in world.Countries)
                    {
                        seeded++;
                        if (c.State.ApprovalRating != StatsSlips.ApprovalSeed) { wrong.Add(F("{0} approval {1}", c.Name, c.State.ApprovalRating)); }
                        if (c.State.TradeBalance != StatsSlips.TradeBalanceSeed) { wrong.Add(F("{0} trade balance {1}", c.Name, c.State.TradeBalance)); }
                    }
                    Check(seeded == 6 && wrong.Count == 0, F("the held seeds are the seed world's own: every country opens at approval {0} and a trade balance of {1} ({2} countries){3}",
                        StatsSlips.ApprovalSeed, StatsSlips.TradeBalanceSeed, seeded, wrong.Count > 0 ? " - " + string.Join(", ", wrong) : string.Empty));

                    // (d) THE SECTOR REMAINDER
                    Country sweden = world.GetCountry(CountryId.Sweden);
                    var shares = new List<float>();
                    foreach (Sector s in sweden.Sectors) { shares.Add(s.OutputShareOfGdp); }
                    float other = StatsReadings.SectorRemainderPercent(shares);
                    Check(Math.Abs(other - 56.7f) < 0.05f, F("Sweden's eight sectors are {0:F1} % of GDP; OTHER is {1:F1} % - the bar sums to its whole (the built bar drew the eight as 100 %)", 100f - other, other));

                    // (e) GROWTH OFF THE KEPT SERIES
                    Check(StatsReadings.YearOnYearGrowthPercent(sweden.History.Gdp.Quarterly) == null,
                        F("at the start the kept series holds {0} point(s): growth is null - ABSENT on the card, never 0.00", sweden.History.Gdp.Quarterly.Count));
                    float? yoy = StatsReadings.YearOnYearGrowthPercent(new List<float> { 100f, 101f, 102f, 103f, 104f });
                    Check(yoy.HasValue && Math.Abs(yoy.Value - 4f) < 1e-4f && StatsReadings.YearOnYearGrowthPercent(new List<float> { 100f, 101f, 102f, 103f }) == null,
                        "growth is four quarters on four: 100 → 104 over five points is +4.0 %; four points hold no year");

                    // (f) THE BOOK
                    Country partner = world.GetCountry(CountryId.USA);
                    PeopleSlips.Book book = StatsSlips.Build(sweden, world, partner, 0, 0, null);
                    string[] drawn =
                    {
                        "title", "card:gdp/growth", "card:history", "fiscal:head", "fiscal:axis", "fiscal:percapita", "fiscal:notyet", "sector:head", "sector:other",
                        "series:head", "series:pager", "chart:gdp/delta", "chart:gdp/name", "chart:unemployment/delta", "chart:inflation/delta", "chart:approval/delta",
                        "chart:poverty/delta", "chart:debt/delta", "policies:head", "policies:nil", "society:head", "society:index", "society:Youth unemployment",
                        "society:Life expectancy", "society:Income inequality (Gini)", "society:Real wages", "society:Productivity", "society:Housing overburden",
                        "society:Homeownership", "society:House prices", "map", "pair:head", "pair:home", "pair:partner", "pair:code:USA", "pair:code:Germany", "readings:head",
                        "links:head", "links:out", "links:in", "links:none", "links:zero", "links:tariff", "links:bloc", "links:currency", "stance:head", "stance:fiscal",
                        "stance:regulation", "relations", "trade:head", "trade:pager", "trade:passthrough", "chart:trade/delta",
                    };
                    var missing = new List<string>();
                    foreach (string id in drawn) { if (!book.Anchors.ContainsKey(id)) { missing.Add(id); } }
                    Check(missing.Count == 0, F("the book holds every anchor the sheet draws on Sweden's seed world ({0} anchors){1}", drawn.Length, missing.Count > 0 ? " - missing " + string.Join(", ", missing) : string.Empty));
                    var unmarked = new List<string>();
                    foreach (SlipContent c in book.Anchors.Values)
                    {
                        foreach (string line in c.Lines) { foreach (string term in SlipContent.TermsIn(line)) { if (!book.Terms.ContainsKey(term)) { unmarked.Add(term); } } }
                    }
                    Check(unmarked.Count == 0, "every term a slip marks has its own slip" + (unmarked.Count > 0 ? " - " + string.Join(", ", unmarked) : string.Empty));
                    Check(book.Anchors["card:gdp/growth"].Head == SymbolRegistry.Word(Symbol.Absent), F("the GDP card's line at the start is ABSENT's slip ('{0}')", book.Anchors["card:gdp/growth"].Head));
                }

                // (c) THE AXIS SPAN
                float min = 9.0f, max = 9.1f;
                StatsReadings.WidenToPrintedSteps(ref min, ref max);
                float wideMin = 5.0f, wideMax = 8.0f;
                StatsReadings.WidenToPrintedSteps(ref wideMin, ref wideMax);
                Check(Math.Abs(max - min - 0.4f) < 1e-4f && Math.Abs((min + max) * 0.5f - 9.05f) < 1e-4f && wideMin == 5.0f && wideMax == 8.0f,
                    F("an axis spans at least four printed steps: 9.0-9.1 widens to {0:F2}-{1:F2}; 5.0-8.0 is left alone", min, max));
            }
            catch (Exception e)
            {
                failures++;
                sb.Append("    FAIL      threw: ").Append(e).Append('\n');
            }

            sb.Append(failures == 0 ? "  CLEAN\n" : $"  {failures} FAILURE(S)\n");
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
