using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Master Sequence step 9, Step A: publishes statistics on their real release schedules, with
    /// preliminary figures later revised.
    ///
    /// **This system WRITES to Country.Published and READS from Country.State. It must never do the
    /// reverse.** The published series is the player-facing, lagged, sometimes-revised view; every
    /// simulation system keeps reading live values off EconomyState. A published value reaching a
    /// simulation input would make the model consume its own stale output - a slow feedback corruption
    /// that per the directive "may not surface for hundreds of turns". See COMPLETED.md section 12.md.
    ///
    /// **This system must not change any simulation number.** It writes only to Published, and its noise
    /// draws from SimulationRandom's own PublicationRevision stream so publishing cannot perturb the draw
    /// sequence of events, SWF returns, Fed chair candidates, cabinet decisions or parliament jitter - see
    /// CLAUDE.md's "Shared RNG structure can invalidate a validation method" for why that isolation is
    /// load-bearing rather than tidiness.
    /// </summary>
    public static class PublicationSystem
    {
        /// <summary>
        /// How far a preliminary estimate can sit from the true value, as a fraction of that value.
        /// Small on purpose: real preliminary-to-final revisions are corrections, not surprises, and the
        /// directive asks for revisions that are "small and plausible, not arbitrary".
        /// </summary>
        private const double PreliminaryNoiseFraction = 0.015;

        private static System.Random RandomSource => SimulationRandom.For(SimulationRandom.Stream.PublicationRevision);

        /// <summary>
        /// Seeds the ONE inherited quarter a new government takes office with, so the published record
        /// does not begin the day the player does.
        ///
        /// Without it, suppressing pre-epoch releases leaves the GDP graph empty until roughly day 120 -
        /// the first full quarter ends 31 March and its advance estimate lands ~30 April - which hides
        /// the reporting lag for a third of the first year, exactly when a player is forming their model
        /// of how the game works.
        ///
        /// This invents nothing. Every country is already seeded with a real sourced starting GDP, and
        /// that is precisely the figure the outgoing administration would have published for the quarter
        /// before taking office. Exactly ONE quarter is seeded: a second would require inventing a value,
        /// since only the starting figure is sourced. Marked Final, because an inherited historical
        /// figure is settled rather than awaiting revision.
        /// </summary>
        public static void SeedInheritedHistory(Country country)
        {
            PublishedSeries series = country.Published.GetOrCreate(PublishedStat.Gdp);
            if (series.Entries.Count > 0)
            {
                return;
            }

            System.DateTime periodEnd = SimulationManager.EpochDate.AddDays(-1);
            series.Entries.Add(new PublishedEntry
            {
                ReferencePeriodStart = periodEnd.AddMonths(-3).AddDays(1),
                ReferencePeriodEnd = periodEnd,
                PublicationDate = SimulationManager.EpochDate,
                Value = country.State.GDP,
                Status = RevisionStatus.Final
            });
        }

        /// <summary>
        /// Called once per simulated day, per country, AFTER the day's simulation has run - so a figure
        /// published today reflects state as of today, not a half-updated intermediate.
        /// </summary>
        public static void PublishDueFigures(Country country, System.DateTime date)
        {
            // Record what the CURRENT period is running at, every day. Overwriting daily means that once
            // a period closes, the stored figure is its value on its final day - which is the value every
            // publication describing that period must converge to. Doing it this way needs no historical
            // lookup, which StatHistory cannot provide (bare float lists, no dates attached).
            //
            // Iterates ClosingStat, not PublishedStat, because DebtToGdpRatio is recorded but never
            // published - Step C4's annual rating review reads a settled debt stock rather than an
            // instantaneous one. Recording still only ever WRITES to Published, so this cannot move a
            // simulation number any more than the rest of this file can.
            foreach (ClosingStat closing in System.Enum.GetValues(typeof(ClosingStat)))
            {
                country.Published.PeriodClosingValues[(closing, ReleaseCalendar.GetCurrentPeriodStart(closing, date))] = ReadClosingValue(country, closing);
            }

            foreach (PublishedStat stat in System.Enum.GetValues(typeof(PublishedStat)))
            {
                if (!ReleaseCalendar.IsReleaseDay(country, stat, date))
                {
                    continue;
                }

                ReleaseCalendar.GetReferencePeriod(stat, date, out System.DateTime periodStart, out System.DateTime periodEnd);

                // Suppress releases for periods that predate the game. The schedule is real - a t+30
                // GDP estimate genuinely fires 30 days into a new game - but its reference quarter ended
                // before the simulation began, so there is no data behind it. Publishing anyway stamped
                // a pre-epoch period with today's live value, which put dates on the graph's axis for
                // quarters the simulation never ran and inverted the apparent trend.
                //
                // The one legitimate pre-epoch figure is the INHERITED quarter seeded by
                // SeedInheritedHistory, which carries the country's real sourced starting value rather
                // than a present-day reading pretending to be historical.
                if (periodStart < SimulationManager.EpochDate)
                {
                    continue;
                }
                // The true value FOR THE REFERENCE PERIOD, not the value as of today. This was the bug:
                // reading live here meant a June-published revision of Q1 converged on June's figure, so
                // Q1 and Q2 revisions could report the same number to four significant figures whenever
                // both publications fell inside one 121-day turn.
                //
                // Falls back to live only when no closing value was recorded - which can only happen for
                // a period that elapsed before this system began recording, i.e. the inherited quarter.
                if (!country.Published.PeriodClosingValues.TryGetValue((stat.ToClosingStat(), periodStart), out float trueValue))
                {
                    trueValue = ReadLiveValue(country, stat);
                }
                RevisionStatus status = GetStatusFor(country, stat, date);

                // The preliminary is a NOISY ESTIMATE OF the true value, not an independent random
                // number, and a later revision converges on the truth. That is what real revisions do,
                // and it is what makes acting on a preliminary figure a fair risk rather than a coin
                // flip - the player is reading a genuine early read of reality, not a fiction.
                float published = status == RevisionStatus.Preliminary
                    ? trueValue * (float)(1.0 + (RandomSource.NextDouble() * 2.0 - 1.0) * PreliminaryNoiseFraction)
                    : trueValue;

                country.Published.GetOrCreate(stat).Entries.Add(new PublishedEntry
                {
                    ReferencePeriodStart = periodStart,
                    ReferencePeriodEnd = periodEnd,
                    PublicationDate = date,
                    Value = published,
                    Status = status
                });
            }
        }

        /// <summary>
        /// Which revision stage today's release represents. GDP is the real multi-stage case - the USA
        /// publishes three estimates per reference quarter and the EU five two - so its stage comes from
        /// the calendar. Everything else publishes once per reference period and is Final immediately;
        /// inventing a revision cycle for stats that do not have one would be fabrication.
        /// </summary>
        private static RevisionStatus GetStatusFor(Country country, PublishedStat stat, System.DateTime date)
        {
            if (stat != PublishedStat.Gdp)
            {
                return RevisionStatus.Final;
            }

            switch (ReleaseCalendar.GetGdpRevisionStage(country, date))
            {
                case ReleaseCalendar.GdpStage.Advance: return RevisionStatus.Preliminary;
                case ReleaseCalendar.GdpStage.Second: return RevisionStatus.Revised;
                default: return RevisionStatus.Final;
            }
        }

        /// <summary>
        /// Reads the LIVE value being published. One-directional by construction: this system reads
        /// EconomyState and writes Published, never the reverse.
        /// </summary>
        private static float ReadLiveValue(Country country, PublishedStat stat)
        {
            return ReadClosingValue(country, stat.ToClosingStat());
        }

        /// <summary>
        /// Reads the LIVE value being recorded or published. One-directional by construction: this system
        /// reads EconomyState and writes Published, never the reverse.
        /// </summary>
        private static float ReadClosingValue(Country country, ClosingStat stat)
        {
            EconomyState state = country.State;
            switch (stat)
            {
                case ClosingStat.Unemployment: return state.Unemployment;
                case ClosingStat.Inflation: return state.Inflation;
                case ClosingStat.Gdp: return state.GDP;
                case ClosingStat.PovertyRate: return state.PovertyRate;
                case ClosingStat.Population: return state.Population;
                case ClosingStat.CrimeIndex: return state.CrimeIndex;
                case ClosingStat.DebtToGdpRatio: return state.DebtToGdpRatio;
                default: return 0f;
            }
        }
    }
}
