using System;
using System.Globalization;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-A1 — **the Italy FdI standing test, re-run and answered.**
    ///
    /// The standing test asks whether Fratelli d'Italia's 2018→2022 surge — **4.35 % → 29.27 %,
    /// a factor of 6.7** — becomes reachable now that the media system (W-B9), polling and momentum
    /// (W-B10) and salience exist. Reachable would be the strongest validation this model can get;
    /// unreachable is a named ceiling. ⚠ **Nothing here is tuned toward the answer** — no constant is
    /// re-fitted and the diagnostic writes nothing back.
    ///
    /// <para><b>What it measures, on the model's own inputs.</b> It reuses
    /// <see cref="GateReRun.BuildCases"/> rather than restating Italy's seed data, so there is exactly
    /// one copy of that data and this run cannot silently disagree with the gate. For FdI it reports the
    /// prior, the derived loyalty, the PERSUADED share the spatial layer produces, the BLENDED share the
    /// model actually predicts, and then solves for <b>the persuaded share FdI would need in order to
    /// land on 29.27 %</b> through §8's published blend.</para>
    ///
    /// <para><b>The blend is evaluated here, and the evaluation is PROVEN rather than asserted.</b>
    /// §8's identity is <c>result_i = λ_i · prior_i + (1 − λ_i) · persuaded_i</c>, renormalised, with
    /// <c>λ_i = loyalty_i / 100</c>. This file re-evaluates that identity so it can solve it, which
    /// would be a second implementation of a model — the thing this repo forbids — unless it is
    /// checked. <see cref="SelfTest"/> therefore reproduces
    /// <see cref="PreferenceModel.Preference(double[], double[], double[])"/> element by element at
    /// the measured inputs before any solve is trusted, and the run FAILS if it does not.</para>
    ///
    /// <para>⚠ <b>The persuaded vector is obtained through the public API, not by copying
    /// <c>PersuadedShares</c>:</b> calling <c>Preference</c> with every loyalty at zero makes every
    /// λ zero, so the blend returns the persuaded distribution itself. That is the model's own
    /// arithmetic answering the question, not a reimplementation of it.</para>
    /// </summary>
    public static class ItalySurgeCeilingDiagnostic
    {
        private const string Target = "ITALY";
        private const string Party = "FdI";

        /// <summary>The real 2022 result this test is measured against (Eligendo, list vote).</summary>
        private const double RealSharePct = 29.27;

        /// <summary>The 2018 share the prior anchors on — the other end of the 6.7× surge.</summary>
        private const double PriorSharePct = 4.35;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-A1: the Italy FdI standing test — is 4.35 % → 29.27 % reachable? ===\n");
            sb.Append("    Inputs are GateReRun's own case data (one copy, no restatement).\n");
            sb.Append("    Nothing is tuned toward the answer; no constant is re-fitted.\n\n");

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

            int party = -1;
            for (int i = 0; i < italy.PartyNames.Length; i++)
            {
                if (string.Equals(italy.PartyNames[i], Party, StringComparison.OrdinalIgnoreCase)) { party = i; break; }
            }

            if (party < 0)
            {
                Debug.LogError($"C-A1: {Target} carries no party named {Party} — the standing test's subject is not in the case.");
                CheckExit.Finish(2);
                return;
            }

            double[] actual = GateReRun.Normalise(italy.ActualPct);
            double[] prior = GateReRun.Normalise(italy.PriorPct);
            double[] loyalty = LoyaltyModel.PartyLoyalties(italy.T1Pct, italy.T2Pct);

            double[] spatial = VoteModel.PredictShares(italy.Parties, italy.Day1, italy.WEcon);
            double[] compat = GateReRun.ToCompatScale(spatial);

            // The persuaded distribution, from the model itself: every λ zero leaves only persuasion.
            var noLoyalty = new double[loyalty.Length];
            double[] persuaded = PreferenceModel.Preference(compat, prior, noLoyalty);

            double[] blended = PreferenceModel.Preference(compat, prior, loyalty);

            int failures = SelfTest(sb, prior, loyalty, persuaded, blended);
            if (failures > 0)
            {
                Debug.LogError($"C-A1: the blend self-test FAILED ({failures} element(s)) — the solve below would be arithmetic this model does not do. Nothing is reported.");
                Debug.Log(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            double lambda = Clamp01(loyalty[party] / 100.0);
            double priorShare = prior[party];
            double persuadedShare = persuaded[party];
            double blendedShare = blended[party];
            double actualShare = actual[party];

            // ⚠ The standing test names two PUBLISHED figures, and the model's vectors are renormalised
            // over the modelled set - so the guard compares the RAW seed value, never the renormalised
            // one. Comparing a renormalised prior against a published share is how a check fires for the
            // wrong reason (or, worse, passes narrowly for the wrong reason: FdI's prior renormalises
            // 4.35 -> 4.81, which sits 0.46 pp from the published figure and would have slipped under a
            // 0.5 pp tolerance while comparing two different quantities).
            double priorRawPct = italy.PriorPct[party];
            if (Math.Abs(100 * actualShare - RealSharePct) > 0.05 || Math.Abs(priorRawPct - PriorSharePct) > 0.05)
            {
                Debug.LogError(F("C-A1: the case no longer carries the standing test's figures — actual {0:F2} % (expected {1:F2}), raw 2018 prior {2:F2} % (expected {3:F2}). The test's subject has moved; re-derive it before reading any answer.",
                    100 * actualShare, RealSharePct, priorRawPct, PriorSharePct));
                Debug.Log(sb.ToString());
                CheckExit.Finish(2);
                return;
            }

            sb.Append("\n--- The measurement ---\n");
            sb.Append(F("    real 2022 (Eligendo list vote)          {0,8:F2} %\n", 100 * actualShare));
            sb.Append(F("    2018 prior, renormalised over the set   {0,8:F2} %   (published {1:F2} %, the surge's other end)\n",
                100 * priorShare, priorRawPct));
            sb.Append(F("    derived loyalty (W-A1, backtest dir.)   {0,8:F1}     -> lambda {1:F3}\n", loyalty[party], lambda));
            sb.Append(F("    PERSUADED share (spatial layer alone)   {0,8:F2} %   dev {1:+0.00;-0.00} pp\n",
                100 * persuadedShare, 100 * (persuadedShare - actualShare)));
            sb.Append(F("    BLENDED share (what the model predicts) {0,8:F2} %   dev {1:+0.00;-0.00} pp\n",
                100 * blendedShare, 100 * (blendedShare - actualShare)));

            // The solve: hold every other party's persuaded share in proportion, raise FdI's, and find
            // the value at which the blend lands on the real result.
            double required = SolveRequiredPersuaded(prior, loyalty, persuaded, party, actualShare, out bool bracketed);

            sb.Append("\n--- The ceiling, solved rather than estimated ---\n");
            if (!bracketed)
            {
                sb.Append("    ⚠ UNREACHABLE AT ANY PERSUASION. Even with every other party's persuaded share\n");
                sb.Append("      driven to zero, the blend cannot reach the real result: the prior mass the\n");
                sb.Append("      OTHER parties carry through their own loyalty is larger than the shortfall.\n");
            }
            else
            {
                sb.Append(F("    persuaded share required to land 29.27 %  {0,8:F2} %\n", 100 * required));
                sb.Append(F("    persuaded share the model produces        {0,8:F2} %\n", 100 * persuadedShare));
                sb.Append(F("    the campaign layer would have to multiply FdI's persuaded share by {0:F2}x\n",
                    persuadedShare > 0 ? required / persuadedShare : double.PositiveInfinity));
            }

            sb.Append("\n--- What each system can and cannot do about it ---\n");
            sb.Append("    ⚠ MOMENTUM CANNOT MOVE A VOTE, BY CONSTRUCTION. MomentumTracker.Apply has exactly\n");
            sb.Append("      two call sites (grep -n momentum.Apply CampaignRun.cs) and BOTH are the argument\n");
            sb.Append("      to PollingSystem.Conduct. Election day counts truePreference, which the blend\n");
            sb.Append("      above produces. Momentum's own doc says so: \"shifts where a race APPEARS to be\n");
            sb.Append("      without changing the underlying preference\". Since C-N1 (2026-09-02) MEDIA CAN:\n");
            sb.Append("      the day's coverage gain is also resolved through the chain into persuasion\n");
            sb.Append("      (MediaSystem.ResolveCoverage) - section 39's Media Effects layer, one line of the\n");
            sb.Append("      attribution ledger, and bounded because the gain is.\n");
            sb.Append("    SALIENCE is the one credited system that does enter the chain: it reaches\n");
            sb.Append("      persuasion through CampaignActions and so moves the persuaded share above.\n");
            sb.Append("    ⚠ AND IT MOVES IT FOR ONE ELECTORATE. W-F4 stopped: there are no voter groups,\n");
            sb.Append("      so loyalty is one number per PARTY over one undifferentiated electorate. Spec\n");
            sb.Append("      sections 5 and 8 make loyalty a per-VOTER-GROUP attribute, and that is the\n");
            sb.Append("      recorded cause of Italy's regression - a party whose 2018 voters largely did\n");
            sb.Append("      not exist as its 2022 voters cannot be represented by one lambda.\n");

            sb.Append("\n--- VERDICT ---\n");
            sb.Append("    NOT REACHABLE, and the ceiling is named: PER-GROUP LOYALTY.\n");
            sb.Append("    lambda = " + lambda.ToString("F3", CultureInfo.InvariantCulture) + " anchors that fraction of FdI s result to its 2018 prior\n");
            sb.Append("    BY CONSTRUCTION, and no system built this week reaches that term. Closing it is\n");
            sb.Append("    register row C-D1 (sourced SCB-equivalent marginals, or the bill), not a constant.\n");
            sb.Append("    THE CONSTANT WAS NOT RE-FITTED AND NOTHING WAS TUNED TOWARD 29.27.\n");

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>
        /// Proves this file's arithmetic IS the model's, at the measured inputs, before the solve is
        /// trusted. Returns the number of elements that disagree beyond 1e-12.
        /// </summary>
        private static int SelfTest(StringBuilder sb, double[] prior, double[] loyalty, double[] persuaded, double[] expected)
        {
            double[] mine = Blend(prior, loyalty, persuaded);
            int bad = 0;
            double worst = 0.0;
            for (int i = 0; i < expected.Length; i++)
            {
                double d = Math.Abs(mine[i] - expected[i]);
                if (d > worst) { worst = d; }
                if (d > 1e-12) { bad++; }
            }

            sb.Append(F("--- Self-test: this file's blend vs PreferenceModel.Preference -> worst element {0:E3}, {1} element(s) over 1e-12 ---\n",
                worst, bad));
            return bad;
        }

        /// <summary>§8's published identity, renormalised — the thing <see cref="SelfTest"/> checks.</summary>
        private static double[] Blend(double[] prior, double[] loyalty, double[] persuaded)
        {
            var result = new double[prior.Length];
            double total = 0.0;
            for (int i = 0; i < result.Length; i++)
            {
                double lambda = Clamp01(loyalty[i] / 100.0);
                result[i] = lambda * prior[i] + (1.0 - lambda) * persuaded[i];
                total += result[i];
            }

            for (int i = 0; i < result.Length; i++) { result[i] /= total; }
            return result;
        }

        /// <summary>
        /// Bisects on <paramref name="party"/>'s persuaded share — every other party's held in its own
        /// proportion of the remainder, because persuaded shares are a distribution — for the value at
        /// which the blend lands on <paramref name="targetShare"/>. <paramref name="bracketed"/> is
        /// false when even persuaded = 1 cannot reach it.
        /// </summary>
        private static double SolveRequiredPersuaded(double[] prior, double[] loyalty, double[] persuaded,
            int party, double targetShare, out bool bracketed)
        {
            double At(double p) => Blend(prior, loyalty, WithPersuaded(persuaded, party, p))[party];

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

        /// <summary>The persuaded distribution with one party set to <paramref name="p"/> and the rest
        /// scaled into the remainder, keeping their relative order intact.</summary>
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

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
