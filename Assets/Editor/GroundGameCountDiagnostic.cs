using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// GATE-18 q2 with A-LOOP q24 piece 5b (2026-10-23; `docs/specs/A_LOOP_PLAN.md` §5, Q6; the q4 table's rows 5, 14, 15; Elias's K1 and K7):
    /// <b>the parliamentary count in play and the doors' turnout, proved where an Editor harness can prove them.</b>
    /// <list type="number">
    /// <item><b>(1) The anchor</b>: [CP20]'s stored PDF against its digest in the q4 register; its CACE, control turnout and upper limit as
    /// `GotvTurnout` carries them, and Gerber &amp; Green's bands for the USA (K7).</item>
    /// <item><b>(2) 5b by hand</b>: coverage persuadable once, the extra votes the CACE times the supporters reached, a party nobody canvassed at
    /// exactly its base, no turnout at the clamp, and the share effect the extra votes over the votes cast, absorbed.</item>
    /// <item><b>(3) the count with no campaign</b> (Sweden, Germany, Poland, each on its own start's prediction): at σ 0 (`ParliamentaryCount.NoiseAtZero`,
    /// the gate's own form) the count returns the shares it was handed to the rounding of a vote - where no campaign runs nothing moves but the
    /// noise; at the declared σ the draw is keyed (two counts of one day identical, another day different), a party that does not stand in a region
    /// gets no vote there, and the noise's national move is printed.</item>
    /// <item><b>(4) the campaign through the count</b> (Sweden's staging, seed 777, the player's party idle and then knocking doors every day in the
    /// largest region it stands in, the AI parties at their casts): every party's ground effect inside its bound (its supporters reached times the
    /// CACE's upper limit, over the votes cast), the highest supporters' turnout below 1 (never at the clamp), the player's campaign against idle
    /// printed whole (the persuasion of 5a plus the turnout of 5b) and its turnout part positive and inside its bound - GA-5-1's question.</item>
    /// <item><b>(5) the AI values a door at the count's own arithmetic</b>: `CampaignAi.DoorTurnoutValuePp` equals the hand formula, falls as the
    /// party's own doors there grow, is zero blind (no poll seen) and zero on a staging whose count reads no 5b turnout (a harness's own - the US staging
    /// reads 5b at Gerber &amp; Green's band since K18).</item>
    /// <item><b>(6) the office in the run-up</b> (row 5, K1): planned where the decision names, paid on the campaign's day 0, and its doors reach
    /// the count - the player's ground effect with the office above the same run without it.</item>
    /// </list>
    /// Prints `GROUND GAME COUNT: PASS` and exits 0 when every check holds; each failure is a `FAIL` line naming its check.
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.GroundGameCountDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class GroundGameCountDiagnostic
    {
        private const int Seed = 777;
        /// <summary>The tolerance of a count at σ 0 against the shares it was handed: a vote's rounding in a region of some 10^5 votes, summed.</summary>
        private const double RoundingTolerance = 1e-6;
        /// <summary>DECLARED: how far the okregi's spread at sigma 0 may sum from the shares handed - the zero floor's residue (`PolishSejmAllocation.SwungVotes`
        /// is not re-solved); a broken spread lands far outside it.</summary>
        private const double PolishSpreadTolerance = 1e-4;
        /// <summary>DECLARED: how far the AI's door value may sit from the count's own move, relative - the AI reads its national poll in every region
        /// (it measures no region's vote), which the second review measured at -0.6 % to +12 % across five valkretsar.</summary>
        private const double RegionalPollTolerance = 0.25;
        /// <summary>DECLARED (Elias's ruling of 2026-10-23, the noise scaled by the square root of s(1-s)): the most a 0.5 % party's mean counted share may
        /// sit above its handed share, in points - a twentieth of its share. The justification: under the scaled form the party's sigma in a region is
        /// about 0.17 points, a third of its share, so the clamp at zero binds in about one draw in six hundred and the mean excess it buys is of the
        /// order of 1e-4 points; the unscaled form's measured bias (+0.262 points, the second review) is ten times this bound, so the bound fails the
        /// unscaled form and any rescaling that lets the clamp work again, and the mean over `BiasDraws` days is known to about a thousandth of a point.</summary>
        private const double SmallPartyBiasBoundPp = 0.025;
        /// <summary>DECLARED: the keyed draws the bias and the threshold crossing are measured over - one polling day each, consecutive days.</summary>
        private const int BiasDraws = 1000;
        /// <summary>The probe party's share: the second review's synthetic 0.5 % party, standing in every valkrets.</summary>
        private const double ProbeShare = 0.005;
        /// <summary>The q4 register's digest of [CP20]'s stored PDF (`docs/reference/raw/aloop/q4/SHA256SUMS.txt`).</summary>
        private const string CantoniPonsDigest = "ac31a040d7bf61b3799a44c29b8f58cf9a0671b75653c3aefa35c8a882003162";
        private const string CantoniPonsFile = "docs/reference/raw/aloop/q4/nber_w27433_cantoni_pons.pdf";

        private static int _failures;
        private static StringBuilder _sb;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            _failures = 0;
            _sb = new StringBuilder("GROUND GAME COUNT: GATE-18 q2 (turnout and the declared noise in play) with A-LOOP q24 5b (the doors' turnout CACE)\n");
            try
            {
                Anchor();
                ByHand();
                using (SimulationManager.EpochScope())
                {
                    foreach (CountryId country in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland }) { NoCampaign(country); }
                    Campaign();
                    AiValue();
                    Office();
                    Noise();
                }
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "GROUND GAME COUNT: PASS - the anchor on its stored file; 5b by hand; the count with no campaign returning its shares at sigma 0 in Sweden, Germany and Poland, its noise keyed; every party's ground effect inside its bound and no turnout at the clamp; the AI's door value the count's own; the office back in the run-up and counted."
                : F("GROUND GAME COUNT: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        // ============================================================ (1) the anchor
        private static void Anchor()
        {
            _sb.Append("  (1) the anchors (K7):\n");
            string path = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), CantoniPonsFile);
            string digest = "(missing)";
            if (File.Exists(path))
            {
                using (SHA256 sha = SHA256.Create()) { digest = string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(b => b.ToString("x2", CultureInfo.InvariantCulture))); }
            }
            Check(digest == CantoniPonsDigest, F("[CP20] stored at {0}, digest {1} - the q4 register's", CantoniPonsFile, digest));
            double upper = GotvTurnout.EuropeanDoorCaceUpperPp;
            Check(GotvTurnout.EuropeanDoorCacePp == 5.01 && GotvTurnout.CantoniPonsControlTurnout == 0.747 && Math.Abs(upper - 100.0 * (0.018 + 1.96 * 0.008) / 0.359) < 1e-12,
                F("Europe: the volunteer canvass's CACE {0} points among the reached, control turnout {1:P1}, the 95 % upper limit {2:0.00} (DERIVED from the ITT 0.018, SE 0.008, opened doors 0.359)",
                    GotvTurnout.EuropeanDoorCacePp, GotvTurnout.CantoniPonsControlTurnout, upper));
            double[] bases = { 0.25, 0.45, 0.64, 0.84 };
            string bands = string.Join("; ", bases.Select(b => F("base {0:P0} -> {1} (upper {2})", b, GotvTurnout.UsDoorCacePp(b), GotvTurnout.DoorCaceUpperPp(CountryId.USA, b))));
            Check(GotvTurnout.UsDoorCacePp(0.25) == 4.6 && GotvTurnout.UsDoorCacePp(0.45) == 6.2 && GotvTurnout.UsDoorCacePp(0.64) == 1.4 && GotvTurnout.UsDoorCacePp(0.84) == 0.6
                  && GotvTurnout.DoorCacePp(CountryId.Sweden, 0.84) == GotvTurnout.EuropeanDoorCacePp && GotvTurnout.DoorCacePp(CountryId.USA, 0.84) == 0.6,
                "the USA keeps Gerber & Green's Table 4 band at the base turnout: " + bands + "; every other country Cantoni & Pons's figure");
            _sb.Append(F("    the transfer (DECLARED, [AUTHORED-DRAFT] on the play-calibration list): [CP20]'s control turnout {0:P1} lies in [GG14]'s 70 %-and-higher band, where the US meta-analysis finds {1} points - the European anchor is {2:0.0} times it at a like base\n",
                GotvTurnout.CantoniPonsControlTurnout, GotvTurnout.UsDoorCacePp(0.747), GotvTurnout.EuropeanDoorCacePp / GotvTurnout.UsDoorCacePp(0.747)));
        }

        // ============================================================ (2) 5b by hand
        private static void ByHand()
        {
            _sb.Append("  (2) 5b by hand:\n");
            const double supporters = 100_000.0, k = 5.01, b = 0.84;
            double half = supporters * Math.Log(2.0);
            Check(Math.Abs(GotvTurnout.Coverage(half, supporters) - 0.5) < 1e-12 && Math.Abs(GotvTurnout.Coverage(2.0 * half, supporters) - 0.75) < 1e-12 && GotvTurnout.Coverage(0.0, supporters) == 0.0,
                "coverage: doors of ln 2 times the supporters reach half of them, twice as many three quarters, none nothing (persuadable once)");
            Check(Math.Abs(GotvTurnout.ExtraVotes(half, supporters, k) - 0.5 * supporters * k / 100.0) < 1e-9,
                F("the extra votes: half of 100 000 supporters reached at {0} points - {1:0.000} votes", k, GotvTurnout.ExtraVotes(half, supporters, k)));
            Check(GotvTurnout.PartyTurnout(b, 1e12, supporters, k) == b + k / 100.0 && b + GotvTurnout.EuropeanDoorCaceUpperPp / 100.0 < 1.0,
                F("never at the clamp: every supporter reached turns out at {0:0.0000}; even at the upper limit {1:0.0000} < 1", GotvTurnout.PartyTurnout(b, 1e12, supporters, k), b + GotvTurnout.EuropeanDoorCaceUpperPp / 100.0));

            // two regions, two parties: the count's share effect is the extra votes over the votes cast, absorbed
            var gotv = new RegionalMobilization(new[] { 1_000_000.0, 500_000.0 }, 2);
            gotv.CarryDoors(0, 0, half);
            double[][] pref = { new[] { 0.10, 0.90 }, new[] { 0.40, 0.60 } };
            double[] bases = { 0.80, 0.70 };
            double[] cace = { k, k };
            ElectionDay.Result counted = ElectionDay.Count(new[] { "A", "B" }, pref, gotv, bases, cace, new System.Random(1), 0.0);
            ElectionDay.Result idle = ElectionDay.Count(new[] { "A", "B" }, pref, new RegionalMobilization(new[] { 1_000_000.0, 500_000.0 }, 2), bases, cace, new System.Random(1), 0.0);
            double sA = 100_000.0;   // party 0's supporters in region A
            double extra = sA * GotvTurnout.Coverage(half, sA) * k / 100.0;
            double v0 = 1_000_000.0 * 0.10 * 0.80 + 500_000.0 * 0.40 * 0.70, cast = 1_000_000.0 * 0.80 + 500_000.0 * 0.70;
            double hand = 100.0 * ((v0 + extra) / (cast + extra) - v0 / cast);
            double got = 100.0 * (counted.NationalShares[0] - idle.NationalShares[0]);
            Check(Math.Abs(got - hand) < 1e-4 && idle.Regions[1].Votes[0] == counted.Regions[1].Votes[0],
                F("the share effect is the extra votes over the votes cast: party 0 +{0:0.0000} points (by hand {1:0.0000}); the region nobody canvassed votes as before", got, hand));
            Check(Math.Abs(idle.NationalShares[0] - v0 / cast) < 1e-6,
                F("with no doors and sigma 0 the count is each region's base turnout times its preference: {0:0.000000} (by hand {1:0.000000})", idle.NationalShares[0], v0 / cast));

            // a party with no share in a region gets no vote from the noise there
            double[][] absent = { new[] { 0.0, 1.0 }, new[] { 0.5, 0.5 } };
            ElectionDay.Result noisy = ElectionDay.Count(new[] { "A", "B" }, absent, new RegionalMobilization(new[] { 1_000_000.0, 500_000.0 }, 2), bases, cace, new System.Random(3), 5.0);
            Check(noisy.Regions[0].Votes[0] == 0.0 && noisy.Regions[1].Votes[0] > 0.0, "a party that does not stand in a region gets no vote there from the noise (sigma 5 points), and its votes elsewhere stand");
        }

        // ============================================================ (3) the count with no campaign
        private static void NoCampaign(CountryId country)
        {
            WorldClock.ApplyStart(country);
            DateTime day = WorldClock.LatestElectionDay(country);
            if (!NationalElection.TryPredictShares(country, out Dictionary<string, double> shares, on: day))
            {
                Check(false, F("(3) {0}: no prediction on {1:yyyy-MM-dd}", country, day));
                return;
            }
            ParliamentaryCount.Result exact;
            using (ParliamentaryCount.NoiseAtZero()) { exact = ParliamentaryCount.Count(country, day, shares, null, null, writeReadout: false); }
            if (exact == null) { Check(false, F("(3) {0}: the count returned nothing", country)); return; }
            double worst = shares.Keys.Max(key => Math.Abs(exact.Shares[key] - shares[key]));
            Check(worst <= RoundingTolerance && exact.SigmaPp == 0.0 && exact.GroundPp.Values.All(g => g == 0.0),
                F("(3) {0} on {1:yyyy-MM-dd} with no campaign, the noise at zero (the gate's form): {2} regions counted, each share {3:0.0e+0} from the shares handed (within {4:0e+0}), {5}",
                    country, day, exact.Regions, worst, RoundingTolerance, double.IsNaN(exact.Turnout) ? "no turnout counted (the okregi carry no electorate)" : F("turnout {0:P2}", exact.Turnout)));

            if (exact.StillShares != null)
            {
                // the review 2's L1: Poland's sigma-0 shares above are the hand-back's identity (the shares handed plus the count's move, zero here);
                // what can fail is the okregi's own spread - their vote-weighted sum at sigma 0, held against the shares handed
                double spread = shares.Keys.Max(key => Math.Abs(exact.StillShares[key] - shares[key]));
                Check(spread <= PolishSpreadTolerance,
                    F("(3) {0}: the identity above is the hand-back's; the okregi's own spread at sigma 0 sums back to the shares handed within {1:0.0e+0} (worst {2:0.0e+0} - the floor's residue, DECLARED)", country, PolishSpreadTolerance, spread));
            }

            // the review 2's L7: the count consumes no counted stream - every stream's draw count the same before and after (none created either)
            SimulationRandom.Seed(Seed);
            foreach (SimulationRandom.Stream stream in Enum.GetValues(typeof(SimulationRandom.Stream))) { SimulationRandom.For(stream).NextDouble(); }   // every stream live, so a draw on any of them shows
            Dictionary<SimulationRandom.Stream, int> before = SimulationRandom.CaptureDrawCounts();
            ParliamentaryCount.Count(country, day, shares, null, null, writeReadout: false);
            Dictionary<SimulationRandom.Stream, int> after = SimulationRandom.CaptureDrawCounts();
            bool untouched = before.Count == after.Count && before.All(kv => after.TryGetValue(kv.Key, out int n) && n == kv.Value);
            Check(untouched, F("(3) {0}: the count draws on no counted stream - {1} streams, every draw count the same before and after{2}", country, before.Count,
                untouched ? string.Empty : " - MOVED: " + string.Join(", ", after.Where(kv => !before.TryGetValue(kv.Key, out int n) || n != kv.Value).Select(kv => kv.Key + " " + kv.Value))));

            ParliamentaryCount.Result a = ParliamentaryCount.Count(country, day, shares, null, null, writeReadout: false);
            ParliamentaryCount.Result b = ParliamentaryCount.Count(country, day, shares, null, null, writeReadout: false);
            // the review 2's L6: a party's draw is tied to its key - the same shares in another insertion order (a shadow world's) count the same
            var reversed = new Dictionary<string, double>();
            foreach (string key in shares.Keys.Reverse()) { reversed[key] = shares[key]; }
            ParliamentaryCount.Result rev = ParliamentaryCount.Count(country, day, reversed, null, null, writeReadout: false);
            Check(rev.Digest == a.Digest && shares.Keys.All(key => rev.Shares[key] == a.Shares[key]),
                F("(3) {0}: the same shares in the reverse key order draw the same noise - digest {1} against {2}", country, rev.Digest, a.Digest));
            if (a.StillShares != null)
            {
                // the review 2's L2: Poland hands back the shares handed plus the count's move, held at zero and renormalised - recomputed here
                double sum = shares.Keys.Sum(key => Math.Max(0.0, shares[key] + a.CountedShares[key] - a.StillShares[key]));
                double worstBack = shares.Keys.Max(key => Math.Abs(a.Shares[key] - Math.Max(0.0, shares[key] + a.CountedShares[key] - a.StillShares[key]) / sum));
                Check(worstBack < 1e-12 && Math.Abs(a.Shares.Values.Sum() - 1.0) < 1e-12,
                    F("(3) {0}: the hand-back is the shares handed plus the count's move, renormalised - recomputed within {1:0.0e+0}, summing to one", country, worstBack));
            }
            ParliamentaryCount.Result other = ParliamentaryCount.Count(country, day.AddDays(1), shares, null, null, writeReadout: false);
            double noiseMost = shares.Keys.Max(key => Math.Abs(a.Shares[key] - shares[key]));
            Check(a.SigmaPp == RegionalAggregation.RegionalNoiseSigmaPp && a.Digest == b.Digest && a.Digest != other.Digest && a.NoiseSeed == SimulationRandom.RegionalElectionNoiseSeedOn(country, day),
                F("(3) {0} at the declared sigma {1:0.0}: keyed (two counts of the day the same digest {2}, the next day another); the noise moves a national share by at most {3:0.000} points", country, a.SigmaPp, a.Digest, 100.0 * noiseMost));
            if (country == CountryId.Germany && shares.ContainsKey("CSU"))
            {
                // the CSU stands in Bayern alone: the count's readout gives it no vote anywhere else
                ParliamentaryCount.Count(country, day, shares, null, null, writeReadout: true);
                IReadOnlyList<string> keys = NationalElection.LastRegionalKeys;
                int csu = keys.ToList().IndexOf("CSU");
                int outside = 0;
                for (int r = 0; r < NationalElection.LastRegionalNames.Length; r++) { if (NationalElection.LastRegionalNames[r] != "Bayern" && NationalElection.LastRegionalShares[r][csu] > 0.0) { outside++; } }
                double sum = 0.0, weight = 0.0;
                for (int r = 0; r < NationalElection.LastRegionalShares.Length; r++) { sum += NationalElection.LastRegionalWeights[r] * NationalElection.LastRegionalShares[r][csu]; weight += NationalElection.LastRegionalWeights[r]; }
                ParliamentaryCount.Result noted = ParliamentaryCount.Count(country, day, shares, null, null, writeReadout: false);
                Check(outside == 0 && Math.Abs(sum / weight - noted.Shares["CSU"]) < 1e-9,
                    F("(3) Germany: the CSU gets no vote outside Bayern under the noise ({0} Länder with one), and the readout's vote-weighted Länder are the counted headline ({1:P3} against {2:P3})", outside, sum / weight, noted.Shares["CSU"]));
            }
        }

        // ============================================================ (4) the campaign through the count
        private static void Campaign()
        {
            WorldClock.ApplyStart(CountryId.Sweden);
            var none = new (int, int, Scandal)[0];
            CampaignRun.Setup probe = LiveCampaignSetup.Sweden(none, out _);
            const int party = 0;
            int region = LargestStanding(probe, party);
            Func<int, AiDecision[]> doors = DoorScript(region, probe);
            CampaignRun.Setup idleSetup = LiveCampaignSetup.Sweden(none, out _, playerParty: party, playerScript: d => new AiDecision[0]);
            CampaignRun.Setup fullSetup = LiveCampaignSetup.Sweden(none, out _, playerParty: party, playerScript: doors);
            Check(fullSetup.DoorCacePp == GotvTurnout.EuropeanDoorCacePp, F("(4) Sweden's staging carries the door CACE its count reads: {0}", fullSetup.DoorCacePp));
            CampaignRun.Result idle = CampaignAiHarness.RunSeeded(idleSetup, Seed);
            CampaignRun.Result full = CampaignAiHarness.RunSeeded(fullSetup, Seed);
            DateTime day = fullSetup.Calendar.ElectionDate;
            ParliamentaryCount.Result idleCount, fullCount;
            using (ParliamentaryCount.NoiseAtZero())
            {
                idleCount = ParliamentaryCount.Count(CountryId.Sweden, day, SharesOf(idleSetup, idle), idleSetup, idle.Gotv, writeReadout: false);
                fullCount = ParliamentaryCount.Count(CountryId.Sweden, day, SharesOf(fullSetup, full), fullSetup, full.Gotv, writeReadout: false);
            }
            if (idleCount == null || fullCount == null) { Check(false, "(4) the count returned nothing for Sweden's campaign"); return; }
            foreach ((string what, ParliamentaryCount.Result count) in new[] { ("idle", idleCount), ("doors", fullCount) })
            {
                var lines = new List<string>();
                bool inside = true;
                foreach (string key in count.GroundPp.Keys)
                {
                    double own = count.GroundPp[key], bound = count.GroundBoundPp[key];
                    inside &= own <= bound + 1e-9 && Math.Abs(own) <= count.GroundBoundPp.Values.Sum() + 1e-9;
                    lines.Add(F("{0} {1:+0.0000;-0.0000} (bound {2:0.0000})", key, own, bound));
                }
                Check(inside && count.HighestPartyTurnout < 1.0,
                    F("(4) Sweden, the player {0}: every party's ground effect inside its bound, the highest supporters' turnout {1:0.0000} (never at the clamp); turnout {2:P2} against {3:P2} without the doors - {4}",
                        what, count.HighestPartyTurnout, count.Turnout, count.TurnoutWithoutGround, string.Join(", ", lines)));
            }
            string key0 = fullSetup.Parties[party].Name;
            double whole = 100.0 * (fullCount.Shares[key0] - idleCount.Shares[key0]);
            double persuasion = 100.0 * (full.FinalShares[party] - idle.FinalShares[party]);
            double turnout = fullCount.GroundPp[key0] - idleCount.GroundPp[key0];
            Check(turnout > 0.0 && fullCount.GroundPp[key0] <= fullCount.GroundBoundPp[key0] + 1e-9,
                F("(4) GA-5-1: the player's campaign ({0}, {1} doors a day in {2}) against idle moves its counted share {3:+0.0000;-0.0000} points - persuasion {4:+0.0000;-0.0000} (5a) and its doors' turnout {5:+0.0000;-0.0000} (5b), its ground effect {6:0.0000} inside its bound {7:0.0000}",
                    key0, DoorsPerDay(probe), probe.Regions[region].Name, whole, persuasion, turnout, fullCount.GroundPp[key0], fullCount.GroundBoundPp[key0]));
        }

        // ============================================================ (7) the noise's form (Elias's ruling of 2026-10-23)
        private static void Noise()
        {
            _sb.Append("  (7) the noise's form - each party's sigma scaled by the square root of s(1-s), W-D1's 1.2 points read at s = 0.5 (Elias, 2026-10-23):\n");
            double[] probe = { 0.5, 0.25, 0.05, 0.005 };
            _sb.Append("    sigma by share: " + string.Join(", ", probe.Select(p => F("{0:P1} -> {1:0.00} points", p, RegionalAggregation.RegionalNoiseSigmaPp * Math.Sqrt(p * (1.0 - p)) / 0.5))) + "\n");

            // the small-party bias: a synthetic party at 0.5 % in every valkrets, Sweden's prediction scaled to make room for it
            WorldClock.ApplyStart(CountryId.Sweden);
            DateTime day = WorldClock.LatestElectionDay(CountryId.Sweden);
            if (!NationalElection.TryPredictShares(CountryId.Sweden, out Dictionary<string, double> se, on: day)) { Check(false, "(7) Sweden: no prediction"); return; }
            var handed = se.ToDictionary(kv => kv.Key, kv => kv.Value * (1.0 - ProbeShare));
            handed["PROBE"] = ProbeShare;
            double sum = 0.0, sumSq = 0.0;
            for (int k = 0; k < BiasDraws; k++)
            {
                ParliamentaryCount.Result c = ParliamentaryCount.Count(CountryId.Sweden, day.AddDays(k), handed, null, null, writeReadout: false);
                double d = 100.0 * (c.Shares["PROBE"] - ProbeShare);
                sum += d; sumSq += d * d;
            }
            double mean = sum / BiasDraws, sd = Math.Sqrt(Math.Max(0.0, sumSq / BiasDraws - mean * mean));
            Check(Math.Abs(mean) <= SmallPartyBiasBoundPp,
                F("(7) Sweden: a party at 0.5 % in every valkrets counts {0:+0.0000;-0.0000} points above its share on average over {1} keyed days (sd {2:0.000} points), within the declared {3} - the unscaled form measured +0.262 (the second review)",
                    mean, BiasDraws, sd, SmallPartyBiasBoundPp));

            // the BSW at 4.98 % on Germany's 2025 record: how often the noise carries it across the 5 % line (a reading, not asserted)
            WorldClock.ApplyStart(CountryId.Germany);
            if (!PartySystems.TryHistory(CountryId.Germany, out double[] latest, out double[] _, ElectionVintage.Germany2025)) { Check(false, "(7) Germany: no 2025 record"); return; }
            IReadOnlyList<PoliticalParty> roster = PartySystems.For(CountryId.Germany);
            var record = new Dictionary<string, double>();
            double unit = latest.Sum() > 1.5 ? 100.0 : 1.0;   // the history is held in per cent; the count reads fractions of the valid vote
            for (int i = 0; i < roster.Count && i < latest.Length; i++) { record[roster[i].Abbrev] = latest[i] / unit; }
            if (!record.ContainsKey("BSW")) { Check(false, "(7) Germany: the 2025 record carries no BSW"); return; }
            double rosterSum = record.Values.Sum();
            // the count reads shares summing to one, as play hands it (the record's roster sums to less: the lists it does not carry); the crossing is read
            // back on the record's own base, every valid vote
            Dictionary<string, double> normalised = record.ToDictionary(kv => kv.Key, kv => kv.Value / rosterSum);
            DateTime seated = new DateTime(2025, 6, 1);   // DECLARED: a day the 2025 chamber sits, so the Länder spread the 2025 record onto 2025's own Länder
            int crossed = 0;
            for (int k = 0; k < BiasDraws; k++)
            {
                ParliamentaryCount.Result c = ParliamentaryCount.Count(CountryId.Germany, seated.AddDays(k), normalised, null, null, writeReadout: false);
                if ((c.Shares["BSW"] * rosterSum >= 0.05) != (record["BSW"] >= 0.05)) { crossed++; }
            }
            _sb.Append(F("    (7) Germany 2025: the BSW at {0:P2} of the valid vote crosses the 5 % line in {1} of {2} keyed days ({3:P1}) - the unscaled form measured 46.5 % (the second review); read, not asserted\n",
                record["BSW"], crossed, BiasDraws, (double)crossed / BiasDraws));
        }

        // ============================================================ (5) the AI's value
        private static void AiValue()
        {
            WorldClock.ApplyStart(CountryId.Sweden);
            CampaignRun.Setup s = LiveCampaignSetup.Sweden(new (int, int, Scandal)[0], out _);
            int r = LargestStanding(s, 0);
            var shares = new double[s.Parties.Length];
            for (int i = 0; i < shares.Length; i++) { shares[i] = s.PriorShares[i] / s.PriorShares.Sum(); }
            var poll = new Poll(s.Calendar.CampaignStart, 1000, "probe", shares, new double[shares.Length]);
            double[] soFar = new double[s.Regions.Length];
            AiView View(bool hasPoll, double cace, double already)
            {
                soFar[r] = already;
                return new AiView(0, CampaignPhase.Campaign, 30, new ResourcePool(1e6, 10.0, 100), 1e5, hasPoll, poll, new double[shares.Length], new IssueMeasurement[IssueVector.IssueCount],
                    0.5, s.NationalAudience, s.Regions, -1, ownDoorContactsByRegion: (double[])soFar.Clone(), doorCacePp: cace);
            }
            const double more = 2_000.0, already = 50_000.0;
            double supporters = s.Regions[r].Eligible * shares[0];
            double hand = 100.0 * (GotvTurnout.ExtraVotes(already + more, supporters, GotvTurnout.EuropeanDoorCacePp) - GotvTurnout.ExtraVotes(already, supporters, GotvTurnout.EuropeanDoorCacePp)) * (1.0 - shares[0]) / s.NationalAudience;
            double first = CampaignAi.DoorTurnoutValuePp(View(true, GotvTurnout.EuropeanDoorCacePp, 0.0), r, more);
            double later = CampaignAi.DoorTurnoutValuePp(View(true, GotvTurnout.EuropeanDoorCacePp, already), r, more);
            Check(Math.Abs(later - hand) < 1e-12 && first > later && later > 0.0,
                F("(5) the AI values {0:N0} more doors in {1} at the count's own arithmetic: {2:0.000000} points with {3:N0} doors already opened (by hand {4:0.000000}), {5:0.000000} with none - a worked region is worth less",
                    more, s.Regions[r].Name, later, already, hand, first));
            Check(CampaignAi.DoorTurnoutValuePp(View(false, GotvTurnout.EuropeanDoorCacePp, 0.0), r, more) == 0.0 && CampaignAi.DoorTurnoutValuePp(View(true, 0.0, 0.0), r, more) == 0.0,
                "(5) blind (no poll seen) a door's turnout is worth nothing, and on a staging whose count reads no 5b turnout (a harness's own; since K18 the USA's reads it, `UsCampaignDiagnostic`) nothing");

            // the review 2's L5: against the count's OWN move - the same doors laid on the count (sigma 0) at the shares the AI polled, the party's counted
            // share with the doors less without them; the AI reads its national share in the region, the count the region's own (the declared tolerance)
            const double block = 20_000.0;
            var handed = new Dictionary<string, double>();
            for (int i = 0; i < shares.Length; i++) { handed[s.Parties[i].Name] = shares[i]; }
            double[] eligible = s.Regions.Select(x => x.Eligible).ToArray();
            RegionalMobilization Doors(double d) { var g = new RegionalMobilization((double[])eligible.Clone(), s.Parties.Length); if (d > 0.0) { g.CarryDoors(r, 0, d); } return g; }
            ParliamentaryCount.Result with, without;
            using (ParliamentaryCount.NoiseAtZero())
            {
                with = ParliamentaryCount.Count(CountryId.Sweden, s.Calendar.ElectionDate, handed, s, Doors(already + block), writeReadout: false);
                without = ParliamentaryCount.Count(CountryId.Sweden, s.Calendar.ElectionDate, handed, s, Doors(already), writeReadout: false);
            }
            string key0 = s.Parties[0].Name;
            double actual = 100.0 * (with.Shares[key0] - without.Shares[key0]);
            double valued = CampaignAi.DoorTurnoutValuePp(View(true, GotvTurnout.EuropeanDoorCacePp, already), r, block);
            Check(actual > 0.0 && Math.Abs(valued / actual - 1.0) <= RegionalPollTolerance,
                F("(5) the AI's value of {0:N0} more doors in {1} against the count's own move: valued {2:0.000000} points, the count moves {3:0.000000} ({4:+0.0;-0.0} %, within the declared {5:P0} for the national poll read in a region)",
                    block, s.Regions[r].Name, valued, actual, 100.0 * (valued / actual - 1.0), RegionalPollTolerance));
        }

        // ============================================================ (6) the office in the run-up
        private static void Office()
        {
            WorldClock.ApplyStart(CountryId.Sweden);
            var none = new (int, int, Scandal)[0];
            CampaignRun.Setup staged = LiveCampaignSetup.Sweden(none, out _);
            // the first party, in the staging's order, whose chest can KEEP one more office to polling day (D-1 (c): `CampaignRun.Begin` opens only those) -
            // an office the plan cannot keep is not opened, and that is the affordability rule's finding, not the office's
            for (int party = 0; party < staged.Parties.Length; party++)
            {
                if (OfficeFor(staged, party, none, out string skipped)) { return; }
                _sb.Append("    (6) ").Append(skipped).Append('\n');
            }
            Check(false, "(6) no party of Sweden's staging can keep one more office to polling day - the office's doors were never counted");
        }

        private static bool OfficeFor(CampaignRun.Setup staged, int party, (int, int, Scandal)[] none, out string skipped)
        {
            skipped = null;
            PreCampaignRun.State pre = PreCampaignRun.Begin(staged, party, new System.Random(5));
            int region = PreCampaignRun.NextOfficeRegion(pre);
            PreCampaignRun.State bare = PreCampaignRun.Begin(staged, party, new System.Random(5));
            PreCampaignRun.StepDay(pre, new[] { new PreCampaignRun.Decision(CampaignActionKind.EstablishOffice, region) });
            PreCampaignRun.StepDay(bare, null);
            while (!pre.Finished) { PreCampaignRun.StepDay(pre, null); PreCampaignRun.StepDay(bare, null); }
            PreCampaignRun.Outcome withOffice = PreCampaignRun.Finish(pre), without = PreCampaignRun.Finish(bare);
            Check(pre.Log.Count == 1 && pre.Log[0].Refusal == null && Array.IndexOf(withOffice.Offices, region) >= 0 && Array.IndexOf(without.Offices, region) < 0 && withOffice.Money == without.Money,
                F("(6) the run-up takes an office in {0} (row 5, K1): planned, the run-up's chest untouched (the campaign's day 0 pays it){1}", staged.Regions[region].Name,
                    pre.Log.Count > 0 && pre.Log[0].Refusal != null ? " - REFUSED \"" + pre.Log[0].Refusal + "\"" : string.Empty));
            Func<int, AiDecision[]> idleScript = d => new AiDecision[0];
            CampaignRun.Setup a = LiveCampaignSetup.Sweden(none, out _, playerParty: party, playerScript: idleScript, playerOutcome: withOffice);
            CampaignRun.Setup b = LiveCampaignSetup.Sweden(none, out _, playerParty: party, playerScript: idleScript, playerOutcome: without);
            CampaignRun.Result ra = CampaignAiHarness.RunSeeded(a, Seed), rb = CampaignAiHarness.RunSeeded(b, Seed);
            if (ra.Parties[party].OfficesOpened <= rb.Parties[party].OfficesOpened)
            {
                skipped = F("{0}: its chest cannot keep the office in {1} to polling day (D-1 (c)), so it is not opened - the next party", staged.Parties[party].Name, staged.Regions[region].Name);
                return false;
            }
            ParliamentaryCount.Result ca, cb;
            using (ParliamentaryCount.NoiseAtZero())
            {
                ca = ParliamentaryCount.Count(CountryId.Sweden, a.Calendar.ElectionDate, SharesOf(a, ra), a, ra.Gotv, writeReadout: false);
                cb = ParliamentaryCount.Count(CountryId.Sweden, b.Calendar.ElectionDate, SharesOf(b, rb), b, rb.Gotv, writeReadout: false);
            }
            string key = a.Parties[party].Name;
            Check(ra.Gotv.DoorContacts(region, party) > rb.Gotv.DoorContacts(region, party) && ca.GroundPp[key] > cb.GroundPp[key],
                F("(6) its doors reach the count: {0}'s doors in {1} {2:N0} against {3:N0} without it, its ground effect {4:+0.0000;-0.0000} against {5:+0.0000;-0.0000} points",
                    key, a.Regions[region].Name, ra.Gotv.DoorContacts(region, party), rb.Gotv.DoorContacts(region, party), ca.GroundPp[key], cb.GroundPp[key]));
            return true;
        }

        // ============================================================ helpers
        private static int LargestStanding(CampaignRun.Setup s, int party)
        {
            int best = 0;
            for (int r = 1; r < s.Regions.Length; r++) { if (s.StandsIn(party, r) && s.Regions[r].Audience > s.Regions[best].Audience) { best = r; } }
            return best;
        }

        /// <summary>DECLARED, the scripted ground game: the door-to-door act at its listed price as many times a day as the candidate's hours allow, all in one region.</summary>
        private static int DoorsPerDay(CampaignRun.Setup s) => Math.Max(1, (int)Math.Floor(CampaignEconomy.HoursPerCampaignDay / CampaignActions.Spec(CampaignActionKind.DoorToDoor).Hours));

        private static Func<int, AiDecision[]> DoorScript(int region, CampaignRun.Setup s)
        {
            CampaignActions.ActionSpec spec = CampaignActions.Spec(CampaignActionKind.DoorToDoor);
            int perDay = DoorsPerDay(s);
            string label = s.Regions[region].Name;
            return day => Enumerable.Range(0, perDay).Select(_ => new AiDecision(CampaignActionKind.DoorToDoor, new CampaignActions.ActionTarget(region, -1, null), label, spec.MoneyCost, spec.Hours, 0.0, false)).ToArray();
        }

        private static Dictionary<string, double> SharesOf(CampaignRun.Setup s, CampaignRun.Result r)
        {
            var shares = new Dictionary<string, double>();
            for (int i = 0; i < s.Parties.Length; i++) { shares[s.Parties[i].Name] = r.FinalShares[i]; }
            return shares;
        }

        private static void Check(bool ok, string what)
        {
            if (!ok) { _failures++; }
            _sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n');
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
