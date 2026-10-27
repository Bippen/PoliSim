using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// §681 (Elias's ruling, 2026-09-30): **THE ENTRANT LAYER - AWARENESS AND FAMILY-GROUPED SUBSTITUTION, ONE FAMILY OF CHANGE.** Applied to a
    /// preference vector wherever the model forms one (the idle prediction, the campaign's daily truth, the backtests' loyalty runs), after
    /// `PreferenceModel` and before anything reads the shares.
    ///
    /// <para><b>What it changes, and only when.</b> An ENTRANT is a party with no share at the previous election (prior 0) - a party the
    /// model had to invent a vote for. With no entrant the input array is returned untouched, so every existing path is byte-identical.
    /// With one: (1) <b>awareness</b> - an entrant reaches only the voters aware of it, so its share is <c>a · p</c>, where <c>p</c> is what the
    /// preference gave it and <c>a</c> its awareness (1 for every real party - established parties seed at full awareness; a created party's
    /// from its Recognition, growing in the campaign); (2) <b>family-grouped substitution</b> - where that share comes from. The established
    /// parties' shares with the entrants removed (<c>s</c>) are the base; a nested logit over the expert survey's party families
    /// (<see cref="PartyFamilies"/>) is calibrated to reproduce <c>s</c> exactly, the entrants join their families' nests, and each entrant's
    /// attraction is solved so its share is <c>a · p</c>. A nest's λ is 1 (the draw proportional to size - the preference model's own property,
    /// confirmed by `EntrantLayerDiagnostic`) unless an entrant in it carries a grouping strength, which draws it first from its own family.</para>
    ///
    /// <para>⚠ <b>Who carries the grouping (measured, §681):</b> a CREATED party - its family is the model's own assignment and no election has
    /// measured its draw. A real entrant keeps the proportional draw: grouped with Die Linke (CHES family 6), BSW worsened every German 2025
    /// backtest (loyalty run 4.55 → 4.85 pp, both layers 5.01 → 5.37, the eight-party set 4.66 → 5.04) - the 2025 Linke rose while BSW took its
    /// share. Grouping real entrants too is the alternative, measured and recorded for Elias.</para>
    /// </summary>
    public static class EntrantLayer
    {
        /// <summary>[AUTHORED-DRAFT] a created party's family grouping strength, 1 - λ of its nest (0 = proportional to size, 1 = only its own
        /// family). On the play-calibration list.</summary>
        public const double GroupingStrength = 0.5;

        /// <summary>[AUTHORED-DRAFT] how fast a created party's awareness grows in a campaign: per unit of its coverage and of its campaigning
        /// pressure, each as a share of the leading party's. On the play-calibration list.</summary>
        public const double AwarenessPerCoverage = 1.0;
        public const double AwarenessPerPressure = 1.0;

        /// <summary>Awareness after a campaign's reach so far: <c>1 - (1 - a0) · exp(-(kc · coverage + kp · pressure))</c>, each input 0-1 against the leader.</summary>
        public static double GrownAwareness(double start, double coverageShare, double pressureShare) =>
            1.0 - (1.0 - Clamp01(start)) * Math.Exp(-(AwarenessPerCoverage * Clamp01(coverageShare) + AwarenessPerPressure * Clamp01(pressureShare)));

        private static double Clamp01(double v) => v < 0.0 ? 0.0 : v > 1.0 ? 1.0 : v;

        /// <summary>Each key's grouping strength: <see cref="GroupingStrength"/> for a created party, 0 for every real one (see the class note).
        /// Null where the country has no created party.</summary>
        public static double[] GroupingOf(CountryId country, IReadOnlyList<string> keys)
        {
            if (!CreatedParties.Any(country)) { return null; }
            var g = new double[keys.Count];
            for (int i = 0; i < g.Length; i++)
            {
                foreach (CreatedParty c in CreatedParties.Of(country)) { if (c.Key == keys[i]) { g[i] = GroupingStrength; } }
            }
            return g;
        }

        public static bool IsEntrant(double prior) => prior <= 0.0;

        /// <summary>§684: which substitution rule is in force - the family nest carried by created parties (§681, the default), or the measured
        /// alternative, similarity by position over the nine dimensions for every entrant (`EntrantSimilarity`). Set only by the measuring entry.</summary>
        public enum Rule { FamilyForCreated, SimilarityForAll }

        public static Rule Active = Rule.FamilyForCreated;

        /// <summary>§684: the layer by the rule in force, for a country's keys - the one entry the idle prediction and the backtests call.</summary>
        public static double[] ApplyFor(double[] preference, double[] prior, CountryId country, IReadOnlyList<string> keys, double[] awareness)
        {
            // GATE-18 q22: every created party enters the layer as an entrant, and a Regional party reaches its own region's voters alone; with none
            // registered, the very prior and the very awareness
            prior = LayerPrior(country, keys, prior);
            awareness = RegionalAwareness(country, keys, awareness);
            if (Active == Rule.SimilarityForAll) { return EntrantSimilarity.Apply(preference, prior, EntrantSimilarity.For(country, keys), awareness); }
            return Apply(preference, prior, PartyFamilies.For(country, keys), awareness, GroupingOf(country, keys));
        }

        // -----------------------------------------------------------------------------------------------------------------------------
        // GATE-18 q22 (Elias's ruling of 2026-10-18, `docs/RULINGS_2026-10-18.md`; the audit's GA-4-2 and GA-4-3): "a created party with no campaign
        // stays below the threshold whatever its origin; origin sets its reach [AUTHORED-DRAFT]. A Regional party's vote comes from its own regional
        // base." Three terms, each inert with no created party registered (the very arrays returned):
        // - every created party is an ENTRANT here, a Splinter too - it stands for the first time, its parent's slice the prior its preference reads,
        //   so its share is reached through its awareness and drawn first from its family as every created party's is (`LayerPrior`);
        // - a Regional party reaches the voters of its own region alone: its awareness is scaled by the region's share of the valid vote, and the
        //   count spreads its vote over that region only (`SwedishRegions.Regions`), so inside it it holds what the preference gives it (`RegionalAwareness`);
        // - THE REACH: its share is held at its origin's reach of the line that binds it (`PartyOrigins.Reach`), lifted by the awareness a campaign buys
        //   (`Ceiling`), the excess going to the real parties by size - after the government's record moves the vote, so no record pushes it over
        //   the line either (`HoldToReach`).
        // -----------------------------------------------------------------------------------------------------------------------------

        /// <summary>GATE-18 q22: a created party of <paramref name="country"/> by its key, or null.</summary>
        public static CreatedParty CreatedOf(CountryId country, string key)
        {
            foreach (CreatedParty c in CreatedParties.Of(country)) { if (c.Key == key) { return c; } }
            return null;
        }

        /// <summary>GATE-18 q22: a created party by its key among every country's registrations - the campaign's cast carries no country; its opening
        /// awareness below one is what marks a created party there (every real party opens at one).</summary>
        private static CreatedParty CreatedAnywhere(string key)
        {
            foreach (CreatedParty c in CreatedParties.All()) { if (c.Key == key) { return c; } }
            return null;
        }

        /// <summary>GATE-18 q22: the prior the layer reads to mark its entrants - a created party's at 0 (a Splinter's slice stays in the prior its
        /// preference read). The very array where no created party holds a prior.</summary>
        public static double[] LayerPrior(CountryId country, IReadOnlyList<string> keys, double[] prior) =>
            !CreatedParties.Any(country) ? prior : LayerPrior(keys, prior, k => CreatedOf(country, k), null);

        /// <summary>GATE-18 q22: the same for a campaign's cast, its opening awareness marking the created parties.</summary>
        public static double[] LayerPrior(IReadOnlyList<string> keys, double[] prior, double[] opening) =>
            opening == null ? prior : LayerPrior(keys, prior, CreatedAnywhere, opening);

        private static double[] LayerPrior(IReadOnlyList<string> keys, double[] prior, Func<string, CreatedParty> created, double[] opening)
        {
            if (prior == null || keys == null) { return prior; }
            double[] marked = null;
            for (int i = 0; i < prior.Length && i < keys.Count; i++)
            {
                if (prior[i] <= 0.0 || (opening != null && (i >= opening.Length || opening[i] >= 1.0)) || created(keys[i]) == null) { continue; }
                marked ??= (double[])prior.Clone();
                marked[i] = 0.0;
            }
            return marked ?? prior;
        }

        /// <summary>GATE-18 q22: the share of the valid vote a created party's own region casts - a Regional party's base; 1 for every other party, and
        /// for a Regional party whose region the country's regions do not name (only Sweden's valkretsar are offered, `PartyCreationFlow.Regions`).</summary>
        public static double RegionalBase(CreatedParty c)
        {
            if (c == null || c.Origin != PartyOrigin.Regional || c.Country != CountryId.Sweden) { return 1.0; }
            return SwedishRegions.TryWeightShare(c.Region, out double share) ? share : 1.0;
        }

        /// <summary>GATE-18 q22: a created party's share of the vote before any campaign, at most - its origin's reach of its country's threshold; a
        /// Regional party's, at most its reach of the regional route's line inside its region, as a share of the nation's vote (and never above the
        /// national figure). NaN where its country counts no national line (no cap).</summary>
        public static double OpeningCeiling(CreatedParty c)
        {
            if (c == null) { return double.NaN; }
            double reach = PartyOrigins.ReachOf(c.Origin);
            double national = reach * NationalElection.ThresholdOf(c.Country);
            double regionalBase = RegionalBase(c);
            double regionalLine = NationalElection.RegionalThresholdOf(c.Country);
            if (regionalBase < 1.0 && !double.IsNaN(regionalLine))
            {
                double inRegion = reach * regionalLine * regionalBase;
                return double.IsNaN(national) ? inRegion : Math.Min(national, inRegion);
            }
            return national;
        }

        /// <summary>GATE-18 q22: the cap on a created party's share once its awareness stands at <paramref name="awareness"/> - the opening ceiling at
        /// its opening awareness, lifted in step with the awareness a campaign buys: <c>c0 + (1 - c0) · (a - a0) / (1 - a0)</c>, no cap (1) at full
        /// awareness, and none where there is no opening ceiling or the party opened fully known.</summary>
        public static double Ceiling(double openingCeiling, double openingAwareness, double awareness)
        {
            if (double.IsNaN(openingCeiling) || openingCeiling >= 1.0) { return 1.0; }
            double a0 = Clamp01(openingAwareness);
            if (a0 >= 1.0) { return 1.0; }
            double lift = Math.Max(0.0, Math.Min(1.0, (Clamp01(awareness) - a0) / (1.0 - a0)));
            return openingCeiling + (1.0 - openingCeiling) * lift;
        }

        /// <summary>GATE-18 q22: the awareness the layer reads - a Regional party's scaled by its region's share of the vote (it reaches that region's
        /// voters alone); every other key as given. The very array where no Regional party is in the keys.</summary>
        public static double[] RegionalAwareness(CountryId country, IReadOnlyList<string> keys, double[] awareness) =>
            !CreatedParties.Any(country) ? awareness : RegionalAwareness(keys, awareness, k => CreatedOf(country, k), null);

        /// <summary>GATE-18 q22: the same for a campaign's cast, its opening awareness marking the created parties.</summary>
        public static double[] RegionalAwareness(IReadOnlyList<string> keys, double[] awareness, double[] opening) =>
            opening == null ? awareness : RegionalAwareness(keys, awareness, CreatedAnywhere, opening);

        private static double[] RegionalAwareness(IReadOnlyList<string> keys, double[] awareness, Func<string, CreatedParty> created, double[] opening)
        {
            if (keys == null) { return awareness; }
            double[] scaled = null;
            for (int i = 0; i < keys.Count; i++)
            {
                if (opening != null && (i >= opening.Length || opening[i] >= 1.0)) { continue; }
                double regionalBase = RegionalBase(created(keys[i]));
                if (regionalBase >= 1.0) { continue; }
                if (scaled == null)
                {
                    scaled = new double[keys.Count];
                    for (int j = 0; j < scaled.Length; j++) { scaled[j] = awareness != null && j < awareness.Length ? awareness[j] : 1.0; }
                }
                scaled[i] *= regionalBase;
            }
            return scaled ?? awareness;
        }

        /// <summary>K129 (Elias, 2026-10-27): a Regional party's share read through its own valkrets's electorate - scaled by its parents' strength
        /// there against the nation (`SwedishRegions.TryStrengthRatio`), its parents weighted as the layer's nest draws them: <see cref="GroupingStrength"/>
        /// from its own family by size, the rest from every real party by size. The real parties absorb the change by size. The very array where no Regional
        /// party is in the keys.</summary>
        public static double[] RegionalAffinity(CountryId country, IReadOnlyList<string> keys, double[] shares, int[] family) =>
            shares == null || !CreatedParties.Any(country) ? shares : Affinity(keys, shares, family, null, k => CreatedOf(country, k));

        /// <summary>K129: the same for a campaign's cast, its opening awareness marking the created parties.</summary>
        public static double[] RegionalAffinity(IReadOnlyList<string> keys, double[] shares, int[] family, double[] opening) =>
            shares == null || opening == null ? shares : Affinity(keys, shares, family, opening, CreatedAnywhere);

        /// <summary>K129: the affinity itself - <c>Σ_j w_j · ratio_j</c> over the real parties, <c>w_j = G · [family_j = family_c] · s_j / S_family
        /// + (1 - G) · s_j / S_real</c>.</summary>
        public static double AffinityOf(IReadOnlyList<string> keys, double[] shares, int[] family, int created, string region, bool[] isReal)
        {
            double all = 0.0, kin = 0.0;
            int own = family != null && created < family.Length ? family[created] : -1;
            for (int j = 0; j < shares.Length; j++)
            {
                if (!isReal[j]) { continue; }
                all += shares[j];
                if (own >= 0 && family != null && j < family.Length && family[j] == own) { kin += shares[j]; }
            }
            if (all <= 0.0) { return 1.0; }
            double g = kin > 0.0 ? GroupingStrength : 0.0, weighted = 0.0, weights = 0.0;
            for (int j = 0; j < shares.Length; j++)
            {
                if (!isReal[j] || !SwedishRegions.TryStrengthRatio(region, keys[j], out double ratio)) { continue; }
                double w = (1.0 - g) * shares[j] / all + (g > 0.0 && family[j] == own ? g * shares[j] / kin : 0.0);
                weighted += w * ratio;
                weights += w;
            }
            return weights > 0.0 ? weighted / weights : 1.0;
        }

        private static double[] Affinity(IReadOnlyList<string> keys, double[] shares, int[] family, double[] opening, Func<string, CreatedParty> created)
        {
            if (keys == null) { return shares; }
            var isReal = new bool[shares.Length];
            var regional = new List<(int Index, string Region)>();
            for (int i = 0; i < shares.Length && i < keys.Count; i++)
            {
                CreatedParty c = opening != null && (i >= opening.Length || opening[i] >= 1.0) ? null : created(keys[i]);
                isReal[i] = c == null;
                if (c != null && RegionalBase(c) < 1.0) { regional.Add((i, c.Region)); }
            }
            if (regional.Count == 0) { return shares; }
            double[] scaled = (double[])shares.Clone();
            foreach ((int i, string region) in regional) { scaled[i] = shares[i] * AffinityOf(keys, shares, family, i, region, isReal); }
            double createdBefore = 0.0, createdAfter = 0.0, real = 0.0;
            for (int i = 0; i < shares.Length; i++) { if (isReal[i]) { real += shares[i]; } else { createdBefore += shares[i]; createdAfter += scaled[i]; } }
            if (real <= 0.0) { return shares; }
            double factor = (real - (createdAfter - createdBefore)) / real;
            for (int i = 0; i < shares.Length; i++) { if (isReal[i]) { scaled[i] = shares[i] * factor; } }
            return scaled;
        }

        /// <summary>GATE-18 q22: the shares with every created party held within its reach (`Ceiling` at its awareness - the opening awareness
        /// where <paramref name="awareness"/> is null: no campaign), the excess spread over the real parties by their size. The very array where no cap binds.</summary>
        public static double[] HoldToReach(CountryId country, IReadOnlyList<string> keys, double[] shares, double[] awareness) =>
            shares == null || !CreatedParties.Any(country) ? shares : Hold(keys, shares, NationalElection.EntrantAwareness(country, keys), awareness, k => CreatedOf(country, k));

        /// <summary>GATE-18 q22: the same for a campaign's cast - its opening awareness and today's.</summary>
        public static double[] HoldToReach(IReadOnlyList<string> keys, double[] shares, double[] opening, double[] awareness) =>
            shares == null || opening == null ? shares : Hold(keys, shares, opening, awareness, CreatedAnywhere);

        private static double[] Hold(IReadOnlyList<string> keys, double[] shares, double[] opening, double[] awareness, Func<string, CreatedParty> created)
        {
            if (keys == null || opening == null) { return shares; }
            double[] held = null;
            var isCreated = new bool[shares.Length];
            double excess = 0.0;
            for (int i = 0; i < shares.Length && i < keys.Count && i < opening.Length; i++)
            {
                if (opening[i] >= 1.0) { continue; }
                CreatedParty c = created(keys[i]);
                if (c == null) { continue; }
                isCreated[i] = true;
                double now = awareness != null && i < awareness.Length ? awareness[i] : opening[i];
                double cap = Ceiling(OpeningCeiling(c), opening[i], now);
                if (shares[i] <= cap) { continue; }
                held ??= (double[])shares.Clone();
                excess += shares[i] - cap;
                held[i] = cap;
            }
            if (held == null) { return shares; }
            double room = 0.0;
            for (int i = 0; i < held.Length; i++) { if (!isCreated[i]) { room += held[i]; } }
            if (room <= 0.0) { return held; }
            for (int i = 0; i < held.Length; i++) { if (!isCreated[i]) { held[i] += excess * held[i] / room; } }
            return held;
        }

        /// <summary>The layer. <paramref name="preference"/> sums to 1; <paramref name="prior"/> marks the entrants (0); <paramref name="family"/> the
        /// party's family (-1 none: its own nest); <paramref name="awareness"/> null means every party at 1; <paramref name="grouping"/> null means
        /// every nest proportional. Returns the same array when nothing is an entrant.</summary>
        public static double[] Apply(double[] preference, double[] prior, int[] family, double[] awareness, double[] grouping)
        {
            if (preference == null || prior == null || prior.Length != preference.Length) { return preference; }
            int n = preference.Length;
            bool any = false;
            for (int i = 0; i < n; i++) { if (IsEntrant(prior[i])) { any = true; break; } }
            if (!any) { return preference; }

            // the base: the established parties with the entrants removed
            double established = 0.0;
            for (int i = 0; i < n; i++) { if (!IsEntrant(prior[i])) { established += preference[i]; } }
            if (established <= 0.0) { return preference; }
            var s = new double[n];
            for (int i = 0; i < n; i++) { s[i] = IsEntrant(prior[i]) ? 0.0 : preference[i] / established; }

            // nests: a family with members, or a party of its own (-1 or a unique id)
            var nestOf = new int[n];
            var nestIds = new Dictionary<int, int>();
            int nests = 0;
            for (int i = 0; i < n; i++)
            {
                int f = family != null && i < family.Length ? family[i] : -1;
                int key = f >= 0 ? f : -1000 - i;
                if (!nestIds.TryGetValue(key, out int k)) { k = nests++; nestIds[key] = k; }
                nestOf[i] = k;
            }
            var nestShare = new double[nests];
            for (int i = 0; i < n; i++) { nestShare[nestOf[i]] += s[i]; }

            // each nest's λ: 1 (proportional) unless an entrant in it carries a grouping strength - the strongest one's
            var lambda = new double[nests];
            for (int k = 0; k < nests; k++) { lambda[k] = 1.0; }
            for (int i = 0; i < n; i++)
            {
                if (!IsEntrant(prior[i]) || grouping == null || i >= grouping.Length) { continue; }
                lambda[nestOf[i]] = Math.Min(lambda[nestOf[i]], Math.Max(1e-3, 1.0 - Clamp01(grouping[i])));
            }

            // calibration: y_i = s_i · S_N^(1/λ_N - 1) reproduces s exactly (Y_N = S_N^(1/λ_N), P(N) = S_N)
            var y = new double[n];
            for (int i = 0; i < n; i++) { if (!IsEntrant(prior[i]) && s[i] > 0.0) { y[i] = s[i] * Math.Pow(nestShare[nestOf[i]], 1.0 / lambda[nestOf[i]] - 1.0); } }

            // the entrants' targets, a · p; their attractions solved so each lands on its target (Gauss-Seidel over the entrants)
            var target = new double[n];
            double targetSum = 0.0;
            for (int i = 0; i < n; i++)
            {
                if (!IsEntrant(prior[i])) { continue; }
                double a = awareness != null && i < awareness.Length ? Clamp01(awareness[i]) : 1.0;
                target[i] = a * preference[i];
                targetSum += target[i];
            }
            if (targetSum >= 0.999) { return preference; }   // nothing left to take from - the base cannot be defined
            for (int sweep = 0; sweep < 30; sweep++)
            {
                for (int e = 0; e < n; e++)
                {
                    if (!IsEntrant(prior[e])) { continue; }
                    if (target[e] <= 0.0) { y[e] = 0.0; continue; }
                    double lo = 0.0, hi = 1.0;
                    while (Shares(y, nestOf, nests, lambda, e, hi)[e] < target[e] && hi < 1e12) { hi *= 2.0; }
                    for (int it = 0; it < 80; it++)
                    {
                        double mid = 0.5 * (lo + hi);
                        if (Shares(y, nestOf, nests, lambda, e, mid)[e] < target[e]) { lo = mid; } else { hi = mid; }
                    }
                    y[e] = 0.5 * (lo + hi);
                }
            }
            return Shares(y, nestOf, nests, lambda, -1, 0.0);
        }

        /// <summary>The nested logit's shares for attractions <paramref name="y"/> (with party <paramref name="e"/>'s replaced by <paramref name="ye"/> when e ≥ 0).</summary>
        private static double[] Shares(double[] y, int[] nestOf, int nests, double[] lambda, int e, double ye)
        {
            int n = y.Length;
            var Y = new double[nests];
            for (int i = 0; i < n; i++) { Y[nestOf[i]] += i == e ? ye : y[i]; }
            double denom = 0.0;
            var W = new double[nests];
            for (int k = 0; k < nests; k++) { W[k] = Y[k] > 0.0 ? Math.Pow(Y[k], lambda[k]) : 0.0; denom += W[k]; }
            var p = new double[n];
            for (int i = 0; i < n; i++)
            {
                double yi = i == e ? ye : y[i];
                int k = nestOf[i];
                p[i] = denom > 0.0 && Y[k] > 0.0 ? W[k] / denom * yi / Y[k] : 0.0;
            }
            return p;
        }
    }
}
