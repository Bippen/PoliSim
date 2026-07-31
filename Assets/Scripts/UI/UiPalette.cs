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
        public static readonly Color PositiveChangeColor = new Color(0.35f, 0.85f, 0.45f, 1f);
        public static readonly Color NegativeChangeColor = new Color(0.90f, 0.35f, 0.35f, 1f);
        public static readonly Color NeutralChangeColor = new Color(0.75f, 0.75f, 0.75f, 1f);

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
            { SystemArea.Neutral, new Color(0.55f, 0.55f, 0.55f) },
            { SystemArea.Fiscal, new Color(0.29f, 0.50f, 0.84f) },        // blue - tax & spending
            { SystemArea.Trade, new Color(0.18f, 0.75f, 0.75f) },         // teal - tariffs & trade partners
            { SystemArea.Political, new Color(0.84f, 0.65f, 0.18f) },     // gold - approval, elections, Federal Reserve
            { SystemArea.Welfare, new Color(0.84f, 0.29f, 0.62f) },       // magenta - welfare programs
            { SystemArea.Labor, new Color(0.84f, 0.48f, 0.18f) },         // orange - labor market policy
            { SystemArea.CrimeJustice, new Color(0.65f, 0.29f, 0.29f) },  // brick - crime & justice
            { SystemArea.Sectors, new Color(0.48f, 0.29f, 0.84f) },       // indigo - economic sectors
            { SystemArea.Infrastructure, new Color(0.29f, 0.56f, 0.65f) },// slate - infrastructure
            { SystemArea.SovereignWealth, new Color(0.65f, 0.56f, 0.18f) },// bronze - sovereign wealth fund
            { SystemArea.Global, new Color(0.42f, 0.68f, 0.88f) }         // sky blue - world map overview
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
        public static Color GetCategoricalColor(int index)
        {
            const float goldenAngle = 137.508f;
            float hue = (index * goldenAngle % 360f) / 360f;
            return Color.HSVToRGB(hue, 0.65f, 0.9f);
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

        private static readonly Color BarTrackColor = new Color(0.22f, 0.22f, 0.24f);

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

        /// <summary>
        /// Builds a button style with a solid-color background per state (normal/hover/active) -
        /// Unity's IMGUI applies these automatically based on real mouse position/press state during
        /// Repaint, so this is genuine hover/pressed feedback, not just a static color. Clones
        /// <paramref name="baseStyle"/> first so font size/fixed height (already screen-scaled by the
        /// caller) carry over unchanged - only the color identity changes here.
        /// </summary>
        public static GUIStyle BuildButtonStyle(GUIStyle baseStyle, ButtonKind kind, SystemArea area = SystemArea.Neutral)
        {
            Color baseColor = GetButtonBaseColor(kind, area);
            var style = new GUIStyle(baseStyle);

            style.normal.background = GetSolidTexture(baseColor);
            style.hover.background = GetSolidTexture(Lighten(baseColor, 0.2f));
            style.active.background = GetSolidTexture(Darken(baseColor, 0.25f));
            style.focused.background = style.normal.background;

            Color textColor = kind == ButtonKind.TabSelected ? Color.white : Lighten(Color.white, 0f);
            style.normal.textColor = textColor;
            style.hover.textColor = textColor;
            style.active.textColor = textColor;
            style.focused.textColor = textColor;
            style.fontStyle = kind == ButtonKind.TabSelected || kind == ButtonKind.Primary ? FontStyle.Bold : baseStyle.fontStyle;

            return style;
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

            var style = new GUIStyle
            {
                border = new RectOffset(cornerRadius + 1, cornerRadius + 1, cornerRadius + 1, cornerRadius + 1),
                padding = new RectOffset(padding + spineWidth, padding, padding, padding),
                margin = new RectOffset(0, 0, 0, 0)
            };
            style.normal.background = GetRoundedTexture(fill, cornerRadius);

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
                    return Darken(GetAreaColor(area), 0.35f);
                case ButtonKind.TabSelected:
                    return GetAreaColor(area);
                case ButtonKind.Neutral:
                default:
                    return new Color(0.32f, 0.32f, 0.34f);
            }
        }
    }
}
