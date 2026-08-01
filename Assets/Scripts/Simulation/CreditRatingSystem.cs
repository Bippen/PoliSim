using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>Sovereign credit rating notches, S&amp;P's ladder. Index order IS the ordering - 0 is best.</summary>
    public enum CreditRating
    {
        AAA = 0, AAplus = 1, AA = 2, AAminus = 3,
        Aplus = 4, A = 5, Aminus = 6,
        BBBplus = 7, BBB = 8, BBBminus = 9,
        BBplus = 10, BB = 11, BBminus = 12,
        Bplus = 13, B = 14, Bminus = 15,
        CCC = 16
    }

    /// <summary>Outlook is a separate signal from the rating - a cheap way to telegraph a downgrade before it lands.</summary>
    public enum RatingOutlook { Positive, Stable, Negative }

    /// <summary>
    /// Master Sequence step 9, Step C4: sovereign credit rating, **DERIVED, never seeded**.
    ///
    /// A rating is a judgment ABOUT a fiscal position, not an independent economic variable. Agencies
    /// build it from debt-to-GDP, deficit trajectory and growth - all three already tracked here. So
    /// this is computed on demand from existing state, exactly like <see cref="DerivedStats"/>: no new
    /// fields, no mean-reverting variable of its own, nothing folding into any combined ceiling. It
    /// therefore cannot desync from the fiscal reality it describes, and cannot change a simulation
    /// number.
    ///
    /// **Built out of order (before C1-C3), deliberately.** C4 consumes no seed data from the earlier
    /// batches and their blocker is external. See the roadmap's resolved Open Question for why that is
    /// not a precedent for casual skipping.
    /// </summary>
    public static class CreditRatingSystem
    {
        /// <summary>
        /// The debt-to-GDP level above which extra borrowing starts costing a country its rating. 60%
        /// is the real Maastricht reference value, not a tuned number - and using a real anchor matters
        /// because this constant is what the reserve-currency discount is measured against.
        /// </summary>
        private const float DebtReferencePercent = 60f;

        /// <summary>Maastricht's deficit reference, again real rather than tuned.</summary>
        private const float DeficitReferencePercent = 3f;

        /// <summary>
        /// (effective debt burden, notches below AAA). Calibrated against the real curve in the seed
        /// data - Sweden ~35%/AAA, Germany ~63%/AAA, France ~116%/AA-, Italy ~138%/BBB+ - and
        /// deliberately accelerating, because sovereign risk compounds rather than growing linearly:
        /// 90 to 116 costs two notches, 116 to 138 costs four.
        /// </summary>
        private static readonly (float Burden, float Notches)[] BurdenCurve =
        {
            (0f, 0f), (70f, 0f), (90f, 1f), (105f, 2f), (116f, 3f), (127f, 5f), (138f, 7f), (160f, 10f), (250f, 16f)
        };

        public readonly struct Assessment
        {
            public readonly CreditRating Rating;
            public readonly RatingOutlook Outlook;
            /// <summary>Debt-to-GDP after the reserve-currency discount - what the rating actually reads.</summary>
            public readonly float EffectiveDebtBurden;

            public Assessment(CreditRating rating, RatingOutlook outlook, float effectiveDebtBurden)
            {
                Rating = rating;
                Outlook = outlook;
                EffectiveDebtBurden = effectiveDebtBurden;
            }
        }

        /// <summary>
        /// This country's current rating and outlook. <paramref name="report"/> may be null on turn 0,
        /// in which case the deficit term is simply omitted rather than guessed at.
        /// </summary>
        public static Assessment Evaluate(Country country, FiscalTurnReport report)
        {
            float burden = GetEffectiveDebtBurden(country);
            float notches = InterpolateCurve(burden);

            // Deficit trajectory. This is what separates the USA from a AAA despite its reserve status,
            // and it is the real story: agencies downgraded the USA (S&P 2011, Fitch 2023) over deficits
            // and governance, not over an inability to borrow.
            float? deficitPercent = DerivedStats.DeficitPercentOfGdp(country, report);
            if (deficitPercent.HasValue && deficitPercent.Value > DeficitReferencePercent)
            {
                notches += (deficitPercent.Value - DeficitReferencePercent) / 3f;
            }

            // Growth. A country can outgrow its debt, and a contracting one cannot. Kept deliberately
            // small - growth moves a rating at the margin, it does not dominate the debt stock.
            float? growth = DerivedStats.RealGdpGrowthPercent(country);
            if (growth.HasValue)
            {
                if (growth.Value >= 3f) notches -= 0.5f;
                else if (growth.Value < 0f) notches += 0.5f;
            }

            int index = Mathf.Clamp(Mathf.RoundToInt(notches), 0, (int)CreditRating.CCC);
            return new Assessment((CreditRating)index, GetOutlook(country, deficitPercent, growth), burden);
        }

        /// <summary>
        /// Debt-to-GDP as a rating agency effectively reads it, after the reserve-currency discount.
        ///
        /// **This reuses `RiskPremiumSensitivity` - the SAME field the debt-interest model already uses -
        /// rather than introducing a second, parallel notion of reserve-currency status**, which the
        /// directive explicitly warns against. The USA is seeded at 0.05 and everyone else defaults to
        /// 1.0, so only debt ABOVE the reference is discounted: a reserve issuer's excess borrowing is
        /// cheap, not free, and its first 60% of GDP counts the same as anyone's.
        ///
        /// This is what reproduces the one real non-monotonicity in the calibration curve - the USA
        /// carries HIGHER debt than France (124 vs 116) yet rates BETTER (AA+ vs AA-). France reads its
        /// full 116; the USA reads 60 + (124-60)*0.05 = 63.2.
        /// </summary>
        private static float GetEffectiveDebtBurden(Country country)
        {
            float debtToGdp = country.State.DebtToGdpRatio;
            if (debtToGdp <= DebtReferencePercent)
            {
                return debtToGdp;
            }

            return DebtReferencePercent + (debtToGdp - DebtReferencePercent) * country.RiskPremiumSensitivity;
        }

        /// <summary>
        /// Outlook, a real signal distinct from the rating itself. Negative on a deficit meaningfully
        /// past the reference or an outright contraction; positive only on a genuine surplus with
        /// growth. France carrying a negative outlook while southern Europe sits stable is the pattern
        /// this reproduces.
        /// </summary>
        private static RatingOutlook GetOutlook(Country country, float? deficitPercent, float? growth)
        {
            if (deficitPercent.HasValue)
            {
                if (deficitPercent.Value > 5f) return RatingOutlook.Negative;
                if (deficitPercent.Value < 0f && growth.GetValueOrDefault() > 0f) return RatingOutlook.Positive;
            }

            return growth.HasValue && growth.Value < 0f ? RatingOutlook.Negative : RatingOutlook.Stable;
        }

        private static float InterpolateCurve(float burden)
        {
            for (int i = 1; i < BurdenCurve.Length; i++)
            {
                if (burden > BurdenCurve[i].Burden) continue;

                (float lowBurden, float lowNotches) = BurdenCurve[i - 1];
                (float highBurden, float highNotches) = BurdenCurve[i];
                float span = highBurden - lowBurden;
                float t = span > 0f ? (burden - lowBurden) / span : 0f;
                return Mathf.Lerp(lowNotches, highNotches, t);
            }

            return BurdenCurve[BurdenCurve.Length - 1].Notches;
        }

        /// <summary>Display text, e.g. "AA+" - the enum names cannot carry '+' or '-'.</summary>
        public static string Format(CreditRating rating)
        {
            switch (rating)
            {
                case CreditRating.AAA: return "AAA";
                case CreditRating.AAplus: return "AA+";
                case CreditRating.AA: return "AA";
                case CreditRating.AAminus: return "AA−";
                case CreditRating.Aplus: return "A+";
                case CreditRating.A: return "A";
                case CreditRating.Aminus: return "A−";
                case CreditRating.BBBplus: return "BBB+";
                case CreditRating.BBB: return "BBB";
                case CreditRating.BBBminus: return "BBB−";
                case CreditRating.BBplus: return "BB+";
                case CreditRating.BB: return "BB";
                case CreditRating.BBminus: return "BB−";
                case CreditRating.Bplus: return "B+";
                case CreditRating.B: return "B";
                case CreditRating.Bminus: return "B−";
                default: return "CCC";
            }
        }
    }
}
