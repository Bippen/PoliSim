using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// A-LOOP q24, piece 5a (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §5 (4)): <b>the persuasion pass criteria an Editor harness can prove</b>, with
    /// reviews 881 and 881b's additions.
    /// <list type="number">
    /// <item><b>A contact's effect, by hand</b>: per class, half the electorate covered at the mean quality is half the class's CACE; twice the
    /// exposure covers three quarters (persuadable once); a contact far above the mean is held within the upper limit; a zero in the chain moves
    /// nothing; an attack carries the sign against; a party standing on 15 % of the electorate covers at most 15 % of it; absorption is
    /// proportional; and the AI values a contact by the run's OWN class-effect difference - a contact that lowers its party's effect is valued
    /// below zero (881b's N3).</item>
    /// <item><b>(b) and (c) on staged campaigns</b> (Sweden's harness staging and Germany's game staging, seed 777): every party's OBSERVED move
    /// from its no-campaign share is the move its own tally implies under the sourced CACEs, recomputed here independently of the run's code
    /// (a compatibility path past the clamp, a lost reach or a changed CACE breaks it); the cap's share; the AI inside the source and blind to
    /// turnout; and the equal-effort drift on the staging's prior, MEASURED and printed per party (a ruling owed to Elias, never asserted).</item>
    /// <item><b>(a) the player's full campaign</b> moves its own share by MORE than GA-5-1's 0.1 point against the same run with the player idle,
    /// and by exactly what its tally implies; the smallest party's same campaign is printed beside it.</item>
    /// <item><b>(d) the run-up's purchases</b>: the television buy charged, its week on the standing at the sourced size within its SE for the
    /// largest and the smallest buyer, the prior exactly before and after, a poll in its week fielded from it, the outcome the no-buy outcome less
    /// the price; the poll information only; the office refused; the outcome carries no share (its fields enumerated).</item>
    /// </list>
    /// The US is held by `UsCampaignGeographyCheck`, which calls <see cref="ObservedMatchesSources"/> and <see cref="AiValuesWithinSource"/> on its own staging.
    /// Prints `CAMPAIGN PERSUASION: PASS` and exits 0 when every check holds; each failure is a `FAIL` line naming its check.
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.CampaignPersuasionDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class CampaignPersuasionDiagnostic
    {
        private const double Exact = 1e-12;
        /// <summary>The tolerance of an independent recomputation against the run, in points of share (floating-point noise only).</summary>
        private const double Recomputed = 1e-8;
        private const int Seed = 777;
        /// <summary>GA-5-1's finding, the defect 5a answers: every mix left the player's share within this many points of an idle player's.</summary>
        private const double IdleWithinPp = 0.1;
        /// <summary>DECLARED: the most of the class effects the sourced upper limit may bind on before the cap is doing the work the sources should.</summary>
        private const double CapBindsAtMost = 0.5;

        private static int _failures;
        private static StringBuilder _sb;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            _failures = 0;
            _sb = new StringBuilder("CAMPAIGN PERSUASION: A-LOOP q24 5a - a campaign moves the vote within sourced bounds, never through the clamp\n");
            _sb.Append(F("    CACE close to election day (points among the contacted, [KB18]): personal {0} (upper {1}), impersonal {2} (upper {3}), earned {4:0.000} (upper {5:0.000}); run-up television {6} per 1 000 GRPs (SE {7}, [GGGS11]), one buy {8:0.00} GRPs, a week of {9} days\n",
                CampaignPersuasion.PersonalCacePp, CampaignPersuasion.PersonalUpperPp, CampaignPersuasion.ImpersonalCacePp, CampaignPersuasion.ImpersonalUpperPp,
                CampaignPersuasion.EarnedCacePp, CampaignPersuasion.EarnedUpperPp, CampaignPersuasion.TelevisionPpPer1000Grp, CampaignPersuasion.TelevisionSePer1000Grp,
                CampaignPersuasion.RunUpTelevisionGrp(), CampaignPersuasion.TelevisionWeekDays));
            try
            {
                using (SimulationManager.EpochScope())
                {
                    CampaignRun.Setup sweden = CampaignAiHarness.BuildSetup(out _);
                    ByHand(sweden);
                    Staged("Sweden (the harness staging, 2022)", sweden);
                    PlayerCampaign();
                    RunUp();
                    WorldClock.ApplyStart(CountryId.Germany);
                    CampaignCalendar cal = CampaignCalendar.FromWorldStart(WorldClock.LatestElectionDay(CountryId.Germany), WorldClock.StartDate(CountryId.Germany));
                    if (LiveCampaignSetup.TryFor(CountryId.Germany, new (int, int, Scandal)[0], cal, out CampaignRun.Setup german, out string note, onVoteModelCompatibility: true))
                    {
                        Staged("Germany (the game's staging on the Länder - the CSU and the SSW stand regionally)", german);
                    }
                    else { Check(false, "no German campaign staged: " + note); }
                }
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "CAMPAIGN PERSUASION: PASS - a contact's effect and its bound by hand, coverage within reach, the AI's value the run's own difference, every staged party's observed move the one its tally implies, the cap's share, the AI inside the source and blind to turnout, the player's campaign past GA-5-1's 0.1 point as its tally implies, the run-up's television at the sourced size for any buyer and gone in a week, the poll information only, the office refused; the equal-effort drift printed (a ruling owed)."
                : F("CAMPAIGN PERSUASION: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        // ============================================================ (1) by hand
        private static void ByHand(CampaignRun.Setup sweden)
        {
            _sb.Append("  (1) a contact's effect, by hand (an electorate of 1 000 000, the norm 0.25):\n");
            const double n = 1_000_000.0, norm = 0.25;
            double half = n * Math.Log(2.0);
            foreach (CampaignActionKind kind in new[] { CampaignActionKind.Rally, CampaignActionKind.TelevisionAd, CampaignActionKind.Interview })
            {
                ContactClass c = CampaignPersuasion.ClassOf(kind);
                var pressure = new CampaignPressure(2);
                pressure.Add(0, Trace(kind, half, norm));
                double pp = 100.0 * pressure.ShareEffects(n, norm)[0];
                Check(Math.Abs(pp - 0.5 * CampaignPersuasion.CaceOf(c)) < 1e-9,
                    F("{0} ({1}): half the electorate covered at the mean quality moves its party {2:0.000000} points - half the CACE {3}", kind, c, pp, CampaignPersuasion.CaceOf(c)));
                var twice = new CampaignPressure(2);
                twice.Add(0, Trace(kind, half, norm));
                twice.Add(0, Trace(kind, half, norm));
                double ppTwice = 100.0 * twice.ShareEffects(n, norm)[0];
                Check(Math.Abs(ppTwice - 0.75 * CampaignPersuasion.CaceOf(c)) < 1e-9,
                    F("{0}: the same contact twice covers three quarters, not all - {1:0.000000} points (persuadable once)", kind, ppTwice));
                var strong = new CampaignPressure(2);
                strong.Add(0, Trace(kind, half, 1.0));   // four times the mean
                double ppStrong = 100.0 * strong.ShareEffects(n, norm)[0];
                Check(ppStrong <= 0.5 * CampaignPersuasion.UpperOf(c) + Exact && Math.Abs(ppStrong - 0.5 * Math.Min(4.0 * CampaignPersuasion.CaceOf(c), CampaignPersuasion.UpperOf(c))) < 1e-9,
                    F("{0}: a contact at four times the mean is held within the sourced upper limit - {1:0.000000} points against the bound {2:0.000000}", kind, ppStrong, 0.5 * CampaignPersuasion.UpperOf(c)));
            }

            var dead = new CampaignPressure(2);
            dead.Add(0, Trace(CampaignActionKind.Rally, half, 0.0));
            Check(dead.ShareEffects(n, norm)[0] == 0.0 && dead.Exposure(0, 0) == 0.0, "a contact with a zero in the chain (no relevance) enters no tally and moves nothing - exactly 0");

            var attack = new CampaignPressure(2);
            attack.AddAgainst(1, Trace(CampaignActionKind.TelevisionAd, half, norm), 1.0);
            double against = 100.0 * attack.ShareEffects(n, norm)[1];
            Check(Math.Abs(against + 0.5 * CampaignPersuasion.ImpersonalCacePp) < 1e-9, F("an attack moves its target by the same size with the sign against - {0:0.000000} points", against));

            // a party on 15 % of the ballots - one national electorate of exposure covers at most its 15 %
            var regional = new CampaignPressure(2, new[] { 0.15, 1.0 });
            regional.Add(0, Trace(CampaignActionKind.TelevisionAd, n, norm));
            regional.Add(1, Trace(CampaignActionKind.TelevisionAd, n, norm));
            double[] both = regional.ShareEffects(n, norm);
            double handRegional = CampaignPersuasion.ImpersonalCacePp * 0.15 * (1.0 - Math.Exp(-1.0 / 0.15));
            Check(Math.Abs(100.0 * both[0] - handRegional) < 1e-9 && Math.Abs(100.0 * both[1] - CampaignPersuasion.ImpersonalCacePp * (1.0 - Math.Exp(-1.0))) < 1e-9,
                F("a party standing on 15 % of the electorate: one electorate of exposure moves it {0:0.0000} points (by hand 0.28 x 0.15 x (1 - e^(-1/0.15)) = {1:0.0000}); a national party the same exposure {2:0.0000}", 100.0 * both[0], handRegional, 100.0 * both[1]));

            double[] even = CampaignPersuasion.Apply(new[] { 0.5, 0.5 }, new[] { 0.004, 0.0 });
            Check(Math.Abs(even[0] - 0.504) < Exact && Math.Abs(even.Sum() - 1.0) < Exact, F("a 50/50 race takes the sourced effect whole: 0.5 + 0.004 -> {0:0.000000}", even[0]));
            double[] three = { 0.40, 0.35, 0.25 };
            double[] moved = CampaignPersuasion.Apply(three, new[] { 0.004, 0.0, 0.0 });
            Check(Math.Abs(moved.Sum() - 1.0) < Exact && Math.Abs(moved[0] - 0.404) < Exact && Math.Abs(moved[1] / moved[2] - 0.35 / 0.25) < 1e-12,
                F("the other parties absorb an effect in proportion to their shares: 0.40 + 0.004 -> {0:0.000000}; the others {1:0.000000} and {2:0.000000}, their ratio kept", moved[0], moved[1], moved[2]));
            Check(ReferenceEquals(CampaignPersuasion.Apply(three, new double[3]), three), "no effect returns the shares as they came (the no-campaign path untouched)");

            AiDifference(sweden);
        }

        /// <summary>
        /// Review 881b's N3: the AI values a contact by the run's own class-effect difference. A party whose impersonal class holds a high-quality
        /// tally near saturation is offered one more post at a quarter of that quality: the AI's value equals what the run's pressure moves (the
        /// tally with the contact less without it), and both are below zero - the AI never values positively a contact that lowers its party's effect.
        /// </summary>
        private static void AiDifference(CampaignRun.Setup setup)
        {
            double n = setup.NationalAudience, norm = CampaignPersuasion.ElectorateMean(setup);
            var before = new CampaignPressure(setup.Parties.Length);
            before.Add(0, Trace(CampaignActionKind.TelevisionAd, 1.7 * n, 2.0 * norm));   // a saturated class at twice the mean quality
            CampaignActions.ChainTrace weak = Trace(CampaignActionKind.SocialPost, 0.05 * n, 0.5 * norm);
            var after = new CampaignPressure(setup.Parties.Length);
            after.Add(0, Trace(CampaignActionKind.TelevisionAd, 1.7 * n, 2.0 * norm));
            after.Add(0, weak);
            double runDelta = 100.0 * (after.ShareEffects(n, norm)[0] - before.ShareEffects(n, norm)[0]);
            var view = new AiView(0, CampaignPhase.Campaign, 30, new ResourcePool(LiveCampaignSetup.WarChest, CampaignEconomy.HoursPerCampaignDay, 0),
                spendingReserve: LiveCampaignSetup.WarChest, hasPoll: false, latestPoll: default, momentumPp: new double[setup.Parties.Length], issues: new IssueMeasurement[IssueVector.IssueCount],
                ownCredibility: LiveCampaignSetup.FlatCredibility, nationalAudience: n, regions: setup.Regions, daysSinceOwnPoll: -1, nationalReachShare: 1.0, persuasionNorm: norm,
                ownExposureByClass: before.OwnExposureByClass(0), ownQualityByClass: before.OwnQualityByClass(0));
            double aiValue = CampaignAi.ContactValuePp(view, weak, PersonalityCatalog.Profile(AiPersonality.Professional));
            Check(Math.Abs(aiValue - runDelta) < 1e-12 && runDelta < 0.0 && aiValue < 0.0,
                F("(N3) a post at a quarter of a saturated class's quality LOWERS its party's effect by {0:+0.000000;-0.000000} points in the run, and the AI values it at {1:+0.000000;-0.000000} - the run's own difference, below zero", runDelta, aiValue));
        }

        /// <summary>A contact of <paramref name="exposure"/> people at relevance × credibility <paramref name="quality"/> (the chain's persuasion is exposure × quality × the action's weight).</summary>
        private static CampaignActions.ChainTrace Trace(CampaignActionKind kind, double exposure, double quality)
        {
            double w = CampaignActions.Spec(kind).PersuasionWeight;
            return new CampaignActions.ChainTrace(kind, exposure, 1.0, exposure, quality, 1.0, exposure * quality * w, 0.0, 0.0);
        }

        // ============================================================ (2) staged campaigns: (b), (c), the cap, the AI, the drift
        private static void Staged(string label, CampaignRun.Setup setup)
        {
            _sb.Append(F("  (2) {0}, seed {1}:\n", label, Seed));
            SimulationRandom.Seed(Seed);
            CampaignRun.State s = CampaignRun.Begin(setup, SimulationRandom.For(SimulationRandom.Stream.CampaignAi), SimulationRandom.For(SimulationRandom.Stream.Debate), SimulationRandom.For(SimulationRandom.Stream.Scandal));
            while (!s.Finished) { CampaignRun.StepDay(s); }
            CampaignRun.Result r = CampaignRun.Finish(s);

            double[] predicted = PredictedMovesPp(setup, r);
            var row = new StringBuilder("      party   reach   effect pp   bound pp   observed move pp   implied move pp\n");
            for (int p = 0; p < setup.Parties.Length; p++)
            {
                row.Append(F("      {0,-6} {1,6:0.000} {2,10:+0.0000;-0.0000} {3,10:0.0000} {4,14:+0.0000;-0.0000} {5,14:+0.0000;-0.0000}\n", setup.Parties[p].Name, setup.StandingShare(p),
                    r.CampaignEffectPp[p], r.CampaignBoundPp[p], 100.0 * (r.FinalShares[p] - r.BaselineShares[p]), predicted[p]));
            }
            _sb.Append(row);
            Check(ObservedMatchesSources(setup, r, out string worst), "(b)/(c) every party's OBSERVED move from its no-campaign share is the move its own tally implies under the sourced CACEs, recomputed independently (each class's CACE x q held at its upper limit, coverage over the party's reach, the attacks against it, the others' absorbed): no other channel - no compatibility past the clamp - moves a share - " + worst);
            Check(Enumerable.Range(0, setup.Parties.Length).All(p => Math.Abs(r.CampaignEffectPp[p]) <= r.CampaignBoundPp[p] + Exact),
                "(b) every party's effect lies inside Σ_class upper x coverage");

            double norm = CampaignPersuasion.ElectorateMean(setup);
            double[] rebuilt = CampaignRun.CloseStanding(s)(CampaignPersuasion.Apply(PreferenceModel.Preference(setup.Compatibility, s.Prior, setup.LoyaltyPerParty), r.Pressure.ShareEffects(setup.NationalAudience, norm)));
            double worstRebuild = Enumerable.Range(0, rebuilt.Length).Max(p => Math.Abs(rebuilt[p] - r.FinalShares[p]));
            Check(worstRebuild <= Exact, F("(c, the plumbing) the final shares are the STAGING's compatibility through the preference model, the share effects and the count's standing (largest difference {0:E2}); the staging's own compatibility above {1:0}: {2} of {3}",
                worstRebuild, ElectionScales.Max, setup.Compatibility.Count(c => c > ElectionScales.Max), setup.Compatibility.Length));
            Check(Math.Abs(r.FinalShares.Sum() - 1.0) < 1e-9 && r.FinalShares.All(v => v >= 0.0 && !double.IsNaN(v) && !double.IsInfinity(v)),
                F("the final shares sum to one ({0:R}), non-negative and finite; the norm {1:0.0000}", r.FinalShares.Sum(), norm));

            r.Pressure.CapBinding(norm, out int binding, out int total);
            Check(total > 0 && binding <= CapBindsAtMost * total, F("the sourced upper limit binds on {0} of {1} class effects (DECLARED: at most half) - the cap is a bound, not the result", binding, total));

            Check(AiValuesWithinSource(setup, out string ai), "(H1) the AI's value of a contact is the run's own sourced arithmetic and blind to turnout - " + ai);

            // review 881b: the equal-effort drift, MEASURED and printed - a ruling owed to Elias (CampaignPersuasion.Apply's note), never asserted
            double sum = setup.PriorShares.Sum();
            double[] prior = setup.PriorShares.Select(v => v / sum).ToArray();
            double[] drifted = CampaignPersuasion.Apply(prior, Enumerable.Repeat(0.004, prior.Length).ToArray());
            _sb.Append("    DECLARED  the equal-effort drift (every party +0.40 points of sourced effect on this prior; a ruling owed to Elias): ")
               .Append(string.Join(" ", Enumerable.Range(0, prior.Length).Select(p => F("{0} {1:+0.000;-0.000}", setup.Parties[p].Name, 100.0 * (drifted[p] - prior[p])))))
               .Append('\n');
        }

        /// <summary>
        /// Review 881b's M-r1, an independent recomputation: each party's move in points as its OWN tally implies it under the sourced CACEs - each
        /// class's exposure and quality read off the tally slot by slot (`TheEight`'s order: three personal, three impersonal, two earned, then the
        /// coverage), CACE × q held at the upper limit, coverage r·(1 − e^(−E/(r·N))) over the party's reach, the attacks it received subtracted,
        /// and every other party's effect absorbed in proportion to its no-campaign share - written here, never through `CampaignPersuasion`'s
        /// effect or absorption code.
        /// </summary>
        internal static double[] PredictedMovesPp(CampaignRun.Setup setup, CampaignRun.Result r)
        {
            int n = setup.Parties.Length;
            double electorate = setup.NationalAudience, norm = CampaignPersuasion.ElectorateMean(setup);
            double[] cace = { 0.58, 0.28, 100.0 / 800.0 }, upper = { 1.66, 0.83, 100.0 / 175.0 };   // [KB18] pp. 153, 159-160, re-read here so the recomputation shares no constant with the run
            int[] classOfSlot = { 0, 0, 0, 1, 1, 1, 2, 2, 2 };
            var effect = new double[n];
            for (int p = 0; p < n; p++)
            {
                double reach = setup.StandingShare(p);
                var e = new double[3]; var q = new double[3];
                for (int slot = 0; slot < classOfSlot.Length; slot++) { e[classOfSlot[slot]] += r.Pressure.Exposure(p, slot); q[classOfSlot[slot]] += r.Pressure.QualityOf(p, slot); }
                double pp = 0.0;
                for (int c = 0; c < 3; c++)
                {
                    pp += ClassPp(cace[c], upper[c], e[c], q[c], electorate, norm, reach);
                    r.Pressure.Against(p, (ContactClass)c, out double ae, out double aq);
                    pp -= ClassPp(cace[c], upper[c], ae, aq, electorate, norm, reach);
                }
                effect[p] = pp / 100.0;
            }
            var moves = new double[n];
            for (int p = 0; p < n; p++)
            {
                double d = effect[p];
                for (int q = 0; q < n; q++) { if (q != p && r.BaselineShares[q] < 1.0) { d -= effect[q] * r.BaselineShares[p] / (1.0 - r.BaselineShares[q]); } }
                moves[p] = 100.0 * d;
            }
            return moves;
        }

        private static double ClassPp(double cace, double upper, double e, double q, double electorate, double norm, double reach)
        {
            if (e <= 0.0 || q <= 0.0 || norm <= 0.0 || reach <= 0.0 || electorate <= 0.0) { return 0.0; }
            double perContact = Math.Min(cace * (q / e) / norm, upper);
            return perContact * reach * (1.0 - Math.Exp(-e / (reach * electorate)));
        }

        /// <summary>Review 881b's M-r1: every party's observed move against <see cref="PredictedMovesPp"/>, to floating-point noise.</summary>
        internal static bool ObservedMatchesSources(CampaignRun.Setup setup, CampaignRun.Result r, out string detail)
        {
            double[] predicted = PredictedMovesPp(setup, r);
            int worst = 0; double worstGap = -1.0;
            for (int p = 0; p < predicted.Length; p++)
            {
                double gap = Math.Abs(100.0 * (r.FinalShares[p] - r.BaselineShares[p]) - predicted[p]);
                if (gap > worstGap) { worstGap = gap; worst = p; }
            }
            detail = F("the largest gap {0:E2} points ({1}: observed {2:+0.0000;-0.0000}, implied {3:+0.0000;-0.0000})", worstGap, r.Parties[worst].Name,
                100.0 * (r.FinalShares[worst] - r.BaselineShares[worst]), predicted[worst]);
            return worstGap <= Recomputed;
        }

        /// <summary>
        /// Review 881's H1 and M5: on a staging's own electorate and norm, the AI's value of a television ad and an interview (`CampaignAi.ContactValuePp`,
        /// the scorer's own reading) is positive, no more than the class's sourced limit over the party's reach, and the SAME with or without an
        /// enthusiasm of a whole electorate behind it - enthusiasm buys no turnout (5b values doors alone, `CampaignAi.DoorTurnoutValuePp`).
        /// </summary>
        internal static bool AiValuesWithinSource(CampaignRun.Setup setup, out string detail)
        {
            double norm = CampaignPersuasion.ElectorateMean(setup);
            double n = setup.NationalAudience;
            PersonalityProfile profile = PersonalityCatalog.Profile(AiPersonality.Professional);
            var view = new AiView(0, CampaignPhase.Campaign, 30, new ResourcePool(LiveCampaignSetup.WarChest, CampaignEconomy.HoursPerCampaignDay, 0),
                spendingReserve: LiveCampaignSetup.WarChest, hasPoll: false, latestPoll: default, momentumPp: new double[setup.Parties.Length], issues: new IssueMeasurement[IssueVector.IssueCount],
                ownCredibility: LiveCampaignSetup.FlatCredibility, nationalAudience: n, regions: setup.Regions, daysSinceOwnPoll: -1, nationalReachShare: setup.StandingShare(0), persuasionNorm: norm);
            var parts = new List<string>();
            bool ok = n > 0.0 && norm > 0.0;
            foreach ((CampaignActionKind kind, double audience) in new[]
            {
                (CampaignActionKind.TelevisionAd, n * MediaOutlet.TelevisionReach(setup.Outlets)),
                (CampaignActionKind.Interview, n * setup.Outlets.Max(o => o.Reach)),
            })
            {
                CampaignActions.ActionSpec spec = CampaignActions.Spec(kind);
                CampaignActions.ChainTrace plain = CampaignActions.Resolve(spec, audience, 0.5, 0.5, LiveCampaignSetup.FlatCredibility, spec.MoneyCost);
                var enthused = new CampaignActions.ChainTrace(plain.Kind, plain.Reach, plain.Salience, plain.Exposure, plain.Relevance, plain.Credibility, plain.Persuasion, n, plain.SalienceShift);
                double value = CampaignAi.ContactValuePp(view, plain, profile);
                double withTurnout = CampaignAi.ContactValuePp(view, enthused, profile);
                double limit = CampaignPersuasion.UpperOf(CampaignPersuasion.ClassOf(kind)) * setup.StandingShare(0);
                ok &= value > 0.0 && value <= limit + Exact && withTurnout == value;
                parts.Add(F("{0} {1:0.00000} pp (limit {2:0.000}; with a whole electorate of enthusiasm {3:0.00000})", kind, value, limit, withTurnout));
            }
            detail = F("electorate {0:N0}: ", n) + string.Join("; ", parts);
            return ok;
        }

        // ============================================================ (3) (a) the player's full campaign
        private static void PlayerCampaign()
        {
            _sb.Append("  (3) the player's full campaign (Sweden, the 2022 staging, scripted), seed 777:\n");
            var none = new (int, int, Scandal)[0];
            CampaignRun.Setup probe = LiveCampaignSetup.Sweden(none, out _, vintage: ElectionVintage.Sweden2022);
            int smallest = Enumerable.Range(0, probe.Parties.Length).OrderBy(p => probe.PriorShares[p]).First();
            foreach (int party in new[] { 0, smallest })
            {
                CampaignActions.ActionSpec post = CampaignActions.Spec(CampaignActionKind.SocialPost), digital = CampaignActions.Spec(CampaignActionKind.DigitalAd),
                    interview = CampaignActions.Spec(CampaignActionKind.Interview);
                const double digitalSpend = 30_000.0;   // DECLARED, the scripted campaign: a 30 000 kr digital ad, a post and an interview request every day - the chest's 56 days
                Func<int, AiDecision[]> full = day => new[]
                {
                    new AiDecision(CampaignActionKind.Interview, CampaignActions.ActionTarget.National(), "national", 0.0, interview.Hours, 0.0, false),
                    new AiDecision(CampaignActionKind.DigitalAd, CampaignActions.ActionTarget.National(), "national", digitalSpend, digital.Hours, 0.0, false),
                    new AiDecision(CampaignActionKind.SocialPost, CampaignActions.ActionTarget.National(), "national", post.MoneyCost, post.Hours, 0.0, false),
                };
                CampaignRun.Setup idleSetup = LiveCampaignSetup.Sweden(none, out _, vintage: ElectionVintage.Sweden2022, playerParty: party, playerScript: d => new AiDecision[0]);
                CampaignRun.Setup fullSetup = LiveCampaignSetup.Sweden(none, out _, vintage: ElectionVintage.Sweden2022, playerParty: party, playerScript: full);
                CampaignRun.Result idle = CampaignAiHarness.RunSeeded(idleSetup, Seed);
                CampaignRun.Result run = CampaignAiHarness.RunSeeded(fullSetup, Seed);
                double moved = 100.0 * (run.FinalShares[party] - idle.FinalShares[party]);
                double implied = PredictedMovesPp(fullSetup, run)[party] - PredictedMovesPp(idleSetup, idle)[party]
                                 + 100.0 * (run.BaselineShares[party] - idle.BaselineShares[party]);
                string line = F("{0} (prior {1:P1}, {2} acts) moves its own share {3:+0.0000;-0.0000} points against its idle run; its tallies imply {4:+0.0000;-0.0000}",
                    run.Parties[party].Name, probe.PriorShares[party] / probe.PriorShares.Sum(), run.Parties[party].TotalActions, moved, implied);
                if (party == 0)
                {
                    Check(run.Parties[0].TotalActions > 0 && moved > IdleWithinPp && Math.Abs(moved - implied) <= Recomputed,
                        "(a) the player's campaign moves its share by MORE than GA-5-1's " + IdleWithinPp.ToString(CultureInfo.InvariantCulture) + " point, by exactly what its tally implies under the sourced CACEs - " + line);
                }
                else
                {
                    Check(Math.Abs(moved - implied) <= Recomputed, "(a) the smallest party's same campaign moves its share by exactly what its tally implies (printed against GA-5-1's 0.1, not asserted) - " + line);
                }
            }
        }

        // ============================================================ (4) (d) the run-up's purchases
        private static void RunUp()
        {
            _sb.Append("  (4) the run-up's purchases (Sweden, the 2022 staging):\n");
            CampaignRun.Setup staging = CampaignAiHarness.BuildSetup(out _);
            const int buyDay = 10, pollDay = 12;
            double price = CampaignActions.Spec(CampaignActionKind.TelevisionAd).MoneyCost;
            double sourcedPp = CampaignPersuasion.TelevisionPpPer1000Grp * CampaignPersuasion.RunUpTelevisionGrp() / 1000.0;
            double sePp = CampaignPersuasion.TelevisionSePer1000Grp * CampaignPersuasion.RunUpTelevisionGrp() / 1000.0;
            double[] prior = staging.PriorShares;
            int smallest = Enumerable.Range(0, prior.Length).OrderBy(p => prior[p]).First();

            foreach (int buyer in new[] { 0, smallest })
            {
                PreCampaignRun.State plain = PreCampaignRun.Begin(staging, buyer, new System.Random(11));
                PreCampaignRun.State tv = PreCampaignRun.Begin(staging, buyer, new System.Random(11));
                var hire = new List<PreCampaignRun.Decision>();
                if (!tv.HasRole(StaffRole.CampaignManager)) { hire.Add(new PreCampaignRun.Decision(CampaignActionKind.RecruitStaff, role: (int)StaffRole.CampaignManager)); }
                PreCampaignRun.StepDay(plain, hire); PreCampaignRun.StepDay(tv, hire);   // day 0: the same payroll in both, so the chests differ by the buy alone
                for (int d = 1; d < buyDay; d++) { PreCampaignRun.StepDay(plain, null); PreCampaignRun.StepDay(tv, null); }
                PreCampaignRun.StepDay(plain, null);
                PreCampaignRun.StepDay(tv, new[] { new PreCampaignRun.Decision(CampaignActionKind.PrepareAdvertising) });
                string name = staging.Parties[buyer].Name;
                Check(tv.Log.Last().Refusal == null && Math.Abs((plain.Money - tv.Money) - price) < 1e-6,
                    F("{0}: the television buy is CHARGED its price the day it airs: the chest {1:N0} against the no-buy run's {2:N0} ({3:N0} kr less)", name, tv.Money, plain.Money, plain.Money - tv.Money));

                double share = prior[buyer] / prior.Sum();
                double worstGap = 0.0, gainSeen = 0.0;
                for (int d = buyDay; d < buyDay + CampaignPersuasion.TelevisionWeekDays; d++)
                {
                    gainSeen = 100.0 * (PreCampaignRun.Standing(tv, d)[buyer] - share);
                    worstGap = Math.Max(worstGap, Math.Abs(gainSeen - sourcedPp));
                }
                double moe = PollingSystem.MarginOfErrorPp(share, staging.InternalHouse.SampleSize);
                Check(worstGap <= sePp, F("(d) {0} (prior {1:P1}): on its week's polls the buy is the sourced size within its SE - the buyer's standing {2:+0.0000} points on each of the {3} days ({4:0.00} GRPs x 4.73 per 1 000 = {5:0.0000}, SE {6:0.0000}; the largest gap {7:E2}) - DECLARED for Elias: inside the internal poll's own +/-{8:0.00} points",
                    name, share, gainSeen, CampaignPersuasion.TelevisionWeekDays, CampaignPersuasion.RunUpTelevisionGrp(), sourcedPp, sePp, worstGap, moe));
                Check(ReferenceEquals(PreCampaignRun.Standing(tv, buyDay + CampaignPersuasion.TelevisionWeekDays), staging.PriorShares) && ReferenceEquals(PreCampaignRun.Standing(tv, buyDay - 1), staging.PriorShares),
                    F("(d) {0}: the day before it airs and from the eighth day on, the standing is the prior exactly - the effect is gone, as Gerber et al. measured a week later", name));

                while (!plain.Finished) { PreCampaignRun.StepDay(plain, null); }
                while (!tv.Finished) { PreCampaignRun.StepDay(tv, null); }
                PreCampaignRun.Outcome a = PreCampaignRun.Finish(plain), b = PreCampaignRun.Finish(tv);
                Check(Math.Abs((a.Money - b.Money) - price) < 1e-6 && a.Volunteers == b.Volunteers && a.Offices.SequenceEqual(b.Offices) && a.Staff.SequenceEqual(b.Staff) && a.TelevisionBuys == b.TelevisionBuys && b.TelevisionBuys == staging.Parties[buyer].TelevisionBuys,
                    F("(d) {0}: the count sees none of it - the outcome the campaign takes is the no-buy outcome less the price (chest {1:N0} against {2:N0}; offices, staff, volunteers and the plan's {3} buys identical)", name, b.Money, a.Money, b.TelevisionBuys));
            }

            const int party = 0;
            var hire0 = new List<PreCampaignRun.Decision>();
            PreCampaignRun.State tvPolled = PreCampaignRun.Begin(staging, party, new System.Random(23));
            if (!tvPolled.HasRole(StaffRole.CampaignManager)) { hire0.Add(new PreCampaignRun.Decision(CampaignActionKind.RecruitStaff, role: (int)StaffRole.CampaignManager)); }
            PreCampaignRun.StepDay(tvPolled, hire0);
            for (int d = 1; d < buyDay; d++) { PreCampaignRun.StepDay(tvPolled, null); }
            PreCampaignRun.StepDay(tvPolled, new[] { new PreCampaignRun.Decision(CampaignActionKind.PrepareAdvertising) });
            PreCampaignRun.StepDay(tvPolled, null);
            PreCampaignRun.StepDay(tvPolled, new[] { new PreCampaignRun.Decision(CampaignActionKind.CommissionPolling) });
            // the poll stream is drawn by polls alone, so the run-up's first poll replays from the seed
            Poll expected = PollingSystem.Conduct(PreCampaignRun.Standing(tvPolled, pollDay), staging.InternalHouse, staging.Calendar.PreCampaignStart.AddDays(pollDay), new System.Random(23));
            Check(tvPolled.LatestPoll.HasValue && Enumerable.Range(0, prior.Length).All(p => tvPolled.LatestPoll.Value.Share(p) == expected.Share(p)),
                F("a poll bought in the buy's week is fielded from that standing (the buyer at {0:0.000} % on the poll)", tvPolled.LatestPoll.HasValue ? 100.0 * tvPolled.LatestPoll.Value.Share(party) : double.NaN));
            string[] fields = typeof(PreCampaignRun.Outcome).GetFields(BindingFlags.Public | BindingFlags.Instance).Select(f => f.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Check(fields.SequenceEqual(new[] { "Digest", "Money", "Offices", "Staff", "TelevisionBuys", "Volunteers" }),
                "(d) the outcome carries no share: its fields are " + string.Join(", ", fields));

            PreCampaignRun.State noPoll = PreCampaignRun.Begin(staging, party, new System.Random(31));
            PreCampaignRun.State onePoll = PreCampaignRun.Begin(staging, party, new System.Random(31));
            PreCampaignRun.StepDay(noPoll, null);
            PreCampaignRun.StepDay(onePoll, new[] { new PreCampaignRun.Decision(CampaignActionKind.CommissionPolling) });
            while (!noPoll.Finished) { PreCampaignRun.StepDay(noPoll, null); }
            while (!onePoll.Finished) { PreCampaignRun.StepDay(onePoll, null); }
            PreCampaignRun.Outcome x = PreCampaignRun.Finish(noPoll), y = PreCampaignRun.Finish(onePoll);
            Check(onePoll.LatestPoll.HasValue && Math.Abs((x.Money - y.Money) - staging.InternalHouse.Cost) < 1e-6 && x.Volunteers == y.Volunteers && x.Offices.SequenceEqual(y.Offices) && x.Staff.SequenceEqual(y.Staff)
                  && x.TelevisionBuys == y.TelevisionBuys && ReferenceEquals(PreCampaignRun.Standing(onePoll, 1), staging.PriorShares),
                F("the poll is information only: the outcome is the no-poll outcome less the house's {0:N0}, the standing untouched, the +/- its sample buys ({1} interviews)", staging.InternalHouse.Cost, staging.InternalHouse.SampleSize));

            PreCampaignRun.State office = PreCampaignRun.Begin(staging, party, new System.Random(41));
            int region = PreCampaignRun.NextOfficeRegion(office);
            PreCampaignRun.StepDay(office, new[] { new PreCampaignRun.Decision(CampaignActionKind.EstablishOffice, region) });
            // q24 5b: the office is back in the run-up now that the count reads its doors (GroundGameCountDiagnostic holds what they buy)
            Check(office.Log.Count == 1 && office.Log[0].Refusal == null && office.PlannedOffices.Count == 1 && office.PlannedOffices[0] == region && Math.Abs(office.Money - staging.Parties[party].StartingMoney) < 1e-9,
                F("the office is back in the run-up since q24 5b counts its doors: planned in {0}, the chest untouched until the campaign's day 0 pays it{1}", office.Log.Count == 1 ? office.Log[0].Target : "-",
                    office.Log.Count == 1 && office.Log[0].Refusal != null ? " - REFUSED \"" + office.Log[0].Refusal + "\"" : string.Empty));
        }

        private static void Check(bool ok, string what)
        {
            if (!ok) { _failures++; }
            _sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n');
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
