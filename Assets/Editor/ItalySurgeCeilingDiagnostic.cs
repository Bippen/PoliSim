using System;
using System.Globalization;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-A1 — **the Italy FdI standing test**, re-run with per-group loyalty live (E-1, 2026-09-10).
    ///
    /// The standing test asks whether Fratelli d'Italia's 2018→2022 surge — **4.35 % → 29.27 %, a factor
    /// of 6.7** — is reachable by the model as built: loyalty derived per party (W-A1), the media system
    /// (W-B9), polling and momentum (W-B10), salience, and now **loyalty per age band** from the ITANES
    /// 2013 and 2018 waves (§443). Reachable would be the strongest validation this model can get;
    /// unreachable is a named ceiling. ⚠ **Nothing here is tuned toward the answer** — no constant is
    /// re-fitted and the diagnostic writes nothing back. **The verdict is COMPUTED**, not written in
    /// advance: the previous version of this file carried its conclusion as text, and a conclusion in a
    /// string cannot change when the model does.
    ///
    /// <para><b>What it measures, on the model's own inputs.</b> It reuses <see cref="GateReRun.BuildCases"/>
    /// and <see cref="GateReRun.GroupInputs"/> rather than restating Italy's data, so there is one copy and
    /// this run cannot disagree with the gate. For FdI it reports the prior, the loyalty, the PERSUADED
    /// share the spatial layer produces, the BLENDED share under the uniform per-party loyalty (the figure
    /// every earlier run of this test reported) and under the per-group loyalty (the figure now), per band,
    /// and then solves for <b>the persuaded share FdI would need in order to land on 29.27 %</b> through
    /// §8's blend as it now runs - per group, summed by the bands' weights.</para>
    ///
    /// <para><b>The blend is evaluated here, and the evaluation is PROVEN rather than asserted.</b> This
    /// file re-evaluates §8's identity per group so it can solve it, which would be a second implementation
    /// unless checked. <see cref="SelfTest"/> reproduces
    /// <see cref="PreferenceModel.PreferenceByGroup"/> element by element at the measured inputs before any
    /// solve is trusted, and the run FAILS if it does not.</para>
    /// </summary>
    public static class ItalySurgeCeilingDiagnostic
    {
        private const string Target = "ITALY";
        private const string Party = "FdI";

        /// <summary>The real 2022 result this test is measured against (Eligendo, list vote).</summary>
        private const double RealSharePct = 29.27;

        /// <summary>The 2018 share the prior anchors on — the other end of the 6.7× surge.</summary>
        private const double PriorSharePct = 4.35;

        /// <summary>"Reached" means the model's own figure lands within this of the real one - half a point, the
        /// resolution the gate's MADs are read at. Not a tolerance to widen.</summary>
        private const double ReachedWithinPp = 0.5;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-A1: the Italy FdI standing test — is 4.35 % → 29.27 % reachable? (per-group loyalty LIVE, E-1) ===\n");
            sb.Append("    Inputs are GateReRun's own case data and group inputs (one copy, no restatement).\n");
            sb.Append("    Nothing is tuned toward the answer; no constant is re-fitted; the verdict is computed.\n\n");

            GateReRun.Case italy = default;
            bool found = false;
            foreach (GateReRun.Case c in GateReRun.BuildCases())
            {
                if (string.Equals(c.Name, Target, StringComparison.OrdinalIgnoreCase)) { italy = c; found = true; break; }
            }

            if (!found)
            {
                Debug.LogError($"C-A1: no case named {Target} in GateReRun.BuildCases() — the diagnostic has nothing to measure.");
                CheckExit.Finish(2);
                return;
            }

            int party = Array.IndexOf(italy.PartyNames, Party);
            if (party < 0)
            {
                Debug.LogError($"C-A1: {Target} carries no party named {Party} — the standing test's subject is not in the case.");
                CheckExit.Finish(2);
                return;
            }

            if (!italy.PerGroupLoyalty)
            {
                Debug.LogError("C-A1: the Italy case carries no per-group loyalty source - the re-run this diagnostic exists for cannot be made. Nothing is reported.");
                CheckExit.Finish(2);
                return;
            }

            double[] actual = GateReRun.Normalise(italy.ActualPct);
            double[] prior = GateReRun.Normalise(italy.PriorPct);
            double[] loyalty = LoyaltyModel.PartyLoyalties(italy.T1Pct, italy.T2Pct);
            ItanesGroupLoyalty.Inputs g = GateReRun.GroupInputs(italy);

            double[] spatial = VoteModel.PredictShares(italy.Parties, italy.Day1, italy.WEcon);
            double[] compat = GateReRun.ToCompatScale(spatial);

            // The persuaded distribution, from the model itself: every λ zero leaves only persuasion.
            var noLoyalty = new double[loyalty.Length];
            double[] persuaded = PreferenceModel.Preference(compat, prior, noLoyalty);

            double[] blendedUniform = PreferenceModel.Preference(compat, prior, loyalty);
            double[] blendedGroups = PreferenceModel.PreferenceByGroup(compat, g.T1ByGroup, g.LoyaltyByGroup, g.GroupWeights);

            int failures = SelfTest(sb, g, persuaded, blendedGroups);
            if (failures > 0)
            {
                Debug.LogError($"C-A1: the per-group blend self-test FAILED ({failures} element(s)) — the solve below would be arithmetic this model does not do. Nothing is reported.");
                Debug.Log(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            // ⚠ The standing test names two PUBLISHED figures, and the model's vectors are renormalised over the
            // modelled set - so the guard compares the RAW seed value, never the renormalised one.
            double priorRawPct = italy.PriorPct[party];
            double actualShare = actual[party];
            if (Math.Abs(100 * actualShare - RealSharePct) > 0.05 || Math.Abs(priorRawPct - PriorSharePct) > 0.05)
            {
                Debug.LogError(F("C-A1: the case no longer carries the standing test's figures — actual {0:F2} % (expected {1:F2}), raw 2018 prior {2:F2} % (expected {3:F2}). The test's subject has moved; re-derive it before reading any answer.",
                    100 * actualShare, RealSharePct, priorRawPct, PriorSharePct));
                Debug.Log(sb.ToString());
                CheckExit.Finish(2);
                return;
            }

            double persuadedShare = persuaded[party];
            double uniformShare = blendedUniform[party];
            double groupShare = blendedGroups[party];
            double[] implied = GroupLoyaltyModel.ImpliedPartyLoyalty(g.LoyaltyByGroup, g.T1ByGroup, g.GroupWeights);

            sb.Append("\n--- The measurement ---\n");
            sb.Append(F("    real 2022 (Eligendo list vote)                    {0,8:F2} %\n", 100 * actualShare));
            sb.Append(F("    2018 prior, renormalised over the set             {0,8:F2} %   (published {1:F2} %, the surge's other end)\n", 100 * prior[party], priorRawPct));
            sb.Append(F("    PERSUADED share (spatial layer alone)             {0,8:F2} %   dev {1:+0.00;-0.00} pp\n", 100 * persuadedShare, 100 * (persuadedShare - actualShare)));
            sb.Append(F("    BLENDED, uniform per-party loyalty {0,5:F1} (before)  {1,8:F2} %   dev {2:+0.00;-0.00} pp\n", loyalty[party], 100 * uniformShare, 100 * (uniformShare - actualShare)));
            sb.Append(F("    BLENDED, loyalty PER AGE BAND (implied {0,5:F1}) (now) {1,8:F2} %   dev {2:+0.00;-0.00} pp\n", implied[party], 100 * groupShare, 100 * (groupShare - actualShare)));

            sb.Append("\n--- FdI by band: where its 2018 vote sat, how much of it stays, what the blend gives it ---\n");
            sb.Append("    band    weight   2018 prior   loyalty   blended in band\n");
            for (int b = 0; b < g.Bands.Length; b++)
            {
                double[] inBand = PreferenceModel.Preference(compat, g.T1ByGroup[b], g.LoyaltyByGroup[b]);
                sb.Append(F("    {0,-6} {1,7:F3} {2,10:F2} % {3,8:F1} {4,12:F2} %\n",
                    g.Bands[b], g.GroupWeights[b], NormalisedPct(g.T1ByGroup[b], party), g.LoyaltyByGroup[b][party], 100 * inBand[party]));
            }

            // The solve: hold every other party's persuaded share in proportion, raise FdI's, and find the value
            // at which the per-group blend lands on the real result.
            double required = SolveRequiredPersuaded(g, persuaded, party, actualShare, out bool bracketed);
            bool reached = Math.Abs(100 * (groupShare - actualShare)) <= ReachedWithinPp;

            sb.Append("\n--- The ceiling, solved rather than estimated (per-group blend) ---\n");
            if (reached)
            {
                sb.Append(F("    the model's own figure lands within {0:F1} pp of the real result - no further persuasion is needed.\n", ReachedWithinPp));
            }
            else if (!bracketed)
            {
                sb.Append("    ⚠ UNREACHABLE AT ANY PERSUASION. Even with every other party's persuaded share\n");
                sb.Append("      driven to zero, the blend cannot reach the real result: the prior mass the\n");
                sb.Append("      OTHER parties hold through their own loyalty is larger than the shortfall.\n");
            }
            else
            {
                sb.Append(F("    persuaded share required to land 29.27 %  {0,8:F2} %\n", 100 * required));
                sb.Append(F("    persuaded share the model produces        {0,8:F2} %\n", 100 * persuadedShare));
                sb.Append(F("    the campaign layers would have to multiply FdI's persuaded share by {0:F2}x\n",
                    persuadedShare > 0 ? required / persuadedShare : double.PositiveInfinity));
            }

            sb.Append("\n--- What each system can and cannot do about it ---\n");
            sb.Append("    ⚠ MOMENTUM CANNOT MOVE A VOTE, BY CONSTRUCTION. MomentumTracker.Apply has exactly\n");
            sb.Append("      two call sites (grep -n momentum.Apply CampaignRun.cs) and BOTH are the argument\n");
            sb.Append("      to PollingSystem.Conduct. Election day counts truePreference, which the blend\n");
            sb.Append("      above produces. Since C-N1 (2026-09-02) MEDIA CAN: the day's coverage gain is\n");
            sb.Append("      resolved through the chain into persuasion (MediaSystem.ResolveCoverage), bounded\n");
            sb.Append("      because the gain is. SALIENCE reaches persuasion through CampaignActions. ⚠ Neither\n");
            sb.Append("      runs in this backtest: the 2022 case has no campaign, so the persuaded share here is\n");
            sb.Append("      the spatial layer's alone, and the multiplier above is what those layers would owe.\n");
            sb.Append("    PER-GROUP LOYALTY NOW ENTERS THE CHAIN (E-1, §443): loyalty is a number per party per\n");
            sb.Append("      age band, from ITANES 2013->2018 anchored to the official returns, summed by the\n");
            sb.Append("      bands' shares of the electorate. The uniform lambda this test named as the ceiling\n");
            sb.Append("      is gone; what it changed is the line marked (now) above.\n");

            sb.Append("\n--- VERDICT (computed) ---\n");
            if (reached)
            {
                sb.Append(F("    REACHED. FdI blends to {0:F2} % against 29.27 % with per-group loyalty live.\n", 100 * groupShare));
            }
            else
            {
                sb.Append(F("    NOT REACHED. FdI blends to {0:F2} % ({1:+0.00;-0.00} pp) with per-group loyalty live, against\n", 100 * groupShare, 100 * (groupShare - actualShare)));
                sb.Append(F("    {0:F2} % ({1:+0.00;-0.00} pp) under the uniform loyalty - the per-group layer moved it {2:+0.00;-0.00} pp.\n",
                    100 * uniformShare, 100 * (uniformShare - actualShare), 100 * (groupShare - uniformShare)));
                if (bracketed)
                {
                    sb.Append(F("    THE CEILING IS NAMED: the SPATIAL layer's persuaded share for FdI. The blend now lets enough\n"));
                    sb.Append(F("    of the electorate move; what it moves toward is {0:F2} %, and 29.27 % needs {1:F2} % - a\n", 100 * persuadedShare, 100 * required));
                    sb.Append(F("    factor of {0:F2} the campaign layers (salience, media) would have to supply, on positions that are\n", required / persuadedShare));
                    sb.Append("    CHES 2024's. Not a loyalty ceiling any longer; a persuasion one.\n");
                }
                else
                {
                    sb.Append("    THE CEILING IS NAMED: the other parties' retained prior mass - no persuasion reaches 29.27 %.\n");
                }
            }
            sb.Append("    THE CONSTANT WAS NOT RE-FITTED AND NOTHING WAS TUNED TOWARD 29.27.\n");

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static double NormalisedPct(double[] pct, int party)
        {
            double sum = 0.0;
            foreach (double p in pct) { sum += p; }
            return sum > 0 ? 100.0 * pct[party] / sum : 0.0;
        }

        /// <summary>Proves this file's per-group arithmetic IS the model's, at the measured inputs. Returns the number of elements disagreeing beyond 1e-12.</summary>
        private static int SelfTest(StringBuilder sb, ItanesGroupLoyalty.Inputs g, double[] persuaded, double[] expected)
        {
            double[] mine = BlendByGroup(g, persuaded);
            int bad = 0;
            double worst = 0.0;
            for (int i = 0; i < expected.Length; i++)
            {
                double d = Math.Abs(mine[i] - expected[i]);
                if (d > worst) { worst = d; }
                if (d > 1e-12) { bad++; }
            }

            sb.Append(F("--- Self-test: this file's per-group blend vs PreferenceModel.PreferenceByGroup -> worst element {0:E3}, {1} element(s) over 1e-12 ---\n", worst, bad));
            return bad;
        }

        /// <summary>§8's identity per group, renormalised inside each group, summed by the weights — the thing <see cref="SelfTest"/> checks.</summary>
        private static double[] BlendByGroup(ItanesGroupLoyalty.Inputs g, double[] persuaded)
        {
            var result = new double[persuaded.Length];
            for (int b = 0; b < g.Bands.Length; b++)
            {
                double priorSum = 0.0;
                foreach (double p in g.T1ByGroup[b]) { priorSum += p; }
                var group = new double[persuaded.Length];
                double total = 0.0;
                for (int i = 0; i < group.Length; i++)
                {
                    double lambda = Clamp01(g.LoyaltyByGroup[b][i] / 100.0);
                    group[i] = lambda * (priorSum > 0 ? g.T1ByGroup[b][i] / priorSum : 0.0) + (1.0 - lambda) * persuaded[i];
                    total += group[i];
                }
                for (int i = 0; i < group.Length; i++) { result[i] += g.GroupWeights[b] * group[i] / total; }
            }
            return result;
        }

        /// <summary>Bisects on the party's persuaded share (the others held in proportion) for the value at which the per-group blend lands on the target; <paramref name="bracketed"/> false when persuaded = 1 cannot reach it.</summary>
        private static double SolveRequiredPersuaded(ItanesGroupLoyalty.Inputs g, double[] persuaded, int party, double targetShare, out bool bracketed)
        {
            double At(double p) => BlendByGroup(g, WithPersuaded(persuaded, party, p))[party];

            bracketed = At(1.0) >= targetShare;
            if (!bracketed) { return double.NaN; }

            double lo = 0.0, hi = 1.0;
            for (int i = 0; i < 200; i++)
            {
                double mid = 0.5 * (lo + hi);
                if (At(mid) < targetShare) { lo = mid; } else { hi = mid; }
            }

            return 0.5 * (lo + hi);
        }

        private static double[] WithPersuaded(double[] persuaded, int party, double p)
        {
            var result = new double[persuaded.Length];
            double othersNow = 0.0;
            for (int i = 0; i < persuaded.Length; i++) { if (i != party) { othersNow += persuaded[i]; } }

            double scale = othersNow > 0.0 ? (1.0 - p) / othersNow : 0.0;
            for (int i = 0; i < persuaded.Length; i++) { result[i] = i == party ? p : persuaded[i] * scale; }
            return result;
        }

        private static double Clamp01(double v) => v < 0.0 ? 0.0 : (v > 1.0 ? 1.0 : v);

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
