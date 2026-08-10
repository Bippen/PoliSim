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
            GUIStyle thumbStyle)
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

            var nameRect = new Rect(row.x, row.y, nameWidth, row.height);
            var trackRect = new Rect(nameRect.xMax + gap, row.y + (row.height - RefTrackHeight * scale) * 0.5f, trackWidth, RefTrackHeight * scale);
            var figureRect = new Rect(trackRect.xMax + gap, row.y, figureWidth, row.height);
            var trailingRect = new Rect(figureRect.xMax + gap, row.y, trailingWidth, row.height);

            Color rowInk = interactive ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted;

            DrawCell(nameRect, name, nameStyle, rowInk, TextAnchor.MiddleLeft);

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
        /// ⚠ **`MeasuredLabel` SHRINKS THE STYLE IT IS HANDED**, permanently, as its way of fitting text.
        ///
        /// Handing it the caller's `_labelStyle` therefore shrinks the whole UI's shared label style a
        /// little more on every frame, compounding until the layout collapses. The first live capture
        /// after this row was wired showed exactly that: rows losing their track and figures entirely,
        /// and text drifting up into the line above.
        ///
        /// **This is the second time this trap has been hit in this project** - `PolicyScreenStatsRenderer`
        /// solved it with a private cached `ChipTextStyle` for the same reason. A per-row cached copy,
        /// rebuilt only when the source style's size actually changes, is the same fix: MeasuredLabel
        /// gets something disposable to shrink, and the caller's style is never touched.
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
    }
}
