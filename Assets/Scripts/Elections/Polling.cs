using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-B10 / SPEC §20–§22 — polls as **imperfect information**, and momentum as a decaying
    /// moving average. PURE FUNCTIONS AND VALUES, WIRED TO NOTHING (R-N2).
    ///
    /// **The structural rule this file exists to enforce: the UI never sees the truth.** A
    /// <see cref="Poll"/> carries sampled shares, a sample size, a margin of error, a field date and
    /// the house that ran it — and **no reference to the true preference vector**. Everything the
    /// player is shown about where the race stands must come from a Poll, so "what do I actually
    /// know?" is a real question with a purchasable answer (§21, §36).
    ///
    /// **Three distinct sources of poll error, kept separate because they behave differently:**
    /// 1. **Sampling error** — random, shrinks with √n, and is what the margin of error describes.
    ///    A poll's MoE is *honest about this and only this*, which is exactly the real-world
    ///    situation and the reason published MoEs understate true poll error.
    /// 2. **House effect** — a systematic, per-house, per-party lean that does NOT shrink with
    ///    sample size. A bigger sample buys precision, never freedom from a house's method.
    /// 3. **Turnout error** — the gap between who answers a pollster and who actually votes;
    ///    modelled at election time by §26, not here, and named so its absence is deliberate.
    ///
    /// **[AUTHORED-DRAFT]** the house roster and their leans; `MomentumHalfLifeDays = 10` (§22's
    /// worked example — a strong debate worth +2.0 decays to ~+1.4 after several days and to
    /// ~+0.4 after two weeks — which a 10-day half-life reproduces closely).
    /// </summary>
    public readonly struct Poll
    {
        public readonly DateTime FieldDate;
        public readonly int SampleSize;
        public readonly string House;
        private readonly double[] _shares;
        private readonly double[] _marginOfErrorPp;

        public Poll(DateTime fieldDate, int sampleSize, string house, double[] shares, double[] marginOfErrorPp)
        {
            FieldDate = fieldDate;
            SampleSize = sampleSize;
            House = house;
            _shares = shares;
            _marginOfErrorPp = marginOfErrorPp;
        }

        public int PartyCount => _shares.Length;

        /// <summary>The measured share for a party, 0–1. This is the ONLY share a UI may read.</summary>
        public double Share(int partyIndex) => _shares[partyIndex];

        /// <summary>The ±, in percentage points, for that party's share at 95 % confidence — sampling error only.</summary>
        public double MarginOfErrorPp(int partyIndex) => _marginOfErrorPp[partyIndex];

        /// <summary>A copy of the measured shares — still no truth anywhere.</summary>
        public double[] Shares()
        {
            var copy = new double[_shares.Length];
            Array.Copy(_shares, copy, _shares.Length);
            return copy;
        }

        public bool Covers(int partyIndex, double trueShare)
        {
            double half = _marginOfErrorPp[partyIndex] / 100.0;
            return Math.Abs(_shares[partyIndex] - trueShare) <= half;
        }
    }

    /// <summary>A pollster: its sample size, cost, and its systematic lean per party. [AUTHORED-DRAFT] throughout.</summary>
    public readonly struct PollingHouse
    {
        public readonly string Name;
        public readonly int SampleSize;
        public readonly double Cost;
        public readonly double[] HouseEffectPp;
        public readonly bool IsInternal;

        public PollingHouse(string name, int sampleSize, double cost, double[] houseEffectPp, bool isInternal = false)
        {
            Name = name; SampleSize = sampleSize; Cost = cost; HouseEffectPp = houseEffectPp; IsInternal = isInternal;
        }
    }

    public static class PollingSystem
    {
        /// <summary>1.96 standard errors — the 95 % convention every published poll uses.</summary>
        public const double ConfidenceZ = 1.96;

        /// <summary>
        /// §22's decay: a shock's half-life in days. **[AUTHORED-DRAFT] = 7.0, and the choice has a
        /// finding attached.**
        ///
        /// §22's worked example gives three points for a +2.0 shock: ~1.4 after "several days",
        /// ~0.4 after two weeks, ~0.0 after a month. **Those three are not consistent with any
        /// single exponential half-life** — they imply ~9.7 days, ~6.0 days and ~5.3 days
        /// respectively, i.e. the spec describes decay that *accelerates*, which is plausible for
        /// how news fades but is not what "decay naturally" usually means.
        ///
        /// Rather than invent a bespoke accelerating curve to hit three illustrative numbers, this
        /// keeps a plain exponential at the best-compromise half-life of 7 days (+2.0 → 1.30 at
        /// five days → 0.50 at two weeks → 0.10 at a month) and asserts the SHAPE the spec actually
        /// requires — monotone decay, substantially gone within a month — instead of pretending to
        /// reproduce points that contradict one another. If play shows the tail is too fat, the
        /// honest upgrade is a named second mechanism (a news-cycle half-life distinct from a
        /// reputation one), not a fudged exponent.
        /// </summary>
        public const double MomentumHalfLifeDays = 7.0;

        /// <summary>The margin of error for an observed proportion at 95 % confidence, in percentage points.</summary>
        public static double MarginOfErrorPp(double share, int sampleSize)
        {
            if (sampleSize <= 0) { throw new ArgumentException("sample size must be positive"); }

            double p = share < 0 ? 0 : (share > 1 ? 1 : share);
            return 100.0 * ConfidenceZ * Math.Sqrt(p * (1.0 - p) / sampleSize);
        }

        /// <summary>
        /// Runs a poll against the true preference vector. **This is the only function in the
        /// system that touches truth**, and it returns a <see cref="Poll"/> — which cannot carry it.
        ///
        /// Sampling is a genuine multinomial draw (one respondent at a time), so the error has the
        /// right distribution rather than a Gaussian approximation bolted on; the house effect is
        /// then added as a systematic lean that no sample size can wash out.
        /// </summary>
        public static Poll Conduct(double[] truth, PollingHouse house, DateTime fieldDate, System.Random random)
        {
            if (truth == null || truth.Length == 0) { throw new ArgumentException("no parties"); }
            if (random == null) { throw new ArgumentNullException(nameof(random)); }

            var counts = new int[truth.Length];
            var cumulative = new double[truth.Length];
            double running = 0.0;
            for (int i = 0; i < truth.Length; i++)
            {
                running += truth[i];
                cumulative[i] = running;
            }

            for (int r = 0; r < house.SampleSize; r++)
            {
                double u = random.NextDouble() * running;
                for (int i = 0; i < truth.Length; i++)
                {
                    if (u <= cumulative[i]) { counts[i]++; break; }
                }
            }

            var shares = new double[truth.Length];
            var moe = new double[truth.Length];
            double total = 0.0;
            for (int i = 0; i < truth.Length; i++)
            {
                double sampled = (double)counts[i] / house.SampleSize;
                double lean = house.HouseEffectPp != null && i < house.HouseEffectPp.Length
                    ? house.HouseEffectPp[i] / 100.0
                    : 0.0;
                shares[i] = Math.Max(0.0, sampled + lean);
                total += shares[i];
            }

            for (int i = 0; i < truth.Length; i++)
            {
                if (total > 0) { shares[i] /= total; }
                moe[i] = MarginOfErrorPp(shares[i], house.SampleSize);
            }

            return new Poll(fieldDate, house.SampleSize, house.Name, shares, moe);
        }

        /// <summary>§22's decay factor over a number of days.</summary>
        public static double MomentumDecay(double days, double halfLifeDays = MomentumHalfLifeDays)
        {
            if (days <= 0) { return 1.0; }
            return Math.Pow(0.5, days / halfLifeDays);
        }
    }

    /// <summary>
    /// §22 — momentum as a decaying stock, not a permanent gain. A shock adds to it; time removes
    /// a fixed fraction. The spec is explicit that an event's boost must fade "unless the event
    /// creates a lasting reputation change", so this type deliberately has no way to make a shock
    /// permanent: durable change belongs in reputation (§38), which is a different stock.
    /// </summary>
    public sealed class MomentumTracker
    {
        private readonly double[] _momentumPp;

        public MomentumTracker(int partyCount)
        {
            _momentumPp = new double[partyCount];
        }

        public double MomentumPp(int party) => _momentumPp[party];

        public void AddShock(int party, double pp) => _momentumPp[party] += pp;

        /// <summary>Advances by <paramref name="days"/>, decaying every party's momentum on §22's half-life.</summary>
        public void Advance(double days, double halfLifeDays = PollingSystem.MomentumHalfLifeDays)
        {
            double factor = PollingSystem.MomentumDecay(days, halfLifeDays);
            for (int i = 0; i < _momentumPp.Length; i++) { _momentumPp[i] *= factor; }
        }

        /// <summary>
        /// §22's blend: underlying support plus current momentum, renormalised. Momentum shifts
        /// where a race APPEARS to be without changing the underlying preference that produced it —
        /// which is why a poll can move before anything real has.
        /// </summary>
        public double[] Apply(double[] underlying)
        {
            var result = new double[underlying.Length];
            double total = 0.0;
            for (int i = 0; i < underlying.Length; i++)
            {
                result[i] = Math.Max(0.0, underlying[i] + _momentumPp[i] / 100.0);
                total += result[i];
            }

            if (total <= 0) { return underlying; }

            for (int i = 0; i < result.Length; i++) { result[i] /= total; }
            return result;
        }
    }
}
