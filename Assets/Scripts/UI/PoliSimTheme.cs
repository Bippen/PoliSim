using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// Design tokens for the R2 GUI (dark, rounded-card visual language): surfaces, text ramp,
    /// semantic state colors, radii, spacing and the type scale — plus the low-level primitives
    /// every widget draws with (rounded box, pill, hairline, tinted category wash). Area hues stay
    /// owned by <see cref="UiPalette"/>; this class only re-tunes them for the dark surfaces and
    /// never introduces a second hue system.
    /// </summary>
    public static class PoliSimTheme
    {
        // --- Surfaces (v2.0 "ministry fresh print"). Values from polisim_palette.json. ---
        //
        // ⚠ THE THEME INVERTED. Until 2026-08-03 this was a dark stack and text was light-on-dark; it is
        // now INK ON PAPER, and the desk is the only dark surface left. Anything that assumed a dark
        // ground - a light text colour, a white-tinted overlay, an alpha hairline meant to lift off black -
        // is wrong now rather than merely off-palette.
        public static readonly Color Desk = Hex(0x241B10);
        public static readonly Color DeskDeep = Hex(0x14100B);
        public static readonly Color AppBackground = Desk;
        public static readonly Color Card = Hex(0xF0E7D8);        // paper
        public static readonly Color CardInset = Hex(0xE7DCC4);   // plate
        public static readonly Color Tile = Hex(0xEDE2CB);
        public static readonly Color ModalBackground = Hex(0xF2EADB);
        public static readonly Color Scrim = new Color(0.078f, 0.063f, 0.043f, 0.85f);
        public static readonly Color Hairline = Hex(0xB7A98C);        // rule
        public static readonly Color HairlineStrong = Hex(0x8A7A5C);  // ruleHeavy
        public static readonly Color BarTrack = new Color(0.545f, 0.478f, 0.361f, 0.35f);
        public static readonly Color ThresholdMarker = Hex(0x2B2620);
        public static readonly Color Brass = Hex(0x9C8148);
        public static readonly Color BrassLight = Hex(0xB3985E);
        public static readonly Color BrassBorder = Hex(0x6F5A30);
        public static readonly Color StockOff = Hex(0xB9A886);
        public static readonly Color StockHover = Hex(0xC4B28E);

        // --- Text ramp: ink on paper. ---
        public static readonly Color TextPrimary = Hex(0x2B2620);   // inkText
        public static readonly Color TextSecondary = Hex(0x5D564A); // inkFaint
        public static readonly Color TextMuted = Hex(0x7A7263);
        /// <summary>Text sitting on the DESK rather than on paper - the interrupt banner, and only that.</summary>
        public static readonly Color TextOnDesk = Hex(0xF0E7D8);

        // --- Semantic inks. ---
        public static readonly Color Good = Hex(0x3E8A5F);
        public static readonly Color Caution = Hex(0xBE8A00);
        public static readonly Color Bad = Hex(0x9C4238);
        public static readonly Color Neutral = Hex(0x6D7480);

        /// <summary>
        /// Amber is reserved for "drafted but not enacted" — budget drafts, pending bills.
        ///
        /// ⚠ **IT IS NO LONGER THE SAME VALUE AS THE POLITICAL AREA HUE, and that separation is the whole
        /// point.** Until 2026-08-03 `Draft`, `Caution` and `SystemArea.Political` were all `#E0B341` —
        /// literally one hex serving two load-bearing behaviours, so on any Political-tinted surface
        /// "drafted, not enacted" was indistinguishable from "this belongs to Politics". Design caught it
        /// and split them: draft amber `#BE8A00`, Political ochre `#A8842E`. A pencil-amber draft mark can
        /// no longer be read as a Politics legend key.
        /// </summary>
        public static readonly Color Draft = Caution;

        // --- Geometry. All radii in unscaled px; multiply by the caller's UI scale. ---
        public const float RadiusPanel = 19f;
        public const float RadiusCard = 16f;
        public const float RadiusInset = 14f;
        public const float RadiusChip = 12f;
        public const float RadiusControl = 11f;

        public const float SpaceXs = 6f;
        public const float SpaceSm = 10f;
        public const float SpaceMd = 14f;
        public const float SpaceLg = 20f;
        public const float CardPaddingX = 20f;
        public const float CardPaddingY = 18f;

        public const float BarHeightSm = 6f;
        public const float BarHeightMd = 8f;
        public const float BarHeightLg = 12f;

        // --- Type scale (unscaled px). Multiply by UI scale before assigning to GUIStyle.fontSize. ---
        public const int FontStatHero = 42;
        public const int FontStatLarge = 34;
        public const int FontStatMedium = 28;
        public const int FontTitle = 22;
        public const int FontSubtitle = 17;
        public const int FontBody = 15;
        public const int FontBodySmall = 13;
        public const int FontLabel = 10;   // uppercase, letter-spaced
        public const int FontMicro = 10;   // mono meta

        /// <summary>
        /// The R2 dark-surface tuning of each <see cref="UiPalette.SystemArea"/> hue. Same hue
        /// family and same meaning as UiPalette — lifted in value/chroma so it stays legible as a
        /// 4px spine or a 2px rule on #151920 instead of muddying into the card.
        /// </summary>
        private static readonly Dictionary<UiPalette.SystemArea, Color> AreaAccents = new Dictionary<UiPalette.SystemArea, Color>
        {
            { UiPalette.SystemArea.Neutral, Hex(0x6D7480) },
            { UiPalette.SystemArea.Fiscal, Hex(0x35619E) },
            { UiPalette.SystemArea.Trade, Hex(0x23867B) },
            { UiPalette.SystemArea.Political, Hex(0xA8842E) },
            { UiPalette.SystemArea.Welfare, Hex(0xA84E7B) },
            { UiPalette.SystemArea.Labor, Hex(0xB5622F) },
            { UiPalette.SystemArea.CrimeJustice, Hex(0x9C4238) },
            { UiPalette.SystemArea.Sectors, Hex(0x62579F) },
            { UiPalette.SystemArea.Infrastructure, Hex(0x3E7480) },
            { UiPalette.SystemArea.SovereignWealth, Hex(0x85643A) },
            { UiPalette.SystemArea.Global, Hex(0x5C87A8) }
        };

        /// <summary>
        /// The same eleven hues at DESK weight — lighter, for the one class of surface that is still a
        /// dark ground: `ui_banner_hold` and anything else desk-mounted. The ink set above would sink into
        /// it. Kept as a separate table rather than derived, because these are authored values from
        /// `polisim_palette.json`'s "lifted" set, not a computed lightening of the inks.
        /// </summary>
        private static readonly Dictionary<UiPalette.SystemArea, Color> AreaAccentsOnDesk = new Dictionary<UiPalette.SystemArea, Color>
        {
            { UiPalette.SystemArea.Neutral, Hex(0x9AA1AD) },
            { UiPalette.SystemArea.Fiscal, Hex(0x7C9CC9) },
            { UiPalette.SystemArea.Trade, Hex(0x5FA89E) },
            { UiPalette.SystemArea.Political, Hex(0xC9A855) },
            { UiPalette.SystemArea.Welfare, Hex(0xC27E9F) },
            { UiPalette.SystemArea.Labor, Hex(0xC98A5E) },
            { UiPalette.SystemArea.CrimeJustice, Hex(0xBC7168) },
            { UiPalette.SystemArea.Sectors, Hex(0x9288C2) },
            { UiPalette.SystemArea.Infrastructure, Hex(0x7BA3AE) },
            { UiPalette.SystemArea.SovereignWealth, Hex(0xB0925F) },
            { UiPalette.SystemArea.Global, Hex(0x8FAEC7) }
        };

        /// <summary>
        /// The four party inks - **their own set, and deliberately not the eleven area accents**.
        ///
        /// Delivered by pass 3 as `parties.*` answering D5: the boards had every party printing in an
        /// area ink (National Labor Front in CrimeJustice's red, which is also semantic `bad`; Agrarian
        /// League in Political's ochre, on the very tab whose own accent is Political). Two load-bearing
        /// meanings sharing one hex is the defect §1B.5 had just resolved for draft amber, arriving from
        /// another direction. These are cut in hue space the areas never occupy - wine, petrol, drab
        /// khaki, sage.
        ///
        /// ⚠ **The hemicycle drew from `UiPalette.GetCategoricalColor` until 2026-08-10**, which is the
        /// CHART SERIES set. Behaviour 9 held by luck - the legend swatch and the arc both called the
        /// same function with the same index, so they matched - but four parties were consuming
        /// categorical slots 0-3, so a party and a pie wedge could print identically. B9 was satisfied
        /// while the thing B9 protects was not.
        /// </summary>
        private static readonly Dictionary<PartyArchetype, Color> PartyInks = new Dictionary<PartyArchetype, Color>
        {
            { PartyArchetype.ProgressiveAlliance, Hex(0x7E3557) },
            { PartyArchetype.ConservativeUnion, Hex(0x2F4E63) },
            { PartyArchetype.CentristCoalition, Hex(0x77714A) },
            { PartyArchetype.NationalistFront, Hex(0x4E5A45) }
        };

        /// <summary>This party's ink. The SAME call must serve a hemicycle arc and its legend swatch - that is behaviour 9, and routing both through one accessor is what makes it true by construction rather than by two call sites agreeing.</summary>
        public static Color Party(PartyArchetype archetype) =>
            PartyInks.TryGetValue(archetype, out Color ink) ? ink : AreaAccents[UiPalette.SystemArea.Neutral];

        public static Color Accent(UiPalette.SystemArea area) => AreaAccents[area];

        /// <summary>The desk-weight variant. See <see cref="AreaAccentsOnDesk"/>.</summary>
        public static Color AccentOnDesk(UiPalette.SystemArea area) => AreaAccentsOnDesk[area];

        /// <summary>The same hue at card-tint strength — badge backgrounds, icon plates, decision-card washes.</summary>
        public static Color AccentWash(UiPalette.SystemArea area, float alpha = 0.13f)
        {
            Color c = AreaAccents[area];
            c.a = alpha;
            return c;
        }

        public static Color Tint(Color c, float alpha)
        {
            c.a = alpha;
            return c;
        }

        public static Color Hex(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f,
            1f);

        // --- Typography (v2.0) --------------------------------------------------------------

        /// <summary>
        /// The three type roles, and the reason they live HERE rather than in whichever class happens to
        /// build a style.
        ///
        /// **The v2.0 survey's font test assigned faces to GameController's 15 styles and every headline
        /// stat value stayed in Unity's default font**, because <see cref="PoliSimWidgets"/> builds its own
        /// styles from `GUI.skin.label` and is invisible from those call sites. A font owned by one screen
        /// class can only ever reach that class. Owned here, it reaches anything that already reads a
        /// design token — which is everything, by construction.
        ///
        /// **DISPLAY and BODY are the same humanist serif** (Elias's direction, 2026-08-03): humanist
        /// serifs keep large counters and stay readable at 13–15px, so one family covers headers and body
        /// without a second face and without a monospace's vertical cost.
        ///
        /// **DOCUMENT is a monospace and is RESERVED**, never ambient body text. It marks a genuine
        /// document artifact — a bill, a printed bulletin, a formal instrument. That reservation is the
        /// whole point: a face used everywhere signals nothing. The measured alternative was a 35–40%
        /// vertical cost on the densest screens for a signal that had stopped meaning anything.
        ///
        /// Null-safe throughout, matching <see cref="IconLibrary"/>'s standing contract: a missing font
        /// leaves `GUIStyle.font` null and Unity falls back to its built-in face, so a failed import
        /// degrades the look instead of rendering nothing.
        /// </summary>
        private const string FontResourcesPath = "Art/UI/Fonts/";

        /// <summary>
        /// **TeX Gyre Pagella**, chosen over three other open candidates after capturing all seven screens
        /// under each (2026-08-03). It is a metric-compatible Palatino clone, so it is the literal
        /// fulfilment of the stated direction rather than an approximation of it — and it also won on the
        /// two things the captures actually measured:
        ///
        /// - **It costs less width than Gentium Book Plus.** Gentium clipped "Sovereign Wealth Fund" in the
        ///   Budget category rail, where Pagella fits it on two lines. Same string, same style, taller line
        ///   box against a `fixedHeight` button.
        /// - **Lining figures.** Vollkorn was eliminated on this alone: it sets old-style (text) figures, so
        ///   "$29.0T" and "37,00%" render with x-height numerals that do not align in a column. In a game
        ///   whose every screen is a table of numbers that is disqualifying, and it is invisible until you
        ///   look at a real stat tile.
        ///
        /// Licensed under the GUST Font License (LPPL 1.3c) rather than OFL — free, redistributable, and
        /// commercially usable. See ATTRIBUTION.md beside the font files.
        /// </summary>
        private const string DisplayFontDefault = "TeXGyrePagella-Bold";
        private const string BodyFontDefault = "TeXGyrePagella-Regular";
        private const string DocumentFontDefault = "CourierPrime-Regular";

        private static Font _display, _body, _document;
        private static bool _fontsResolved;

        public static Font Display { get { ResolveFonts(); return _display; } }
        public static Font Body { get { ResolveFonts(); return _body; } }
        public static Font Document { get { ResolveFonts(); return _document; } }

        /// <summary>
        /// Loads the three faces once.
        ///
        /// `-fontfamily=&lt;name&gt;` overrides the serif for a run, so the candidate comparison captures a
        /// real build of the real code rather than a mock-up. Same command-line-argument idiom
        /// `SimulationTestRunner` already uses for `-seed=` and `-skipsimulationtestrunner`, and inert in
        /// a shipped game, which is never launched with it.
        /// </summary>
        private static void ResolveFonts()
        {
            if (_fontsResolved)
            {
                return;
            }

            _fontsResolved = true;

            string family = null;
            foreach (string arg in System.Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("-fontfamily="))
                {
                    family = arg.Substring("-fontfamily=".Length);
                }
            }

            string display = string.IsNullOrEmpty(family) ? DisplayFontDefault : family + "-Bold";
            string body = string.IsNullOrEmpty(family) ? BodyFontDefault : family + "-Regular";

            _display = Resources.Load<Font>(FontResourcesPath + display) ?? Resources.Load<Font>(FontResourcesPath + body);
            _body = Resources.Load<Font>(FontResourcesPath + body);
            _document = Resources.Load<Font>(FontResourcesPath + DocumentFontDefault);

            Debug.Log($"PoliSimTheme fonts: display={(_display != null ? _display.name : "DEFAULT")} " +
                $"body={(_body != null ? _body.name : "DEFAULT")} document={(_document != null ? _document.name : "DEFAULT")}");
        }

        /// <summary>Applies the body face to a style, or leaves Unity's default when the font is missing. A one-liner so no call site has to repeat the null check.</summary>
        public static GUIStyle WithBody(GUIStyle style)
        {
            if (Body != null) { style.font = Body; }
            return style;
        }

        /// <summary>The display-face equivalent of <see cref="WithBody"/>, for headers, tabs and banners.</summary>
        public static GUIStyle WithDisplay(GUIStyle style)
        {
            if (Display != null) { style.font = Display; }
            return style;
        }

        // --- Primitives -------------------------------------------------------------------

        private static Texture2D _white;

        private static Texture2D White
        {
            get
            {
                if (_white == null)
                {
                    _white = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                    _white.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                    _white.Apply(false);
                }

                return _white;
            }
        }

        /// <summary>
        /// Solid rounded rectangle. Unity 6's GUI.DrawTexture takes border widths and per-corner
        /// radii directly, so no nine-slice sprite and no generated per-size texture is needed —
        /// this is the single call every card, tile, pill and bar in the R2 UI is built from.
        /// </summary>
        public static void RoundedBox(Rect rect, Color fill, float radius)
        {
            GUI.DrawTexture(rect, White, ScaleMode.StretchToFill, true, 0f, fill, Vector4.zero, radius * Vector4.one);
        }

        /// <summary>Rounded rectangle with a 1px hairline border — the standard card treatment.</summary>
        public static void RoundedCard(Rect rect, Color fill, Color border, float radius, float borderWidth = 1f)
        {
            GUI.DrawTexture(rect, White, ScaleMode.StretchToFill, true, 0f, fill, Vector4.zero, radius * Vector4.one);
            GUI.DrawTexture(rect, White, ScaleMode.StretchToFill, true, 0f, border, borderWidth * Vector4.one, radius * Vector4.one);
        }

        /// <summary>Fully-rounded pill (radius = half height). Badges, buttons, chips.</summary>
        public static void Pill(Rect rect, Color fill)
        {
            RoundedBox(rect, fill, rect.height * 0.5f);
        }

        public static void Rule(Rect rect, Color color)
        {
            GUI.DrawTexture(rect, White, ScaleMode.StretchToFill, true, 0f, color, Vector4.zero, Vector4.zero);
        }

        /// <summary>The 2px category rule that sits along the top edge of a stat tile.</summary>
        public static void TopAccent(Rect cardRect, UiPalette.SystemArea area, float thickness = 2f)
        {
            Color c = Accent(area);
            c.a = 0.55f;
            Rule(new Rect(cardRect.x, cardRect.y, cardRect.width, thickness), c);
        }

        /// <summary>The 4px category spine down the left edge of a decision card or bill row.</summary>
        public static void LeftSpine(Rect cardRect, UiPalette.SystemArea area, float thickness = 4f)
        {
            Rule(new Rect(cardRect.x, cardRect.y, thickness, cardRect.height), Accent(area));
        }
    }
}
