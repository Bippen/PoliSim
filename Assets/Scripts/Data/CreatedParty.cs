using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PoliSim.Data
{
    /// <summary>The six origins (START_POINTS_AND_PARTY_CREATION_SPEC §2.2), numbered as the cards are.</summary>
    public enum PartyOrigin { Grassroots = 1, Splinter = 2, Protest = 3, BusinessTechnocrats = 4, SingleIssue = 5, Regional = 6 }

    /// <summary>
    /// §671 (SP-3, the spec's S3): **A PARTY THE PLAYER CREATED, AS DATA.** Its ASCII key (the persisted key - saves, seats, marks, inks; the
    /// diacritics lesson, §566), its names, its nine CHES positions (the two axes the vote model places, LrEcon and Galtan, and the seven the
    /// stance model reads), its leader, its mark's cell stem and ink, its origin and the origin's five stats, and the origin's own choices (the
    /// Splinter's parent and slice, the Single-issue party's issue, the Regional party's region). Saved with the game (`SaveGame.CreatedParties`)
    /// and registered with `CreatedParties` so `PartySystems.For` carries it after the country's real parties - every existing index stands.
    /// </summary>
    [Serializable]
    public sealed class CreatedParty
    {
        public CountryId Country;
        public string Key;
        public string Name;
        public string ShortName;
        public float LrEcon, Galtan, EuPosition = float.NaN, LrGen = float.NaN, Environment = float.NaN, Regions = float.NaN, SpendVsTax = float.NaN,
            ImmigratePolicy = float.NaN, Deregulation = float.NaN, Redistribution = float.NaN, PeopleVsElite = float.NaN, AntiEliteSalience = float.NaN,
            CivLibLawOrder = float.NaN, Nationalism = float.NaN;
        public string LeaderName;
        public string LeaderTitle;
        public string MarkStem;
        public string InkHex;
        public PartyOrigin Origin = PartyOrigin.Grassroots;
        /// <summary>The five stats as pips, 1-5 (§2.2): Funding, Organisation, Recognition, Activists, Leader - the origin's preset unless changed.</summary>
        public int Funding = 1, Organisation = 1, Recognition = 1, Activists = 1, Leader = 1;
        /// <summary>The Splinter's parent (a real party's key) and the slice of the parent's last result it inherits as its prior, 0-1.</summary>
        public string ParentKey;
        public double InheritedSlice;
        public string Issue;
        public string Region;

        /// <summary>§676 (SP-4): the party's declarations as the creation flow stores them, by real party KEY (an index moves with the roster, a key
        /// does not) - the parties it will not sit in or support a cabinet with (a symmetric red line), the parties it will not sit in or support any
        /// cabinet CONTAINING (one-way, K-1's shape), the party whose leader it backs for prime minister (its own key for its own leader, empty for
        /// none), and whether it refuses every cabinet it is not in (in-or-against, K-1f's shape). READ by the formation since §679 (`DeclaredRedLines` appends them to the sourced declarations;
        /// every reading, dated or not).</summary>
        public List<string> RedLinesAgainst = new List<string>();
        public List<string> OneWayAgainst = new List<string>();
        public string BacksCandidateOf;
        public bool InOrAgainst;

        /// <summary>The party as the roster carries it: no seats of its own (a created party enters the chamber by election), its positions and leader.</summary>
        public PoliticalParty ToParty() => new PoliticalParty(Key, Name, LrEcon, Galtan, 0, MarkStem, EuPosition,
            string.IsNullOrEmpty(LeaderName) ? null : new[] { new PartyLeader(LeaderName, LeaderTitle ?? "partiledare") },
            LrGen, Environment, Regions, SpendVsTax, ImmigratePolicy, Deregulation, Redistribution, PeopleVsElite, AntiEliteSalience, CivLibLawOrder, Nationalism,
            string.IsNullOrEmpty(ShortName) ? null : ShortName);

        /// <summary>A copy placed exactly on a real party's nine positions - the spec's first assertion's fixture, and the Splinter's starting point.</summary>
        public void PlaceOn(in PoliticalParty p)
        {
            LrEcon = p.LrEcon; Galtan = p.Galtan; EuPosition = p.EuPosition; LrGen = p.LrGen; Environment = p.Environment; Regions = p.Regions;
            SpendVsTax = p.SpendVsTax; ImmigratePolicy = p.ImmigratePolicy; Deregulation = p.Deregulation; Redistribution = p.Redistribution;
            PeopleVsElite = p.PeopleVsElite; AntiEliteSalience = p.AntiEliteSalience; CivLibLawOrder = p.CivLibLawOrder; Nationalism = p.Nationalism;
        }

        /// <summary>The origin's five stats and its rule, preset (§2.2).</summary>
        public void ApplyOrigin(PartyOrigin origin)
        {
            Origin = origin;
            PartyOrigins.Preset p = PartyOrigins.Of(origin);
            Funding = p.Funding; Organisation = p.Organisation; Recognition = p.Recognition; Activists = p.Activists; Leader = p.Leader;
        }
    }

    /// <summary>
    /// §671: **THE SIX ORIGINS AS DATA** (spec §2.2) - each a five-stat profile as pips and its special rule, and **THE PIP-TO-PARAMETER TABLE**, the
    /// value each pip sets in the campaign engine. ⚠ Every value here is [AUTHORED-DRAFT] game design, on the play-calibration list - only play can
    /// judge whether a Grassroots start is an uphill battle or a wall. The profiles' words are the spec's; the numbers under them are drafts.
    /// </summary>
    public static class PartyOrigins
    {
        public readonly struct Preset
        {
            public readonly PartyOrigin Origin;
            public readonly string Title;
            public readonly int Funding, Organisation, Recognition, Activists, Leader;
            public readonly string Rule;
            public Preset(PartyOrigin origin, string title, int funding, int organisation, int recognition, int activists, int leader, string rule)
            { Origin = origin; Title = title; Funding = funding; Organisation = organisation; Recognition = recognition; Activists = activists; Leader = leader; Rule = rule; }
        }

        /// <summary>[AUTHORED-DRAFT] the six profiles - the spec's words (high / low / middling) as pips, 5 high, 1 low, 3 middling.</summary>
        public static readonly Preset[] All =
        {
            new Preset(PartyOrigin.Grassroots, "Grassroots movement", 1, 2, 1, 5, 3, "none - the pure newcomer; the hardest start"),
            new Preset(PartyOrigin.Splinter, "Splinter party", 3, 1, 4, 3, 3, "a parent party: begins at the parent's positions, inherits a chosen slice of the parent's last result as its prior; the parent declares against it"),
            new Preset(PartyOrigin.Protest, "Protest movement", 2, 1, 5, 4, 3, "positions locked toward the people-versus-elite end"),
            new Preset(PartyOrigin.BusinessTechnocrats, "Business-backed technocrats", 5, 3, 3, 1, 3, "none"),
            new Preset(PartyOrigin.SingleIssue, "Single-issue party", 3, 3, 3, 3, 3, "one issue chosen: raised salience among the voter groups that care about it, and little elsewhere"),
            new Preset(PartyOrigin.Regional, "Regional party", 2, 5, 2, 3, 3, "support concentrated in one chosen region, through the regional layer"),
        };

        public static Preset Of(PartyOrigin origin)
        {
            foreach (Preset p in All) { if (p.Origin == origin) { return p; } }
            throw new ArgumentOutOfRangeException(nameof(origin), origin, "no such origin");
        }

        /// <summary>[AUTHORED-DRAFT] the pip-to-parameter table (spec §2.2): what one to five pips set, per stat, in the engine the stat names -
        /// Funding the opening war chest (W-B2, share of the largest real party's), Organisation the offices at the start (W-B4), Recognition the
        /// baseline name awareness (W-B9, 0-1), Activists the volunteer capacity (W-B11, share of the largest real party's), Leader the leader's
        /// debate and appeal attribute (W-B7, 0-1). Indexed [pips - 1].</summary>
        public static readonly double[] FundingShare = { 0.02, 0.05, 0.10, 0.20, 0.35 };
        public static readonly int[] Offices = { 0, 1, 2, 4, 6 };
        public static readonly double[] Awareness = { 0.05, 0.15, 0.30, 0.50, 0.70 };
        public static readonly double[] ActivistShare = { 0.05, 0.15, 0.30, 0.50, 0.80 };
        public static readonly double[] LeaderAttribute = { 0.30, 0.40, 0.50, 0.60, 0.70 };

        public static double Pip(double[] table, int pips) => table[Math.Max(1, Math.Min(5, pips)) - 1];
        public static int Pip(int[] table, int pips) => table[Math.Max(1, Math.Min(5, pips)) - 1];
    }

    /// <summary>
    /// §671: **THE CREATED PARTIES OF THIS GAME, REGISTERED PER COUNTRY.** `PartySystems.For` appends them after the country's real parties, so every
    /// positional table keyed by the real roster keeps its index; `PartySystems.TryHistory` pads their history (a newcomer's prior 0 and no loyal base -
    /// the vote model's own logic, §2.7; a Splinter's prior its slice of the parent's last result, moved from the parent). ⚠ With none registered
    /// nothing reads differently: `For` returns the very array it always did, which is what keeps the no-policy path byte-inert.
    /// <para>The game's own state, shared by every world the game holds (the played one, the shadow baseline, the preview forks - each should see the
    /// party). A new game clears it; a load replaces it with the save's (`SaveGameService.RestoreInto`); a check that registers one does so
    /// inside <see cref="Scope"/>.</para>
    /// </summary>
    public static class CreatedParties
    {
        private static readonly Dictionary<CountryId, List<CreatedParty>> ByCountry = new Dictionary<CountryId, List<CreatedParty>>();
        private static readonly Dictionary<CountryId, PoliticalParty[]> Rosters = new Dictionary<CountryId, PoliticalParty[]>();
        private static readonly Regex AsciiKey = new Regex(@"^[A-Z][A-Z0-9]{0,5}$", RegexOptions.CultureInvariant);

        /// <summary>Raised when the set changes - the ink cache and anything else keyed by a roster listen.</summary>
        public static event Action Changed;

        public static IReadOnlyList<CreatedParty> Of(CountryId country) => ByCountry.TryGetValue(country, out List<CreatedParty> l) ? l : (IReadOnlyList<CreatedParty>)Array.Empty<CreatedParty>();

        public static bool Any(CountryId country) => ByCountry.TryGetValue(country, out List<CreatedParty> l) && l.Count > 0;

        /// <summary>Every registered party, for the save.</summary>
        public static List<CreatedParty> All()
        {
            var all = new List<CreatedParty>();
            foreach (List<CreatedParty> l in ByCountry.Values) { all.AddRange(l); }
            return all;
        }

        /// <summary>Registers a created party, or says why not: the key must be ASCII capitals and digits (the persisted-key rule, §566) and collide
        /// with no roster key, no other created party's key and no Splinter parent's absence.</summary>
        public static bool TryRegister(CreatedParty party, IReadOnlyList<PoliticalParty> realRoster, out string refused)
        {
            refused = null;
            if (party == null) { refused = "no party"; return false; }
            if (string.IsNullOrEmpty(party.Key) || !AsciiKey.IsMatch(party.Key)) { refused = $"the key '{party.Key}' is not 1-6 ASCII capitals and digits, beginning with a letter"; return false; }
            foreach (PoliticalParty p in realRoster) { if (string.Equals(p.Abbrev, party.Key, StringComparison.OrdinalIgnoreCase)) { refused = $"the key '{party.Key}' is a real party's"; return false; } }
            foreach (CreatedParty c in Of(party.Country)) { if (string.Equals(c.Key, party.Key, StringComparison.OrdinalIgnoreCase)) { refused = $"the key '{party.Key}' is already a created party's"; return false; } }
            if (party.Origin == PartyOrigin.Splinter)
            {
                bool parent = false;
                foreach (PoliticalParty p in realRoster) { if (p.Abbrev == party.ParentKey) { parent = true; } }
                if (!parent) { refused = $"a Splinter needs a real parent party; '{party.ParentKey}' is none"; return false; }
                if (party.InheritedSlice < 0.0 || party.InheritedSlice > 1.0) { refused = "a Splinter's inherited slice is a share of its parent's result, 0-1"; return false; }
            }
            if (!ByCountry.TryGetValue(party.Country, out List<CreatedParty> list)) { list = new List<CreatedParty>(); ByCountry[party.Country] = list; }
            list.Add(party);
            Rosters.Remove(party.Country);
            Changed?.Invoke();
            return true;
        }

        public static void Clear()
        {
            if (ByCountry.Count == 0) { return; }
            ByCountry.Clear();
            Rosters.Clear();
            Changed?.Invoke();
        }

        /// <summary>The roster with the created parties appended - built once per change; null where none is registered (the caller keeps its own array).</summary>
        public static PoliticalParty[] Extend(CountryId country, PoliticalParty[] real)
        {
            if (!Any(country)) { return null; }
            if (Rosters.TryGetValue(country, out PoliticalParty[] cached)) { return cached; }
            List<CreatedParty> created = ByCountry[country];
            var all = new PoliticalParty[real.Length + created.Count];
            Array.Copy(real, all, real.Length);
            for (int i = 0; i < created.Count; i++) { all[real.Length + i] = created[i].ToParty(); }
            Rosters[country] = all;
            return all;
        }

        /// <summary>A check's registrations, undone on dispose - the process's set is put back whatever happens.</summary>
        public static IDisposable Scope() => new Restore(All());

        private sealed class Restore : IDisposable
        {
            private readonly List<CreatedParty> _saved;
            public Restore(List<CreatedParty> saved) { _saved = saved; }
            public void Dispose()
            {
                ByCountry.Clear();
                Rosters.Clear();
                foreach (CreatedParty c in _saved)
                {
                    if (!ByCountry.TryGetValue(c.Country, out List<CreatedParty> l)) { l = new List<CreatedParty>(); ByCountry[c.Country] = l; }
                    l.Add(c);
                }
                Changed?.Invoke();
            }
        }
    }
}
