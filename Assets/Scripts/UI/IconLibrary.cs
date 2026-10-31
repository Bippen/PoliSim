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

        /// <summary>§723 (UI v3.5): one of the 69 v3.5 icons (`Resources/Art/UI/V35/icon_v35_&lt;name&gt;`, white on alpha, tinted at draw), by its short
        /// name - <c>V35("desk")</c>; null where the file is missing, the same contract as <see cref="Get"/>.</summary>
        public static Texture2D V35(string shortName)
        {
            return Load("Art/UI/V35/icon_v35_" + shortName);
        }

        /// <summary>The Campaign HQ's glyph stems drawn as their WORD this session because the file did not load - the HQ's fallbacks, named for the film's log.</summary>
        public static readonly System.Collections.Generic.HashSet<string> HqGlyphFallbacks = new System.Collections.Generic.HashSet<string>();

        /// <summary>
        /// S4 (Design's return of 2026-10-30, `assets/s4/glyphs`): one of the Campaign HQ's glyphs - `Resources/Art/UI/CampaignHq/&lt;stem&gt;` (the delivered folder, renamed on main), white on alpha and tinted
        /// at draw: a Polish party's mark for a polled tile and a picked AGAINST cell (<c>mark_pl_pis_w_14</c>, its 28 px pair at 2560), the field report
        /// (<c>report_16</c> / <c>report_32</c>), ≈ (<c>approx_18</c> / <c>approx_36</c>). Null where the file is not held - the caller draws the glyph's WORD in
        /// its place (UI v3.3 §4.2's honest fallback, never a blank and never a stand-in) and the stem is named in <see cref="HqGlyphFallbacks"/>. The PNGs are
        /// installed from Design's delivery as files, never written from a pulled image (§424).
        /// </summary>
        public static Texture2D HqGlyph(string stem)
        {
            Texture2D t = Load("Art/UI/CampaignHq/" + stem);
            if (t == null) { HqGlyphFallbacks.Add(stem); }
            return t;
        }

        /// <summary>The S9 verb glyph stems that did not load this session - named in the film's log (the slot draws nothing, as a missing v3.5 icon does).</summary>
        public static readonly System.Collections.Generic.HashSet<string> S9Fallbacks = new System.Collections.Generic.HashSet<string>();

        /// <summary>
        /// COAL-2 with §9 (Design's return of 2026-10-30, `assets/s9/glyphs`): a coalition verb's glyph - `Resources/Art/UI/CoalitionVerbs/verb_&lt;verb&gt;_16` or `_32`
        /// (<paramref name="large"/>), the verb one of threat, leave, dismiss, enforce, redeclare, c158; white on transparent, tinted at draw (ink, or paper on a
        /// filled chip). Null where the file is not held, the stem named in <see cref="S9Fallbacks"/>. Installed from Design's delivery as files (§424).
        /// </summary>
        public static Texture2D S9Verb(string verb, bool large)
        {
            string stem = "verb_" + verb + (large ? "_32" : "_16");
            Texture2D t = Load("Art/UI/CoalitionVerbs/" + stem);
            if (t == null) { S9Fallbacks.Add(stem); }
            return t;
        }

        /// <summary>
        /// The area icon for a <see cref="UiPalette.SystemArea"/> - `icon_area_&lt;area&gt;`, the
        /// filename derived from the enum like every other art category here. Omnibus 2026-08-28
        /// (R-K6): the eight area icons that had no call site (`COMPLETED.md` §33) are promoted to the
        /// sub-tab rows through THIS accessor, and `AreaIconCoverageCheck` enumerates the enum through
        /// it - one accessor, so the check and the screens cannot ask for different names. Neutral has
        /// no icon by design (it is the absence of an area) and returns null; every other member returns
        /// null only when the file is missing, the same contract as <see cref="Get"/>.
        /// </summary>
        public static Texture2D GetAreaIcon(UiPalette.SystemArea area)
        {
            if (area == UiPalette.SystemArea.Neutral)
            {
                return null;
            }

            return Get("icon_area_" + area.ToString().ToLowerInvariant());
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
        /// set is complete at 43 as a DELIVERY. As a reachable set it is 18: only the StatNodeId names
        /// PolicyScreenStatsRenderer.GetIconName maps are ever requested; the other 25 (19 stat icons
        /// without a node, the trend arrows, the badges and the release marker - GraphRenderer and
        /// PublishedFigure draw those procedurally) are held stock with no call site (2026-08-27,
        /// COMPLETED.md §33). ⚠ RULED HELD STOCK, 2026-08-28 (omnibus R-K6): those 25 stay in the
        /// folder with no loader - the display surfaces that would use them (a wider stat set, sprite
        /// markers and badges) are not being built - and are re-counted by `DeliveredAssetCheck`,
        /// never re-triaged as a gap. The same ruling holds six chrome names (see GetChrome).
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
        ///
        /// ⚠ HELD STOCK, RULED 2026-08-28 (omnibus R-K6, roadmap item 5): `ui_btn_disabled` gained its
        /// loader that day (the speed buttons' held face, `UiPalette.BuildButtonStyle`); the six
        /// remaining chrome names with no load call - `ui_frame_double`, `ui_stamp_draft`,
        /// `ui_portrait_frame_oval`, `ui_btn_paper_canvas` (+`_hover`, `_pressed`) and
        /// `ui_slider_knob_disabled` - stay held stock, delivered and counted by `DeliveredAssetCheck`,
        /// with no call site by decision (the 2026-08-12 "revivable by ruling" set plus the Canvas paper
        /// button the pilot never needed). §738 (2026-10-02): `ui_chip_outline` joined them - its one drawer, the published bulletin's
        /// badge (`PublishedFigure`), became a slip line when the Crime tab became tiles.
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

        /// <summary>
        /// W-G1 RETIRED `GetPartyEmblem`. It loaded `emblem_party_&lt;archetype&gt;` for the four generic
        /// fictional archetypes, and those archetypes no longer exist - every seat-holder is now a real
        /// party and takes <see cref="GetPartyMark"/>. The four delivered emblem files stay on disk
        /// rather than being deleted: they are Design's work, and the D0 collision map's `emblem_* -&gt;
        /// mark_*` clause is a rename of the SUBJECT, not a licence to throw away art.
        /// </summary>

        /// <summary>
        /// A REAL party's identity mark, by its full mark name — the file stem, e.g.
        /// `"mark_party_se_s"`. Five were delivered and, until Campaign HQ (W-E1, 2026-08-29), drawn
        /// by **nothing**; this is their first call site.
        ///
        /// **The argument is the whole stem, not a suffix**, and deliberately so: that is exactly
        /// what `PartyMarkCoverageCheck` passes to `Resources.Load` and exactly what it compares
        /// against the folder's file names, so this is the one-line wrapper over that call which the
        /// check's own comment promises rather than a second naming convention beside it. When the
        /// party system's seeds land, their mark names feed straight in with no translation.
        ///
        /// W-G1: this is now the ONLY party art accessor - `GetPartyEmblem` and the fictional archetypes
        /// it served are retired. Rule 9a
        /// governs the art — a mark is ORIGINAL art recognisable by silhouette and real colour,
        /// never the organisation's registered logo. Returns null when the file is missing, the same
        /// contract as everything else here.
        ///
        /// ⚠ **THE TINT: the two call sites disagree, and BOTH are deliberate** (measured 2026-09-09,
        /// §419, while D18's inventory was being generated; this doc comment previously said "do not
        /// tint it" flatly, and had been overtaken).
        /// The campaign masthead (`DrawCampaignMasthead`) draws the mark UNTINTED, in its authored colours.
        /// `HemicycleRenderer.DrawMark` draws it under `PoliSimTheme.PartyLaddered` - the party's own
        /// laddered ink - because board 6b row 5 asked for the chamber's mark in that ink, and the
        /// chamber is the one surface permitted to draw party ink at all (`PartyInkDrawSiteCheck`).
        /// A mark therefore has to read BOTH ways: as authored colour on the campaign masthead, and as
        /// a silhouette multiplied by one ink in the chamber. **That is a fact about the art, so it is
        /// D18's to state to Design, not something to settle by editing one of the two call sites.**
        /// </summary>
        public static Texture2D GetPartyMark(string markName)
        {
            if (string.IsNullOrEmpty(markName)) { return null; }

            // §676 (SP-4): a created party wears one of the cells no seed spends, installed under its own name in Cells/ (never Emblems/, where
            // PartyMarkCoverageCheck counts an unclaimed mark as an orphan).
            if (markName.StartsWith(CellPrefix, System.StringComparison.Ordinal)) { return Load(CellResourcesPath + markName); }

            return Load(EmblemResourcesPath + markName);
        }

        private const string CellPrefix = "mark_cell_";
        private const string CellResourcesPath = "Art/UI/Cells/";

        /// <summary>S5 (Design's return of 2026-10-30, answer 5): whether a mark is a created party's CELL. Since S5 the cells are cut white on
        /// transparent (`Tools/s5_cell_whitecut.pl`, RGB to white with the alpha kept), so a cell is drawn TINTED in its party's ink at every site -
        /// never "in its authored colours", where white on the paper would vanish (`PoliSimTheme.MarkDrawTint`).</summary>
        public static bool IsWhiteCutCell(string markName) => markName != null && markName.StartsWith(CellPrefix, System.StringComparison.Ordinal);

        /// <summary>S5 (answer 5): the size below which a hatched cell does not read - "hatched cells read at 24 px and above; at 16 px (the strip, the
        /// night) a hatched mark draws its solid twin".</summary>
        public const float HatchedReadsFrom = 24f;

        /// <summary>S5 (answer 5): the stem a mark draws at <paramref name="px"/> - a hatched cell below <see cref="HatchedReadsFrom"/> draws its solid
        /// twin (the same silhouette and cut, solid) WHERE THE TWIN IS UNSPENT, so no created party ever draws a seated party's mark; every other
        /// stem is itself. ⚠ The solid twins of the none, bar and notch cuts are spent by the batch chambers' seeds (`PartyCreationFlow.SeedCells`),
        /// so those hatched cells keep their hatch at 16 px - their twins are owed by Design (S5's return, answer 5: "author a solid twin").</summary>
        public static string MarkStemAt(string markName, float px)
        {
            if (!IsWhiteCutCell(markName) || px >= HatchedReadsFrom || !markName.EndsWith("_hatched", System.StringComparison.Ordinal)) { return markName; }
            string twin = markName.Substring(0, markName.Length - "_hatched".Length) + "_solid";
            return PoliSim.UI.PartyCreationFlow.IsUnspent(twin) ? twin : markName;
        }

        /// <summary>S5: <see cref="GetPartyMark(string)"/> at a drawn size - a hatched cell's solid twin under 24 px (<see cref="MarkStemAt"/>).</summary>
        public static Texture2D GetPartyMark(string markName, float px) => GetPartyMark(MarkStemAt(markName, px));

        /// <summary>
        /// S5 (Design's return of 2026-10-30): one of party creation's glyphs - `Resources/Art/UI/PartyCreation/&lt;stem&gt;`, white on alpha and tinted at draw: the
        /// five stats (<c>stat_funding_16</c> / <c>_32</c>, organisation, recognition, activists, leader) and the six origins (<c>origin_grassroots_20</c> /
        /// <c>_40</c>, splinter, protest, technocrats, single, regional). Null where the file is not held - the caller draws the glyph's WORD in its place
        /// (UI v3.3 §4.2's honest fallback) and the stem is named in <see cref="PartyCreationFallbacks"/>. Installed from Design's delivery as files (§424).
        /// </summary>
        public static Texture2D PartyCreationGlyph(string stem)
        {
            Texture2D t = Load("Art/UI/PartyCreation/" + stem);   // the folder named for what it holds (main 7fbd678a)
            if (t == null) { PartyCreationFallbacks.Add(stem); }
            return t;
        }

        /// <summary>The party-creation glyph stems drawn as their WORD this session because the file did not load.</summary>
        public static readonly System.Collections.Generic.HashSet<string> PartyCreationFallbacks = new System.Collections.Generic.HashSet<string>();

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
