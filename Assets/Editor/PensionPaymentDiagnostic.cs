using System;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PN-2 (2026-09-16, the plan's S6): **the pension payment as a readout, MEASURED FIRST.** The model has one pension line
    /// (`SpendingCategory.SocialSecurity`) and a pyramid; the average benefit it implies is that line over the people at or above
    /// the statutory age, and the replacement rate is that benefit over the mean income the schedules already read.
    ///
    /// <para><b>This run measures and asserts NOTHING yet.</b> It prints, per country: the line, the headcount the statute's age
    /// implies, the benefit that quotient gives in the book's dollars and in the source's own euros, the sourced average pension
    /// (ESSPROS 2022) beside it, and the replacement rate against Eurostat's aggregate replacement ratio (`ilc_pnp3` 2024). The
    /// premise of PN-2's gate is whether those two pairs are within a stated tolerance or DIVERGENT - and that is what this reads.</para>
    ///
    /// <para><b>The headcount's sub-band assumption, stated.</b> The pyramid is five-year bands, so a statutory age inside a band
    /// takes that band's fraction ((band end − age) ⁄ 5) and every band above it whole - the uniform-within-cohort approximation
    /// `PopulationCohorts` names, applied here and said out loud rather than discovered later (DS-3b's own rule).</para>
    ///
    /// <para><b>What the two sources measure, which is not the same thing.</b> ESSPROS is expenditure over beneficiaries, so it
    /// counts everyone drawing a pension including survivors and the disabled and those below the statutory age; this readout's
    /// denominator is the cohorts at or above the age. `ilc_pnp3` is the median individual gross pension of 65–74 over the median
    /// gross earnings of 50–59; this readout's is a mean over a mean. Both differences are printed as the reason a gap is not
    /// automatically an error.</para>
    /// </summary>
    public static class PensionPaymentDiagnostic
    {
        /// <summary>
        /// ESSPROS 2022 as `ElectionsData/pensions/average_pension_2022.csv` carries it (§475): expenditure in million euro and beneficiaries in persons,
        /// for BOTH of the source's definitions - old-age pensions (old age + anticipated + partial) and all pension types. The USA is not in ESSPROS and
        /// has no row here; its own source (OECD's net replacement rate) is a different quantity and is not used as an expenditure gate.
        /// </summary>
        private static readonly (CountryId Id, double AllTypesExpMio, double OldAgeExpMio, double AllTypesBen, double OldAgeBen)[] SourcedEssprosEur =
        {
            (CountryId.Germany, 462194.48, 364356.91, 23507818, 19860928),
            (CountryId.France, 393151.24, 324367.32, 20556173, 17040084),
            (CountryId.Italy, 309254.00, 249397.00, 15694751, 12426492),
            (CountryId.Poland, 66799.09, 53916.32, 10505618, 8143281),
            (CountryId.Sweden, 58792.05, 54271.18, 2818737, 2520552),
            (CountryId.USA, 0, 0, 0, 0),
        };

        /// <summary>Eurostat `ilc_pnp3` 2024, sex T: the aggregate replacement ratio (`ElectionsData/pensions/replacement_ratio_2024.csv`, §475).</summary>
        private static readonly (CountryId Id, double Ratio)[] SourcedReplacementRatio =
        {
            (CountryId.Germany, 0.49), (CountryId.France, 0.61), (CountryId.Italy, 0.79), (CountryId.Poland, 0.60), (CountryId.Sweden, 0.59),
        };

        /// <summary>The year the game opens in - the seed world is 2026, the same year the pension row is filmed at.</summary>
        private const int SeedYear = 2026;

        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            sb.Append("=== PENSION PAYMENT (PN-2): the line over the people the statute retires, against the sourced average pension and replacement ratio - MEASURED, NOTHING ASSERTED ===\n");

            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            EnergyMarket.BeginTurn(world);
            CountryId[] order = { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.USA };

            sb.Append("\n    1. THE HEADCOUNT the statute's age implies - the bands at or above it, the straddled band by its fraction (uniform within the cohort)\n");
            foreach (CountryId id in order)
            {
                Country c = world.GetCountry(id);
                float age = PensionAgeStatute.AgeInForce(id, SeedYear);
                double head = PensionPayment.PensionersMillions(c, age); double straddleFraction = StraddleFraction(c, age, out int straddleBand);
                sb.Append(F("    {0,-8} age {1:F2} · band {2} takes {3:P1} of itself · pensioners {4:N3} m of {5:N3} m people ({6:P1})\n",
                    id, age, straddleBand, straddleFraction, head, TotalMillions(c), TotalMillions(c) > 0 ? head / TotalMillions(c) : 0));
            }

            sb.Append("\n    2. THE BENEFIT the line implies - the pension line over that headcount, in the book's dollars and in the source's euros\n");
            foreach (CountryId id in order)
            {
                Country c = world.GetCountry(id);
                float age = PensionAgeStatute.AgeInForce(id, SeedYear);
                double head = PensionPayment.PensionersMillions(c, age);
                double line = PensionPayment.LineBillions(c);
                double perYearUsd = head > 0 ? line * 1e9 / (head * 1e6) : 0;   // billions over millions = thousands; × 1000 = the person's own figure
                double nationalPerUsd = EnergyLayer.NationalPerUsd(id);
                double perYearNational = perYearUsd * nationalPerUsd;
                sb.Append(F("    {0,-8} line {1:N1} bn · {2:N3} m pensioners · benefit {3:N0} USD/yr = {4:N0} {5}/yr\n",
                    id, line, head, perYearUsd, perYearNational, EnergyLayer.CurrencyCode(id)));
            }

            // THE GAP, DECOMPOSED - and the GATE on the half that compares like with like.
            // The benefit is expenditure over heads, so its gap against the source is exactly the expenditure gap over the headcount gap. The headcount
            // gap is definitional (ESSPROS counts every beneficiary; this counts the cohorts at or above the age) and cannot be a verdict. The EXPENDITURE
            // gap can: both sides are a country's old-age pension spending in euro for the same year. THE BAND IS THE SOURCE'S OWN: ESSPROS publishes two
            // definitions - old-age pensions, and all pension types - and the model's one line is asked only to fall between them. That band is not chosen
            // to pass anything; it is the width of the source's own disagreement with itself, country by country.
            double eurPerUsd = EnergyLayer.NationalPerUsd(CountryId.Germany);   // the book is in dollars, the source in euro; Germany's rate IS the euro's
            sb.Append(F("\n    3. THE GATE - the model's pension line against ESSPROS 2022, in euro, with the band the source draws itself (old-age .. all types). 1 USD = {0:F4} EUR\n", eurPerUsd));
            int within = 0, divergent = 0, unsourced = 0;
            foreach ((CountryId id, double allTypesExpMio, double oldAgeExpMio, double allTypesBen, double oldAgeBen) in SourcedEssprosEur)
            {
                Country c = world.GetCountry(id);
                float age = PensionAgeStatute.AgeInForce(id, SeedYear);
                double head = PensionPayment.PensionersMillions(c, age);
                double lineEurBn = PensionPayment.LineBillions(c) * eurPerUsd;
                if (allTypesExpMio <= 0) { unsourced++; sb.Append(F("    {0,-8} NO SOURCE ROW - the fetch does not carry this country; no verdict is formed\n", id)); continue; }
                double lowBn = oldAgeExpMio / 1000.0, highBn = allTypesExpMio / 1000.0;
                bool inBand = lineEurBn >= lowBn && lineEurBn <= highBn;
                if (inBand) { within++; } else { divergent++; }
                double nearest = lineEurBn < lowBn ? lineEurBn / lowBn : lineEurBn / highBn;
                sb.Append(F("    {0,-8} line {1:N1} bn EUR against ESSPROS {2:N1} (old age) .. {3:N1} (all types) · {4} · nearest edge x{5:F3} · heads {6:N2} m against {7:N2} m beneficiaries (x{8:F3})\n",
                    id, lineEurBn, lowBn, highBn, inBand ? "WITHIN" : "DIVERGENT", nearest, head, oldAgeBen / 1e6, oldAgeBen > 0 ? head / (oldAgeBen / 1e6) : 0));
            }
            sb.Append(F("    {0} WITHIN the source's own band, {1} DIVERGENT, {2} unsourced.\n", within, divergent, unsourced));

            sb.Append("\n    4. THE REPLACEMENT RATE - the benefit over the mean income the schedules read, against Eurostat's aggregate replacement ratio\n");
            foreach ((CountryId id, double ratio) in SourcedReplacementRatio)
            {
                Country c = world.GetCountry(id);
                float age = PensionAgeStatute.AgeInForce(id, SeedYear);
                double head = PensionPayment.PensionersMillions(c, age);
                double perYearUsd = PensionPayment.AverageBenefitPerYear(c, SeedYear);
                double meanIncomeStatute = TaxSchedule.AverageIncome(c, 1.0);            // the statute's own currency, the schedules' reading
                double meanIncomeUsd = meanIncomeStatute / Math.Max(1e-9, EnergyLayer.NationalPerUsd(id));
                double model = meanIncomeUsd > 0 ? perYearUsd / meanIncomeUsd : 0;
                sb.Append(F("    {0,-8} benefit {1:N0} USD/yr over mean income {2:N0} USD/yr = {3:F3} against ilc_pnp3 {4:F2} · ratio {5:F3}\n",
                    id, perYearUsd, meanIncomeUsd, model, ratio, ratio > 0 ? model / ratio : 0));
            }

            sb.Append("\n    ⚠ THE REPLACEMENT RATE IS A READING, NOT A GATE: ilc_pnp3 is a median individual pension over median earnings of a named age band, and this is a\n");
            sb.Append("    mean over a mean, so the two cannot be equal even where the model is right. It is printed because a reader will ask, and the gap is named with it.\n");

            // (5) THE VERDICTS, ASSERTED - so a seed that drifts out of the source's band trips the bar instead of being read as the same row
            bool ok = true;
            foreach ((CountryId id, double allTypesExpMio, double oldAgeExpMio, double _, double __) in SourcedEssprosEur)
            {
                Country c = world.GetCountry(id);
                double lineEur = PensionPayment.LineBillions(c) * eurPerUsd;
                bool expectedWithin = id == CountryId.Germany || id == CountryId.France || id == CountryId.Italy;
                if (allTypesExpMio <= 0) { continue; }
                bool inBand = lineEur >= oldAgeExpMio / 1000.0 && lineEur <= allTypesExpMio / 1000.0;
                if (inBand != expectedWithin)
                {
                    ok = false;
                    Debug.LogError(F("PENSION PAYMENT: {0}'s line reads {1:N1} bn EUR against the source's band {2:N1} .. {3:N1} - it is {4} where §518 measured it {5}. A seed moved, or a rate did.",
                        id, lineEur, oldAgeExpMio / 1000.0, allTypesExpMio / 1000.0, inBand ? "WITHIN" : "DIVERGENT", expectedWithin ? "WITHIN" : "DIVERGENT"));
                }
            }
            // the two DIVERGENT ones at the size they were measured at, so the record's figures cannot rot silently
            AssertNearestEdge(world, CountryId.Poland, 1.209, ref ok);
            AssertNearestEdge(world, CountryId.Sweden, 0.724, ref ok);
            // the identity the row and the gate both stand on: the benefit IS the line over the heads
            foreach (CountryId id in order)
            {
                Country c = world.GetCountry(id);
                if (!PensionAgeStatute.Has(id)) { continue; }
                double heads = PensionPayment.PensionersMillions(c, PensionAgeStatute.AgeInForce(id, SeedYear));
                double benefit = PensionPayment.AverageBenefitPerYear(c, SeedYear);
                double implied = heads > 0 ? benefit * heads * 1e6 / 1e9 : 0;
                if (Math.Abs(implied - PensionPayment.LineBillions(c)) > 0.01)
                {
                    ok = false;
                    Debug.LogError(F("PENSION PAYMENT: {0}'s benefit × heads reads {1:N3} bn against the line's {2:N3} bn - the readout is not the line over the people it names.", id, implied, PensionPayment.LineBillions(c)));
                }
            }

            Debug.Log(sb.ToString());
            Debug.Log(ok ? "=== PensionPaymentDiagnostic: ALL ASSERTIONS PASS ===" : "=== PensionPaymentDiagnostic: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>A DIVERGENT country's distance from the nearest edge of the source's own band, asserted at the figure §518 measured - a drift either way is a finding.</summary>
        private static void AssertNearestEdge(World world, CountryId id, double measured, ref bool ok)
        {
            foreach ((CountryId rowId, double allTypesExpMio, double oldAgeExpMio, double _, double __) in SourcedEssprosEur)
            {
                if (rowId != id) { continue; }
                double lineEur = PensionPayment.LineBillions(world.GetCountry(id)) * EnergyLayer.NationalPerUsd(CountryId.Germany);
                double low = oldAgeExpMio / 1000.0, high = allTypesExpMio / 1000.0;
                double edge = lineEur < low ? lineEur / low : lineEur / high;
                if (Math.Abs(edge - measured) > 0.01)
                {
                    ok = false;
                    Debug.LogError(F("PENSION PAYMENT: {0} sits x{1:F3} from the source's nearest edge against the x{2:F3} §518 measured - the gap moved and the record's figure is stale.", id, edge, measured));
                }
                return;
            }
        }

        private static double TotalMillions(Country c)
        {
            double t = 0;
            if (c.Cohorts?.Counts != null) { foreach (float n in c.Cohorts.Counts) { t += n; } }
            return t;
        }

        /// <summary>Which band the statutory age falls inside and how much of it the headcount takes - the sub-band assumption, printed so it is read
        /// rather than assumed. The headcount itself is <see cref="PensionPayment.PensionersMillions"/>'s, which the row on the screen reads too.</summary>
        private static double StraddleFraction(Country c, float age, out int straddleBand)
        {
            straddleBand = -1;
            if (c?.Cohorts?.Counts == null) { return 0; }
            for (int b = 0; b < PopulationCohorts.OpenBandIndex; b++)
            {
                int start = b * PopulationCohorts.CohortWidth, end = start + PopulationCohorts.CohortWidth;
                if (age > start && age < end) { straddleBand = b; return (end - age) / (double)PopulationCohorts.CohortWidth; }
            }
            return 0;
        }
    }
}
