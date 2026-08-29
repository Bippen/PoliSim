using System;
using System.Globalization;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-E3's model half — proves the estimate BAND is honest before any screen draws it.
    ///
    /// The item's bar is "ranges, never false precision", and a range is only honest if it is
    /// actually the model's uncertainty. Four assertions, in the order they could fail:
    ///
    /// **1. The corners really are the extremes.** `ResolveBand` evaluates §42's chain at (low,low)
    /// and (high,high) rather than sweeping, on the argument that persuasion is monotone in both
    /// salience and issue-match. That argument is ASSERTED here by brute force — a 41×41 sweep of
    /// the uncertainty box for every one of §12's eight actions — because a comment claiming
    /// monotonicity is worth nothing if a later stage breaks it. If a future change makes the chain
    /// non-monotone, this fails and `ResolveBand` must become a sweep.
    ///
    /// **2. A wider measured interval gives a wider estimate, and a perfect measurement gives a
    /// point.** The band's width must be a function of the MEASUREMENT, so that buying better
    /// polling (§21, §36) visibly narrows what the player is told. A band whose width did not
    /// respond to its input would be decoration.
    ///
    /// **3. An unmeasured quantity yields NO estimate, not a wide one.** §36's gate: the screen must
    /// be able to say "not measured" rather than print a number it cannot justify.
    ///
    /// **4. The band still cannot express a vote share.** `ChainBand` is three `ChainTrace`s, and
    /// W-B3's structural bar applies to it unchanged — reflection over it finds no share, no
    /// preference, no party. Widening the chain's output into an interval must not be a back door
    /// around the rule that an action never writes a share.
    /// </summary>
    public static class ChainBandHarness
    {
        public static void Run()
        {
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-E3: the estimate band (§42's chain across measured uncertainty) ===\n");

            const double audience = 120_000;
            const double salience = 0.55;
            const double salienceError = 0.12;
            const double match = 0.60;
            const double matchError = 0.15;
            const double credibility = 0.70;

            // ---------- 1. the corners ARE the extremes, swept not argued ----------
            const int steps = 40;
            int cornerFailures = 0;
            double worstUnder = 0.0, worstOver = 0.0;

            foreach (CampaignActionKind kind in CampaignActions.TheEight)
            {
                CampaignActions.ActionSpec spec = CampaignActions.Spec(kind);
                CampaignActions.ChainBand band = CampaignActions.ResolveBand(spec, audience,
                    salience, salienceError, match, matchError, credibility, spec.MoneyCost);

                for (int i = 0; i <= steps; i++)
                {
                    double s = salience - salienceError + 2.0 * salienceError * i / steps;
                    for (int j = 0; j <= steps; j++)
                    {
                        double m = match - matchError + 2.0 * matchError * j / steps;
                        double p = CampaignActions.Resolve(spec, audience, s, m, credibility, spec.MoneyCost).Persuasion;

                        // A tolerance of zero would fail on floating-point equality at the corners
                        // themselves; 1e-9 relative is far below anything the screen renders.
                        double tol = 1e-9 * Math.Max(1.0, Math.Abs(p));
                        if (p < band.Low.Persuasion - tol)
                        {
                            cornerFailures++;
                            worstUnder = Math.Max(worstUnder, band.Low.Persuasion - p);
                        }

                        if (p > band.High.Persuasion + tol)
                        {
                            cornerFailures++;
                            worstOver = Math.Max(worstOver, p - band.High.Persuasion);
                        }
                    }
                }
            }

            failures += Assert(sb, "1. the two corners bound the WHOLE uncertainty box (41x41 sweep x 8 actions)",
                cornerFailures == 0,
                cornerFailures == 0
                    ? $"{CampaignActions.TheEight.Length * 41 * 41:N0} interior points, none outside the band"
                    : string.Format(CultureInfo.InvariantCulture,
                        "{0} point(s) escaped - worst under {1:E3}, worst over {2:E3}: the chain is NOT monotone and ResolveBand must become a sweep",
                        cornerFailures, worstUnder, worstOver));

            // ---------- 2. the width tracks the MEASUREMENT ----------
            CampaignActions.ActionSpec tv = CampaignActions.Spec(CampaignActionKind.TelevisionAd);
            var tight = CampaignActions.ResolveBand(tv, audience, salience, 0.02, match, 0.02, credibility, tv.MoneyCost);
            var loose = CampaignActions.ResolveBand(tv, audience, salience, 0.20, match, 0.20, credibility, tv.MoneyCost);
            var exact = CampaignActions.ResolveBand(tv, audience, salience, 0.0, match, 0.0, credibility, tv.MoneyCost);

            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "  television ad, persuasion: exact {0:N1} · tight poll {1:N1}-{2:N1} · loose poll {3:N1}-{4:N1}\n",
                exact.Mid.Persuasion, tight.Low.Persuasion, tight.High.Persuasion,
                loose.Low.Persuasion, loose.High.Persuasion));

            failures += Assert(sb, "2a. a wider measured interval gives a wider estimate",
                loose.PersuasionSpan > tight.PersuasionSpan * 2.0,
                string.Format(CultureInfo.InvariantCulture, "span {0:N1} vs {1:N1}",
                    loose.PersuasionSpan, tight.PersuasionSpan));

            failures += Assert(sb, "2b. a PERFECT measurement collapses the band to a point",
                Math.Abs(exact.PersuasionSpan) < 1e-12,
                string.Format(CultureInfo.InvariantCulture, "span {0:E3}", exact.PersuasionSpan));

            failures += Assert(sb, "2c. the mid estimate lies inside its own band",
                loose.Mid.Persuasion >= loose.Low.Persuasion && loose.Mid.Persuasion <= loose.High.Persuasion,
                string.Format(CultureInfo.InvariantCulture, "{0:N1} in [{1:N1}, {2:N1}]",
                    loose.Mid.Persuasion, loose.Low.Persuasion, loose.High.Persuasion));

            // ---------- 3. unmeasured is NOT "wide" ----------
            var unmeasured = CampaignActions.ResolveBand(tv, audience, salience, salienceError,
                match, matchError, credibility, tv.MoneyCost, measured: false);
            failures += Assert(sb, "3. §36 - an unpolled quantity yields NO estimate, not a wide one",
                !unmeasured.Measured && unmeasured.Mid.Persuasion == 0.0,
                "Measured=false, so the screen must print 'not measured' rather than a number");

            // ---------- 4. W-B3's structural bar still holds over the band ----------
            var forbidden = new[] { "share", "vote", "preference", "party", "percent" };
            var offenders = new StringBuilder();
            foreach (System.Reflection.FieldInfo f in typeof(CampaignActions.ChainBand)
                         .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                string lower = f.Name.ToLowerInvariant();
                foreach (string bad in forbidden)
                {
                    if (lower.Contains(bad)) { offenders.Append(f.Name).Append(' '); }
                }
            }

            failures += Assert(sb, "4. W-B3's bar survives the widening - a ChainBand cannot express a vote share",
                offenders.Length == 0,
                offenders.Length == 0 ? "three ChainTraces and a flag, none of them a share" : $"offenders: {offenders}");

            sb.Append($"\n=== ChainBandHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
