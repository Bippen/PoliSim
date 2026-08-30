using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-C2's harness — opponent reactivity: the AI answers the player's moves within its
    /// personality's tempo, on what it can SEE (§36), never on the truth.
    ///
    /// The scenario: W-C1's staging (eight Swedish parties, seed by seed) with L replaced by a
    /// SCRIPTED party — the player's stand-in — that from day 5 works Blekinge län every day (a town
    /// hall and a canvassing day) and announces policy AGAINST S every day. Blekinge is a region no
    /// AI would otherwise choose (the smallest valkrets but one), so anything an AI does there is a
    /// reaction. The same seeds are run without the script as the control.
    ///
    /// The done-when, asserted: **a professional AI reallocates to a threatened region and a
    /// chaotic one does not** — over ten seeds POOLED (a mean of per-seed
    /// shares can rest on a single act, so every seed's acts share one denominator and the counts
    /// are printed in full), S (professional, reactivity 1) puts a measurably larger share of its
    /// local actions into Blekinge with the script than without, and C
    /// (chaotic, reactivity 0) does not; **attacks are answered** — the party that looks answers at
    /// its own tempo (one a week, a ceiling the campaign presses it against, so what an attack
    /// changes is WHOM it answers) and the party that never looks answers not once; **the reaction
    /// is on the public record** — the scripted acts are on
    /// `PublicActivity` and nothing there could hold a truth (by reflection); the scripted run
    /// reproduces under a seed. The establishment party (reactivity 0.7) is measured and reported;
    /// so is the lag to the first act in Blekinge.
    /// </summary>
    public static class CampaignReactivityHarness
    {
        private const int ScriptStart = 5;
        private const int ScriptEnd = 45;
        private const double PlayerWarChest = 6_000_000.0;   // [AUTHORED-DRAFT] the scripted player's purse - enough to work a region for 40 days
        private static readonly int[] Seeds = { 777, 778, 779, 780, 781, 782, 783, 784, 785, 786 };
        private const int S = 0, M = 2, C = 4, L = 7;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-C2: opponent reactivity - a contested region defended, an attack answered, on the public record only, within the personality's tempo ===\n");

            // ---------- structural: the public record cannot hold a truth ----------
            {
                var forbidden = new[] { "truth", "true", "actual", "underlying", "real", "preference", "hidden", "share", "vote" };
                var offenders = new StringBuilder();
                foreach (MemberInfo m in typeof(PublicActivity).GetMembers(BindingFlags.Public | BindingFlags.Instance))
                {
                    string lower = m.Name.ToLowerInvariant();
                    foreach (string bad in forbidden) { if (lower.Contains(bad) && m.Name != "GetType") { offenders.Append(m.Name).Append(' '); } }
                }

                failures += Assert(sb, "0a. PublicActivity cannot carry a truth (no truth/actual/preference/share/vote member)", offenders.Length == 0,
                    offenders.Length == 0 ? "clean" : $"offenders: {offenders}");
            }

            CampaignRun.Setup control = CampaignAiHarness.BuildSetup(out _);
            int blekinge = Array.FindIndex(control.Regions, r => r.Name.StartsWith("Blekinge"));
            failures += Assert(sb, "0b. the scenario's region exists in the staging", blekinge >= 0, blekinge >= 0 ? control.Regions[blekinge].Name : "no Blekinge");
            CampaignRun.Setup scripted = WithScriptedPlayer(control, blekinge);

            // ---------- determinism, and the record ----------
            {
                CampaignRun.Result a = CampaignAiHarness.RunSeeded(scripted, Seeds[0]);
                CampaignRun.Result b = CampaignAiHarness.RunSeeded(scripted, Seeds[0]);
                failures += Assert(sb, "1a. the scripted run reproduces under a seed", a.Digest == b.Digest, $"{a.Digest} twice");
                failures += Assert(sb, "1b. the scripted party's acts are on the public record, decayed: Blekinge pressure as S sees it is positive at the end, and the attacks on S are counted",
                    a.Activity.PressureOn(blekinge, S) > 0.0 && a.Activity.AttacksOn(S) > 0.0 && a.Activity.Local(L, blekinge) > 0.0,
                    string.Format(CultureInfo.InvariantCulture, "pressure on Blekinge as S sees it {0:F2}; attacks on S {1:F2}; L's own record there {2:F2}",
                        a.Activity.PressureOn(blekinge, S), a.Activity.AttacksOn(S), a.Activity.Local(L, blekinge)));
                int spent = a.Parties[L].TotalActions;
                failures += Assert(sb, "1c. the scripted party played its script through the same seams (paid, resolved, logged)", spent >= 3 * (ScriptEnd - ScriptStart) - 3 && a.Parties[L].MoneySpentOnActions > 0,
                    string.Format(CultureInfo.InvariantCulture, "{0} actions, {1:N0} kr", spent, a.Parties[L].MoneySpentOnActions));
            }

            // ---------- the measured scenario over ten seeds ----------
            {
                // Both readings are kept: the POOLED share (every seed's local acts in one
                // denominator) is what 2a and 2b are asserted on, because a mean of per-seed shares
                // can be carried by two seeds in which the party made a single local act; the mean
                // of the per-seed shares is reported beside it, with the counts, so the two can be
                // read against each other rather than one standing for the other.
                var withThere = new int[3]; var withLocal = new int[3];
                var withoutThere = new int[3]; var withoutLocal = new int[3];
                var withShare = new double[3]; var withoutShare = new double[3];
                var withAnswers = new double[3]; var withoutAnswers = new double[3];
                var withNotes = new double[3]; var withoutNotes = new double[3];
                var firstDay = new List<int>();
                int daysRun = 0;
                int[] parties = { S, M, C };
                foreach (int seed in Seeds)
                {
                    CampaignRun.Result on = CampaignAiHarness.RunSeeded(scripted, seed);
                    CampaignRun.Result off = CampaignAiHarness.RunSeeded(control, seed);
                    for (int i = 0; i < parties.Length; i++)
                    {
                        LocalCounts(on.Parties[parties[i]], control.Regions[blekinge].Name, out int onThere, out int onLocal);
                        LocalCounts(off.Parties[parties[i]], control.Regions[blekinge].Name, out int offThere, out int offLocal);
                        withThere[i] += onThere; withLocal[i] += onLocal;
                        withoutThere[i] += offThere; withoutLocal[i] += offLocal;
                        withShare[i] += (onLocal > 0 ? (double)onThere / onLocal : 0.0) / Seeds.Length;
                        withoutShare[i] += (offLocal > 0 ? (double)offThere / offLocal : 0.0) / Seeds.Length;
                        withAnswers[i] += on.Parties[parties[i]].Answers / (double)Seeds.Length;
                        withoutAnswers[i] += off.Parties[parties[i]].Answers / (double)Seeds.Length;
                        withNotes[i] += Announcements(on.Parties[parties[i]]) / (double)Seeds.Length;
                        withoutNotes[i] += Announcements(off.Parties[parties[i]]) / (double)Seeds.Length;
                    }

                    daysRun = on.DaysRun;
                    int first = FirstDayIn(on.Parties[S], control.Regions[blekinge].Name);
                    if (first >= 0) { firstDay.Add(first); }
                }

                var pooledWith = new double[3]; var pooledWithout = new double[3];
                for (int i = 0; i < parties.Length; i++)
                {
                    pooledWith[i] = withLocal[i] > 0 ? (double)withThere[i] / withLocal[i] : 0.0;
                    pooledWithout[i] = withoutLocal[i] > 0 ? (double)withoutThere[i] / withoutLocal[i] : 0.0;
                }

                sb.Append("\n  Blekinge's share of the party's local actions from day 5, POOLED over ten seeds (the counts in full), with the script against without:\n");
                string[] names = { "S professional (reactivity 1.0)", "M establishment (0.7)", "C chaotic (0.0)" };
                for (int i = 0; i < parties.Length; i++)
                {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-32} {1} of {2} local acts -> {3} of {4}; share {5:P1} -> {6:P1} ({7:+0.0;-0.0} pp); mean of the per-seed shares {8:P1} -> {9:P1}; answers made {10:F1} -> {11:F1}; policy announcements in all (answers and its own weighing alike) {12:F1} -> {13:F1}\n",
                        names[i], withoutThere[i], withoutLocal[i], withThere[i], withLocal[i],
                        pooledWithout[i], pooledWith[i], (pooledWith[i] - pooledWithout[i]) * 100,
                        withoutShare[i], withShare[i], withoutAnswers[i], withAnswers[i], withoutNotes[i], withNotes[i]));
                }

                // The denominators are the finding: the rational personalities scarcely campaign
                // locally at all, so a reaction is most of their local campaign.
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "  Local acts per campaign (the denominator above): S {0:F1}, M {1:F1}, C {2:F1} - §33's expected value buys a national audience with the same hours, so a professional party goes to a region only when a rule sends it (the reaction is {3} of S's {4} local acts over ten campaigns)\n",
                    withLocal[0] / (double)Seeds.Length, withLocal[1] / (double)Seeds.Length, withLocal[2] / (double)Seeds.Length, withThere[0], withLocal[0]));

                if (firstDay.Count > 0)
                {
                    double meanLag = 0; foreach (int d in firstDay) { meanLag += d - ScriptStart; }
                    meanLag /= firstDay.Count;
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "  S acted in Blekinge in {0} of {1} seeds, first act a mean {2:F1} days after the script began (the tempo, measured not asserted)\n", firstDay.Count, Seeds.Length, meanLag));
                }
                else
                {
                    sb.Append("  S never acted in Blekinge\n");
                }

                // The share is reported, but the ASSERTION is the count: a professional party makes
                // 0.7 local acts a campaign (§33's expected value buys television and takes the free
                // interview - the finding above), so a percentage of that denominator would be a
                // figure the scenario cannot carry. What the done-when actually asks is whether the
                // party goes to the threatened region at all, and it can be counted.
                failures += Assert(sb, "2a. the professional AI reallocates to the threatened region: over ten seeds S acts in Blekinge three times with the script and never without, and its pooled share of local acts rises by at least 10 pp",
                    withThere[0] >= 3 && withoutThere[0] == 0 && pooledWith[0] - pooledWithout[0] >= 0.10,
                    string.Format(CultureInfo.InvariantCulture, "{0} of {1} local acts -> {2} of {3}; {4:P1} -> {5:P1}", withoutThere[0], withoutLocal[0], withThere[0], withLocal[0], pooledWithout[0], pooledWith[0]));
                failures += Assert(sb, "2b. the chaotic AI does not: C acts in Blekinge no more often with the script than without, on a campaign of hundreds of local acts",
                    withThere[2] <= withoutThere[2] && Math.Abs(pooledWith[2] - pooledWithout[2]) < 0.05 && withLocal[2] > 0,
                    string.Format(CultureInfo.InvariantCulture, "{0} of {1} local acts -> {2} of {3}; {4:P1} -> {5:P1}", withoutThere[2], withoutLocal[2], withThere[2], withLocal[2], pooledWithout[2], pooledWith[2]));
                // The answer is counted as ANSWERS MADE, not as policy announcements: which message a
                // party answers with is its personality's (the populist posts, the establishment
                // announces), so counting one kind would measure the professional's taste rather than
                // the reaction. The announcements are reported beside it.
                //
                // And the count is NOT asserted to rise with the script, because it cannot: one answer
                // a week is a ceiling of {campaign days}/7, and the chaotic party's negative campaign
                // already presses S against it in the control. What the script changes is WHOM S is
                // answering, not how often. What the scenario can honestly show is the reaction itself:
                // the party that looks answers at its tempo, and the party that never looks (C,
                // reactivity 0) never answers at all, in the same world under the same attacks.
                double ceiling = Math.Floor(daysRun / (double)CampaignAi.AnswerCooldownDays);
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "  Answers are tempo-bound, not attack-bound: one a week is a ceiling near {0:F0} a campaign, and S is against it with the script and without ({1:F1} -> {2:F1}) - the script changes whom it answers, not how often; M never crosses its own threshold ({3:F1} -> {4:F1}) because the negative campaign is aimed at the polled leader, not at it\n",
                    ceiling, withoutAnswers[0], withAnswers[0], withoutAnswers[1], withAnswers[1]));
                failures += Assert(sb, "2c. the attack is answered by the party that looks and never by the party that does not: S answers at its tempo in both arms; C, reactivity 0, answers not once",
                    withAnswers[0] >= 5.0 && withoutAnswers[0] >= 5.0 && withAnswers[2] == 0.0 && withoutAnswers[2] == 0.0,
                    string.Format(CultureInfo.InvariantCulture, "S {0:F1} -> {1:F1} answers; C {2:F1} -> {3:F1}", withoutAnswers[0], withAnswers[0], withoutAnswers[2], withAnswers[2]));

                CampaignRun.Result one = CampaignAiHarness.RunSeeded(scripted, Seeds[0]);
                CampaignRun.Result none = CampaignAiHarness.RunSeeded(control, Seeds[0]);
                failures += Assert(sb, "2d. the reallocation is real money: S opens an office in Blekinge with the script and not without; C opens none either way",
                    one.Offices[S].HasOffice(blekinge) && !none.Offices[S].HasOffice(blekinge) && !one.Offices[C].HasOffice(blekinge) && one.Parties[S].OfficesOpenedInReaction >= 1 && one.Parties[C].OfficesOpenedInReaction == 0,
                    string.Format(CultureInfo.InvariantCulture, "S: {0} office(s) opened in reaction, {1} defences, {2} answers; M: {3} / {4} / {5}; C: {6} / {7} / {8}",
                        one.Parties[S].OfficesOpenedInReaction, one.Parties[S].Defences, one.Parties[S].Answers,
                        one.Parties[M].OfficesOpenedInReaction, one.Parties[M].Defences, one.Parties[M].Answers,
                        one.Parties[C].OfficesOpenedInReaction, one.Parties[C].Defences, one.Parties[C].Answers));
            }

            sb.Append($"\nREACTIVITY: {(failures == 0 ? "all assertions hold" : failures + " FAILED")}\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>The control setup with L replaced by the scripted player: the same party, a bigger purse, and a script instead of a personality's choices.</summary>
        private static CampaignRun.Setup WithScriptedPlayer(CampaignRun.Setup control, int blekinge)
        {
            var parties = (CampaignRun.PartySetup[])control.Parties.Clone();
            CampaignRun.PartySetup l = parties[L];
            string region = control.Regions[blekinge].Name;
            var townHall = CampaignActions.Spec(CampaignActionKind.TownHall);
            var doors = CampaignActions.Spec(CampaignActionKind.DoorToDoor);
            var announce = CampaignActions.Spec(CampaignActionKind.PolicyAnnouncement);
            AiDecision[] Script(int day)
            {
                if (day < ScriptStart || day >= ScriptEnd) { return new AiDecision[0]; }
                return new[]
                {
                    new AiDecision(CampaignActionKind.TownHall, new CampaignActions.ActionTarget(blekinge, -1, null), region, townHall.MoneyCost, townHall.Hours, 0.0, false),
                    new AiDecision(CampaignActionKind.DoorToDoor, new CampaignActions.ActionTarget(blekinge, -1, null), region, doors.MoneyCost, doors.Hours, 0.0, false),
                    new AiDecision(CampaignActionKind.PolicyAnnouncement, CampaignActions.ActionTarget.National(), "national", announce.MoneyCost, announce.Hours, 0.0, false, againstParty: S),
                };
            }

            parties[L] = new CampaignRun.PartySetup(l.Name, l.Personality, l.Credibility, PlayerWarChest, l.TrueIssueMatch, l.Volunteers, l.Candidate,
                l.Offices, l.OfficeOperationsPerDay, l.Staff, l.TelevisionBuys, Script);
            return new CampaignRun.Setup(control.Calendar, parties, control.PriorShares, control.LoyaltyPerParty, control.Compatibility, control.TrueSalience,
                control.NationalAudience, control.Regions, control.PublicHouse, control.PublicPollEveryDays, control.InternalHouse, control.ElectorateLoyalty,
                control.Outlets, control.DebateDays, control.Scandals);
        }

        private static bool IsLocal(CampaignActionKind kind) => kind == CampaignActionKind.Rally || kind == CampaignActionKind.TownHall || kind == CampaignActionKind.DoorToDoor;

        /// <summary>A party's local acts from the day the script began: how many, and how many of them in the scripted region.</summary>
        private static void LocalCounts(CampaignRun.PartyLedger ledger, string region, out int there, out int local)
        {
            local = 0; there = 0;
            foreach (CampaignRun.DecisionRecord d in ledger.Log)
            {
                if (!IsLocal(d.Kind) || d.Day < ScriptStart) { continue; }
                local++;
                if (d.Target.StartsWith(region)) { there++; }
            }
        }

        /// <summary>Every policy announcement the party made from the script's first day - by the reaction rule AND by its own weighing, which is why this can exceed the answers counter.</summary>
        private static int Announcements(CampaignRun.PartyLedger ledger)
        {
            int n = 0;
            foreach (CampaignRun.DecisionRecord d in ledger.Log) { if (d.Kind == CampaignActionKind.PolicyAnnouncement && d.Day >= ScriptStart) { n++; } }
            return n;
        }

        private static int FirstDayIn(CampaignRun.PartyLedger ledger, string region)
        {
            foreach (CampaignRun.DecisionRecord d in ledger.Log)
            {
                if (IsLocal(d.Kind) && d.Day >= ScriptStart && d.Target.StartsWith(region)) { return d.Day; }
            }

            return -1;
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
