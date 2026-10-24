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
    /// turnout; and, ruled by K65 and K81, equal effort moving nothing, a lone campaigner moving exactly its sourced effect and every live move
    /// within its sourced bound - asserted.</item>
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
                ? "CAMPAIGN PERSUASION: PASS - a contact's effect and its bound by hand, coverage within reach, the AI's value the run's own difference, every staged party's observed move the one its tally implies, the cap's share, the AI inside the source and blind to turnout, the player's campaign past GA-5-1's 0.1 point as its tally implies, the run-up's television at the sourced size for any buyer and gone in a week, the poll information only, the office back in the run-up; equal effort moving nothing and a lone campaigner moving exactly its sourced effect (K65, K81); the attack, the poll, the topic, the size and the siting the player's own."
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
            attack.AddAttack(0, 1, Trace(CampaignActionKind.TelevisionAd, half, norm), 1.0);
            double against = 100.0 * attack.ShareEffects(n, norm)[1];
            Check(Math.Abs(against + 0.5 * CampaignPersuasion.ImpersonalCacePp) < 1e-9 && attack.ShareEffects(n, norm)[0] == 0.0,
                F("an attack moves its target by the same size with the sign against - {0:0.000000} points; a contact wholly about the opponent moves its attacker by nothing", against));

            // K66: an attack shares coverage with the attacker's own contacts - one contact split 40/60 covers what the unsplit contact covers,
            // its own part and its attack part summing to the unsplit contact's effect: no free extra reach
            var whole = new CampaignPressure(2);
            whole.Add(0, Trace(CampaignActionKind.TelevisionAd, half, norm));
            var split = new CampaignPressure(2);
            split.AddAttack(0, 1, Trace(CampaignActionKind.TelevisionAd, half, norm), 0.6);
            double wholePp = 100.0 * whole.ShareEffects(n, norm)[0];
            double[] splitPp = split.ShareEffects(n, norm).Select(v => 100.0 * v).ToArray();
            Check(Math.Abs(splitPp[0] - 0.4 * wholePp) < 1e-9 && Math.Abs(-splitPp[1] - 0.6 * wholePp) < 1e-9 && Math.Abs(splitPp[0] - splitPp[1] - wholePp) < 1e-9,
                F("(K66) one contact, 60 % of it an attack: its own part {0:+0.000000;-0.000000} and the attack's {1:+0.000000;-0.000000} points sum to the unsplit contact's {2:0.000000} - the attack shares the attacker's coverage, no extra reach", splitPp[0], splitPp[1], wholePp));
            var ownThenAttack = new CampaignPressure(2);
            ownThenAttack.Add(0, Trace(CampaignActionKind.TelevisionAd, half, norm));
            ownThenAttack.AddAttack(0, 1, Trace(CampaignActionKind.TelevisionAd, half, norm), 1.0);
            double[] ota = ownThenAttack.ShareEffects(n, norm).Select(v => 100.0 * v).ToArray();
            Check(Math.Abs(ota[0] - 0.375 * CampaignPersuasion.ImpersonalCacePp) < 1e-9 && Math.Abs(ota[1] + 0.375 * CampaignPersuasion.ImpersonalCacePp) < 1e-9,
                F("(K66) half an electorate of own contact and half of attack cover three quarters between them, shared half and half: own {0:+0.000000;-0.000000}, the target {1:+0.000000;-0.000000} points (by hand 0.375 x 0.28 each)", ota[0], ota[1]));

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

            // K66: a negative campaign's contact, split as the run splits it (the opponent's share into the attack, one coverage between them),
            // is valued by the AI at the run's own difference in its OWN effect - with attacks already on its books
            // K86: the strategy keeps its choice (the share spent on the polled leader) and no multiplier - the run resolves the contact unscaled
            StrategyModifiers negative = CampaignStrategyModel.ChoiceOnly(CampaignStrategyModel.Modifiers(CampaignStrategy.NegativeCampaign, setup.ElectorateLoyalty, false));
            CampaignActions.ChainTrace plain = Trace(CampaignActionKind.TelevisionAd, 0.3 * n, norm);
            CampaignActions.ChainTrace resolved = plain;
            var books = new CampaignPressure(setup.Parties.Length);
            books.Add(0, Trace(CampaignActionKind.TelevisionAd, 0.4 * n, norm));
            books.AddAttack(0, 1, Trace(CampaignActionKind.TelevisionAd, 0.4 * n, norm), negative.OpponentShare);
            var booksAfter = new CampaignPressure(setup.Parties.Length);
            booksAfter.Add(0, Trace(CampaignActionKind.TelevisionAd, 0.4 * n, norm));
            booksAfter.AddAttack(0, 1, Trace(CampaignActionKind.TelevisionAd, 0.4 * n, norm), negative.OpponentShare);
            booksAfter.AddAttack(0, 1, resolved, negative.OpponentShare);
            double ownBefore = OwnPp(books, 0, n, norm), ownAfter = OwnPp(booksAfter, 0, n, norm);
            var shares = Enumerable.Repeat(1.0 / setup.Parties.Length, setup.Parties.Length).ToArray();
            shares[0] += 0.01; shares[1] += 0.05; double total = shares.Sum(); shares = shares.Select(v => v / total).ToArray();   // party 1 leads the poll the AI sees
            var poll = new Poll(setup.Calendar.CampaignStart, 1000, "the AI's poll", shares, new double[shares.Length]);
            var negView = new AiView(0, CampaignPhase.Campaign, 30, new ResourcePool(LiveCampaignSetup.WarChest, CampaignEconomy.HoursPerCampaignDay, 0),
                spendingReserve: LiveCampaignSetup.WarChest, hasPoll: true, latestPoll: poll, momentumPp: new double[setup.Parties.Length], issues: new IssueMeasurement[IssueVector.IssueCount],
                ownCredibility: LiveCampaignSetup.FlatCredibility, nationalAudience: n, regions: setup.Regions, daysSinceOwnPoll: -1, nationalReachShare: 1.0, persuasionNorm: norm,
                ownExposureByClass: books.OwnExposureByClass(0), ownQualityByClass: books.OwnQualityByClass(0), ownAttackExposureByClass: books.OwnAttackExposureByClass(0));
            double negValue = CampaignAi.ContactValuePp(negView, plain, PersonalityCatalog.Profile(AiPersonality.Chaotic), negative);
            Check(negView.PolledLeaderOtherThanSelf == 1 && Math.Abs(negValue - (ownAfter - ownBefore)) < 1e-12 && negValue > 0.0,
                F("(K66) a negative campaign's television contact (60 % of it about the polled leader) raises its party's OWN effect by {0:+0.000000} points in the run, on the coverage it shares with its attacks, and the AI values it at {1:+0.000000}", ownAfter - ownBefore, negValue));
        }

        /// <summary>K66: a party's own share effect in points on a pressure where no other party campaigns or attacks it - its own part of the coverage it shares with its attacks.</summary>
        private static double OwnPp(CampaignPressure pressure, int party, double electorate, double norm) => 100.0 * pressure.ShareEffects(electorate, norm)[party];

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
            double[] rebuiltEffects = r.Pressure.ShareEffects(setup.NationalAudience, norm, null, null, out double[] rebuiltLoss, out double[] rebuiltGain);
            double[] rebuilt = CampaignRun.CloseStanding(s)(CampaignPersuasion.ApplyCampaign(PreferenceModel.Preference(setup.Compatibility, s.Prior, setup.LoyaltyPerParty), rebuiltEffects,
                setup.Stands == null ? null : Enumerable.Range(0, setup.Parties.Length).Select(p => setup.StandingShare(p)).ToArray(), setup.OverlapMatrix(), rebuiltLoss, rebuiltGain));
            double worstRebuild = Enumerable.Range(0, rebuilt.Length).Max(p => Math.Abs(rebuilt[p] - r.FinalShares[p]));
            Check(worstRebuild <= Exact, F("(c, the plumbing) the final shares are the STAGING's compatibility through the preference model, the share effects and the count's standing (largest difference {0:E2}); the staging's own compatibility above {1:0}: {2} of {3}",
                worstRebuild, ElectionScales.Max, setup.Compatibility.Count(c => c > ElectionScales.Max), setup.Compatibility.Length));
            Check(Math.Abs(r.FinalShares.Sum() - 1.0) < 1e-9 && r.FinalShares.All(v => v >= 0.0 && !double.IsNaN(v) && !double.IsInfinity(v)),
                F("the final shares sum to one ({0:R}), non-negative and finite; the norm {1:0.0000}", r.FinalShares.Sum(), norm));

            r.Pressure.CapBinding(norm, out int binding, out int total);
            Check(total > 0 && binding <= CapBindsAtMost * total, F("the sourced upper limit binds on {0} of {1} class effects (DECLARED: at most half) - the cap is a bound, not the result", binding, total));

            Check(AiValuesWithinSource(setup, out string ai), "(H1) the AI's value of a contact is the run's own sourced arithmetic and blind to turnout - " + ai);

            // K65, K81, K91, K92 (2026-10-23/24): equal effort per voter reached moves nothing (every party, a party with no share too - review 2's
            // L3); a party campaigning ALONE moves by exactly its sourced effect; its gain is taken from the others by their shares (K91); an
            // attack moves only its two parties (K92) - asserted on this prior; the absorption alone (881's measurement) printed beside it
            double sum = setup.PriorShares.Sum();
            double[] prior = setup.PriorShares.Select(v => v / sum).ToArray();
            double[] reach = setup.Stands == null ? null : Enumerable.Range(0, prior.Length).Select(p => setup.StandingShare(p)).ToArray();
            double[][] overlapM = setup.OverlapMatrix();
            double R(int p) => reach == null ? 1.0 : reach[p];
            double[] equal = Enumerable.Range(0, prior.Length).Select(p => 0.004 * R(p)).ToArray();   // equal effort per voter each party can reach
            double[] drifted = CampaignPersuasion.Apply(prior, equal);
            double[] relative = CampaignPersuasion.ApplyCampaign(prior, equal, reach, overlapM);
            double worstDrift = Enumerable.Range(0, prior.Length).Max(p => Math.Abs(relative[p] - prior[p]));
            Check(100.0 * worstDrift <= 1e-9, F("(K65) equal effort moves nothing on net: every party (one with no share too) at +0.40 points of sourced effect over the voters it can reach, the largest move {0:E2} points", 100.0 * worstDrift));
            _sb.Append("    MEASURED  the absorption alone, as before K65 (every party +0.40 points over its reach): ")
               .Append(string.Join(" ", Enumerable.Range(0, prior.Length).Select(p => F("{0} {1:+0.000;-0.000}", setup.Parties[p].Name, 100.0 * (drifted[p] - prior[p])))))
               .Append('\n');
            int largest = Enumerable.Range(0, prior.Length).OrderByDescending(p => prior[p]).First();
            int smallest = Enumerable.Range(0, prior.Length).Where(p => prior[p] > 0.0).OrderBy(p => prior[p]).First();
            var lone = new List<string>();
            double worstLone = 0.0;
            double[] aloneLargest = null;
            foreach (int solo in new[] { largest, smallest })
            {
                var alone = new double[prior.Length];
                alone[solo] = 0.004;
                double[] movedAll = CampaignPersuasion.ApplyCampaign(prior, alone, reach, overlapM);
                if (solo == largest) { aloneLargest = movedAll; }
                double moved = 100.0 * (movedAll[solo] - prior[solo]);
                worstLone = Math.Max(worstLone, Math.Abs(moved - 0.4));
                lone.Add(F("{0} {1:+0.000000}", setup.Parties[solo].Name, moved));
            }
            Check(worstLone <= 1e-9, F("(K81) a party campaigning ALONE at +0.40 points of sourced effect moves exactly that: {0} - no party's gain past its sourced effect", string.Join(", ", lone)));

            // K91 (2026-10-24), the closed form the implementation does not compute: where every party stands everywhere the only absorption with
            // columns and rows summing to one in which no smaller party absorbs more than a bigger one is the uniform 1/(n - 1) (an ask to Elias:
            // a share-weighted one cannot meet equal effort, `CampaignPersuasion.ApplyCampaign`'s note) - so a lone campaigner's gain is taken in
            // equal points from every other party holding a share; with regional candidacies the losses are printed against the proportional ones
            var losers = Enumerable.Range(0, prior.Length).Where(p => p != largest && prior[p] > 0.0).ToList();
            double den = losers.Sum(p => prior[p] * Overlap(overlapM, reach, p, largest));
            var lossRows = new List<string>();
            double worstUniform = 0.0;
            foreach (int p in losers.OrderByDescending(p => prior[p]))
            {
                double balanced = 100.0 * (prior[p] - aloneLargest[p]);
                worstUniform = Math.Max(worstUniform, Math.Abs(balanced - 0.4 / losers.Count));
                lossRows.Add(F("{0} {1:0.0000} (proportional {2:0.0000})", setup.Parties[p].Name, balanced, 0.4 * prior[p] * Overlap(overlapM, reach, p, largest) / den));
            }
            if (setup.Stands == null)
            {
                Check(worstUniform <= 1e-9, F("(K91) {0} alone at +0.40, everyone everywhere: every other party loses 0.4/{1} = {2:0.0000} points, the closed form (largest gap {3:E1}): {4}",
                    setup.Parties[largest].Name, losers.Count, 0.4 / losers.Count, worstUniform, string.Join(", ", lossRows)));
            }
            else
            {
                _sb.Append(F("    MEASURED  (K91) {0} alone at +0.40 on the regional candidacies, each other party's loss: {1}\n", setup.Parties[largest].Name, string.Join(", ", lossRows)));
            }

            // K92: an attack moves only its two parties - the largest wholly attacking the second, nothing else: the target loses what the attack
            // takes, the attacker gains exactly that, every idle party stays to 1e-15 (this would fail if the field absorbed the attack)
            int second = Enumerable.Range(0, prior.Length).Where(p => p != largest).OrderByDescending(p => prior[p]).First();
            var loss = new double[prior.Length]; var gain = new double[prior.Length]; var net = new double[prior.Length];
            loss[second] = 0.004; gain[largest] = 0.004; net[second] = -0.004;
            double[] attacked = CampaignPersuasion.ApplyCampaign(prior, net, reach, overlapM, loss, gain);
            double idleMost = Enumerable.Range(0, prior.Length).Where(p => p != largest && p != second).Max(p => Math.Abs(attacked[p] - prior[p]));
            Check(idleMost <= 1e-15 && Math.Abs(attacked[second] - prior[second] + 0.004) < 1e-15 && Math.Abs(attacked[largest] - prior[largest] - 0.004) < 1e-15,
                F("(K92) {0} attacking {1} for 0.40 points: {1} {2:+0.000000}, {0} {3:+0.000000}, every idle party within {4:E1}", setup.Parties[largest].Name, setup.Parties[second].Name,
                    100.0 * (attacked[second] - prior[second]), 100.0 * (attacked[largest] - prior[largest]), 100.0 * idleMost));

            // K92 on the live run: no party moves past its own sourced effect plus what its attacks took - an idle party lifted by an attack fails it
            r.Pressure.ShareEffects(setup.NationalAudience, norm, null, null, out double[] liveLoss, out double[] liveGain);
            double worstOver = double.NegativeInfinity; int worstParty = 0;
            for (int p = 0; p < setup.Parties.Length; p++)
            {
                double own = r.CampaignEffectPp[p] + 100.0 * liveLoss[p];
                double over = 100.0 * (r.FinalShares[p] - r.BaselineShares[p]) - Math.Max(0.0, own) - 100.0 * liveGain[p];
                if (over > worstOver) { worstOver = over; worstParty = p; }
            }
            Check(worstOver <= 1e-9, F("(K81, K92) every party's live move lies within its own sourced effect plus what its attacks took: the closest {0} at {1:+0.0000;-0.0000} points of it", setup.Parties[worstParty].Name, worstOver));

            // review 2's L2: a balancing that cannot be met (two parties of unequal reach, no third) falls back to the proportional form and says
            // so - it never collapses to zeros: the lone campaigner still moves its sourced effect
            CampaignPersuasion.Absorption unbalanced = CampaignPersuasion.BalancedAbsorption(new[] { 0.6, 0.4 }, new[] { 1.0, 0.5 }, null);
            double[] fellBack = CampaignPersuasion.ApplyBalanced(new[] { 0.6, 0.4 }, new[] { 0.004, 0.0 }, unbalanced);
            Check(unbalanced.Fallback != null && Math.Abs(fellBack[0] - 0.604) < 1e-12 && unbalanced.W[1][0] > 0.0,
                F("(L2) an unsolvable balancing falls back to the proportional absorption and names why (\"{0}\"): the lone campaigner moves {1:+0.000000}", unbalanced.Fallback ?? "none", 100.0 * (fellBack[0] - 0.6)));
        }

        private static double Overlap(double[][] o, double[] reach, int p, int q) => o != null ? o[p][q] : Math.Min(reach == null ? 1.0 : reach[p], reach == null ? 1.0 : reach[q]);

        /// <summary>
        /// Review 881b's M-r1, an independent recomputation: each party's move in points as its OWN tally implies it under the sourced CACEs - each
        /// class's exposure and quality read off the tally slot by slot (`TheEight`'s order: three personal, three impersonal, two earned, then the
        /// coverage), CACE × q held at the upper limit, coverage r·(1 − e^(−E/(r·N))) over the party's reach, its own effect absorbed by the others
        /// through the balanced absorption computed here by a different method (`BalancedByHand`), an attack moved from target to attacker (K92) -
        /// written here, never through `CampaignPersuasion`'s effect or absorption code.
        /// </summary>
        internal static double[] PredictedMovesPp(CampaignRun.Setup setup, CampaignRun.Result r)
        {
            int n = setup.Parties.Length;
            double electorate = setup.NationalAudience, norm = CampaignPersuasion.ElectorateMean(setup);
            double[] cace = { 0.58, 0.28, 100.0 / 800.0 }, upper = { 1.66, 0.83, 100.0 / 175.0 };   // [KB18] pp. 153, 159-160, re-read here so the recomputation shares no constant with the run
            int[] classOfSlot = { 0, 0, 0, 1, 1, 1, 2, 2, 2 };
            var own = new double[n];
            var transfer = new double[n];
            double[][] overlap = OverlapByHand(setup);
            for (int p = 0; p < n; p++)
            {
                double reach = setup.StandingShare(p);
                var e = new double[3]; var q = new double[3];
                for (int slot = 0; slot < classOfSlot.Length; slot++) { e[classOfSlot[slot]] += r.Pressure.Exposure(p, slot); q[classOfSlot[slot]] += r.Pressure.QualityOf(p, slot); }
                double pp = 0.0;
                for (int c = 0; c < 3; c++)
                {
                    // K66: the party's attacks share its class coverage - its own part takes its share of the coverage of all it sent
                    double made = 0.0;
                    for (int t = 0; t < n; t++) { r.Pressure.Attack(p, t, (ContactClass)c, out double te, out _); made += te; }
                    double all = e[c] + made;
                    pp += made <= 0.0 ? ClassPp(cace[c], upper[c], e[c], q[c], electorate, norm, reach)
                                      : PerContact(cace[c], upper[c], e[c], q[c], norm) * Cover(all, electorate, reach) * (e[c] > 0.0 ? e[c] / all : 0.0);
                    for (int t = 0; t < n; t++)
                    {
                        r.Pressure.Attack(p, t, (ContactClass)c, out double ae, out double aq);
                        if (ae <= 0.0) { continue; }
                        double onBallot = reach <= 0.0 ? 0.0 : Math.Min(1.0, overlap[p][t] / reach);   // review M2: where the target stands, of the attacker's reach
                        double taken = PerContact(cace[c], upper[c], ae, aq, norm) * Cover(all, electorate, reach) * (ae / all) * onBallot / 100.0;
                        transfer[t] -= taken;   // K92: from the target ...
                        transfer[p] += taken;   // ... to the attacker
                    }
                }
                own[p] += pp / 100.0;
            }
            double[] reaches = Enumerable.Range(0, n).Select(p => setup.StandingShare(p)).ToArray();
            double[] baseShares = r.BaselineShares;
            // a party with no share: read against the held field's mean per voter reached, floored at zero
            double mean = 0.0, weight = 0.0;
            for (int p = 0; p < n; p++) { if (baseShares[p] > 0.0 && reaches[p] > 0.0) { mean += baseShares[p] * own[p] / reaches[p]; weight += baseShares[p]; } }
            mean = weight > 0.0 ? mean / weight : 0.0;
            for (int z = 0; z < n; z++) { if (!(baseShares[z] > 0.0 && reaches[z] > 0.0)) { own[z] = Math.Max(0.0, own[z] - reaches[z] * mean); } }
            double[][] w = BalancedByHand(baseShares, reaches, overlap);
            var moves = new double[n];
            for (int p = 0; p < n; p++)
            {
                double d = own[p] + transfer[p];
                for (int q = 0; q < n; q++) { if (q != p) { d -= w[p][q] * own[q]; } }
                moves[p] = 100.0 * d;
            }
            return moves;
        }

        /// <summary>Review M2, by hand: [p][q] the share of the national audience in the regions where both parties stand.</summary>
        internal static double[][] OverlapByHand(CampaignRun.Setup setup)
        {
            int n = setup.Parties.Length;
            double all = setup.Regions == null ? 0.0 : setup.Regions.Sum(x => x.Audience);
            var o = new double[n][];
            for (int p = 0; p < n; p++)
            {
                o[p] = new double[n];
                for (int q = 0; q < n; q++)
                {
                    double both = 0.0;
                    for (int x = 0; setup.Regions != null && x < setup.Regions.Length; x++) { if (setup.StandsIn(p, x) && setup.StandsIn(q, x)) { both += setup.Regions[x].Audience; } }
                    o[p][q] = all > 0.0 ? both / all : 1.0;
                }
            }
            return o;
        }

        /// <summary>
        /// K81 and K91, by hand: where every held party meets every other in full (no regional candidacy), the closed form 1/(n_held − 1); else
        /// the balancing of who meets whom computed here columns first (the implementation scales rows first), to the same two conditions -
        /// columns summing to one, Σ_q W_pq r_q = r_p. A party with no share gives up nothing, and its column is the shares' proportion.
        /// </summary>
        internal static double[][] BalancedByHand(double[] shares, double[] reach, double[][] overlap)
        {
            int n = shares.Length;
            var held = Enumerable.Range(0, n).Select(p => shares[p] > 0.0 && reach[p] > 0.0).ToArray();
            int h = held.Count(x => x);
            var w = new double[n][];
            for (int p = 0; p < n; p++) { w[p] = new double[n]; }
            bool full = Enumerable.Range(0, n).All(p => !held[p] || Enumerable.Range(0, n).All(q => !held[q] || Math.Abs(overlap[p][q] - 1.0) < 1e-15));
            if (full)
            {
                for (int p = 0; p < n; p++) { for (int q = 0; q < n; q++) { if (p != q && held[p] && held[q]) { w[p][q] = 1.0 / (h - 1); } } }
            }
            else
            {
                var x = Enumerable.Range(0, n).Select(p => held[p] ? 1.0 : 0.0).ToArray();
                var y = (double[])x.Clone();
                for (int it = 0; it < 20000; it++)
                {
                    for (int q = 0; q < n; q++)
                    {
                        if (!held[q]) { continue; }
                        double col = 0.0;
                        for (int p = 0; p < n; p++) { if (p != q && held[p]) { col += x[p] * overlap[p][q]; } }
                        y[q] = col > 0.0 ? 1.0 / col : 0.0;
                    }
                    for (int p = 0; p < n; p++)
                    {
                        if (!held[p]) { continue; }
                        double row = 0.0;
                        for (int q = 0; q < n; q++) { if (q != p && held[q]) { row += overlap[p][q] * y[q] * reach[q]; } }
                        x[p] = row > 0.0 ? reach[p] / row : 0.0;
                    }
                }
                for (int p = 0; p < n; p++) { for (int q = 0; q < n; q++) { if (p != q && held[p] && held[q]) { w[p][q] = x[p] * overlap[p][q] * y[q]; } } }
            }
            for (int z = 0; z < n; z++)
            {
                if (held[z]) { continue; }
                double den = 0.0;
                for (int p = 0; p < n; p++) { if (held[p] && p != z) { den += shares[p] * overlap[p][z]; } }
                for (int p = 0; p < n; p++) { w[p][z] = held[p] && p != z && den > 0.0 ? shares[p] * overlap[p][z] / den : 0.0; }
            }
            return w;
        }

        private static double ClassPp(double cace, double upper, double e, double q, double electorate, double norm, double reach)
        {
            if (e <= 0.0 || q <= 0.0 || norm <= 0.0 || reach <= 0.0 || electorate <= 0.0) { return 0.0; }
            double perContact = Math.Min(cace * (q / e) / norm, upper);
            return perContact * reach * (1.0 - Math.Exp(-e / (reach * electorate)));
        }

        private static double PerContact(double cace, double upper, double e, double q, double norm) => e <= 0.0 || q <= 0.0 || norm <= 0.0 ? 0.0 : Math.Min(cace * (q / e) / norm, upper);

        private static double Cover(double e, double electorate, double reach) => e <= 0.0 || reach <= 0.0 || electorate <= 0.0 ? 0.0 : reach * (1.0 - Math.Exp(-e / (reach * electorate)));

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
            PlayerAttack(probe);
        }

        /// <summary>
        /// K3 (the player's ATTACK verb), K66 and K6 on Sweden's 2022 staging, seed 777: the queued record carries its target into the run; the
        /// player's party, scripted to air a television ad against the second party every day, splits each contact 40/60 between its own case and
        /// the target (exactly the negative campaign's share), takes the target's share down against the same script on its own case, and moves
        /// by exactly what its tallies imply; and a scripted party plays under no strategy - cast as the chaotic personality (the negative
        /// campaign), its own acts attack no one unless it names a target, while the same party played by the AI does attack.
        /// </summary>
        private static void PlayerAttack(CampaignRun.Setup probe)
        {
            var none = new (int, int, Scandal)[0];
            const int player = 0, target = 1;
            var record = new QueuedDecisionRecord { Day = 0, Kind = CampaignActionKind.TelevisionAd, RegionIndex = -1, Issue = -1, Spend = CampaignActions.Spec(CampaignActionKind.TelevisionAd).MoneyCost, AgainstParty = target };
            Check(record.ToDecision(probe).AgainstParty == target && new QueuedDecisionRecord { Kind = CampaignActionKind.TelevisionAd, AgainstParty = player + probe.Parties.Length }.ToDecision(probe).AgainstParty == -1,
                "(K3) a queued act's target rides the record into the run's decision (and one outside the campaign's parties is none)");
            CampaignActions.ActionSpec tv = CampaignActions.Spec(CampaignActionKind.TelevisionAd);
            Func<int, AiDecision[]> own = day => new[] { new AiDecision(CampaignActionKind.TelevisionAd, CampaignActions.ActionTarget.National(), "national", tv.MoneyCost / 4.0, tv.Hours, 0.0, false) };
            Func<int, AiDecision[]> attack = day => new[] { new AiDecision(CampaignActionKind.TelevisionAd, CampaignActions.ActionTarget.National(), "national", tv.MoneyCost / 4.0, tv.Hours, 0.0, false, target) };
            CampaignRun.Setup ownSetup = LiveCampaignSetup.Sweden(none, out _, vintage: ElectionVintage.Sweden2022, playerParty: player, playerScript: own);
            CampaignRun.Setup attackSetup = LiveCampaignSetup.Sweden(none, out _, vintage: ElectionVintage.Sweden2022, playerParty: player, playerScript: attack);
            CampaignRun.Result ownRun = CampaignAiHarness.RunSeeded(ownSetup, Seed), attackRun = CampaignAiHarness.RunSeeded(attackSetup, Seed);
            attackRun.Pressure.Attack(player, target, ContactClass.Impersonal, out double attackE, out _);
            double ownTv = attackRun.Pressure.Exposure(player, CampaignActions.TheEight.ToList().IndexOf(CampaignActionKind.TelevisionAd));
            double split = attackE + ownTv > 0.0 ? attackE / (attackE + ownTv) : 0.0;
            ownRun.Pressure.Attack(player, target, ContactClass.Impersonal, out double ownRunAttack, out _);
            Check(attackRun.Parties[player].ActionCount[CampaignActions.TheEight.ToList().IndexOf(CampaignActionKind.TelevisionAd)] > 0 && Math.Abs(split - CampaignStrategyModel.NegativeOpponentShare) < 1e-12 && ownRunAttack == 0.0,
                F("(K3, K66) the player's television against {0} every day: its contacts split {1:0.000000} to the attack (the negative campaign's share, {2}), the rest its own; on its own case it attacks no one", attackRun.Parties[target].Name, split, CampaignStrategyModel.NegativeOpponentShare));
            double targetMove = 100.0 * (attackRun.FinalShares[target] - ownRun.FinalShares[target]);
            double impliedTarget = PredictedMovesPp(attackSetup, attackRun)[target] - PredictedMovesPp(ownSetup, ownRun)[target] + 100.0 * (attackRun.BaselineShares[target] - ownRun.BaselineShares[target]);
            bool matches = ObservedMatchesSources(attackSetup, attackRun, out string worst);
            Check(targetMove < 0.0 && Math.Abs(targetMove - impliedTarget) <= Recomputed && matches,
                F("(K3) the attack takes {0} {1:+0.0000;-0.0000} points against the same script on the player's own case (its tallies imply {2:+0.0000;-0.0000}); every party's move is its tally's - {3}", attackRun.Parties[target].Name, targetMove, impliedTarget, worst));

            // K2: the campaign poll is information the player buys - a scripted party polls only when its script queues a poll
            var pollRecord = new QueuedDecisionRecord { Day = 0, Kind = CampaignActionKind.CommissionPolling, Spend = probe.InternalHouse.Cost };
            AiDecision pollDecision = pollRecord.ToDecision(probe);
            Func<int, AiDecision[]> polled = day => day % 7 == 0 ? new[] { pollDecision } : new AiDecision[0];   // polls alone, so the chest pays every one
            CampaignRun.Setup pollSetup = LiveCampaignSetup.Sweden(none, out _, vintage: ElectionVintage.Sweden2022, playerParty: player, playerScript: polled);
            CampaignRun.Result pollRun = CampaignAiHarness.RunSeeded(pollSetup, Seed);
            int weeks = (pollSetup.Calendar.TotalCampaignDays + 6) / 7;
            int pollsLogged = pollRun.Parties[player].Log.Count(d => d.Kind == CampaignActionKind.CommissionPolling);
            Check(ownRun.Parties[player].PollsBought == 0 && pollRun.Parties[player].PollsBought == weeks && pollsLogged == weeks
                  && Math.Abs(pollRun.Parties[player].PollMoney - weeks * probe.InternalHouse.Cost) < 1e-6 && pollDecision.Hours == CampaignAi.PollingHours,
                F("(K2) the player's party buys the polls it queues and no others: {0} queued (one a week, nothing else) and {0} bought at {1:N0} each; none queued, none bought ({2}) - the cast personality no longer polls for the player",
                    pollRun.Parties[player].PollsBought, probe.InternalHouse.Cost, ownRun.Parties[player].PollsBought));

            // K1 rows 9 and 12 (R2, K5): the topic and the outlay are PARAMETERS - the record carries the topic into the run; a contact on the
            // electorate's most salient contested issue lands at that issue's relevance, three times the outlay reaches more people; in both every
            // party's move is its tally's under the unchanged class CACEs (no larger figure for a chosen topic or a bigger buy)
            int top = -1;
            for (int i = 0; probe.TrueSalience != null && i < probe.TrueSalience.Length; i++) { if (!double.IsNaN(probe.TrueSalience[i]) && (top < 0 || probe.TrueSalience[i] > probe.TrueSalience[top])) { top = i; } }
            var topicRecord = new QueuedDecisionRecord { Day = 0, Kind = CampaignActionKind.TelevisionAd, Issue = top, Spend = tv.MoneyCost / 4.0 };
            AiDecision topical = topicRecord.ToDecision(probe);
            Func<int, AiDecision[]> onTopic = day => new[] { topical };
            Func<int, AiDecision[]> bigger = day => new[] { new AiDecision(CampaignActionKind.TelevisionAd, CampaignActions.ActionTarget.National(), "national", 3.0 * tv.MoneyCost / 4.0, tv.Hours, 0.0, false) };
            CampaignRun.Setup topicSetup = LiveCampaignSetup.Sweden(none, out _, vintage: ElectionVintage.Sweden2022, playerParty: player, playerScript: onTopic);
            CampaignRun.Setup biggerSetup = LiveCampaignSetup.Sweden(none, out _, vintage: ElectionVintage.Sweden2022, playerParty: player, playerScript: bigger);
            CampaignRun.Result topicRun = CampaignAiHarness.RunSeeded(topicSetup, Seed), biggerRun = CampaignAiHarness.RunSeeded(biggerSetup, Seed);
            int tvSlot = CampaignActions.TheEight.ToList().IndexOf(CampaignActionKind.TelevisionAd);
            double ownExposure = ownRun.Pressure.Exposure(player, tvSlot), biggerExposure = biggerRun.Pressure.Exposure(player, tvSlot);
            int ownActs = ownRun.Parties[player].ActionCount[tvSlot], biggerActs = biggerRun.Parties[player].ActionCount[tvSlot];
            bool topicMatches = ObservedMatchesSources(topicSetup, topicRun, out string topicWorst), biggerMatches = ObservedMatchesSources(biggerSetup, biggerRun, out string biggerWorst);
            Check(top >= 0 && topical.Target.Issue == (IssueId)top && topicMatches && biggerMatches && biggerActs > 0 && ownActs > 0 && biggerExposure / biggerActs > ownExposure / ownActs,
                F("(K1 rows 9, 12) a topic rides the record ({0}) and its contacts' move is their tally's ({1}); three times the outlay reaches {2:N0} people's exposure an act against {3:N0} (its chest buys {4} acts, the single outlay {5}), its move its tally's ({6})",
                    top >= 0 ? ((IssueId)top).ToString() : "none", topicWorst, biggerExposure / Math.Max(1, biggerActs), ownExposure / Math.Max(1, ownActs), biggerActs, ownActs, biggerWorst));

            // K82: the phone bank - the player's party calls its supporters in one region every day; the calls count only in the final week, and
            // a reached supporter's turnout rises by Mann & Klofstad's high-quality figure on the share the calls reached
            int callRegion = Enumerable.Range(0, probe.Regions.Length).OrderByDescending(x => probe.Regions[x].Eligible).First();
            Func<int, AiDecision[]> phones = day => new[] { new QueuedDecisionRecord { Day = day, Kind = CampaignActionKind.PhoneBank, RegionIndex = callRegion, Spend = GotvTurnout.PhoneDayMoney }.ToDecision(probe) };
            CampaignRun.Setup phoneSetup = LiveCampaignSetup.Sweden(none, out _, vintage: ElectionVintage.Sweden2022, playerParty: player, playerScript: phones);
            CampaignRun.Result phoneRun = CampaignAiHarness.RunSeeded(phoneSetup, Seed);
            var callDays = phoneRun.Parties[player].Log.Where(x => x.Kind == CampaignActionKind.PhoneBank).Select(x => x.Day).ToList();
            int firstFinalWeekDay = Enumerable.Range(0, phoneSetup.Calendar.TotalCampaignDays).First(x => phoneSetup.Calendar.DaysUntilElection(phoneSetup.Calendar.CampaignStart.AddDays(x)) <= GotvTurnout.PhoneFinalWeekDays);
            double calls = phoneRun.Gotv.PhoneContacts(callRegion, player);
            double supporters = phoneRun.Gotv.Eligible(callRegion) * probe.PriorShares[player] / probe.PriorShares.Sum();
            double lift = 100.0 * (GotvTurnout.PartyTurnout(0.8, 0.0, supporters, GotvTurnout.EuropeanDoorCacePp, calls) - 0.8);
            double handLift = 100.0 * (1.0 - Math.Exp(-calls / supporters)) * 1.15 / 100.0;
            Check(callDays.Count > 0 && callDays.All(x => x >= firstFinalWeekDay) && calls > 0.0 && ownRun.Gotv.PhoneContacts(callRegion, player) == 0.0 && Math.Abs(lift - handLift) < 1e-12
                  && Math.Abs(1.0 / GotvModel.Spec(GotvOperation.PhoneBanking).VolunteerHoursPerContact - 50.0) < 1e-9,
                F("(K82) the phone bank calls only in the final week (days {0}-{1}, from day {2}): {3:N0} completed calls in {4}, {5:0.00} % of the party's supporters there reached; their turnout +{6:0.0000} points (by hand the reached share x 1.15)",
                    callDays.Count > 0 ? callDays.Min() : -1, callDays.Count > 0 ? callDays.Max() : -1, firstFinalWeekDay, calls, probe.Regions[callRegion].Name, 100.0 * (1.0 - Math.Exp(-calls / supporters)), lift));

            // review 2's L4: the calls counted end to end - a region's count (`ElectionDay.Count`, the polling day's own) with 100 000 completed
            // calls to the first party's 600 000 supporters at a base of 80 %: its votes are its supporters at the base plus the reached share at
            // 1.15 points, to the rounding of a vote; with no calls, exactly the base
            var callGotv = new RegionalMobilization(new[] { 1_000_000.0 }, 2);
            callGotv.CarryCalls(0, 0, 100_000.0);
            ElectionDay.Result counted = ElectionDay.Count(new[] { "the region" }, new[] { new[] { 0.6, 0.4 } }, callGotv, new[] { 0.8 }, new[] { GotvTurnout.EuropeanDoorCacePp }, new System.Random(1), 0.0);
            double expectVotes = 600_000.0 * (0.8 + (1.0 - Math.Exp(-100_000.0 / 600_000.0)) * GotvTurnout.PhoneCacePp / 100.0);
            Check(Math.Abs(counted.Regions[0].Votes[0] - expectVotes) <= 0.5 && Math.Abs(counted.Regions[0].Votes[1] - 400_000.0 * 0.8) <= 0.5,
                F("(K82, L4) the count reads the calls: the called party's votes {0:N0} against {1:N0} by hand (base 80 % plus the reached share at 1.15 points), the other's {2:N0} at the base",
                    counted.Regions[0].Votes[0], expectVotes, counted.Regions[0].Votes[1]));

            // K6: the player's strategy is cut - a scripted party cast as the chaotic personality attacks no one on its own case
            int chaotic = Array.IndexOf(CampaignAiHarness.Assignment, AiPersonality.Chaotic);
            if (chaotic < 0 || chaotic >= probe.Parties.Length) { Check(false, "(K6) the fixture cast has no chaotic party to script"); return; }
            Func<int, AiDecision[]> plain = day => new[] { new AiDecision(CampaignActionKind.TelevisionAd, CampaignActions.ActionTarget.National(), "national", tv.MoneyCost / 4.0, tv.Hours, 0.0, false) };
            CampaignRun.Setup scriptedChaos = LiveCampaignSetup.Sweden(none, out _, vintage: ElectionVintage.Sweden2022, playerParty: chaotic, playerScript: plain, castOverride: CampaignAiHarness.Assignment);
            CampaignRun.Setup aiChaos = LiveCampaignSetup.Sweden(none, out _, vintage: ElectionVintage.Sweden2022, castOverride: CampaignAiHarness.Assignment);
            double MadeBy(CampaignRun.Result r, int p)
            {
                double made = 0.0;
                for (int t2 = 0; t2 < r.Parties.Length; t2++) { foreach (ContactClass c in new[] { ContactClass.Personal, ContactClass.Impersonal, ContactClass.Earned }) { r.Pressure.Attack(p, t2, c, out double e, out _); made += e; } }
                return made;
            }
            double scriptedMade = MadeBy(CampaignAiHarness.RunSeeded(scriptedChaos, Seed), chaotic), aiMade = MadeBy(CampaignAiHarness.RunSeeded(aiChaos, Seed), chaotic);
            Check(aiChaos.Parties[chaotic].Personality == AiPersonality.Chaotic && scriptedMade == 0.0 && aiMade > 0.0,
                F("(K6) {0}, cast chaotic (the negative campaign): played by the AI its contacts attack ({1:N0} people's exposure); scripted as the player's, on its own case, they attack no one ({2:N0}) - the player's party plays under no strategy",
                    aiChaos.Parties[chaotic].Name, aiMade, scriptedMade));
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
                if (buyer == 0)
                {
                    // K67 (2026-10-23): the run-up HQ shows the buy's estimated effect beside the poll - the snapshot's note for the player's row
                    // carries the sourced size and its 95 % band while the buy airs, and nothing the day after its week
                    var played = new Country { Id = CountryId.Sweden, Name = "Sweden", PlayerPartyAbbrev = staging.Parties[buyer].Name };
                    string airing = LiveCampaignSnapshot.BuildPreCampaign(tv, played, 50.0)?.PlayerPollNote;
                    string expectedNote = F("TV BUY {0:+0.00} ±{1:0.00} PP UNTIL", sourcedPp, 1.96 * sePp);
                    PreCampaignRun.State later = PreCampaignRun.Begin(staging, buyer, new System.Random(11));
                    foreach (int aired in tv.TelevisionAired) { later.TelevisionAired.Add(aired); }
                    for (int d = 0; d < buyDay + CampaignPersuasion.TelevisionWeekDays; d++) { PreCampaignRun.StepDay(later, null); }
                    string after = LiveCampaignSnapshot.BuildPreCampaign(later, played, 50.0)?.PlayerPollNote;
                    Check(airing != null && airing.StartsWith(expectedNote, StringComparison.Ordinal) && after == null,
                        F("(K67) the run-up HQ's poll carries the buy beside the party's figure while it airs - \"{0}\" - and nothing once its week is out", airing ?? "none"));
                }

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

            // K84: FUNDRAISE - a mailing to the party's own supporters at the country's postal tariff, the letters' [BCP12] revenue into the chest;
            // refused where no tariff is on disk (Sweden, BILLED). Held on the arithmetic with each sourced tariff (the staging's electorate stands in)
            PreCampaignRun.State unpriced = PreCampaignRun.Begin(staging, party, new System.Random(43), CountryId.Sweden);
            PreCampaignRun.StepDay(unpriced, new[] { new PreCampaignRun.Decision(CampaignActionKind.Fundraise) });
            var mailings = new List<string>();
            bool mailingsHold = unpriced.Log.Count == 1 && unpriced.Log[0].Refusal != null && Math.Abs(unpriced.Money - staging.Parties[party].StartingMoney) < 1e-9;
            foreach (CountryId c in new[] { CountryId.Germany, CountryId.USA })
            {
                PreCampaignRun.State mail = PreCampaignRun.Begin(staging, party, new System.Random(43), c);
                PreCampaignRun.StepDay(mail, new[] { new PreCampaignRun.Decision(CampaignActionKind.Fundraise) });
                double letters = Math.Min(PreCampaignRun.MailingLetters, Math.Floor(PreCampaignRun.SupportersLeft(PreCampaignRun.Begin(staging, party, new System.Random(43), c))));
                double expect = letters * (PreCampaignRun.RevenuePerLetterUsd * EnergyLayer.NationalPerUsd(c) - PreCampaignRun.LetterTariff(c)) / LiveCampaignSetup.PursePerUnit(c);   // K110: in the purse's units
                mailingsHold &= mail.Log.Count == 1 && mail.Log[0].Refusal == null && Math.Abs((mail.Money - staging.Parties[party].StartingMoney) - expect) < 1e-6 && mail.LettersSent == letters;
                mailings.Add(F("{0}: {1:N0} letters at {2} postage raise {3:N0} for {4:N0} - the chest {5:+#,0;-#,0} (in the national currency, K110: the purse's units times {6:0.####})", c, letters, PreCampaignRun.LetterTariff(c), mail.Raised * LiveCampaignSetup.PursePerUnit(c), mail.Log[0].Cost * LiveCampaignSetup.PursePerUnit(c), (mail.Money - staging.Parties[party].StartingMoney) * LiveCampaignSetup.PursePerUnit(c), LiveCampaignSetup.PursePerUnit(c)));
            }
            Check(mailingsHold, F("(K84) a fundraising mailing pays the country's postage and banks [BCP12]'s revenue per letter, never a vote; Sweden refused (\"{0}\"); {1}",
                unpriced.Log.Count == 1 ? unpriced.Log[0].Refusal : "-", string.Join("; ", mailings)));

            PreCampaignRun.State office = PreCampaignRun.Begin(staging, party, new System.Random(41));
            int region = PreCampaignRun.NextOfficeRegion(office);

            // K1 row 5 (S-C2): the run-up's map sites the next office - a pick where the party stands and has none is where it opens, and
            // the sheet says so; with no pick, or a pick where an office is planned, CL-1's rule (the largest electorate without one) stands
            int other = Enumerable.Range(0, staging.Regions.Length).First(r => r != region && staging.StandsIn(party, r) && !office.HasOffice(r));
            var playedSweden = new Country { Id = CountryId.Sweden, Name = "Sweden", PlayerPartyAbbrev = staging.Parties[party].Name };
            CampaignMapSnapshot? siting = LiveCampaignSnapshot.BuildPreCampaignMap(office, playedSweden, 50.0, null, other);
            int sitedPick = LiveCampaignSnapshot.NextOfficeSite(office, other, out bool wasPicked), sitedNone = LiveCampaignSnapshot.NextOfficeSite(office, -1, out bool nonePicked);
            int stagedOffice = staging.Parties[party].Offices.Length > 0 ? staging.Parties[party].Offices[0] : -1;
            int sitedTaken = stagedOffice >= 0 ? LiveCampaignSnapshot.NextOfficeSite(office, stagedOffice, out _) : region;
            Check(siting.HasValue && siting.Value.Siting && siting.Value.Live && siting.Value.NextLocalActRegion == other && sitedPick == other && wasPicked
                  && sitedNone == region && !nonePicked && sitedTaken == region && siting.Value.Campaign.LocalActsRegion != null && siting.Value.Campaign.LocalActsRegion.StartsWith(staging.Regions[other].Name, StringComparison.Ordinal),
                F("(S-C2) the run-up's map sites the next office: a pick in {0} opens it there (the sheet: \"{1}\"); no pick, or a pick where one is planned, the largest electorate without one ({2})",
                    staging.Regions[other].Name, siting.HasValue ? siting.Value.Campaign.LocalActsRegion : "none", staging.Regions[region].Name));
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
