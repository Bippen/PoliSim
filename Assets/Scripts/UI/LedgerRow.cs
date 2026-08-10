using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// One row of the v2.0 Budget ledger: name, an in-track slider carrying the standing/draft pair,
    /// the inline figures, and a trailing column. **The Budget screen's atom** - see
    /// POLISIM_V2_SCREEN_SPEC.md §A.9.
    ///
    /// Its own file rather than another method on <see cref="PoliSimWidgets"/> for the same reason
    /// <see cref="RankedBarLedgerRenderer"/> has one: this is a composite with its own geometry rules,
    /// not a one-shot drawing helper.
    ///
    /// <para><b>The in-track standing/draft pair is the strongest idea in the v2.0 pack, and it is
    /// behaviour 1's primary carrier.</b> The enacted value is a hard tick on the track; the drafted
    /// value is the knob; the span between them is hatched in draft amber. A cut and a rise are equally
    /// legible because the hatch is drawn in whichever direction the two differ - which is the part a
    /// coloured fill would lose.</para>
    /// </summary>
    public static class LedgerRow
    {
        /// <summary>Reference column measures from the spec, quoted at 1080p with 13px row type. Scaled by the live font size at draw time - see <see cref="Scale"/>. **They are measurements at one resolution, never constants** (CLAUDE.md, instance #9).</summary>
        private const float RefNameWidth = 250f;
        private const float RefFigureWidth = 150f;
        private const float RefTrailingWidth = 88f;
        private const float RefFontSize = 13f;
        private const float RefTrackHeight = 15f;
        private const float RefColumnGap = 10f;

        /// <summary>Knob geometry, also from the spec (14x23 at 1080p), scaled the same way.</summary>
        private const float RefKnobWidth = 14f;
        private const float RefKnobHeight = 23f;
        private const float RefTickWidth = 2f;

        private static float Scale(GUIStyle style) => Mathf.Max(1f, style.fontSize) / RefFontSize;

        /// <summary>
        /// The row's height, DERIVED from the font metric rather than fixed.
        ///
        /// ⚠ **The spec quotes 36px, and 36px is the value at 1080p, not the row height.** A generated
        /// name may wrap to two lines; at 1080p that is 2 x 13 x 1.1 = 28.6px inside 36px, but every
        /// style in this UI rescales with Screen.height, so at 1440p the same name sets at ~17.3px and
        /// two lines become 38.1px - taller than the row meant to contain it. That is the ninth instance
        /// of this project's fixed-height-versus-scaling-type defect and the first caught in a
        /// specification instead of a capture.
        /// </summary>
        public static float Height(GUIStyle nameStyle)
        {
            float line = Mathf.Max(nameStyle.lineHeight, nameStyle.fontSize + 4f);
            float scale = Scale(nameStyle);
            return Mathf.Max(line * 2f, RefTrackHeight * scale + line) + 6f * scale;
        }

        /// <summary>
        /// Draws one ledger row and returns the (possibly dragged) draft value.
        ///
        /// <para><b>Control-ID stability.</b> This emits EXACTLY ONE control - the slider - on every
        /// path, every frame, whether or not the row is interactive. A non-applicable row is drawn
        /// disabled via <c>GUI.enabled</c>, never by omitting the control. The Budget screen's own doc
        /// comment on <c>DrawTaxPolicyContent</c> explains why that is a hang trigger rather than a
        /// style preference: GUILayout allocates control IDs positionally, and a background bill can
        /// resolve mid-drag.</para>
        ///
        /// <para><b>Nothing here clips.</b> Every text cell routes through
        /// <see cref="PoliSimWidgets.MeasuredLabel"/>, which shrinks to fit. Per §A.9a the numeric
        /// variant of the resort ladder is widen-then-shrink: a figure must not wrap (a money value
        /// broken across two lines is briefly readable as a different number) and has no abbreviation
        /// table, because MoneyUnit tiering already did that job.</para>
        /// </summary>
        /// <param name="standing">The enacted value - drawn as the hard tick.</param>
        /// <param name="draft">The drafted value - drawn as the knob.</param>
        public static float Draw(
            Rect row,
            string name,
            float standing,
            float draft,
            float min,
            float max,
            string standingText,
            string draftText,
            string trailingText,
            bool interactive,
            GUIStyle nameStyle,
            GUIStyle figureStyle,
            GUIStyle sliderStyle,
            GUIStyle thumbStyle,
            float barFraction = -1f)
        {
            float scale = Scale(nameStyle);
            Columns(row, nameStyle, TrailingNeed(trailingText, figureStyle),
                out Rect nameRect, out Rect trackRect, out Rect figureRect, out Rect trailingRect);

            Color rowInk = interactive ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted;

            // The MAGNITUDE bar, scaled by the caller against its GROUP's largest row (Design's answer to
            // V2). It sits along the bottom of the name cell so it never fights the text above it.
            //
            // **Why the group and not the whole screen.** Scaled globally, every discretionary line would
            // sit at a few percent of Social Security and the whole group would render as one flat
            // smear - the same failure the SHARE column has, for the same reason. Scaled to the group,
            // the bar discriminates exactly where the share column cannot. The header prints which scale
            // is in force, because a bar whose scale is unstated is a bar that can be misread across
            // sections, and two adjacent groups here really do differ by three orders of magnitude.
            if (barFraction >= 0f && Event.current.type == EventType.Repaint)
            {
                float barHeight = Mathf.Max(1f, 2f * scale);
                var barRect = new Rect(
                    nameRect.x,
                    nameRect.yMax - barHeight,
                    nameRect.width * Mathf.Clamp01(barFraction),
                    barHeight);
                Color barPrevious = GUI.color;
                GUI.color = interactive ? PoliSimTheme.TextSecondary : PoliSimTheme.TextMuted;
                GUI.DrawTexture(barRect, Texture2D.whiteTexture);
                GUI.color = barPrevious;
            }

            DrawNameCell(nameRect, name, nameStyle, rowInk);

            if (Event.current.type == EventType.Repaint)
            {
                DrawTrackFurniture(trackRect, standing, draft, min, max, scale, interactive);
            }

            // ALWAYS emitted, enabled or not - see the control-ID note above.
            bool ambient = GUI.enabled;
            GUI.enabled = ambient && interactive;
            float result = GUI.HorizontalSlider(trackRect, draft, min, max, sliderStyle, thumbStyle);
            GUI.enabled = ambient;

            DrawFigurePair(figureRect, standingText, draftText, figureStyle, rowInk);


            DrawCell(trailingRect, trailingText, figureStyle, rowInk, TextAnchor.MiddleRight);

            return interactive ? result : draft;
        }

        /// <summary>
        /// The four column rects, shared by <see cref="Draw"/> and <see cref="DrawReadOnly"/> so a
        /// read-only sub-screen lines up with the ones carrying sliders.
        /// </summary>
        /// <summary>What the trailing column's content actually needs, so <see cref="Columns"/> can size it rather than assume it. Zero for an empty cell, which leaves the proportional width untouched.</summary>
        private static float TrailingNeed(string trailingText, GUIStyle figureStyle)
        {
            return string.IsNullOrEmpty(trailingText) ? 0f : figureStyle.CalcSize(new GUIContent(trailingText)).x;
        }

        private static void Columns(Rect row, GUIStyle nameStyle, float trailingNeed,
            out Rect nameRect, out Rect trackRect, out Rect figureRect, out Rect trailingRect)
        {
            float scale = Scale(nameStyle);
            float gap = RefColumnGap * scale;

            // ⚠ THE COLUMNS ARE PROPORTIONAL TO THE ROW, NOT TO THE FONT. The first cut scaled the
            // spec's 250/150/88 by font size alone, and the first live capture killed it immediately:
            // the spec quotes those measures against board 1b's ~1100px ledger panel, this screen's
            // centre column is ~745px, and at this screen's font size the three fixed columns summed to
            // more than the row was wide. The track collapsed to its floor and both figure columns were
            // pushed past the panel edge, where they simply did not render.
            //
            // Proportions are taken from the board (fixed columns are ~44% of its ledger width), with
            // font-derived FLOORS so they cannot shrink below legibility on a narrow window. This is the
            // same lesson as the row height one line below - a number from a mockup is a measurement at
            // one size, and here it was a measurement against one panel width too.
            float nameWidth = Mathf.Max(row.width * 0.26f, RefKnobWidth * scale * 4f);
            float figureWidth = Mathf.Max(row.width * 0.19f, RefKnobWidth * scale * 3f);
            float trailingWidth = Mathf.Max(row.width * 0.11f, RefKnobWidth * scale * 2f);
            float minTrack = RefKnobWidth * scale * 4f;

            // ⚠ THE TRAILING COLUMN IS SIZED FROM ITS CONTENT, because it holds two different KINDS of
            // thing. Everywhere the board specified it, it holds a figure - estimated revenue, share of
            // GDP - and 11% of the row is ample. The dial rows put a SCALE LEGEND there instead ("0
            // minimal - 100 pro-natalist"), which is prose, and prose does not fit a column sized for
            // "$1.05T".
            //
            // Measured 2026-08-10: the legends need 160-243px against the 83.8px this proportion gives
            // them, up to 2.9x over. That is not a number to tune - no proportion satisfies both content
            // kinds - and it cannot be shrunk out of either, since 243->84 needs roughly 5px type
            // against §A.9a's 11px floor. So the column asks what it is holding.
            //
            // Capped at a third of the row so a long legend can never starve the track, with the squeeze
            // below as the second backstop and minTrack as the third.
            if (trailingNeed > trailingWidth)
            {
                trailingWidth = Mathf.Min(trailingNeed, row.width * 0.34f);
            }

            // If the floors still do not fit - a very narrow window - the fixed columns give ground
            // together rather than one of them collapsing, so the row degrades evenly instead of losing
            // a whole column. MeasuredLabel shrinks the text inside whatever is left; nothing clips.
            float fixedTotal = nameWidth + figureWidth + trailingWidth;
            float available = row.width - gap * 3f - minTrack;
            if (fixedTotal > available && fixedTotal > 0f)
            {
                float squeeze = Mathf.Max(0.35f, available / fixedTotal);
                nameWidth *= squeeze;
                figureWidth *= squeeze;
                trailingWidth *= squeeze;
            }

            float trackWidth = Mathf.Max(minTrack, row.width - nameWidth - figureWidth - trailingWidth - gap * 3f);

            nameRect = new Rect(row.x, row.y, nameWidth, row.height);
            trackRect = new Rect(nameRect.xMax + gap, row.y + (row.height - RefTrackHeight * scale) * 0.5f, trackWidth, RefTrackHeight * scale);
            figureRect = new Rect(trackRect.xMax + gap, row.y, figureWidth, row.height);
            trailingRect = new Rect(figureRect.xMax + gap, row.y, trailingWidth, row.height);
        }

        /// <summary>The standing tick and the draft hatch band - drawn UNDER the slider so the knob reads as sitting on the track rather than beside it.</summary>
        private static void DrawTrackFurniture(Rect track, float standing, float draft, float min, float max, float scale, bool interactive)
        {
            float span = Mathf.Max(0.0001f, max - min);
            float standingX = track.x + track.width * Mathf.Clamp01((standing - min) / span);
            float draftX = track.x + track.width * Mathf.Clamp01((draft - min) / span);

            // The hatch runs from whichever of the two is left to whichever is right, so a CUT hatches
            // exactly as visibly as a RISE. A one-directional fill would silently drop half the cases,
            // and "the player can see what they changed" is the whole of behaviour 1.
            float hatchLeft = Mathf.Min(standingX, draftX);
            float hatchWidth = Mathf.Abs(draftX - standingX);
            if (interactive && hatchWidth > 1f)
            {
                var hatchRect = new Rect(hatchLeft, track.y, hatchWidth, track.height);
                Texture2D hatch = IconLibrary.GetChrome("ui_hatch_draft");
                Color previous = GUI.color;
                GUI.color = PoliSimTheme.Draft;
                if (hatch != null)
                {
                    GUI.DrawTextureWithTexCoords(hatchRect, hatch, new Rect(0f, 0f, hatchWidth / hatch.width, track.height / hatch.height));
                }
                else
                {
                    // No sprite is not the same as no cue. Behaviour 1 may change form; it may not
                    // become nothing, so the fallback is a flat amber wash at the hatch's own weight.
                    GUI.color = new Color(PoliSimTheme.Draft.r, PoliSimTheme.Draft.g, PoliSimTheme.Draft.b, 0.5f);
                    GUI.DrawTexture(hatchRect, Texture2D.whiteTexture);
                }
                GUI.color = previous;
            }

            // The standing tick: the enacted value, and the thing a draft is read AGAINST. Drawn last of
            // the furniture and taller than the track so it stays visible under the hatch.
            var tickRect = new Rect(
                standingX - RefTickWidth * scale * 0.5f,
                track.y - 3f * scale,
                RefTickWidth * scale,
                track.height + 6f * scale);
            Color tickPrevious = GUI.color;
            GUI.color = interactive ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted;
            GUI.DrawTexture(tickRect, Texture2D.whiteTexture);
            GUI.color = tickPrevious;

            // D1's agreed draft carrier: a pencil at the DRAFT end, so the two ends of the change are
            // marked by what they ARE - the standing value by a hard rule, the proposed one by a
            // drawing implement. The hatch says how far; these two say which end is which.
            //
            // ⚠ PURELY ADDITIVE, and it must stay that way. Behaviour 1 is already carried above by the
            // hatch and by the amber figures in DrawFigurePair, both of which have their own fallbacks.
            // This is a fidelity upgrade on a cue that already holds, NOT the cue itself - so when the
            // sprite is missing this draws nothing and B1 is unharmed. Making the pencil load-bearing
            // (by moving any part of the hatch or the tick under this null check) would convert an
            // optional refinement into a single point of failure for the behaviour.
            if (interactive && hatchWidth > 1f)
            {
                Texture2D pencil = IconLibrary.GetChrome("icon_pencil_draft");
                if (pencil != null)
                {
                    float size = track.height + 6f * scale;
                    var pencilRect = new Rect(draftX - size * 0.5f, track.y - 3f * scale, size, size);
                    Color pencilPrevious = GUI.color;
                    GUI.color = PoliSimTheme.Draft;
                    GUI.DrawTexture(pencilRect, pencil, ScaleMode.ScaleToFit);
                    GUI.color = pencilPrevious;
                }
            }
        }

        /// <summary>`standing → draft`, the draft half in amber. Behaviour 1 in text, beside the same pair drawn on the track.</summary>
        private static void DrawFigurePair(Rect rect, string standingText, string draftText, GUIStyle style, Color rowInk)
        {
            if (string.IsNullOrEmpty(draftText))
            {
                DrawCell(rect, standingText, style, rowInk, TextAnchor.MiddleRight);
                return;
            }

            // Measured as one string so the pair shrinks together - shrinking the halves independently
            // would set the same row's figures at two different sizes, which reads as an error rather
            // than as a fit (the reasoning behind §A.9a's resort ladder).
            float half = rect.width * 0.5f;
            DrawCell(new Rect(rect.x, rect.y, half, rect.height), standingText, style, rowInk, TextAnchor.MiddleRight);
            DrawCell(new Rect(rect.x + half, rect.y, half, rect.height), draftText, style, PoliSimTheme.Draft, TextAnchor.MiddleRight);
        }

        /// <summary>
        /// A private copy so this row never writes alignment or colour onto the caller's style.
        ///
        /// **It no longer needs to guard against MeasuredLabel's shrink** - that was the original reason
        /// (the first capture after this row was wired showed the shared `_labelStyle` shrinking a little
        /// more each frame until layout collapsed across every screen), and `MeasuredLabel` now clones
        /// internally so the shared-style path is impossible rather than merely discouraged. What remains
        /// is the narrower need: each cell sets its own alignment and ink, and doing that on the caller's
        /// style would be the same class of mutation one level down.
        /// </summary>
        private static GUIStyle _cellStyle;
        private static int _cellStyleSourceSize = -1;

        private static GUIStyle CellStyle(GUIStyle source)
        {
            if (_cellStyle == null || _cellStyleSourceSize != source.fontSize)
            {
                _cellStyle = new GUIStyle(source) { wordWrap = true, clipping = TextClipping.Overflow };
                _cellStyleSourceSize = source.fontSize;
            }

            // Reset the size every call: MeasuredLabel may have shrunk the cached copy for a previous
            // cell, and a row whose columns each print at whatever size the last one needed is the
            // "reads as an error rather than as a fit" failure §A.9a exists to prevent.
            _cellStyle.fontSize = source.fontSize;
            return _cellStyle;
        }

        /// <summary>
        /// A row with **no control at all**: name, a gauge bar where the track would be, figure,
        /// trailing column.
        ///
        /// **Not `Draw` with `interactive: false`.** That renders a disabled slider, which is the right
        /// answer for a value the player could change but currently cannot (behaviour 5: disabled is
        /// rendered, never omitted). Infrastructure condition is a different case - there is nothing to
        /// drag under any circumstances, because it is an OUTPUT driven by the Infrastructure spending
        /// category rather than a dial. Emitting a slider for it would add a control where the screen has
        /// never had one, and would tell the player a lie about what they can do.
        ///
        /// The column geometry is shared, so a read-only sub-screen still lines up with the four that
        /// carry sliders - which is the whole reason this lives here rather than being drawn ad hoc.
        /// </summary>
        /// <param name="fill">0..1 gauge proportion.</param>
        public static void DrawReadOnly(
            Rect row, string name, float fill, string figureText, string trailingText,
            Color barInk, GUIStyle nameStyle, GUIStyle figureStyle)
        {
            Columns(row, nameStyle, TrailingNeed(trailingText, figureStyle),
                out Rect nameRect, out Rect trackRect, out Rect figureRect, out Rect trailingRect);

            DrawNameCell(nameRect, name, nameStyle, PoliSimTheme.TextPrimary);

            if (Event.current.type == EventType.Repaint)
            {
                Color previous = GUI.color;
                GUI.color = PoliSimTheme.BarTrack;
                GUI.DrawTexture(trackRect, Texture2D.whiteTexture);
                GUI.color = barInk;
                GUI.DrawTexture(new Rect(trackRect.x, trackRect.y, trackRect.width * Mathf.Clamp01(fill), trackRect.height), Texture2D.whiteTexture);
                GUI.color = previous;
            }

            DrawCell(figureRect, figureText, figureStyle, PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
            DrawCell(trailingRect, trailingText, figureStyle, PoliSimTheme.TextSecondary, TextAnchor.MiddleRight);
        }

        /// <summary>Public so a caller composing extra columns beside a row - the tax screen's verdict cell, say - prints them in the same measured, never-clipping way rather than reaching for a raw GUI.Label.</summary>
        public static void Cell(Rect rect, string text, GUIStyle source, Color ink, TextAnchor alignment)
        {
            DrawCell(rect, text, source, ink, alignment);
        }

        private static void DrawCell(Rect rect, string text, GUIStyle source, Color ink, TextAnchor alignment)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            GUIStyle style = CellStyle(source);
            style.alignment = alignment;
            style.normal.textColor = ink;
            PoliSimWidgets.MeasuredLabel(rect, text, style);
        }

        /// <summary>
        /// The NAME cell, which wraps to two lines before it shrinks - §A.9a's resort ladder, step 2
        /// ahead of step 4.
        ///
        /// ⚠ **Found by the first Policy/Laws capture, and it is D7's own objection landing on this
        /// build.** Every cell went straight through `MeasuredLabel`, which only ever shrinks. On the
        /// Budget screen the names were short enough that nothing triggered it; on Labor Market
        /// "Overtime / Working-Hour Regulation" is long enough to shrink while "Minimum Wage" above it is
        /// not, so the two rows printed at visibly different sizes. Design's exact words when they
        /// amended D7: *"a ledger column where every row prints at a different size reads as an error"*.
        ///
        /// So: try one line at full size, then two wrapped lines at full size, and only then fall back to
        /// shrinking. `Height` already reserves two lines, so the wrap costs no layout.
        /// </summary>
        private static void DrawNameCell(Rect rect, string text, GUIStyle source, Color ink)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            GUIStyle style = CellStyle(source);
            style.alignment = TextAnchor.MiddleLeft;
            style.normal.textColor = ink;

            style.wordWrap = false;
            var content = new GUIContent(text);
            if (style.CalcSize(content).x <= rect.width)
            {
                GUI.Label(rect, content, style);
                return;
            }

            style.wordWrap = true;
            if (style.CalcHeight(content, rect.width) <= rect.height)
            {
                GUI.Label(rect, content, style);
                return;
            }

            // Two lines still will not hold it - shrink, which is the floor of the ladder rather than
            // its first move. MeasuredLabel re-seeds wordWrap itself, so the cached style is safe to
            // hand over in whatever state this left it.
            PoliSimWidgets.MeasuredLabel(rect, text, style);
        }
    }
}
