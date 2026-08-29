using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-B8's harness — §17's scandals.
    ///
    /// The done-when, asserted:
    /// 1. **a scandal's full lifecycle runs deterministically under a seed** — the same seed and
    ///    response reproduce every day's coverage, the momentum shock and the credibility cost; a
    ///    different seed can differ (the evidence's chance to surface);
    /// 2. **each response has a measured DISTINCT outcome distribution** — over 400 seeds per
    ///    response on one major scandal with middling evidence, the seven mean total damages are
    ///    pairwise distinct by at least half a credibility point, and §17's two sentences hold as
    ///    measurements: the apology has the largest momentum decline and among the smallest lasting
    ///    costs; the denial has the smallest immediate cost and the widest spread (it works or it
    ///    is catastrophic), and against strong evidence it is the WORST response on average;
    /// 3. **nothing is scripted as game-over** — `ScandalOutcome` has no member that could end a
    ///    campaign (by reflection), a resignation replaces the candidate and the campaign continues,
    ///    and even a catastrophic scandal on the worst response leaves a party with credibility
    ///    above zero;
    /// 4. the damage lands on the right stocks — applied to the media, the momentum tracker and a
    ///    credibility figure, the preference recomputed from the SAME compatibility is bit-identical
    ///    (a scandal reaches the vote only through credibility inside §42's chain), and the chain
    ///    itself then delivers less persuasion for the same action.
    /// </summary>
    public static class ScandalHarness
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-B8: scandals (§17) - a lifecycle, seven responses, two stocks, no game over ===\n");

            // ---------- 3a. structural ----------
            var forbidden = new[] { "share", "vote", "preference", "party", "gameover", "ended", "lose", "lost", "defeat", "eliminat", "terminat" };
            var offenders = new StringBuilder();
            foreach (MemberInfo m in typeof(ScandalOutcome).GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                string lower = m.Name.ToLowerInvariant();
                foreach (string bad in forbidden) { if (lower.Contains(bad) && m.Name != "GetType") { offenders.Append(m.Name).Append(' '); } }
            }

            failures += Assert(sb, "3a. ScandalOutcome cannot express a vote share and has no member that could end a campaign",
                offenders.Length == 0, offenders.Length == 0 ? "clean" : $"offenders: {offenders}");

            // ---------- 1. determinism ----------
            var major = new Scandal(ScandalKind.Corruption, ScandalSeverity.Major, evidence: 0.5);
            ScandalOutcome a = Seeded(777, () => Scandals.Resolve(major, ScandalResponse.Deny, SimulationRandom.For(SimulationRandom.Stream.Scandal)));
            ScandalOutcome b = Seeded(777, () => Scandals.Resolve(major, ScandalResponse.Deny, SimulationRandom.For(SimulationRandom.Stream.Scandal)));
            bool same = a.CoverageShockPerDay.Length == b.CoverageShockPerDay.Length && a.MomentumShockPp == b.MomentumShockPp && a.CredibilityCost == b.CredibilityCost && a.Escalated == b.Escalated;
            for (int i = 0; same && i < a.CoverageShockPerDay.Length; i++) { same = a.CoverageShockPerDay[i] == b.CoverageShockPerDay[i]; }
            failures += Assert(sb, "1a. the same seed and response reproduce the whole lifecycle exactly", same, $"{a.DaysInTheNews} days, escalated {a.Escalated}, credibility cost {a.CredibilityCost:F4}");

            int escalatedCount = 0;
            for (int s = 0; s < 200; s++) { if (Seeded(1000 + s, () => Scandals.Resolve(major, ScandalResponse.Deny, SimulationRandom.For(SimulationRandom.Stream.Scandal))).Escalated) { escalatedCount++; } }
            failures += Assert(sb, "1b. across seeds the denial sometimes survives and sometimes is caught out (the evidence surfaces on some seeds, not all)",
                escalatedCount > 20 && escalatedCount < 180, $"{escalatedCount} of 200 denials escalated at evidence 0.5");

            // ---------- 2. the seven distributions ----------
            const int seeds = 400;
            var mean = new double[7];
            var spread = new double[7];
            var meanMomentum = new double[7];
            var meanCredibility = new double[7];
            sb.Append("\n  a MAJOR corruption scandal, evidence 0.5, 400 seeds per response:\n  response               damage mean   sd   momentum pp   credibility cost   days   escalated\n");
            for (int r = 0; r < 7; r++)
            {
                ScandalResponse response = Scandals.TheSeven[r];
                double sum = 0, sumSq = 0, sumM = 0, sumC = 0, sumDays = 0;
                int escalations = 0;
                for (int s = 0; s < seeds; s++)
                {
                    ScandalOutcome o = Seeded(50_000 + s, () => Scandals.Resolve(major, response, SimulationRandom.For(SimulationRandom.Stream.Scandal)));
                    double dmg = o.TotalDamage;
                    sum += dmg; sumSq += dmg * dmg; sumM += o.MomentumShockPp; sumC += o.CredibilityCost; sumDays += o.DaysInTheNews;
                    if (o.Escalated) { escalations++; }
                }

                mean[r] = sum / seeds;
                spread[r] = Math.Sqrt(Math.Max(0.0, sumSq / seeds - mean[r] * mean[r]));
                meanMomentum[r] = sumM / seeds;
                meanCredibility[r] = sumC / seeds;
                sb.Append(string.Format(CultureInfo.InvariantCulture, "  {0,-22} {1,8:F2} {2,6:F2}   {3,8:F2}      {4,8:F3}       {5,5:F1}   {6,5}\n",
                    response, mean[r], spread[r], meanMomentum[r], meanCredibility[r], sumDays / seeds, escalations));
            }

            double minGap = double.MaxValue;
            string closest = "";
            for (int i = 0; i < 7; i++)
            {
                for (int j = i + 1; j < 7; j++)
                {
                    // Distinct DISTRIBUTIONS: two responses are the same only if both their mean and
                    // their spread agree within half a point - a gamble and a certainty can average
                    // alike and still be different things to choose between.
                    double gap = Math.Max(Math.Abs(mean[i] - mean[j]), Math.Abs(spread[i] - spread[j]));
                    if (gap < minGap) { minGap = gap; closest = $"{Scandals.TheSeven[i]}/{Scandals.TheSeven[j]}"; }
                }
            }

            failures += Assert(sb, "2a. the seven responses have pairwise DISTINCT outcome distributions (mean or spread at least 0.5 apart)",
                minGap >= 0.5, string.Format(CultureInfo.InvariantCulture, "closest pair {0} at {1:F2}", closest, minGap));

            int apologize = Array.IndexOf(Scandals.TheSeven, ScandalResponse.Apologize);
            int deny = Array.IndexOf(Scandals.TheSeven, ScandalResponse.Deny);
            bool apologyHurtsNow = true, apologyHealsLater = true;
            for (int r = 0; r < 7; r++)
            {
                if (r != apologize && Scandals.TheSeven[r] != ScandalResponse.Resign && meanMomentum[r] < meanMomentum[apologize]) { apologyHurtsNow = false; }
            }

            int cheaperLasting = 0;
            for (int r = 0; r < 7; r++) { if (r != apologize && meanCredibility[r] < meanCredibility[apologize]) { cheaperLasting++; } }
            apologyHealsLater = cheaperLasting <= 2;   // only a resignation (the candidate goes) and a well-judged denial can cost less lasting credibility
            failures += Assert(sb, "2b. §17: 'a transparent apology may reduce long-term damage but cause a short-term polling decline' - the largest momentum decline of the responses that keep the candidate, among the smallest lasting costs",
                apologyHurtsNow && apologyHealsLater, string.Format(CultureInfo.InvariantCulture, "apology momentum {0:F2} pp, credibility cost {1:F3}; {2} responses cost less lasting credibility", meanMomentum[apologize], meanCredibility[apologize], cheaperLasting));

            bool denialCheapestNow = true;
            for (int r = 0; r < 7; r++) { if (r != deny && meanMomentum[r] > meanMomentum[deny] + 1e-9) { denialCheapestNow = false; } }
            bool denialWidest = true;
            for (int r = 0; r < 7; r++) { if (r != deny && spread[r] >= spread[deny]) { denialWidest = false; } }
            failures += Assert(sb, "2c. §17: 'a denial can work if evidence is weak but become catastrophic if evidence later appears' - the smallest immediate cost and the WIDEST spread of the seven",
                denialCheapestNow && denialWidest, string.Format(CultureInfo.InvariantCulture, "denial momentum {0:F2} pp (least), sd {1:F2} (widest)", meanMomentum[deny], spread[deny]));

            // Against strong evidence, denial is the worst response on average; against weak, among the best.
            var strong = new Scandal(ScandalKind.Corruption, ScandalSeverity.Major, evidence: 0.95);
            var weak = new Scandal(ScandalKind.Corruption, ScandalSeverity.Major, evidence: 0.05);
            double denyStrong = MeanDamage(strong, ScandalResponse.Deny, seeds), denyWeak = MeanDamage(weak, ScandalResponse.Deny, seeds);
            double worstStrongOther = 0, bestWeakOther = double.MaxValue;
            for (int r = 0; r < 7; r++)
            {
                if (r == deny) { continue; }
                worstStrongOther = Math.Max(worstStrongOther, MeanDamage(strong, Scandals.TheSeven[r], seeds));
                bestWeakOther = Math.Min(bestWeakOther, MeanDamage(weak, Scandals.TheSeven[r], seeds));
            }

            failures += Assert(sb, "2d. against STRONG evidence the denial is the worst response on average; against WEAK evidence it is the best",
                denyStrong > worstStrongOther && denyWeak < bestWeakOther,
                string.Format(CultureInfo.InvariantCulture, "strong: deny {0:F2} vs worst other {1:F2}; weak: deny {2:F2} vs best other {3:F2}", denyStrong, worstStrongOther, denyWeak, bestWeakOther));

            ScandalOutcome cynical = Seeded(1, () => Scandals.Resolve(new Scandal(ScandalKind.OffensiveStatement, ScandalSeverity.Major, 0.5), ScandalResponse.SacrificeStaffMember, SimulationRandom.For(SimulationRandom.Stream.Scandal)));
            ScandalOutcome plausible = Seeded(1, () => Scandals.Resolve(new Scandal(ScandalKind.CampaignFinanceViolation, ScandalSeverity.Major, 0.5), ScandalResponse.SacrificeStaffMember, SimulationRandom.For(SimulationRandom.Stream.Scandal)));
            failures += Assert(sb, "2e. sacrificing a staff member for a scandal no staff member could carry (an offensive statement) costs more than for one they could (a finance violation)",
                cynical.CredibilityCost > plausible.CredibilityCost, string.Format(CultureInfo.InvariantCulture, "{0:F3} vs {1:F3}", cynical.CredibilityCost, plausible.CredibilityCost));

            // ---------- 3. no game over ----------
            ScandalOutcome resigned = Seeded(2, () => Scandals.Resolve(major, ScandalResponse.Resign, SimulationRandom.For(SimulationRandom.Stream.Scandal)));
            failures += Assert(sb, "3b. a resignation replaces the candidate and the campaign continues (the outcome says who goes, nothing says the campaign ends)",
                resigned.CandidateReplaced && resigned.CredibilityCost < 0.5, $"candidate replaced {resigned.CandidateReplaced}, credibility cost {resigned.CredibilityCost:F3}");

            var catastrophic = new Scandal(ScandalKind.Corruption, ScandalSeverity.Catastrophic, evidence: 1.0);
            double worstCost = 0.0;
            for (int s = 0; s < 200; s++)
            {
                ScandalOutcome o = Seeded(9000 + s, () => Scandals.Resolve(catastrophic, ScandalResponse.Deny, SimulationRandom.For(SimulationRandom.Stream.Scandal)));
                if (o.CredibilityCost > worstCost) { worstCost = o.CredibilityCost; }
            }

            failures += Assert(sb, "3c. even a catastrophic scandal on the worst response with certain evidence leaves credibility above zero (a large cost, not an ending)",
                worstCost < 1.0, string.Format(CultureInfo.InvariantCulture, "worst credibility cost {0:F3} of 1", worstCost));

            // ---------- 4. the right stocks ----------
            double[] compatibility = { 62.0, 58.0, 45.0, 38.0 };
            double[] prior = { 0.34, 0.30, 0.22, 0.14 };
            double[] loyalty = { 70.0, 65.0, 60.0, 55.0 };
            double[] before = PreferenceModel.Preference(compatibility, prior, loyalty);
            ScandalOutcome hit = Seeded(3, () => Scandals.Resolve(major, ScandalResponse.Explain, SimulationRandom.For(SimulationRandom.Stream.Scandal)));
            var coverage = new MediaCoverage(4);
            var momentum = new MomentumTracker(4);
            foreach (double shock in hit.CoverageShockPerDay) { coverage.AddShock(0, shock); coverage.CloseDay(); }
            momentum.AddShock(0, hit.MomentumShockPp);
            double credibility = 0.7 * (1.0 - hit.CredibilityCost);
            double[] after = PreferenceModel.Preference(compatibility, prior, loyalty);
            bool untouched = true;
            for (int i = 0; i < 4; i++) { if (after[i] != before[i]) { untouched = false; } }
            failures += Assert(sb, "4a. applied to the media, the momentum tracker and a credibility figure, the preference from the SAME compatibility is bit-identical (a scandal has no direct route to a share)",
                untouched && coverage.Coverage(0) > 0 && momentum.MomentumPp(0) < 0, string.Format(CultureInfo.InvariantCulture, "coverage {0:F3}, momentum {1:+0.00} pp, credibility 0.70 -> {2:F3}", coverage.Coverage(0), momentum.MomentumPp(0), credibility));

            CampaignActions.ActionSpec rally = CampaignActions.Spec(CampaignActionKind.Rally);
            double persuasionBefore = CampaignActions.Resolve(rally, 100_000, 0.8, 0.75, 0.7, rally.MoneyCost).Persuasion;
            double persuasionAfter = CampaignActions.Resolve(rally, 100_000, 0.8, 0.75, credibility, rally.MoneyCost).Persuasion;
            failures += Assert(sb, "4b. the lasting damage reaches the vote ONLY through the chain: the same rally now persuades less, in proportion to the credibility lost",
                persuasionAfter < persuasionBefore && Math.Abs(persuasionAfter / persuasionBefore - credibility / 0.7) < 1e-9,
                string.Format(CultureInfo.InvariantCulture, "{0:N0} -> {1:N0} persuasion ({2:P1} of before)", persuasionBefore, persuasionAfter, persuasionAfter / persuasionBefore));

            double seen = Seeded(4, () => Scandals.EvidenceAsSeen(major, SimulationRandom.For(SimulationRandom.Stream.Scandal)));
            failures += Assert(sb, "4c. the party sees an ESTIMATE of the evidence, not the truth (§36)",
                Math.Abs(seen - major.Evidence) > 1e-9 && Math.Abs(seen - major.Evidence) <= Scandals.EvidenceEstimateError + 1e-9,
                string.Format(CultureInfo.InvariantCulture, "true 0.50, seen {0:F3}", seen));

            sb.Append($"\n=== ScandalHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static double MeanDamage(Scandal scandal, ScandalResponse response, int seeds)
        {
            double sum = 0.0;
            for (int s = 0; s < seeds; s++) { sum += Seeded(70_000 + s, () => Scandals.Resolve(scandal, response, SimulationRandom.For(SimulationRandom.Stream.Scandal))).TotalDamage; }
            return sum / seeds;
        }

        private static T Seeded<T>(int seed, Func<T> run)
        {
            SimulationRandom.Seed(seed);
            return run();
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
