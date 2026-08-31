using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
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
        /// SOURCED, and verified at the text rather than recalled: the reference value in **Protocol
        /// No. 12 on the excessive deficit procedure, Article 1**, annexed to the TEU and TFEU -
        /// *"60 % for the ratio of government debt to gross domestic product at market prices"*
        /// (EUR-Lex CELEX 12016E/PRO/12, read 2026-09-01).
        /// <para>The debt-to-GDP level above which extra borrowing starts costing a country its rating.
        /// Using a real anchor matters because this constant is what the reserve-currency discount is
        /// measured against.</para>
        /// </summary>
        private const float DebtReferencePercent = 60f;

        /// <summary>SOURCED, from the same article as `DebtReferencePercent`: Protocol No. 12, Article 1 - *"3 % for the ratio of the planned or actual government deficit to gross domestic product at market prices"*. Real rather than tuned.</summary>
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
            return EvaluateFrom(
                country.State.DebtToGdpRatio,
                country.RiskPremiumSensitivity,
                DerivedStats.DeficitPercentOfGdp(country, report),
                DerivedStats.RealGdpGrowthPercent(country));
        }

        /// <summary>
        /// The rating formula itself, over explicit inputs.
        ///
        /// **This body is unchanged from the original `Evaluate` (`76a8f35`), and that is deliberate.**
        /// Elias's A1 ruling fixed the rating thrash by changing WHEN the rating is computed, not HOW -
        /// so `BurdenCurve`, the reserve-currency discount, the deficit divisor and the growth thresholds
        /// are all untouched. That is precisely what preserves the 5-anchor calibration: the scheduled
        /// review and the anchor check run through this same method, so a passing anchor check after the
        /// change means the same thing it meant before it.
        ///
        /// Extracted so the two callers - the live evaluation and the scheduled review reading settled
        /// values - cannot drift into two slightly different formulas.
        /// </summary>
        public static Assessment EvaluateFrom(float debtToGdp, float riskPremiumSensitivity, float? deficitPercent, float? growthPercent)
        {
            float burden = GetEffectiveDebtBurden(debtToGdp, riskPremiumSensitivity);
            float notches = InterpolateCurve(burden);

            // Deficit trajectory. This is what separates the USA from a AAA despite its reserve status,
            // and it is the real story: agencies downgraded the USA (S&P 2011, Fitch 2023) over deficits
            // and governance, not over an inability to borrow.
            if (deficitPercent.HasValue && deficitPercent.Value > DeficitReferencePercent)
            {
                notches += (deficitPercent.Value - DeficitReferencePercent) / 3f;
            }

            // Growth. A country can outgrow its debt, and a contracting one cannot. Kept deliberately
            // small - growth moves a rating at the margin, it does not dominate the debt stock.
            if (growthPercent.HasValue)
            {
                if (growthPercent.Value >= 3f) notches -= 0.5f;
                else if (growthPercent.Value < 0f) notches += 0.5f;
            }

            int index = Mathf.Clamp(Mathf.RoundToInt(notches), 0, (int)CreditRating.CCC);
            return new Assessment((CreditRating)index, GetOutlook(deficitPercent, growthPercent), burden);
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
        private static float GetEffectiveDebtBurden(float debtToGdp, float riskPremiumSensitivity)
        {
            if (debtToGdp <= DebtReferencePercent)
            {
                return debtToGdp;
            }

            return DebtReferencePercent + (debtToGdp - DebtReferencePercent) * riskPremiumSensitivity;
        }

        /// <summary>
        /// Outlook, a real signal distinct from the rating itself. Negative on a deficit meaningfully
        /// past the reference or an outright contraction; positive only on a genuine surplus with
        /// growth. France carrying a negative outlook while southern Europe sits stable is the pattern
        /// this reproduces.
        /// </summary>
        private static RatingOutlook GetOutlook(float? deficitPercent, float? growth)
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

        /// <summary>
        /// The trailing-year fiscal position as of the most recently CLOSED quarter - the settled figures
        /// a review reads instead of one turn's instantaneous state.
        ///
        /// **The deficit is derived from the debt stock, not read from a flow, and that is the fix.** The
        /// thrash came from `FiscalTurnReport.BudgetBalance`: one 121-day turn's balance, which swings
        /// hard enough to move the rating 14 notches and back. A year's deficit is by definition the
        /// year's increase in indebtedness, so with both stocks recorded it is exact rather than
        /// approximated:
        ///
        ///     Debt = (d/100) * Y,  so  deficit%(of current GDP) = d_now - d_prev * (Y_prev / Y_now)
        ///
        /// Both `d` and `Y` come from `PeriodClosingValues` on the SAME quarterly boundaries, which is why
        /// `ClosingStat.DebtToGdpRatio` deliberately shares GDP's period rule - comparing a debt reading
        /// to a GDP reading from a different date would silently corrupt the ratio.
        ///
        /// Returns nulls rather than zeros when a year of history does not exist yet. A missing year-ago
        /// figure means "cannot compute a deficit", not "the deficit was zero", and `EvaluateFrom` already
        /// omits the term rather than guessing.
        /// </summary>
        public static void GetSettledPosition(Country country, System.DateTime date,
            out float debtToGdp, out float? deficitPercent, out float? growthPercent)
        {
            // The quarter that has just CLOSED, not the one now running - hence date.AddDays(-1). On a
            // 1 January review this is Oct-Dec, which is the position the review is actually judging.
            System.DateTime settled = ReleaseCalendar.GetCurrentPeriodStart(ClosingStat.Gdp, date.AddDays(-1));
            System.DateTime prior = settled.AddYears(-1);

            float? debtNow = country.Published.ClosingValue(ClosingStat.DebtToGdpRatio, settled);
            float? gdpNow = country.Published.ClosingValue(ClosingStat.Gdp, settled);
            float? debtPrior = country.Published.ClosingValue(ClosingStat.DebtToGdpRatio, prior);
            float? gdpPrior = country.Published.ClosingValue(ClosingStat.Gdp, prior);

            // Debt-to-GDP is a STOCK, so a reading at the review instant is already a settled position -
            // no averaging needed or wanted. The live fallback covers only the first review, before any
            // quarter has closed, and is the same value the closing would have recorded that day.
            debtToGdp = debtNow ?? country.State.DebtToGdpRatio;

            deficitPercent = null;
            growthPercent = null;
            if (!gdpNow.HasValue || !gdpPrior.HasValue || gdpPrior.Value <= 0f)
            {
                return;
            }

            growthPercent = (gdpNow.Value - gdpPrior.Value) / gdpPrior.Value * 100f;

            if (debtNow.HasValue && debtPrior.HasValue && gdpNow.Value > 0f)
            {
                deficitPercent = debtNow.Value - debtPrior.Value * (gdpPrior.Value / gdpNow.Value);
            }
        }

        /// <summary>
        /// Runs a scheduled rating review if one is due today, and refreshes the outlook each time a
        /// quarter closes. Called once per simulated day, per country, from the same daily pass as
        /// <see cref="PublicationSystem.PublishDueFigures"/>.
        ///
        /// **Cadence: annual, on the country's own fiscal-year start** (USA 1 October, the European five
        /// 1 January). Justification, since Elias asked for one rather than a default:
        /// - Real agencies review sovereigns roughly once or twice a year, so one scheduled review is
        ///   within the real range and comfortably inside the "small number per year" bound set by A1.
        /// - That date already exists in this project (`FiscalYearData.GetFiscalYearStart`) and is
        ///   already what `ReleaseCalendar` treats as the boundary annual figures settle on, so this adds
        ///   no new date rule and no parallel timer - the A1 ruling's explicit requirement.
        /// - It is the same boundary the country's own budget process turns on, so a rating review lands
        ///   exactly when the fiscal year it judges has closed.
        ///
        /// **The outlook refreshes quarterly while the rating moves annually**, which is the real
        /// division of labour between the two signals and what makes the outlook worth having: with the
        /// rating deliberately still between reviews, a deteriorating position would otherwise be
        /// invisible until the next review. It reads the same trailing-year position, so it reports a
        /// developing trend rather than one quarter's noise.
        /// </summary>
        public static void ReviewIfDue(Country country, System.DateTime date)
        {
            GetSettledPosition(country, date, out float debtToGdp, out float? deficitPercent, out float? growthPercent);
            Assessment assessment = EvaluateFrom(debtToGdp, country.RiskPremiumSensitivity, deficitPercent, growthPercent);

            SovereignRatingState rating = country.Rating;
            (int fiscalMonth, int fiscalDay) = FiscalYearData.GetFiscalYearStart(country.Id);
            bool isReviewDay = date.Month == fiscalMonth && date.Day == fiscalDay;

            if (isReviewDay || !rating.HasBeenReviewed)
            {
                rating.HasBeenReviewed = true;
                rating.Rating = assessment.Rating;
                rating.LastReviewDate = date;
                rating.ReviewedDebtBurden = assessment.EffectiveDebtBurden;
                rating.ReviewedDeficitPercent = deficitPercent;
                rating.ReviewedGrowthPercent = growthPercent;
            }

            // Outlook tracks the settled position continuously; because that position only moves when a
            // quarter closes, this changes at most quarterly despite running daily.
            rating.Outlook = assessment.Outlook;
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
