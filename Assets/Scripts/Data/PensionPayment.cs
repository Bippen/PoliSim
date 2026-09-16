using System;

namespace PoliSim.Data
{
    /// <summary>
    /// PN-2 (2026-09-16, the plan's S6; opened §474): **the pension payment as a READOUT.** The model has one pension line and a pyramid,
    /// so the average benefit it implies is that line over the people the statute retires, and the replacement rate is that benefit over
    /// the mean income the tax schedules already read. Nothing here is a lever: the line is headcount × benefit and the player moves the
    /// line, not the payment.
    ///
    /// <para><b>ONE ACCESSOR, READ BY BOTH SITES.</b> The row on the Budget page and `PensionPaymentDiagnostic`'s seed gate read these
    /// three methods; the arithmetic exists once, so the figure on the screen is the figure the gate is measured against.</para>
    ///
    /// <para>⚠ <b>The sub-band assumption, stated where it is made.</b> The pyramid is five-year bands (`PopulationCohorts`), so a
    /// statutory age inside a band takes that band's fraction ((band end − age) ⁄ 5) and every band above it whole - people spread
    /// evenly inside a cohort. That is the standard approximation and it is standardly wrong at the ends of the pyramid; DS-3b required
    /// it said out loud rather than discovered, and this is where it is said.</para>
    ///
    /// <para>⚠ <b>What the denominator is NOT.</b> It is the cohorts at or above the statutory age - not the beneficiaries a pension
    /// system actually pays, which include survivors, the disabled and early retirements. Against ESSPROS's beneficiary counts the
    /// measured gap is between 0.80 and 1.07 (§518's gate), so the proxy is close but it is a proxy, and a comparison that forgets which
    /// denominator it is using will read a definitional gap as an error.</para>
    /// </summary>
    public static class PensionPayment
    {
        /// <summary>The people at or above the statutory age, MILLIONS: every band above it whole, the straddled band by its fraction.</summary>
        public static double PensionersMillions(Country country, float age)
        {
            if (country?.Cohorts?.Counts == null) { return 0; }
            double total = 0;
            for (int b = 0; b < PopulationCohorts.CohortCount; b++)
            {
                int start = b * PopulationCohorts.CohortWidth;
                if (age <= start) { total += country.Cohorts.Counts[b]; continue; }
                if (b == PopulationCohorts.OpenBandIndex) { continue; }   // the open band is only reached by the branch above
                int end = start + PopulationCohorts.CohortWidth;
                if (age >= end) { continue; }
                total += country.Cohorts.Counts[b] * ((end - age) / (double)PopulationCohorts.CohortWidth);
            }
            return total;
        }

        /// <summary>The pension line itself - the model's one of them (`SpendingCategory.SocialSecurity`), in the book's billions.</summary>
        public static double LineBillions(Country country)
        {
            if (country?.SpendingLines == null) { return 0; }
            foreach (SpendingLine l in country.SpendingLines) { if (l.Category == SpendingCategory.SocialSecurity) { return l.Amount; } }
            return 0;
        }

        /// <summary>The average benefit the line implies, the book's dollars per pensioner per year. Zero where the pyramid or the line is empty.</summary>
        public static double AverageBenefitPerYear(Country country, int year)
        {
            if (country == null) { return 0; }
            float age = PensionAgeStatute.Has(country.Id) ? PensionAgeStatute.AgeInForce(country.Id, year) : 0f;
            if (age <= 0f) { return 0; }
            double heads = PensionersMillions(country, age);
            return heads > 0 ? LineBillions(country) * 1e9 / (heads * 1e6) : 0;
        }

        /// <summary>
        /// The replacement rate: the average benefit over the mean income, both in the book's dollars. The mean income is the one the tax
        /// schedules read (`TaxSchedule.AverageIncome`, the cohorts' own log-normals at this boundary), converted out of the statute's
        /// currency, so the row and the schedule row are reading one income and not two.
        /// </summary>
        public static double ReplacementRate(Country country, int year)
        {
            if (country == null) { return 0; }
            double benefit = AverageBenefitPerYear(country, year);
            if (benefit <= 0) { return 0; }
            double nationalPerUsd = Math.Max(1e-9, EnergyLayer.NationalPerUsd(country.Id));
            double meanIncomeUsd = TaxSchedule.AverageIncome(country, TaxSchedule.IncomeScale(country)) / nationalPerUsd;
            return meanIncomeUsd > 0 ? benefit / meanIncomeUsd : 0;
        }
    }
}
