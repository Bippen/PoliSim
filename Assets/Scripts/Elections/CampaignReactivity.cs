using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-C2 — what every party can SEE of every other party's campaign: the public record of
    /// local actions per region and of attacks, by attacker, decayed on a news half-life. PURE,
    /// WIRED TO NOTHING (R-N2); the AI campaign keeps one and hands each party its own reading.
    ///
    /// §36 draws the line: a rally in Blekinge is public (the press was there), the doors an
    /// office knocked are not, and neither is any party's true standing. So the record holds
    /// COUNTS of visible acts — a rally, a town hall, a door-to-door day (the canvassers are seen
    /// even if the doors are not), an attack — weighted by the act's size and decayed with
    /// <see cref="HalfLifeDays"/> so last week's rally still counts and last month's does not. An
    /// AI reads a region's PRESSURE (every other party's activity there), its PUSH (the largest
    /// single opponent's concentration there — one party working a region is a threat in a way
    /// that eight parties passing through is not) and the ATTACKS aimed at it, by attacker; its
    /// personality decides what to do with the reading (<see cref="PersonalityProfile.Reactivity"/>):
    /// the professional defends a contested region and answers an attack, the chaotic party never
    /// looks. No truth can be expressed here — the harness asserts it by reflection as it does for
    /// <see cref="AiView"/>.
    /// </summary>
    public sealed class PublicActivity
    {
        /// <summary>[AUTHORED-DRAFT] how long a visible act stays in the public mind: half gone in a week.</summary>
        public const double HalfLifeDays = 7.0;

        private readonly double[][] _local;    // [party][region]
        private readonly double[][] _attacks;  // [attacker][target]

        public PublicActivity(int partyCount, int regionCount)
        {
            if (partyCount <= 0 || regionCount <= 0) { throw new ArgumentException("parties and regions"); }
            _local = new double[partyCount][];
            _attacks = new double[partyCount][];
            for (int p = 0; p < partyCount; p++)
            {
                _local[p] = new double[regionCount];
                _attacks[p] = new double[partyCount];
            }
        }

        public int PartyCount => _local.Length;
        public int RegionCount => _local[0].Length;

        /// <summary>A visible local act by a party in a region, weighted (1 = one rally-sized act).</summary>
        public void ObserveLocal(int party, int region, double weight = 1.0)
        {
            if (weight <= 0.0) { return; }
            _local[party][region] += weight;
        }

        /// <summary>A visible attack by one party on another (a negative message aimed at it). An attack on oneself is not an act.</summary>
        public void ObserveAttack(int attacker, int target, double weight = 1.0)
        {
            if (weight <= 0.0 || attacker == target) { return; }
            _attacks[attacker][target] += weight;
        }

        /// <summary>The day closes: everything decays on the half-life.</summary>
        public void Decay(double days = 1.0)
        {
            double f = Math.Pow(0.5, days / HalfLifeDays);
            for (int p = 0; p < _local.Length; p++)
            {
                for (int r = 0; r < _local[p].Length; r++) { _local[p][r] *= f; }
                for (int q = 0; q < _attacks[p].Length; q++) { _attacks[p][q] *= f; }
            }
        }

        /// <summary>What a party can see of one region: every OTHER party's decayed local activity there.</summary>
        public double PressureOn(int region, int exceptParty)
        {
            double s = 0.0;
            for (int p = 0; p < _local.Length; p++) { if (p != exceptParty) { s += _local[p][region]; } }
            return s;
        }

        /// <summary>The pressure on every region as one party sees it (a fresh array - the party's own reading).</summary>
        public double[] PressureSeenBy(int party)
        {
            var r = new double[RegionCount];
            for (int region = 0; region < r.Length; region++) { r[region] = PressureOn(region, party); }
            return r;
        }

        /// <summary>
        /// The PUSH on every region as one party sees it: the largest visible activity any ONE
        /// opponent has concentrated there. A region eight parties pass through is busy; a region
        /// one party works every day is contested, and that is what a defence answers.
        /// </summary>
        public double[] PushSeenBy(int party)
        {
            var push = new double[RegionCount];
            for (int region = 0; region < push.Length; region++)
            {
                double most = 0.0;
                for (int p = 0; p < _local.Length; p++)
                {
                    if (p != party && _local[p][region] > most) { most = _local[p][region]; }
                }

                push[region] = most;
            }

            return push;
        }

        /// <summary>The decayed attacks aimed at a party, by attacker - which it can see, being the one attacked (a fresh array).</summary>
        public double[] AttackersOf(int party)
        {
            var by = new double[PartyCount];
            for (int p = 0; p < _attacks.Length; p++) { by[p] = _attacks[p][party]; }
            return by;
        }

        /// <summary>The decayed count of attacks on a party from every quarter.</summary>
        public double AttacksOn(int party)
        {
            double s = 0.0;
            for (int p = 0; p < _attacks.Length; p++) { s += _attacks[p][party]; }
            return s;
        }

        /// <summary>The decayed attacks one party has aimed at another.</summary>
        public double AttacksBy(int attacker, int target) => _attacks[attacker][target];

        public double Local(int party, int region) => _local[party][region];
    }
}
