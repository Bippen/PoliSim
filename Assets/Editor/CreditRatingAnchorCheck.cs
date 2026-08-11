using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Step C4's 5-anchor calibration check, run headlessly:
    /// `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.CreditRatingAnchorCheck.Run -logFile &lt;path&gt;`
    ///
    /// **No Play mode.** <see cref="CreditRatingSystem.EvaluateFrom"/> is pure arithmetic over explicit
    /// inputs, so this needs no World, no SimulationManager and no scene - which is why it runs in
    /// seconds and can be run BEFORE the matrix, as Elias's A1 ruling requires.
    ///
    /// **This is the FIRST EXECUTABLE version of this check.** The original calibration (`76a8f35`) was
    /// done by hand and recorded only in that commit message, so "passes unchanged" here means "reproduces
    /// the five results recorded there", not "matches a previous script". Codifying it is the point: an
    /// anchor check that only exists in prose cannot be re-run after a change, which is exactly the
    /// situation the review-cadence work needed it for.
    ///
    /// **Why the anchors are debt-only for four of the five.** The recorded calibration states the curve
    /// in terms of debt alone - Sweden 35 to AAA, Germany 63 to AAA, France 116 to AA-, Italy 138 to
    /// BBB+ - so those four are fed debt with no deficit and no growth term, which is what `EvaluateFrom`
    /// already does when both are null. The USA is the one anchor that genuinely needs a deficit: at 124%
    /// debt its reserve-currency discount reads an effective 63.2%, which is AAA on the curve, and the
    /// recorded reasoning is explicit that "deficit is what takes the USA from AAA to AA+".
    /// </summary>
    public static class CreditRatingAnchorCheck
    {
        /// <summary>
        /// The USA's real federal deficit, ~6.4% of GDP (2024). Only the USA anchor needs one - see the
        /// class comment. The check also reports the full deficit RANGE that holds the USA at AA+, so the
        /// sensitivity this anchor carries is visible rather than buried in one hand-picked number.
        /// </summary>
        private const float UsaDeficitPercentOfGdp = 6.4f;

        private readonly struct Anchor
        {
            public readonly string Country;
            public readonly float DebtToGdp;
            public readonly float RiskPremiumSensitivity;
            public readonly float? DeficitPercent;
            public readonly CreditRating Expected;
            public readonly string Note;

            public Anchor(string country, float debtToGdp, float riskPremiumSensitivity, float? deficitPercent, CreditRating expected, string note)
            {
                Country = country;
                DebtToGdp = debtToGdp;
                RiskPremiumSensitivity = riskPremiumSensitivity;
                DeficitPercent = deficitPercent;
                Expected = expected;
                Note = note;
            }
        }

        private static readonly Anchor[] Anchors =
        {
            new Anchor("Sweden",  35f,  1f,    null,                   CreditRating.AAA,     "low debt, comfortably inside the curve's flat region"),
            new Anchor("Germany", 63f,  1f,    null,                   CreditRating.AAA,     "just above the 60% Maastricht reference, still flat"),
            new Anchor("France",  116f, 1f,    null,                   CreditRating.AAminus, "curve point (116, 3) exactly"),
            new Anchor("Italy",   138f, 1f,    null,                   CreditRating.BBBplus, "curve point (138, 7) exactly"),
            new Anchor("USA",     124f, 0.05f, UsaDeficitPercentOfGdp, CreditRating.AAplus,  "reserve discount reads 63.2% effective; deficit is what costs the notch"),

            // ADDED 2026-08-02, AND EXPECTED TO FAIL. Poland's real rating (S&P A−, Moody's A2, Fitch A−)
            // was sourced 2026-08-02 and breaks this curve's monotonicity in the OPPOSITE direction to
            // the USA: Poland carries LOWER debt than Germany (59% vs 63%) and rates FOUR NOTCHES WORSE.
            //
            // The model cannot express that. `GetEffectiveDebtBurden` discounts debt above the reference
            // by RiskPremiumSensitivity and has no term that PENALISES, so no country can rate below the
            // curve its debt alone places it on. Poland at 59% lands in the flat AAA region by
            // construction, and no parameter choice moves it.
            //
            // It is added anyway, failing, ON PURPOSE. A calibration check that passes because the hard
            // case was left out of it is exactly the confirmation this project has learned to distrust:
            // "5 of 5" was a statement about five countries chosen partly for being expressible. The
            // missing factor is some combination of currency status (Poland borrows partly in a currency
            // it does not issue), institutional quality, and an EU-periphery premium. Until such a term
            // exists, this line is the standing reminder that it does not.
            new Anchor("Poland",  59f,  1f,    null,                   CreditRating.Aminus,  "EXPECTED FAIL — lower debt than Germany, four notches worse; no penalising term exists"),
        };

        public static void Run()
        {
            int passed = 0;
            // Counted separately so the exit code can gate on the five EXPRESSIBLE anchors while Poland's
            // documented failure is reported without failing the build - see the note at the end.
            int expressiblePassed = 0;
            Debug.Log("=== Step C4 6-anchor calibration check (Poland added 2026-08-02, expected to FAIL) ===");

            foreach (Anchor anchor in Anchors)
            {
                CreditRatingSystem.Assessment result = CreditRatingSystem.EvaluateFrom(
                    anchor.DebtToGdp, anchor.RiskPremiumSensitivity, anchor.DeficitPercent, null);

                bool ok = result.Rating == anchor.Expected;
                if (ok)
                {
                    passed++;
                    if (anchor.Country != "Poland") { expressiblePassed++; }
                }

                Debug.Log($"{(ok ? "PASS" : "FAIL")} {anchor.Country,-8} debt {anchor.DebtToGdp,5:F1}% " +
                    $"deficit {(anchor.DeficitPercent.HasValue ? anchor.DeficitPercent.Value.ToString("F1") + "%" : "n/a"),-5} " +
                    $"-> effective burden {result.EffectiveDebtBurden,5:F1}% " +
                    $"-> {CreditRatingSystem.Format(result.Rating),-4} (expected {CreditRatingSystem.Format(anchor.Expected)}) | {anchor.Note}");
            }

            // The USA anchor is the one with a tunable input, so state the band it survives in rather
            // than only that one value passes. A reader can then see how much of the anchor's result is
            // the model and how much is the number chosen for it.
            ReportUsaDeficitBand();

            Debug.Log($"=== 6-anchor calibration: {passed} of {Anchors.Length} PASS ===");

            // EXIT 0 ON THE KNOWN, DOCUMENTED FAILURE - deliberately, and this is the only place in the
            // project where a check does that. Poland cannot pass until CreditRatingSystem gains a
            // penalising term, so failing the build on it would make this check something people disable
            // rather than read. The five expressible anchors still gate: if any of THEM breaks, that is a
            // real regression and the exit code says so.
            //
            // ⚠ WHEN A PENALISING TERM IS ADDED, DELETE THIS ALLOWANCE. A permanently-tolerated failure
            // is how a known gap becomes invisible.
            if (passed < Anchors.Length)
            {
                Debug.Log("NOTE Poland's failure is EXPECTED and documented - the model has no term that " +
                    "rates a country BELOW its debt curve. Exit code reflects the other five only.");
            }
            CheckExit.Finish(expressiblePassed >= Anchors.Length - 1 ? 0 : 1);
        }

        private static void ReportUsaDeficitBand()
        {
            float? low = null;
            float? high = null;
            for (float deficit = 0f; deficit <= 20f; deficit += 0.1f)
            {
                CreditRating rating = CreditRatingSystem.EvaluateFrom(124f, 0.05f, deficit, null).Rating;
                if (rating == CreditRating.AAplus)
                {
                    if (!low.HasValue) low = deficit;
                    high = deficit;
                }
            }

            Debug.Log(low.HasValue
                ? $"     USA holds AA+ for deficits in [{low.Value:F1}%, {high.Value:F1}%] of GDP; the anchor uses {UsaDeficitPercentOfGdp:F1}%."
                : "     USA never reaches AA+ at 124% debt for any deficit in [0, 20]% - the anchor's premise no longer holds.");
        }
    }
}
