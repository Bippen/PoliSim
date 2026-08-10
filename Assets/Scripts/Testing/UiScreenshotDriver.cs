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
        /// The Budget screen's five sub-categories, captured individually.
        ///
        /// **This is what the tab-level capture could not show.** The v2.0 conversion runs per ROW TYPE,
        /// and all five row types live behind sub-selectors on one tab - so a single "Budget" screenshot
        /// shows whichever sub-category happened to be selected and silently omits the other four. While
        /// the screen is half-converted that is exactly the wrong picture: Tax is `LedgerRow` and
        /// Spending is still the old stacked form, and the comparison between them is the most useful
        /// thing a capture can currently produce.
        /// </summary>
        private static readonly string[] BudgetCategories =
        {
            "Tax", "Spending", "Welfare", "Infrastructure", "Swf"
        };

        private IEnumerator Start()
        {
            Directory.CreateDirectory(OutputDirectory);

            GameController controller = FindFirstObjectByType<GameController>();
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

                if (Tabs[i] != "Budget")
                {
                    continue;
                }

                // Budget is the only tab whose sub-selector changes which row type is on screen, so it
                // is the only one that gets a capture per sub-category.
                for (int c = 0; c < BudgetCategories.Length; c++)
                {
                    if (!SetEnumField(controller, "_budgetProcessCategory", BudgetCategories[c]))
                    {
                        continue;
                    }

                    yield return Settle();
                    yield return Capture($"{i + 2:00}{(char)('a' + c)}_budget_{BudgetCategories[c].ToLowerInvariant()}");
                }

                SetEnumField(controller, "_budgetProcessCategory", "Tax");
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
