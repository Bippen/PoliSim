using PoliSim.UI;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Regression check for the currency unit bug (visual review item 3, P2, 2026-08-02):
    /// <c>FormatAxisValue(29000)</c> rendered "29k" for $29 TRILLION.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.MoneyFormatDiagnostic.Run -logFile &lt;path&gt;`
    ///
    /// **Why this exists at all, rather than trusting the formatter.** This is the third instance of the
    /// same value being displayed wrong, and the previous fix was itself a careful formatter written to
    /// make the failure impossible. Formatting is pure arithmetic on a pure function, so there is no
    /// excuse for it to be unverifiable - and per the standing rule from the sparkline crash, maths that
    /// can only be reached through OnGUI is maths no batch run can check. <see cref="UiFormat.Money"/>
    /// has no GUI dependency and this hammers it.
    ///
    /// Expected values are taken from the real seeds, not invented: USA GDP 29000, GovernmentDebt 35960,
    /// SocialSecurity 1530, Defense 850, Sweden's SWF 195.
    /// </summary>
    public static class MoneyFormatDiagnostic
    {
        public static void Run()
        {
            int passed = 0, total = 0;

            // SELF-TEST FIRST, per the standing rule: prove the harness can observe the original defect,
            // or every PASS below is meaningless.
            string gdp = UiFormat.Money(29000f, MoneyUnit.Billions);
            Debug.Log($"SELFTEST Money(29000, Billions) = \"{gdp}\", and the bug rendered \"29k\" -> " +
                $"{(gdp != "29k" ? "OK" : "BROKEN - the defect is still present, results below are void")}");

            // CHECK 1: the seed table. Each expectation is a real figure this project already calibrated
            // against, so a wrong answer here is wrong about the world and not merely about formatting.
            total++;
            bool check1 = true;
            check1 &= Expect("USA GDP", 29000f, MoneyUnit.Billions, "$29.0T");
            check1 &= Expect("USA GovernmentDebt", 35960f, MoneyUnit.Billions, "$36.0T");
            check1 &= Expect("SocialSecurity", 1530f, MoneyUnit.Billions, "$1.53T");
            check1 &= Expect("Defense", 850f, MoneyUnit.Billions, "$850B");
            check1 &= Expect("Sweden SWF", 195f, MoneyUnit.Billions, "$195B");
            check1 &= Expect("a half-billion line", 0.5f, MoneyUnit.Billions, "$500M");
            if (check1) { passed++; }
            Debug.Log($"{(check1 ? "PASS" : "FAIL")} CHECK 1 real seed values render at their true magnitude.");

            // CHECK 2: the per-stat unit actually changes the answer. DerivedStats.GdpPerCapita is
            // thousands per person, and a formatter that assumed billions would render it a million times
            // too large - the one exception that forced the unit onto the stat rather than into here.
            total++;
            bool check2 = true;
            check2 &= Expect("GdpPerCapita (USA-ish)", 85.6f, MoneyUnit.Thousands, "$85.6k");
            check2 &= Expect("GdpPerCapita (Poland-ish)", 24.5f, MoneyUnit.Thousands, "$24.5k");
            bool unitsDiffer = UiFormat.Money(85.6f, MoneyUnit.Thousands) != UiFormat.Money(85.6f, MoneyUnit.Billions);
            if (!unitsDiffer) { Debug.Log("FAIL the two units render identically - the unit parameter does nothing."); }
            check2 &= unitsDiffer;
            if (check2) { passed++; }
            Debug.Log($"{(check2 ? "PASS" : "FAIL")} CHECK 2 the unit travels with the stat and changes the result.");

            // CHECK 3: signs and zero. TradeBalance, Budget and Net Government Position are all signed,
            // and the net position is specifically expected to go NEGATIVE once the debt floor is removed.
            total++;
            bool check3 = true;
            check3 &= Expect("negative trade balance", -1200f, MoneyUnit.Billions, "-$1.20T");
            check3 &= Expect("negative net position", -599f, MoneyUnit.Billions, "-$599B");
            check3 &= Expect("zero", 0f, MoneyUnit.Billions, "$0");
            check3 &= ExpectDelta("a positive change", 42f, MoneyUnit.Billions, "+$42.0B");
            check3 &= ExpectDelta("a negative change", -42f, MoneyUnit.Billions, "-$42.0B");
            check3 &= ExpectDelta("no change", 0f, MoneyUnit.Billions, "+$0");
            if (check3) { passed++; }
            Debug.Log($"{(check3 ? "PASS" : "FAIL")} CHECK 3 signed amounts and zero.");

            // CHECK 4: rounding must not hand a value the tier below its own text. Round-then-tier is the
            // whole reason the rounding happens in dollars rather than after scaling.
            total++;
            bool check4 = true;
            check4 &= Expect("just under a trillion", 999.97f, MoneyUnit.Billions, "$1.00T");
            check4 &= Expect("exactly a trillion", 1000f, MoneyUnit.Billions, "$1.00T");
            check4 &= Expect("just under a billion", 0.99997f, MoneyUnit.Billions, "$1.00B");
            if (check4) { passed++; }
            Debug.Log($"{(check4 ? "PASS" : "FAIL")} CHECK 4 tier boundaries survive rounding.");

            // CHECK 5: width and unambiguity, swept across every magnitude the model can produce. These
            // are the two properties the display sites depend on - an axis gutter is a few characters
            // wide, and a bare number with no symbol is exactly how "29k" passed for a plausible reading.
            total++;
            bool check5 = true;
            int cases = 0, widest = 0;
            string widestText = string.Empty;
            for (float v = -1e6f; v <= 1e6f; v = NextSweepValue(v))
            {
                foreach (MoneyUnit unit in new[] { MoneyUnit.Billions, MoneyUnit.Thousands })
                {
                    string text = UiFormat.Money(v, unit);
                    cases++;
                    if (text.Length > widest) { widest = text.Length; widestText = $"{v} {unit} -> {text}"; }
                    if (!text.Contains("$"))
                    {
                        Debug.Log($"FAIL {v} {unit} rendered \"{text}\" with no currency symbol.");
                        check5 = false;
                    }
                }
            }
            if (widest > 8) { Debug.Log($"FAIL widest result is {widest} chars: {widestText}"); check5 = false; }
            if (check5) { passed++; }
            Debug.Log($"{(check5 ? "PASS" : "FAIL")} CHECK 5 {cases} magnitudes swept; widest {widest} chars ({widestText}); every result carries a symbol.");

            // CHECK 6: non-finite input must not throw. Every call site is inside OnGUI, and an exception
            // there aborts the rest of the frame - which is exactly how the sparkline defect blanked the
            // Budget screen (visual review item 9). A4's trajectory run says these values stay finite;
            // this says that if one ever does not, the player sees a strange number and not a black
            // screen. Correct-looking output is not the bar here, surviving is.
            total++;
            bool check6 = true;
            foreach (float v in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.MaxValue, float.MinValue, float.Epsilon })
            {
                foreach (MoneyUnit unit in new[] { MoneyUnit.Billions, MoneyUnit.Thousands })
                {
                    try
                    {
                        string text = UiFormat.Money(v, unit);
                        Debug.Log($"  ok   survived {v} {unit} -> \"{text}\"");
                    }
                    catch (System.Exception e)
                    {
                        Debug.Log($"  FAIL {v} {unit} threw {e.GetType().Name}: {e.Message}");
                        check6 = false;
                    }
                }
            }
            if (check6) { passed++; }
            Debug.Log($"{(check6 ? "PASS" : "FAIL")} CHECK 6 non-finite and extreme input cannot throw inside OnGUI.");

            Debug.Log($"=== UiFormat.Money: {passed} of {total} PASS ===");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }

        /// <summary>Geometric sweep with a zero crossing, so every tier and both signs get hit.</summary>
        private static float NextSweepValue(float v)
        {
            if (v < -1f) { return v / 7f; }
            if (v < 0f) { return 0f; }
            if (v == 0f) { return 1e-6f; }
            return v * 7f;
        }

        private static bool Expect(string label, float value, MoneyUnit unit, string expected)
        {
            string actual = UiFormat.Money(value, unit);
            bool ok = actual == expected;
            Debug.Log($"  {(ok ? "ok  " : "FAIL")} {label}: {value} {unit} -> \"{actual}\"" +
                (ok ? string.Empty : $", expected \"{expected}\""));
            return ok;
        }

        private static bool ExpectDelta(string label, float value, MoneyUnit unit, string expected)
        {
            string actual = UiFormat.MoneyDelta(value, unit);
            bool ok = actual == expected;
            Debug.Log($"  {(ok ? "ok  " : "FAIL")} {label}: {value} {unit} -> \"{actual}\"" +
                (ok ? string.Empty : $", expected \"{expected}\""));
            return ok;
        }
    }
}
