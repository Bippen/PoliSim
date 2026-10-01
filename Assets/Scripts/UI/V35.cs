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
    }
}
