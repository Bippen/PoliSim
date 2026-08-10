using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// Single source of truth for GameController's visual language (Phase 3 of the UI revamp):
    /// one limited color palette (a distinct hue per system area) plus one firm, game-wide
    /// convention for signed change - green for a positive-direction change, red for negative -
    /// used everywhere a delta is shown (dashboard, the live policy preview, per-tab readouts, and
    /// GraphRenderer's own title-row change summary, which reads its colors from here too rather
    /// than keeping its own separate copy). Also builds the button styles (real hover/pressed
    /// states, action-type color coding) GameController uses for every Implement/Remove/neutral/tab
    /// button, so a new button anywhere in the UI is one line, not a hand-rolled GUIStyle.
    /// </summary>
    public static class UiPalette
    {
        public enum SystemArea
        {
            Neutral,
            Fiscal,
            Trade,
            Political,
            Welfare,
            Labor,
            CrimeJustice,
            Sectors,
            Infrastructure,
            SovereignWealth,
            /// <summary>The world map tab (Phase 5) - a cross-cutting overview, not owned by any one policy area, so it gets its own distinct hue rather than reusing Neutral or borrowing another area's.</summary>
            Global
        }

        public enum ButtonKind
        {
            /// <summary>Generic action button with no positive/negative connotation (e.g. "Set Override", "Appoint").</summary>
            Neutral,
            /// <summary>Adds/enables something (e.g. "Implement", "Create Fund").</summary>
            Implement,
            /// <summary>Removes/disables something (e.g. "Remove", "Dissolve Fund", "Reset to Default").</summary>
            Remove,
            /// <summary>An unselected right-column tab, tinted by its own SystemArea.</summary>
            Tab,
            /// <summary>The currently-selected right-column tab, tinted brighter than Tab.</summary>
            TabSelected,
            /// <summary>The single most important action on screen (Advance Turn).</summary>
            Primary
        }

        // --- Signed-change convention: the ONE green/red pair every delta in the UI uses. ---
        //
        // v2.0: the aged inks. Screen green and screen red do not survive being printed on paper, and
        // these still satisfy the behaviour that matters (B2) — a delta is coloured by whether the change
        // is GOOD, never by whether the number rose. See GetDeltaColor.
        public static readonly Color PositiveChangeColor = PoliSimTheme.Hex(0x3E8A5F);
        public static readonly Color NegativeChangeColor = PoliSimTheme.Hex(0x9C4238);
        public static readonly Color NeutralChangeColor = PoliSimTheme.Hex(0x5D564A);

        /// <summary>
        /// The de-emphasised icon tint - white at 60% alpha, for an icon that is present but not the
        /// focus (an unselected nav tab, a stat chip's category glyph).
        ///
        /// **White-with-alpha rather than a grey, deliberately.** Every chrome and icon sprite in this
        /// project is authored pure white with all of its depth carried in the alpha channel, so tinting
        /// toward grey would flatten that depth instead of dimming it. The value matches what
        /// `DrawConsolidatedTabButton` had inline for unselected tabs - this promotes that literal to a
        /// named member rather than introducing a second, slightly-different muted shade.
        ///
        /// Not to be confused with <see cref="NeutralChangeColor"/>, which means "this delta has no
        /// direction" and belongs to the signed-change convention above. These look similar and mean
        /// entirely different things.
        /// </summary>
        /// <remarks>
        /// v2.0: was white @60%, which was correct against a dark ground and is INVISIBLE on paper. It is
        /// now faint ink at the same alpha. The white-on-alpha authoring note above still holds and is the
        /// reason this is a tint at all — only the ground it sits on changed.
        /// </remarks>
        public static readonly Color MutedIconTint = new Color(0x5D / 255f, 0x56 / 255f, 0x4A / 255f, 0.75f);

        /// <summary>Below this absolute value/percent, a change reads as "no real change" (neutral gray) rather than an arbitrarily-signed green/red on essentially noise.</summary>
        private const float NeutralChangeThreshold = 0.05f;

        /// <summary>
        /// The color for a signed change, honoring which direction is actually good for THIS stat -
        /// a rising line means opposite things for GDP (higherIsBetter: true) versus Unemployment
        /// (higherIsBetter: false). The single place this judgment call is made, reused by every
        /// caller instead of each one re-deciding it.
        /// </summary>
        public static Color GetDeltaColor(float delta, bool higherIsBetter)
        {
            if (Mathf.Abs(delta) < NeutralChangeThreshold)
            {
                return NeutralChangeColor;
            }

            bool isPositiveChange = delta > 0f;
            bool isGoodChange = higherIsBetter ? isPositiveChange : !isPositiveChange;
            return isGoodChange ? PositiveChangeColor : NegativeChangeColor;
        }

        // --- System-area hues: one distinct family per part of the game, spread around the wheel so no two are confusable, and none of them overlap the pure green/red reserved for the change convention above. ---
        private static readonly Dictionary<SystemArea, Color> AreaColors = new Dictionary<SystemArea, Color>
        {
            // v2.0 AGED INKS (2026-08-03), from polisim_palette.json. Derived by Design in oklch:
            // chroma −45%, lightness normalised to 0.48–0.62, 3–6° warm drift. **All eleven survive** —
            // Elias's decision, and the evidence for it is that these same values key CHARTS (the
            // hemicycle legend, pie wedges, the political compass). Reducing the palette would not
            // simplify anything; it would break a legend. This is the ON-PAPER set; PoliSimTheme carries
            // the same eleven at desk weight for the one surface still on a dark ground.
            { SystemArea.Neutral, PoliSimTheme.Hex(0x6D7480) },
            { SystemArea.Fiscal, PoliSimTheme.Hex(0x35619E) },          // blue - tax & spending
            { SystemArea.Trade, PoliSimTheme.Hex(0x23867B) },           // teal - tariffs & trade partners
            { SystemArea.Political, PoliSimTheme.Hex(0xA8842E) },       // ochre - approval, elections, Federal Reserve
            { SystemArea.Welfare, PoliSimTheme.Hex(0xA84E7B) },         // magenta - welfare programs
            { SystemArea.Labor, PoliSimTheme.Hex(0xB5622F) },           // orange - labor market policy
            { SystemArea.CrimeJustice, PoliSimTheme.Hex(0x9C4238) },    // brick - crime & justice
            { SystemArea.Sectors, PoliSimTheme.Hex(0x62579F) },         // indigo - economic sectors
            { SystemArea.Infrastructure, PoliSimTheme.Hex(0x3E7480) },  // slate - infrastructure
            { SystemArea.SovereignWealth, PoliSimTheme.Hex(0x85643A) }, // bronze - sovereign wealth fund
            { SystemArea.Global, PoliSimTheme.Hex(0x5C87A8) }           // sky blue - world map overview
        };

        public static Color GetAreaColor(SystemArea area) => AreaColors[area];

        /// <summary>
        /// Per-country identity color, single source of truth (moved here from MapRenderer so the
        /// country-selection screen and the World Map tab can never drift apart on which color means
        /// which country). USA reuses the Political hue - already established (the Federal Reserve/
        /// elections tab). The other five have no individual tab of their own to reuse, so each is
        /// assigned one of the remaining existing hues - an arbitrary but consistent pairing, not a
        /// pre-existing one, chosen when the World Map first shipped.
        /// </summary>
        private static readonly Dictionary<CountryId, SystemArea> CountryAreas = new Dictionary<CountryId, SystemArea>
        {
            { CountryId.USA, SystemArea.Political },
            { CountryId.Sweden, SystemArea.Trade },
            { CountryId.Germany, SystemArea.Welfare },
            { CountryId.France, SystemArea.Labor },
            { CountryId.Italy, SystemArea.Sectors },
            { CountryId.Poland, SystemArea.SovereignWealth },
        };

        public static SystemArea GetCountryArea(CountryId countryId) => CountryAreas[countryId];

        /// <summary>
        /// Master Sequence step 5e, Phase C: which system area a cabinet portfolio belongs to. Every
        /// cabinet surface previously colored itself flat `Political` regardless of portfolio (see
        /// GameController.DrawCabinetPortfolioPanel), which is fine when portfolios are only ever shown
        /// one-per-panel but reads as a single undifferentiated block once several pending cabinet
        /// decisions stack up together in the Decisions tab. Each portfolio maps to the area it actually
        /// governs, so two simultaneous decisions are told apart at a glance.
        /// </summary>
        public static SystemArea GetPortfolioArea(CabinetPortfolio portfolio)
        {
            switch (portfolio)
            {
                case CabinetPortfolio.FinanceTreasury: return SystemArea.Fiscal;
                case CabinetPortfolio.InteriorJustice: return SystemArea.CrimeJustice;
                case CabinetPortfolio.HealthSocialAffairs: return SystemArea.Welfare;
                default: return SystemArea.Political;
            }
        }
        public static Color GetCountryColor(CountryId countryId) => GetAreaColor(CountryAreas[countryId]);

        /// <summary>
        /// Political Systems Overhaul Part C: one distinct color per slice of an N-way categorical
        /// breakdown (sector employment shares, spending categories, tax revenue sources) - none of
        /// these have their own pre-assigned SystemArea/CountryId color the way a tab or a country
        /// does, and N varies per call site (4-20+), so a fixed lookup table isn't practical. Evenly
        /// spaced hues around the color wheel, golden-angle-offset (not a plain N-way even split) so
        /// adjacent indices land far apart in hue even when N is small - a plain even split of, say,
        /// 4 slices would put them at 0/90/180/270 degrees, which is fine, but the SAME formula at
        /// N=5 (0/72/144/216/288) starts looking visually similar to N=4's spacing; the golden angle
        /// avoids that ever regressing toward evenly-spaced-but-visually-clustered as index grows,
        /// without needing a hand-picked table sized to the largest N any call site might ever pass.
        /// </summary>
        /// <remarks>
        /// SUPERSEDED 2026-08-10. The golden-angle walk above answered "N varies per call site
        /// (4-20+), so a fixed table isn't practical", and it did answer it - but in saturated screen
        /// colour, which is the one thing the v2.0 palette exists to remove. Every other hue in the
        /// game was aged in that pass; these four charts were missed, and they are the largest colour
        /// areas on the Statistics tab. The answer to a long series is now a different chart, not a
        /// generated hue.
        /// </remarks>
        private static Color CategoricalHex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

        private static readonly Color[] CategoricalSeries =
        {
            CategoricalHex(0x9C5233), CategoricalHex(0x8A7A2C), CategoricalHex(0x5C7434), CategoricalHex(0x2F7458),
            CategoricalHex(0x2E6E7E), CategoricalHex(0x46608F), CategoricalHex(0x745394), CategoricalHex(0x95517A)
        };

        /// <summary>The hard cap. Eight is what can be cut aged and stay mutually distinguishable at a 12px legend swatch; a ninth cannot, and pretending otherwise is how a legend stops keying its chart.</summary>
        public const int MaxCategoricalSeries = 8;

        /// <summary>
        /// One aged ink per slice of a categorical breakdown, assigned in series order.
        ///
        /// EIGHT IS A HARD CAP, AND A NINTH INDEX THROWS. It deliberately does NOT wrap. A modulo
        /// would keep every existing call site compiling and running while category 0 and category 8
        /// printed in the same ink on the same chart - behaviour 9 (a legend swatch must be drawn in
        /// the colour of the element it explains) broken with nothing failing anywhere. That is the
        /// exact class of defect that had to be found by eye last time, and a silent wrap is how it
        /// would come back. A series that outgrows the cap is not a palette problem to absorb here; it
        /// is a signal that the chart form is wrong.
        ///
        /// WHAT TO DO INSTEAD OF CATCHING THIS: a series longer than eight renders as a ranked
        /// single-ink bar ledger in its owning area's ink - see RankedBarLedgerRenderer. One ink,
        /// sorted descending, every row carrying its own inline label, so there is no legend left to
        /// disagree with the chart. Spending (29 categories) and tax revenue (13 types) both went that
        /// way on 2026-08-10; sector employment (8) sits exactly at the cap and stays hue-keyed.
        /// </summary>
        public static Color GetCategoricalColor(int index)
        {
            if (index < 0 || index >= MaxCategoricalSeries)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(index), index,
                    $"The categorical palette holds exactly {MaxCategoricalSeries} aged inks and does not wrap. " +
                    "A series longer than that must render as a ranked single-ink bar ledger " +
                    "(RankedBarLedgerRenderer) rather than as a hue-keyed chart - see this method's doc comment.");
            }

            return CategoricalSeries[index];
        }

        private static readonly Dictionary<Color, Texture2D> SwatchCache = new Dictionary<Color, Texture2D>();

        /// <summary>A cached 2x2 solid-color texture for the given color - GUIStyle backgrounds need a Texture2D, not a raw Color, and this avoids allocating a new one every frame for the same color.</summary>
        private static Texture2D GetSolidTexture(Color color)
        {
            if (SwatchCache.TryGetValue(color, out Texture2D existing) && existing != null)
            {
                return existing;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color[4] { color, color, color, color };
            texture.SetPixels(pixels);
            texture.Apply(false);
            SwatchCache[color] = texture;
            return texture;
        }

        private static Color Lighten(Color c, float amount) => Color.Lerp(c, Color.white, amount);
        private static Color Darken(Color c, float amount) => Color.Lerp(c, Color.black, amount);

        /// <summary>The unfilled part of any bar. PoliSimTheme already carries the aged one; this private copy was left on the dark-dashboard value and is every bar in the game, so it read as a black gutter on paper.</summary>
        private static readonly Color BarTrackColor = PoliSimTheme.BarTrack;

        /// <summary>
        /// Proportionally-sized bar for breakdown/comparison data (Phase 4 of the UI revamp) - a
        /// dark track the full available width, with a colored fill sized to <paramref name="fraction"/>
        /// (already clamped to [0,1] by the caller's own normalization, e.g. value/maxValue or an
        /// already-0-1 weight) - reads better than a line graph for "how do these N things compare
        /// right now" data (spending categories, trade partner volumes, asset-class mix, per-asset
        /// condition), where a graph's trend-over-time framing doesn't apply.
        /// </summary>
        public static void DrawBar(float fraction, Color fillColor, float height = 14f)
        {
            fraction = Mathf.Clamp01(fraction);
            Rect rect = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rect, GetSolidTexture(BarTrackColor), ScaleMode.StretchToFill);

            if (fraction > 0f)
            {
                var fillRect = new Rect(rect.x, rect.y, rect.width * fraction, rect.height);
                GUI.DrawTexture(fillRect, GetSolidTexture(fillColor), ScaleMode.StretchToFill);
            }
        }

        private static readonly Color ThresholdMarkerColor = Color.white;

        /// <summary>
        /// Same track+fill as DrawBar, plus a thin vertical marker at <paramref name="thresholdFraction"/>
        /// - the election reveal screen's own "approval bar with a win/lose line" needs a reference
        /// point DrawBar alone can't show, and a dedicated marker overlay is simpler and more legible
        /// than trying to encode it as a second bar.
        /// </summary>
        public static void DrawBarWithThreshold(float fraction, float thresholdFraction, Color fillColor, float height = 14f)
        {
            fraction = Mathf.Clamp01(fraction);
            thresholdFraction = Mathf.Clamp01(thresholdFraction);
            Rect rect = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rect, GetSolidTexture(BarTrackColor), ScaleMode.StretchToFill);

            if (fraction > 0f)
            {
                var fillRect = new Rect(rect.x, rect.y, rect.width * fraction, rect.height);
                GUI.DrawTexture(fillRect, GetSolidTexture(fillColor), ScaleMode.StretchToFill);
            }

            float markerX = rect.x + rect.width * thresholdFraction;
            var markerRect = new Rect(markerX - 1f, rect.y, 2f, rect.height);
            GUI.DrawTexture(markerRect, GetSolidTexture(ThresholdMarkerColor), ScaleMode.StretchToFill);
        }

        /// <summary>
        /// Master Sequence step 5e, Phase B: draws a sprite icon tinted to <paramref name="tint"/> -
        /// the actual mechanism the Claude Design asset pack's own README specifies ("Authored white so
        /// a single texture serves every state - tint with PoliSimTheme.Accent(...) instead of shipping
        /// a coloured copy per hue"). `GUI.color` multiplies every subsequent draw call's own color
        /// (white pixels become exactly <paramref name="tint"/>, the icon's own alpha channel is left
        /// alone since IMGUI always respects source alpha), the same idiom `HemicycleRenderer` already
        /// uses for its own per-seat dot tinting - restored immediately after so this never leaks into
        /// whatever the caller draws next. A no-op (not a placeholder box) if <paramref name="icon"/>
        /// is null, so a not-yet-imported/misconfigured texture reference fails silently rather than
        /// drawing a wrong-looking fallback that could be mistaken for a working icon.
        /// </summary>
        public static void DrawTintedIcon(Rect rect, Texture2D icon, Color tint)
        {
            if (icon == null)
            {
                return;
            }

            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        // Keyed on the source texture REFERENCE rather than an instance id - Unity 6000.5 deprecated
        // Object.GetInstanceID(), and the reference is a perfectly good key here since IconLibrary
        // caches and returns the same Texture2D instance for a given sprite name.
        private static readonly Dictionary<(Texture2D Source, Color Tint), Texture2D> TintedChromeCache =
            new Dictionary<(Texture2D, Color), Texture2D>();

        // Unreadability is a property of the SOURCE sprite, not of any one tint, so it is tracked
        // separately rather than as a null entry in the cache above - whose lookup deliberately treats
        // null as "absent" so it also recovers from textures Unity destroyed across a domain reload.
        // Without this set, a failed read would be retried (and re-logged) on every frame of every tint.
        private static readonly HashSet<Texture2D> UnreadableChrome = new HashSet<Texture2D>();

        /// <summary>
        /// Master Sequence step 5e, chrome batch 1: multiplies one of the imported chrome sprites by a
        /// colour, producing a cached tinted copy.
        ///
        /// The sprites are authored pure white with ALL depth - gradient, bevel, pressed inset, edge
        /// highlight - carried in the alpha channel (verified on import: channel spread across the pack
        /// is 0). Multiplying therefore yields exactly the requested hue while preserving that depth,
        /// where the old `GetSolidTexture` could only produce a flat rectangle.
        ///
        /// Pre-tinting into a copy, rather than setting `GUI.backgroundColor` around each draw, is
        /// deliberate: it keeps <see cref="BuildButtonStyle"/>'s signature and behaviour identical, so
        /// none of the ~24 existing button call sites need to change. Rewriting them all to bracket
        /// each draw with a colour push/pop would be a far larger and more fragile edit for the same
        /// pixels. The cost is one 48x48 texture per (sprite, colour) pair - a few KB each, for a
        /// handful of pairs.
        ///
        /// Returns null if <paramref name="source"/> is null (sprite not imported), which callers treat
        /// as "fall back to the old solid-colour background" rather than drawing nothing.
        /// </summary>
        private static Texture2D GetTintedChrome(Texture2D source, Color tint)
        {
            if (source == null)
            {
                return null;
            }

            if (UnreadableChrome.Contains(source))
            {
                return null;
            }

            var key = (source, tint);
            if (TintedChromeCache.TryGetValue(key, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            // GetPixels throws unless the texture was imported with Read/Write enabled. That flag was set
            // by editing the .meta files, which only takes effect once Unity reimports them - so on a
            // machine where that reimport hasn't happened yet, this would throw INSIDE OnGUI and take the
            // entire UI down rather than degrading one button. Caught and cached as a miss so the fallback
            // path is used from then on, without re-throwing (and re-logging) every frame.
            // ⚠ **THIS CATCH WAS WRONG AND THE COMMENT ABOVE DESCRIBES A DEFENCE THAT DID NOT WORK.**
            //
            // It caught `UnityException`. Unity throws `ArgumentException` for a non-readable texture
            // ("texture data is either not readable, corrupted or does not exist"), and ArgumentException
            // does not derive from UnityException — so the guard written specifically to stop this from
            // taking the UI down let it through. It went unnoticed for as long as every chrome sprite
            // happened to be imported readable; the v2.0 pack arrived with `isReadable: 0` (copied from the
            // ICON meta template, which is correct for icons and wrong for chrome) and the whole interface
            // rendered as an empty desk.
            //
            // Catching Exception rather than a named type is deliberate here: this runs inside OnGUI,
            // where ANY escape blanks the entire frame, and the fallback is a complete, already-working
            // flat-colour path. There is no failure mode where re-throwing serves the player better.
            Color[] pixels;
            try
            {
                pixels = source.GetPixels();
            }
            catch (System.Exception)
            {
                Debug.LogWarning($"UiPalette: chrome sprite '{source.name}' is not readable, falling back to flat button backgrounds. Re-import it with Read/Write enabled to get the sprite chrome.");
                UnreadableChrome.Add(source);
                return null;
            }

            var tinted = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] *= tint;
            }

            tinted.SetPixels(pixels);
            tinted.Apply(false);
            TintedChromeCache[key] = tinted;
            return tinted;
        }

        /// <summary>Border insets for the 9-sliced chrome sprites, from the delivered pack's own README - buttons 10, panel 13, capsules 14 left/right. Kept here as named constants so the numbers live beside the code that applies them rather than only in a README.</summary>
        public const int ChromeButtonBorder = 10;
        public const int ChromePanelBorder = 13;
        public const int ChromeCapsuleBorder = 14;

        /// <summary>
        /// Builds a button style with the imported chrome sprite as its background per state
        /// (normal/hover/active) - Unity's IMGUI applies these automatically based on real mouse
        /// position/press state during Repaint, so this is genuine hover/pressed feedback, not just a
        /// static color. Clones <paramref name="baseStyle"/> first so font size/fixed height (already
        /// screen-scaled by the caller) carry over unchanged.
        ///
        /// Master Sequence step 5e, chrome batch 1: each state now uses its OWN sprite - a pressed
        /// button gets genuinely different geometry (inverted gradient, inner shadow) rather than just a
        /// darker flat fill. `style.border` is what makes 9-slicing work in IMGUI: the slice comes from
        /// the STYLE, never from the texture asset's own `spriteBorder` field, which IMGUI does not read.
        /// If the sprites are missing for any reason the method silently falls back to the previous
        /// solid-color rectangles, so a broken import degrades to the old look rather than to invisible
        /// buttons.
        /// </summary>
        public static GUIStyle BuildButtonStyle(GUIStyle baseStyle, ButtonKind kind, SystemArea area = SystemArea.Neutral)
        {
            Color baseColor = GetButtonBaseColor(kind, area);
            var style = new GUIStyle(baseStyle);

            // v2.0: brass for the emphatic kinds, paper for the rest, and the pack's own disabled plate.
            // These sprites are REAL-COLOUR paper furniture rather than white-on-alpha, so they are drawn
            // untinted for Neutral/TabUnselected and only lightly tinted where an area hue must read
            // through - the opposite of the old chrome pack, which was tintable by construction.
            bool emphatic = kind == ButtonKind.Primary || kind == ButtonKind.TabSelected || kind == ButtonKind.Implement || kind == ButtonKind.Remove;
            Texture2D normalSprite = IconLibrary.GetChrome(emphatic ? "ui_btn_brass" : "ui_btn_paper");
            Texture2D pressedSprite = normalSprite;
            Texture2D hoverSprite = normalSprite;

            style.normal.background = GetTintedChrome(normalSprite, baseColor) ?? GetSolidTexture(baseColor);
            style.hover.background = GetTintedChrome(hoverSprite, Lighten(baseColor, 0.12f)) ?? GetSolidTexture(Lighten(baseColor, 0.2f));
            style.active.background = GetTintedChrome(pressedSprite, Darken(baseColor, 0.14f)) ?? GetSolidTexture(Darken(baseColor, 0.25f));
            style.focused.background = style.normal.background;

            if (normalSprite != null)
            {
                // The pack's buttons are 128x64 @2x with a 20/20/20/28 inset - the bottom edge is deeper
                // because the drop shadow is baked into it. GUIStyle.border is at @1x, so these are half
                // the manifest's figures. IMGUI reads the slice from the STYLE and never from the texture
                // asset's own spriteBorder field, which is why this cannot be set in the importer.
                style.border = new RectOffset(10, 10, 10, 14);
            }

            // Ink on paper, cream on brass. The old value was white for everything, which was right on a
            // dark ground and unreadable on a paper button.
            Color textColor = emphatic ? PoliSimTheme.Hex(0xF2EADB) : PoliSimTheme.TextPrimary;
            style.normal.textColor = textColor;
            style.hover.textColor = textColor;
            style.active.textColor = textColor;
            style.focused.textColor = textColor;
            style.fontStyle = kind == ButtonKind.TabSelected || kind == ButtonKind.Primary ? FontStyle.Bold : baseStyle.fontStyle;

            return style;
        }

        /// <summary>
        /// Master Sequence step 5e, Phase C batch 3: a bar that fills OUTWARD FROM THE CENTRE - right
        /// and green when <paramref name="value"/> is positive, left and red when negative - with the
        /// centre line marking the decision threshold.
        ///
        /// This shape was chosen because the thing it draws (ParliamentSystem.GetSeatWeightedAlignment)
        /// has a zero threshold rather than a majority one. A conventional left-to-right progress bar
        /// with a marker part-way along would imply "fill this much to pass", which is not how the vote
        /// model works. <paramref name="displayRange"/> is a presentation choice only - the value is
        /// clamped into it purely so a lopsided parliament still renders a readable bar - so callers
        /// should NOT print a number derived from it and imply precision the model doesn't claim.
        /// </summary>
        public static void DrawDivergingBar(Rect rect, float value, float displayRange)
        {
            GUI.DrawTexture(rect, GetSolidTexture(BarTrackColor), ScaleMode.StretchToFill);

            float centreX = rect.x + rect.width * 0.5f;
            float fraction = displayRange > 0f ? Mathf.Clamp(value / displayRange, -1f, 1f) : 0f;
            float halfWidth = rect.width * 0.5f * Mathf.Abs(fraction);

            if (halfWidth > 0.5f)
            {
                var fillRect = fraction >= 0f
                    ? new Rect(centreX, rect.y, halfWidth, rect.height)
                    : new Rect(centreX - halfWidth, rect.y, halfWidth, rect.height);
                GUI.DrawTexture(fillRect, GetSolidTexture(fraction >= 0f ? PositiveChangeColor : NegativeChangeColor), ScaleMode.StretchToFill);
            }

            var centreLine = new Rect(centreX - 1f, rect.y - 2f, 2f, rect.height + 4f);
            GUI.DrawTexture(centreLine, GetSolidTexture(ThresholdMarkerColor), ScaleMode.StretchToFill);
        }

        private static readonly Dictionary<(Color Fill, int Radius), Texture2D> RoundedCache =
            new Dictionary<(Color, int), Texture2D>();

        /// <summary>
        /// A cached rounded-corner texture, built once per (color, radius) and 9-sliced by the styles
        /// below. `PoliSimTheme.RoundedBox` already draws rounded rects, but only into an explicit Rect
        /// the caller has already measured - that works for fixed-layout widgets like StatTile and is
        /// no help at all for the large majority of this UI, which is GUILayout flow whose height isn't
        /// known until after its content has been laid out. A 9-sliced STYLE background is the piece
        /// that was missing: it lets ordinary `GUILayout.BeginVertical(cardStyle)` content sit in a
        /// proper card without any of it being rewritten into manual Rect math (which is exactly the
        /// kind of rewrite that produced two real layout bugs in 5b).
        /// Corner alpha is computed from true distance-to-corner-centre, so edges are smooth rather
        /// than stair-stepped; every non-corner pixel resolves to distance 0 and stays fully opaque.
        /// </summary>
        private static Texture2D GetRoundedTexture(Color fill, int radius)
        {
            if (RoundedCache.TryGetValue((fill, radius), out Texture2D existing) && existing != null)
            {
                return existing;
            }

            int size = radius * 2 + 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cornerX = x < radius ? radius - 0.5f : (x >= size - radius ? size - radius - 0.5f : x);
                    float cornerY = y < radius ? radius - 0.5f : (y >= size - radius ? size - radius - 0.5f : y);
                    float dx = x - cornerX;
                    float dy = y - cornerY;
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    pixels[y * size + x] = new Color(fill.r, fill.g, fill.b, fill.a * alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
            RoundedCache[(fill, radius)] = texture;
            return texture;
        }

        private static readonly Dictionary<(Color Fill, int Radius, int Padding, int Spine), GUIStyle> CardStyleCache =
            new Dictionary<(Color, int, int, int), GUIStyle>();

        /// <summary>
        /// Master Sequence step 5e, Phase C: a rounded card style for wrapping existing GUILayout
        /// content - `GUILayout.BeginVertical(UiPalette.BuildCardStyle(...))`. <paramref name="spineWidth"/>
        /// only reserves extra LEFT padding; the spine itself is drawn by <see cref="DrawCardSpine"/>
        /// once the card's real rect is known. Reserving it in the style rather than drawing over the
        /// content afterwards is the same discipline the tab-bar icons had to learn the hard way (see
        /// GameController.DrawConsolidatedTabButton): art laid on top of a control that doesn't know
        /// it's there will eventually collide with it.
        /// Cached per distinct combination - these are rebuilt every frame by callers, so allocating a
        /// GUIStyle and a texture per call would be a per-frame leak, not just a cost.
        /// </summary>
        public static GUIStyle BuildCardStyle(Color fill, int cornerRadius = 8, int padding = 12, int spineWidth = 0)
        {
            var key = (fill, cornerRadius, padding, spineWidth);
            if (CardStyleCache.TryGetValue(key, out GUIStyle cached) && cached != null && cached.normal.background != null)
            {
                return cached;
            }

            // v2.0: the paper sheet. ui_panel_paper is 512x512 @2x with a 44/44/44/56 inset - the bottom
            // is deeper because the drop shadow is baked into that edge, so an even border would stretch
            // the shadow up the sides. Halved here because GUIStyle.border is @1x.
            //
            // Falls back to the procedural rounded rect when the sprite is missing, per IconLibrary's
            // standing null contract: a failed import degrades the look rather than producing an
            // invisible card.
            Texture2D paper = IconLibrary.GetChrome("ui_panel_paper");
            var style = new GUIStyle
            {
                border = paper != null
                    ? new RectOffset(22, 22, 22, 28)
                    : new RectOffset(cornerRadius + 1, cornerRadius + 1, cornerRadius + 1, cornerRadius + 1),
                padding = new RectOffset(padding + spineWidth, padding, padding, padding),
                margin = new RectOffset(0, 0, 0, 0)
            };
            style.normal.background = GetTintedChrome(paper, fill) ?? GetRoundedTexture(fill, cornerRadius);

            CardStyleCache[key] = style;
            return style;
        }

        /// <summary>
        /// The area-colored accent bar down a card's left edge - the single strongest "which system does
        /// this belong to" cue in the design language, and the reason <see cref="BuildCardStyle"/> takes
        /// a spineWidth at all. Call with the rect from GUILayoutUtility.GetLastRect() immediately after
        /// the card's EndVertical. Drawn inside the padding the style already reserved, so it can never
        /// overlap the card's own content.
        /// </summary>
        public static void DrawCardSpine(Rect cardRect, SystemArea area, float width = 4f, float inset = 5f)
        {
            var spineRect = new Rect(cardRect.x + inset, cardRect.y + inset, width, cardRect.height - inset * 2f);
            GUI.DrawTexture(spineRect, GetRoundedTexture(GetAreaColor(area), 2), ScaleMode.StretchToFill, true);
        }

        private static Color GetButtonBaseColor(ButtonKind kind, SystemArea area)
        {
            switch (kind)
            {
                case ButtonKind.Implement:
                    return Darken(PositiveChangeColor, 0.15f);
                case ButtonKind.Remove:
                    return Darken(NegativeChangeColor, 0.1f);
                case ButtonKind.Primary:
                    return GetAreaColor(SystemArea.Political);
                case ButtonKind.Tab:
                    // v2.0: an unselected folder tab is off-stock, NOT a darkened hue. The hue survives on
                    // its spine (ui_tab_spine), which is where per-area identity lives now - darkening the
                    // whole tab was a dark-theme device and turns paper muddy.
                    return PoliSimTheme.StockOff;
                case ButtonKind.TabSelected:
                    return GetAreaColor(area);
                case ButtonKind.Neutral:
                default:
                    return PoliSimTheme.Card;
            }
        }
    }
}
