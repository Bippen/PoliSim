using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-B3's harness — proves each of §12's eight actions delivers its effect through §42's chain,
    /// and that **no action can write a vote share directly**.
    ///
    /// The item's bar, asserted three ways because one way would be too easy to satisfy:
    ///
    /// **(a) Structurally.** `ChainTrace` is reflected over and asserted to contain no member whose
    /// name suggests a share, preference, vote or party. An action that wanted to write a vote
    /// share would have nowhere to put it.
    ///
    /// **(b) Behaviourally — every stage is a NECESSARY factor.** For each of the eight, zeroing
    /// any one stage (audience, salience, issue match, credibility) must drive persuasion to
    /// exactly zero. A direct `+2 %` would survive all four; the chain cannot. This is the test the
    /// spec's §42 actually asks for.
    ///
    /// **(c) End to end.** A real campaign burst is run through the chain, converted to a
    /// compatibility bonus, and the resulting preference is recomputed by `PreferenceModel` — the
    /// same function that runs with no campaign at all. The campaign moves the INPUTS; the output
    /// is always derived. The vote share is then shown to have moved, and to have moved only
    /// because the chain did.
    /// </summary>
    public static class CampaignActionHarness
    {
        public static void Run()
        {
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-B3: §12's eight actions through §42's chain ===\n");

            // ---------- (a) structural: no share can be expressed ----------
            var forbidden = new[] { "share", "vote", "preference", "party", "percent" };
            var offenders = new StringBuilder();
            foreach (FieldInfo f in typeof(CampaignActions.ChainTrace).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                string lower = f.Name.ToLowerInvariant();
                foreach (string bad in forbidden)
                {
                    if (lower.Contains(bad)) { offenders.Append(f.Name).Append(' '); }
                }
            }

            failures += Assert(sb, "a. ChainTrace cannot express a vote share (no share/vote/preference/party member)",
                offenders.Length == 0,
                offenders.Length == 0 ? "8 members, none of them a share" : $"offending members: {offenders}");

            // ---------- (b) every stage is a necessary factor, for all eight ----------
            sb.Append("\n  the chain, per action (audience 100 000, salience 0.80, match 0.75, credibility 0.70, full spend):\n");
            sb.Append("  action              reach   exposure  relevance  persuasion  enthusiasm\n");

            int chainFailures = 0;
            foreach (CampaignActionKind kind in CampaignActions.TheEight)
            {
                CampaignActions.ActionSpec spec = CampaignActions.Spec(kind);
                double spend = spec.MoneyCost;

                CampaignActions.ChainTrace full = CampaignActions.Resolve(spec, 100_000, 0.80, 0.75, 0.70, spend);
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "  {0,-18} {1,8:N0}  {2,8:N0}   {3,6:F3}   {4,9:N1}   {5,9:N1}\n",
                    kind, full.Reach, full.Exposure, full.Relevance, full.Persuasion, full.Enthusiasm));

                if (!full.TravelledWholeChain) { chainFailures++; }

                // Each stage zeroed in turn must annihilate persuasion.
                var noAudience = CampaignActions.Resolve(spec, 0, 0.80, 0.75, 0.70, spend);
                var noSalience = CampaignActions.Resolve(spec, 100_000, 0.0, 0.75, 0.70, spend);
                var noMatch = CampaignActions.Resolve(spec, 100_000, 0.80, 0.0, 0.70, spend);
                var noCredibility = CampaignActions.Resolve(spec, 100_000, 0.80, 0.75, 0.0, spend);

                bool allZero = noAudience.Persuasion == 0.0 && noSalience.Persuasion == 0.0
                                                           && noMatch.Persuasion == 0.0 && noCredibility.Persuasion == 0.0;
                if (!allZero)
                {
                    chainFailures++;
                    sb.Append($"    FAIL {kind}: a zeroed stage did not annihilate persuasion " +
                              $"(audience {noAudience.Persuasion:F4}, salience {noSalience.Persuasion:F4}, " +
                              $"match {noMatch.Persuasion:F4}, credibility {noCredibility.Persuasion:F4})\n");
                }
            }

            failures += Assert(sb, "b. every stage is a NECESSARY factor for all eight actions",
                chainFailures == 0, $"{CampaignActions.TheEight.Length} actions x 4 zeroed stages, {chainFailures} failure(s)");

            // A general (issue-less) message still travels the chain, and an off-target one dies.
            CampaignActions.ActionSpec tv = CampaignActions.Spec(CampaignActionKind.TelevisionAd);
            var onTarget = CampaignActions.Resolve(tv, 100_000, 0.90, 0.80, 0.70, tv.MoneyCost);
            var offTarget = CampaignActions.Resolve(tv, 100_000, 0.05, 0.80, 0.70, tv.MoneyCost);
            failures += Assert(sb, "b2. a message on an issue the audience barely cares about is nearly inert",
                offTarget.Persuasion < onTarget.Persuasion * 0.1,
                string.Format(CultureInfo.InvariantCulture,
                    "on-target {0:N1} vs off-target {1:N1} persuasion", onTarget.Persuasion, offTarget.Persuasion));

            // Spend obeys §35 inside the chain too.
            var cheap = CampaignActions.Resolve(tv, 100_000, 0.90, 0.80, 0.70, tv.MoneyCost * 0.25);
            var dear = CampaignActions.Resolve(tv, 100_000, 0.90, 0.80, 0.70, tv.MoneyCost * 4.0);
            double perKronaCheap = cheap.Persuasion / (tv.MoneyCost * 0.25);
            double perKronaDear = dear.Persuasion / (tv.MoneyCost * 4.0);
            failures += Assert(sb, "b3. §35 still binds inside the chain (persuasion per krona falls with spend)",
                perKronaCheap > perKronaDear,
                string.Format(CultureInfo.InvariantCulture, "{0:E2} vs {1:E2} persuasion per krona", perKronaCheap, perKronaDear));

            // ---------- (c) end to end: the campaign moves inputs, never the output ----------
            double[] compatibility = { 62.0, 58.0, 45.0, 38.0 };
            double[] prior = { 0.34, 0.30, 0.22, 0.14 };
            double[] loyalty = { 70.0, 65.0, 60.0, 55.0 };

            double[] before = PreferenceModel.Preference(compatibility, prior, loyalty);

            var pressure = new CampaignPressure(4);
            // Party 0 runs a week: three rallies, two town halls, a TV buy, daily door-knocking.
            AddBurst(pressure, 0, CampaignActionKind.Rally, 3, 120_000);
            AddBurst(pressure, 0, CampaignActionKind.TownHall, 2, 120_000);
            AddBurst(pressure, 0, CampaignActionKind.TelevisionAd, 1, 1_500_000);
            AddBurst(pressure, 0, CampaignActionKind.DoorToDoor, 7, 120_000);
            // Party 1 does far less.
            AddBurst(pressure, 1, CampaignActionKind.SocialPost, 5, 200_000);

            double[] bonus = pressure.ToCompatibilityBonus();
            var boosted = new double[compatibility.Length];
            for (int i = 0; i < boosted.Length; i++) { boosted[i] = compatibility[i] + bonus[i]; }

            double[] after = PreferenceModel.Preference(boosted, prior, loyalty);

            sb.Append("\n  end to end - a week of campaigning, preference RECOMPUTED by PreferenceModel:\n");
            sb.Append("  party  compat  +bonus   before    after     delta\n");
            for (int i = 0; i < 4; i++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "  {0,-5}  {1,6:F2}  {2,6:F2}  {3,7:F3}%  {4,7:F3}%  {5,8:+0.000;-0.000;0.000}pp\n",
                    i, compatibility[i], bonus[i], 100 * before[i], 100 * after[i], 100 * (after[i] - before[i])));
            }

            failures += Assert(sb, "c1. the campaigning party's share rose",
                after[0] > before[0],
                string.Format(CultureInfo.InvariantCulture, "{0:F3} -> {1:F3} pp", 100 * before[0], 100 * after[0]));
            failures += Assert(sb, "c2. shares still sum to 1 (the campaign redistributed, it did not create votes)",
                Math.Abs(Sum(after) - 1.0) < 1e-9, $"sum {Sum(after):F12}");

            // The decisive one: with the chain severed (zero audience everywhere) an identical
            // campaign must move NOTHING.
            var deadPressure = new CampaignPressure(4);
            AddBurst(deadPressure, 0, CampaignActionKind.Rally, 3, 0);
            AddBurst(deadPressure, 0, CampaignActionKind.TownHall, 2, 0);
            AddBurst(deadPressure, 0, CampaignActionKind.TelevisionAd, 1, 0);
            AddBurst(deadPressure, 0, CampaignActionKind.DoorToDoor, 7, 0);
            double[] deadBonus = deadPressure.ToCompatibilityBonus();
            var deadBoost = new double[compatibility.Length];
            for (int i = 0; i < deadBoost.Length; i++) { deadBoost[i] = compatibility[i] + deadBonus[i]; }
            double[] afterDead = PreferenceModel.Preference(deadBoost, prior, loyalty);

            bool unchanged = true;
            for (int i = 0; i < 4; i++) { if (Math.Abs(afterDead[i] - before[i]) > 1e-12) { unchanged = false; } }

            failures += Assert(sb, "c3. THE DECISIVE TEST - the same campaign with the chain severed (no audience) moves NOTHING",
                unchanged, "a direct vote delta would have moved the shares anyway");

            sb.Append($"\n=== CampaignActionHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static void AddBurst(CampaignPressure pressure, int party, CampaignActionKind kind, int times, double audience)
        {
            CampaignActions.ActionSpec spec = CampaignActions.Spec(kind);
            for (int i = 0; i < times; i++)
            {
                pressure.Add(party, CampaignActions.Resolve(spec, audience, 0.80, 0.75, 0.70, spec.MoneyCost));
            }
        }

        private static double Sum(double[] v)
        {
            double s = 0.0;
            foreach (double x in v) { s += x; }
            return s;
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
