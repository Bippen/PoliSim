using System;
using System.Collections.Generic;
using System.Globalization;

namespace PoliSim.Data
{
    /// <summary>
    /// PS-6 US-33 (`docs/specs/USA_STAGE_PLAN.md`, "Your own nominee (SP-6a)"; R-US4 (a), RULED - F8 §781: PS-6 builds the player's own nominee at the head of
    /// a major ticket, the third-party run its own row, SP-6b): **THE PLAYER'S OWN NOMINEE, AS DATA** - a leader the player creates at the head of the DEM or REP
    /// ticket. Not a party: the party, its mark, its ink, its seats and its history stay the real one's (the plan's §5, "Not asked": the REP/DEM marks and inks
    /// exist), so the record carries only what the player chooses - the ticket by its key, the nominee's name, and the two positions the vote model places
    /// (CHES's economic left-right and GAL-TAN, 0-10 - the roster's two axes), each inside the party's range (<see cref="OwnNominees.TryRangeOf"/>). Saved with
    /// the game (`SaveGame.OwnNominees`) and registered with <see cref="OwnNominees"/>.
    /// </summary>
    [Serializable]
    public sealed class OwnNominee
    {
        public CountryId Country;
        /// <summary>The ticket the nominee heads - a roster party's key (DEM or REP).</summary>
        public string Party;
        /// <summary>The nominee's name, as the count and the screens print it - a new person, never one the record holds (<see cref="OwnNominees.Refusal"/>).</summary>
        public string Name;
        /// <summary>The nominee's position on the vote model's two axes, inside the party's range - the point the party's poll reads (the moved point).</summary>
        public float LrEcon, Galtan;
    }

    /// <summary>
    /// PS-6 US-33: **THE OWN NOMINEE OF THIS GAME, ITS RANGE AND ITS RULES.** One per country, the player's; the game's own state, shared by every world the game
    /// holds (the played one, the shadow baseline, the impact ledger's forks - as <see cref="CreatedParties"/> is: the nominee is who the player is, not a policy,
    /// so every world sees it). A new game clears it, a load replaces it with the save's (`SaveGameService.RestoreInto`), a check that registers one does so
    /// inside <see cref="Scope"/>. ⚠ With none registered nothing reads differently - <see cref="TryPointOf"/> answers false and the vote model places every
    /// party where it always did, which is what keeps the no-policy path and the sentinel byte-inert.
    /// <list type="bullet">
    /// <item><description><b>Where it is offered</b> (<see cref="Offered"/>): a country whose president is chosen by electors - the USA (`UsPresidentialElection.ElectsIn`).</description></item>
    /// <item><description><b>The tickets</b> (<see cref="Tickets"/>): the roster parties the Global Party Survey 2019 places (US-32's extract, `UsGps2019`) - DEM and REP.</description></item>
    /// <item><description><b>The range</b> (<see cref="TryRangeOf"/>, DECLARED - the plan says "the experts' spread" and leaves the statistic open): on each axis the
    /// middle half of the party's experts' placements, from the first quartile to the third (`UsGps2019.Rows`: V4 for the economic axis, V6 for GAL-TAN - the two
    /// items the roster carries as its two axes). The alternatives, the whole spread (minimum to maximum) and the mean within one standard deviation, are put to
    /// Elias with their figures; the party's own point lies inside each.</description></item>
    /// <item><description><b>A new person</b> (<see cref="Refusal"/>): the name does not read as one the Twenty-second Amendment's count reads from the record
    /// (`UsTermLimit.TryPersonOfRecord` - the surname and the first given name's initial, so "Donald Trump" and "Joe Biden" read as their persons of record) and
    /// is not the unnamed candidate's form, so the term limit reads the player's nominee as a person of the game's - elected only by the game's own counts.
    /// The name is kept folded (`UsTermLimit.FoldName`: one space between words, a straight apostrophe).</description></item>
    /// <item><description><b>One authority</b> (the US-33 review's L2): this registry alone decides both where the vote model places the party
    /// (<see cref="TryPointOf"/>) and that the nominee heads the field of record (`UsTermLimit.NomineesFor`), whatever seat the player holds later.</description></item>
    /// </list>
    /// </summary>
    public static class OwnNominees
    {
        private static readonly Dictionary<CountryId, OwnNominee> ByCountry = new Dictionary<CountryId, OwnNominee>();

        /// <summary>The longest name the flow takes - the creation flow's leader field's own limit.</summary>
        public const int MaxNameLength = 40;

        /// <summary>The party's range on the vote model's two axes - each the first to the third quartile of its experts' placements, with the experts counted.</summary>
        public readonly struct Range
        {
            public readonly string Party;
            public readonly float EconLo, EconHi, SocLo, SocHi;
            public readonly int EconExperts, SocExperts;
            public Range(string party, float econLo, float econHi, float socLo, float socHi, int econExperts, int socExperts)
            {
                Party = party; EconLo = econLo; EconHi = econHi; SocLo = socLo; SocHi = socHi; EconExperts = econExperts; SocExperts = socExperts;
            }

            public bool Holds(float econ, float soc) => econ >= EconLo && econ <= EconHi && soc >= SocLo && soc <= SocHi;
        }

        /// <summary>Where the player may stand a nominee of their own: a country whose president is chosen by electors - the USA alone.</summary>
        public static bool Offered(CountryId country) => Elections.UsPresidentialElection.ElectsIn(country);

        /// <summary>The tickets offered, in the roster's order: each roster party the survey places on both axes (<see cref="TryRangeOf"/>); empty where none is offered.</summary>
        public static List<string> Tickets(CountryId country)
        {
            var tickets = new List<string>();
            if (!Offered(country)) { return tickets; }
            foreach (PoliticalParty p in PartySystems.RealRoster(country)) { if (TryRangeOf(country, p.Abbrev, out Range _)) { tickets.Add(p.Abbrev); } }
            return tickets;
        }

        /// <summary>The party's range (see the class), read from the survey's rows; false where the country offers no own nominee or the survey does not place
        /// the party on both axes.</summary>
        public static bool TryRangeOf(CountryId country, string party, out Range range)
        {
            range = default;
            if (!Offered(country) || string.IsNullOrEmpty(party)) { return false; }
            float eLo = float.NaN, eHi = float.NaN, sLo = float.NaN, sHi = float.NaN;
            int eN = 0, sN = 0;
            foreach (Generated.UsGps2019.Row row in Generated.UsGps2019.Rows)
            {
                if (row.Party != party) { continue; }
                if (row.Item == "V4_Scale") { eLo = Read(row.Q1); eHi = Read(row.Q3); eN = int.Parse(row.Experts, CultureInfo.InvariantCulture); }
                else if (row.Item == "V6_Scale") { sLo = Read(row.Q1); sHi = Read(row.Q3); sN = int.Parse(row.Experts, CultureInfo.InvariantCulture); }
            }
            if (float.IsNaN(eLo) || float.IsNaN(eHi) || float.IsNaN(sLo) || float.IsNaN(sHi) || eHi < eLo || sHi < sLo) { return false; }
            range = new Range(party, eLo, eHi, sLo, sHi, eN, sN);
            return true;
        }

        private static float Read(string cell) => float.Parse(cell, NumberStyles.Float, CultureInfo.InvariantCulture);

        /// <summary>A point brought inside the party's range on each axis; unchanged where the party has no range.</summary>
        public static void Clamp(OwnNominee n)
        {
            if (n == null || !TryRangeOf(n.Country, n.Party, out Range r)) { return; }
            n.LrEcon = Math.Min(r.EconHi, Math.Max(r.EconLo, n.LrEcon));
            n.Galtan = Math.Min(r.SocHi, Math.Max(r.SocLo, n.Galtan));
        }

        /// <summary>What stops a nominee from standing - one reason, in the player's words - or null where it may stand. <paramref name="requireName"/> false
        /// reads only the ticket and the point (the flow's preview, before the player names the nominee).</summary>
        public static string Refusal(OwnNominee n, bool requireName = true)
        {
            if (n == null) { return "no nominee"; }
            if (!Offered(n.Country)) { return "this country does not choose its president by electors, so no nominee of your own stands"; }
            if (!TryRangeOf(n.Country, n.Party, out Range r)) { return "the ticket must be a major party's: " + string.Join(" or ", Tickets(n.Country)); }
            if (float.IsNaN(n.LrEcon) || float.IsNaN(n.Galtan) || float.IsInfinity(n.LrEcon) || float.IsInfinity(n.Galtan)) { return "the nominee has no position"; }
            if (!r.Holds(n.LrEcon, n.Galtan))
            {
                return string.Format(CultureInfo.InvariantCulture, "the position ({0:0.0}, {1:0.0}) is outside the party's range - economic {2:0.0} to {3:0.0}, GAL-TAN {4:0.0} to {5:0.0}",
                    n.LrEcon, n.Galtan, r.EconLo, r.EconHi, r.SocLo, r.SocHi);
            }
            if (!requireName) { return null; }
            string name = Elections.UsTermLimit.FoldName(n.Name);   // the review's L1: one space between words, a straight apostrophe - the unnamed form's ’ is its '
            if (string.IsNullOrEmpty(name)) { return "the nominee has no name"; }
            if (name.Length > MaxNameLength) { return "the name is longer than " + MaxNameLength.ToString(CultureInfo.InvariantCulture) + " characters"; }
            if (Elections.UsTermLimit.IsUnnamedLabel(name)) { return "the name is the form of a party's unnamed candidate"; }
            if (Elections.UsTermLimit.TryPersonOfRecord(name, out string ofRecord))
            {
                return (string.Equals(name, ofRecord, StringComparison.OrdinalIgnoreCase) ? name + " is a person of record" : name + " reads as " + ofRecord + ", a person of record")
                    + " - your nominee is a new person, whom only the game's own counts elect";
            }
            return null;
        }

        /// <summary>The country's own nominee, where one is registered.</summary>
        public static bool TryOf(CountryId country, out OwnNominee nominee) => ByCountry.TryGetValue(country, out nominee);

        public static bool Any(CountryId country) => ByCountry.ContainsKey(country);

        /// <summary>Every registered nominee, for the save.</summary>
        public static List<OwnNominee> All() => new List<OwnNominee>(ByCountry.Values);

        /// <summary>Registers the country's own nominee in place of any before it, or says why not (<see cref="Refusal"/>); nothing is written on a refusal.</summary>
        public static bool TryRegister(OwnNominee n, out string refused)
        {
            refused = Refusal(n);
            if (refused != null) { return false; }
            n.Name = Elections.UsTermLimit.FoldName(n.Name);
            ByCountry[n.Country] = n;
            return true;
        }

        public static void Clear() => ByCountry.Clear();

        /// <summary>
        /// THE MOVED POINT: where the vote model places <paramref name="party"/> - the own nominee's position where the country's nominee heads that party's ticket
        /// (the party's point moved to its nominee's: the poll, and so the count, reads it); false otherwise, and the model places the party where the roster does.
        /// </summary>
        public static bool TryPointOf(CountryId country, string party, out double econ, out double soc)
        {
            econ = double.NaN; soc = double.NaN;
            if (ByCountry.Count == 0 || !ByCountry.TryGetValue(country, out OwnNominee n) || n.Party != party) { return false; }
            econ = n.LrEcon;
            soc = n.Galtan;
            return true;
        }

        /// <summary>A check's registrations, undone on dispose - the process's set is put back whatever happens.</summary>
        public static IDisposable Scope() => new Restore(All());

        /// <summary>The flow's preview: <paramref name="draft"/>'s ticket and point stand for the country while the scope lives (its name is not asked - the player may
        /// not have named the nominee yet), and the set before it is put back on dispose. A draft that cannot stand installs nothing.</summary>
        public static IDisposable Preview(OwnNominee draft)
        {
            var restore = new Restore(All());
            if (draft != null && Refusal(draft, requireName: false) == null)
            {
                ByCountry[draft.Country] = new OwnNominee { Country = draft.Country, Party = draft.Party, Name = draft.Name, LrEcon = draft.LrEcon, Galtan = draft.Galtan };
            }
            return restore;
        }

        /// <summary>
        /// An INSTRUMENT'S PROBE, never a game path (the US-33 review's L5): the country's ticket <paramref name="party"/> placed at (<paramref name="econ"/>,
        /// <paramref name="soc"/>) while the scope lives, the range NOT enforced - so a check can print what a point outside today's range would do to the split
        /// (the alternative ranges put to Elias). The ticket must still be one the country offers and the point finite on the 0-10 scales; anything else installs
        /// nothing. The set before it is put back on dispose.
        /// </summary>
        public static IDisposable ProbeAt(CountryId country, string party, float econ, float soc)
        {
            var restore = new Restore(All());
            bool onScale = !float.IsNaN(econ) && !float.IsNaN(soc) && econ >= 0f && econ <= 10f && soc >= 0f && soc <= 10f;
            if (onScale && TryRangeOf(country, party, out Range _))
            {
                ByCountry[country] = new OwnNominee { Country = country, Party = party, Name = string.Empty, LrEcon = econ, Galtan = soc };
            }
            return restore;
        }

        private sealed class Restore : IDisposable
        {
            private readonly List<OwnNominee> _saved;
            public Restore(List<OwnNominee> saved) { _saved = saved; }
            public void Dispose()
            {
                ByCountry.Clear();
                foreach (OwnNominee n in _saved) { ByCountry[n.Country] = n; }
            }
        }
    }
}
