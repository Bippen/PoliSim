using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Simulation;
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
                    new[] { "LaborMarket", "CrimeJustice", "Sectors", "PolicyWeb", "Trade" }) },
                { "Statistics", new KeyValuePair<string, string[]>("_statisticsCategory",
                    new[] { "Domestic", "International" }) },
                { "Politics", new KeyValuePair<string, string[]>("_politicsCategory",
                    new[] { "Parliament", "Compass", "Cabinet", "FederalReserve" }) }
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

            AdvanceDays(controller);
            DivergeSwfWeights(controller);
            DraftSpendingLines(controller);
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

            int overflows = ReportOverflows();
            Debug.Log($"SHOT: {overflows} text overflow(s) recorded.");
            Finish(_failed == 0 && overflows == 0 ? 0 : 1);
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
            // ⚠ THE SCREENSHOT IS NOT THE VERDICT. Every one of this project's eleven clipping instances
            // was found by a person looking at a screen, and twelve screens were captured and APPROVED
            // by eye with overflows still in them. So each shot is CHECKED as well as taken.
            //
            // Labelled here rather than after the capture because MeasuredLabel records during the OnGUI
            // of the frame being captured - naming the screen afterwards would file this screen's
            // overflows under the next one.
            UiOverflowGuard.CurrentScreen = name;

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

        /// <summary>Prints every overflow the guard recorded and returns the count. Already deduped by the guard itself - see the note there on why that has to happen at record time.</summary>
        private static int ReportOverflows()
        {
#if UNITY_EDITOR
            foreach (UiOverflowGuard.Violation v in UiOverflowGuard.Violations)
            {
                Debug.LogError($"OVERFLOW: {v}");
            }

            // ⚠ THE TOTAL, NOT THE LIST LENGTH. The list is capped at 200 to bound memory; the first
            // pass reported exactly 200 and that was read as the count when it was the ceiling - the
            // real figure was 608. Print both, and fail on the total.
            int total = UiOverflowGuard.TotalViolations;
            if (total > UiOverflowGuard.Violations.Count)
            {
                Debug.LogError($"OVERFLOW: {total - UiOverflowGuard.Violations.Count} further violation(s) " +
                               $"beyond the {UiOverflowGuard.Violations.Count} printed above.");
            }

            return total;
#else
            return 0;
#endif
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

        /// <summary>
        /// Days to advance before capturing. Long enough that every history-dependent surface is
        /// populated: graphs plot, the map has trade lines, published bulletins exist, and
        /// `GraphRenderer`'s page row has more than one page to page through.
        ///
        /// ⚠ **The reason this exists is that turn-0 captures systematically under-tested the UI**, and it
        /// took three separate discoveries to see it. A chart with no data draws no plate, so
        /// `GraphRenderer` and `MapRenderer` sat on near-black grounds through every v2.0 capture and
        /// looked like paper. The page row emitted no controls, so a behaviour-5 defect was invisible.
        /// Anything whose appearance depends on history existing was, in effect, never captured at all.
        ///
        /// 3 turns x 365 days clears `WindowSize` (50) history points on the quarterly series, which is
        /// what makes the pagination row real rather than merely present.
        /// </summary>
        private const int MinWarmupDays = 365 * 3;

        /// <summary>Ceiling on the search for a preliminary release, so a schedule change can never turn the warm-up into an unbounded loop - the same bounded-retry discipline UiScreenshotCapture applies to waiting for play mode.</summary>
        private const int MaxWarmupDays = 365 * 5;

        /// <summary>
        /// Drives the simulation forward through the real `AdvanceDay` / `AdvanceTurn` pair, so warmed-up
        /// state is produced by the same path play produces it.
        ///
        /// ⚠ **`AdvanceDay` ALONE IS NOT ENOUGH, and the first attempt at this proved it.** `AdvanceDay`
        /// runs the daily systems and returns true on a turn boundary, but it is the CALLER that then
        /// calls `AdvanceTurn` - which is what appends `StatHistory`. Driving days alone advanced the
        /// calendar three years and moved debt from $36.0T to $41.9T, yet left the UI reading "Turn 0"
        /// and every graph saying "No data yet". A warm-up that moves the economy without producing
        /// history is precisely the failure this whole pass exists to correct, in miniature.
        /// </summary>
        private static void AdvanceDays(object controller)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(simField?.GetValue(controller) is SimulationManager sim))
            {
                Debug.LogError("SHOT: could not reach SimulationManager - capturing at turn 0, which under-tests every history-dependent surface.");
                return;
            }

            // Empty decisions: the warm-up is "time passed with no policy changes", which is the neutral
            // baseline a capture wants. Anything else would bake one playthrough's choices into what is
            // meant to be a picture of the UI.
            var noDecisions = new Dictionary<CountryId, PolicyDecision>();
            int turns = 0;
            int days = 0;
            for (int i = 0; i < MaxWarmupDays; i++)
            {
                if (sim.AdvanceDay())
                {
                    sim.AdvanceTurn(noDecisions);
                    turns++;
                }

                days++;

                // ⚠ STOP ON A PRELIMINARY RELEASE, not on a day count.
                //
                // Behaviour 6's whole point is the state where a figure is published but not yet
                // revised - badged, dated AND dashed at once. A fixed warm-up cannot capture it except
                // by luck: three years of history left every series reading FINAL, because everything
                // old enough to plot is also old enough to have been revised. The window between a
                // release and its revision is narrow and moves, so the driver has to WATCH for it
                // rather than guess a date.
                //
                // Minimum days first, so the graphs still have a trend to draw - a preliminary figure on
                // an empty chart would prove B6 and nothing else.
                if (days >= MinWarmupDays && AnyPreliminary(sim))
                {
                    Debug.Log($"SHOT: stopped on a PRELIMINARY release at day {days} / turn {turns} - behaviour 6's provisional state is on screen.");
                    return;
                }
            }

            Debug.LogWarning($"SHOT: advanced {days} days / {turns} turns without catching a preliminary release - B6's provisional case will NOT be in this capture.");
        }

        /// <summary>
        /// Drafts a Sovereign Wealth Fund and pushes ONE asset-class weight off its default.
        ///
        /// ⚠ **Without this the SWF trailing column cannot be judged, and was verified by argument only
        /// for two sessions.** The four weights default to 40/30/15/15, which sums to exactly 100 - so
        /// raw weight and normalised "% of fund" print identical numbers and the column looks like a
        /// restatement of the figure beside it. Its actual purpose is that dragging ONE weight moves the
        /// other three, and that is invisible until the four stop summing to 100.
        ///
        /// Equities to 80 makes the sum 140, so every share diverges from its raw weight at once
        /// (80 -> 57%, 30 -> 21%, 15 -> 11%) and the three untouched rows visibly move. Drafting the fund
        /// as well so the rows render interactive rather than disabled, which is also what puts the amber
        /// draft cue on screen - the row differs from standing, which is exactly what behaviour 1 marks.
        /// </summary>
        private static void DivergeSwfWeights(object controller)
        {
            if (!SetPrivateField(controller, "_swfExistsDraft", true)
                || !SetPrivateField(controller, "_swfEquitiesWeightInput", 80f))
            {
                Debug.LogWarning("SHOT: could not diverge SWF weights - the normalised column will read identical to raw and prove nothing.");
            }
        }

        /// <summary>
        /// Drafts two spending lines - one up, one down - so behaviour 1's carriers actually RENDER.
        ///
        /// Without this every spending row captures at zero change, which means the hatch band has zero
        /// width, the draft figure is null, and icon_pencil_draft never draws. The screen would look
        /// correct and prove nothing, exactly the way the SWF weight column did before
        /// <see cref="DivergeSwfWeights"/>. Two directions rather than one because the hatch is
        /// explicitly bidirectional (see LedgerRow.DrawTrackFurniture) and a single raise would leave
        /// the cut case unexercised.
        /// </summary>
        private static void DraftSpendingLines(object controller)
        {
            FieldInfo f = controller.GetType().GetField("_spendingLineInputs", BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null || !(f.GetValue(controller) is System.Collections.IDictionary inputs))
            {
                Debug.LogWarning("SHOT: could not draft spending lines - B1's hatch and pencil will not render and the capture proves nothing.");
                return;
            }

            // ⚠ SEEDED FROM THE COUNTRY, NOT FROM THE DICTIONARY'S EXISTING KEYS. `_spendingLineInputs`
            // is filled lazily as each row draws, so before the first Budget frame it is EMPTY - an
            // earlier version of this iterated its keys, drafted nothing, and reported success.
            object country = controller.GetType()
                .GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(controller);
            object lines = country?.GetType().GetProperty("SpendingLines")?.GetValue(country)
                ?? country?.GetType().GetField("SpendingLines")?.GetValue(country);

            if (!(lines is System.Collections.IEnumerable spendingLines))
            {
                Debug.LogWarning("SHOT: could not reach SpendingLines - B1's hatch and pencil will not render.");
                return;
            }

            int drafted = 0;
            foreach (object line in spendingLines)
            {
                object category = line.GetType().GetProperty("Category")?.GetValue(line)
                    ?? line.GetType().GetField("Category")?.GetValue(line);
                if (category == null)
                {
                    continue;
                }

                // EVERY line, alternating direction - not the first two. Drafting only a couple left the
                // carriers real but off-screen: the bill direction moved, proving the draft was live,
                // while every row in frame still rendered at zero change. A capture that depends on
                // which rows happen to be scrolled into view is not evidence.
                inputs[category] = (drafted % 2 == 0) ? 6f : -4f;
                drafted++;
            }

            Debug.Log($"SHOT: drafted {drafted} spending lines (+6%, -4%) so B1's hatch, draft figure and pencil render.");
        }

        private static bool SetPrivateField(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null || !f.FieldType.IsInstanceOfType(value))
            {
                Debug.LogError($"SHOT: field {field} not found or wrong type.");
                return false;
            }

            f.SetValue(target, value);
            return true;
        }

        /// <summary>True when ANY published series on the player country is currently sitting on a preliminary release - the state behaviour 6 exists to distinguish, and the one a fixed-length warm-up reliably misses.</summary>
        private static bool AnyPreliminary(SimulationManager sim)
        {
            Country country = sim.World?.GetCountry(CountryId.USA);
            if (country?.Published == null)
            {
                return false;
            }

            foreach (KeyValuePair<PublishedStat, PublishedSeries> kv in country.Published.Series)
            {
                PublishedEntry latest = kv.Value.Latest();
                if (latest != null && latest.Status == RevisionStatus.Preliminary)
                {
                    return true;
                }
            }

            return false;
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
