using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// C-D1 — **voter groups as a VIEW over the cohort substrate, never a parallel population.**
    ///
    /// <para><b>The standing gap this closes, and the order that unblocked it.</b> The live election path
    /// runs ONE group, "the electorate", at a uniform loyalty — the ceiling C-A1 named for the Italy FdI
    /// surge. C-D1 was closed AS BILLED because the blocker turned out not to be the data but the
    /// **order**: the cohort spec-let §5 rules voter groups a view with COMPUTED shares *"precisely so
    /// the game never carries two populations"*, so sourcing per-<i>valkrets</i> marginals onto a new
    /// group layer would have built the second population that spec-let forbids. P-I2's substrate has now
    /// landed, so the view can be built on it instead.</para>
    ///
    /// <para><b>The join, exactly as §5 states it.</b> A group is a predicate over cohorts; its
    /// `PopulationShare` is **computed from the cohorts it covers**, never seeded independently. ⚠ **The
    /// eligible population is NOT the population** — voting age cuts the cohort total, and a share taken
    /// of the country rather than of the electorate inflates every young group. Shares here are of the
    /// **electorate**.</para>
    ///
    /// <para>⚠ <b>What this deliberately does NOT do: it does not carry per-group LOYALTY.</b> That is
    /// C-A1's named ceiling and it needs vote shares BY AGE GROUP — survey data, per country, per
    /// election, which no source consulted at C-D1 or here publishes for Italy 2022. Building the view
    /// without it is the honest half: the substrate the loyalty would hang on now exists, and the figure
    /// it needs is billed rather than invented. **A per-group loyalty guessed from a national one would
    /// reproduce the uniform 60 this whole chain exists to replace.**</para>
    ///
    /// <para>⚠ <b>Turnout is SOURCED for Sweden and ABSENT elsewhere, and absent means absent.</b>
    /// `TurnoutByAge` carries SCB's *valdeltagandeundersökning* — Riksdag election, voting rates among all
    /// those entitled to vote, both sexes, table `ME0105C/ME0105T01`. **Vintage 2014, which is the most
    /// recent the survey published**: SCB's table ends at 2014 and no later wave exists in it. That is
    /// stated at the call site rather than dressed as current. No other country's turnout by age was
    /// found, so `For` returns groups with a NaN turnout there — never a Swedish figure worn by another
    /// electorate.</para>
    /// </summary>
    public static class CohortVoterGroups
    {
        /// <summary>SOURCED, per constitution: the age at which a citizen may vote in national elections.
        /// 18 in all six — Sweden RF 3 kap. 4 §, Germany GG Art. 38(2), France, Italy (since the 2021
        /// constitutional law lowered the Senate's electorate to 18), Poland, and the USA (Amendment
        /// XXVI). ⚠ Enumerated per country rather than assumed to be universal, because it is a
        /// constitutional fact and the day one of them changes, this is where it changes.</summary>
        public static int VotingAge(CountryId country) => 18;

        /// <summary>
        /// SOURCED — Sweden only. SCB, *Valdeltagandeundersökningen*, election to the Riksdag, voting
        /// rates among all those entitled to vote, both sexes, **election year 2014** (table
        /// `ME/ME0105/ME0105C/ME0105T01`, fetched 2026-08-31 by POST — the PxWeb API serves data by POST
        /// and metadata by GET).
        ///
        /// <para>⚠ <b>2014 is not a choice, it is the end of the series</b>: the table's own range is
        /// 2002–2014 and the survey published no later wave. Turnout by age is among the most stable
        /// patterns in electoral behaviour, which is why an eleven-year-old vintage is usable — but it is
        /// a vintage, and the national total it implies (85.8 %) is 2014's, not 2022's.</para>
        ///
        /// <para>Keyed by the band's lower bound. 80+ is the open band.</para>
        /// </summary>
        private static readonly (int FromAge, int ToAge, double Percent)[] SwedenTurnout2014 =
        {
            (18, 24, 81.3), (25, 29, 81.4), (30, 34, 83.9), (35, 39, 86.0), (40, 44, 88.0),
            (45, 49, 86.8), (50, 54, 87.7), (55, 59, 88.9), (60, 64, 89.5), (65, 69, 91.9),
            (70, 74, 90.9), (75, 79, 87.0), (80, 999, 73.8),
        };

        /// <summary>SOURCED: the same table's own all-ages figure, carried separately so the group
        /// weights can be checked against it rather than against a number derived from them.</summary>
        public const double SwedenTurnoutAll2014 = 85.8;

        /// <summary>One group in the view: which cohort bands it covers, its computed share of the
        /// ELECTORATE, and its turnout where sourced.</summary>
        public readonly struct Group
        {
            public readonly string Name;
            public readonly int FromAge;
            public readonly int ToAge;
            /// <summary>DERIVED from the cohorts this group covers, as a share of the eligible
            /// population. Never seeded.</summary>
            public readonly double PopulationShare;
            /// <summary>SOURCED where a country publishes turnout by age; `double.NaN` where it does
            /// not. ⚠ NaN is the honest value: it says the model does not know, not that nobody votes.</summary>
            public readonly double TurnoutBase;

            public Group(string name, int fromAge, int toAge, double populationShare, double turnoutBase)
            {
                Name = name; FromAge = fromAge; ToAge = toAge;
                PopulationShare = populationShare; TurnoutBase = turnoutBase;
            }
        }

        /// <summary>
        /// The country's electorate as age groups over its own cohort pyramid.
        ///
        /// <para>⚠ <b>The band edges are the TURNOUT SOURCE's, not the substrate's, where one exists.</b>
        /// Sweden's first group is 18–24 because that is how SCB publishes it; the cohort substrate is
        /// five-year and cannot resolve 18 from 20, so the 15–19 band is apportioned. **That
        /// apportionment is the one approximation here and it is named**: two fifths of the 15–19 band
        /// (ages 18 and 19) are counted as eligible, which assumes people are spread evenly inside a
        /// five-year band — the same standard-and-standardly-wrong assumption the aging step REPLACED
        /// with observed data, and which cannot be replaced here because no source publishes the
        /// electorate by single year.</para>
        /// </summary>
        public static Group[] For(Country country)
        {
            if (country?.Cohorts == null) { return Array.Empty<Group>(); }

            int votingAge = VotingAge(country.Id);
            double eligible = EligiblePopulation(country.Cohorts, votingAge);
            if (eligible <= 0.0) { return Array.Empty<Group>(); }

            (int FromAge, int ToAge, double Percent)[] bands = country.Id == CountryId.Sweden
                ? SwedenTurnout2014
                : DefaultBands(votingAge);

            var groups = new Group[bands.Length];
            for (int b = 0; b < bands.Length; b++)
            {
                double people = InEligibleRange(country.Cohorts, bands[b].FromAge, bands[b].ToAge, votingAge);
                groups[b] = new Group(
                    bands[b].ToAge >= 999 ? $"{bands[b].FromAge}+" : $"{bands[b].FromAge}-{bands[b].ToAge}",
                    bands[b].FromAge, bands[b].ToAge,
                    people / eligible,
                    country.Id == CountryId.Sweden ? bands[b].Percent : double.NaN);
            }

            return groups;
        }

        /// <summary>The five-year bands from the voting age up, for a country with no turnout source of
        /// its own — the substrate's own resolution, with no turnout attached.</summary>
        private static (int, int, double)[] DefaultBands(int votingAge)
        {
            var bands = new List<(int, int, double)>();
            for (int from = votingAge - votingAge % PopulationCohorts.CohortWidth; from < 80; from += PopulationCohorts.CohortWidth)
            {
                bands.Add((Math.Max(from, votingAge), from + PopulationCohorts.CohortWidth - 1, double.NaN));
            }

            bands.Add((80, 999, double.NaN));
            return bands.ToArray();
        }

        /// <summary>Millions eligible to vote: the cohorts at or above the voting age, with the band that
        /// STRADDLES it apportioned by the share of its years that qualify.</summary>
        /// <summary>
        /// F3 (2026-09-02): the same view PER VALKRETS, over the pyramid `SwedishValkretsPopulation2024`
        /// carries for it — SCB's 31 December 2024 population by five-year band, mapped to the 29
        /// Riksdag valkretsar by Vallagen's own municipality lists. Sweden only; index = the statute's
        /// item − 1 (`SwedishValkretsPopulation2024.Names[index]` is the valkrets in Valmyndigheten's
        /// form). ⚠ This pyramid is the 2024 STOCK, not the walked, stepping national pyramid the
        /// country carries: a regional projection is not on record, so a valkrets's age structure is
        /// its last observed one, and the doc says so rather than scaling it silently.
        /// </summary>
        public static Group[] ForValkrets(int index)
        {
            PopulationCohorts pyramid = ValkretsPyramid(index);
            if (pyramid == null) { return Array.Empty<Group>(); }
            int votingAge = VotingAge(CountryId.Sweden);
            double eligible = EligiblePopulation(pyramid, votingAge);
            if (eligible <= 0.0) { return Array.Empty<Group>(); }
            (int FromAge, int ToAge, double Percent)[] bands = SwedenTurnout2014;
            var groups = new Group[bands.Length];
            for (int b = 0; b < bands.Length; b++)
            {
                double people = InEligibleRange(pyramid, bands[b].FromAge, bands[b].ToAge, votingAge);
                groups[b] = new Group(
                    bands[b].ToAge >= 999 ? $"{bands[b].FromAge}+" : $"{bands[b].FromAge}-{bands[b].ToAge}",
                    bands[b].FromAge, bands[b].ToAge, people / eligible, bands[b].Percent);
            }
            return groups;
        }

        /// <summary>The valkrets's 2024 pyramid in the substrate's unit (millions), or null when the index is out of the catalog's range.</summary>
        public static PopulationCohorts ValkretsPyramid(int index)
        {
            if (index < 0 || index >= Generated.SwedishValkretsPopulation2024.Bands.Length) { return null; }
            long[] bands = Generated.SwedishValkretsPopulation2024.Bands[index];
            var counts = new float[PopulationCohorts.CohortCount];
            for (int k = 0; k < counts.Length && k < bands.Length; k++) { counts[k] = bands[k] / 1_000_000f; }
            return new PopulationCohorts(counts);
        }

        /// <summary>The valkrets's index in the catalog by Valmyndigheten's name (the returns catalog's form), or −1.</summary>
        public static int ValkretsIndex(string name) => Array.IndexOf(Generated.SwedishValkretsPopulation2024.Names, name);

        public static double EligiblePopulation(PopulationCohorts cohorts, int votingAge)
            => InEligibleRange(cohorts, votingAge, 999, votingAge);

        private static double InEligibleRange(PopulationCohorts cohorts, int fromAge, int toAge, int votingAge)
        {
            int from = Math.Max(fromAge, votingAge);
            double total = 0.0;
            for (int k = 0; k < PopulationCohorts.CohortCount; k++)
            {
                int bandFrom = k * PopulationCohorts.CohortWidth;
                int bandTo = k == PopulationCohorts.OpenBandIndex ? 999 : bandFrom + PopulationCohorts.CohortWidth - 1;
                if (bandTo < from || bandFrom > toAge) { continue; }

                if (bandFrom >= from && bandTo <= toAge) { total += cohorts.Counts[k]; continue; }

                // A straddling band, apportioned by the share of its years inside the range. The open
                // band is never straddled at its top, so its width stays its own.
                int width = k == PopulationCohorts.OpenBandIndex ? 1 : PopulationCohorts.CohortWidth;
                int lo = Math.Max(bandFrom, from);
                int hi = Math.Min(bandTo, toAge);
                int years = Math.Max(0, hi - lo + 1);
                total += cohorts.Counts[k] * (Math.Min(years, width) / (double)width);
            }

            return total;
        }
    }
}
