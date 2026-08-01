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
    /// that per the directive "may not surface for hundreds of turns". See STEP_A_LIVE_VALUE_AUDIT.md.
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
        /// Called once per simulated day, per country, AFTER the day's simulation has run - so a figure
        /// published today reflects state as of today, not a half-updated intermediate.
        /// </summary>
        public static void PublishDueFigures(Country country, System.DateTime date)
        {
            foreach (PublishedStat stat in System.Enum.GetValues(typeof(PublishedStat)))
            {
                if (!ReleaseCalendar.IsReleaseDay(country, stat, date))
                {
                    continue;
                }

                ReleaseCalendar.GetReferencePeriod(stat, date, out System.DateTime periodStart, out System.DateTime periodEnd);
                float trueValue = ReadLiveValue(country, stat);
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
            EconomyState state = country.State;
            switch (stat)
            {
                case PublishedStat.Unemployment: return state.Unemployment;
                case PublishedStat.Inflation: return state.Inflation;
                case PublishedStat.Gdp: return state.GDP;
                case PublishedStat.PovertyRate: return state.PovertyRate;
                case PublishedStat.Population: return state.Population;
                case PublishedStat.CrimeIndex: return state.CrimeIndex;
                default: return 0f;
            }
        }
    }
}
