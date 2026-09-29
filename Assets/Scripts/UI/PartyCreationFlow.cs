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

        /// <summary>Where a party may be created: a country whose campaign is staged (`LiveCampaignSetup.TryFor` - Sweden alone), since a created party
        /// enters the chamber only by fighting an election.</summary>
        public static bool Offered(CountryId country) => country == CountryId.Sweden;

        /// <summary>The five steps' plate heads, in order.</summary>
        public static readonly string[] StepHeads = { "PROFILE", "PLACEMENT", "DECLARATIONS", "LEADER", "REVIEW" };

        // ── the mark: the cells no seed spends ───────────────────────────────────────────────────────────

        /// <summary>D18's fifty-cell vocabulary (board 11b): five silhouettes, five cuts, two fills.</summary>
        public static readonly string[] Silhouettes = { "square", "disc", "hex", "wedge", "keystone" };
        public static readonly string[] Cuts = { "none", "bar", "notch", "split", "spine" };
        public static readonly string[] Fills = { "solid", "hatched" };

        /// <summary>The cuts D18's ladder spends on the real parties (solid only) - the largest chamber needs three. `PartyCreationCheck` asserts this
        /// against `D18MarkAssignment.Build`'s own rows, so the two cannot part.</summary>
        public const int SpentCuts = 3;

        /// <summary>The cells a created party may wear: every cell the seeds do not (the 25 hatched, and the split and spine cuts solid) - so a created
        /// party never wears a real party's mark. Installed under `Resources/Art/UI/Cells/`, loaded by `IconLibrary.GetPartyMark`.</summary>
        public static List<string> UnspentCells()
        {
            var cells = new List<string>();
            foreach (string fill in Fills)
            {
                for (int c = 0; c < Cuts.Length; c++)
                {
                    if (fill == "solid" && c < SpentCuts) { continue; }
                    foreach (string s in Silhouettes) { cells.Add($"mark_cell_{s}_{Cuts[c]}_{fill}"); }
                }
            }
            return cells;
        }

        // ── the ink: the ring and its fence ──────────────────────────────────────────────────────────────

        /// <summary>A ring step's verdict: clear, or colliding with the nearest real party's drawn ink (under the nudge's tolerance).</summary>
        public static bool InkClear(CountryId country, int step, out string nearest, out float distance)
        {
            nearest = PoliSimTheme.NearestRealInk(country, PoliSimTheme.CreatedInkCandidate(step), out distance);
            return nearest == null || distance >= PoliSimTheme.NudgeTolerance;
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

        /// <summary>The regional party's region: a valkrets NAME, as `LiveCampaignSetup.TryCreatedDayZero` matches it (§675's review caveat).</summary>
        public static string[] Regions(CountryId country)
        {
            if (country != CountryId.Sweden) { return Array.Empty<string>(); }
            RegionAudience[] regions = LiveCampaignSetup.SwedenRegions(out double _);
            var names = new string[regions.Length];
            for (int i = 0; i < regions.Length; i++) { names[i] = regions[i].Name; }
            return names;
        }

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
            2 => string.Format(CultureInfo.InvariantCulture, "name awareness {0:P0} - read by nothing yet: the model has no awareness term", PartyOrigins.Pip(PartyOrigins.Awareness, pips)),
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
                var slip = new SlipContent(p.Title.ToUpperInvariant()).Add("Rule: " + p.Rule);
                for (int stat = 0; stat < 5; stat++) { slip.Add($"{StatName(stat)} {StatOf(p, stat)} of 5 - {PipMeaning(stat, StatOf(p, stat))}"); }
                book.Anchors["origin/" + p.Origin] = slip;
            }
            for (int stat = 0; stat < 5; stat++) { book.Anchors["stat/" + stat] = new SlipContent(StatName(stat).ToUpperInvariant()).Add($"{StatOf(d, stat)} of 5 - {PipMeaning(stat, StatOf(d, stat))}"); }
            foreach (string cell in UnspentCells()) { book.Anchors["mark/" + cell] = new SlipContent("MARK").Add(cell.Replace("mark_cell_", string.Empty).Replace('_', ' ')).Add("No real party wears it."); }
            for (int step = 0; step < PoliSimTheme.CreatedInkSteps; step++)
            {
                bool clear = InkClear(d.Country, step, out string nearest, out float distance);
                book.Anchors["ink/" + step] = new SlipContent(clear ? "INK" : "INK - REFUSED")
                    .Add(nearest == null ? "No real party's ink to measure against." : string.Format(CultureInfo.InvariantCulture, "{0:0.000} from {1}'s drawn ink; the fence asks {2:0.00}.", distance, nearest, PoliSimTheme.NudgeTolerance))
                    .Add(clear ? "Clear of every real party's ink." : "Too close to read as a second party at dot size.");
            }
            foreach (UiPalette.SystemArea area in Issues) { book.Anchors["issue/" + area] = new SlipContent("ISSUE").Add(area.ToString()).Add("Raised salience with the voters who care about it - not yet in the vote."); }
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
            2 => "Red lines, the candidacy and whether it supports from outside. Declared now; the coalition talks do not read them yet.",
            3 => "The leader's name; the pips are the origin's.",
            _ => "The party as it will enter the campaign. START registers it and seats you as it.",
        };
    }
}
