using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// Regional 5a (`docs/RULINGS_2026-10-23.md` K146-K148, Elias 2026-10-27): <b>5a's effects become regional - a contact moves the region it lands
    /// in, and the national total and the count stay consistent.</b> Until this piece every contact was tallied against the whole electorate, so a
    /// rally in Norrbotten covered the same national pool as a television buy and the count spread the campaign's national result over the regions
    /// by the uniform swing alone.
    ///
    /// <para><b>Where a contact lands.</b> A local act (a rally, a town hall, the doors) lands in the region it was held in (<see cref="Add(int, CampaignActions.ChainTrace, int)"/>
    /// with the region); a national act (television, digital, the post, an interview, an announcement, the press's coverage) lands on every region
    /// where the party stands in proportion to the region's audience - the same voters §699's standing share already restricts it to. The national
    /// tallies the rest of the campaign reads (the AI's own books, the bounds, the ledgers) are unchanged: only the local acts' share of each slot
    /// is kept per region, and a region's tally is the slot's national remainder spread by audience plus what was held there.</para>
    ///
    /// <para><b>The effect, region by region.</b> In region r every party standing there has reach one, and its effect is 5a's own -
    /// Σ_class min(CACE·q̄, upper) × coverage, the coverage read over the region's audience (`CampaignPersuasion.ClassEffectPp`, the attacks K66's
    /// shared coverage, an attack taking only where its target stands). DERIVED, the identity the regional form is built on: a contact spread over the
    /// standing regions in proportion to their audiences covers each region at r_p·(1 − e^(−E/(r_p·A))) / r_p of its voters, so the regions'
    /// audience-weighted sum is exactly the national formula - a campaign of national acts alone has the national effect it always had; a local act
    /// saturates its own region's voters faster, which is what holding it there means.</para>
    ///
    /// <para><b>The absorption, region by region</b> (K81/K91 read per region): inside a region every party standing there stands everywhere, so the
    /// balanced absorption there is the uniform one (`CampaignPersuasion.BalancedAbsorption` on the region's held parties). The national movement is the
    /// regions' movements weighted by their audiences (<see cref="NationalShift"/>). DECLARED: K81's three conditions hold for the sum - each region's
    /// columns sum to one, so the national shares still sum to one; equal effort per voter reached moves nobody in any region, so nobody nationally;
    /// a lone campaigner moves exactly its region-weighted sourced effect, which is its national sourced effect. Where every party stands everywhere
    /// (Sweden, the USA) the sum IS the national balanced absorption (it is linear and the matrix is the same uniform one); where candidacies differ
    /// (Germany) it replaces the national balancing of who meets whom with the meeting itself, region by region.</para>
    ///
    /// <para><b>The count</b> reads each region's movement (`CampaignRun.Result.RegionalShift`): the region's prior is moved by it before the uniform
    /// swing spreads the national shares (`NationalElection.RegionalOf`), so the region a contact landed in carries it, every other region does not, and
    /// the swing's own re-solve keeps the regions summing to the national shares (`ParliamentaryCount`).</para>
    /// </summary>
    public sealed partial class CampaignPressure
    {
        // Regional 5a: each region's audience (null = the national tally alone, every harness staging with fewer than two regions), their sum, where each
        // party stands ([p][r], null = everywhere) and the audience where it stands; the local acts' exposure and quality per own slot and region, and the
        // attacks' per attacker, target, slot and region (allocated on the first).
        private double[] _regionAudience;
        private double _allAudience;
        private bool[][] _regionStands;
        private double[] _standAudience;
        private double[][][] _localExposure;
        private double[][][] _localQuality;
        private double[][][][] _localAttackExposure;
        private double[][][][] _localAttackQuality;
        // the running sums of the local tallies over the regions, per slot - so a region's national remainder is read without a sweep
        private double[][] _localExposureSum;
        private double[][] _localQualitySum;
        private double[][][] _localAttackExposureSum;
        private double[][][] _localAttackQualitySum;

        /// <summary>Regional 5a: tally by region from here on - <paramref name="audience"/> each region's audience (the staging's `RegionAudience.Audience`),
        /// <paramref name="stands"/> where each party stands ([p][r], null = everywhere). Called once, before the first contact; fewer than two regions or no
        /// audience leaves the tally national.</summary>
        public void ConfigureRegions(double[] audience, bool[][] stands)
        {
            if (audience == null || audience.Length < 2) { return; }
            double all = 0.0;
            foreach (double a in audience) { all += Math.Max(0.0, a); }
            if (!(all > 0.0)) { return; }
            int n = _persuasion.Length;
            _regionAudience = (double[])audience.Clone();
            _allAudience = all;
            _regionStands = stands;
            _standAudience = new double[n];
            _localExposure = new double[n][][];
            _localQuality = new double[n][][];
            _localAttackExposure = new double[n][][][];
            _localAttackQuality = new double[n][][][];
            _localExposureSum = new double[n][];
            _localQualitySum = new double[n][];
            _localAttackExposureSum = new double[n][][];
            _localAttackQualitySum = new double[n][][];
            for (int p = 0; p < n; p++)
            {
                for (int r = 0; r < audience.Length; r++) { if (StandsIn(p, r)) { _standAudience[p] += Math.Max(0.0, audience[r]); } }
                _localExposure[p] = new double[OwnSlots][];
                _localQuality[p] = new double[OwnSlots][];
                _localExposureSum[p] = new double[OwnSlots];
                _localQualitySum[p] = new double[OwnSlots];
                for (int slot = 0; slot < OwnSlots; slot++) { _localExposure[p][slot] = new double[audience.Length]; _localQuality[p][slot] = new double[audience.Length]; }
            }
        }

        /// <summary>Regional 5a: whether the tally is kept by region.</summary>
        public bool Regional => _regionAudience != null;

        /// <summary>Regional 5a: the number of regions tallied (0 for a national tally).</summary>
        public int RegionCount => _regionAudience == null ? 0 : _regionAudience.Length;

        /// <summary>Regional 5a: region <paramref name="r"/>'s audience over all the regions' - the weight its movement takes in the national one.</summary>
        public double RegionWeight(int r) => _regionAudience == null || r < 0 || r >= _regionAudience.Length ? 0.0 : Math.Max(0.0, _regionAudience[r]) / _allAudience;

        /// <summary>Regional 5a: region <paramref name="r"/>'s audience.</summary>
        public double RegionAudience(int r) => _regionAudience == null || r < 0 || r >= _regionAudience.Length ? 0.0 : Math.Max(0.0, _regionAudience[r]);

        /// <summary>Regional 5a: whether <paramref name="p"/> stands in region <paramref name="r"/> (everywhere where no candidacy was handed).</summary>
        public bool StandsIn(int p, int r) => _regionStands == null || p < 0 || p >= _regionStands.Length || _regionStands[p] == null || r < 0 || r >= _regionStands[p].Length || _regionStands[p][r];

        private void AddLocal(int p, int slot, double exposure, double quality, int region)
        {
            if (_regionAudience == null || region < 0 || region >= _regionAudience.Length) { return; }
            _localExposure[p][slot][region] += exposure;
            _localQuality[p][slot][region] += quality;
            _localExposureSum[p][slot] += exposure;
            _localQualitySum[p][slot] += quality;
        }

        private void AddLocalAttack(int attacker, int target, int slot, double exposure, double quality, double share, int region)
        {
            if (_regionAudience == null || region < 0 || region >= _regionAudience.Length) { return; }
            _localExposure[attacker][slot][region] += exposure * (1.0 - share);
            _localQuality[attacker][slot][region] += quality * (1.0 - share);
            _localExposureSum[attacker][slot] += exposure * (1.0 - share);
            _localQualitySum[attacker][slot] += quality * (1.0 - share);
            if (_localAttackExposure[attacker] == null) { _localAttackExposure[attacker] = new double[_persuasion.Length][][]; _localAttackQuality[attacker] = new double[_persuasion.Length][][]; _localAttackExposureSum[attacker] = new double[_persuasion.Length][]; _localAttackQualitySum[attacker] = new double[_persuasion.Length][]; }
            if (_localAttackExposure[attacker][target] == null)
            {
                _localAttackExposure[attacker][target] = new double[OwnSlots][];
                _localAttackQuality[attacker][target] = new double[OwnSlots][];
                _localAttackExposureSum[attacker][target] = new double[OwnSlots];
                _localAttackQualitySum[attacker][target] = new double[OwnSlots];
                for (int s = 0; s < OwnSlots; s++) { _localAttackExposure[attacker][target][s] = new double[_regionAudience.Length]; _localAttackQuality[attacker][target][s] = new double[_regionAudience.Length]; }
            }
            _localAttackExposure[attacker][target][slot][region] += exposure * share;
            _localAttackQuality[attacker][target][slot][region] += quality * share;
            _localAttackExposureSum[attacker][target][slot] += exposure * share;
            _localAttackQualitySum[attacker][target][slot] += quality * share;
        }

        /// <summary>The share of a national act of <paramref name="p"/>'s that lands in region <paramref name="r"/>: its audience over the audience where the party stands, 0 where it does not stand.</summary>
        private double NationalShareIn(int p, int r) => StandsIn(p, r) && _standAudience[p] > 0.0 ? Math.Max(0.0, _regionAudience[r]) / _standAudience[p] : 0.0;

        /// <summary>Regional 5a: one own slot's exposure and quality in region <paramref name="r"/> - the slot's national remainder (its tally less every local act's) spread by audience, plus what was held there.</summary>
        public void RegionalSlot(int p, int slot, int r, out double exposure, out double quality)
        {
            double share = NationalShareIn(p, r);
            exposure = Math.Max(0.0, _exposure[p][slot] - _localExposureSum[p][slot]) * share + _localExposure[p][slot][r];
            quality = Math.Max(0.0, _quality[p][slot] - _localQualitySum[p][slot]) * share + _localQuality[p][slot][r];
        }

        /// <summary>Regional 5a: the attacks <paramref name="attacker"/> made on <paramref name="target"/> in one slot, in region <paramref name="r"/> - as <see cref="RegionalSlot"/>.</summary>
        private void RegionalAttackSlot(int attacker, int target, int slot, int r, out double exposure, out double quality)
        {
            exposure = 0.0; quality = 0.0;
            double[] e = _attackExposure[attacker]?[target];
            if (e == null) { return; }
            double[] q = _attackQuality[attacker][target];
            double[][] le = _localAttackExposure[attacker]?[target];
            double[][] lq = le != null ? _localAttackQuality[attacker][target] : null;
            double[] les = le != null ? _localAttackExposureSum[attacker][target] : null;
            double[] lqs = le != null ? _localAttackQualitySum[attacker][target] : null;
            double share = NationalShareIn(attacker, r);
            exposure = Math.Max(0.0, e[slot] - (les != null ? les[slot] : 0.0)) * share + (le != null ? le[slot][r] : 0.0);
            quality = Math.Max(0.0, q[slot] - (lqs != null ? lqs[slot] : 0.0)) * share + (lq != null ? lq[slot][r] : 0.0);
        }

        /// <summary>Regional 5a: the attacks <paramref name="attacker"/> made on <paramref name="target"/> in one class, in region <paramref name="r"/> - exposure and quality summed over its slots, what an independent recomputation reads.</summary>
        public void RegionalAttack(int attacker, int target, ContactClass c, int r, out double exposure, out double quality)
        {
            exposure = 0.0; quality = 0.0;
            if (_regionAudience == null) { return; }
            for (int slot = 0; slot < OwnSlots; slot++)
            {
                if (SlotClass(slot) != c) { continue; }
                RegionalAttackSlot(attacker, target, slot, r, out double e, out double q);
                exposure += e; quality += q;
            }
        }

        /// <summary>
        /// Regional 5a: each party's own effect in region <paramref name="r"/> (fractions of the region's vote, signed, the attacks it received netted),
        /// with the attacks' losses and takings - `ShareEffects`'s form on the region's tally, the coverage read over the region's audience at reach one
        /// for every party standing there, an attack taking only where its target stands. The predicates admit sources as `ShareEffects`'s do.
        /// </summary>
        public double[] RegionEffects(int r, double norm, Func<int, int, bool> include, Func<int, int, int, bool> includeAttack, out double[] attackLoss, out double[] attackGain)
        {
            int n = _persuasion.Length;
            var effects = new double[n];
            attackLoss = new double[n];
            attackGain = new double[n];
            if (_regionAudience == null || r < 0 || r >= _regionAudience.Length) { return effects; }
            double electorate = Math.Max(0.0, _regionAudience[r]);
            int classes = CampaignPersuasion.ClassCount;
            for (int p = 0; p < n; p++)
            {
                if (!StandsIn(p, r)) { continue; }   // a party with no list here moves no vote here, and takes none
                var e = new double[classes];
                var q = new double[classes];
                for (int slot = 0; slot < OwnSlots; slot++)
                {
                    if (include != null && !include(p, slot)) { continue; }
                    RegionalSlot(p, slot, r, out double se, out double sq);
                    int c = (int)SlotClass(slot);
                    e[c] += se;
                    q[c] += sq;
                }
                double[] made = null;
                double[][] ae = null, aq = null;
                double[][] attacks = _attackExposure[p];
                if (attacks != null)
                {
                    for (int t = 0; t < attacks.Length; t++)
                    {
                        if (attacks[t] == null) { continue; }
                        for (int slot = 0; slot < OwnSlots; slot++)
                        {
                            if (attacks[t][slot] <= 0.0) { continue; }
                            bool admitted = includeAttack != null ? includeAttack(p, slot, t) : include == null || (include(p, slot) && include(t, AgainstSlot));
                            if (!admitted) { continue; }
                            RegionalAttackSlot(p, t, slot, r, out double xe, out double xq);
                            if (made == null) { made = new double[classes]; ae = new double[n][]; aq = new double[n][]; }
                            if (ae[t] == null) { ae[t] = new double[classes]; aq[t] = new double[classes]; }
                            int c = (int)SlotClass(slot);
                            ae[t][c] += xe;
                            aq[t][c] += xq;
                            made[c] += xe;
                        }
                    }
                }
                double pp = 0.0;
                for (int c = 0; c < classes; c++)
                {
                    pp += made == null ? CampaignPersuasion.ClassEffectPp((ContactClass)c, e[c], q[c], electorate, norm)
                                       : CampaignPersuasion.SharedClassEffectPp((ContactClass)c, e[c], q[c], made[c], electorate, norm);
                }
                effects[p] += pp / 100.0;
                if (made == null) { continue; }
                for (int t = 0; t < n; t++)
                {
                    if (ae[t] == null) { continue; }
                    double onBallot = StandsIn(t, r) ? 1.0 : 0.0;
                    for (int c = 0; c < classes; c++)
                    {
                        double taken = CampaignPersuasion.AttackEffectPp((ContactClass)c, ae[t][c], aq[t][c], e[c] + made[c], electorate, norm, 1.0, onBallot) / 100.0;
                        effects[t] -= taken;
                        attackLoss[t] += taken;
                        attackGain[p] += taken;
                    }
                }
            }
            return effects;
        }

        /// <summary>
        /// Regional 5a: every party's effect as the national vector `ShareEffects` returns once the tally is regional - each region's effects
        /// (<see cref="RegionEffects"/>) weighted by its audience, the attacks' losses and takings likewise. A campaign of national acts alone returns
        /// the national formula's effects (the DERIVED identity in the class's note, to the rounding of a double).
        /// </summary>
        public double[] RegionalShareEffects(double norm, Func<int, int, bool> include, Func<int, int, int, bool> includeAttack, out double[] attackLoss, out double[] attackGain)
        {
            int n = _persuasion.Length;
            var effects = new double[n];
            attackLoss = new double[n];
            attackGain = new double[n];
            for (int r = 0; r < RegionCount; r++)
            {
                double w = RegionWeight(r);
                if (w <= 0.0) { continue; }
                double[] e = RegionEffects(r, norm, include, includeAttack, out double[] loss, out double[] gain);
                for (int p = 0; p < n; p++) { effects[p] += w * e[p]; attackLoss[p] += w * loss[p]; attackGain[p] += w * gain[p]; }
            }
            return effects;
        }

        /// <summary>
        /// Regional 5a (review L1): the plan's (4)(b) bound read as the effect is - in each region `BoundsPp`'s form on the region's own tally (the class's upper
        /// limit × the coverage of everything the party sent there × its own part, plus for every attack it received there the attacker's same product × the
        /// attack's part, where the target stands), the regions weighted by their audience. Each region's effect lies inside its own bound term by term, so the
        /// weighted sum does; a national bound need not hold where a party's own/attack split differs between regions (the review's worked case).
        /// </summary>
        public double[] RegionalBoundsPp()
        {
            int n = _persuasion.Length, classes = CampaignPersuasion.ClassCount;
            var bounds = new double[n];
            for (int r = 0; r < RegionCount; r++)
            {
                double w = RegionWeight(r), electorate = RegionAudience(r);
                if (w <= 0.0) { continue; }
                for (int p = 0; p < n; p++)
                {
                    if (!StandsIn(p, r)) { continue; }
                    var e = new double[classes];
                    for (int slot = 0; slot < OwnSlots; slot++) { RegionalSlot(p, slot, r, out double se, out _); e[(int)SlotClass(slot)] += se; }
                    var made = new double[classes];
                    var ae = new double[n][];
                    double[][] attacks = _attackExposure[p];
                    for (int t = 0; attacks != null && t < attacks.Length; t++)
                    {
                        if (attacks[t] == null) { continue; }
                        ae[t] = new double[classes];
                        for (int c = 0; c < classes; c++) { RegionalAttack(p, t, (ContactClass)c, r, out double x, out _); ae[t][c] = x; made[c] += x; }
                    }
                    for (int c = 0; c < classes; c++)
                    {
                        double all = e[c] + made[c];
                        if (all <= 0.0) { continue; }
                        double cover = CampaignPersuasion.Coverage(all, electorate);
                        double upper = CampaignPersuasion.UpperOf((ContactClass)c);
                        bounds[p] += w * upper * cover * (e[c] / all);
                        for (int t = 0; t < n; t++)
                        {
                            if (ae[t] == null || ae[t][c] <= 0.0 || !StandsIn(t, r)) { continue; }
                            bounds[t] += w * upper * cover * (ae[t][c] / all);
                        }
                    }
                }
            }
            return bounds;
        }

        /// <summary>Regional 5a: the absorption each region's effects are laid on - `CampaignPersuasion.BalancedAbsorption` over the parties standing there
        /// (their shares those handed, every reach one); built once per set of shares and reused (the night's attribution lays many coalitions on one).</summary>
        public sealed class RegionalAbsorption
        {
            public CampaignPersuasion.Absorption[] Regions;
            public double[][] Shares;
            /// <summary>The first region whose balancing could not be met (the proportional absorption standing in there), and why; null when every one was.</summary>
            public string Fallback;
        }

        /// <summary>Regional 5a: <see cref="RegionalAbsorption"/> for <paramref name="shares"/>; null for a national tally.</summary>
        public RegionalAbsorption AbsorptionFor(double[] shares)
        {
            if (_regionAudience == null || shares == null) { return null; }
            int n = shares.Length, k = _regionAudience.Length;
            var a = new RegionalAbsorption { Regions = new CampaignPersuasion.Absorption[k], Shares = new double[k][] };
            for (int r = 0; r < k; r++)
            {
                var masked = new double[n];
                var reach = new double[n];
                for (int p = 0; p < n; p++) { bool here = StandsIn(p, r); masked[p] = here ? shares[p] : 0.0; reach[p] = here ? 1.0 : 0.0; }
                a.Shares[r] = masked;
                a.Regions[r] = CampaignPersuasion.BalancedAbsorption(masked, reach, null);
                if (a.Fallback == null && a.Regions[r].Fallback != null && RegionWeight(r) > 0.0) { a.Fallback = "region " + r + ": " + a.Regions[r].Fallback; }
            }
            return a;
        }

        /// <summary>
        /// Regional 5a (K147): each region's movement per party ([region][party], fractions of the region's vote) - its effects (<see cref="RegionEffects"/>)
        /// laid on the region's absorption (`CampaignPersuasion.Moved`, before any floor), the attacks moved from target to attacker there. Each row sums to
        /// zero; a party with no list in a region has a zero there. Null for a national tally.
        /// </summary>
        public double[][] RegionalShifts(double[] shares, double norm, Func<int, int, bool> include = null, Func<int, int, int, bool> includeAttack = null, RegionalAbsorption absorption = null)
        {
            if (_regionAudience == null || shares == null) { return null; }
            absorption ??= AbsorptionFor(shares);
            var shifts = new double[_regionAudience.Length][];
            for (int r = 0; r < shifts.Length; r++)
            {
                double[] e = RegionEffects(r, norm, include, includeAttack, out double[] loss, out double[] gain);
                shifts[r] = CampaignPersuasion.Moved(absorption.Shares[r], e, absorption.Regions[r], loss, gain);
            }
            return shifts;
        }

        /// <summary>Regional 5a: the national movement - each region's movement weighted by its audience (the identity K147 asks the count to keep).</summary>
        public double[] NationalShift(double[][] shifts)
        {
            if (shifts == null) { return null; }
            var national = new double[_persuasion.Length];
            for (int r = 0; r < shifts.Length; r++)
            {
                double w = RegionWeight(r);
                for (int p = 0; p < national.Length && p < shifts[r].Length; p++) { national[p] += w * shifts[r][p]; }
            }
            return national;
        }

        /// <summary>Regional 5a: <paramref name="shares"/> moved by the campaign, region by region - the national movement laid on them (`CampaignPersuasion.ApplyShift`: a share pushed below zero held at zero, the vector renormalised).</summary>
        public double[] ApplyRegional(double[] shares, double norm, Func<int, int, bool> include = null, Func<int, int, int, bool> includeAttack = null, RegionalAbsorption absorption = null) =>
            CampaignPersuasion.ApplyShift(shares, NationalShift(RegionalShifts(shares, norm, include, includeAttack, absorption)));

        /// <summary>Regional 5a: one party's own tally per region and contact class - exposure, quality and its attacks' exposure ([region][class]) - what the AI
        /// values a contact against where it lands (`CampaignAi`); null arrays for a national tally.</summary>
        public void RegionalOwnByClass(int p, out double[][] exposure, out double[][] quality, out double[][] attack)
        {
            exposure = null; quality = null; attack = null;
            if (_regionAudience == null) { return; }
            int k = _regionAudience.Length, classes = CampaignPersuasion.ClassCount;
            exposure = new double[k][]; quality = new double[k][]; attack = new double[k][];
            for (int r = 0; r < k; r++)
            {
                exposure[r] = new double[classes]; quality[r] = new double[classes]; attack[r] = new double[classes];
                for (int slot = 0; slot < OwnSlots; slot++)
                {
                    RegionalSlot(p, slot, r, out double e, out double q);
                    int c = (int)SlotClass(slot);
                    exposure[r][c] += e; quality[r][c] += q;
                }
                double[][] made = _attackExposure[p];
                if (made == null) { continue; }
                for (int t = 0; t < made.Length; t++)
                {
                    if (made[t] == null) { continue; }
                    for (int slot = 0; slot < OwnSlots; slot++) { RegionalAttackSlot(p, t, slot, r, out double xe, out _); attack[r][(int)SlotClass(slot)] += xe; }
                }
            }
        }
        /// <summary>
        /// Regional 5a (K147): what the AI reads of its own regional tally - per region and class its exposure, its quality and its attacks' exposure, each
        /// region's weight in the national movement and its audience, and the share of a national act of its that lands there. Its own books, as the national
        /// <c>OwnExposureByClass</c> is; nothing about another party.
        /// </summary>
        public sealed class RegionalOwn
        {
            public double[][] Exposure, Quality, Attack;
            public double[] Weight, Audience, NationalShare;
            public bool[] Stands;

            /// <summary>
            /// The run's own regional class-effect difference for one contact of class <paramref name="c"/> (its exposure and quality as the run would tally them,
            /// <paramref name="alpha"/> the share of it aimed at an opponent, K66): landing in <paramref name="region"/>, that region's effect with the contact less
            /// without, times its weight; a national act (−1), the same summed over every region where the party stands, the contact spread by audience as the
            /// tally spreads it. In points of the national vote, as `CampaignAi`'s national valuation is; 0 where the party has no list in the region.
            /// </summary>
            public double ValuePp(ContactClass c, double exposure, double quality, double alpha, int region, double norm)
            {
                if (region >= 0) { return region < Weight.Length && Stands[region] ? Weight[region] * Difference(region, c, exposure, quality, alpha, norm) : 0.0; }
                double sum = 0.0;
                for (int r = 0; r < Weight.Length; r++)
                {
                    if (!Stands[r] || NationalShare[r] <= 0.0) { continue; }
                    sum += Weight[r] * Difference(r, c, exposure * NationalShare[r], quality * NationalShare[r], alpha, norm);
                }
                return sum;
            }

            private double Difference(int r, ContactClass c, double exposure, double quality, double alpha, double norm)
            {
                int k = (int)c;
                double e0 = Exposure[r][k], q0 = Quality[r][k], a0 = Attack[r][k], electorate = Audience[r];
                if (alpha > 0.0 || a0 > 0.0)
                {
                    return CampaignPersuasion.SharedClassEffectPp(c, e0 + exposure * (1.0 - alpha), q0 + quality * (1.0 - alpha), a0 + exposure * alpha, electorate, norm)
                           - CampaignPersuasion.SharedClassEffectPp(c, e0, q0, a0, electorate, norm);
                }
                return CampaignPersuasion.ClassEffectPp(c, e0 + exposure, q0 + quality, electorate, norm) - CampaignPersuasion.ClassEffectPp(c, e0, q0, electorate, norm);
            }
        }

        /// <summary>Regional 5a: <see cref="RegionalOwn"/> for party <paramref name="p"/>; null for a national tally.</summary>
        public RegionalOwn RegionalOwnView(int p)
        {
            if (_regionAudience == null) { return null; }
            RegionalOwnByClass(p, out double[][] e, out double[][] q, out double[][] a);
            int k = _regionAudience.Length;
            var view = new RegionalOwn { Exposure = e, Quality = q, Attack = a, Weight = new double[k], Audience = new double[k], NationalShare = new double[k], Stands = new bool[k] };
            for (int r = 0; r < k; r++) { view.Weight[r] = RegionWeight(r); view.Audience[r] = RegionAudience(r); view.NationalShare[r] = NationalShareIn(p, r); view.Stands[r] = StandsIn(p, r); }
            return view;
        }
    }
}
