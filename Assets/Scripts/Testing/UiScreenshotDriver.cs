using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PoliSim.Data;
using PoliSim.UI;
using UnityEngine;

namespace PoliSim.Testing
{
    /// <summary>
    /// Drives <see cref="GameController"/> to a set of known screens and captures each one.
    ///
    /// **Promoted from scratch tooling into the repo on 2026-08-10**, because the v2.0 restyle converts
    /// one row type at a time and every conversion needs to be SEEN rather than described. It lived in a
    /// throwaway project copy for the font test; keeping it there meant every visual round began by
    /// rebuilding it, and meant the half-converted states nobody can judge from a diff were never
    /// looked at.
    ///
    /// It reaches into GameController's private state by reflection rather than adding test hooks to
    /// production code. That trade is deliberate: the alternative is widening a 5,000-line class's
    /// public surface for a screenshot, and a reflection failure here is LOUD - logged and skipped, with
    /// the screen named - rather than silently capturing the wrong screen, which is the only outcome
    /// that would actually mislead.
    ///
    /// <para><b>Captures happen after `WaitForEndOfFrame`, which is the only point IMGUI has actually
    /// composited</b> - and is why the runner cannot use `-batchmode`. See <c>Capture</c>.</para>
    /// </summary>
    public class UiScreenshotDriver : MonoBehaviour
    {
        public string OutputDirectory = "screenshots";
        public string Label = "run";

        /// <summary>Frames to let IMGUI settle before a capture. IMGUI lays out on the frame it draws, so a screen switched to on frame N is not fully measured until N+1; four is cheap insurance rather than a measured minimum.</summary>
        private const int SettleFrames = 4;

        private static readonly string[] Tabs =
        {
            "Statistics", "Decisions", "Demographics", "Budget", "PolicyLaws", "Politics"
        };

        /// <summary>
        /// Tabs whose sub-selector changes which row type is on screen, and the private enum field that
        /// drives each one.
        ///
        /// **This is what a tab-level capture cannot show.** The v2.0 conversion runs per ROW TYPE, and
        /// row types live behind sub-selectors - so one screenshot per tab shows whichever sub-screen
        /// happened to be selected and silently omits the rest. During a conversion that is exactly the
        /// wrong picture, because the half-converted state (one sub-screen restyled, its neighbours not)
        /// is the most useful thing a capture can produce.
        ///
        /// **A table rather than a branch per tab.** It was a hardcoded Budget branch until Policy/Laws
        /// needed the same treatment; a second special case is the point at which the shape should stop
        /// being special. Politics (Parliament/Compass/Cabinet/FederalReserve) will drop in here as one
        /// more line when its turn comes.
        /// </summary>
        private static readonly Dictionary<string, KeyValuePair<string, string[]>> SubScreens =
            new Dictionary<string, KeyValuePair<string, string[]>>
            {
                { "Budget", new KeyValuePair<string, string[]>("_budgetProcessCategory",
                    new[] { "Tax", "Spending", "Welfare", "Infrastructure", "Swf" }) },
                { "PolicyLaws", new KeyValuePair<string, string[]>("_policyLawsCategory",
                    new[] { "LaborMarket", "CrimeJustice", "Sectors", "PolicyWeb", "Trade" }) }
            };

        private IEnumerator Start()
        {
            Directory.CreateDirectory(OutputDirectory);

            GameController controller = FindAnyObjectByType<GameController>();
            if (controller == null)
            {
                Debug.LogError("SHOT: no GameController in scene - nothing to capture.");
                Finish(1);
                yield break;
            }

            yield return Settle();
            yield return Capture("01_country_selector");

            Invoke(controller, "SelectPlayerCountry", CountryId.USA);
            yield return Settle();

            for (int i = 0; i < Tabs.Length; i++)
            {
                if (!SetEnumField(controller, "_consolidatedTab", Tabs[i]))
                {
                    continue;
                }

                yield return Settle();
                yield return Capture($"{i + 2:00}_{Tabs[i].ToLowerInvariant()}");

                if (!SubScreens.TryGetValue(Tabs[i], out KeyValuePair<string, string[]> sub))
                {
                    continue;
                }

                for (int c = 0; c < sub.Value.Length; c++)
                {
                    if (!SetEnumField(controller, sub.Key, sub.Value[c]))
                    {
                        continue;
                    }

                    string stem = $"{i + 2:00}{(char)('a' + c)}_{Tabs[i].ToLowerInvariant()}_{sub.Value[c].ToLowerInvariant()}";

                    ResetScrolls(controller);
                    yield return Settle();
                    yield return Capture(stem);

                    // ⚠ A SECOND CAPTURE, SCROLLED PAST THE PREAMBLE. Every sub-screen opens with a
                    // header, explanatory prose and a summary block that together fill most of the panel,
                    // so a capture at scroll zero shows one or two rows and answers nothing about
                    // density - which on Spending's 29 categories was the entire question D3 was about.
                    // Scrolling far enough to put rows on screen is the only way to see whether a
                    // treatment that reads well at 13 rows survives at 29.
                    ScrollBy(controller, 900f);
                    yield return Settle();
                    yield return Capture(stem + "_rows");

                    // ⚠ A THIRD CAPTURE, DEEPER STILL. 900px clears the preamble and shows the first few
                    // rows, which answers "does a row render". It does not answer "does this hold at
                    // depth" - and on the two screens where that is the real question it lands inside the
                    // first group. Economic Sectors is eight sectors x five dials = FORTY rows, and the
                    // whole point of the group headers is what happens at a sector BOUNDARY, which is
                    // ~1400px down. Spending's 29-row discretionary tail is the same shape of question.
                    ScrollBy(controller, 2200f);
                    yield return Settle();
                    yield return Capture(stem + "_deep");
                }

                SetEnumField(controller, sub.Key, sub.Value[0]);
            }

            Debug.Log($"SHOT: done, {_captured} captured, {_failed} failed.");
            Finish(_failed == 0 ? 0 : 1);
        }

        private IEnumerator Settle()
        {
            for (int i = 0; i < SettleFrames; i++)
            {
                yield return null;
            }
        }

        private int _captured;
        private int _failed;

        private IEnumerator Capture(string name)
        {
            // ⚠ THIS IS WHY THE RUNNER CANNOT USE -batchmode.
            //
            // `WaitForEndOfFrame` NEVER RESUMES in batchmode - the coroutine simply stops, the capture
            // never happens, and Unity sits there until something kills it. That is exactly how the
            // first attempt at this failed: it attached, logged "driver attached, label=none, 640x480",
            // and then hung with no error. A silent hang, not a stack trace.
            //
            // Running with a real Editor window fixes both halves at once: the coroutine resumes, and
            // the Game View is a real size instead of batchmode's 640x480 default - which matters more
            // than usual here, because every style in this UI derives its font size from Screen.height,
            // so a 640x480 capture would be showing font sizes no player ever sees.
            yield return new WaitForEndOfFrame();

            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
            if (shot == null)
            {
                Debug.LogError($"SHOT: capture returned null for {name}.");
                _failed++;
                yield break;
            }

            string path = Path.Combine(OutputDirectory, $"{Label}_{name}.png");
            File.WriteAllBytes(path, shot.EncodeToPNG());
            Debug.Log($"SHOT: wrote {path} at {shot.width}x{shot.height}");
            _captured++;
            Destroy(shot);
        }

        private static void Finish(int exitCode)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(exitCode);
#endif
        }

        private static void Invoke(object target, string method, object arg)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            if (m == null)
            {
                Debug.LogError($"SHOT: method {method} not found - the screen will be WRONG, not missing.");
                return;
            }

            m.Invoke(target, new[] { arg });
        }

        /// <summary>
        /// The left column's own scroll view, deliberately left alone so it stays identical between
        /// captures and cannot be mistaken for part of what changed.
        /// </summary>
        private const string LeftColumnScrollField = "_leftColumnScrollPosition";

        private static FieldInfo[] _scrollFields;

        /// <summary>
        /// Every `Vector2 *ScrollPosition` field on GameController, DISCOVERED rather than listed.
        ///
        /// ⚠ **A hardcoded list of six shipped and was silently wrong.** Policy/Laws does not scroll
        /// through `_policyLawsContentScrollPosition` - each of its sub-screens owns its own
        /// (`_laborMarketScrollPosition`, `_crimeJusticeScrollPosition`, `_sectorPolicyScrollPosition`),
        /// none of which was in the list. So every "scrolled" capture of that tab was a no-op that
        /// happened to look plausible, because the preamble there is short enough that rows appear
        /// anyway. The deep pass is what exposed it: identical to the shallow one, pixel for pixel.
        ///
        /// There are eighteen such fields. Enumerating them is the same discipline
        /// `ChromeV2CoverageCheck` applies to sprites - a list omits whatever it forgot, and cannot tell
        /// you that it did. Only one view is on screen at a time, so setting them all is harmless.
        /// </summary>
        private static FieldInfo[] ScrollFields(object target)
        {
            if (_scrollFields != null)
            {
                return _scrollFields;
            }

            var found = new List<FieldInfo>();
            foreach (FieldInfo f in target.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (f.FieldType == typeof(Vector2) && f.Name.EndsWith("ScrollPosition") && f.Name != LeftColumnScrollField)
                {
                    found.Add(f);
                }
            }

            _scrollFields = found.ToArray();
            Debug.Log($"SHOT: driving {_scrollFields.Length} scroll views.");
            return _scrollFields;
        }

        private static void ResetScrolls(object target) => SetScrolls(target, 0f);

        private static void ScrollBy(object target, float y) => SetScrolls(target, y);

        private static void SetScrolls(object target, float y)
        {
            foreach (FieldInfo f in ScrollFields(target))
            {
                f.SetValue(target, new Vector2(0f, y));
            }
        }

        private static bool SetEnumField(object target, string field, string value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null)
            {
                Debug.LogError($"SHOT: field {field} not found - skipping rather than capturing the wrong screen.");
                return false;
            }

            // Enum.Parse throws on a name the enum does not carry, which is what should happen: a
            // renamed sub-category must fail loudly here rather than quietly capturing whichever screen
            // was already up. Every caller checks the return and skips.
            try
            {
                f.SetValue(target, System.Enum.Parse(f.FieldType, value));
                return true;
            }
            catch (System.ArgumentException)
            {
                Debug.LogError($"SHOT: '{value}' is not a member of {f.FieldType.Name} - skipping.");
                return false;
            }
        }
    }
}
