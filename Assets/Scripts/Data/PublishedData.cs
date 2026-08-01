using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// Master Sequence step 9, Step A: which statistic a published figure describes.
    ///
    /// Deliberately NOT every field on <see cref="EconomyState"/>. Only stats with a REAL release rule in
    /// `POLISIM_SEED_DATA_MACRO_OVERHAUL.md` appear here - inventing a publication cadence for something
    /// like ConsumerConfidence would be the same fabrication the seed file's own `[GAP]` discipline
    /// forbids. Everything else keeps reading live in the UI until a real schedule is sourced.
    /// </summary>
    public enum PublishedStat
    {
        Unemployment,
        Inflation,
        Gdp,
        PovertyRate,
        Population,
        CrimeIndex
    }

    /// <summary>Where a figure sits in the revision cycle. Real agencies publish an early estimate and correct it later - BEA advance/second/third, Eurostat flash/final.</summary>
    public enum RevisionStatus
    {
        Preliminary,
        Revised,
        Final
    }

    /// <summary>
    /// One figure as the PLAYER saw it: what it measures, when it became visible, and how settled it is.
    ///
    /// ReferencePeriod and PublicationDate are stored separately rather than one derived from the other,
    /// because the gap between them IS the reporting lag - the thing this whole step exists to model, and
    /// what Step B's graphs draw as the distance between "period covered" and "release point".
    /// </summary>
    public class PublishedEntry
    {
        public System.DateTime ReferencePeriodStart;
        public System.DateTime ReferencePeriodEnd;
        public System.DateTime PublicationDate;
        public float Value;
        public RevisionStatus Status;
    }

    /// <summary>
    /// The append-only publication history of one statistic.
    ///
    /// A revision APPENDS a new entry sharing the earlier one's reference period; it never edits the
    /// existing entry. A player who acted on a preliminary figure must still be able to see the number
    /// they were actually looking at when they acted - overwriting it would erase the evidence of the
    /// decision they made, which is precisely the situation this mechanic exists to create.
    /// </summary>
    public class PublishedSeries
    {
        public readonly List<PublishedEntry> Entries = new List<PublishedEntry>();

        /// <summary>The most recently published figure, or null before this stat's first release. Callers must handle null rather than substituting a live value, or the reporting lag silently disappears at the start of every game.</summary>
        public PublishedEntry Latest()
        {
            return Entries.Count == 0 ? null : Entries[Entries.Count - 1];
        }

        /// <summary>The newest figure for a given reference period - i.e. the revised value once one exists, the preliminary until then. This, not Latest(), is what a graph plots against the period axis.</summary>
        public PublishedEntry LatestForPeriod(System.DateTime referencePeriodStart)
        {
            PublishedEntry newest = null;
            foreach (PublishedEntry entry in Entries)
            {
                if (entry.ReferencePeriodStart == referencePeriodStart
                    && (newest == null || entry.PublicationDate > newest.PublicationDate))
                {
                    newest = entry;
                }
            }

            return newest;
        }
    }

    /// <summary>
    /// Every published series for one country - the player-facing, lagged, sometimes-revised view of the
    /// economy.
    ///
    /// **This type must never be read by a simulation system.** The UI reads published values; Okun's Law,
    /// the Phillips Curve, the Fiscal Reaction Function, sector integration and everything else keep
    /// reading LIVE values off <see cref="EconomyState"/>. If a published figure reached a simulation
    /// input, the model would begin consuming its own stale output - a slow feedback corruption that, per
    /// the directive, "may not surface for hundreds of turns".
    ///
    /// That guarantee is structural rather than a matter of review discipline: this lives on `Country`
    /// beside `State`, never inside it, so the 55 simulation call sites that read `country.State.X` cannot
    /// reach a published value without someone adding a visible new reference. See
    /// `STEP_A_LIVE_VALUE_AUDIT.md` for the enumeration, and note the two cheap checks it defines - that
    /// `EconomyState.cs` stays unchanged, and that no file under `Assets/Scripts/Simulation/` ever
    /// mentions `Published`.
    /// </summary>
    public class PublishedData
    {
        public readonly Dictionary<PublishedStat, PublishedSeries> Series =
            new Dictionary<PublishedStat, PublishedSeries>();

        public PublishedSeries GetOrCreate(PublishedStat stat)
        {
            if (!Series.TryGetValue(stat, out PublishedSeries series))
            {
                series = new PublishedSeries();
                Series[stat] = series;
            }

            return series;
        }

        /// <summary>Convenience for the UI: the newest published figure for a stat, or null if it has never been published. Null is deliberately not papered over with a live value - see PublishedSeries.Latest.</summary>
        public PublishedEntry Latest(PublishedStat stat)
        {
            return Series.TryGetValue(stat, out PublishedSeries series) ? series.Latest() : null;
        }
    }
}
