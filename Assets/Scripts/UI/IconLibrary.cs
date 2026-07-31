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
            if (Cache.TryGetValue(iconName, out Texture2D cached))
            {
                return cached;
            }

            Texture2D loaded = Resources.Load<Texture2D>(IconResourcesPath + iconName);
            Cache[iconName] = loaded;
            return loaded;
        }
    }
}
