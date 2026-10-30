using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections;

namespace PoliSim.UI
{
    /// <summary>
    /// S4 (Design's return of 2026-10-30, "For Code" 1): ONE MAP GRAMMAR - the HQ's regions as a tile map. Design placed three maps (the okregi, the
    /// wojewodztwa and the valkretsar: `CampaignTiles.Generated.cs`, generated from its hq_tiles.json by `Tools/s4_tiles.pl`); a staging's region is
    /// matched to its tile by the region's key (an okreg's number, a wojewodztwo's or a valkrets's name). Germany's Lander take the same grammar on a
    /// plain 4 x 4 grid in the staging's order (<see cref="GermanLander"/>) - DECLARED, Code's placeholder: Design placed no Lander, and a grid in the
    /// staging's order claims no geography. The USA's tiles are Design's section 7 and are not drawn here (<see cref="For"/> answers null).
    /// </summary>
    public static partial class CampaignTiles
    {
        /// <summary>One tile: its id (the label drawn on it), its column and row in the map's grid, and its long label (the slip's and the plate's head).</summary>
        public readonly struct Tile
        {
            public readonly string Id;
            public readonly int Column;
            public readonly int Row;
            public readonly string Label;

            public Tile(string id, int column, int row, string label) { Id = id; Column = column; Row = row; Label = label; }
        }

        /// <summary>One map: its grid, its tile and gap in px at 1280, its origin at 1280 (the tile at column 0, row 0) and its tiles.</summary>
        public sealed class TileMap
        {
            public readonly string Key;
            public readonly int Columns;
            public readonly int Rows;
            public readonly float TileSize;
            public readonly float Gap;
            public readonly float OriginX;
            public readonly float OriginY;
            public readonly Tile[] Tiles;

            public TileMap(string key, int columns, int rows, float tileSize, float gap, float originX, float originY, Tile[] tiles)
            {
                Key = key; Columns = columns; Rows = rows; TileSize = tileSize; Gap = gap; OriginX = originX; OriginY = originY; Tiles = tiles;
            }

            /// <summary>The map's width and height in px at 1280.</summary>
            public float Width => Columns * TileSize + (Columns - 1) * Gap;
            public float Height => Rows * TileSize + (Rows - 1) * Gap;

            /// <summary>Whether the number sits at the tile's top left (Poland's maps) or centred (Sweden's, and the Lander's codes).</summary>
            public bool LabelTopLeft => Key == "pl_sejm" || Key == "pl_wojewodztwa";
        }

        /// <summary>ISO 3166-2:DE's codes, by the Land's name as the staging names it (`GermanRegions.NameAt`) - the tile's label on the placeholder grid.</summary>
        private static readonly Dictionary<string, string> LandCodes = new Dictionary<string, string>
        {
            { "Baden-Württemberg", "BW" }, { "Bayern", "BY" }, { "Berlin", "BE" }, { "Brandenburg", "BB" }, { "Bremen", "HB" }, { "Hamburg", "HH" },
            { "Hessen", "HE" }, { "Mecklenburg-Vorpommern", "MV" }, { "Niedersachsen", "NI" }, { "Nordrhein-Westfalen", "NW" }, { "Rheinland-Pfalz", "RP" },
            { "Saarland", "SL" }, { "Sachsen", "SN" }, { "Sachsen-Anhalt", "ST" }, { "Schleswig-Holstein", "SH" }, { "Thüringen", "TH" },
        };

        /// <summary>DECLARED (Code's placeholder until Design places the Lander): <paramref name="names"/> on a 4 x 4 grid in the staging's order, 70 px
        /// tiles 6 apart (the wojewodztwa's measure), each labelled with its ISO 3166-2:DE code (its name where the code is not held).</summary>
        public static TileMap GermanLander(IReadOnlyList<string> names)
        {
            var tiles = new Tile[names.Count];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new Tile(LandCodes.TryGetValue(names[i], out string code) ? code : names[i], i % 4, i / 4, names[i]);
            }
            return new TileMap("de_lander", 4, (tiles.Length + 3) / 4, 70f, 6f, 495f, 163f, tiles);
        }

        /// <summary>The map the HQ draws for <paramref name="country"/>'s <paramref name="contest"/> over <paramref name="regionNames"/>, or null where none is
        /// drawn (the USA, until section 7's tiles; a country with no regional campaign).</summary>
        public static TileMap For(CountryId country, CampaignContest contest, IReadOnlyList<string> regionNames)
        {
            switch (country)
            {
                case CountryId.Poland: return contest == CampaignContest.Chamber ? PolishSejm : PolishWojewodztwa;
                case CountryId.Sweden: return SwedishValkretsar;
                case CountryId.Germany: return regionNames != null && regionNames.Count > 0 ? GermanLander(regionNames) : null;
                default: return null;
            }
        }

        /// <summary>The region (an index into <paramref name="regionNames"/>) <paramref name="tile"/> stands for on <paramref name="map"/>, or −1: an okreg by
        /// its number ("okreg 3" is tile "3"), every other map by the tile's label.</summary>
        public static int RegionOf(TileMap map, Tile tile, IReadOnlyList<string> regionNames)
        {
            if (regionNames == null) { return -1; }
            string key = map.Key == "pl_sejm" ? "okreg " + tile.Id : tile.Label;
            for (int r = 0; r < regionNames.Count; r++) { if (regionNames[r] == key) { return r; } }
            return -1;
        }

        /// <summary>A region's name as the player reads it: an okreg's key ("okreg 3") with its diacritic ("okręg 3") - a NAME carries its diacritics, a key
        /// is ASCII (CLAUDE.md's rule) - every other name as the staging holds it.</summary>
        public static string Display(string regionName)
        {
            if (string.IsNullOrEmpty(regionName)) { return regionName; }
            return regionName.StartsWith("okreg ", System.StringComparison.Ordinal) ? "okręg " + regionName.Substring(6) : regionName;
        }

        /// <summary>The long label of <paramref name="regionName"/>'s tile on <paramref name="map"/> ("okręg 3 · Wrocław"), else its display name.</summary>
        public static string LabelOf(TileMap map, string regionName, IReadOnlyList<string> regionNames)
        {
            if (map != null)
            {
                foreach (Tile t in map.Tiles)
                {
                    int r = RegionOf(map, t, regionNames);
                    if (r >= 0 && regionNames[r] == regionName) { return t.Label; }
                }
            }
            return Display(regionName);
        }
    }
}
