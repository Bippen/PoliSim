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
        /// <summary>Empty-step ink for a stepped ordinal rule (board 1i's exact spec, the law
        /// browser's magnitude tier) - filled steps use TextPrimary/#2B2620, this is the unfilled
        /// remainder. Not reused from an existing token: the closest neighbors (StockOff/Hairline)
        /// are a different warm-tan value, and the delivered spec names this hex precisely rather
        /// than "close enough."</summary>
        public static readonly Color MagnitudeStepEmpty = Hex(0xCEC0A2);
        /// <summary>Label ink on the CLOSED folder stock — §A.7's inactive tab type colour. Darker than
        /// the paper ink ramp's faint values because the stock ground is itself mid-tone: TextSecondary
        /// on #B9A886 has barely any contrast left.</summary>
        public static readonly Color InkOnStock = Hex(0x45392A);

        // --- Text ramp: ink on paper. ---
        public static readonly Color TextPrimary = Hex(0x2B2620);   // inkText
        public static readonly Color TextSecondary = Hex(0x5D564A); // inkFaint
        /// <summary>D6 (2026-08-28, the v3.1 contrast pass): `#7A7263` → `#665E4F` — the token carries 7–8 px labels (the tile label, C20), and read 3.9 : 1 on Card; measured after the change 5.22 on Card, 4.98 on Tile (Annex F of the request doc, re-measured).</summary>
        public static readonly Color TextMuted = Hex(0x665E4F);
        /// <summary>Text sitting on the DESK rather than on paper - the interrupt banner, and only that.</summary>
        public static readonly Color TextOnDesk = Hex(0xF0E7D8);

        // --- §A.2 tokens that had no constant at HEAD (re-derived 2026-08-28, omnibus R-K10: the
        // 2026-08-27 sweep said three; against this file it is FIVE - ruleRow, borderPaper, borderPlate,
        // mutedInk, deskCaption - plus the RUNNING plate's own type ink from §A.6). Two of them were
        // already in use as bare literals (#C9BA9B in CountrySelectorScreen and SigningScreen).
        /// <summary>§A.2 `ruleRow` — the 1px ledger row separator.</summary>
        public static readonly Color RuleRow = Hex(0xD5C8AB);
        /// <summary>§A.2 `borderPaper` — the paper panel edge.</summary>
        public static readonly Color BorderPaper = Hex(0xCBBC9D);
        /// <summary>§A.2 `borderPlate` — the plate / tile edge (the RUNNING status plate, the disabled button).</summary>
        public static readonly Color BorderPlate = Hex(0xC9BA9B);
        /// <summary>§A.2 `mutedInk` — disabled row ink and disabled button text.</summary>
        public static readonly Color MutedInk = Hex(0x9A917D);
        /// <summary>§A.2 `deskCaption` — mono annotations on the desk ground.</summary>
        public static readonly Color DeskCaption = Hex(0x8D7D5F);
        /// <summary>§A.6's RUNNING status text — type on the `#EDE2CB` plate (`Tile`), a shade lighter than inkText.</summary>
        public static readonly Color TextOnPlate = Hex(0x3D372E);
        /// <summary>§A.6's disabled speed-button face fill (`#DDD2B8`) — the procedural fallback when `ui_btn_disabled` is missing.</summary>
        public static readonly Color DisabledFace = Hex(0xDDD2B8);

        // --- Semantic inks. ---
        // D6 (2026-08-28, the v3.1 contrast pass — Design's board D6 against the request doc's Annex F):
        // where a token carries TEXT below 4.5 : 1 at its real size its VALUE darkens (oklch L −0.07,
        // hue and chroma held); the identity survives, the whisper does not. Good 3.4 → 4.86 measured;
        // Caution (text uses) 2.5 → 4.09 on Card / 3.90 on Tile measured (D6 aimed at ≥ 4.5; the
        // measurement is the fact, filed back); Neutral (text uses) 3.8 → 4.72 measured. Bad stands
        // (5.3). The draft-amber FILLS keep the old value as `Draft` below — the split the palette
        // note promised: a threshold LINE and a hatch stay #BE8A00, the label beside them is this ink.
        public static readonly Color Good = Hex(0x2D6D46);   // P2-1.2 (2026-09-02): from 0x2E7048, scaled toward black (hue held) to clear 4.5 on CardInset as body text
        public static readonly Color Caution = Hex(0x8F6900);
        public static readonly Color Bad = Hex(0x9C4238);
        public static readonly Color Neutral = Hex(0x5F6672);

        /// <summary>
        /// Amber is reserved for "drafted but not enacted" — budget drafts, pending bills.
        ///
        /// ⚠ **IT IS NO LONGER THE SAME VALUE AS THE POLITICAL AREA HUE, and that separation is the whole
        /// point.** Until 2026-08-03 `Draft`, `Caution` and `SystemArea.Political` were all `#E0B341` —
        /// literally one hex serving two load-bearing behaviours, so on any Political-tinted surface
        /// "drafted, not enacted" was indistinguishable from "this belongs to Politics". Design caught it
        /// and split them: draft amber `#BE8A00`, Political ochre `#A8842E`. A pencil-amber draft mark can
        /// no longer be read as a Politics legend key.
        ///
        /// D6 (2026-08-28): no longer an alias of <see cref="Caution"/>. Caution darkened for its TEXT
        /// uses; this is the FILL amber — the hatch, the pencil, a graph's threshold line, the
        /// preliminary-release frame — and keeps `polisim_palette.json`'s `semantic.draftAmber` value.
        /// </summary>
        public static readonly Color Draft = Hex(0xBE8A00);

        /// <summary>
        /// Draft/caution amber at DESK weight — `polisim_palette.json`'s `semantic.draftAmber.lifted`
        /// (`#D4A72C`), an authored value like the <see cref="AreaAccentsOnDesk"/> set, not a computed
        /// lightening. The ink amber above was cut for paper and sinks into the dark desk ground; this
        /// is the value the spec's HELD status lamp and anything else amber-on-desk uses.
        /// </summary>
        public static readonly Color DraftOnDesk = Hex(0xD4A72C);

        // --- Geometry. All radii in unscaled px; multiply by the caller's UI scale. ---
        // D4 (2026-08-28): 19/16/14/12/11 → 16/13/11/10/9.
        public const float RadiusPanel = 16f;
        public const float RadiusCard = 13f;
        public const float RadiusInset = 11f;
        public const float RadiusChip = 10f;
        public const float RadiusControl = 9f;

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
            { UiPalette.SystemArea.Political, Hex(0x8A6B21) },   // D6 (2026-08-28): #A8842E → #8A6B21, with UiPalette's on-paper entry
            { UiPalette.SystemArea.Welfare, Hex(0xA84E7B) },
            { UiPalette.SystemArea.Labor, Hex(0xB5622F) },
            { UiPalette.SystemArea.CrimeJustice, Hex(0x9C4238) },
            { UiPalette.SystemArea.Sectors, Hex(0x62579F) },
            { UiPalette.SystemArea.Infrastructure, Hex(0x3E7480) },
            { UiPalette.SystemArea.SovereignWealth, Hex(0x85643A) },
            { UiPalette.SystemArea.Global, Hex(0x47708E) }       // D6 (2026-08-28): #5C87A8 → #47708E, with UiPalette's on-paper entry
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
        /// W-G1: real parties' inks, keyed by country and by the abbreviation that country's own
        /// election authority uses.
        ///
        /// **The hue is SOURCED, the saturation and value are the desk's.** Valmyndigheten publishes
        /// a `fargkod` per party in the same JSON backend W-F1 took the counts from — S #FF0000,
        /// SD #4E83A3, M #66BEE6, V #C40000, C #63A91D, KD #1B5CB1, MP #008000, L #3399FF. Those are
        /// screen-primary colours and would tear a hole in a paper sheet whose whole palette sits at
        /// S 0.23–0.58 and V 0.35–0.49 (measured off the four inks this replaces). **So the hue is
        /// taken exactly as published and the saturation and value are re-seated into the desk's own
        /// range** — a party stays recognisably itself (S red, MP green, C green-gold, L and KD and
        /// SD their three blues) while the sheet stays one object. The derivation is one line of
        /// arithmetic, stated here so it can be checked or struck.
        ///
        /// ⚠ **THE OTHER FIVE COUNTRIES HAVE NO INK, AND ARE NOT GIVEN ONE.** No published colour
        /// table for their parties is on disk, and picking 30 colours by eye would be exactly the
        /// invention §0.4 forbids — it would also be wrong, since these are real organisations with
        /// real colours a player may know. `Party` returns the neutral accent and `HasPartyInk`
        /// returns false, so a caller can say "not yet coloured" rather than assert a colour.
        /// The gap is a line in the W-H4 Design ask.
        ///
        /// The four archetype inks this replaces (wine, petrol, drab khaki, sage) were cut in hue
        /// space the eleven area accents never occupy, to keep a party from printing in an area's
        /// semantic colour. **That constraint is inherited, not discarded**: the desk-seated hues
        /// below are checked against the area accents by `PartyInkHarness`. ⚠ **That harness did not exist
        /// until 2026-08-31 (C-B2) - this comment named a check nothing performed.** It exists now, and it
        /// found the constraint BROKEN: six of the eight sit closer to an area accent than two area
        /// accents sit to each other, and **S and V collapse onto one ink** (#753838), their published
        /// colours differing only in darkness while the seating keeps hue and replaces saturation and
        /// value. Reported as PEND, not fixed: any fix stops using the authority own hue or picks a
        /// replacement by eye, and that ruling is Design (asset request D8-2).
        ///
        /// ⚠ **RECONCILED AND ANSWERED 2026-09-01.** *Six* is the right number: the harness's summary
        /// briefly printed **7** because one counter was adding hue-floor breaches, near-grey saturation
        /// proximity and the S/V collision into a single total while the label called it "inside the
        /// floor". The three are counted and printed apart now, and the printed rows check the printed
        /// number. ⚠ **And the FLOOR ITSELF IS RETIRED as the wrong constraint** — D9 row 5 (Design):
        /// it separates two AREA accents, chrome semantics that sit side by side in one rail, and party
        /// inks never appear in that company, so it binds *within* a channel and not across them. The
        /// six measurements stay printed because they are true; they are no longer a debt. What binds
        /// instead is structural — party ink is never drawn adjacent to an area accent — and lives at
        /// **S-29**, where a hue harness cannot reach it.
        /// </summary>
        private static readonly Dictionary<string, Color> PartyHues = new Dictionary<string, Color>
        {
            { "Sweden/S",  DeskSeated(0xFF0000) },
            { "Sweden/SD", DeskSeated(0x4E83A3) },
            { "Sweden/M",  DeskSeated(0x66BEE6) },
            { "Sweden/V",  DeskSeated(0xC40000) },
            { "Sweden/C",  DeskSeated(0x63A91D) },
            { "Sweden/KD", DeskSeated(0x1B5CB1) },
            { "Sweden/MP", DeskSeated(0x008000) },
            { "Sweden/L",  DeskSeated(0x3399FF) },
        };

        /// <summary>The desk's saturation for a party ink — the midpoint of the four inks this replaces (0.23–0.58).</summary>
        private const float PartyInkSaturation = 0.52f;

        /// <summary>The desk's value for a party ink — inside the 0.35–0.49 the four replaced inks occupied.</summary>
        private const float PartyInkValue = 0.46f;

        /// <summary>W-G1: the published colour's HUE, at the desk's own saturation and value. See <see cref="PartyHues"/> for why this is a derivation rather than a straight copy.</summary>
        private static Color DeskSeated(int publishedHex)
        {
            Color.RGBToHSV(Hex(publishedHex), out float h, out float s, out float v);
            // A published colour with no chroma at all (pure black or white) has no hue to keep;
            // nothing in the table is such a colour, and if one ever is, it lands on the desk's
            // neutral rather than on an arbitrary red.
            if (s <= 0.001f) { return AreaAccents[UiPalette.SystemArea.Neutral]; }
            return Color.HSVToRGB(h, PartyInkSaturation, PartyInkValue);
        }

        /// <summary>W-G1: true when this party has a published colour on disk. False is NOT "grey is its colour" — it is "no colour is known", and a caller that draws a legend should say so.</summary>
        public static bool HasPartyInk(PoliSim.Data.CountryId country, string abbrev) =>
            PartyHues.ContainsKey(country + "/" + abbrev);

        /// <summary>This party's ink. The SAME call must serve a hemicycle arc and its legend swatch - that is behaviour 9, and routing both through one accessor is what makes it true by construction rather than by two call sites agreeing.</summary>
        public static Color Party(PoliSim.Data.CountryId country, string abbrev) =>
            PartyHues.TryGetValue(country + "/" + abbrev, out Color ink) ? ink : AreaAccents[UiPalette.SystemArea.Neutral];

        public static Color Accent(UiPalette.SystemArea area) => AreaAccents[area];

        /// <summary>The desk-weight variant. See <see cref="AreaAccentsOnDesk"/>.</summary>
        public static Color AccentOnDesk(UiPalette.SystemArea area) => AreaAccentsOnDesk[area];

        /// <summary>
        /// §A.3's THIRD column - the inactive tab-swatch tint, delivered by pass 3 as `tabTint.*` and
        /// wired 2026-08-28 (omnibus, roadmap item 4): the hue an unselected folder tongue's swatch
        /// (its icon, in this build) prints in on the closed-stock ground. Snapped values from the
        /// spec's table (ink at oklch chroma ×0.78, lightness ×0.97) for the six areas that own a
        /// tongue or a promoted sub-tab; the five the table leaves "—" fall back to the ink, so a
        /// future tongue in one of those areas is never invisible.
        /// </summary>
        private static readonly Dictionary<UiPalette.SystemArea, Color> TabSwatchTints = new Dictionary<UiPalette.SystemArea, Color>
        {
            { UiPalette.SystemArea.Fiscal, Hex(0x3D6494) },
            { UiPalette.SystemArea.Political, Hex(0x96762A) },
            { UiPalette.SystemArea.Labor, Hex(0xA2653E) },
            { UiPalette.SystemArea.CrimeJustice, Hex(0x8E4A40) },
            { UiPalette.SystemArea.Sectors, Hex(0x5B5187) },
            { UiPalette.SystemArea.Global, Hex(0x4E7291) }
        };

        /// <summary>The inactive tab swatch's tint for an area - the snapped §A.3 value where one is delivered, the area ink otherwise.</summary>
        public static Color TabSwatchTint(UiPalette.SystemArea area) =>
            TabSwatchTints.TryGetValue(area, out Color tint) ? tint : AreaAccents[area];

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

        /// <summary>
        /// Board 1k's almanac strike (built 2026-08-28, omnibus R-K3): ONE diagonal ink stroke across
        /// a box - a spent calendar day crossed off the way an almanac does it. The stroke runs from
        /// the box's lower-left toward its upper-right at <paramref name="angleDegrees"/> (the board's
        /// ≈ −24°), its length the box width along that angle, centred on the box; drawn through
        /// GUI.matrix around the box centre and restored before returning. Repaint-gated by the
        /// caller, like every paint primitive here.
        /// </summary>
        public static void Stroke(Rect box, float angleDegrees, float thickness, Color ink)
        {
            float length = box.width / Mathf.Max(0.2f, Mathf.Cos(angleDegrees * Mathf.Deg2Rad));
            length = Mathf.Min(length, Mathf.Sqrt(box.width * box.width + box.height * box.height));
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(angleDegrees, box.center);
            Rule(new Rect(box.center.x - length * 0.5f, box.center.y - thickness * 0.5f, length, thickness), ink);
            GUI.matrix = saved;
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
