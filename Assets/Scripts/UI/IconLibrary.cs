using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// Master Sequence step 5e, Phase B: runtime lookup for the imported icon sprites
    /// (`Assets/Resources/Art/UI/Icons/`), by name rather than `UiPalette.SystemArea` - several icons
    /// (the four `icon_nav_*` ones) deliberately don't correspond to any `SystemArea` value at all (see
    /// `CLAUDE_DESIGN_ASSET_REQUEST_5E.md`'s own note on why), so a `SystemArea`-keyed API would be
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
