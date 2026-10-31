using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §676 (SP-4, `docs/specs/START_POINTS_AND_PARTY_CREATION_SPEC.md` §2): **THE CREATION FLOW'S LOGIC, APART FROM ITS DRAWING** - the five steps' state
    /// (a <see cref="CreatedParty"/> draft), the choices each step offers (the unspent cells, the ink ring and its fence, the origins, the areas a
    /// single-issue party may choose, the valkrets a regional party may choose), the key derived from the short name, the words the slips carry, and
    /// the commit through `CreatedParties.TryRegister`. `GameController.DrawPartyCreationScreen` draws it; `PartyCreationCheck` reads the same members, so the
    /// check cannot pass on a choice the screen does not offer.
    /// </summary>
    public static class PartyCreationFlow
    {
        public const int StepCount = 5;

        /// <summary>Where a party may be created: a country whose campaign is staged, since a created party enters the chamber only by fighting an
        /// election. SP-5 (S5, K164): Sweden and, Germany first of the rest, Germany - its day zero read by Germany's staging, its placement on Germany's
        /// electorate, its ballot-access task by the BWahlG (`BallotAccess`). Poland next, Italy after PS-7 (K164); a country whose rules are not on disk is
        /// not offered.</summary>
        public static bool Offered(CountryId country) => country == CountryId.Sweden || country == CountryId.Germany;

        /// <summary>The five steps' plate heads, in order.</summary>
        public static readonly string[] StepHeads = { "PROFILE", "PLACEMENT", "DECLARATIONS", "LEADER", "REVIEW" };

        // ── the mark: the cells no seed spends ───────────────────────────────────────────────────────────

        /// <summary>D18's fifty-cell vocabulary (board 11b): five silhouettes, five cuts, two fills.</summary>
        public static readonly string[] Silhouettes = { "square", "disc", "hex", "wedge", "keystone" };
        public static readonly string[] Cuts = { "none", "bar", "notch", "split", "spine" };
        public static readonly string[] Fills = { "solid", "hatched" };

        /// <summary>D18's batch (board 11b's scope): the four chambers with no sourced ink, whose marks are the neutral cell vocabulary. Sweden's and the
        /// USA's are drawn art, outside it.</summary>
        public static readonly CountryId[] MarkBatchChambers = { CountryId.France, CountryId.Italy, CountryId.Germany, CountryId.Poland };

        /// <summary>
        /// SEAT-1 (the film's PartyCreationCheck failure, 2026-10-22): THE CELLS THE SEEDS SPEND - D18's assignment rule, the ONE source both the
        /// assignment (`D18MarkAssignment.Build`, Editor) and the picker (<see cref="UnspentCells"/>) read, so no cell is ever both assigned and offered.
        /// In each batch chamber the roster's parties by seed seats, largest first (the seed's order breaking ties); rank k takes silhouette k mod 5 and
        /// cut k / 5, solid. A rank past the ladder takes nothing (`D18MarkAssignment` reports it). Before SEAT-1 the picker reserved the first three solid
        /// cuts whole; the XVIII Camera's units took Italy's roster to 18, so its ranks 15-17 spend three cells of the fourth cut, which the picker no longer offers.
        /// </summary>
        public static List<(CountryId Country, PoliticalParty Party, string Silhouette, string Cut, string Cell)> SeedCells()
        {
            var rows = new List<(CountryId, PoliticalParty, string, string, string)>();
            foreach (CountryId country in MarkBatchChambers)
            {
                // S5: the REAL roster - a created party is no seed; with Germany offered (S5), `PartySystems.For` would rank a created German party
                // after the seeds and "spend" a cell for it. With none registered the two are the same array, so every existing rank stands.
                var pending = new List<PoliticalParty>(PartySystems.RealRoster(country));
                pending.Sort((a, b) => b.SeedSeats.CompareTo(a.SeedSeats));   // the very call D18's assignment made, so every existing rank stands
                for (int k = 0; k < pending.Count && k / Silhouettes.Length < Cuts.Length; k++)
                {
                    string silhouette = Silhouettes[k % Silhouettes.Length], cut = Cuts[k / Silhouettes.Length];
                    rows.Add((country, pending[k], silhouette, cut, $"mark_cell_{silhouette}_{cut}_solid"));
                }
            }
            return rows;
        }

        /// <summary>The cells a created party may wear: every cell the seeds do not spend (<see cref="SeedCells"/>) - so a created party never wears a real
        /// party's mark. Installed under `Resources/Art/UI/Cells/`, loaded by `IconLibrary.GetPartyMark`.</summary>
        public static List<string> UnspentCells()
        {
            var spent = new HashSet<string>();
            foreach ((CountryId _, PoliticalParty _, string _, string _, string cell) in SeedCells()) { spent.Add(cell); }
            var cells = new List<string>();
            foreach (string fill in Fills)
            {
                foreach (string cut in Cuts)
                {
                    foreach (string s in Silhouettes) { string cell = $"mark_cell_{s}_{cut}_{fill}"; if (!spent.Contains(cell)) { cells.Add(cell); } }
                }
            }
            return cells;
        }

        private static HashSet<string> _unspent;

        /// <summary>S5: whether <paramref name="cell"/> is one no seed spends (<see cref="UnspentCells"/>, cached: the seeds are the real rosters', fixed
        /// for the process) - the draw path's question for a hatched cell's solid twin (`IconLibrary.MarkStemAt`).</summary>
        public static bool IsUnspent(string cell)
        {
            if (_unspent == null) { _unspent = new HashSet<string>(UnspentCells()); }
            return cell != null && _unspent.Contains(cell);
        }

        // ── the ink: the ring and its fence ──────────────────────────────────────────────────────────────

        /// <summary>S5 (Design's return of 2026-10-30, answer 4: "a cell within ΔE 0.09 of a seated party's ink draws as taken"): the distance, in the
        /// fence's own measure (oklab, Euclidean - `PoliSimTheme.NearestRealInk`), under which a ring cell is TAKEN and not selectable. K333 (§965): the
        /// chamber's own collision line (`PoliSimTheme.NudgeTolerance`), not Design's 0.09 - the seated inks sit at the ring's own seat, so at 0.09 Germany's
        /// nine and Italy's ten take all 24 cells (measured: 0 clear; 3 each at 0.06) and a German party could not be founded. A created party is held to
        /// the line the seated parties are held to each other.</summary>
        public const float TakenWithin = PoliSimTheme.NudgeTolerance;

        /// <summary>A ring step's verdict: clear, or TAKEN by the nearest seated party's drawn ink (within <see cref="TakenWithin"/>).</summary>
        public static bool InkClear(CountryId country, int step, out string nearest, out float distance)
        {
            nearest = PoliSimTheme.NearestRealInk(country, PoliSimTheme.CreatedInkCandidate(step), out distance);
            return nearest == null || distance >= TakenWithin;
        }

        public static string HexOf(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        // ── the origin's own choices ─────────────────────────────────────────────────────────────────────

        /// <summary>The single-issue party's issue: one of the policy areas (its icon is the choice; nothing reads the issue yet - owed with the
        /// salience term).</summary>
        public static readonly UiPalette.SystemArea[] Issues =
        {
            UiPalette.SystemArea.Fiscal, UiPalette.SystemArea.Welfare, UiPalette.SystemArea.Labor, UiPalette.SystemArea.CrimeJustice, UiPalette.SystemArea.Energy,
            UiPalette.SystemArea.Infrastructure, UiPalette.SystemArea.Trade, UiPalette.SystemArea.Sectors, UiPalette.SystemArea.Political,
        };

        /// <summary>The regional party's region: a valkrets NAME, as `LiveCampaignSetup.TryCreatedDayZero` matches it (§675's review caveat). Outside Sweden
        /// none: a Regional party there reads nationally (K122, K166).</summary>
        public static string[] Regions(CountryId country)
        {
            if (country != CountryId.Sweden) { return Array.Empty<string>(); }
            RegionAudience[] regions = LiveCampaignSetup.SwedenRegions(out double _);
            var names = new string[regions.Length];
            for (int i = 0; i < regions.Length; i++) { names[i] = regions[i].Name; }
            return names;
        }

        /// <summary>S5 (answer 6): the map the Regional party picks its constituency on - the HQ's own tiles for the country (`CampaignTiles.For`, the
        /// chamber's contest over <see cref="Regions"/>), or null where the country offers no region (K166).</summary>
        public static CampaignTiles.TileMap RegionMap(CountryId country)
        {
            string[] names = Regions(country);
            return names.Length == 0 ? null : CampaignTiles.For(country, CampaignContest.Chamber, names);
        }

        /// <summary>S5: the tile <paramref name="region"/> is drawn as on <paramref name="map"/>, or null.</summary>
        public static CampaignTiles.Tile? TileOf(CampaignTiles.TileMap map, CountryId country, string region)
        {
            if (map == null || string.IsNullOrEmpty(region)) { return null; }
            string[] names = Regions(country);
            foreach (CampaignTiles.Tile t in map.Tiles)
            {
                int r = CampaignTiles.RegionOf(map, t, names);
                if (r >= 0 && names[r] == region) { return t; }
            }
            return null;
        }

        /// <summary>S5 (answer 1): a stat's glyph stem name (`Resources/Art/UI/PartyCreation/stat_&lt;name&gt;_16`).</summary>
        public static string StatGlyph(int stat) => stat switch { 0 => "funding", 1 => "organisation", 2 => "recognition", 3 => "activists", _ => "leader" };

        /// <summary>S5 (answer 2): an origin's glyph stem name (`Resources/Art/UI/PartyCreation/origin_&lt;name&gt;_20`) - a sprout, a fork, a placard, a briefcase, a
        /// target, a pin.</summary>
        public static string OriginGlyph(PartyOrigin origin) => origin switch
        {
            PartyOrigin.Grassroots => "grassroots", PartyOrigin.Splinter => "splinter", PartyOrigin.Protest => "protest",
            PartyOrigin.BusinessTechnocrats => "technocrats", PartyOrigin.SingleIssue => "single", _ => "regional",
        };

        /// <summary>The Splinter's slices of its parent's last result (the spec's "a chosen slice"), as shares 0-1. [AUTHORED-DRAFT] the steps.</summary>
        public static readonly double[] Slices = { 0.05, 0.10, 0.20, 0.30 };

        // ── the draft ────────────────────────────────────────────────────────────────────────────────────

        /// <summary>A new draft: a grassroots party at the electorate's centre, the first unspent cell, the first clear ink - every default a choice the
        /// flow offers, none invented. The names are empty: the player names the party.</summary>
        public static CreatedParty NewDraft(CountryId country)
        {
            var d = new CreatedParty { Country = country, Name = string.Empty, ShortName = string.Empty, LeaderName = string.Empty };
            d.ApplyOrigin(PartyOrigin.Grassroots);
            if (PartySystems.TryElectorate(country, out VoteModel.Electorate e, out double _)) { d.LrEcon = (float)e.MuEcon; d.Galtan = (float)e.MuSoc; }
            else { d.LrEcon = 5f; d.Galtan = 5f; }
            d.MarkStem = UnspentCells()[0];
            for (int step = 0; step < PoliSimTheme.CreatedInkSteps; step++)
            {
                if (InkClear(country, step, out string _, out float _)) { d.InkHex = HexOf(PoliSimTheme.CreatedInkCandidate(step)); break; }
            }
            return d;
        }

        /// <summary>An origin chosen: its preset stats (§2.2) and its rule where the rule sets something - the Splinter starts at its parent's nine
        /// positions once the parent is chosen; the Protest movement's people-versus-elite position is locked at the chamber's most anti-elite
        /// real position (read from the roster, not authored).</summary>
        public static void ChooseOrigin(CreatedParty d, PartyOrigin origin)
        {
            d.ApplyOrigin(origin);
            d.ParentKey = origin == PartyOrigin.Splinter ? d.ParentKey : null;
            d.InheritedSlice = origin == PartyOrigin.Splinter ? (d.InheritedSlice > 0.0 ? d.InheritedSlice : Slices[1]) : 0.0;
            d.Issue = origin == PartyOrigin.SingleIssue ? (d.Issue ?? Issues[0].ToString()) : null;
            string[] regions = Regions(d.Country);
            d.Region = origin == PartyOrigin.Regional ? (d.Region ?? (regions.Length > 0 ? regions[0] : null)) : null;
            if (origin == PartyOrigin.Protest)
            {
                float most = float.NaN;
                foreach (PoliticalParty p in PartySystems.RealRoster(d.Country)) { if (!float.IsNaN(p.PeopleVsElite) && (float.IsNaN(most) || p.PeopleVsElite > most)) { most = p.PeopleVsElite; } }
                d.PeopleVsElite = most;
            }
            else { d.PeopleVsElite = float.NaN; }
        }

        /// <summary>The Splinter's parent chosen: the draft moves onto the parent's nine positions (`CreatedParty.PlaceOn`).</summary>
        public static void ChooseParent(CreatedParty d, string parentKey)
        {
            foreach (PoliticalParty p in PartySystems.RealRoster(d.Country))
            {
                if (p.Abbrev != parentKey) { continue; }
                d.ParentKey = parentKey;
                d.PlaceOn(p);
                return;
            }
        }

        /// <summary>The persisted key from the short name: its letters and digits folded to ASCII capitals (å/ä → A, ö → O - the diacritics lesson,
        /// §566), at most six, beginning with a letter; a clash with a real or created key takes a digit.</summary>
        public static string KeyFrom(CountryId country, string shortName)
        {
            var sb = new StringBuilder();
            string decomposed = (shortName ?? string.Empty).Normalize(NormalizationForm.FormD);
            foreach (char ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) { continue; }
                char up = char.ToUpperInvariant(ch);
                if ((up >= 'A' && up <= 'Z') || (sb.Length > 0 && up >= '0' && up <= '9')) { sb.Append(up); }
                if (sb.Length == 6) { break; }
            }
            if (sb.Length == 0) { return string.Empty; }
            string key = sb.ToString();
            for (int n = 2; Taken(country, key) && n < 10; n++) { key = (sb.Length >= 6 ? sb.ToString(0, 5) : sb.ToString()) + n.ToString(CultureInfo.InvariantCulture); }
            return key;
        }

        private static bool Taken(CountryId country, string key)
        {
            foreach (PoliticalParty p in PartySystems.For(country)) { if (string.Equals(p.Abbrev, key, StringComparison.OrdinalIgnoreCase)) { return true; } }
            return false;
        }

        /// <summary>What still stops the draft (the review's refusal, one reason), or null when it can be registered. The registry's own refusal is the
        /// last word; this names what the flow asks of the player first.</summary>
        public static string Missing(CreatedParty d)
        {
            if (string.IsNullOrWhiteSpace(d.Name)) { return "name"; }
            if (string.IsNullOrWhiteSpace(d.ShortName) || string.IsNullOrEmpty(d.Key)) { return "short name"; }
            if (string.IsNullOrWhiteSpace(d.LeaderName)) { return "leader"; }
            if (string.IsNullOrEmpty(d.InkHex)) { return "ink"; }
            if (d.Origin == PartyOrigin.Splinter && string.IsNullOrEmpty(d.ParentKey)) { return "parent party"; }
            return null;
        }

        /// <summary>The commit: the draft registered (the registry's refusal returned as it words it). The flow then seats it as the player's party.</summary>
        public static bool TryCommit(CreatedParty d, out string refused)
        {
            refused = Missing(d);
            if (refused != null) { refused = "the " + refused + " is not set"; return false; }
            return CreatedParties.TryRegister(d, PartySystems.RealRoster(d.Country), out refused);
        }

        /// <summary>The model's idle prediction for the draft where it stands (a percentage), or NaN where the country has no fitted electorate - read
        /// inside a registry scope, then the prediction re-run without it so the regional derivation the live game reads is the real roster's again.</summary>
        public static double PredictedShare(CreatedParty d)
        {
            double share = double.NaN;
            string key = string.IsNullOrEmpty(d.Key) ? "DRAFT" : d.Key;
            string previous = d.Key;
            d.Key = key;
            try
            {
                using (CreatedParties.Scope())
                {
                    if (CreatedParties.TryRegister(d, PartySystems.RealRoster(d.Country), out string _)
                        && NationalElection.TryPredictShares(d.Country, out Dictionary<string, double> shares) && shares.TryGetValue(key, out double s))
                    {
                        share = s * 100.0;
                    }
                }
            }
            finally
            {
                d.Key = previous;
                NationalElection.TryPredictShares(d.Country, out _);
            }
            return share;
        }

        // ── the slips: every word the flow keeps off the page ────────────────────────────────────────────

        public static string StatName(int stat) => stat switch { 0 => "Funding", 1 => "Organisation", 2 => "Recognition", 3 => "Activists", _ => "Leader" };

        public static int StatOf(CreatedParty d, int stat) => stat switch { 0 => d.Funding, 1 => d.Organisation, 2 => d.Recognition, 3 => d.Activists, _ => d.Leader };

        public static int StatOf(PartyOrigins.Preset p, int stat) => stat switch { 0 => p.Funding, 1 => p.Organisation, 2 => p.Recognition, 3 => p.Activists, _ => p.Leader };

        /// <summary>What one pip-count sets, in the engine's own units (the pip table, §2.2).</summary>
        public static string PipMeaning(int stat, int pips) => stat switch
        {
            0 => string.Format(CultureInfo.InvariantCulture, "a war chest {0:P0} of a real party's; no state support (it pays on past elections)", PartyOrigins.Pip(PartyOrigins.FundingShare, pips)),
            1 => string.Format(CultureInfo.InvariantCulture, "{0} office(s) at the start", PartyOrigins.Pip(PartyOrigins.Offices, pips)),
            2 => string.Format(CultureInfo.InvariantCulture, "name awareness {0:P0} at the start - the share of voters who know the party, growing with its coverage and campaigning", PartyOrigins.Pip(PartyOrigins.Awareness, pips)),
            3 => string.Format(CultureInfo.InvariantCulture, "volunteers {0:P0} of a real party's", PartyOrigins.Pip(PartyOrigins.ActivistShare, pips)),
            _ => string.Format(CultureInfo.InvariantCulture, "the leader's debate and appeal at {0:0}", PartyOrigins.Pip(PartyOrigins.LeaderAttribute, pips) * 100.0),
        };

        /// <summary>The flow's slip book, built from the draft and the model: an anchor per control the page draws without words.</summary>
        public static PeopleSlips.Book Slips(CreatedParty d)
        {
            var book = new PeopleSlips.Book();
            for (int s = 0; s < StepCount; s++) { book.Anchors["step/" + s] = new SlipContent(StepHeads[s]).Add(StepLine(s)); }
            foreach (PartyOrigins.Preset p in PartyOrigins.All)
            {
                // S5 (answer 1): the card's words moved here, in the board's form - four fragments, the funding term marked (its level 2), and the rule
                book.Anchors["origin/" + p.Origin] = new SlipContent(p.Title)
                    .Add(string.Format(CultureInfo.InvariantCulture, "[[Funding]] {0} · {1:0} % of a real chest", p.Funding, 100.0 * PartyOrigins.Pip(PartyOrigins.FundingShare, p.Funding)))
                    .Add(string.Format(CultureInfo.InvariantCulture, "Organisation {0} · {1}", p.Organisation, Offices(PartyOrigins.Pip(PartyOrigins.Offices, p.Organisation))))
                    .Add(string.Format(CultureInfo.InvariantCulture, "Recognition {0} · awareness {1:0} %", p.Recognition, 100.0 * PartyOrigins.Pip(PartyOrigins.Awareness, p.Recognition)))
                    .Add(string.Format(CultureInfo.InvariantCulture, "Activists {0} · Leader {1}", p.Activists, p.Leader))
                    .Add(string.Format(CultureInfo.InvariantCulture, "Volunteers {0:0} % of a real party's · leader at {1:0}", 100.0 * PartyOrigins.Pip(PartyOrigins.ActivistShare, p.Activists), 100.0 * PartyOrigins.Pip(PartyOrigins.LeaderAttribute, p.Leader)))   // K320 (5): the two figures the board's four lines leave out stay on the slip
                    .Add("Rule · " + p.Rule);
            }
            book.Terms["Funding"] = new SlipContent("FUNDING").Add("The opening war chest, a share of a real party's; state funding pays only on a party's own past result.");
            for (int stat = 0; stat < 5; stat++)
            {
                book.Anchors["stat/" + stat] = new SlipContent(StatName(stat).ToUpperInvariant()).Add($"{StatOf(d, stat)} of 5 - {PipMeaning(stat, StatOf(d, stat))}");
                book.Anchors["statkey/" + stat] = new SlipContent(StatName(stat)).Add(StatSets(stat)).Add("Five pips, one to five");
            }
            foreach (string cell in UnspentCells())
            {
                book.Anchors["mark/" + cell] = new SlipContent(cell == d.MarkStem ? "Yours" : "A mark").Add(cell.Replace("mark_cell_", string.Empty).Replace('_', ' '))
                    .Add("White-cut, tinted in your ink").Add(cell.EndsWith("_hatched", StringComparison.Ordinal) ? (IsUnspent(cell.Replace("_hatched", "_solid")) ? "Hatched: solid twin at 16 px" : "Hatched: no free twin at 16 px") : "Solid")
                    .Add("No real party wears it");
            }
            for (int step = 0; step < PoliSimTheme.CreatedInkSteps; step++)
            {
                // S5 (answer 4): the board's slip - "Taken · M" / "Too near a seated party's ink", or "Free" / "Clear of every seated ink", with the measure
                bool clear = InkClear(d.Country, step, out string nearest, out float distance);
                book.Anchors["ink/" + step] = new SlipContent(clear ? "Free" : "Taken · " + PartySystems.ShortName(d.Country, nearest))
                    .Add(nearest == null ? "No seated ink to measure against" : string.Format(CultureInfo.InvariantCulture, "{0} · ΔE {1:0.000} to {2}", HexOf(PoliSimTheme.CreatedInkCandidate(step)), distance, PartySystems.ShortName(d.Country, nearest)))
                    .Add(clear ? "Clear of every seated ink" : "Too near a seated party's ink")
                    .Add(string.Format(CultureInfo.InvariantCulture, "Taken within ΔE {0:0.00}", TakenWithin));
            }
            // S5 (answer 6): the constituency map's slips
            CampaignTiles.TileMap regionMap = RegionMap(d.Country);
            book.Anchors["region/national"] = new SlipContent("REGION").Add("No region to pick here").Add("A Regional party reads nationally").Add("Outside Sweden, by ruling");
            book.Anchors["region/picked"] = new SlipContent(CampaignTiles.Display(d.Region ?? "No region")).Add("Your constituency").Add("The map opens from here").Add("Its first office opens here");
            if (regionMap != null)
            {
                string[] names = Regions(d.Country);
                foreach (CampaignTiles.Tile t in regionMap.Tiles)
                {
                    int r = CampaignTiles.RegionOf(regionMap, t, names);
                    if (r < 0) { continue; }
                    book.Anchors["region/tile/" + t.Id] = new SlipContent(t.Label).Add(names[r] == d.Region ? "Your constituency" : "A constituency to stand in");
                }
            }
            foreach (UiPalette.SystemArea area in Issues) { book.Anchors["issue/" + area] = new SlipContent("ISSUE").Add(UiPalette.AreaName(area)).Add("Raised salience with the voters who care about it - not yet in the vote."); }
            foreach (PoliticalParty p in PartySystems.RealRoster(d.Country))
            {
                book.Anchors["party/" + p.Abbrev] = new SlipContent(p.Abbrev).Add(p.Name).Add(string.Format(CultureInfo.InvariantCulture, "Economic {0:0.0} · GAL-TAN {1:0.0}", p.LrEcon, p.Galtan));
                string state = d.RedLinesAgainst.Contains(p.Abbrev) ? "Red line: will not sit in or support a cabinet with it."
                    : d.OneWayAgainst.Contains(p.Abbrev) ? "One way: will not sit in or support any cabinet it is in."
                    : "No declaration.";
                book.Anchors["line/" + p.Abbrev] = new SlipContent("DECLARATION - " + p.Abbrev).Add(state).Add("Each press steps: none, red line, one way.");
                book.Anchors["parent/" + p.Abbrev] = new SlipContent("PARENT - " + p.Abbrev).Add(p.Name).Add("The splinter starts at its positions and takes a slice of its last result.");
            }
            foreach (double slice in Slices) { book.Anchors["slice/" + slice.ToString("0.00", CultureInfo.InvariantCulture)] = new SlipContent("SLICE").Add(string.Format(CultureInfo.InvariantCulture, "{0:P0} of the parent's last result, moved from the parent.", slice)); }
            book.Anchors["candidate"] = new SlipContent("PRIME-MINISTER CANDIDACY").Add("The leader the party backs: its own, a real party's, or none.");
            book.Anchors["outside/yes"] = new SlipContent("SUPPORT FROM OUTSIDE").Add("May support a cabinet it does not sit in.");
            book.Anchors["outside/no"] = new SlipContent("IN OR AGAINST").Add("Refuses every cabinet it is not in.");
            book.Anchors["compass"] = new SlipContent("PLACEMENT").Add("Economic left-right across, GAL-TAN up (CHES, 0-10).").Add("The cross is the electorate's centre; the marks are the real parties.")
                .Add("Nearest: " + string.Join(", ", Nearest(d, 2)));
            book.Anchors["predicted"] = new SlipContent("THE MODEL'S IDLE PREDICTION").Add("The share the vote model gives the party where it stands, before any campaign.").Add("A newcomer has no earlier result and no loyal voters.");
            book.Anchors["key"] = new SlipContent("KEY").Add("The party's key in saves and seats: the short name in ASCII capitals.");
            return book;
        }

        /// <summary>S5: an office count as the slip reads it.</summary>
        private static string Offices(int n) => n == 1 ? "1 office" : n.ToString(CultureInfo.InvariantCulture) + " offices";

        /// <summary>S5 (answer 1): what a stat sets in the engine, in a fragment - the key's slip (the stat glyph alone at rest).</summary>
        public static string StatSets(int stat) => stat switch
        {
            0 => "The opening war chest",
            1 => "Offices at the start",
            2 => "Name awareness at the start",
            3 => "Volunteers at the doors",
            _ => "Debate and appeal",
        };

        /// <summary>The real parties nearest the draft on the vote model's two axes, nearest first, each with its distance.</summary>
        public static List<string> Nearest(CreatedParty d, int count)
        {
            var near = new List<(double Distance, string Abbrev)>();
            foreach (PoliticalParty p in PartySystems.RealRoster(d.Country))
            {
                if (!p.HasPosition) { continue; }
                near.Add((Math.Sqrt((p.LrEcon - d.LrEcon) * (p.LrEcon - d.LrEcon) + (p.Galtan - d.Galtan) * (p.Galtan - d.Galtan)), p.Abbrev));
            }
            near.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            var lines = new List<string>();
            for (int i = 0; i < Math.Min(count, near.Count); i++) { lines.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1:0.0} away", near[i].Abbrev, near[i].Distance)); }
            return lines;
        }

        private static string StepLine(int s) => s switch
        {
            0 => "Name, short name, mark, ink and origin.",
            1 => "Where the party stands on the two axes the vote model reads.",
            2 => "Red lines, the candidacy and whether it supports from outside. The coalition talks read them like every party's.",
            3 => "The leader's name; the pips are the origin's.",
            _ => "The party as it will enter the campaign. START registers it and seats you as it.",
        };
    }
}
