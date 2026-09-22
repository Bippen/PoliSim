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
    /// where the LABOUR INPUT is employment - the labour force × (1 − the unemployment rate), read each day - and the
    /// PRODUCTIVITY INDEX compounds daily at the ledger's trend (Country.ProductivityTrendGrowth: the seeded trend of
    /// labour productivity per hour, SOURCED in WorldFactory, plus the infrastructure and sector adjustments that were
    /// always productivity channels, Q3). Country.PotentialGrowthRate becomes what it always claimed to be, the growth
    /// of potential, and is DERIVED once a turn: (1 + trend/100) × (labour now ÷ labour a turn ago) − 1. Okun's growth
    /// gap, the AI's budget rule and every other reader take that derived rate. A country without captured seeds (a
    /// save from before this pass) keeps the old compounding, stated at the call site.</para>
    ///
    /// <para><b>PN-3 (2026-09-16, §522; DS-3c §474): THE LABOUR FORCE IS THE PYRAMID'S, NOT A WINDOW'S.</b> Until this pass the
    /// labour force was the 20–64 cohort × the state's participation rate. The state's rate is a 15-AND-OVER rate by
    /// construction - `ParticipationRateTable.StructuralRate` is Σ(band × sourced rate by age) over the population aged 15
    /// and over, and `MacroSystem.ApplyLaborForceParticipationRate` reverts the state's rate toward it - so the window
    /// multiplied a count on one base by a rate on another, and an ageing pyramid moved both the same way: two ageing
    /// effects on one input. `LabourInputWindowDiagnostic` measured it before the re-form - after 25 years of the model's
    /// own ageing the window sat 2 to 11 % below the labour force the sourced rates by age imply (Italy −11.4 %, Poland
    /// −10.0, Germany −7.0, France −6.4, the USA −3.2, Sweden −2.2), while the levers' deviation ran the other way. The
    /// labour force is now the 15+ population × the state's rate: that is Σ(band × sourced rate) EXACTLY while no lever has
    /// moved participation (the table's own definition) and the same labour force scaled by the levers' deviation
    /// otherwise - the 15–19 and 65+ bands enter at their sourced rates, and a participation response of the bands a
    /// pension age crosses (PN-1's open item) reaches this input through the table it would move. Potential's 20–64
    /// window, DS-3c's seam, is closed; the wage bill's base (`TaxBases`, WageBill) READS THIS INPUT since PN-3b (§577) and was its own
    /// family (PN-3b), measured in the same diagnostic.</para>
    /// </summary>
    public static class PotentialOutput
    {
        /// <summary>PN-3 (§522): the population the participation rate is defined on - ages 15 and over; the state's population where there is no pyramid.</summary>
        public static float Population15Plus(Country country)
            => country.Cohorts != null ? country.Cohorts.InAgeRange(15, 999) : country.State.Population;

        /// <summary>PN-3 (§522): the labour force - the 15+ population × the state's participation rate, which is the labour force the pyramid implies at the
        /// sourced rates by age (Σ band × rate) scaled by the levers' deviation from the structural rate. Units are irrelevant - only ratios are used.</summary>
        public static float LabourForce(Country country)
            => Population15Plus(country) * Mathf.Clamp(country.State.LaborForceParticipationRate, 0f, 100f) / 100f;

        /// <summary>FT-7, the second seat (2026-09-08, §394): EMPLOYMENT - the labour force × (1 − the unemployment rate). Before this seat the
        /// input read (1 − NAIRU), employment at the natural rate, so a rise in participation was potential the same year whatever happened to the jobs
        /// (§372, §389); now a supply shock reaches potential only as it is employed - the first seat (§391) puts it in unemployment on impact and absorbs it
        /// at SELMA's rate, and this seat lets potential follow the absorption. PN-3 (§522): the labour force is <see cref="LabourForce"/>, the pyramid's,
        /// where it was the 20–64 cohort × the 15+ rate. Units are irrelevant - only ratios are used.</summary>
        public static float LabourInput(Country country)
        {
            // ONE expression, the shape §394 left it in with the pyramid's population in the window's place - not LabourForce × (1 − U): Mono rounds a
            // stored intermediate where it does not round inside an expression (the numeric-inertness rule), and the attribution probe that maps the
            // population back to the 20–64 cohort must reproduce the old arithmetic to the bit.
            return Population15Plus(country)
                   * Mathf.Clamp(country.State.LaborForceParticipationRate, 0f, 100f) / 100f
                   * Mathf.Clamp(100f - country.State.Unemployment, 0f, 100f) / 100f;
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
