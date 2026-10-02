using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// §756 (Elias's ruling A5, 2026-10-02): <i>"Minimum wage: Sweden Off (no statutory minimum; collective agreements). Italy Off, for the same reason.
    /// Germany, France, Poland and the USA carry their statutory rates, dated and sourced by read."</i> THE STATUTORY RATES, one row per step as
    /// `ElectionsData/rules/minimum_wage_rates.csv` holds it - each step's instrument read from the page saved under `ElectionsData/rules/raw/`
    /// (`minimum_wage_rates.md`); `MinimumWageRatesCheck` holds this table to the file, row by row. Sweden and Italy carry none
    /// (<see cref="Country.MinimumWageImplemented"/> false - Regeringskansliet FPM 2020/21:FPM41; Eurostat, 1 July 2026).
    /// <para>The model's lever is the Kaitz index (<see cref="Country.MinimumWagePercentOfMedian"/>); the statutory rate in force on the game's date is
    /// what that index stands for at its seed. The Labour dial carries it beside the index, and a draft's rate scaled by the draft over the seed's
    /// index - the premise, stated: the statutory steps track the median wage (a premise, not read from
    /// the instruments).</para>
    /// </summary>
    public static class MinimumWageRates
    {
        public enum Per { Hour, Month }

        public readonly struct Step
        {
            public readonly CountryId Country;
            public readonly DateTime From;
            public readonly float Rate;
            public readonly string Currency;
            public readonly Per Unit;
            public readonly string Instrument;
            public Step(CountryId country, DateTime from, float rate, string currency, Per unit, string instrument)
            {
                Country = country; From = from; Rate = rate; Currency = currency; Unit = unit; Instrument = instrument;
            }
        }

        private static DateTime D(int y, int m, int d) => new DateTime(y, m, d);

        /// <summary>SOURCED - every step of `minimum_wage_rates.csv` marked SOURCED, in its order (Poland's monthly and hourly figures both).</summary>
        public static readonly IReadOnlyList<Step> Steps = new[]
        {
            new Step(CountryId.Germany, D(2022, 10, 1), 12.00f, "EUR", Per.Hour, "MiLoG § 1 Abs. 2"),
            new Step(CountryId.Germany, D(2024, 1, 1), 12.41f, "EUR", Per.Hour, "MiLoV4 § 1 Nr. 1"),
            new Step(CountryId.Germany, D(2025, 1, 1), 12.82f, "EUR", Per.Hour, "MiLoV4 § 1 Nr. 2"),
            new Step(CountryId.Germany, D(2026, 1, 1), 13.90f, "EUR", Per.Hour, "MiLoV5 § 1 Nr. 1"),
            new Step(CountryId.Germany, D(2027, 1, 1), 14.60f, "EUR", Per.Hour, "MiLoV5 § 1 Nr. 2"),
            new Step(CountryId.France, D(2023, 1, 1), 11.27f, "EUR", Per.Hour, "Décret n° 2022-1608 du 22 décembre 2022"),
            new Step(CountryId.France, D(2023, 5, 1), 11.52f, "EUR", Per.Hour, "Arrêté du 26 avril 2023"),
            new Step(CountryId.France, D(2024, 1, 1), 11.65f, "EUR", Per.Hour, "Décret n° 2023-1216 du 20 décembre 2023"),
            new Step(CountryId.France, D(2024, 11, 1), 11.88f, "EUR", Per.Hour, "Décret n° 2024-951 du 23 octobre 2024"),
            new Step(CountryId.France, D(2026, 1, 1), 12.02f, "EUR", Per.Hour, "Décret n° 2025-1228 du 17 décembre 2025"),
            new Step(CountryId.France, D(2026, 6, 1), 12.31f, "EUR", Per.Hour, "Arrêté du 22 mai 2026"),
            new Step(CountryId.Poland, D(2023, 1, 1), 3490f, "PLN", Per.Month, "Dz.U. 2022 poz. 1952, § 1"),
            new Step(CountryId.Poland, D(2023, 1, 1), 22.80f, "PLN", Per.Hour, "Dz.U. 2022 poz. 1952, § 2"),
            new Step(CountryId.Poland, D(2023, 7, 1), 3600f, "PLN", Per.Month, "Dz.U. 2022 poz. 1952, § 3"),
            new Step(CountryId.Poland, D(2023, 7, 1), 23.50f, "PLN", Per.Hour, "Dz.U. 2022 poz. 1952, § 4"),
            new Step(CountryId.Poland, D(2024, 1, 1), 4242f, "PLN", Per.Month, "Dz.U. 2023 poz. 1893, § 1"),
            new Step(CountryId.Poland, D(2024, 1, 1), 27.70f, "PLN", Per.Hour, "Dz.U. 2023 poz. 1893, § 2"),
            new Step(CountryId.Poland, D(2024, 7, 1), 4300f, "PLN", Per.Month, "Dz.U. 2023 poz. 1893, § 3"),
            new Step(CountryId.Poland, D(2024, 7, 1), 28.10f, "PLN", Per.Hour, "Dz.U. 2023 poz. 1893, § 4"),
            new Step(CountryId.Poland, D(2025, 1, 1), 4666f, "PLN", Per.Month, "Dz.U. 2024 poz. 1362, § 1"),
            new Step(CountryId.Poland, D(2025, 1, 1), 30.50f, "PLN", Per.Hour, "Dz.U. 2024 poz. 1362, § 2"),
            new Step(CountryId.Poland, D(2026, 1, 1), 4806f, "PLN", Per.Month, "Dz.U. 2025 poz. 1242, § 1"),
            new Step(CountryId.Poland, D(2026, 1, 1), 31.40f, "PLN", Per.Hour, "Dz.U. 2025 poz. 1242, § 2"),
            new Step(CountryId.Poland, D(2027, 1, 1), 4950f, "PLN", Per.Month, "Dz.U. 2026 poz. 1213, § 1"),
            new Step(CountryId.Poland, D(2027, 1, 1), 32.30f, "PLN", Per.Hour, "Dz.U. 2026 poz. 1213, § 2"),
            new Step(CountryId.USA, D(2009, 7, 24), 7.25f, "USD", Per.Hour, "29 U.S.C. 206(a)(1)(C)"),
        };

        /// <summary>The headline step in force on <paramref name="date"/>: the latest whose day has come - Poland's MONTHLY minimum (its headline; the
        /// hourly rate is for contracts of mandate), every other country's hourly rate. False for Sweden and Italy, and before a country's first step held.</summary>
        public static bool TryInForce(CountryId country, DateTime date, out Step step)
        {
            step = default;
            bool found = false;
            Per headline = country == CountryId.Poland ? Per.Month : Per.Hour;
            foreach (Step s in Steps)
            {
                if (s.Country != country || s.Unit != headline || s.From > date) { continue; }
                if (!found || s.From >= step.From) { step = s; found = true; }
            }
            return found;
        }
    }
}
