using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// GATE-18 piece three, q25 (Elias's ruling of 2026-10-18, `docs/RULINGS_2026-10-18.md`: "the tactical layer reads the polls' mean, not one tracker";
    /// GA-5-2: one published tracker of n = 600 decided whether S lends V a point): <b>THE POLLS' MEAN THE TACTICAL LAYER BELIEVES</b>.
    /// <list type="number">
    /// <item><description><b>The mean, planted</b> (`PollingSystem.Mean`): the polls fielded in the window up to the day and no other, each share their
    /// plain mean, the sample pooled, each margin of error the pooled sample's at the mean share; nothing where no poll falls in the window or the polls
    /// cover different parties.</description></item>
    /// <item><description><b>The series, on a staged campaign</b> (`LiveCampaignSetup.Sweden`, seed 777, stepped day by day as the game steps it): every
    /// published tracker is kept, the last of them is the run's `PublicPoll`, and `NationalElection.TacticalBelief` on the polling day is the mean of the
    /// trackers in `NationalElection.TacticalBeliefWindowDays`.</description></item>
    /// <item><description><b>GA-5-2, measured</b>: the staged run's final shares as the truth, its smallest party planted just over the threshold, the
    /// tracker series drawn again on fixed seeds - the spread of the counted shares the tactical layer gives when it believes the last tracker, against
    /// when it believes the mean. The mean's spread must be the smaller for the threatened party; both spreads and the seat ranges are printed.</description></item>
    /// </list>
    /// Prints `=== TacticalBeliefDiagnostic: ALL CHECKS PASS ===` and exits 0 when every check holds. Run:
    /// `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.TacticalBeliefDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class TacticalBeliefDiagnostic
    {
        /// <summary>CONVENTION: how many tracker series GA-5-2's measurement redraws, and the first of their fixed seeds.</summary>
        private const int Replications = 200;
        private const int FirstSeed = 9100;
        /// <summary>CONVENTION: the planted threatened party's true share - just over the 4 % line, where a tracker's noise decides the layer.</summary>
        private const double PlantedShare = 0.046;

        private static int _failures;
        private static StringBuilder _sb;

        private static void Check(bool ok, string what) { if (!ok) { _failures++; } _sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            _failures = 0;
            _sb = new StringBuilder("=== TacticalBeliefDiagnostic (GATE-18 piece three, q25): the tactical layer believes the polls' mean ===\n");
            Action restore = NationalElection.KeepRegionalReadout();
            // the review's L-8: the staged campaign re-seeds the process streams - the live generator is taken and put back, so a check after this one in a
            // batch draws what it would have drawn
            SimulationRandom.LiveState live = SimulationRandom.CaptureLive();
            try
            {
                Planted();
                CampaignRun.State s = Staged(out CampaignRun.Result run);
                if (s != null) { LivePath(s, run); Spread(s, run); }
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            finally { restore(); SimulationRandom.Reinstate(live); }
            _sb.Append(_failures == 0 ? "=== TacticalBeliefDiagnostic: ALL CHECKS PASS ===" : F("=== TacticalBeliefDiagnostic: {0} FAILED ===", _failures));
            if (_failures == 0) { Debug.Log(_sb.ToString()); } else { Debug.LogError(_sb.ToString()); }
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        // ============================================================ 1. the mean, planted
        private static void Planted()
        {
            _sb.Append("  1. the mean, planted (a 28-day window to 2026-09-13):\n");
            var day = new DateTime(2026, 9, 13);
            const int window = 28;
            Poll P(int daysBefore, double a, double b, double c) => new Poll(day.AddDays(-daysBefore), 600, "planted", new[] { a, b, c }, new[] { 1.0, 1.0, 1.0 });
            var polls = new List<Poll>
            {
                P(window, 0.90, 0.05, 0.05),   // on the window's open edge: out
                P(21, 0.50, 0.30, 0.20), P(14, 0.40, 0.40, 0.20), P(7, 0.45, 0.35, 0.20), P(0, 0.41, 0.33, 0.26),
                P(-1, 0.10, 0.10, 0.80),        // after the day: out
            };
            Poll? mean = PollingSystem.Mean(polls, day, window, out int included);
            double[] expect = { (0.50 + 0.40 + 0.45 + 0.41) / 4.0, (0.30 + 0.40 + 0.35 + 0.33) / 4.0, (0.20 + 0.20 + 0.20 + 0.26) / 4.0 };
            bool shares = mean.HasValue && Enumerable.Range(0, 3).All(i => Math.Abs(mean.Value.Share(i) - expect[i]) < 1e-12);
            bool moe = mean.HasValue && Enumerable.Range(0, 3).All(i => Math.Abs(mean.Value.MarginOfErrorPp(i) - PollingSystem.MarginOfErrorPp(expect[i], 2400)) < 1e-12);
            Check(included == 4 && shares && moe && mean.Value.SampleSize == 2400 && mean.Value.FieldDate == day,
                F("four polls in the window (one on its open edge and one after the day left out): shares {0}, the sample {1}, the margin of error the pooled sample's ({2:0.00} points at {3:0.000})",
                    mean.HasValue ? string.Join(" ", mean.Value.Shares().Select(v => v.ToString("0.0000", CultureInfo.InvariantCulture))) : "none", mean.HasValue ? mean.Value.SampleSize : 0,
                    mean.HasValue ? mean.Value.MarginOfErrorPp(0) : double.NaN, expect[0]));
            Check(!PollingSystem.Mean(polls, day.AddDays(-60), window, out _).HasValue && !PollingSystem.Mean(new List<Poll>(), day, window, out _).HasValue,
                "no poll in the window: no mean (the layer is skipped, as with no tracker)");
            var mixed = new List<Poll> { P(7, 0.5, 0.3, 0.2), new Poll(day, 600, "planted", new[] { 0.5, 0.5 }, new[] { 1.0, 1.0 }) };
            Check(!PollingSystem.Mean(mixed, day, window, out _).HasValue, "polls covering different parties: no mean");
        }

        // ============================================================ 2. the series, on a staged campaign
        private static CampaignRun.State Staged(out CampaignRun.Result run)
        {
            _sb.Append("  2. the series, on a staged Swedish campaign (seed 777, stepped day by day):\n");
            CampaignRun.Setup setup = LiveCampaignSetup.Sweden(new (int Day, int Party, Scandal Scandal)[0], out _, vintage: ElectionVintage.Sweden2022);
            SimulationRandom.Seed(777);
            CampaignRun.State s = CampaignRun.Begin(setup, SimulationRandom.For(SimulationRandom.Stream.CampaignAi), SimulationRandom.For(SimulationRandom.Stream.Debate), SimulationRandom.For(SimulationRandom.Stream.Scandal));
            while (!s.Finished) { CampaignRun.StepDay(s); }
            run = CampaignRun.Finish(s);
            DateTime polling = setup.Calendar.ElectionDate;
            bool lastIsPublic = s.PublishedPolls.Count > 0 && s.PublicPoll.HasValue && s.PublishedPolls[s.PublishedPolls.Count - 1].FieldDate == s.PublicPoll.Value.FieldDate
                && s.PublishedPolls[s.PublishedPolls.Count - 1].Shares().SequenceEqual(s.PublicPoll.Value.Shares());
            Check(s.PublishedPolls.Count == s.PublicPolls && lastIsPublic,
                F("every published tracker is kept ({0} kept, {1} published), the last of them the run's PublicPoll ({2:yyyy-MM-dd})", s.PublishedPolls.Count, s.PublicPolls,
                    s.PublicPoll.HasValue ? s.PublicPoll.Value.FieldDate : DateTime.MinValue));
            Poll? belief = NationalElection.TacticalBelief(s, polling, out int included);
            int inWindow = s.PublishedPolls.Count(p => p.FieldDate.Date > polling.Date.AddDays(-NationalElection.TacticalBeliefWindowDays) && p.FieldDate.Date <= polling.Date);
            bool meanOfThem = belief.HasValue && Enumerable.Range(0, belief.Value.PartyCount).All(i =>
                Math.Abs(belief.Value.Share(i) - s.PublishedPolls.Where(p => p.FieldDate.Date > polling.Date.AddDays(-NationalElection.TacticalBeliefWindowDays) && p.FieldDate.Date <= polling.Date).Average(p => p.Share(i))) < 1e-12);
            Check(included == inWindow && included >= 2 && meanOfThem,
                F("the belief on the polling day ({0:yyyy-MM-dd}) is the mean of the {1} tracker(s) fielded in the {2} days before it, pooled sample {3}", polling, included,
                    NationalElection.TacticalBeliefWindowDays, belief.HasValue ? belief.Value.SampleSize : 0));
            return s;
        }

        // ============================================================ 2b. the player's polling day (the review's L-3)
        /// <summary>The path the player's chamber election takes: `NationalElection.SharesOnPublishedPolls` on the staged state must believe the mean and
        /// count what the tactical layer gives on it; and the polling day's code (`SimulationManager.PlayerElection.cs`, comments stripped) must reach the
        /// layer through that path and never hand it the last tracker.</summary>
        private static void LivePath(CampaignRun.State s, CampaignRun.Result run)
        {
            DateTime polling = s.Setup.Calendar.ElectionDate;
            string[] keys = s.Setup.Parties.Select(p => p.Name).ToArray();
            Dictionary<string, double> counted = NationalElection.SharesOnPublishedPolls(CountryId.Sweden, keys, run.FinalShares, s, polling, out TacticalResult _, out Poll? belief, out int polls);
            Poll? mean = NationalElection.TacticalBelief(s, polling, out int meanPolls);
            Dictionary<string, double> onMean = NationalElection.SharesFromCampaign(CountryId.Sweden, keys, run.FinalShares, mean, out _, on: polling);
            bool sameBelief = belief.HasValue && mean.HasValue && polls == meanPolls && belief.Value.Shares().SequenceEqual(mean.Value.Shares());
            bool sameCount = counted.Count == onMean.Count && counted.All(kv => onMean.TryGetValue(kv.Key, out double v) && v == kv.Value);
            Check(sameBelief && sameCount, F("the player's polling-day path believes the mean of {0} tracker(s) and counts what the tactical layer gives on it", polls));

            string path = System.IO.Path.Combine(Application.dataPath, "Scripts", "Simulation", "SimulationManager.PlayerElection.cs");
            string code = SourceText.ReadWithoutComments(path);
            bool callsPath = code.Contains("NationalElection.SharesOnPublishedPolls(");
            bool handsLast = code.Contains("PlayerCampaign.PublicPoll") || code.Contains("NationalElection.SharesFromCampaign(");
            Check(callsPath && !handsLast, F("the polling day's code reaches the layer through SharesOnPublishedPolls ({0}) and hands it no tracker of its own ({1})",
                callsPath ? "called" : "NOT CALLED", handsLast ? "IT DOES" : "none"));
        }

        // ============================================================ 3. GA-5-2, measured
        private static void Spread(CampaignRun.State s, CampaignRun.Result run)
        {
            CampaignRun.Setup setup = s.Setup;
            DateTime polling = setup.Calendar.ElectionDate;
            string[] keys = setup.Parties.Select(p => p.Name).ToArray();
            double[] truth = (double[])run.FinalShares.Clone();
            int threatened = Array.IndexOf(truth, truth.Min());
            double rest = 1.0 - truth[threatened];
            for (int i = 0; i < truth.Length; i++) { truth[i] = i == threatened ? PlantedShare : truth[i] * (1.0 - PlantedShare) / rest; }
            _sb.Append(F("  3. GA-5-2, measured: the staged run's final shares as the truth, {0} planted at {1:0.0} %, the tracker series redrawn on {2} fixed seeds:\n", keys[threatened], 100.0 * PlantedShare, Replications));

            DateTime[] fielded = s.PublishedPolls.Select(p => p.FieldDate).ToArray();
            var lastCounted = new List<double>();
            var meanCounted = new List<double>();
            var lastSeats = new List<int>();
            var meanSeats = new List<int>();
            int leader = Array.IndexOf(truth, truth.Max());
            var leaderLast = new List<double>();
            var leaderMean = new List<double>();
            for (int k = 0; k < Replications; k++)
            {
                var random = new System.Random(FirstSeed + k);
                var series = fielded.Select(d => PollingSystem.Conduct(truth, setup.PublicHouse, d, random)).ToList();
                Poll last = series[series.Count - 1];
                Poll? mean = PollingSystem.Mean(series, polling, NationalElection.TacticalBeliefWindowDays, out _);
                Dictionary<string, double> onLast = NationalElection.SharesFromCampaign(CountryId.Sweden, keys, truth, last, out _, on: polling);
                Dictionary<string, double> onMean = NationalElection.SharesFromCampaign(CountryId.Sweden, keys, truth, mean, out _, on: polling);
                lastCounted.Add(onLast[keys[threatened]]);
                meanCounted.Add(onMean[keys[threatened]]);
                leaderLast.Add(onLast[keys[leader]]);
                leaderMean.Add(onMean[keys[leader]]);
                lastSeats.Add(NationalElection.Run(CountryId.Sweden, 0, onLast).Seats.TryGetValue(keys[leader], out int a) ? a : -1);
                meanSeats.Add(NationalElection.Run(CountryId.Sweden, 0, onMean).Seats.TryGetValue(keys[leader], out int b) ? b : -1);
            }
            double sdLast = Sd(lastCounted), sdMean = Sd(meanCounted);
            Check(sdMean < sdLast,
                F("{0}'s counted share spreads less when the layer believes the mean: sd {1:0.000} points on the mean against {2:0.000} on the last tracker (ratio {3:0.00})",
                    keys[threatened], 100.0 * sdMean, 100.0 * sdLast, sdLast > 0.0 ? sdMean / sdLast : double.NaN));
            _sb.Append(F("      printed: {0} counted {1:0.00}-{2:0.00} % and {3}-{4} seats on the last tracker, {5:0.00}-{6:0.00} % and {7}-{8} seats on the mean; {9} counted {10:0.00}-{11:0.00} % on the last, {12:0.00}-{13:0.00} % on the mean\n",
                keys[leader], 100.0 * leaderLast.Min(), 100.0 * leaderLast.Max(), lastSeats.Min(), lastSeats.Max(), 100.0 * leaderMean.Min(), 100.0 * leaderMean.Max(), meanSeats.Min(), meanSeats.Max(),
                keys[threatened], 100.0 * lastCounted.Min(), 100.0 * lastCounted.Max(), 100.0 * meanCounted.Min(), 100.0 * meanCounted.Max()));
        }

        private static double Sd(List<double> v)
        {
            double m = v.Average();
            return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / Math.Max(1, v.Count - 1));
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
