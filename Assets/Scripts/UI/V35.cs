using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §723 (UI v3.5, Design's V35 and board 27a, ruled by Elias 2026-10-01): THE v3.5 GRAMMAR'S TOKENS - one place for the unit, the type
    /// scale and its 14 px floor, the rail's measures and the colour states, so every retrofitted screen reads the same numbers.
    ///
    /// <para><b>The unit.</b> Design draws v3.5 in the real 1280 window, whose client area is 1280 x 699; every measure below is a pixel at
    /// that height and scales with the window's height (<see cref="Px"/>), the axis the desk's styles already scale by
    /// (<c>RescaleStylesToScreen</c>). At 2560 x 1419 a 14 px floor draws at 28.</para>
    ///
    /// <para><b>The type scale</b> (27a §3): nothing below <see cref="Floor"/>, the rail and the masthead included; headings 15 in small
    /// caps, names 16-17, figures 21-30.</para>
    ///
    /// <para><b>The colour states</b> (V35_ASK rule 5, 27a §5): a change carries ▲▼ in the outcome's ink where a rise is good or bad by broad
    /// consensus, in <see cref="DirectionNeutral"/> otherwise; a draft is <see cref="PoliSimTheme.Caution"/> amber; an Off tile is muted and
    /// dashed. Good and bad keep D6's inks (27a: "Stand: D6's ink values") - <see cref="PoliSimTheme.Good"/> is 27a's #2E7048 scaled toward
    /// black for the 4.5 floor (P2-1.2), <see cref="PoliSimTheme.Bad"/> is 27a's #9C4238.</para>
    /// </summary>
    public static class V35
    {
        /// <summary>The reference window's height, px: the 1280 window's client area (Design's frames, 1280 x 699).</summary>
        public const float ReferenceHeight = 699f;

        /// <summary>The window's scale against the reference - its height over 699.</summary>
        public static float Scale => Mathf.Max(0.5f, UiScreen.Height / ReferenceHeight);

        /// <summary>A v3.5 measure (px at 1280 x 699) at this window, rounded to whole pixels.</summary>
        public static float Px(float atReference) => Mathf.Round(atReference * Scale);

        /// <summary>A v3.5 type size at this window, never below the floor's own size at this window.</summary>
        public static int FontPx(float atReference) => Mathf.Max(Mathf.RoundToInt(Mathf.Max(atReference, Floor) * Scale), 1);

        // ---- the type scale (27a §3) ----
        /// <summary>The 14 px floor, everywhere - the rail and the masthead included.</summary>
        public const float Floor = 14f;

        /// <summary>§726: THE FLOOR'S GUARD - true while a v3.5 surface draws (a retrofitted screen sets it around its page), and
        /// <see cref="PoliSimWidgets.MeasuredLabel"/> then reports a shrink below the floor to the overflow guard as an overflow. A screen not yet
        /// retrofitted keeps its faces and is not asked.</summary>
        public static bool FloorGuarded;
        /// <summary>Section headings, small caps.</summary>
        public const float Heading = 15f;
        /// <summary>Row and tile names (16-17).</summary>
        public const float Name = 16f;
        /// <summary>A figure, at its smallest (21-30).</summary>
        public const float FigureSmall = 21f;
        /// <summary>A tile's figure.</summary>
        public const float Figure = 24f;
        /// <summary>A page's lead figure.</summary>
        public const float FigureLarge = 30f;

        // ---- the rail (V35_ASK rule 8) ----
        /// <summary>The rail's width - it grows to carry 14 px captions.</summary>
        public const float RailWidth = 84f;
        /// <summary>A folder tab's width, flush to the page (the rail's other 4 px are desk ground).</summary>
        public const float TabWidth = 80f;
        /// <summary>A folder tab's height.</summary>
        public const float TabHeight = 56f;
        /// <summary>A tab's icon - inside the tab, never crossing its edge.</summary>
        public const float RailIcon = 24f;
        /// <summary>The ground between two tabs.</summary>
        public const float RailGap = 4f;
        /// <summary>The active tab's area-ink spine (the composition's `border-left: 4px`); an inactive tab's spine is transparent.</summary>
        public const float RailSpine = 4f;
        /// <summary>A tab's field is inset this much on the right (the composition's `padding-right: 4px`).</summary>
        public const float RailFieldInset = 4f;
        /// <summary>The gap between a tab's icon and its caption; the caption's line height.</summary>
        public const float RailIconGap = 3f, RailCaptionLine = 16f;
        /// <summary>An inactive tab's caption ink (#4A433A); the active caption is TextPrimary.</summary>
        public static readonly Color RailCaptionInactive = PoliSimTheme.Hex(0x4A433A);
        /// <summary>An inactive tab's paper (#D9CDB4), behind hairline edges; the active tab is the page's own paper.</summary>
        public static readonly Color TabInactive = PoliSimTheme.Hex(0xD9CDB4);

        // ---- the colour states (V35_ASK rule 5, 27a §5) ----
        /// <summary>A change with no consensus direction (levers, mixes, prices, the trade balance): direction only, in the composition's muted ink.</summary>
        public static readonly Color DirectionNeutral = PoliSimTheme.Hex(0x665E4F);

        /// <summary>§725: the warning state - a figure past a statutory limit (<see cref="FiscalRules"/>); D6's bad ink.</summary>
        public static Color Warning => PoliSimTheme.Bad;

        /// <summary>§725 (Elias's ruling): debt's or the balance's ink - <paramref name="neutral"/> by default, <see cref="Warning"/> only where the level
        /// breaches the country's statutory rule; <paramref name="rule"/> names it for the slip.</summary>
        public static Color FiscalInk(PoliSim.Data.CountryId country, FiscalRules.Measure measure, float percentOfGdp, Color neutral, out string rule) =>
            FiscalRules.Breaches(country, measure, percentOfGdp, out rule) ? Warning : neutral;

        // ---- the card and the page (§726, the composition's map, calendar and list cards) ----
        /// <summary>The grid's gutter between two cards, and between two rows of cards.</summary>
        public const float Gutter = 12f;
        /// <summary>A card's inside: 14 px left and right, 12 px top and bottom.</summary>
        public const float CardPadX = 14f, CardPadY = 12f;
        /// <summary>The space between a card's head and its content.</summary>
        public const float CardHeadGap = 8f;
        /// <summary>A card's head icon, and a list card's (the Approval ledger's) larger one.</summary>
        public const float CardIcon = 28f, ListIcon = 30f;
        /// <summary>A list row's line (the composition's 24 px rows under a 1 px rule).</summary>
        public const float ListRow = 24f;
        /// <summary>A page's title (the composition's "Desk", "Statistics").</summary>
        public const float PageTitle = 30f;
        /// <summary>A card's paper (#F5EDDE) - a shade lighter than the page's.</summary>
        public static readonly Color CardPaper = PoliSimTheme.Hex(0xF5EDDE);
        /// <summary>A card's edge (#D5C8AB, the theme's row rule).</summary>
        public static Color CardEdge => PoliSimTheme.RuleRow;
        /// <summary>The rule under a list row (#E6DCC8).</summary>
        public static readonly Color ListRule = PoliSimTheme.Hex(0xE6DCC8);

        // ---- the data inks (§732, the composition's People: a population's bars and a share bar's parts) ----
        /// <summary>The population's bars of working age, and the turnout's ticks (#47708E).</summary>
        public static readonly Color DataSlate = PoliSimTheme.Hex(0x47708E);
        /// <summary>The dependent bands' bars, and a share bar's lightest slate part (#9AA7B4).</summary>
        public static readonly Color DataSlateLight = PoliSimTheme.Hex(0x9AA7B4);
        /// <summary>The age split's 65-and-over part (#7A8A9C).</summary>
        public static readonly Color DataSlateMid = PoliSimTheme.Hex(0x7A8A9C);
        /// <summary>A share bar's first part where it reads as the remainder (below upper secondary, electricity) (#B7A98C).</summary>
        public static readonly Color DataSand = PoliSimTheme.Hex(0xB7A98C);
        /// <summary>A share bar's deepest part (tertiary, everything else) (#5F6672).</summary>
        public static readonly Color DataDeep = PoliSimTheme.Hex(0x5F6672);
        /// <summary>A part's words on a dark part (#F6EFDF), and on a light one (#2B2620).</summary>
        public static readonly Color OnDataDark = PoliSimTheme.Hex(0xF6EFDF), OnDataLight = PoliSimTheme.Hex(0x2B2620);
        /// <summary>The population's two dashed thresholds, 15 and 65 (#8A7A5C).</summary>
        public static readonly Color PyramidThreshold = PoliSimTheme.Hex(0x8A7A5C);

        // ---- the slip (§733, V35_ASK rule 2) ----
        /// <summary>The slip's paper (#F6EFDF), its 1 px edge (#8A7A5C) and its 2 px shadow (the ink at a quarter).</summary>
        public static readonly Color SlipPaper = PoliSimTheme.Hex(0xF6EFDF), SlipEdge = PoliSimTheme.Hex(0x8A7A5C);
        public static readonly Color SlipShadow = new Color(43f / 255f, 38f / 255f, 32f / 255f, 0.25f);
    }
}
