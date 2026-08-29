using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-C1 — an AI-ONLY campaign, run day by day through the same pieces the player's screens
    /// read: W-B1's calendar, W-B2's resources, W-B3's actions and §42 chain, W-B10's polls, W-A1's
    /// derived loyalty and §8's preference model. PURE, WIRED TO NOTHING (R-N2): the harness is the
    /// only caller, it is handed everything in a <see cref="Setup"/>, and it touches no `World`.
    ///
    /// **This type IS the world for the duration of a run, and is therefore the one place the
    /// truth lives**: it holds the electorate's real salience and each party's real issue-match,
    /// resolves every action against them, recomputes the true preference at each day's end, and
    /// conducts every poll from it. The AIs see none of that directly — they are handed an
    /// <see cref="AiView"/> built from the polls they have bought or been published, exactly as a
    /// player would be (§36). The seam is the point: `CampaignAi` cannot read a field this type
    /// does not put on the view.
    ///
    /// **What moves and what does not.** The true preference is
    /// `PreferenceModel.Preference(compatibility + campaignBonus, prior, loyaltyPerParty)` — the
    /// same recomputation W-B3's harness proved, so a campaign changes inputs and the shares are
    /// always derived; with no actions at all the shares equal the prior by construction (asserted).
    /// Momentum (§22) is carried and decays but takes no shock, because nothing that shocks it
    /// (debates §15, events §18, media §13) exists yet — the view shows zeros honestly rather than
    /// an invented drift. Pre-campaign days are not simulated: §3's preparation verbs have no
    /// price yet, so the run covers the campaign proper (W-B1's `CampaignStart` to the day before
    /// polling day) and stops at election day, which is W-D1's.
    /// </summary>
    public static class CampaignRun
    {
        /// <summary>One party as the run needs it: who it is, how it decides, what it knows about itself, and the truth about it the AI cannot see.</summary>
        public readonly struct PartySetup
        {
            public readonly string Name;
            public readonly AiPersonality Personality;
            public readonly double Credibility;
            public readonly double StartingMoney;
            /// <summary>The TRUE issue-match per <see cref="IssueId"/> (NaN where the issue is not contested). Never handed to the AI; measured through polling.</summary>
            public readonly double[] TrueIssueMatch;

            public PartySetup(string name, AiPersonality personality, double credibility, double startingMoney, double[] trueIssueMatch)
            {
                Name = name; Personality = personality; Credibility = credibility; StartingMoney = startingMoney;
                TrueIssueMatch = trueIssueMatch;
            }
        }

        public readonly struct Setup
        {
            public readonly CampaignCalendar Calendar;
            public readonly PartySetup[] Parties;
            /// <summary>Where the electorate's vote sat at the last election (the §8 prior), one per party, summing to 1.</summary>
            public readonly double[] PriorShares;
            /// <summary>W-A1's derived loyalty per party.</summary>
            public readonly double[] LoyaltyPerParty;
            /// <summary>Each party's compatibility with the electorate at the campaign's start (0–100).</summary>
            public readonly double[] Compatibility;
            /// <summary>The TRUE salience per <see cref="IssueId"/> (NaN = not an issue in this contest).</summary>
            public readonly double[] TrueSalience;
            public readonly double NationalAudience;
            public readonly RegionAudience[] Regions;
            /// <summary>The published tracker every party sees, and how often it fields.</summary>
            public readonly PollingHouse PublicHouse;
            public readonly int PublicPollEveryDays;
            /// <summary>What a party buys when it commissions its own poll (§21): horse race AND issue detail.</summary>
            public readonly PollingHouse InternalHouse;
            /// <summary>The electorate's loyalty as ONE group (0–100) — W-A1's size-weighted mean, a public derivation from past returns — until W-F4's voter groups give §11's strategies their per-group targets.</summary>
            public readonly double ElectorateLoyalty;

            public Setup(CampaignCalendar calendar, PartySetup[] parties, double[] priorShares, double[] loyaltyPerParty,
                double[] compatibility, double[] trueSalience, double nationalAudience, RegionAudience[] regions,
                PollingHouse publicHouse, int publicPollEveryDays, PollingHouse internalHouse, double electorateLoyalty = 50.0)
            {
                ElectorateLoyalty = electorateLoyalty;
                if (parties == null || parties.Length == 0) { throw new ArgumentException("no parties"); }
                if (priorShares.Length != parties.Length || loyaltyPerParty.Length != parties.Length || compatibility.Length != parties.Length)
                {
                    throw new ArgumentException("prior, loyalty and compatibility must be one per party");
                }

                Calendar = calendar; Parties = parties; PriorShares = priorShares; LoyaltyPerParty = loyaltyPerParty;
                Compatibility = compatibility; TrueSalience = trueSalience; NationalAudience = nationalAudience;
                Regions = regions; PublicHouse = publicHouse; PublicPollEveryDays = publicPollEveryDays;
                InternalHouse = internalHouse;
            }
        }

        public readonly struct DecisionRecord
        {
            public readonly int Day;
            public readonly CampaignActionKind Kind;
            public readonly string Target;
            public readonly double Spend;
            public readonly double Score;
            public readonly bool Blind;

            public DecisionRecord(int day, CampaignActionKind kind, string target, double spend, double score, bool blind)
            {
                Day = day; Kind = kind; Target = target; Spend = spend; Score = score; Blind = blind;
            }
        }

        /// <summary>What one party did over the campaign — the action mix the done-when compares.</summary>
        public sealed class PartyLedger
        {
            public readonly string Name;
            public readonly AiPersonality Personality;
            /// <summary>Count per §12 action, in `TheEight`'s order.</summary>
            public readonly int[] ActionCount = new int[CampaignActions.TheEight.Length];
            public readonly double[] MoneyByAction = new double[CampaignActions.TheEight.Length];
            public int PollsBought;
            public double PollMoney;
            public int BlindDecisions;
            public double MoneyLeft;
            public double PersuasionDelivered;
            public double EnthusiasmDelivered;
            public readonly List<DecisionRecord> Log = new List<DecisionRecord>();

            public PartyLedger(string name, AiPersonality personality)
            {
                Name = name; Personality = personality;
            }

            public int TotalActions
            {
                get { int n = 0; foreach (int c in ActionCount) { n += c; } return n; }
            }

            public double MoneySpentOnActions
            {
                get { double m = 0.0; foreach (double x in MoneyByAction) { m += x; } return m; }
            }

            /// <summary>The action mix as fractions of all actions taken (zeros when none) — the vector two personalities are compared on.</summary>
            public double[] Mix()
            {
                var mix = new double[ActionCount.Length];
                int total = TotalActions;
                if (total == 0) { return mix; }
                for (int i = 0; i < mix.Length; i++) { mix[i] = (double)ActionCount[i] / total; }
                return mix;
            }
        }

        public sealed class Result
        {
            public double[] FinalShares;
            /// <summary>The shares with no campaign at all — the prior, by construction; asserted rather than assumed.</summary>
            public double[] BaselineShares;
            public PartyLedger[] Parties;
            public int DaysRun;
            public int PublicPolls;
            /// <summary>A deterministic digest of every decision and the final shares — two runs of one seed must print the same one.</summary>
            public string Digest;
        }

        public static Result Simulate(Setup setup, System.Random random)
        {
            if (random == null) { throw new ArgumentNullException(nameof(random)); }

            int partyCount = setup.Parties.Length;
            int issueCount = IssueVector.IssueCount;

            // --- the truth, held here and nowhere the AI can reach ---
            double[] prior = Normalised(setup.PriorShares);
            var pressure = new CampaignPressure(partyCount);
            double[] truePreference = CurrentPreference(setup, prior, pressure);
            double[] baseline = (double[])truePreference.Clone();

            var momentum = new MomentumTracker(partyCount);
            var momentumPp = new double[partyCount];

            // --- what each party knows ---
            var ledgers = new PartyLedger[partyCount];
            var pools = new ResourcePool[partyCount];
            var latestPoll = new Poll?[partyCount];
            var issues = new IssueMeasurement[partyCount][];
            var lastOwnPollDay = new int[partyCount];
            var reserve = new double[partyCount];
            var profiles = new PersonalityProfile[partyCount];
            for (int p = 0; p < partyCount; p++)
            {
                ledgers[p] = new PartyLedger(setup.Parties[p].Name, setup.Parties[p].Personality);
                pools[p] = new ResourcePool(setup.Parties[p].StartingMoney, 0.0, 0);
                issues[p] = new IssueMeasurement[issueCount];
                lastOwnPollDay[p] = int.MinValue;
                profiles[p] = PersonalityCatalog.Profile(setup.Parties[p].Personality);
            }

            var digest = new StringBuilder();
            int publicPolls = 0;
            int totalDays = setup.Calendar.TotalCampaignDays;

            for (int day = 0; day < totalDays; day++)
            {
                DateTime today = setup.Calendar.CampaignStart.AddDays(day);
                CampaignPhase phase = setup.Calendar.PhaseOn(today);

                // The published tracker: fielded from the true preference, seen by everyone.
                if (setup.PublicPollEveryDays > 0 && day % setup.PublicPollEveryDays == 0)
                {
                    Poll tracker = PollingSystem.Conduct(momentum.Apply(truePreference), setup.PublicHouse, today, random);
                    publicPolls++;
                    for (int p = 0; p < partyCount; p++) { latestPoll[p] = tracker; }
                }

                for (int p = 0; p < partyCount; p++)
                {
                    pools[p] = pools[p].StartDay();
                    PartyLedger ledger = ledgers[p];

                    // The pace releases today's money into the reserve (capped at what the party has).
                    reserve[p] = Math.Min(pools[p].Money, reserve[p] + CampaignAi.DailyRelease(profiles[p], pools[p].Money, totalDays - day));

                    // The poll decision is taken on the view BEFORE today's measurement, and the
                    // measurement then feeds the same day's action estimates (a fresh poll is
                    // what you act on, not what you file).
                    AiView view = BuildView(setup, p, phase, today, pools[p], reserve[p], latestPoll[p], momentumPp, issues[p], day, lastOwnPollDay[p]);
                    if (CampaignAi.WantsPoll(view, profiles[p], setup.InternalHouse))
                    {
                        if (pools[p].TrySpend(setup.InternalHouse.Cost, CampaignAi.PollingHours, out ResourcePool afterPoll))
                        {
                            pools[p] = afterPoll;
                            reserve[p] -= setup.InternalHouse.Cost;
                            latestPoll[p] = PollingSystem.Conduct(momentum.Apply(truePreference), setup.InternalHouse, today, random);
                            issues[p] = CampaignIntelligence.MeasureIssues(setup.TrueSalience, setup.Parties[p].TrueIssueMatch,
                                setup.InternalHouse.SampleSize, random);
                            lastOwnPollDay[p] = day;
                            ledger.PollsBought++;
                            ledger.PollMoney += setup.InternalHouse.Cost;
                            ledger.Log.Add(new DecisionRecord(day, CampaignActionKind.CommissionPolling, setup.InternalHouse.Name,
                                setup.InternalHouse.Cost, 0.0, false));
                            Append(digest, day, p, CampaignActionKind.CommissionPolling, setup.InternalHouse.Name, setup.InternalHouse.Cost);
                        }
                    }

                    view = BuildView(setup, p, phase, today, pools[p], reserve[p], latestPoll[p], momentumPp, issues[p], day, lastOwnPollDay[p]);

                    // Actions: the AI's plan, applied one by one against the TRUE inputs - the
                    // world's response, which the AI estimated but did not see.
                    for (int guard = 0; guard < 64; guard++)
                    {
                        List<ScoredCandidate> candidates = CampaignAi.Evaluate(view, profiles[p], pools[p], reserve[p]);
                        ScoredCandidate? chosen = CampaignAi.Choose(candidates, profiles[p].Temperature, random);
                        if (chosen == null) { break; }

                        AiDecision d = chosen.Value.Decision;
                        if (!pools[p].TrySpend(d.Spend, d.Hours, out ResourcePool after)) { break; }
                        pools[p] = after;
                        reserve[p] -= d.Spend;

                        CampaignActions.ActionSpec spec = CampaignActions.Spec(d.Kind);
                        double audience = d.Target.RegionIndex >= 0 ? setup.Regions[d.Target.RegionIndex].Audience : setup.NationalAudience;
                        TrueMessage(setup, p, d.Target.Issue, out double salience, out double match);

                        // W-B6: the party's strategy modifies the world's response - the electorate
                        // as one group at its derived loyalty (W-F4's groups make this per group),
                        // the message prioritised when it is on the electorate's most salient issue.
                        StrategyModifiers modifiers = CampaignStrategyModel.Modifiers(profiles[p].Strategy, setup.ElectorateLoyalty,
                            d.Target.Issue.HasValue && IsTopSalience(setup, d.Target.Issue.Value));
                        CampaignActions.ChainTrace trace = CampaignStrategyModel.Resolve(spec, audience, salience, match,
                            setup.Parties[p].Credibility, d.Spend, modifiers);
                        pressure.Add(p, trace);
                        if (modifiers.OpponentShare > 0.0)
                        {
                            int target = view.PolledLeaderOtherThanSelf;   // chosen from the Poll, not the truth
                            if (target >= 0) { pressure.AddAgainst(target, trace.Persuasion * modifiers.OpponentShare); }
                        }

                        int slot = CampaignAi.IndexOfAction(d.Kind);
                        ledger.ActionCount[slot]++;
                        ledger.MoneyByAction[slot] += d.Spend;
                        if (d.Blind) { ledger.BlindDecisions++; }
                        ledger.PersuasionDelivered += trace.Persuasion;
                        ledger.EnthusiasmDelivered += trace.Enthusiasm;
                        ledger.Log.Add(new DecisionRecord(day, d.Kind, d.TargetLabel + IssueSuffix(d.Target.Issue), d.Spend, d.Score, d.Blind));
                        Append(digest, day, p, d.Kind, d.TargetLabel + IssueSuffix(d.Target.Issue), d.Spend);

                        view = BuildView(setup, p, phase, today, pools[p], reserve[p], latestPoll[p], momentumPp, issues[p], day, lastOwnPollDay[p]);
                    }
                }

                // The day closes: the true preference is RECOMPUTED from the moved inputs, never patched.
                truePreference = CurrentPreference(setup, prior, pressure);
                momentum.Advance(1.0);
                for (int p = 0; p < partyCount; p++) { momentumPp[p] = momentum.MomentumPp(p); }
            }

            for (int p = 0; p < partyCount; p++) { ledgers[p].MoneyLeft = pools[p].Money; }

            for (int p = 0; p < partyCount; p++)
            {
                digest.Append(truePreference[p].ToString("F9", CultureInfo.InvariantCulture)).Append('|');
            }

            return new Result
            {
                FinalShares = truePreference,
                BaselineShares = baseline,
                Parties = ledgers,
                DaysRun = totalDays,
                PublicPolls = publicPolls,
                Digest = Fnv1a64(digest.ToString()),
            };
        }

        // ---------- the seam: what the AI is handed ----------

        private static AiView BuildView(Setup setup, int party, CampaignPhase phase, DateTime today, ResourcePool pool,
            double reserve, Poll? latest, double[] momentumPp, IssueMeasurement[] issues, int day, int lastOwnPollDay)
        {
            int since = lastOwnPollDay == int.MinValue ? -1 : day - lastOwnPollDay;
            return new AiView(party, phase, setup.Calendar.DaysUntilElection(today), pool, reserve,
                latest.HasValue, latest ?? default, (double[])momentumPp.Clone(), issues,
                setup.Parties[party].Credibility, setup.NationalAudience, setup.Regions, since,
                PersonalityCatalog.Profile(setup.Parties[party].Personality).Strategy, setup.ElectorateLoyalty);
        }

        /// <summary>Whether an issue is the electorate's most salient (the populist's "prioritised" test for a one-group electorate).</summary>
        private static bool IsTopSalience(Setup setup, IssueId issue)
        {
            double top = double.NegativeInfinity;
            int topIndex = -1;
            for (int i = 0; i < setup.TrueSalience.Length; i++)
            {
                if (double.IsNaN(setup.TrueSalience[i])) { continue; }
                if (setup.TrueSalience[i] > top) { top = setup.TrueSalience[i]; topIndex = i; }
            }

            return topIndex == (int)issue;
        }

        // ---------- the truth ----------

        private static double[] CurrentPreference(Setup setup, double[] prior, CampaignPressure pressure)
        {
            double[] bonus = pressure.ToCompatibilityBonus();
            var compatibility = new double[setup.Compatibility.Length];
            for (int i = 0; i < compatibility.Length; i++) { compatibility[i] = setup.Compatibility[i] + bonus[i]; }
            return PreferenceModel.Preference(compatibility, prior, setup.LoyaltyPerParty);
        }

        /// <summary>The TRUE salience and match behind a message: one issue's, or the mean over the contested issues for a general message.</summary>
        private static void TrueMessage(Setup setup, int party, IssueId? issue, out double salience, out double match)
        {
            double[] matchRow = setup.Parties[party].TrueIssueMatch;
            if (issue.HasValue)
            {
                salience = setup.TrueSalience[(int)issue.Value];
                match = matchRow[(int)issue.Value];
                if (double.IsNaN(salience) || double.IsNaN(match)) { salience = 0.0; match = 0.0; }
                return;
            }

            double s = 0.0, m = 0.0;
            int n = 0;
            for (int i = 0; i < setup.TrueSalience.Length; i++)
            {
                if (double.IsNaN(setup.TrueSalience[i]) || double.IsNaN(matchRow[i])) { continue; }
                s += setup.TrueSalience[i]; m += matchRow[i]; n++;
            }

            salience = n > 0 ? s / n : 0.0;
            match = n > 0 ? m / n : 0.0;
        }

        private static double[] Normalised(double[] shares)
        {
            double sum = 0.0;
            foreach (double s in shares) { sum += s; }
            if (sum <= 0.0) { throw new ArgumentException("prior shares sum to zero"); }
            var result = new double[shares.Length];
            for (int i = 0; i < result.Length; i++) { result[i] = shares[i] / sum; }
            return result;
        }

        private static string IssueSuffix(IssueId? issue) => issue.HasValue ? " / " + issue.Value : " / general";

        private static void Append(StringBuilder digest, int day, int party, CampaignActionKind kind, string target, double spend)
        {
            digest.Append(day).Append(':').Append(party).Append(':').Append((int)kind).Append(':')
                .Append(target).Append(':').Append(spend.ToString("F2", CultureInfo.InvariantCulture)).Append(';');
        }

        /// <summary>FNV-1a over the decision text, hex — small, stable, and independent of the platform's string hash.</summary>
        private static string Fnv1a64(string text)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= prime;
            }

            return hash.ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}
