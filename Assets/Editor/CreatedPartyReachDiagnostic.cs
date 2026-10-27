using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// GATE-18 piece three, q22 (Elias's ruling of 2026-10-18, `docs/RULINGS_2026-10-18.md`; the audit's GA-4-2 and GA-4-3): <b>A CREATED PARTY WITH NO
    /// CAMPAIGN STAYS BELOW THE THRESHOLD WHATEVER ITS ORIGIN; ITS ORIGIN SETS ITS REACH; A REGIONAL PARTY'S VOTE COMES FROM ITS OWN REGIONAL BASE.</b>
    /// FAILS while any of these is false:
    /// <list type="number">
    /// <item><description><b>The table</b>: every origin carries a reach (`PartyOrigins.Reach`, [AUTHORED-DRAFT]) above zero and below one - so no origin's
    /// cap reaches the line - and the cap lifts to none at full awareness (`EntrantLayer.Ceiling`) and stands at the opening ceiling at the opening awareness.</description></item>
    /// <item><description><b>Inert with none registered</b>: each new term returns the very array it was handed, and Sweden's regions stand as the catalog has them.</description></item>
    /// <item><description><b>Below the line, whatever the origin</b>, in each country the game counts by a national line (Sweden, Germany, Poland), with no
    /// campaign: a created party of each origin at every point of a compass grid, and a Splinter of every real party at the largest slice the creation flow
    /// offers placed on its parent, takes less than the threshold in the idle prediction - and no seat in the count's allocation. The six origins at the
    /// electorate's centre (the creation flow's own default draft) are printed beside it, with their seats: GA-4-2's table re-run.</description></item>
    /// <item><description><b>A Regional party's vote is its region's</b> (Sweden): its share is zero in every other valkrets, its share inside its own valkrets
    /// times that valkrets' weight is its national share, and inside it it stays below the 12 % route's line with no campaign (GA-4-3's run re-run).</description></item>
    /// </list>
    /// Prints `=== CreatedPartyReachDiagnostic: q22 holds ===` and exits 0. Run:
    /// `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.CreatedPartyReachDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class CreatedPartyReachDiagnostic
    {
        /// <summary>CONVENTION: the compass grid the gate sweeps - each axis at these points of the CHES 0-10 scale.</summary>
        private static readonly float[] Grid = { 0f, 2.5f, 5f, 7.5f, 10f };

        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== CreatedPartyReachDiagnostic (GATE-18 q22): a created party's reach by origin, and a Regional party's base ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            using IDisposable created = CreatedParties.Scope();
            // the count draws its regional noise keyed on the master seed: the play key, and the live streams put back after
            SimulationRandom.LiveState live = SimulationRandom.CaptureLive();
            SimulationRandom.Seed(777);
            try
            {
                CreatedParties.Clear();

                // (1) the table
                var origins = (PartyOrigin[])Enum.GetValues(typeof(PartyOrigin));
                var reachLine = new List<string>();
                bool table = true;
                foreach (PartyOrigin o in origins)
                {
                    double r = PartyOrigins.ReachOf(o);
                    reachLine.Add(F("{0} {1:0.00}", o, r));
                    if (double.IsNaN(r) || r <= 0.0 || r >= 1.0) { table = false; }
                }
                Check(table, "every origin carries a reach above zero and below one of the line that binds it [AUTHORED-DRAFT]: " + string.Join(", ", reachLine));
                double c0 = 0.03, a0 = 0.15;
                Check(EntrantLayer.Ceiling(c0, a0, a0) == c0 && EntrantLayer.Ceiling(c0, a0, 1.0) == 1.0 && EntrantLayer.Ceiling(c0, a0, 0.5) > c0 && EntrantLayer.Ceiling(c0, a0, 0.5) < 1.0
                      && EntrantLayer.Ceiling(double.NaN, a0, a0) == 1.0,
                    F("the cap: {0} at the opening awareness, {1:0.0000} at awareness 0.5, {2} at full awareness; none without an opening ceiling", EntrantLayer.Ceiling(c0, a0, a0), EntrantLayer.Ceiling(c0, a0, 0.5), EntrantLayer.Ceiling(c0, a0, 1.0)));

                // (2) inert with none registered
                WorldClock.ApplyStart(CountryId.Sweden);
                NationalElection.TryCompatibility(CountryId.Sweden, out string[] k0, out double[] _, out double[] p0, out double[] _);
                var probe = new double[k0.Length];
                for (int i = 0; i < probe.Length; i++) { probe[i] = 1.0 / probe.Length; }
                RegionalVoteModel.RegionInput[] plain = SwedishRegions.Regions(k0);
                bool catalog = plain.All(r => r.PartyAvailable == null || r.PartyAvailable.All(x => x));
                Check(ReferenceEquals(EntrantLayer.LayerPrior(CountryId.Sweden, k0, p0), p0) && ReferenceEquals(EntrantLayer.RegionalAwareness(CountryId.Sweden, k0, probe), probe)
                      && ReferenceEquals(EntrantLayer.HoldToReach(CountryId.Sweden, k0, probe, null), probe) && catalog,
                    "none registered: the layer's prior, its awareness and the held shares are the very arrays handed; every party stands in every valkrets the catalog has it in");

                // (3) below the line, whatever the origin, in each country counted by a national line
                foreach (CountryId c in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland })
                {
                    WorldClock.ApplyStart(c);
                    CreatedParties.Clear();
                    PoliticalParty[] real = PartySystems.RealRoster(c);
                    double line = NationalElection.ThresholdOf(c);
                    string[] regions = PartyCreationFlow.Regions(c);
                    CreatedParty Make(PartyOrigin o)
                    {
                        var d = new CreatedParty { Country = c, Key = "NEW", Name = "Testpartiet", LeaderName = "A. Testsson" };
                        PartyCreationFlow.ChooseOrigin(d, o);
                        return d;
                    }
                    // K123 (Elias, 2026-10-25): the EXPECTED share - the prediction, no noise - is what q22 holds below the line, with its seats; the count at
                    // the declared regional noise is printed beside it, and a crossing by noise alone is a REPORTED line, never a failure
                    bool Measure(CreatedParty d, out double share, out int seats, out double countedShare, out int countedSeats)
                    {
                        share = double.NaN; seats = -1; countedShare = double.NaN; countedSeats = -1;
                        CreatedParties.Clear();
                        if (!CreatedParties.TryRegister(d, real, out string _)) { return false; }
                        if (!NationalElection.TryPredictShares(c, out Dictionary<string, double> shares) || !shares.TryGetValue(d.Key, out share)) { return false; }
                        DateTime day = SimulationManager.EpochDate;
                        ElectionRecord expectedRecord = NationalElection.Run(c, 0, shares, day);
                        seats = expectedRecord.Seats.TryGetValue(d.Key, out int s) ? s : 0;
                        // the review's M4: the count as play holds it (`ParliamentaryCount.Count`), the chamber allocated on its counted shares; the count
                        // goes to `Run` as its `counted` argument (s918, merged on s917's q3) so Sweden's 12 % route reads the counted valkretsar
                        ParliamentaryCount.Result counted = ParliamentaryCount.Count(c, day, shares, null, null, writeReadout: false);
                        if (counted != null && counted.Shares.TryGetValue(d.Key, out double cs))
                        {
                            countedShare = cs;
                            ElectionRecord countedRecord = NationalElection.Run(c, 0, counted.Shares, day, 1.0, counted);
                            countedSeats = countedRecord.Seats.TryGetValue(d.Key, out int k) ? k : 0;
                        }
                        return true;
                    }

                    // the six origins at the electorate's centre, the creation flow's default draft - GA-4-2's table
                    var centre = new List<string>();
                    PartySystems.TryElectorate(c, out VoteModel.Electorate electorate, out double _);
                    foreach (PartyOrigin o in origins)
                    {
                        CreatedParty d = Make(o);
                        d.LrEcon = (float)electorate.MuEcon; d.Galtan = (float)electorate.MuSoc;
                        if (o == PartyOrigin.Splinter) { PoliticalParty largest = real.OrderByDescending(p => p.SeedSeats).First(); d.ParentKey = largest.Abbrev; d.PlaceOn(largest); }
                        if (o == PartyOrigin.Regional && regions.Length > 0) { d.Region = regions[0]; }
                        centre.Add(Measure(d, out double share, out int seats, out double counted, out int countedSeats)
                            ? F("{0} {1:0.00} % {2} seat(s) (counted at the declared noise {3:0.00} %, {4} seat(s))", o, share * 100.0, seats, counted * 100.0, countedSeats) : F("{0} not measured", o));
                    }
                    sb.Append(F("    measured  {0}, no campaign, at the electorate's centre (the flow's default draft; a Splinter of the largest party on its positions at the flow's default slice): {1}\n", c, string.Join("; ", centre)));

                    // the gate: every origin over the grid, and every real party's Splinter at the largest slice
                    double worst = 0.0, worstCounted = 0.0;
                    string worstWhere = "none", worstCountedWhere = "none";
                    int measured = 0, seated = 0, unmeasured = 0, uncounted = 0;
                    var crossings = new List<string>();
                    void Gate(CreatedParty d, string where)
                    {
                        if (!Measure(d, out double share, out int seats, out double counted, out int countedSeats)) { unmeasured++; return; }
                        measured++;
                        if (seats > 0) { seated++; }
                        if (share > worst) { worst = share; worstWhere = where; }
                        if (double.IsNaN(counted)) { uncounted++; return; }
                        if (counted > worstCounted) { worstCounted = counted; worstCountedWhere = where; }
                        if (counted >= line || countedSeats > 0) { crossings.Add(F("{0}: expected {1:0.00} %, counted {2:0.00} %, {3} seat(s)", where, share * 100.0, counted * 100.0, countedSeats)); }
                    }
                    foreach (PartyOrigin o in origins)
                    {
                        if (o == PartyOrigin.Splinter) { continue; }
                        foreach (float x in Grid)
                        {
                            foreach (float y in Grid)
                            {
                                CreatedParty d = Make(o);
                                d.LrEcon = x; d.Galtan = y;
                                if (o == PartyOrigin.Regional && regions.Length > 0) { d.Region = regions[0]; }
                                Gate(d, F("{0} at ({1}, {2})", o, x, y));
                            }
                        }
                    }
                    double slice = PartyCreationFlow.Slices.Max();
                    foreach (PoliticalParty parent in real)
                    {
                        if (!parent.HasPosition) { continue; }
                        CreatedParty d = Make(PartyOrigin.Splinter);
                        d.ParentKey = parent.Abbrev; d.InheritedSlice = slice; d.PlaceOn(parent);
                        Gate(d, F("a Splinter of {0} at a slice of {1:0.00} on its positions", parent.Abbrev, slice));
                    }
                    Check(unmeasured == 0 && measured > 0 && worst < line && seated == 0,
                        F("{0}: with no campaign no created party's EXPECTED share reaches the {1:0.0} % line (K123: the prediction, no noise) - {2} measured (every origin over the compass grid, a Splinter of every placed party at the flow's largest slice), the highest {3:0.00} % ({4}), {5} seated on it{6}",
                            c, line * 100.0, measured, worst * 100.0, worstWhere, seated, unmeasured > 0 ? F(", {0} NOT MEASURED", unmeasured) : string.Empty));
                    sb.Append(F("    measured  {0}: the count at the declared regional noise beside it - the highest counted share {1:0.00} % ({2}){3}\n",
                        c, worstCounted * 100.0, worstCountedWhere, uncounted > 0 ? F("; {0} not counted (no count for this country)", uncounted) : string.Empty));
                    if (crossings.Count > 0)
                    {
                        sb.Append(F("    REPORTED  {0}: {1} created part(ies) cross the {2:0.0} % line by the count's noise alone (K123: reported, not a failure) - {3}\n",
                            c, crossings.Count, line * 100.0, string.Join("; ", crossings)));
                    }
                }

                // (4) a Regional party's vote is its region's (Sweden: the regions the creation flow offers)
                WorldClock.ApplyStart(CountryId.Sweden);
                CreatedParties.Clear();
                PoliticalParty[] sweden = PartySystems.RealRoster(CountryId.Sweden);
                string[] valkretsar = PartyCreationFlow.Regions(CountryId.Sweden);
                foreach (string region in new[] { valkretsar.First(), valkretsar.Last() })
                {
                    CreatedParties.Clear();
                    var reg = new CreatedParty { Country = CountryId.Sweden, Key = "REG", Name = "Regionpartiet", LeaderName = "A. Testsson" };
                    PartyCreationFlow.ChooseOrigin(reg, PartyOrigin.Regional);
                    reg.Region = region;
                    reg.LrEcon = 5f; reg.Galtan = 5f;
                    CreatedParties.TryRegister(reg, sweden, out _);
                    if (!NationalElection.TryPredictShares(CountryId.Sweden, out Dictionary<string, double> shares) || NationalElection.LastRegionalShares == null) { Check(false, "a Regional party in " + region + ": no prediction or no regions derived"); continue; }
                    int col = NationalElection.LastRegionalKeys.ToList().IndexOf("REG");
                    int own = Array.IndexOf(NationalElection.LastRegionalNames, region);
                    double total = NationalElection.LastRegionalWeights.Sum();
                    double outside = 0.0;
                    for (int r = 0; r < NationalElection.LastRegionalShares.Length; r++) { if (r != own) { outside = Math.Max(outside, NationalElection.LastRegionalShares[r][col]); } }
                    double inside = NationalElection.LastRegionalShares[own][col];
                    double rebuilt = inside * NationalElection.LastRegionalWeights[own] / total;
                    double regionalLine = NationalElection.RegionalThresholdOf(CountryId.Sweden);
                    Check(col >= 0 && own >= 0 && outside == 0.0 && Math.Abs(rebuilt - shares["REG"]) < 1e-9 && inside < regionalLine && shares["REG"] < NationalElection.ThresholdOf(CountryId.Sweden),
                        F("a Regional party of {0} at (5, 5), no campaign: {1:0.00} % inside its valkrets, 0 in every other (the largest elsewhere {2:0.00} %), {3:0.000} % of the nation's vote = its valkrets' share times its weight {4:0.000} % - below the {5:0} % route's line and the {6:0} % national line",
                            region, inside * 100.0, outside * 100.0, shares["REG"] * 100.0, rebuilt * 100.0, regionalLine * 100.0, NationalElection.ThresholdOf(CountryId.Sweden) * 100.0));
                }
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally
            {
                SimulationRandom.Reinstate(live);   // first (the delta review's L-e): nothing below can skip it
                // the prediction derives the regions as a side effect: derive them again on the real roster, so no check after this reads a created party's
                CreatedParties.Clear();
                WorldClock.ApplyStart(CountryId.Sweden);
                NationalElection.TryPredictShares(CountryId.Sweden, out _);
            }

            if (failures > 0) { Debug.LogError($"CREATED PARTY REACH: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            sb.Append("=== CreatedPartyReachDiagnostic: q22 holds ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
