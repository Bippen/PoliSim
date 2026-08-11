using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>Everything the vote model needs, as plain numbers - no <c>EconomyState</c>, no Unity types - so this whole file runs in the standalone harness and in a plain unit test.</summary>
    public struct VoteModelInputs
    {
        public string IncumbentPartyId;
        public double ApprovalRating;        // 0-100
        public double GdpGrowthPercent;      // annualised
        public double UnemploymentPercent;
        public double InflationPercent;
        public double YearsInOffice;
        public bool IsMidterm;
    }

    /// <summary>
    /// The hybrid vote model: a national swing decides party shares, and per-cohort turnout reweights
    /// them.
    ///
    /// <para>Elias chose this shape on 2026-08-11 over pure national swing and over a full demographic
    /// preference matrix. The reason it is worth the extra term is <b>campaigning</b>: with turnout as a
    /// separate, movable quantity, a ground game has something mechanical to act on (mobilise cohorts)
    /// that is distinct from what advertising acts on (move shares). Collapse turnout into a constant and
    /// half the campaign screen becomes decoration.</para>
    ///
    /// <para><b>The midterm effect needs TWO mechanisms, and this is worth stating because one of them
    /// looks sufficient and is not.</b> The demographic one is that low-salience elections have an older
    /// electorate, since the youngest cohort's turnout falls furthest. On its own that produces a
    /// PARTY-specific bias - with the seeded age gradient it helps Republicans and hurts Democrats
    /// regardless of who holds the White House - which is the wrong shape entirely. The historical
    /// regularity is that the president's party loses ground at midterms *whichever party it is*, so the
    /// second mechanism is thermostatic: the out-party's supporters turn out at a higher rate than the
    /// in-party's. Only the second gives the sign that matches the record.</para>
    /// </summary>
    public static class NationalVoteModel
    {
        /// <summary>Share points of two-party swing per point of approval above/below 50. MODELLED. At 35% approval - the old game-over line - this costs the incumbent ~4.5 points, which is a bad night rather than annihilation, and that is the intended feel.</summary>
        public const double ApprovalWeight = 0.003;

        /// <summary>Share points per point of annualised GDP growth above trend. MODELLED, in the spirit of the Kramer/Fair economic-voting relations rather than fitted to them.</summary>
        public const double GrowthWeight = 0.010;

        /// <summary>Assumed trend growth, percent. Growth above this rewards the incumbent, below it punishes.</summary>
        public const double TrendGrowthPercent = 2.0;

        /// <summary>Share points per point of unemployment above the reference rate.</summary>
        public const double UnemploymentWeight = -0.008;
        public const double ReferenceUnemploymentPercent = 5.0;

        /// <summary>Share points per point of inflation away from target, in EITHER direction - deflation is not a gift to an incumbent. Applied to the absolute deviation for that reason.</summary>
        public const double InflationWeight = -0.006;
        public const double TargetInflationPercent = 2.0;

        /// <summary>Cost of ruling: share points per year in office. MODELLED, and mild - it exists so that a long, competent incumbency still faces a rising headwind, which is the well-attested pattern.</summary>
        public const double TimeInOfficeWeight = -0.004;

        /// <summary>
        /// The thermostatic midterm gap: the out-party's cohort turnout is multiplied by this and the
        /// in-party's divided by it. MODELLED at 1.06, which produces a few points of effective-turnout
        /// advantage - enough to matter in a close chamber, not enough to decide a landslide on its own.
        /// </summary>
        public const double MidtermEnthusiasmGap = 1.06;

        /// <summary>Floor and ceiling on any party's final share. Nothing here should be able to zero a major party or hand it everything; a model that can is one bad input away from a nonsense chamber.</summary>
        public const double MinShare = 0.001;
        public const double MaxShare = 0.95;

        /// <summary>
        /// Projects vote shares for an election.
        /// </summary>
        /// <param name="parties">The country's parties, carrying their baseline shares and cohort appeal.</param>
        /// <param name="cohorts">The electorate. <see cref="ElectorateCohort.ResetForElection"/> is called here, so any campaign mobilisation must be applied AFTER this returns.</param>
        /// <returns>Party id to vote share, summing to 1.</returns>
        public static Dictionary<string, double> Project(
            IReadOnlyList<PoliticalParty> parties,
            IReadOnlyList<ElectorateCohort> cohorts,
            VoteModelInputs inputs)
        {
            if (parties == null) throw new ArgumentNullException(nameof(parties));
            if (cohorts == null) throw new ArgumentNullException(nameof(cohorts));

            double swing = IncumbentSwing(inputs);

            // Step 1 - the swing term. The incumbent party gains `swing`; everyone else shares the
            // opposite in proportion to their own baseline, so a three-point incumbent loss does not
            // land entirely on the largest opponent just because it is largest.
            var raw = new Dictionary<string, double>(parties.Count);
            double opposition = 0.0;
            foreach (PoliticalParty p in parties)
            {
                if (p.Id != inputs.IncumbentPartyId)
                {
                    opposition += p.BaselineVoteShare;
                }
            }

            foreach (PoliticalParty p in parties)
            {
                double share = p.Id == inputs.IncumbentPartyId
                    ? p.BaselineVoteShare + swing
                    : p.BaselineVoteShare - swing * (opposition > 0.0 ? p.BaselineVoteShare / opposition : 0.0);

                raw[p.Id] = Math.Max(MinShare, Math.Min(MaxShare, share));
            }

            // Step 2 - turnout. Each cohort votes at its own rate, and each party's pull within a cohort
            // is its share scaled by its appeal there. A party strong among the young loses ground when
            // the young stay home, which is the entire point of carrying cohorts at all.
            foreach (ElectorateCohort c in cohorts)
            {
                c.ResetForElection(!inputs.IsMidterm);
            }

            // ⚠ COHORT TURNOUT IS APPLIED AS A DEVIATION FROM THE BASELINE PATTERN, NOT AS AN ABSOLUTE
            // REWEIGHTING, and the difference is the whole correctness of this step. A baseline share is
            // a count of votes ACTUALLY CAST at a real election, so it already embeds that election's
            // turnout pattern. Reweighting it again double-counts: measured 2026-08-11, a neutral
            // election returned the Republicans 52.1% against a seeded 49.75%, purely because older
            // cohorts vote more and lean right and the model applied that twice.
            //
            // So each party's weight is divided by the same weight computed at the HIGH-SALIENCE
            // turnout - the pattern the baseline was drawn from. At a presidential election with no
            // campaign effects the ratio is exactly 1 and the projection returns the baseline unchanged,
            // which is the identity a vote model should obviously satisfy and this one did not.
            var weighted = new Dictionary<string, double>(parties.Count);
            var reference = new Dictionary<string, double>(parties.Count);
            foreach (PoliticalParty p in parties)
            {
                weighted[p.Id] = 0.0;
                reference[p.Id] = 0.0;
            }

            for (int i = 0; i < cohorts.Count; i++)
            {
                ElectorateCohort cohort = cohorts[i];
                double cohortWeight = cohort.EffectiveWeight;
                double referenceWeight = cohort.ShareOfElectorate * cohort.HighSalienceTurnout;

                foreach (PoliticalParty p in parties)
                {
                    double baseAppeal = p.CohortAppeal != null && i < p.CohortAppeal.Length ? p.CohortAppeal[i] : 1.0;
                    double appeal = baseAppeal;

                    // The thermostatic term, applied to turnout rather than to preference: at a midterm
                    // the out-party's supporters show up and the in-party's do not. Symmetric, so it
                    // cannot inflate the total electorate. Absent from the reference on purpose - it is
                    // exactly the deviation being measured.
                    if (inputs.IsMidterm)
                    {
                        appeal *= p.Id == inputs.IncumbentPartyId
                            ? 1.0 / MidtermEnthusiasmGap
                            : MidtermEnthusiasmGap;
                    }

                    weighted[p.Id] += appeal * cohortWeight;
                    reference[p.Id] += baseAppeal * referenceWeight;
                }
            }

            var projected = new Dictionary<string, double>(parties.Count);
            foreach (PoliticalParty p in parties)
            {
                double ratio = reference[p.Id] > 0.0 ? weighted[p.Id] / reference[p.Id] : 1.0;
                projected[p.Id] = raw[p.Id] * ratio;
            }

            return Normalise(projected);
        }

        /// <summary>
        /// The incumbent party's swing in share points. Separated from <see cref="Project"/> so it can be
        /// inspected and tested on its own - every term in it is a modelled coefficient, and a model
        /// whose terms cannot be examined one at a time is one nobody can calibrate.
        /// </summary>
        public static double IncumbentSwing(VoteModelInputs inputs)
        {
            double swing = 0.0;
            swing += (inputs.ApprovalRating - 50.0) * ApprovalWeight;
            swing += (inputs.GdpGrowthPercent - TrendGrowthPercent) * GrowthWeight;
            swing += (inputs.UnemploymentPercent - ReferenceUnemploymentPercent) * UnemploymentWeight;
            swing += Math.Abs(inputs.InflationPercent - TargetInflationPercent) * InflationWeight;
            swing += inputs.YearsInOffice * TimeInOfficeWeight;
            return swing;
        }

        private static Dictionary<string, double> Normalise(Dictionary<string, double> shares)
        {
            double total = 0.0;
            foreach (double v in shares.Values)
            {
                total += v;
            }

            if (total <= 0.0)
            {
                return shares;
            }

            var result = new Dictionary<string, double>(shares.Count);
            foreach (KeyValuePair<string, double> kv in shares)
            {
                result[kv.Key] = kv.Value / total;
            }

            return result;
        }
    }
}
