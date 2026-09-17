using System;

namespace PoliSim.Data
{
    /// <summary>
    /// **P6-E1 (2026-09-17, `COMPLETED.md` §534): Sweden's *jobbskatteavdrag* - the earned income tax credit, as the statute
    /// computes it.** Inkomstskattelagen (1999:1229) 67 kap. 5–9 §§, in the arithmetic Skatteverket publishes for the tax
    /// tables: *Teknisk beskrivning SKV 433, utgåva 35 (2024-12-11), income year 2025*, sections 7.5.2 (the credit) and the
    /// *grundavdrag* section it rests on. Every constant here is that document's, verbatim; the worked examples it prints are
    /// what `EarnedIncomeCreditDiagnostic` reproduces to the krona.
    ///
    /// <para><b>The statute's own shape.</b> The credit is a CREDIT LAYER on the schedule, not a rate: it is computed from the
    /// earned income (*arbetsinkomst*, rounded down to a whole hundred kronor), the price base amount (*prisbasbelopp*, PBB),
    /// the basic allowance (*grundavdrag*, GA) and the municipal tax rate (KI), phased in four intervals of PBB for a person
    /// under 66 at the start of the income year and in three for a person who has turned 66 - the enhancement the statute
    /// gives working pensioners - and it is set off against municipal income tax ONLY, so it can never exceed it. The
    /// result is rounded down to a whole krona.</para>
    ///
    /// <para><b>KI, and why the model's municipal rate is the statute's KI without the 1.16 the tables subtract.</b> The tax
    /// tables carry a column rate that includes the burial fee and the church fee, and the statute takes 1.16 points off it
    /// for the credit; the model's flat layer is SCB's national average municipal rate, which is municipality plus region
    /// and carries neither fee, so it IS the rate the statute multiplies by and nothing is subtracted here.</para>
    ///
    /// <para>⚠ <b>Landed INERT, and why (P6-E1's own ruling and the sheet's).</b> The sheet says *"two BASELINE families never
    /// in one pass"*, and P6-D1 is this pass's family (the trade matrix moved the no-policy path). A credit that reaches
    /// Sweden's income-tax yield moves the same path, so it is a second family; <see cref="Live"/> holds it off the book
    /// until it is ruled on film, exactly as the row asks (*"build none until E1 is ruled on film"* is E2's rule, and E1's
    /// own landing is held to the same bar by the two-family rule). What IS live: the formula, its diagnostic against the
    /// statute's examples, and the readout that prints the effective average rate WITH the credit beside the one without,
    /// so the gap the row names is on the screen. Flipping <see cref="Live"/> is its own family with its own dump.</para>
    /// </summary>
    public static class EarnedIncomeCredit
    {
        /// <summary>The income year the constants are read at.</summary>
        public const int IncomeYear = 2025;

        /// <summary>The price base amount for the income year, kronor (SKV 433 utgåva 35: "Prisbasbeloppet för 2025 har fastställts till 58 800 kr").</summary>
        public const double PriceBaseAmount = 58800;

        /// <summary>⚠ The hold: false until the credit's family is dumped and ruled (see the class note). The readout and the diagnostic do not read this;
        /// the yield path does.</summary>
        public const bool Live = false;

        /// <summary>The credit's statute scale, % - 100 is the statute; the lever's own sub-row moves it (P6-E1: "its size is the political instrument").</summary>
        public const double StatuteScalePercent = 100;

        // ---- grundavdrag (the basic allowance), the schedule the credit subtracts ------------------------------------------------
        // Fastställd förvärvsinkomst (FFI) in PBB → grundavdrag:
        //   not over 0.99 PBB → 0.423 PBB
        //   0.99–2.72 PBB    → 0.423 PBB + 20 % of the income above 0.99 PBB
        //   2.72–3.11 PBB    → 0.77 PBB
        //   3.11–7.88 PBB    → 0.77 PBB − 10 % of the income above 3.11 PBB
        //   over 7.88 PBB    → 0.293 PBB
        // The allowance may not exceed the income, and is rounded UP to a whole hundred kronor.

        /// <summary>The basic allowance for an income, kronor, per the statute's schedule and rounding.</summary>
        public static double BasicAllowance(double income)
        {
            if (income <= 0) { return 0; }
            double pbb = PriceBaseAmount;
            double x = income / pbb;
            double ga;
            if (x <= 0.99) { ga = 0.423 * pbb; }
            else if (x <= 2.72) { ga = 0.423 * pbb + 0.20 * (income - 0.99 * pbb); }
            else if (x <= 3.11) { ga = 0.77 * pbb; }
            else if (x <= 7.88) { ga = 0.77 * pbb - 0.10 * (income - 3.11 * pbb); }
            else { ga = 0.293 * pbb; }
            ga = Math.Min(ga, income);
            return Math.Ceiling(ga / 100.0) * 100.0;
        }

        // ---- the credit -------------------------------------------------------------------------------------------------------
        // Under 66 at the start of the income year (AI = earned income rounded down to a whole hundred; KI = the municipal rate):
        //   AI not over 0.91 PBB        → (AI − GA) × KI
        //   0.91–3.24 PBB               → (0.91 PBB + 0.3874 × (AI − 0.91 PBB) − GA) × KI
        //   3.24–8.08 PBB               → (1.813 PBB + 0.1990 × (AI − 3.24 PBB) − GA) × KI
        //   over 8.08 PBB               → (2.776 PBB − GA) × KI
        // Turned 66 at the start of the income year:
        //   AI not over 1.75 PBB        → 22 % of AI
        //   1.75–5.24 PBB               → 0.2635 PBB + 7 % of AI
        //   over 5.24 PBB               → 0.6293 PBB
        // Rounded down to a whole krona; set off against municipal income tax only.

        /// <summary>The credit for an earned income at a municipal rate, kronor, per the statute - for a person under 66 unless <paramref name="turned66"/>.
        /// <paramref name="scalePercent"/> is the lever (100 = the statute). For an employee the earned income IS the assessed income the allowance is
        /// computed on; the four-argument form takes the two apart where the statute does (a seafarer's sea-income deduction, the document's own third example).</summary>
        public static double Credit(double earnedIncome, double municipalRatePercent, bool turned66, double scalePercent = StatuteScalePercent)
            => Credit(earnedIncome, earnedIncome, municipalRatePercent, turned66, scalePercent);

        /// <summary>The credit where the earned income the schedule phases on and the assessed income the allowance is computed on differ (a deduction
        /// taken from the one and not the other). ⚠ The document's third example is exactly this case, and the first form of this class - the allowance
        /// on the earned income - reproduced its first two examples and missed the third by the allowance's difference (§534).</summary>
        public static double Credit(double earnedIncome, double assessedIncome, double municipalRatePercent, bool turned66, double scalePercent = StatuteScalePercent)
        {
            if (earnedIncome <= 0 || municipalRatePercent <= 0 || scalePercent <= 0) { return 0; }
            double pbb = PriceBaseAmount;
            double ai = Math.Floor(earnedIncome / 100.0) * 100.0;
            double ki = municipalRatePercent / 100.0;
            double credit;
            if (turned66)
            {
                double x = ai / pbb;
                if (x <= 1.75) { credit = 0.22 * ai; }
                else if (x <= 5.24) { credit = 0.2635 * pbb + 0.07 * ai; }
                else { credit = 0.6293 * pbb; }
            }
            else
            {
                double ga = BasicAllowance(Math.Max(assessedIncome, ai));
                double x = ai / pbb;
                double baseAmount;
                if (x <= 0.91) { baseAmount = ai; }
                else if (x <= 3.24) { baseAmount = 0.91 * pbb + 0.3874 * (ai - 0.91 * pbb); }
                else if (x <= 8.08) { baseAmount = 1.813 * pbb + 0.1990 * (ai - 3.24 * pbb); }
                else { baseAmount = 2.776 * pbb; }
                credit = Math.Max(0, baseAmount - ga) * ki;
            }

            credit = Math.Floor(credit) * (scalePercent / 100.0);
            // Set off against municipal income tax only: it cannot exceed the municipal tax on the income itself.
            return Math.Min(credit, ki * ai);
        }
    }
}
