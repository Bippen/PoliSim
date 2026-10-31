using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

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
    ///
    /// <para><b>K321 (2026-10-30): what the declutter check reads, in the flags column</b> (the columns are unchanged; a reader that only
    /// asks for OUTSIDE reads every table as before). <c>MULTILINE</c> - the draw runs past one line: its needed height (wrapped at its
    /// width, or with its own line breaks) is over one and a half times its style's one-line height. <c>CONTROL</c> - the draw is a
    /// control's label: drawn by Unity's own control machinery (`GUIStyle.onDraw` hands it a control id above zero - a button or a
    /// toggle's own words; a label draws with none), or the ONE visible text draw inside a click target - a control drawn with no words
    /// through the empty style (`GUIStyle.none`) over a face: the chip idiom, where `DrawRowChip`, `DrawStripChip`, the desk's chips and
    /// the rail's tabs paint a label, then lay an invisible button over the same rect. A target holding two or more draws is a row or a
    /// card, and its words are not a label; a styled layout group draws its face as a box with a control id as well, and is never a
    /// target. The face's style cannot tell a control: every face here is a copy of the skin's label, and a copy keeps the label's name
    /// (so a style named button or toggle is the rare case, not the rule). <c>SLIPHEAD</c> /
    /// <c>SLIPLINE</c> (level 1) and <c>SLIPHEAD2</c> / <c>SLIPLINE2</c> (level 2) - the slip's own draws, named by the slip as it draws
    /// them (`UiGuardContext.DrawRole`). <c>CANVAS</c> - a real film's uGUI `Text`, walked at the capture (<see cref="RecordCanvas"/>):
    /// the election nights and the selector are Canvas, and the IMGUI hook never sees them. Beside the table a film writes the slip books
    /// its pages drew (<see cref="WriteSlipBooks"/>), which K321 (g)'s moved words are judged against.</para>
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

        /// <summary>One row of the table: its first ten columns as written, its flags (K321's are added after the draw where a capture's whole
        /// frame decides them - CONTROL), its text, and where it drew on the screen.</summary>
        private sealed class TableRow
        {
            public string Head;
            public List<string> Flags;
            public string Text;
            public Rect Screen;
            public bool Visible;
            public bool Slip;
            public bool Canvas;
        }

        private static readonly List<TableRow> Rows = new List<TableRow>();
        private static readonly HashSet<string> RowKeys = new HashSet<string>();
        private static int _texts, _wide, _tall, _straddle, _hidden;

        /// <summary>K321 (a): the capture's click targets - controls drawn with no words of their own, in screen space - and where its rows begin.</summary>
        private static readonly HashSet<Rect> Targets = new HashSet<Rect>();
        private static int _captureFrom;
        private static FieldInfo _statesControlId, _statesTextInput;
        private static readonly GUIContent OneLineProbe = new GUIContent("Ag");

        /// <summary>K321 (g): the slip books the captured frames drew, a row a head or a line (capture, level, key, role, text).</summary>
        private static readonly List<string> SlipRows = new List<string>();
        private static readonly HashSet<string> SlipRowKeys = new HashSet<string>();

        /// <summary>Text draws recorded, and how many exceed their rect or their clip. Report figures, never a verdict.</summary>
        public static string Summary =>
            string.Format(CultureInfo.InvariantCulture,
                "{0} text draw(s) measured; {1} need more width than their rect (unwrapped), {2} more height (wrapped); {3} straddle their clip, {4} outside it; {5} of them a real film's Canvas text (K321)",
                _texts, _wide, _tall, _straddle, _hidden, _canvasTexts);

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

                // K321 (a): the draw's states (a DrawStates struct, internal) reach OnDraw boxed, and its control id is read by reflection there -
                // inside OnDraw's own guard, so a Unity that renamed the field loses the CONTROL flag, never the draw.
                Expression states = ps.Length > 3 && ps[3].Type.IsValueType ? (Expression)Expression.Convert(ps[3], typeof(object)) : Expression.Constant(null, typeof(object));
                if (ps.Length > 3)
                {
                    _statesControlId = ps[3].Type.GetField("controlId", All);
                    _statesTextInput = ps[3].Type.GetField("hasTextInput", All);
                }
                if (_statesControlId == null) { Debug.LogWarning("SHOT: GUIStyle.onDraw carries no control id on this Unity - the label table's CONTROL flag (K321) cannot be read, and the declutter check's LABEL cap reads nothing."); }
                Expression call = Expression.Call(typeof(DryGuiPass).GetMethod(nameof(OnDraw), All), ps[0], ps[1], ps[2], states);
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
            _captureFrom = Rows.Count;
            Targets.Clear();
        }

        /// <summary>Stops recording. K321 (a): a click target the frame drew holding exactly ONE visible text draw makes that draw its label -
        /// CONTROL - decided here, once the whole frame has drawn (the chip paints its words before its button).</summary>
        public static void EndCapture()
        {
            _recording = false;
            foreach (Rect target in Targets)
            {
                int found = -1, count = 0;
                for (int i = _captureFrom; i < Rows.Count && count < 2; i++)
                {
                    TableRow r = Rows[i];
                    if (r.Canvas || r.Slip || !r.Visible || !target.Contains(r.Screen.center)) { continue; }
                    count++;
                    found = i;
                }
                if (count == 1 && !Rows[found].Flags.Contains("CONTROL")) { Rows[found].Flags.Add("CONTROL"); }
            }
            Targets.Clear();
        }

        private static bool OnDraw(GUIStyle style, Rect rect, GUIContent content, object states)
        {
            if (!_recording || Event.current == null || Event.current.type != EventType.Repaint) { return false; }

            // The game draws with the game skin (mode 0). While a film records, the Editor's own windows - the Game View's
            // toolbar, the Hierarchy - draw through the same hook with the Editor skin, and they are not the game.
            if (_skinMode != null && _skinMode.GetValue(null) is int mode && mode != 0) { return false; }
            if (style == null) { return false; }
            string text = content?.text;

            // K321 (a): Unity's control machinery (GUI.DoControl, the box) draws with the control's id; a label draws with none (-1). A text
            // field's draw is the player's own words, never a label.
            int controlId = -1;
            bool textInput = false;
            try
            {
                if (states != null && _statesControlId != null) { controlId = (int)_statesControlId.GetValue(states); }
                if (states != null && _statesTextInput != null) { textInput = (bool)_statesTextInput.GetValue(states); }
            }
            catch (Exception)
            {
                controlId = -1;
            }

            if (string.IsNullOrEmpty(text))
            {
                // A control with no words and no face of its own - the invisible button over a chip, a tab or a row, drawn through the empty
                // style - is a click target (EndCapture). ⚠ Only the empty style: a styled layout group (GUILayout.BeginVertical with a style,
                // GUI.BeginGroup) draws its face as a box with a control id too, and a card holding one caption is not a button.
                if (controlId > 0 && !textInput && ReferenceEquals(style, GUIStyle.none))
                {
                    try { Targets.Add(GUIUtility.GUIToScreenRect(rect)); }
                    catch (Exception) { }
                }
                return false;
            }

            try
            {
                float neededWidth, neededHeight, oneLine;
                if (style.wordWrap)
                {
                    neededWidth = 0f;
                    neededHeight = style.CalcHeight(content, rect.width);
                    oneLine = style.CalcHeight(OneLineProbe, 100000f);
                }
                else
                {
                    Vector2 size = style.CalcSize(content);
                    neededWidth = size.x;
                    neededHeight = size.y;
                    oneLine = style.CalcSize(OneLineProbe).y;
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
                var flags = new List<string>();
                if (wide) { flags.Add("WIDE"); }
                if (tall) { flags.Add("TALL"); }
                if (clip != "in") { flags.Add(clip.ToUpperInvariant()); }
                if (oneLine > 0f && neededHeight > oneLine * 1.5f) { flags.Add("MULTILINE"); }
                string role = PoliSim.UI.UiGuardContext.DrawRole;
                if (!string.IsNullOrEmpty(role)) { flags.Add(role); }
                if (controlId > 0 && !textInput) { flags.Add("CONTROL"); }
                string head = string.Format(CultureInfo.InvariantCulture,
                    "{0}\t{1}x{2}\t{3:F1}\t{4:F1}\t{5:F1}\t{6:F1}\t{7}\t{8}\t{9:F1}\t{10:F1}",
                    _capture, PoliSim.UI.UiScreen.Width, PoliSim.UI.UiScreen.Height, screen.x, screen.y, rect.width, rect.height,
                    style.fontSize, style.wordWrap ? "wrap" : "line", neededWidth, neededHeight);

                // Once per capture: the Game View can deliver more than one Repaint to a frame.
                if (!RowKeys.Add(head + "\t" + string.Join(" ", flags) + "\t" + Escape(text))) { return false; }
                Rows.Add(new TableRow { Head = head, Flags = flags, Text = text, Screen = screen, Visible = clip != "outside", Slip = role != null && role.StartsWith("SLIP", StringComparison.Ordinal) });
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

        /// <summary>
        /// K321 (a): **A REAL FILM'S CANVAS TEXT, AT THE CAPTURE.** The election nights, the selector, the signing screen are uGUI, which the
        /// IMGUI hook never sees, so a night's frame held only the strip's few draws. Called by a real film after the frame has rendered and
        /// before the capture ends: every active, visible `Text` under every live root Canvas is a row flagged CANVAS - its rect on the screen
        /// in pixels, its type size in pixels (the authored size times the canvas's scale), its preferred size in pixels, wrapped or one line,
        /// MULTILINE when the generator lays it on more than one line (the rect's width, every line counted: a truncating rect drops lines it
        /// still lays out), OUTSIDE or STRADDLES against the screen, CONTROL when it is the one visible Text under a `Selectable` (a faced
        /// button's word; the flag is added once the frame's Texts are all read, as an IMGUI target's is). Visible: enabled, not culled, an inherited
        /// alpha above a hundredth.
        /// ⚠ A dry film never calls this: with no window a Canvas is scaled by a 640x480 backbuffer, whatever geometry the run names (§578).
        /// A rect a RectMask2D cuts partly is read whole (DECLARED).
        /// </summary>
        public static void RecordCanvas()
        {
            if (!_recording) { return; }
            try
            {
                int width = PoliSim.UI.UiScreen.Width, height = PoliSim.UI.UiScreen.Height;
                var corners = new Vector3[4];
                var byControl = new Dictionary<Selectable, List<TableRow>>();
                foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (canvas == null || !canvas.isRootCanvas || !canvas.isActiveAndEnabled) { continue; }
                    Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                    foreach (Text t in canvas.GetComponentsInChildren<Text>(false))
                    {
                        if (t == null || !t.isActiveAndEnabled || string.IsNullOrEmpty(t.text)) { continue; }
                        Canvas own = t.canvas;
                        CanvasRenderer renderer = t.canvasRenderer;
                        if (own == null || !own.isActiveAndEnabled || renderer == null || renderer.cull || t.color.a * renderer.GetInheritedAlpha() <= 0.01f) { continue; }
                        t.rectTransform.GetWorldCorners(corners);
                        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 p = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
                        }
                        var screen = new Rect(minX, height - maxY, maxX - minX, maxY - minY);
                        Rect local = t.rectTransform.rect;
                        float scale = local.height > 0.01f ? screen.height / local.height : own.scaleFactor;
                        bool wrap = t.horizontalOverflow == HorizontalWrapMode.Wrap;
                        float neededWidth = wrap ? 0f : t.preferredWidth * scale;
                        float neededHeight = t.preferredHeight * scale;
                        bool overlaps = screen.xMax > 0f && screen.xMin < width && screen.yMax > 0f && screen.yMin < height;
                        bool inside = screen.xMin >= -0.5f && screen.xMax <= width + 0.5f && screen.yMin >= -0.5f && screen.yMax <= height + 0.5f;
                        var flags = new List<string>();
                        if (!wrap && t.preferredWidth > local.width + 0.5f) { flags.Add("WIDE"); }
                        if (wrap && t.preferredHeight > local.height + 0.5f) { flags.Add("TALL"); }
                        if (!overlaps) { flags.Add("OUTSIDE"); } else if (!inside) { flags.Add("STRADDLES"); }
                        if (CanvasLines(t) > 1) { flags.Add("MULTILINE"); }
                        flags.Add("CANVAS");
                        string head = string.Format(CultureInfo.InvariantCulture,
                            "{0}\t{1}x{2}\t{3:F1}\t{4:F1}\t{5:F1}\t{6:F1}\t{7}\t{8}\t{9:F1}\t{10:F1}",
                            _capture, width, height, screen.x, screen.y, screen.width, screen.height,
                            Mathf.RoundToInt(t.fontSize * scale), wrap ? "wrap" : "line", neededWidth, neededHeight);
                        if (!RowKeys.Add(head + "\t" + string.Join(" ", flags) + "\t" + Escape(t.text))) { continue; }
                        var row = new TableRow { Head = head, Flags = flags, Text = t.text, Screen = screen, Visible = overlaps, Canvas = true };
                        Rows.Add(row);
                        _texts++;
                        _canvasTexts++;
                        // K321 (a), the Canvas's controls: a Text under a Selectable (the chrome's faced buttons, a toggle) is its label when it is
                        // the Selectable's ONE visible Text - a clickable card holding several is not a label, as with the IMGUI click targets
                        Selectable control = t.GetComponentInParent<Selectable>();
                        if (control != null)
                        {
                            if (!byControl.TryGetValue(control, out List<TableRow> under)) { byControl[control] = under = new List<TableRow>(); }
                            under.Add(row);
                        }
                    }
                    // K322 S6: THE CANVAS SLIPS' BOOK. A night's slips are `CanvasSlip` components (CanvasRows.Slip), which no IMGUI draw hands to
                    // NoteSlipBook, so a night's frame recorded no book and every word a sweep moved onto a night's slip read UNREACHABLE. Each live
                    // slip on the frame is its level-1 book entry, keyed canvas:<its anchor's name> (Canvas has no level 2: CanvasRows' summary).
                    foreach (PoliSim.UI.CanvasSlip slip in canvas.GetComponentsInChildren<PoliSim.UI.CanvasSlip>(false))
                    {
                        if (slip == null || !slip.isActiveAndEnabled || string.IsNullOrEmpty(slip.Head)) { continue; }
                        NoteCanvasSlip("canvas:" + slip.gameObject.name, slip.Head, slip.Lines);
                    }
                }
                foreach (List<TableRow> under in byControl.Values) { if (under.Count == 1) { under[0].Flags.Add("CONTROL"); } }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SHOT: the label table's Canvas pass failed on {_capture} ({e.GetType().Name}: {e.Message}) - its Canvas rows are missing, and the declutter judge reads the frame without them.");
            }
        }

        private static int _canvasTexts;

        /// <summary>The lines the generator lays <paramref name="t"/> on at its rect's width, every one counted (vertical overflow opened).</summary>
        private static int CanvasLines(Text t)
        {
            TextGenerationSettings settings = t.GetGenerationSettings(t.rectTransform.rect.size);
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            var generator = new TextGenerator();
            generator.Populate(t.text, settings);
            return generator.lineCount;
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
            foreach (TableRow row in Rows) { sb.Append(row.Head).Append('\t').Append(string.Join(" ", row.Flags)).Append('\t').Append(Escape(row.Text)).Append('\n'); }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return path;
        }

        /// <summary>K321 (g): writes the slip books the captured frames drew beside the label table, as `&lt;label&gt;_slipbooks.tsv` (capture, level,
        /// key, role, text) - always, a header alone when no page drew one, so the declutter judge can tell "no book drawn" from "none recorded".</summary>
        public static string WriteSlipBooks(string directory, string label)
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, label + "_slipbooks.tsv");
            var sb = new StringBuilder("capture\tlevel\tkey\trole\ttext\n");
            foreach (string row in SlipRows) { sb.Append(row).Append('\n'); }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return path;
        }

        private static void NoteSlip(int level, string key, PoliSim.UI.SlipContent slip)
        {
            void Add(string role, string text)
            {
                string row = string.Join("\t", _capture, level.ToString(CultureInfo.InvariantCulture), Escape(key), role, Escape(text));
                if (SlipRowKeys.Add(row)) { SlipRows.Add(row); }
            }
            Add("head", PoliSim.UI.SlipContent.Plain(slip.Head));
            foreach (string line in slip.Lines) { Add("line", PoliSim.UI.SlipContent.Plain(line)); }
        }

        /// <summary>K322 S6: a Canvas slip (plain head and lines, one level) as a level-1 book entry of the capture, as NoteSlip writes an IMGUI one.</summary>
        private static void NoteCanvasSlip(string key, string head, List<string> lines)
        {
            void Add(string role, string text)
            {
                string row = string.Join("\t", _capture, "1", Escape(key), role, Escape(text ?? string.Empty));
                if (SlipRowKeys.Add(row)) { SlipRows.Add(row); }
            }
            Add("head", head);
            if (lines == null) { return; }
            foreach (string line in lines) { Add("line", line); }
            // §971: a night's slip lines are SlipWrap's wrap of whole sentences, and the judge reads a book line by line - a phrase the wrap split
            // ("The Sejm / first sits on", the Polish film's e7e5) read UNREACHABLE. The slip's lines joined are recorded too, so the sentences read whole.
            if (lines.Count > 1) { Add("line", string.Join(" ", lines)); }
        }
#endif

        /// <summary>
        /// K321 (g): the page's slip book as the captured frame draws it (`GameController.DrawSlips` hands it here on the repaint) - every anchor's
        /// slip, level 1, and the slip of every term a level-1 slip marks, level 2 (nothing deeper: UI v3.3's two levels). The moved words are
        /// judged against these. Inert outside a capture, and outside the Editor.
        /// </summary>
        public static void NoteSlipBook(PoliSim.UI.PeopleSlips.Book book)
        {
#if UNITY_EDITOR
            if (!_recording || book == null || Event.current == null || Event.current.type != EventType.Repaint) { return; }
            try
            {
                var marked = new List<string>();
                foreach (KeyValuePair<string, PoliSim.UI.SlipContent> anchor in book.Anchors)
                {
                    NoteSlip(1, anchor.Key, anchor.Value);
                    foreach (string term in PoliSim.UI.SlipContent.TermsIn(anchor.Value.Head)) { if (!marked.Contains(term)) { marked.Add(term); } }
                    foreach (string line in anchor.Value.Lines) { foreach (string term in PoliSim.UI.SlipContent.TermsIn(line)) { if (!marked.Contains(term)) { marked.Add(term); } } }
                }
                foreach (string term in marked) { if (book.Terms.TryGetValue(term, out PoliSim.UI.SlipContent slip)) { NoteSlip(2, term, slip); } }
            }
            catch (Exception)
            {
                // A record must never break the draw it watches.
            }
#endif
        }
    }
}
