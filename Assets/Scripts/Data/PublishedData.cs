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
        CrimeIndex,

        /// <summary>ROUND 4 BATCH 1: publishes with the monthly LFS family - Eurostat releases it in
        /// the same monthly series as headline unemployment (`une_rt_m`), the BLS in the same
        /// Employment Situation. The scoping brief called both C3 stats annual-class; the SOURCE says
        /// monthly for this one, and the source wins (flagged, not absorbed).</summary>
        YouthUnemployment,

        /// <summary>ROUND 4 BATCH 1: annual-class, like Population - Eurostat `demo_mlexpec` and
        /// CDC/NCHS both publish yearly.</summary>
        LifeExpectancy,

        /// <summary>ROUND 4 BATCH 2: annual-class BY THE SAME-RELEASE ARGUMENT youth unemployment
        /// used for monthly - Gini is literally the same survey release as the poverty rate at both
        /// sources (Eurostat EU-SILC `ilc_di12`; the US Census Income &amp; Poverty report carries
        /// both figures), so it publishes with the annual family rather than on an invented
        /// schedule.</summary>
        Gini,

        /// <summary>ROUND 4 BATCH 2: monthly for the USA BY SOURCE - BLS "Real Earnings" is
        /// released WITH CPI, same day (the seed doc's §5 records the 2025-01-15 release, which is
        /// CPI day) - so it rides the recorded CPI rule. The EU five have no wage release rule in
        /// the seed file and follow the same cadence rather than inventing one, the exact
        /// unemployment-family precedent.</summary>
        RealWageIndex
    }

    /// <summary>
    /// Which statistic a recorded PERIOD CLOSING belongs to - a strict superset of
    /// <see cref="PublishedStat"/>.
    ///
    /// **Why this is a separate enum rather than extra members on `PublishedStat`.** That enum makes a
    /// deliberate correctness claim - only stats with a REAL sourced release rule appear in it, because
    /// inventing a publication cadence would be fabrication. `DebtToGdpRatio` has no release rule and is
    /// never published; it is recorded only so derived consumers can read a SETTLED value for a closed
    /// period instead of an instantaneous one. Adding it to `PublishedStat` would quietly contradict that
    /// claim and make `Latest(DebtToGdpRatio)` a permanent null trap.
    ///
    /// **The first six members mirror `PublishedStat` in the same order** - see
    /// <see cref="ClosingStatExtensions.ToClosingStat"/>, which maps between them explicitly and throws
    /// on an unmapped member, so drift surfaces immediately rather than silently mis-keying a series.
    /// </summary>
    public enum ClosingStat
    {
        Unemployment,
        Inflation,
        Gdp,
        PovertyRate,
        Population,
        CrimeIndex,

        /// <summary>
        /// Recorded, never published. Added 2026-08-02 for Step C4's scheduled rating review, which reads
        /// a settled annual fiscal position rather than one turn's state. A STOCK, so its closing value
        /// is the settled position directly - no averaging needed or wanted.
        /// </summary>
        DebtToGdpRatio,

        // ROUND 4 BATCH 1: the two C3 stats. ⚠ The "first six mirror PublishedStat in the same
        // order" note above now reads "the first six, then DebtToGdpRatio, then these two" - the
        // mapping was always the explicit throw-on-unmapped switch below, never positional, so the
        // append is safe; the enum stays append-only per the save format's standing rule.
        YouthUnemployment,
        LifeExpectancy,

        // ROUND 4 BATCH 2: the two C2 stats, appended per the same standing rule.
        Gini,
        RealWageIndex
    }

    public static class ClosingStatExtensions
    {
        /// <summary>Explicit, exhaustive map. Throws rather than defaulting, so adding a `PublishedStat` member without extending `ClosingStat` fails loudly on the first recording pass instead of silently writing every new stat under one wrong key.</summary>
        public static ClosingStat ToClosingStat(this PublishedStat stat)
        {
            switch (stat)
            {
                case PublishedStat.Unemployment: return ClosingStat.Unemployment;
                case PublishedStat.Inflation: return ClosingStat.Inflation;
                case PublishedStat.Gdp: return ClosingStat.Gdp;
                case PublishedStat.PovertyRate: return ClosingStat.PovertyRate;
                case PublishedStat.Population: return ClosingStat.Population;
                case PublishedStat.CrimeIndex: return ClosingStat.CrimeIndex;
                case PublishedStat.YouthUnemployment: return ClosingStat.YouthUnemployment;
                case PublishedStat.LifeExpectancy: return ClosingStat.LifeExpectancy;
                case PublishedStat.Gini: return ClosingStat.Gini;
                case PublishedStat.RealWageIndex: return ClosingStat.RealWageIndex;
                default: throw new System.ArgumentOutOfRangeException(nameof(stat), stat, "PublishedStat has no ClosingStat counterpart - add one.");
            }
        }
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
    /// `COMPLETED.md` section 12 for the enumeration, and note the two cheap checks it defines - that
    /// `EconomyState.cs` stays unchanged, and that no file under `Assets/Scripts/Simulation/` ever
    /// mentions `Published`.
    /// </summary>
    public class PublishedData
    {
        public readonly Dictionary<PublishedStat, PublishedSeries> Series =
            new Dictionary<PublishedStat, PublishedSeries>();

        /// <summary>
        /// The TRUE value each reference period closed at, keyed by (stat, period start). Recorded as
        /// the period elapses, not looked up afterwards - StatHistory keeps bare `List&lt;float&gt;` with
        /// no dates attached, so there is no way to ask it "what was GDP in Q1".
        ///
        /// This is what a revision converges toward. Without it, a revision published in June resolved to
        /// JUNE's value rather than to the Q1 value it claims to describe - and because GDP only changes
        /// at a turn boundary (once per 121 days), publications for genuinely different quarters could
        /// land inside the same turn and report an identical figure.
        ///
        /// Not itself published, and never read by the simulation: this is the underlying truth the
        /// published series is a lagged, noisy view OF.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public readonly Dictionary<(ClosingStat Stat, System.DateTime PeriodStart), float> PeriodClosingValues =
            new Dictionary<(ClosingStat, System.DateTime), float>();

        /// <summary>One recorded period closing, in the flat shape the save file carries - see
        /// <see cref="SerializedPeriodClosings"/> for why the dictionary above cannot go to JSON
        /// directly.</summary>
        public class PeriodClosing
        {
            public ClosingStat Stat;
            public System.DateTime PeriodStart;
            public float Value;
        }

        // ⚠ SAVE/LOAD (item 8, hazard 5): a JSON object key must be a string, and a ValueTuple key
        // stringifies via ToString into something nothing can parse back - so the dictionary above
        // is [JsonIgnore]d and THIS private surrogate is what the save file carries, as a flat list.
        // A settable private property with [JsonProperty] is used instead of a JsonConverter on the
        // readonly field deliberately: Json.NET's converter-on-unwritable-member path is murky
        // enough that the mechanism report chose the boring shape that cannot be misread.
        //
        // ⚠ ObjectCreationHandling.Replace is LOAD-BEARING, proven by the round-trip diagnostic's
        // first run (289 closings -> 0 on every country, every scenario) and then isolated against
        // this exact Newtonsoft DLL: under the default Auto handling Json.NET treats a readable
        // collection member as populate-in-place - it calls THIS GETTER during deserialization,
        // populates the temporary list the getter returns, and never calls the setter, so the
        // incoming data is discarded into a throwaway. Replace forces "build the list, call the
        // setter", which is the entire point of a surrogate. (The payload name differing from the
        // ignored field's name is cosmetic by comparison - kept because two members claiming one
        // name is asking Json.NET to pick.)
        [Newtonsoft.Json.JsonProperty("PeriodClosings", ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace)]
        private List<PeriodClosing> SerializedPeriodClosings
        {
            get
            {
                var closings = new List<PeriodClosing>(PeriodClosingValues.Count);
                foreach (KeyValuePair<(ClosingStat Stat, System.DateTime PeriodStart), float> pair in PeriodClosingValues)
                {
                    closings.Add(new PeriodClosing { Stat = pair.Key.Stat, PeriodStart = pair.Key.PeriodStart, Value = pair.Value });
                }

                return closings;
            }
            set
            {
                PeriodClosingValues.Clear();
                if (value == null)
                {
                    return;
                }

                foreach (PeriodClosing closing in value)
                {
                    PeriodClosingValues[(closing.Stat, closing.PeriodStart)] = closing.Value;
                }
            }
        }

        /// <summary>
        /// The settled value this stat closed the period at, or null if that period was never recorded -
        /// which is the honest answer before the game has run long enough, and must not be papered over
        /// with a live reading. Step C4's annual rating review depends on that distinction: a missing
        /// year-ago figure means "cannot compute a deficit yet", not "the deficit was zero".
        /// </summary>
        public float? ClosingValue(ClosingStat stat, System.DateTime periodStart)
        {
            return PeriodClosingValues.TryGetValue((stat, periodStart), out float value) ? value : (float?)null;
        }

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
