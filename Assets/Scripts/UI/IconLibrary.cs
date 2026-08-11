using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// Master Sequence step 5e, Phase B: runtime lookup for the imported icon sprites
    /// (`Assets/Resources/Art/UI/Icons/`), by name rather than `UiPalette.SystemArea` - several icons
    /// (the four `icon_nav_*` ones) deliberately don't correspond to any `SystemArea` value at all (see
    /// COMPLETED.md section 8's note on why), so a `SystemArea`-keyed API would be
    /// wrong for half the set. Backed by `Resources.Load`, not `AssetDatabase` (Editor-only, would
    /// silently break in a real player build) - the Icons folder was moved into a `Resources/`
    /// subfolder specifically so this works both in-Editor and in a build. Textures are cached after
    /// first load (Resources.Load itself doesn't cache the C# reference across calls, just the
    /// underlying asset), since these get requested every OnGUI frame a tab-bar button renders.
    /// </summary>
    public static class IconLibrary
    {
        private const string IconResourcesPath = "Art/UI/Icons/";

        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> Cache =
            new System.Collections.Generic.Dictionary<string, Texture2D>();

        /// <summary>Loads (and caches) an icon by its exact filename minus extension, e.g. "icon_nav_statistics" or "icon_area_fiscal" - returns null (not a placeholder) if not found, so a typo'd name fails silently rather than drawing something misleading. UiPalette.DrawTintedIcon already treats a null texture as a no-op.</summary>
        public static Texture2D Get(string iconName)
        {
            return Load(IconResourcesPath + iconName);
        }

        /// <summary>Shared cache/load for every art category below - keyed on the FULL Resources path, so an icon and a portrait that happened to share a bare filename could never collide in the cache.</summary>
        private static Texture2D Load(string resourcePath)
        {
            if (Cache.TryGetValue(resourcePath, out Texture2D cached))
            {
                return cached;
            }

            Texture2D loaded = Resources.Load<Texture2D>(resourcePath);
            Cache[resourcePath] = loaded;
            return loaded;
        }

        private const string StatsResourcesPath = "Art/UI/Stats/";

        /// <summary>
        /// Master Sequence step 9, Step D: the macro stat icons, trend arrows, release marker and
        /// revision badges, by filename minus extension - e.g. "icon_stat_gdp", "icon_trend_up",
        /// "badge_preliminary". All 42 were delivered and imported in `be97ebb`, and
        /// `icon_stat_interestrate` - the one the original manifest missed - landed 2026-08-02, so the
        /// set is complete at 43.
        ///
        /// Same null-on-missing contract as <see cref="Get"/>: a typo draws nothing rather than
        /// something misleading.
        ///
        /// **Why interest rate was missed, kept because the lesson generalises:** the macro pack derived
        /// its stat list from the 29 fields on `EconomyState`, which is the right instinct and still
        /// missed this one - `InterestRate` lives on `CurrencyZone`, because a rate belongs to a currency
        /// zone rather than to one country's economy (the Eurozone five share one). It was structurally
        /// invisible to that derivation while being a `StatNodeId`, a `PolicyNodeId` target, a Taylor
        /// Rule input and the headline figure on two screens. **Enumerate the display enum, not the
        /// storage struct** - anything the UI can show needs an icon regardless of which type owns it.
        /// </summary>
        public static Texture2D GetStat(string statIconName)
        {
            return Load(StatsResourcesPath + statIconName);
        }

        private const string TextureResourcesPath = "Art/UI/Textures/";

        /// <summary>
        /// Full-surface background textures, by filename minus extension - currently just
        /// "menu_pattern_tile", imported 2026-08-02.
        ///
        /// Separate from <see cref="Get"/> and <see cref="GetStat"/> because these are used differently
        /// rather than merely stored elsewhere: a tile is drawn with `GUI.DrawTextureWithTexCoords` over
        /// a whole screen and **imported with Wrap Mode Repeat**, where every icon in this project is
        /// Clamp. Its `.meta` is the icon convention with exactly that one deviation. Mixing the two
        /// categories behind one accessor would invite an icon being drawn tiled, or a tile being
        /// imported clamped - which shows up as a visible seam rather than as an error.
        ///
        /// Same null-on-missing contract as everything else here: callers draw their existing background
        /// and skip the overlay.
        /// </summary>
        public static Texture2D GetTexture(string textureName)
        {
            return Load(TextureResourcesPath + textureName);
        }

        private const string ChromeResourcesPath = "Art/UI/Chrome/";

        /// <summary>
        /// Master Sequence step 5e, chrome batch 1: the imported control-chrome sprites (button
        /// backgrounds, slider and scrollbar parts, panel frame) by filename minus extension, e.g.
        /// "ui_button_normal". Authored pure white with all depth in alpha, so callers are expected to
        /// tint them - see `UiPalette.BuildButtonStyle`. Returns null when a sprite is missing, and every
        /// caller is written to fall back to its previous procedural appearance rather than draw nothing,
        /// so a failed import degrades the look instead of producing invisible controls.
        /// </summary>
        public static Texture2D GetChrome(string chromeName)
        {
            return Load(ChromeResourcesPath + chromeName);
        }

        private const string FlagResourcesPath = "Art/UI/Flags/";
        private const string EmblemResourcesPath = "Art/UI/Emblems/";

        /// <summary>
        /// A country's national flag, by <see cref="PoliSim.Data.CountryId"/> - filename derived from the
        /// enum, like every other art category here.
        ///
        /// ⚠ **THESE WERE DELIVERED, IMPORTED, AND THEN INVISIBLE FOR WEEKS.** The flags and the party
        /// emblems both sat under `Assets/Art/UI/`, which is NOT under a `Resources/` folder, so
        /// `Resources.Load` could never have found them - and nothing referenced them anyway, so nothing
        /// ever failed. Found by the v2.0 UI survey (2026-08-03), which counted sprites on disk and
        /// compared that against sprites the code can reach.
        ///
        /// **`DeliveredAssetCheck` passes on these and always did**, which is the part worth keeping:
        /// that check asks *"did every delivered file land under `Assets/`"* and the answer was yes.
        /// **A file existing under `Assets/` does not mean the game can load it.** `StatIconCoverageCheck`
        /// is the check that asks the runtime question, and it only covers the 19 names the UI hard-codes.
        /// Working-discipline rule 12 says a status describing the outside world needs an expiry; this is
        /// the same lesson one layer in - **an asset's status has TWO parts, delivered and reachable, and
        /// the project only had a check for the first.**
        ///
        /// **Not white-on-alpha, unlike every other category here.** Flags and emblems are authored in
        /// their own real colours (the emblem SVGs carry `#E0B23C` gold and `#FFFFFF`), which is correct
        /// for what they are, and means callers must NOT tint them the way `UiPalette.BuildButtonStyle`
        /// tints the chrome pack or `DrawTintedIcon` tints an area icon.
        /// </summary>
        public static Texture2D GetFlag(PoliSim.Data.CountryId countryId)
        {
            return Load(FlagResourcesPath + "flag_country_" + countryId.ToString().ToLowerInvariant());
        }

        /// <summary>A party archetype's emblem, by <see cref="PoliSim.Data.PartyArchetype"/>. Same derivation, same delivered-but-unreachable history, and same "already coloured, do not tint" caveat as <see cref="GetFlag"/>.</summary>
        public static Texture2D GetPartyEmblem(PoliSim.Data.PartyArchetype archetype)
        {
            return Load(EmblemResourcesPath + "emblem_party_" + archetype.ToString().ToLowerInvariant());
        }

        /// <summary>
        /// A real party's ballot stamp, by the mark name carried in its seed data.
        ///
        /// <para><b>WHITE-ON-ALPHA, AND THEREFORE TINTED AT DRAW TIME - the opposite of
        /// <see cref="GetPartyEmblem"/> above, which must never be tinted.</b> That difference is the
        /// whole reason the family is named `mark_party_*` rather than `emblem_party_*`, and it is a
        /// deliberate convention call by Design (2026-08-11): a mark carries silhouette only and takes
        /// its colour from <c>PoliticalParty.DisplayColor</c>, so a party rebrand - or Sweden's
        /// 13 September election changing a whole party set - is a SEED-DATA edit and never a
        /// redelivery. `emblem_*` keeps its already-coloured, never-tint meaning and retires with the
        /// archetypes.</para>
        ///
        /// <para>The name is stored on the party rather than derived from its id, because these are
        /// authored drawings and a derivation rule would silently point at the wrong file the first time
        /// a party id and a filename disagree. Returns null for a party with no mark yet, which every
        /// call site already treats as "draw the row without one".</para>
        /// </summary>
        public static Texture2D GetPartyMark(string markName)
        {
            return string.IsNullOrEmpty(markName) ? null : Load(EmblemResourcesPath + markName);
        }

        private const string PortraitResourcesPath = "Art/UI/Portraits/";

        /// <summary>
        /// Master Sequence step 5e, Phase C batch 3: the imported cabinet/Fed-chair portrait art, looked
        /// up from the character's own generated name. The art was authored against the EXACT name pools
        /// in `CabinetSystem`/`FederalReserveSystem`, so the filename is derived rather than stored in a
        /// hand-maintained table that could drift out of sync with those pools: lowercase, drop anything
        /// that isn't a letter, spaces become underscores ("Amara Osei-Bonsu" -> `amara_oseibonsu`,
        /// "Wei-Lin Tanaka" -> `weilin_tanaka`). Returns null for an unknown name, so a name added to
        /// either pool later without matching art degrades to the existing procedural placeholder rather
        /// than drawing someone else's face - the one failure mode here that would actively mislead.
        /// </summary>
        public static Texture2D GetCabinetPortrait(PoliSim.Data.CabinetPortfolio portfolio, string ministerName)
        {
            return Load(PortraitResourcesPath + "portrait_cabinet_" + portfolio.ToString().ToLowerInvariant() + "_" + Slug(ministerName));
        }

        /// <summary>The Fed chair equivalent of <see cref="GetCabinetPortrait"/> - same naming rule, minus the portfolio segment, since Fed chair candidates aren't split by portfolio.</summary>
        public static Texture2D GetFedChairPortrait(string chairName)
        {
            return Load(PortraitResourcesPath + "portrait_fedchair_" + Slug(chairName));
        }

        private static string Slug(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (char.IsLetter(c))
                {
                    builder.Append(char.ToLowerInvariant(c));
                }
                else if (c == ' ')
                {
                    builder.Append('_');
                }
            }

            return builder.ToString();
        }
    }
}
