using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// The six reusable widgets the R2 GUI is assembled from — stat tile, threshold bar, legislative
    /// support bar, standing/draft pair, decision-card chrome and the geometric portrait. Every one
    /// is drawn from <see cref="PoliSimTheme"/> primitives (rounded rects, lines, arcs), so a screen
    /// is a layout of these calls rather than a hand-rolled block of GUILayout per tab.
    /// All sizes are unscaled px; pass <paramref name="scale"/> from GameController's existing UI scale.
    /// </summary>
    public static class PoliSimWidgets
    {
        // --- Styles -----------------------------------------------------------------------

        private static GUIStyle _label, _value, _body, _mono;
        private static float _builtAtScale = -1f;

        /// <summary>Floor for StatTile's shrink-to-fit (see StatTile). Below this a headline figure stops being readable at a glance, and a tile that narrow is a layout problem to fix rather than something to keep shrinking text into.</summary>
        private const int MinStatValueFontSize = 11;

        private static void EnsureStyles(float scale)
        {
            if (Mathf.Approximately(_builtAtScale, scale) && _label != null)
            {
                return;
            }

            _builtAtScale = scale;

            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(PoliSimTheme.FontLabel * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 0, 0)
            };
            _label.normal.textColor = PoliSimTheme.TextMuted;

            _value = new GUIStyle(_label)
            {
                fontSize = Mathf.RoundToInt(PoliSimTheme.FontStatHero * scale),
                fontStyle = FontStyle.Bold
            };
            _value.normal.textColor = PoliSimTheme.TextPrimary;

            _body = new GUIStyle(_label)
            {
                fontSize = Mathf.RoundToInt(PoliSimTheme.FontBodySmall * scale),
                fontStyle = FontStyle.Normal
            };
            _body.normal.textColor = PoliSimTheme.TextSecondary;

            _mono = new GUIStyle(_body)
            {
                fontSize = Mathf.RoundToInt(PoliSimTheme.FontMicro * scale + 2f)
            };
            _mono.normal.textColor = PoliSimTheme.TextMuted;

            // v2.0 typography. **This block closes a real gap.** The survey's font test assigned faces to
            // GameController's 15 styles and every headline stat value on screen stayed in Unity's default
            // - because these four styles are built HERE, from `GUI.skin.label`, and nothing at the call
            // site can see them. Sized() derives from these four, so the face propagates to every widget
            // variant from these assignments alone.
            //
            // `_mono` takes the DOCUMENT face rather than the body face: it is this file's meta/annotation
            // style, the closest the widget set has to a printed-instrument voice. Everything else takes
            // body. Null-safe via PoliSimTheme, so a missing font leaves Unity's default.
            PoliSimTheme.WithBody(_label);
            PoliSimTheme.WithBody(_value);
            PoliSimTheme.WithBody(_body);
            if (PoliSimTheme.Document != null) { _mono.font = PoliSimTheme.Document; }
        }

        /// <summary>
        /// The v2.0 plate sprite, or false when it is missing so the caller can fall back to the
        /// procedural rounded card. Cached because a stat tile is drawn many times per frame and
        /// `Resources.Load` does not cache the managed reference across calls.
        /// </summary>
        private static Texture2D _plate;
        private static bool _plateResolved;

        private static bool PlateSprite(out Texture2D plate)
        {
            if (!_plateResolved)
            {
                _plate = IconLibrary.GetChrome("ui_plate_tile");
                _plateResolved = true;
            }

            plate = _plate;
            return plate != null;
        }

        private static GUIStyle Sized(GUIStyle basis, int unscaledSize, Color color, float scale, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var s = new GUIStyle(basis)
            {
                fontSize = Mathf.RoundToInt(unscaledSize * scale),
                alignment = anchor
            };
            s.normal.textColor = color;
            return s;
        }

        /// <summary>Floor for <see cref="MeasuredLabel"/>'s shrink-to-fit. Deliberately lower than
        /// <see cref="MinStatValueFontSize"/>: a supporting label may shrink further than a headline
        /// figure before it stops being worth drawing, and a shared floor would either clip labels or
        /// hold headline values larger than their tile.</summary>
        private const int MinMeasuredLabelFontSize = 8;

        /// <summary>
        /// Draws text guaranteed to fit its rect, by measuring it in THE STYLE IT WILL ACTUALLY RENDER IN
        /// and shrinking the font until it fits.
        ///
        /// **One fix for a class that has now recurred seven times** — the Manufacturing sector label,
        /// World Map country names, TaxLine/WelfareProgram rows, Policy Web category headers,
        /// Policy/Laws' "Trade" button, Budget tile labels, and `StatTile`'s value field. Every instance
        /// is the same shape: a text rect sized by a hardcoded constant instead of by measuring the
        /// string, in a project where every style rescales with the window.
        ///
        /// **The argument for a helper rather than an eighth site-specific fix is in this very file.**
        /// `StatTile`'s VALUE field has carried this exact treatment since the "9,3" incident, with a
        /// comment naming the cause — and the LABEL field ten lines above it never got it, and clipped.
        /// A fix that must be remembered at each site will be forgotten at the next one.
        ///
        /// **Shrink, never truncate.** Truncating changes what the text says: "29689,3" clipped to "2968"
        /// is a plausible-looking wrong number, the worst failure a readout can have. Shrinking makes text
        /// smaller but never different. Wrapping to two lines inside a one-line rect is the same failure
        /// in another hat — both wrapped lines then lose their tops and bottoms — so `wordWrap` is forced
        /// off here rather than left to whatever `GUI.skin.label` was inherited with.
        ///
        /// Measured per frame on purpose: styles derive from `scale`, which derives from `Screen.height`,
        /// so a width cached at one window size is wrong at the next.
        /// </summary>
        /// <param name="reserveWidth">Width already spoken for inside the same rect — a suffix, a pill, a
        /// following control — so text is fitted to what is genuinely left rather than to the whole rect.</param>
        /// <returns>The rendered size, so a caller can lay out what follows against real geometry instead
        /// of an assumed width.</returns>
        /// <summary>
        /// Per-source scratch clones, so shrinking never touches a caller's style.
        ///
        /// ⚠ **This method used to mutate the style it was handed** - setting `wordWrap` and, when text
        /// overflowed, permanently lowering `fontSize`. Handing it a shared style therefore shrank that
        /// style a little more on every frame, compounding until layout collapsed across every screen
        /// using it.
        ///
        /// **It caught two callers.** `PolicyScreenStatsRenderer` worked around it with a private cached
        /// `ChipTextStyle`; `LedgerRow` then hit the identical bug from scratch and needed the identical
        /// workaround. **A method that permanently mutates its argument is a defect in the method, not a
        /// discipline problem for callers** - the second occurrence proved the convention was not
        /// transmissible. The shared-style path is now impossible rather than merely discouraged.
        ///
        /// Keyed on the source style by reference and refreshed from it each call, so a style that
        /// rescales with `Screen.height` is followed rather than frozen, and the previous call's shrink
        /// is undone rather than accumulated.
        /// </summary>
        private static readonly Dictionary<GUIStyle, GUIStyle> MeasuredScratchStyles = new Dictionary<GUIStyle, GUIStyle>();

        private static GUIStyle MeasuredScratch(GUIStyle source)
        {
            if (!MeasuredScratchStyles.TryGetValue(source, out GUIStyle scratch) || scratch == null)
            {
                scratch = new GUIStyle(source);
                MeasuredScratchStyles[source] = scratch;
            }

            // Re-seeded from the source every call. fontSize is the one that matters (it both follows a
            // window resize and undoes the previous call's shrink); the rest are copied because a caller
            // legitimately varies them per cell and the scratch must not remember the last cell's.
            scratch.fontSize = source.fontSize;
            scratch.font = source.font;
            scratch.alignment = source.alignment;
            scratch.normal.textColor = source.normal.textColor;
            scratch.padding = source.padding;
            return scratch;
        }

        /// <summary>
        /// What one child of <paramref name="container"/> may actually claim, given the container's OUTER
        /// width. The single implementation of a subtraction that has been forgotten at four sites and
        /// hand-rolled at two.
        ///
        /// ⚠ **FOUR TERMS SINCE 2026-08-11 — the CONTAINER'S OWN MARGIN was missing, and it was missing
        /// from my own first version.** <paramref name="outerWidth"/> is the budget the container is drawn
        /// INTO, not the width the container ends up with, and that distinction is the whole of the fourth
        /// term. Every caller here passes a column width that a `_boxStyle` box is then laid out inside,
        /// and GUILayout takes that box's `margin` out of the column before the box gets anything — so the
        /// subtraction was 8px short at all four sites from the day it was written. Measured at 1600×929:
        /// the Politics sub-tab row ran past the clip rect and lost the right edge of "Federal Reserve" by
        /// exactly `_boxStyle.margin.horizontal`. **A helper is not evidence that its arithmetic is
        /// complete.**
        ///
        /// <para><b>Each term has been the bug.</b> The container's PADDING is space the
        /// child never sees (Budget Process, three sites, overflowing by exactly padding.horizontal). The
        /// child's MARGINS are inserted between siblings and come out of the same budget (the Policy/Laws
        /// sub-tab row, 20px short across five buttons). And the CHILD COUNT divides what is left - the
        /// term a style-only accessor could not carry, and the one that went missing.</para>
        ///
        /// <para>Pass <paramref name="childCount"/> 1 and no <paramref name="child"/> for the common case:
        /// "how wide may content inside this box be". Pass both for a row of n siblings.</para>
        ///
        /// ⚠ This answers a LAYOUT question, not a CONTENT one. It says what space exists, never whether
        /// what you intend to put there fits - the trailing caption column overflowed by 159px with the
        /// arithmetic entirely correct, because the column was sized for figures and given prose.
        /// <see cref="UiOverflowGuard"/> is what catches that.
        /// </summary>
        public static float InnerWidth(float outerWidth, GUIStyle container, int childCount = 1, GUIStyle child = null)
        {
            int count = Mathf.Max(1, childCount);
            float padding = container != null ? container.padding.horizontal : 0f;
            float containerMargin = container != null ? container.margin.horizontal : 0f;
            float margins = child != null ? count * child.margin.horizontal : 0f;
            return Mathf.Max(1f, (outerWidth - padding - containerMargin - margins) / count);
        }

        /// <summary>
        /// The vertical twin of <see cref="InnerWidth"/>, and it exists because the HEIGHT half of the
        /// same subtraction had no helper at all — so it was never performed anywhere.
        ///
        /// <para>Same contract: <paramref name="outerHeight"/> is the budget the container is drawn INTO.
        /// Both of this UI's full-height containers — the left column's box, and the box each tab wraps
        /// its content in — reserved their internal rows against the RAW area height, so each stood
        /// `padding.vertical + margin.vertical` taller than the clip rect containing it, and the overrun
        /// landed on whatever was drawn last.</para>
        ///
        /// ⚠ **This is instance #12, and it is precisely what a width-only helper could not prevent.**
        /// Measured 2026-08-11 at 1600×929: content sat flush against the bottom clip edge on **all 54
        /// gameplay screens**, and the left column's Pause/1x/2x/3x strip — the control this UI can least
        /// afford to lose, being the only always-visible one — was cut through the middle. Both text
        /// guards reported zero throughout, correctly: every label fitted the rect it was handed.
        ///
        /// <para><b>No child-count or child-margin terms, deliberately.</b> A vertical row of n equal
        /// siblings is not something this UI lays out — its stacks are heterogeneous (a status label,
        /// then a button row), so their heights are measured individually rather than divided. Adding
        /// the parameters "for symmetry" would invite dividing a column height by a count, which is not a
        /// question anything here asks.</para>
        /// </summary>
        public static float InnerHeight(float outerHeight, GUIStyle container)
        {
            float padding = container != null ? container.padding.vertical : 0f;
            float margin = container != null ? container.margin.vertical : 0f;
            return Mathf.Max(1f, outerHeight - padding - margin);
        }

        public static Vector2 MeasuredLabel(Rect rect, string text, GUIStyle style, float reserveWidth = 0f)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Vector2.zero;
            }

            // The caller's style is READ, never written. Everything below shrinks the scratch.
            GUIStyle fitted = MeasuredScratch(style);
            fitted.wordWrap = false;

            float available = rect.width - reserveWidth;
            Vector2 size = fitted.CalcSize(new GUIContent(text));

            if (size.x > available && size.x > 0f && available > 0f)
            {
                fitted.fontSize = Mathf.Max(MinMeasuredLabelFontSize, Mathf.FloorToInt(fitted.fontSize * (available / size.x)));
                size = fitted.CalcSize(new GUIContent(text));
            }

            // Shrink-to-fit has now done everything it can. If it still does not fit, the label clips -
            // there is no further resort below the floor - so record it rather than drawing it quietly.
            // Height goes in too: shrinking solves width, and a single line too tall for its rect is a
            // different defect that this path has never looked at.
            UiOverflowGuard.Check(text, size, new Vector2(available, rect.height), fitted.fontSize);

            GUI.Label(new Rect(rect.x, rect.y, available, rect.height), text, fitted);
            return size;
        }

        /// <summary>
        /// How wide this text genuinely needs, in the style it will render in, plus margin.
        ///
        /// For the WIDTH variant of the same class: a row of `ExpandWidth` controls with no width budget,
        /// where the last one clips because the container over-committed. This lets a caller ask before
        /// laying out instead of discovering it at draw time.
        /// </summary>
        public static float MeasuredWidth(string text, GUIStyle style, float margin = 0f)
        {
            if (string.IsNullOrEmpty(text))
            {
                return margin;
            }

            return style.CalcSize(new GUIContent(text)).x + margin;
        }

        // --- 1. Stat tile -----------------------------------------------------------------

        /// <summary>
        /// The tile's vertical stack, in unscaled units, named once so the drawing code and the height
        /// accessor cannot disagree.
        ///
        /// ⚠ **They DID disagree, and it is the defect Elias reported.** The caller sized every tile at
        /// a flat `92 * scale` while this stack needs ~107 with a delta present, so the delta was drawn
        /// BELOW the tile's own bottom edge and collided with the next tile's keyline
        /// (`run_02_statistics.png`, top-left). A magic number in the caller and a cumulative `y` in the
        /// widget are two statements of the same geometry, which is one more than can be kept true.
        /// </summary>
        private const float TilePadY = 16f;
        private const float TileLabelHeight = 12f;
        private const float TileLabelBlock = 20f;
        private const float TileValueGap = 9f;
        private const float TileDeltaHeight = 18f;
        private const float TileDeltaGap = 8f;
        private const float TileValueLeading = 1.05f;

        /// <summary>
        /// How tall a tile must be to hold what it will be asked to draw. Callers laying out a grid
        /// should ask rather than assume — a tile shorter than this does not clip its overflow, it draws
        /// it onto whatever is underneath.
        /// </summary>
        public static float StatTileHeight(float scale, bool hasDelta, bool hasBar)
        {
            float height = TilePadY + TileLabelBlock + PoliSimTheme.FontStatHero * TileValueLeading + TileValueGap;

            if (hasDelta)
            {
                height += TileDeltaHeight;
                if (hasBar)
                {
                    height += TileDeltaGap;
                }
            }

            if (hasBar)
            {
                height += PoliSimTheme.BarHeightSm;
            }

            return (height + TilePadY) * scale;
        }

        /// <summary>
        /// Headline figure with a small caps label, an optional signed delta pill and an optional
        /// capacity bar with a threshold tick. <paramref name="barFraction"/> below zero hides the bar.
        ///
        /// ⚠ Size the <paramref name="rect"/> with <see cref="StatTileHeight"/>. Nothing here clamps to
        /// it — the content stack draws where the arithmetic puts it, on top of the neighbour if need be.
        /// </summary>
        public static void StatTile(
            Rect rect,
            string label,
            string value,
            string suffix,
            string delta,
            bool deltaIsGood,
            string subLabel,
            UiPalette.SystemArea area,
            float scale,
            float barFraction = -1f,
            float thresholdFraction = -1f)
        {
            EnsureStyles(scale);

            // v2.0: a stat tile is a printed plate. ui_plate_tile is 128x96 @2x, inset 14/14/14/18 - the
            // bottom deeper for its baked shadow - so the @1x border is 7/7/7/9. The area keyline along
            // the top edge stays: it is B9's colour key, drawn over the plate rather than replacing it.
            //
            // W2 from the direction doc lands here: this is the flattest surface in the game on purpose.
            // Grain behind a figure that redraws every frame shimmers, so ui_grain_tile must never sit
            // under a numeral plate.
            if (!PlateSprite(out Texture2D plate))
            {
                PoliSimTheme.RoundedCard(rect, PoliSimTheme.Card, PoliSimTheme.Hairline, PoliSimTheme.RadiusCard * scale);
            }
            else
            {
                GUI.DrawTexture(rect, plate, ScaleMode.StretchToFill, true);
            }

            PoliSimTheme.TopAccent(rect, area, 2f * scale);

            float padX = 17f * scale;
            float padY = TilePadY * scale;
            float x = rect.x + padX;
            float y = rect.y + padY;
            float innerWidth = rect.width - padX * 2f;

            // REVIEW ITEM 6 WAS HERE, and it was the "9,3" bug one field away from its own fix. This
            // style inherits wordWrap from GUI.skin.label and is drawn into a fixed 12f*scale rect with a
            // middle anchor - so "DEBT-TO-GDP" wrapped to two lines and both lost their tops and bottoms.
            // The value field below has carried the fix since the "9,3" incident; this label never got it.
            // Tracks how far down the stack has actually reached, so the assert at the end measures what
            // was DRAWN rather than the running cursor - `y` overshoots the last element by its trailing
            // gap, and an assert that fires on padding is an assert that gets disabled.
            float contentBottom = y + TileLabelHeight * scale;

            MeasuredLabel(new Rect(x, y, innerWidth, TileLabelHeight * scale), label.ToUpperInvariant(),
                Sized(_label, PoliSimTheme.FontLabel, PoliSimTheme.TextMuted, scale));
            y += TileLabelBlock * scale;

            var valueStyle = Sized(_value, PoliSimTheme.FontStatHero, PoliSimTheme.TextPrimary, scale, TextAnchor.LowerLeft);
            float valueHeight = PoliSimTheme.FontStatHero * scale * TileValueLeading;

            // A stat tile must never be able to DISPLAY A DIFFERENT NUMBER than it was given. Two
            // properties conspired to let it: this style inherits wordWrap from GUI.skin.label, and the
            // anchor is a BOTTOM one - so a value too wide for the tile wrapped, and the bottom anchor
            // rendered only the LAST wrapped line. "29689,3" became "9,3": not obviously broken, just a
            // plausible-looking wrong number, which is the worst possible failure for a readout.
            // Percentages never showed it because they are short enough never to wrap.
            //
            // wordWrap off alone is not sufficient - it converts wrapping into clipping, which would show
            // "2968" instead, still a wrong number. So the font is also shrunk until the whole value fits.
            // Shrinking is the only option here that cannot misreport: the figure gets smaller, never
            // different.
            //
            // This now goes through MeasuredLabel, which is that same treatment generalised. The value
            // keeps its own higher font floor (MinStatValueFontSize) by pre-shrinking against it, because
            // a headline figure that shrinks as far as a label may would be unreadable at a glance.
            valueStyle.wordWrap = false;
            Vector2 valueSize = valueStyle.CalcSize(new GUIContent(value));
            float suffixWidth = string.IsNullOrEmpty(suffix)
                ? 0f
                : MeasuredWidth(suffix, Sized(_body, PoliSimTheme.FontBodySmall, PoliSimTheme.Neutral, scale), 5f * scale);
            float valueRoom = innerWidth - suffixWidth;
            if (valueSize.x > valueRoom && valueSize.x > 0f && valueRoom > 0f)
            {
                valueStyle.fontSize = Mathf.Max(MinStatValueFontSize, Mathf.FloorToInt(valueStyle.fontSize * (valueRoom / valueSize.x)));
            }

            // The suffix is reserved rather than ignored: it was previously drawn at x + valueSize.x with
            // the value fitted to the FULL inner width, so a wide value pushed its own unit off the tile.
            valueSize = MeasuredLabel(new Rect(x, y, innerWidth, valueHeight), value, valueStyle, suffixWidth);
            contentBottom = y + valueHeight;

            if (!string.IsNullOrEmpty(suffix))
            {
                MeasuredLabel(new Rect(x + valueSize.x + 5f * scale, y, innerWidth - valueSize.x - 5f * scale, valueHeight), suffix,
                    Sized(_body, PoliSimTheme.FontBodySmall, PoliSimTheme.Neutral, scale, TextAnchor.LowerLeft));
            }

            y += valueHeight + TileValueGap * scale;

            if (!string.IsNullOrEmpty(delta))
            {
                // ⚠ **THE DELTA IS NO LONGER A PILL, and that is a design ruling rather than a tidy-up.**
                //
                // Asked whether a chip sprite was wanted here, Design answered that it was not: *"on paper
                // a delta is inked text, not a lozenge."* A rounded lozenge is screen-UI furniture that
                // would sit on aged paper looking like it had wandered in from another program. So the
                // figure is simply inked, left-aligned where the pill used to start.
                //
                // ui_chip / ui_chip_outline still ship and are still used - by Badge, for the printed
                // markers (PRELIMINARY, REVISED, ACTION REQUIRED) where a chip is period-correct.
                //
                // B2 is untouched by this and must stay so: the ink is chosen by whether the change is
                // GOOD, never by whether the number rose. `deltaIsGood` is the caller's judgment and this
                // only renders it.
                Color tone = deltaIsGood ? PoliSimTheme.Good : PoliSimTheme.Bad;
                var deltaStyle = Sized(_body, PoliSimTheme.FontBodySmall, tone, scale, TextAnchor.MiddleLeft);
                deltaStyle.fontStyle = FontStyle.Bold;
                float w = deltaStyle.CalcSize(new GUIContent(delta)).x + 6f * scale;
                var pill = new Rect(x, y, w, TileDeltaHeight * scale);
                GUI.Label(pill, delta, deltaStyle);
                contentBottom = pill.yMax;

                if (!string.IsNullOrEmpty(subLabel))
                {
                    // Was given `innerWidth` while starting at the pill's right edge, so its rect ran off
                    // the tile by exactly the pill's width - the same class, in its quietest form.
                    MeasuredLabel(new Rect(pill.xMax + 7f * scale, y, x + innerWidth - pill.xMax - 7f * scale, TileDeltaHeight * scale), subLabel,
                        Sized(_body, PoliSimTheme.FontBodySmall - 1, PoliSimTheme.TextMuted, scale));
                }

                y += (TileDeltaHeight + TileDeltaGap) * scale;
            }

            if (barFraction >= 0f)
            {
                var bar = new Rect(x, y, innerWidth, PoliSimTheme.BarHeightSm * scale);
                ThresholdBar(bar, barFraction, thresholdFraction, PoliSimTheme.Accent(area));
                contentBottom = bar.yMax;
            }

            // ⚠ THE POINT OF THIS ASSERT. `StatTileHeight` above walks the same constants this method
            // walks, and the two agreeing is currently a property of nobody having broken it. Add an
            // element here, forget the accessor, and the tile overruns by exactly that element's height
            // with nothing failing - which is precisely how the delta came to be drawn onto the next
            // row's keyline. This makes the agreement checkable instead of remembered.
            UiContainmentGuard.CheckStackBottom("StatTile content stack", contentBottom, rect);
        }

        // --- 2. Threshold bar --------------------------------------------------------------

        /// <summary>
        /// Rounded track + fill + a white tick at the threshold. This is the game-wide convention
        /// for "value against a target": comfortable debt level, approval floor, capacity limits.
        /// Pass a negative threshold to draw a plain capacity bar.
        /// </summary>
        public static void ThresholdBar(Rect rect, float fraction, float thresholdFraction, Color fill)
        {
            float radius = rect.height * 0.5f;
            PoliSimTheme.RoundedBox(rect, PoliSimTheme.BarTrack, radius);

            fraction = Mathf.Clamp01(fraction);
            if (fraction > 0f)
            {
                float w = Mathf.Max(rect.height, rect.width * fraction);
                PoliSimTheme.RoundedBox(new Rect(rect.x, rect.y, w, rect.height), fill, radius);
            }

            if (thresholdFraction >= 0f)
            {
                float markerX = rect.x + rect.width * Mathf.Clamp01(thresholdFraction);
                PoliSimTheme.Rule(new Rect(markerX - 1f, rect.y - 2f, 2f, rect.height + 4f), PoliSimTheme.ThresholdMarker);
            }
        }

        // --- 3. Legislative support bar ----------------------------------------------------

        /// <summary>
        /// Seats-for versus the majority line, with the status word derived from the margin — the
        /// one control used by the budget summary, every standalone bill and any pending vote.
        /// </summary>
        public static void SupportBar(Rect rect, int seatsFor, int totalSeats, int majority, float scale, bool showScale = true)
        {
            EnsureStyles(scale);

            int margin = seatsFor - majority;
            Color tone = margin >= 5 ? PoliSimTheme.Good : margin >= -8 ? PoliSimTheme.Caution : PoliSimTheme.Bad;
            string status = margin >= 5 ? "PASSING" : margin >= 0 ? "RAZOR THIN" : Mathf.Abs(margin) + " SHORT";
            float percent = totalSeats > 0 ? seatsFor / (float)totalSeats : 0f;

            float y = rect.y;
            GUI.Label(new Rect(rect.x, y, rect.width, 14f * scale), "LEGISLATIVE SUPPORT",
                Sized(_label, PoliSimTheme.FontLabel, PoliSimTheme.TextMuted, scale));

            var statusStyle = Sized(_label, PoliSimTheme.FontLabel + 1, tone, scale, TextAnchor.MiddleCenter);
            float statusWidth = statusStyle.CalcSize(new GUIContent(status)).x + 18f * scale;
            var statusPill = new Rect(rect.xMax - statusWidth, y - 2f * scale, statusWidth, 18f * scale);
            PoliSimTheme.RoundedBox(statusPill, PoliSimTheme.Tint(tone, 0.14f), 9f * scale);
            GUI.Label(statusPill, status, statusStyle);

            y += 22f * scale;
            GUI.Label(new Rect(rect.x, y, rect.width, 36f * scale), Mathf.RoundToInt(percent * 100f) + "%",
                Sized(_value, PoliSimTheme.FontStatLarge, tone, scale, TextAnchor.LowerLeft));
            GUI.Label(new Rect(rect.x, y, rect.width, 36f * scale), seatsFor + " / " + majority + " SEATS",
                Sized(_mono, PoliSimTheme.FontBodySmall, PoliSimTheme.Neutral, scale, TextAnchor.LowerRight));

            y += 44f * scale;
            ThresholdBar(new Rect(rect.x, y, rect.width, PoliSimTheme.BarHeightLg * scale), percent,
                totalSeats > 0 ? majority / (float)totalSeats : 0.5f, tone);

            if (!showScale)
            {
                return;
            }

            y += 18f * scale;
            var scaleStyle = Sized(_mono, PoliSimTheme.FontMicro, PoliSimTheme.TextMuted, scale);
            GUI.Label(new Rect(rect.x, y, rect.width, 14f * scale), "0", scaleStyle);
            GUI.Label(new Rect(rect.x, y, rect.width, 14f * scale), "MAJORITY " + majority,
                Sized(_mono, PoliSimTheme.FontMicro, PoliSimTheme.TextMuted, scale, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(rect.x, y, rect.width, 14f * scale), totalSeats.ToString(),
                Sized(_mono, PoliSimTheme.FontMicro, PoliSimTheme.TextMuted, scale, TextAnchor.MiddleRight));
        }

        // --- 4. Standing / draft pair -------------------------------------------------------

        /// <summary>
        /// The budget line-item readout: the enacted value, an arrow, the draft value, and a signed
        /// delta pill when they differ. Draft text turns amber the moment it diverges from standing,
        /// which is the only cue that a change is pending a vote — so it is never optional.
        /// </summary>
        public static void StandingDraftPair(Rect rect, string standingText, string draftText, float delta, string unitSuffix, float scale)
        {
            EnsureStyles(scale);

            bool changed = Mathf.Abs(delta) > 0.0001f;
            Color draftColor = changed ? PoliSimTheme.Draft : PoliSimTheme.TextPrimary;

            float half = rect.height * 0.5f;
            GUI.Label(new Rect(rect.x, rect.y, 110f * scale, half), "STANDING",
                Sized(_label, PoliSimTheme.FontLabel, PoliSimTheme.TextMuted, scale, TextAnchor.LowerRight));
            GUI.Label(new Rect(rect.x, rect.y + half, 110f * scale, half), standingText,
                Sized(_mono, PoliSimTheme.FontSubtitle, PoliSimTheme.Neutral, scale, TextAnchor.UpperRight));

            float arrowX = rect.x + 120f * scale;
            PoliSimTheme.Rule(new Rect(arrowX, rect.y + half - 1f, 12f * scale, 2f), PoliSimTheme.Tint(PoliSimTheme.Neutral, 0.6f));

            float draftX = arrowX + 22f * scale;
            GUI.Label(new Rect(draftX, rect.y, 120f * scale, half), "DRAFT",
                Sized(_label, PoliSimTheme.FontLabel, changed ? PoliSimTheme.Draft : PoliSimTheme.TextMuted, scale, TextAnchor.LowerRight));
            GUI.Label(new Rect(draftX, rect.y + half, 120f * scale, half), draftText,
                Sized(_mono, PoliSimTheme.FontTitle, draftColor, scale, TextAnchor.UpperRight));

            if (!changed)
            {
                return;
            }

            string deltaText = (delta > 0f ? "+" : "−") + Mathf.Abs(delta).ToString("0.0") + unitSuffix;
            var deltaStyle = Sized(_mono, PoliSimTheme.FontBodySmall - 2, PoliSimTheme.Draft, scale, TextAnchor.MiddleCenter);
            float w = deltaStyle.CalcSize(new GUIContent(deltaText)).x + 16f * scale;
            var pill = new Rect(draftX + 130f * scale, rect.y + half - 9f * scale, w, 18f * scale);
            PoliSimTheme.RoundedBox(pill, PoliSimTheme.Tint(PoliSimTheme.Draft, 0.14f), 9f * scale);
            GUI.Label(pill, deltaText, deltaStyle);
        }

        /// <summary>Draft track: the standing value as a grey underlay behind the coloured draft fill.</summary>
        public static void DraftTrack(Rect rect, float standingFraction, float draftFraction, UiPalette.SystemArea area)
        {
            float radius = rect.height * 0.5f;
            PoliSimTheme.RoundedBox(rect, PoliSimTheme.BarTrack, radius);
            PoliSimTheme.RoundedBox(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(standingFraction), rect.height),
                new Color(1f, 1f, 1f, 0.16f), radius);
            PoliSimTheme.RoundedBox(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(draftFraction), rect.height),
                PoliSimTheme.Tint(PoliSimTheme.Accent(area), 0.9f), radius);
        }

        // --- 5. Decision card chrome ---------------------------------------------------------

        public enum Urgency
        {
            Scheduled,
            Soon,
            Urgent
        }

        /// <summary>
        /// Draws the card shell (fill, urgency-tinted border, category spine, corner wash) and the
        /// badge row, and returns the content rect the caller fills with title/body/facts/actions.
        /// </summary>
        public static Rect DecisionCard(Rect rect, string kind, string urgencyText, Urgency urgency, string deadline, UiPalette.SystemArea area, float scale)
        {
            EnsureStyles(scale);

            Color urgencyColor = urgency == Urgency.Urgent ? PoliSimTheme.Bad
                : urgency == Urgency.Soon ? PoliSimTheme.Caution
                : PoliSimTheme.Accent(area);

            Color border = urgency == Urgency.Scheduled ? PoliSimTheme.Hairline : PoliSimTheme.Tint(urgencyColor, 0.32f);
            PoliSimTheme.RoundedCard(rect, PoliSimTheme.Card, border, PoliSimTheme.RadiusPanel * scale);
            PoliSimTheme.LeftSpine(rect, area, 4f * scale);

            float padX = 22f * scale;
            float padY = 20f * scale;
            var badgeRow = new Rect(rect.x + padX, rect.y + padY, rect.width - padX * 2f, 20f * scale);

            float x = badgeRow.x;
            x += Badge(new Rect(x, badgeRow.y, 0f, badgeRow.height), kind, PoliSimTheme.Accent(area), scale) + 8f * scale;
            Badge(new Rect(x, badgeRow.y, 0f, badgeRow.height), urgencyText, urgencyColor, scale);

            GUI.Label(badgeRow, deadline, Sized(_mono, PoliSimTheme.FontMicro + 1, PoliSimTheme.TextMuted, scale, TextAnchor.MiddleRight));

            return new Rect(rect.x + padX, badgeRow.yMax + 12f * scale, rect.width - padX * 2f, rect.yMax - badgeRow.yMax - padY - 12f * scale);
        }

        /// <summary>Auto-width caps badge. Returns the width drawn so badges can be laid out in a row.</summary>
        public static float Badge(Rect rect, string text, Color tone, float scale)
        {
            EnsureStyles(scale);

            // v2.0: a PRINTED CHIP - square-cornered, r=2px - not a rounded pill. This is the call site
            // ui_chip was made for (REVISED, PRELIMINARY, ACTION REQUIRED, N/A); the signed delta on a
            // stat tile deliberately is not, and is inked text instead. See StatTile.
            //
            // ui_chip is white-on-alpha, so it takes the tone directly. Two variants ship: solid for a
            // stated fact, outline for a qualified one. Solid is the default here because Badge's existing
            // callers are all statements rather than qualifications; the outline is available for the
            // published/preliminary distinction (B6) when that gets its own pass.
            var style = Sized(_label, PoliSimTheme.FontLabel, tone, scale, TextAnchor.MiddleCenter);
            float w = style.CalcSize(new GUIContent(text)).x + 20f * scale;
            var box = new Rect(rect.x, rect.y, w, rect.height);

            Texture2D chip = IconLibrary.GetChrome("ui_chip");
            if (chip != null)
            {
                GUI.DrawTexture(box, chip, ScaleMode.StretchToFill, true, 0f, PoliSimTheme.Tint(tone, 0.85f), Vector4.zero, Vector4.zero);
            }
            else
            {
                PoliSimTheme.RoundedBox(box, PoliSimTheme.Tint(tone, 0.14f), 9f * scale);
            }

            style.normal.textColor = PoliSimTheme.Hex(0xF2EADB);
            GUI.Label(box, text, style);
            return w;
        }

        // --- 6. Portrait placeholder ----------------------------------------------------------

        /// <summary>
        /// The abstract minister portrait: a hue ring, a dark disc, and a head circle plus a
        /// top-rounded shoulder rectangle in the portfolio hue. Three procedural shapes, no art
        /// asset — and the ring is the only thing that identifies the portfolio, so keep it.
        /// </summary>
        public static void Portrait(Rect rect, UiPalette.SystemArea area, float scale)
        {
            float size = Mathf.Min(rect.width, rect.height);
            var disc = new Rect(rect.x, rect.y, size, size);
            Color hue = PoliSimTheme.Accent(area);

            PoliSimTheme.RoundedBox(disc, hue, size * 0.5f);

            float ring = 2.5f * scale;
            var inner = new Rect(disc.x + ring, disc.y + ring, size - ring * 2f, size - ring * 2f);
            PoliSimTheme.RoundedBox(inner, PoliSimTheme.Hex(0x0E1218), inner.width * 0.5f);

            // Head and shoulders are drawn inside a GUI group so the disc clips them.
            GUI.BeginGroup(inner);
            float u = inner.width / 53f; // reference geometry is a 53px inner disc
            var head = new Rect(inner.width * 0.5f - 7f * u, 8.5f * u, 14f * u, 14f * u);
            PoliSimTheme.RoundedBox(head, hue, head.width * 0.5f);

            var shoulders = new Rect(inner.width * 0.5f - 14f * u, 27.5f * u, 28f * u, 26f * u);
            GUI.DrawTexture(shoulders, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, hue,
                Vector4.zero, new Vector4(14f * u, 14f * u, 0f, 0f));
            GUI.EndGroup();
        }
    }
}
