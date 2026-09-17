using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PoliSim.Testing
{
    /// <summary>
    /// **The dry film's IMGUI pass, and the label table both kinds of film write (2026-09-17).**
    ///
    /// <para><b>Why a frame was never needed.</b> An IMGUI screen's rects exist the moment its Repaint
    /// event runs: GUILayout computes them on the CPU during Layout and hands them back at Repaint, and
    /// `UiOverflowGuard` / `UiContainmentGuard` already measure there, before any pixel is composited. The
    /// film needed a window only because Unity delivers OnGUI's events from the Game View. Under
    /// `-batchmode` nothing delivers them, so this delivers them itself: the IMGUIContainer idiom -
    /// `GUIUtility.BeginContainer`, `GUILayoutUtility.BeginContainer`, the controller's own `OnGUI`, then
    /// `GUILayoutUtility.LayoutFromContainer` at the screen size - once for Layout and once for Repaint,
    /// with the mouse held off the screen as the film's parked cursor holds it.</para>
    ///
    /// <para>⚠ <b>Those entry points are Unity internals, reached by reflection</b> (measured present on
    /// 6000.5.6f1: `BeginContainer(ObjectGUIState)`, `LayoutCache(int)`, `LayoutFromContainer(float,
    /// float)`, `GUIStyle.onDraw`). An upgrade that renames one makes <see cref="Prepare"/> fail and the dry
    /// film exit 1 naming the member - it can never degrade to a pass that measured nothing.</para>
    ///
    /// <para><b>The label table.</b> `GUIStyle.onDraw` sees every styled draw at Repaint - labels, boxes,
    /// buttons, MeasuredLabel's shrunk text and the 271 raw `GUI.Label`/`GUILayout.Label` sites the overflow
    /// guard cannot see. For each draw with text, in the captured frame only, the table records its rect,
    /// its type size, what its text needs (`CalcSize`, or `CalcHeight` at the rect's width when it wraps)
    /// and whether the rect straddles or leaves the visible clip. ⚠ **The table REPORTS; it does not
    /// judge.** The film's verdict stays the guards' (their populations are defined and their false
    /// positives were cleared); the table is how a layout iteration reads what moved, row by row, and a
    /// film and a dry film of one tree write the same table.</para>
    /// </summary>
    public static class DryGuiPass
    {
#if UNITY_EDITOR
        private const BindingFlags All = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static bool _prepared;
        private static object _objectGuiState;
        private static object _layoutCache;
        private static MethodInfo _beginContainer;
        private static MethodInfo _endContainer;
        private static MethodInfo _layoutBeginContainer;
        private static MethodInfo _layoutFromContainer;
        private static MethodInfo _resetGlobalState;
        private static FieldInfo _skinMode;
        private static PropertyInfo _visibleRect;
        private static bool _hooked;

        /// <summary>Why <see cref="Prepare"/> failed, naming the member; null while it has not.</summary>
        public static string Failure { get; private set; }

        private static bool _recording;
        private static string _capture;
        private static readonly List<string> Rows = new List<string>();
        private static readonly HashSet<string> RowKeys = new HashSet<string>();
        private static int _texts, _wide, _tall, _straddle, _hidden;

        /// <summary>Text draws recorded, and how many exceed their rect or their clip. Report figures, never a verdict.</summary>
        public static string Summary =>
            string.Format(CultureInfo.InvariantCulture,
                "{0} text draw(s) measured; {1} need more width than their rect (unwrapped), {2} more height (wrapped); {3} straddle their clip, {4} outside it",
                _texts, _wide, _tall, _straddle, _hidden);

        /// <summary>Installs the draw hook (films and dry films) - idempotent, and harmless when it fails: the table is then empty and says so.</summary>
        public static void InstallHook()
        {
            if (_hooked) { return; }
            try
            {
                FieldInfo onDraw = typeof(GUIStyle).GetField("onDraw", All);
                if (onDraw == null) { Debug.LogWarning("SHOT: GUIStyle.onDraw not found - the label table will be empty on this Unity."); return; }
                MethodInfo invoke = onDraw.FieldType.GetMethod("Invoke");
                ParameterExpression[] ps = invoke.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
                if (ps.Length < 3 || ps[0].Type != typeof(GUIStyle) || ps[1].Type != typeof(Rect) || ps[2].Type != typeof(GUIContent) || invoke.ReturnType != typeof(bool))
                {
                    Debug.LogWarning("SHOT: GUIStyle.onDraw has an unexpected shape - the label table will be empty on this Unity.");
                    return;
                }

                Expression call = Expression.Call(typeof(DryGuiPass).GetMethod(nameof(OnDraw), All), ps[0], ps[1], ps[2]);
                Delegate previous = onDraw.GetValue(null) as Delegate;
                Delegate ours = Expression.Lambda(onDraw.FieldType, call, ps).Compile();
                onDraw.SetValue(null, previous == null ? ours : Delegate.Combine(previous, ours));
                _visibleRect = typeof(GUIUtility).Assembly.GetType("UnityEngine.GUIClip")?.GetProperty("visibleRect", All);
                _skinMode = _skinMode ?? typeof(GUIUtility).GetField("s_SkinMode", All);
                _hooked = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SHOT: the label table's draw hook did not install ({e.GetType().Name}: {e.Message}) - the table will be empty.");
            }
        }

        /// <summary>Resolves the IMGUI container entry points the dry pass drives. False, with <see cref="Failure"/> naming the member, on any Unity that lacks one.</summary>
        public static bool Prepare()
        {
            if (_prepared) { return true; }
            Assembly imgui = typeof(GUIUtility).Assembly;
            Type objectGuiState = imgui.GetType("UnityEngine.ObjectGUIState");
            Type layoutCache = imgui.GetType("UnityEngine.GUILayoutUtility+LayoutCache");
            if (objectGuiState == null) { Failure = "UnityEngine.ObjectGUIState"; return false; }
            if (layoutCache == null) { Failure = "UnityEngine.GUILayoutUtility+LayoutCache"; return false; }

            _beginContainer = typeof(GUIUtility).GetMethods(All).FirstOrDefault(m => m.Name == "BeginContainer" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == objectGuiState);
            _endContainer = typeof(GUIUtility).GetMethods(All).FirstOrDefault(m => m.Name == "EndContainer" && m.GetParameters().Length == 0);
            _layoutBeginContainer = typeof(GUILayoutUtility).GetMethods(All).FirstOrDefault(m => m.Name == "BeginContainer" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == layoutCache);
            _layoutFromContainer = typeof(GUILayoutUtility).GetMethods(All).FirstOrDefault(m => m.Name == "LayoutFromContainer" && m.GetParameters().Length == 2);
            _resetGlobalState = typeof(GUIUtility).GetMethod("ResetGlobalState", All, null, Type.EmptyTypes, null);
            _skinMode = typeof(GUIUtility).GetField("s_SkinMode", All);
            ConstructorInfo cacheCtor = layoutCache.GetConstructors(All).FirstOrDefault(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(int));

            if (_beginContainer == null) { Failure = "GUIUtility.BeginContainer(ObjectGUIState)"; return false; }
            if (_endContainer == null) { Failure = "GUIUtility.EndContainer()"; return false; }
            if (_layoutBeginContainer == null) { Failure = "GUILayoutUtility.BeginContainer(LayoutCache)"; return false; }
            if (_layoutFromContainer == null) { Failure = "GUILayoutUtility.LayoutFromContainer(float, float)"; return false; }
            if (_resetGlobalState == null || _skinMode == null)
            {
                Debug.LogWarning($"SHOT: DRY - {(_skinMode == null ? "GUIUtility.s_SkinMode" : "GUIUtility.ResetGlobalState()")} not found; the pass runs without it, so compare the label table with a film before trusting a type size.");
            }
            if (cacheCtor == null) { Failure = "GUILayoutUtility.LayoutCache(int)"; return false; }

            _objectGuiState = Activator.CreateInstance(objectGuiState, true);
            _layoutCache = cacheCtor.Invoke(new object[] { 1 });
            _prepared = true;
            return true;
        }

        /// <summary>The OnGUI exceptions logged in full before the pass only counts them - a screen that throws every frame
        /// must fail the run, not fill the disk (the first dry film wrote 1.5 GB of one exception in ten minutes).</summary>
        private const int LoggedOnGuiExceptions = 20;
        private static int _onGuiExceptions;

        /// <summary>
        /// One IMGUI frame of <paramref name="target"/>'s OnGUI at the seam's screen size: a Layout event, then a Repaint.
        /// ⚠ False when the pass's own machinery failed (not the screen's code): <see cref="Failure"/> names it, and the
        /// caller stops pumping - the dry film ends there rather than repeating the fault every frame.
        /// </summary>
        public static bool Pump(MonoBehaviour target, MethodInfo onGui)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            bool ok = Deliver(target, onGui, EventType.Layout);
            LastLayoutMilliseconds = clock.ElapsedMilliseconds;
            ok = ok && Deliver(target, onGui, EventType.Repaint);
            LastPassMilliseconds = clock.ElapsedMilliseconds;
            return ok;
        }

        /// <summary>The last pump's Layout event, and the whole pass, in milliseconds - a screen that is slow to draw is slow in a film too, and this is where it shows.</summary>
        public static long LastLayoutMilliseconds { get; private set; }

        /// <summary>See <see cref="LastLayoutMilliseconds"/>.</summary>
        public static long LastPassMilliseconds { get; private set; }

        private static bool Deliver(MonoBehaviour target, MethodInfo onGui, EventType type)
        {
            Event.current = new Event { type = type, mousePosition = new Vector2(-10000f, -10000f) };
            try
            {
                _beginContainer.Invoke(null, new[] { _objectGuiState });
            }
            catch (TargetInvocationException e)
            {
                Failure = $"GUIUtility.BeginContainer threw {(e.InnerException ?? e).GetType().Name}: {(e.InnerException ?? e).Message}";
                return false;
            }

            try
            {
                // The game skin, as a player's OnGUI gets it (skin mode 0), and the per-pass state BeginGUI resets - both
                // inside the container, where GUI calls are legal (ResetGlobalState assigns GUI.skin).
                _skinMode?.SetValue(null, 0);
                _resetGlobalState?.Invoke(null, null);
                _layoutBeginContainer.Invoke(null, new[] { _layoutCache });
                try
                {
                    onGui.Invoke(target, null);
                }
                catch (TargetInvocationException e) when (e.InnerException is ExitGUIException)
                {
                    // GUIUtility.ExitGUI - the ordinary early exit of an IMGUI pass, not a failure.
                }
                catch (TargetInvocationException e)
                {
                    // Logged as Unity logs an OnGUI exception, so the driver's log fold counts it - in full up to the cap.
                    _onGuiExceptions++;
                    if (_onGuiExceptions <= LoggedOnGuiExceptions) { Debug.LogException(e.InnerException ?? e); }
                    else if (_onGuiExceptions == LoggedOnGuiExceptions + 1) { Debug.LogError($"SHOT: DRY - OnGUI has thrown {_onGuiExceptions} times; further exceptions are counted, not printed."); }
                }

                if (type == EventType.Layout)
                {
                    _layoutFromContainer.Invoke(null, new object[] { (float)PoliSim.UI.UiScreen.Width, (float)PoliSim.UI.UiScreen.Height });
                }

                return true;
            }
            catch (TargetInvocationException e)
            {
                Failure = $"the IMGUI container pass threw {(e.InnerException ?? e).GetType().Name}: {(e.InnerException ?? e).Message}";
                return false;
            }
            finally
            {
                try { _endContainer.Invoke(null, null); }
                catch (TargetInvocationException e) { Failure = Failure ?? $"GUIUtility.EndContainer threw {(e.InnerException ?? e).Message}"; }
            }
        }

        /// <summary>Starts recording the text draws of the frame a capture takes.</summary>
        public static void BeginCapture(string capture)
        {
            _capture = capture;
            _recording = true;
        }

        /// <summary>Stops recording.</summary>
        public static void EndCapture()
        {
            _recording = false;
        }

        private static bool OnDraw(GUIStyle style, Rect rect, GUIContent content)
        {
            if (!_recording || Event.current == null || Event.current.type != EventType.Repaint) { return false; }

            // The game draws with the game skin (mode 0). While a film records, the Editor's own windows - the Game View's
            // toolbar, the Hierarchy - draw through the same hook with the Editor skin, and they are not the game.
            if (_skinMode != null && _skinMode.GetValue(null) is int mode && mode != 0) { return false; }
            string text = content?.text;
            if (string.IsNullOrEmpty(text) || style == null) { return false; }

            try
            {
                float neededWidth, neededHeight;
                if (style.wordWrap)
                {
                    neededWidth = 0f;
                    neededHeight = style.CalcHeight(content, rect.width);
                }
                else
                {
                    Vector2 size = style.CalcSize(content);
                    neededWidth = size.x;
                    neededHeight = size.y;
                }

                string clip = "in";
                if (_visibleRect != null)
                {
                    var visible = (Rect)_visibleRect.GetValue(null);
                    bool overlaps = rect.xMax > visible.xMin && rect.xMin < visible.xMax && rect.yMax > visible.yMin && rect.yMin < visible.yMax;
                    bool inside = rect.xMin >= visible.xMin - 0.5f && rect.xMax <= visible.xMax + 0.5f && rect.yMin >= visible.yMin - 0.5f && rect.yMax <= visible.yMax + 0.5f;
                    clip = !overlaps ? "outside" : inside ? "in" : "straddles";
                }

                bool wide = !style.wordWrap && neededWidth > rect.width + 1f;
                bool tall = style.wordWrap && neededHeight > rect.height + 1f;
                Rect screen = GUIUtility.GUIToScreenRect(rect);
                string flags = (wide ? "WIDE " : string.Empty) + (tall ? "TALL " : string.Empty) + (clip == "in" ? string.Empty : clip.ToUpperInvariant());
                string row = string.Format(CultureInfo.InvariantCulture,
                    "{0}\t{1}x{2}\t{3:F1}\t{4:F1}\t{5:F1}\t{6:F1}\t{7}\t{8}\t{9:F1}\t{10:F1}\t{11}\t{12}",
                    _capture, PoliSim.UI.UiScreen.Width, PoliSim.UI.UiScreen.Height, screen.x, screen.y, rect.width, rect.height,
                    style.fontSize, style.wordWrap ? "wrap" : "line", neededWidth, neededHeight, flags.Trim(), Escape(text));

                // Once per capture: the Game View can deliver more than one Repaint to a frame.
                if (!RowKeys.Add(row)) { return false; }
                Rows.Add(row);
                _texts++;
                if (wide) { _wide++; }
                if (tall) { _tall++; }
                if (clip == "straddles") { _straddle++; }
                if (clip == "outside") { _hidden++; }
            }
            catch (Exception)
            {
                // A measurement must never break the draw it watches.
            }

            return false;
        }

        private static string Escape(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        /// <summary>Writes the run's label table (tab-separated, invariant numbers). Returns the path, or null when nothing was recorded.</summary>
        public static string WriteTable(string directory, string label)
        {
            if (Rows.Count == 0) { return null; }
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, label + "_labels.tsv");
            var sb = new StringBuilder("capture\tscreen\tx\ty\twidth\theight\tfont\tmode\tneeds_width\tneeds_height\tflags\ttext\n");
            foreach (string row in Rows) { sb.Append(row).Append('\n'); }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return path;
        }
#endif
    }
}
