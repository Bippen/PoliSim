using UnityEngine;

namespace PoliSim.Data
{
    /// <summary>
    /// P5-B7 (2026-09-05): POTENTIAL OUTPUT READS THE WORKFORCE. Before this pass EconomyState.PotentialGDP compounded at
    /// Country.PotentialGrowthRate - a seeded trend plus two ceilinged policy adjustments - whatever the working-age
    /// cohort did, so a shrinking country kept its output and (since P5-B3 put the tax bases on the wage bill) lost its
    /// revenue against it: POTENTIAL_PREMISE.md measured Poland's potential at 31× its seed after a century while its
    /// labour × productivity read 17×, Italy 2.2× against 1.4×. Now potential is its two factors:
    ///
    /// <para>potential = the seed's potential × (labour input ÷ the seed's labour input) × the productivity index,
    /// where the LABOUR INPUT is the 20–64 cohort (F2's substrate) × the participation rate × (1 − the natural rate of
    /// unemployment) - employment at the natural rate, read each day - and the PRODUCTIVITY INDEX compounds daily at
    /// the ledger's trend (Country.ProductivityTrendGrowth: the seeded trend of labour productivity per hour, SOURCED
    /// in WorldFactory, plus the infrastructure and sector adjustments that were always productivity channels, Q3).
    /// Country.PotentialGrowthRate becomes what it always claimed to be, the growth of potential, and is DERIVED once a
    /// turn: (1 + trend/100) × (labour now ÷ labour a turn ago) − 1. Okun's growth gap, the AI's budget rule and every
    /// other reader take that derived rate. A country without captured seeds (a save from before this pass) keeps the
    /// old compounding, stated at the call site.</para>
    /// </summary>
    public static class PotentialOutput
    {
        /// <summary>Employment at the natural rate: the 20–64 cohort × participation × (1 − NAIRU). Units are irrelevant - only ratios are used.</summary>
        public static float LabourInput(Country country)
        {
            return SpendingDrivers.Level(SpendingDriver.WorkingAge20To64, country)
                   * Mathf.Clamp(country.State.LaborForceParticipationRate, 0f, 100f) / 100f
                   * Mathf.Clamp(100f - country.NaturalUnemploymentRate, 0f, 100f) / 100f;
        }

        /// <summary>True when the country carries the seeds this potential is built on (Country.CaptureStructuralBases wrote them).</summary>
        public static bool HasSeeds(Country country) => country.PotentialGdpSeed > 0f && country.PotentialLabourSeed > 0f && country.PotentialProductivityIndex > 0f;

        /// <summary>The potential the factors give today: seed potential × labour ratio × productivity index.</summary>
        public static float Potential(Country country)
        {
            float labour = LabourInput(country);
            float labourRatio = country.PotentialLabourSeed > 0f && labour > 0f ? labour / country.PotentialLabourSeed : 1f;
            return country.PotentialGdpSeed * labourRatio * country.PotentialProductivityIndex;
        }

        /// <summary>The derived annual growth of potential, in percent: the trend compounded with last turn's labour growth.</summary>
        public static float DerivedGrowthPercent(Country country, float labourAtLastTurn)
        {
            float labour = LabourInput(country);
            float labourGrowth = labourAtLastTurn > 0f && labour > 0f ? labour / labourAtLastTurn : 1f;
            return ((1f + country.ProductivityTrendGrowth / 100f) * labourGrowth - 1f) * 100f;
        }
    }
}
