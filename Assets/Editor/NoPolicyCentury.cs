using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **One no-policy century per player, run once per process and read by every diagnostic that needs it (2026-09-17,
    /// `COMPLETED.md` §525).**
    ///
    /// <para><b>Why it exists.</b> `HealthTrendDiagnostic` and `InfrastructureReadoutDiagnostic` each advanced the SAME six
    /// worlds - seed 777, the default world, each country in turn as the player, no decision on any turn, a hundred years -
    /// and each ran Sweden's first twenty years again on its own, so most of each diagnostic's simulated years were a copy
    /// of the other's (the cost measured in §525). The loop here is the one both ran, verbatim, and it records every year's
    /// readouts both of them read, so the second reader gets the first reader's run instead of a copy of it.</para>
    ///
    /// <para>⚠ <b>The shared run is checked against an independent one.</b> `InfrastructureReadoutDiagnostic` still advances
    /// Sweden's untouched twenty years by itself and fails when that differs by a bit from the shared run's year twenty - a
    /// defect in this cache, or state leaking between the diagnostics that ran in between, cannot pass silently.</para>
    /// </summary>
    public static class NoPolicyCentury
    {
        /// <summary>The seed both diagnostics seeded with.</summary>
        public const int Seed = 777;

        /// <summary>The horizon the longer reader asks for; a shorter reader reads a prefix of it.</summary>
        public const int Years = 100;

        /// <summary>One year's readouts of the player's country, taken after that year's turn.</summary>
        public sealed class Year
        {
            public float RoadQuality;
            public float TreatableMortality;
            public float TrendIndex;
            public float DeathRate;
            public float Population;
            public float NetMigrationRate;
            public float LifeExpectancy;
        }

        private static readonly Dictionary<CountryId, Year[]> Runs = new Dictionary<CountryId, Year[]>();

        /// <summary>The century with <paramref name="player"/> as the player - element <c>y - 1</c> is year <c>y</c> - run on the first ask and handed out after.</summary>
        public static IReadOnlyList<Year> For(CountryId player)
        {
            if (!Runs.TryGetValue(player, out Year[] run))
            {
                run = Advance(player);
                Runs[player] = run;
            }

            return run;
        }

        /// <summary>The loop the two diagnostics ran for an untouched country, recording every year.</summary>
        private static Year[] Advance(CountryId player)
        {
            SimulationRandom.Seed(Seed);
            using System.IDisposable epoch = SimulationManager.EpochScope();   // PS-1 (§618): the century opens on the player's own start, as the game does, and the epoch is put back after
            PoliSim.Elections.WorldClock.ApplyStart(player);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("NOPOLICYCENTURY");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = player;
                Country c = world.GetCountry(player);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var years = new Year[Years];
                for (int year = 1; year <= Years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    decisions[player] = d;
                    sim.AdvanceTurn(decisions);
                    years[year - 1] = new Year
                    {
                        RoadQuality = c.State.RoadQuality,
                        TreatableMortality = c.State.TreatableMortality,
                        TrendIndex = c.Health.TrendIndex,
                        DeathRate = c.State.DeathRate,
                        Population = c.State.Population,
                        NetMigrationRate = c.State.NetMigrationRate,
                        LifeExpectancy = c.State.LifeExpectancy,
                    };
                }

                return years;
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
