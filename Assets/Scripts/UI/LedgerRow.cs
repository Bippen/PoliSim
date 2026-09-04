using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// One row of the v2.0 Budget ledger: name, an in-track slider carrying the standing/draft pair,
    /// the inline figures, and a trailing column. **The Budget screen's atom** - see
    /// COMPLETED.md §187 §A.9.
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
        // P2-1.3 (2026-09-02): thinner - a bar half its former height; the knob is sized by the thumb style.
        // P5-1 (2026-09-03, board 6a): 14 px @1x - the delivered ui_slider_track drawn at the height the board names.
        private const float RefTrackHeight = 14f;
        /// <summary>P5-1 (board 6a): the caption band UNDER the track where a dial's end-names sit ("0 nationalized" left, "100 deregulated" right) - furniture, never on the track.</summary>
        private const float RefEndCaption = 12f;   // 12 - the caption face at 0.6 of a 17 px row needs 14 px; 10 was one short on film
        /// <summary>P5-1 (board 6a): the knob, 15x23 @1x, uniform scale; the hit rect is the sprite rect.</summary>
        private const float RefKnobSpriteWidth = 15f;
        private const float RefKnobSpriteHeight = 23f;
        /// <summary>P5-1 (board 6a): the pencil's slot in the rate cell - reserved at rest, filled by the pencil while a draft differs.</summary>
        private const float RefPencilSlot = 12f;
        private const float RefTickMinPitch = 12f;
        private const float RefColumnGap = 10f;

        /// <summary>Knob geometry, also from the spec (14x23 at 1080p), scaled the same way.</summary>
        private const float RefKnobWidth = 14f;
        private const float RefKnobHeight = 23f;

        /// <summary>P2-1.3: the draft's step, in the row's own units (percentage points for a rate). A tenth: fine
        /// enough that any whole point is a resting value, coarse enough that the figure column's two decimals
        /// never show a third.</summary>
        public const float SnapStep = 0.1f;

        /// <summary>P2-1.3: a whole unit - the bound the film holds every Budget row to. A row whose track covers more
        /// than one unit per pixel cannot rest on every whole value, however it snaps.</summary>
        public const float WholeUnit = 1f;

        /// <summary>The step a track can carry: as fine as its pixels allow and never finer than what a whole unit needs
        /// - a tenth where a unit has ten pixels, a half where it has two, else the whole unit. Adaptive so a 0-100 range
        /// on a 1280 film and a 0-5 range on a 2560 film both rest on every whole value.</summary>
        public static float StepFor(float unitsPerPixel)
        {
            if (unitsPerPixel <= SnapStep) { return SnapStep; }
            if (unitsPerPixel <= 0.5f) { return 0.5f; }
            return WholeUnit;
        }

        /// <summary>P2-1.3: the range each pixel of a drawn track covers, per row name, recorded only while the capture
        /// harness is armed. A whole point is reachable without overshoot when this does not exceed <see cref="SnapStep"/>;
        /// the driver reads the worst at its exit.</summary>
        public static readonly System.Collections.Generic.Dictionary<string, float> ReachByRow = new System.Collections.Generic.Dictionary<string, float>();

        /// <summary>P4-1 (2026-09-03): the four column rects of every interactive row on the last Repaint, keyed the way the reach is
        /// (screen / name), recorded only while a capture run is armed - so the harness can assert that a row's control rects at rest
        /// equal its rects mid-drag (stable control layout: the control never moves under the pointer moving it).</summary>
        public static readonly System.Collections.Generic.Dictionary<string, (Rect Name, Rect Track, Rect Figure, Rect Trailing)> GeometryByRow = new System.Collections.Generic.Dictionary<string, (Rect Name, Rect Track, Rect Figure, Rect Trailing)>();

        private static void RecordReach(string name, float unitsPerPixel)
        {
            if (!ReachByRow.TryGetValue(name, out float worst) || unitsPerPixel > worst) { ReachByRow[name] = unitsPerPixel; }
        }
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
            // D4 (2026-08-28, the v3.1 density token table): the two-line lane's track term and its
            // padding "12s + line, +6s → 8s + line, +4s" (Annex C quoted the term as 12; the drawn
            // track is RefTrackHeight = 15, which stays - it is a bar height, not a lane term, and it
            // fits inside two lines at every size this UI reaches: 15·s < line for fonts under ~27 px
            // and the serif's own line box carries it above that).
            // P5-1 (board 6a): plus the caption band under the track, on every row - one geometry for the family.
            return Mathf.Max(line * 2f, RefLaneTrackTerm * scale + line) + RefLanePadding * scale + RefEndCaption * scale;
        }

        /// <summary>D4's lane term and padding (see <see cref="Height"/>): 8 and 4 at the 13 px reference, scaled.</summary>
        private const float RefLaneTrackTerm = 8f;
        private const float RefLanePadding = 4f;

        /// <summary>
        /// The ONE-LINE row's height (R-C1, the continuation kickoff of 2026-08-28): the name's line plus
        /// the same vertical padding <see cref="Height"/> wraps its two lines in (6 px at the 13 px
        /// reference, scaled) - the ledger convention with the second line taken out.
        ///
        /// Derived, not picked. Board 1i (`board_1i_law_browser.png`, 1920x1080) draws its statute rows
        /// on a 32 px pitch with a 14 px bold name - 2.29 name-fonts per row, ~26 rows in its 835 px
        /// scroller (the rulings doc's "~27 rows per screen"). On this UI's px basis - the name at the
        /// live font size, the Budget rows' flat 10 px gap after each row - the pitch this gives is
        /// line + 6·scale + 10: ~37 px at a 16 px font (1280x720), ~43 at 20 (1600x900), ~55 at 28
        /// (2560x1440), 2.0 to 2.3 name-fonts per row - the board's class, measured on film in the
        /// continuation's Phase 1 record. A caller stacking one-line rows must give the name cell the
        /// shrink path (<see cref="Cell"/>), never the wrap-first ladder: there is no second line.
        /// </summary>
        public static float OneLineHeight(GUIStyle nameStyle)
        {
            // D4 (2026-08-28): the one-line pitch line + 6s → line + 4s (≈ 24 → ≈ 22 at 16 px). At the
            // time of the change this accessor's only callers were the law browser's four sites, whose
            // pitch D4 rules STANDS (R-C1) - they moved to LawBrowserRowHeight below, so the token
            // moved and the browser held. A new one-line ledger reads this.
            float line = Mathf.Max(nameStyle.lineHeight, nameStyle.fontSize + 4f);
            return line + RefLanePadding * Scale(nameStyle);
        }

        /// <summary>
        /// The law browser's row pitch, frozen at R-C1's derivation (line + 6·scale; the browser adds its
        /// own 10 px gap) - D4's table rules it STANDS while the ledger's one-line token moved to +4s,
        /// so the two are separate accessors on purpose: the browser's ~27 rows per screen were measured
        /// on film against board 1i and are not a density token.
        /// </summary>
        public static float LawBrowserRowHeight(GUIStyle nameStyle)
        {
            float line = Mathf.Max(nameStyle.lineHeight, nameStyle.fontSize + 4f);
            return line + 6f * Scale(nameStyle);
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
            float barFraction = -1f,
            float tickStep = 0f)
        {
            float scale = Scale(nameStyle);
            Columns(row, nameStyle, NameNeed(name, nameStyle), TrailingNeed(trailingText, figureStyle), FigureNeed(standingText, draftText, figureStyle),
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
                DrawTrackFurniture(trackRect, standing, draft, min, max, scale, interactive, tickStep);
                DrawEndNames(trackRect, trailingText, figureStyle, scale, interactive);
                // P4-B2: the last row's track and scale, for a caller that draws a range caption into the caption band
                // beneath it (the band DrawEndNames uses) after this returns - read on the same Repaint, never stored.
                LastTrackRect = trackRect;
                LastScale = scale;
                LastHadEndNames = IsEndNames(trailingText);
            }

            // ALWAYS emitted, enabled or not - see the control-ID note above.
            bool ambient = GUI.enabled;
            GUI.enabled = ambient && interactive;
            float result = GUI.HorizontalSlider(trackRect, draft, min, max, sliderStyle, KnobStyle(thumbStyle, scale, interactive));
            GUI.enabled = ambient;
            // P2-1.3 (2026-09-02): FINER STEP - the draft snaps to SnapStep, so a whole point is a value the
            // thumb can rest on rather than one it passes through; and the film records the range each pixel
            // covers, which is the reach a whole point needs (the driver fails a run where it exceeds the snap).
            float unitsPerPixel = trackRect.width > 0f ? (max - min) / trackRect.width : float.PositiveInfinity;
            float step = StepFor(unitsPerPixel);
            if (interactive && !Mathf.Approximately(result, draft))
            {
                result = Mathf.Clamp(Mathf.Round(result / step) * step, min, max);
                if (!Mathf.Approximately(result, draft)) { AudioDirector.FireStep(); }   // P4-2: the draft snapped to its next step
            }

            // Repaint only: on the Layout event GetRect hands the caller a dummy rect, the columns squeeze to their
            // minimum and the record would keep that ghost as the row's worst (it did, on the first film).
            if (interactive && PoliSim.Testing.CaptureIdentity.Armed && trackRect.width > 0f && Event.current.type == EventType.Repaint)
            {
                RecordReach(UiGuardContext.CurrentScreen + " / " + name, unitsPerPixel);
                GeometryByRow[UiGuardContext.CurrentScreen + " / " + name] = (nameRect, trackRect, figureRect, trailingRect);
            }

            DrawFigurePair(figureRect, standingText, draftText, figureStyle, rowInk);


            if (!IsEndNames(trailingText) && Event.current.type == EventType.Repaint) { DrawTrailingUnderFigure(figureRect, trackRect, trailingText, figureStyle, scale, interactive); }   // P5-1: under the rate cell

            return interactive ? result : draft;
        }

        /// <summary>
        /// The four column rects, shared by <see cref="Draw"/> and <see cref="DrawReadOnly"/> so a
        /// read-only sub-screen lines up with the ones carrying sliders.
        /// </summary>
        /// <summary>What the trailing column's content actually needs, so <see cref="Columns"/> can size it rather than assume it. Zero for an empty cell, which leaves the proportional width untouched.</summary>
        private static float TrailingNeed(string trailingText, GUIStyle figureStyle)
        {
            return string.IsNullOrEmpty(trailingText) || IsEndNames(trailingText) ? 0f : figureStyle.CalcSize(new GUIContent(trailingText)).x;
        }

        /// <summary>
        /// What the figure column's content needs, so <see cref="Columns"/> can size it rather than
        /// assume it (2026-08-28, UI v3.0 Phase A - label-clipping instance #15). Zero for an empty cell,
        /// which leaves the proportional width untouched.
        ///
        /// <para>P4-1 (2026-09-03, STABLE CONTROL LAYOUT): the column holds ONE readout - the standing
        /// figure at rest, the draft figure in the draft cue while a draft differs - so its need is the
        /// wider of the two single figures, never twice it. The pair it used to hold doubled the need
        /// the moment a draft appeared, the column widened past its proportion and the track gave
        /// ground mid-drag: the control moved under the pointer that was moving it.</para>
        /// </summary>
        private static float FigureNeed(string standingText, string draftText, GUIStyle figureStyle)
        {
            float standing = string.IsNullOrEmpty(standingText) ? 0f : figureStyle.CalcSize(new GUIContent(standingText)).x;
            float draft = string.IsNullOrEmpty(draftText) ? 0f : figureStyle.CalcSize(new GUIContent(draftText)).x;
            // P5-1 (board 6a): the pencil's slot is part of the cell at rest, so the figure never shifts when a draft begins.
            return Mathf.Max(standing, draft) + RefPencilSlot * Scale(figureStyle);
        }

        /// <summary>P2-1.3: the widest unbreakable token of the name - the name cell wraps at spaces before it shrinks, so
        /// a single long word (a hyphenated party name) is the width the cell cannot go under.</summary>
        private static float NameNeed(string name, GUIStyle nameStyle)
        {
            if (string.IsNullOrEmpty(name)) { return 0f; }
            float widest = 0f;
            foreach (string token in name.Split(' '))
            {
                widest = Mathf.Max(widest, nameStyle.CalcSize(new GUIContent(token)).x);
            }
            return widest + RefColumnGap * Scale(nameStyle);
        }

        private static void Columns(Rect row, GUIStyle nameStyle, float nameNeed, float trailingNeed, float figureNeed,
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
            // P2-1.3 (2026-09-02): LONGER TRACK - the fixed columns take less of the row (name 26 -> 20 %, figure
            // 19 -> 14 %, trailing 11 -> 9 %, the needs capped at 24 % instead of 34 %), so the track is what is
            // left, roughly half the row; the reach record below is what proves a whole point is reachable.
            float nameWidth = Mathf.Max(row.width * 0.18f, RefKnobWidth * scale * 4f);
            if (nameNeed > nameWidth)
            {
                nameWidth = Mathf.Min(nameNeed, row.width * 0.30f);   // P2-1.3: a name that cannot wrap takes up to 30 %
            }
            float figureWidth = Mathf.Max(row.width * 0.13f, RefKnobWidth * scale * 3f);
            // P5-1 (board 6a, measured on film, 2026-09-03): NO TRAILING COLUMN. The board's rate cell carries its measure beneath it, so the
            // trailing figure (a tax line's revenue, a programme's cost, a dial's end-names) draws in the caption band the family
            // already reserves under every row - under the rate cell, or under the track ends for an end-name legend - and the
            // column it used to take (8-22 % of the row) goes to the track. On the Budget's narrowest welfare row the track had
            // hit its floor at 1.37 units per pixel against the whole-unit bound; the column was the lever.
            float trailingWidth = 0f;
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
            // (the trailing need no longer sizes a column - see above)

            // ⚠ THE FIGURE COLUMN ASKS WHAT IT IS HOLDING TOO (2026-08-28, UI v3.0 Phase A - the
            // label-clipping class's instance #15). The v3 rail took ~75 px from the Budget ledger's
            // centre column at 1280×720, and "not implemented" - a STATUS WORD in a column proportioned
            // for "$1.05T" - no longer fit at the guard's 8 px floor (60.6 px needed in 57.4, twelve
            // captures). Same rule as the trailing column, same cap, the track giving ground: a
            // proportion is a measurement at one width, and a word is not a figure.
            if (figureNeed > figureWidth)
            {
                figureWidth = Mathf.Min(figureNeed, row.width * 0.22f);
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
            trackRect = new Rect(nameRect.xMax + gap, row.y + (row.height - RefEndCaption * scale - RefTrackHeight * scale) * 0.5f, trackWidth, RefTrackHeight * scale);
            figureRect = new Rect(trackRect.xMax + gap, row.y, figureWidth, row.height);
            trailingRect = new Rect(figureRect.xMax + gap, row.y, trailingWidth, row.height);

            // ⚠ THE TRAILING COLUMN IS THE ONE THAT CAN ESCAPE, and it is the one that recently learned
            // to size itself from its content. Four widths, three gaps and a squeeze all feed its x, so
            // "the last column ends inside the row" is a consequence of arithmetic nobody re-checks
            // after touching any one term. The others are asserted too because they cost nothing and
            // because the squeeze moves all of them together.
            UiContainmentGuard.Check("LedgerRow name column", nameRect, row);
            UiContainmentGuard.Check("LedgerRow track", trackRect, row);
            UiContainmentGuard.Check("LedgerRow figure column", figureRect, row);
            if (trailingRect.width > 0f) { UiContainmentGuard.Check("LedgerRow trailing column", trailingRect, row); }
        }

        /// <summary>The standing tick and the draft hatch band - drawn UNDER the slider so the knob reads as sitting on the track rather than beside it.</summary>
        private static void DrawTrackFurniture(Rect track, float standing, float draft, float min, float max, float scale, bool interactive, float tickStep)
        {
            float span = Mathf.Max(0.0001f, max - min);
            float standingX = track.x + track.width * Mathf.Clamp01((standing - min) / span);
            float draftX = track.x + track.width * Mathf.Clamp01((draft - min) / span);

            // ⚠ v2.0 CHROME, 2026-08-11 — `ui_slider_tick` every 10%, per §A.9's track spec: *"ticks as
            // repeating-linear 90° #B7A98C 0->1px, transparent 1->10% (every 10%)"*. Drawn FIRST, under
            // the hatch and the standing tick, because they are the ruled scale the two markers are read
            // AGAINST — a tick that sits on top of the draft band would compete with the thing it exists
            // to measure.
            //
            // ⚠ TINTED TO `PoliSimTheme.Hairline`, WHICH IS §A.9's `#B7A98C` EXACTLY — and getting this
            // wrong is what §3.0a's question exists to prevent. The first version drew it untinted, on
            // the reasoning that `Chrome/`'s slider parts are real-colour paper furniture like
            // `ui_btn_*`. **They are not: this sprite is white-on-alpha, and the first capture showed
            // nine white bars across every track.** Reasoning by analogy from a NEIGHBOURING sprite is
            // the reference-class trap again — the family a sprite sits in does not settle how it is
            // drawn, only what it is drawn from.
            //
            // Null-safe by the same contract as the hatch below: no sprite means no ticks, and the track
            // still reads. The scale is a refinement on a control that already works without it.
            Texture2D tick = IconLibrary.GetChrome("ui_slider_tick");
            if (tick != null && track.width > 0f)
            {
                float tickWidth = Mathf.Max(1f, RefTickWidth * scale * 0.5f);
                Color tickPrev = GUI.color;
                GUI.color = PoliSimTheme.Hairline;
                // P5-1 (board 6a): SPARSE - a quarter of the span on every 0-100 dial (0/25/50/75/100), and the caller's own step on
                // a dial in points (a quarter-point on the rate sliders) widened fourfold until the pitch holds 12 px @1x.
                float step = tickStep > 0f ? tickStep : span / 4f;
                while (step > 0f && track.width * (step / span) < RefTickMinPitch * scale && step < span) { step *= 4f; }
                for (float v = min; v <= max + step * 0.001f; v += step)
                {
                    float x = track.x + track.width * Mathf.Clamp01((v - min) / span);
                    GUI.DrawTexture(new Rect(x - tickWidth * 0.5f, track.y, tickWidth, track.height), tick, ScaleMode.StretchToFill);
                }
                GUI.color = tickPrev;
            }

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

        /// <summary>P5-1 (board 6a): whether a trailing text is an end-name legend ("0 nationalized - 100 deregulated") - then it sits UNDER the track ends and the trailing cell holds its absence.</summary>
        private static readonly System.Text.RegularExpressions.Regex EndNames = new System.Text.RegularExpressions.Regex(@"^\s*(?<n>-?\d+(?:\.\d+)?)\s+(?<a>[^-]+?)\s*-\s*(?<m>-?\d+(?:\.\d+)?)\s+(?<b>.+?)\s*$");
        public static bool IsEndNames(string trailingText) => !string.IsNullOrEmpty(trailingText) && EndNames.IsMatch(trailingText);

        private static GUIStyle _endCaptionStyle;
        private static int _endCaptionSourceSize = -1;

        /// <summary>The end-names under the track ends in caption mono (board 6a: 7.5 board px - here 0.6 of the row's figure size, floored at 8), left end left-aligned, right end right-aligned; furniture, never on the track.</summary>
        /// <summary>P4-B2: the track rect the last <see cref="Draw"/> painted on this Repaint, and its scale, so the caller can place a range caption in the caption band beneath it.</summary>
        public static Rect LastTrackRect;
        public static float LastScale = 1f;
        /// <summary>P4-B2: whether the last row drew end-names in its band (the caption then keeps clear of the band's two ends).</summary>
        public static bool LastHadEndNames;

        /// <summary>P4-B2: the caption band beneath the last row's track - the rect <see cref="DrawEndNames"/> writes its two ends into.</summary>
        public static Rect LastCaptionBand => new Rect(LastTrackRect.x, LastTrackRect.yMax + 1f * LastScale, LastTrackRect.width, RefEndCaption * LastScale);

        /// <summary>P4-B2: the caption face the band is set in (the end-names' own), for a range caption drawn into the same band.</summary>
        public static GUIStyle CaptionStyle(GUIStyle figureStyle)
        {
            int size = Mathf.Max(8, Mathf.RoundToInt(figureStyle.fontSize * 0.6f));
            if (_endCaptionStyle == null || _endCaptionSourceSize != size)
            {
                _endCaptionStyle = new GUIStyle(figureStyle) { fontSize = size, wordWrap = false, fontStyle = FontStyle.Normal };
                if (PoliSimTheme.Document != null) { _endCaptionStyle.font = PoliSimTheme.Document; }
                _endCaptionStyle.padding = new RectOffset(0, 0, 0, 0);
                _endCaptionSourceSize = size;
            }
            return _endCaptionStyle;
        }

        private static void DrawEndNames(Rect track, string trailingText, GUIStyle figureStyle, float scale, bool interactive)
        {
            if (!IsEndNames(trailingText)) { return; }
            System.Text.RegularExpressions.Match m = EndNames.Match(trailingText);
            int size = Mathf.Max(8, Mathf.RoundToInt(figureStyle.fontSize * 0.6f));
            if (_endCaptionStyle == null || _endCaptionSourceSize != size)
            {
                _endCaptionStyle = new GUIStyle(figureStyle) { fontSize = size, wordWrap = false, fontStyle = FontStyle.Normal };
                if (PoliSimTheme.Document != null) { _endCaptionStyle.font = PoliSimTheme.Document; }
                _endCaptionStyle.padding = new RectOffset(0, 0, 0, 0);
                _endCaptionSourceSize = size;
            }
            Color ink = interactive ? PoliSimTheme.TextSecondary : PoliSimTheme.TextMuted;
            var band = new Rect(track.x, track.yMax + 1f * scale, track.width, RefEndCaption * scale);
            string left = (m.Groups["n"].Value + " " + m.Groups["a"].Value).ToUpperInvariant();
            string right = (m.Groups["m"].Value + " " + m.Groups["b"].Value).ToUpperInvariant();
            Cell(new Rect(band.x, band.y, band.width * 0.5f, band.height), left, _endCaptionStyle, ink, TextAnchor.UpperLeft);
            Cell(new Rect(band.x + band.width * 0.5f, band.y, band.width * 0.5f, band.height), right, _endCaptionStyle, ink, TextAnchor.UpperRight);
        }

        /// <summary>P5-1 (board 6a): the trailing measure UNDER the rate cell in the caption band - right-aligned to the figure, caption mono, secondary ink.</summary>
        private static void DrawTrailingUnderFigure(Rect figure, Rect track, string trailingText, GUIStyle figureStyle, float scale, bool interactive)
        {
            if (string.IsNullOrEmpty(trailingText)) { return; }
            GUIStyle caption = EndCaptionStyle(figureStyle);
            Color ink = interactive ? PoliSimTheme.TextSecondary : PoliSimTheme.TextMuted;
            // The band runs from the track's start to the rate cell's right edge and the measure right-aligns to the rate: a figure sits
            // under the rate, and a prose trailing (the tariff dial's "inert - bloc rates apply to every partner") has the track's
            // width too - the end-names, which own the track's band, are the exclusive other case.
            var band = new Rect(track.x, track.yMax + 1f * scale, figure.xMax - track.x, RefEndCaption * scale);
            Cell(band, trailingText.ToUpperInvariant(), caption, ink, TextAnchor.UpperRight);
        }

        private static GUIStyle EndCaptionStyle(GUIStyle figureStyle)
        {
            int size = Mathf.Max(8, Mathf.RoundToInt(figureStyle.fontSize * 0.6f));
            if (_endCaptionStyle == null || _endCaptionSourceSize != size)
            {
                _endCaptionStyle = new GUIStyle(figureStyle) { fontSize = size, wordWrap = false, fontStyle = FontStyle.Normal };
                if (PoliSimTheme.Document != null) { _endCaptionStyle.font = PoliSimTheme.Document; }
                _endCaptionStyle.padding = new RectOffset(0, 0, 0, 0);
                _endCaptionSourceSize = size;
            }
            return _endCaptionStyle;
        }

        private static GUIStyle _knobStyle, _knobDisabledStyle;
        private static float _knobScale = -1f;

        /// <summary>P5-1 (board 6a): the brass knob at 15x23 @1x, uniform scale, the hit rect the sprite rect; the disabled face when the row cannot be moved (PENDING, or not applicable) - the row stays drawn and counted.</summary>
        private static GUIStyle KnobStyle(GUIStyle thumbStyle, float scale, bool interactive)
        {
            if (_knobStyle == null || !Mathf.Approximately(_knobScale, scale))
            {
                _knobStyle = new GUIStyle(thumbStyle) { fixedWidth = Mathf.Round(RefKnobSpriteWidth * scale), fixedHeight = Mathf.Round(RefKnobSpriteHeight * scale) };
                _knobDisabledStyle = new GUIStyle(_knobStyle);
                Texture2D disabled = IconLibrary.GetChrome("ui_slider_knob_disabled");
                if (disabled != null) { _knobDisabledStyle.normal.background = disabled; _knobDisabledStyle.hover.background = disabled; _knobDisabledStyle.active.background = disabled; _knobDisabledStyle.focused.background = disabled; }
                _knobScale = scale;
            }
            return interactive ? _knobStyle : _knobDisabledStyle;
        }

        /// <summary>
        /// ONE readout (P4-1, 2026-09-03): the standing figure at rest in the row's ink; while a draft differs,
        /// the same cell shows the DRAFT figure in the draft cue (D6: the darkened Caution ink for text on paper;
        /// the knob and the hatch on the track keep the amber fill). No second number, no reflow - the standing
        /// value stays readable as the hard tick on the track, and the hatch band is the change. The pair this
        /// used to print ("standing → draft" in two halves) is what shrank the track the moment a draft appeared.
        /// </summary>
        private static void DrawFigurePair(Rect rect, string standingText, string draftText, GUIStyle style, Color rowInk)
        {
            bool drafted = !string.IsNullOrEmpty(draftText);
            // P5-1 (board 6a): the pencil's slot at the cell's left is reserved at rest and filled while a draft differs - one draft colour, three carriers (the hatch, the pencil, the figure).
            float scale = Scale(style);
            float slot = RefPencilSlot * scale;
            if (drafted && Event.current.type == EventType.Repaint)
            {
                Texture2D pencil = IconLibrary.GetChrome("icon_pencil_draft");
                if (pencil != null)
                {
                    float size = RefPencilSlot * scale;
                    Color previous = GUI.color; GUI.color = PoliSimTheme.Caution;
                    GUI.DrawTexture(new Rect(rect.x, rect.y + (rect.height - size) * 0.5f, size, size), pencil, ScaleMode.ScaleToFit);
                    GUI.color = previous;
                }
            }
            DrawCell(new Rect(rect.x + slot, rect.y, Mathf.Max(1f, rect.width - slot), rect.height), drafted ? draftText : standingText, style, drafted ? PoliSimTheme.Caution : rowInk, TextAnchor.MiddleRight);
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
            Columns(row, nameStyle, NameNeed(name, nameStyle), TrailingNeed(trailingText, figureStyle), FigureNeed(figureText, null, figureStyle),
                out Rect nameRect, out Rect trackRect, out Rect figureRect, out Rect trailingRect);

            DrawNameCell(nameRect, name, nameStyle, PoliSimTheme.TextPrimary);

            // ⚠ A NEGATIVE FILL MEANS "THIS FIGURE HAS NO DENOMINATOR" — no track, no gauge, nothing in
            // the lane. §A.9 does not describe this case: every read-only row it specifies is a
            // proportion (a condition index out of 100, a share of GDP), and `DrawReadOnly` was built
            // assuming one. Statistics has figures that are genuinely unbounded — GDP per capita is
            // currency per person, with no ceiling to be a fraction of.
            //
            // Drawing an empty track for those would be WORSE than drawing nothing, because an empty
            // track is not neutral: it reads as a gauge sitting at zero, which is a confident wrong
            // number of exactly the kind this project keeps finding.
            //
            // The `-1f` convention is NOT invented here — `Draw`'s own `barFraction` parameter already
            // means "no bar" at negative, twelve lines up in this same file. Reusing it keeps one idiom
            // rather than adding a second way to say the same thing.
            if (fill >= 0f && Event.current.type == EventType.Repaint)
            {
                Color previous = GUI.color;
                GUI.color = PoliSimTheme.BarTrack;
                GUI.DrawTexture(trackRect, Texture2D.whiteTexture);
                GUI.color = barInk;
                GUI.DrawTexture(new Rect(trackRect.x, trackRect.y, trackRect.width * Mathf.Clamp01(fill), trackRect.height), Texture2D.whiteTexture);
                GUI.color = previous;
            }

            DrawCell(figureRect, figureText, figureStyle, PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
            // P5-1 (board 6a): a read-only row with an end-name legend draws it under the track ends too, and the trailing cell holds its absence.
            if (Event.current.type == EventType.Repaint) { DrawEndNames(trackRect, trailingText, figureStyle, Scale(nameStyle), true); }
            if (!IsEndNames(trailingText) && Event.current.type == EventType.Repaint) { DrawTrailingUnderFigure(figureRect, trackRect, trailingText, figureStyle, Scale(nameStyle), true); }   // P5-1: under the rate cell
        }

        /// <summary>Public so a caller composing extra columns beside a row - the tax screen's verdict cell, say - prints them in the same measured, never-clipping way rather than reaching for a raw GUI.Label.</summary>
        public static void Cell(Rect rect, string text, GUIStyle source, Color ink, TextAnchor alignment)
        {
            DrawCell(rect, text, source, ink, alignment);
        }

        /// <summary>The NAME-cell resort ladder (wrap to two lines at full size before shrinking -
        /// §A.9a step 2 ahead of step 4), public for the same composing-caller reason as
        /// <see cref="Cell"/> - pass 3's floor sweep found the law browser's name cells shrinking
        /// long statute names past MeasuredLabel's 8px floor ("Restorative Justice &amp;
        /// Victim-Offender Mediation needs 170.8 wide in 141.8 at 8px", 1280x720), exactly the
        /// class this ladder was built for. Always MiddleLeft, like every name cell.</summary>
        public static void NameCell(Rect rect, string text, GUIStyle source, Color ink)
        {
            DrawNameCell(rect, text, source, ink);
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
        /// <summary>
        /// The narrowest line word-wrapping can produce: no line can be shorter than the longest run
        /// with no break opportunity in it. This is the width a wrapped label genuinely needs, as
        /// distinct from the width the whole string would need unwrapped.
        /// </summary>
        private static float WidestUnbreakableRun(string text, GUIStyle style)
        {
            bool previousWrap = style.wordWrap;
            style.wordWrap = false;

            float widest = 0f;
            string[] runs = text.Split(' ');
            for (int i = 0; i < runs.Length; i++)
            {
                if (runs[i].Length == 0)
                {
                    continue;
                }

                widest = Mathf.Max(widest, style.CalcSize(new GUIContent(runs[i])).x);
            }

            style.wordWrap = previousWrap;
            return widest;
        }

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
            Vector2 flat = style.CalcSize(content);
            if (flat.x <= rect.width)
            {
                // ⚠ CHECKED EVEN THOUGH IT FITS, because it was chosen on WIDTH ALONE. Nothing on this
                // path has looked at height, and a name set at full size inside a row whose pitch was
                // derived from a different font metric spills into the row beneath it - the row-pitch
                // class, which has produced its own instances and which no width test can see.
                UiOverflowGuard.Check(text, flat, rect.size, style.fontSize);
                GUI.Label(rect, content, style);
                return;
            }

            style.wordWrap = true;
            float wrappedHeight = style.CalcHeight(content, rect.width);
            if (wrappedHeight <= rect.height)
            {
                // ⚠ AND THIS ONE WAS CHOSEN ON HEIGHT ALONE. CalcHeight answers "how tall once wrapped
                // to this width" and is silent about what the wrap had to DO to get there. When a single
                // token is wider than the column - a raw enum name like `NegativeIncomeTax`, which has no
                // space to break on - IMGUI does not overflow. It breaks mid-word, and the row renders
                // "NegativeInco / meTax": measured as fitting, and unreadable.
                //
                // So the quantity to test is the WIDEST PIECE THE WRAP CANNOT SPLIT. Wider than the
                // column means the wrap can only proceed by cutting through a word, which is a defect
                // whether or not any pixel leaves the box. Confirmed in
                // screenshots/run_05c_budget_welfare_deep.png before this check was written.
                UiOverflowGuard.Check(text, new Vector2(WidestUnbreakableRun(text, style), wrappedHeight),
                    rect.size, style.fontSize);
                GUI.Label(rect, content, style);
                return;
            }

            // §A.9a's missing rung (2026-08-28, UI v3.0 Phase A - the label-clipping class's instance
            // #15, its second column): TWO LINES AT A REDUCED SIZE before one line at the floor. The
            // wrap at full size fails for one of two reasons - a token wider than the column, or two
            // lines taller than the row - and both give way to a smaller size long before the 8 px
            // floor does: in the rail-narrowed Budget ledger at 1280×720 "Universal Healthcare" and
            // "Negative Income Tax" shrank to 8 px on one line and still missed by 3 px, where two
            // lines at 13 px fit. Largest size first, so the name gives up as little as it can; the
            // shared cell style's size is restored either way (CellStyle re-seeds it, but a caller
            // composing beside this cell in the same frame must not see the shrunken value).
            int fullSize = style.fontSize;
            for (int size = fullSize - 1; size >= PoliSimWidgets.MinMeasuredLabelFontSize; size--)
            {
                style.fontSize = size;
                float widestRun = WidestUnbreakableRun(text, style);
                if (widestRun > rect.width)
                {
                    continue;
                }

                style.wordWrap = true;
                float reducedHeight = style.CalcHeight(content, rect.width);
                if (reducedHeight > rect.height)
                {
                    continue;
                }

                UiOverflowGuard.Check(text, new Vector2(widestRun, reducedHeight), rect.size, style.fontSize);
                GUI.Label(rect, content, style);
                style.fontSize = fullSize;
                return;
            }

            style.fontSize = fullSize;

            // Two lines still will not hold it - shrink, which is the floor of the ladder rather than
            // its first move. MeasuredLabel re-seeds wordWrap itself, so the cached style is safe to
            // hand over in whatever state this left it.
            PoliSimWidgets.MeasuredLabel(rect, text, style);
        }
    }
}
