using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// SP-7b (`docs/specs/POLAND_PRESIDENTIAL_START_PLAN.md` §5a and §6; Elias's ruling I14, 2026-10-18, installed §857): POLAND'S PRESIDENTIAL START - the
    /// player as a candidate of record, the world opened on the Marshal's order.
    /// <list type="bullet">
    /// <item><b>Ask 1 (a), with the factor of record standing.</b> The player is a candidate of record backed by a roster party - the five of the 2025 field
    /// whose backing party sits in the Sejm (<see cref="PresidencyOfRecord.CandidatesOf"/>: Nawrocki PiS, Trzaskowski KO, Mentzen Konf, Hołownia TD, Biejat
    /// NL); the player's party is the backing party, so the player also holds its role in the Sejm from the first day. The other eight of record are shown
    /// and not pickable, the reason in one line (the plan's words: no party in the Sejm, nothing a player could move). The candidate keeps the factor of
    /// record: the field and its shares are the game's election's as built (`PresidentialElection`); the contest only marks which candidate is the
    /// player's (`PresidentialElection.Contest.PlayerCandidate`).</item>
    /// <item><b>Ask 4 (4b), a stated deviation for presidential starts only.</b> The world opens on <see cref="PolandOpens"/>, the day the Marshal ordered
    /// the election and the campaign opened, not on §618's standard run-up; the Sejm starts keep §618's rule (`WorldClock.StartDate` is unchanged). The card
    /// and the brief say so (`StartPoints.ModeLine`, `StartBrief`).</item>
    /// <item><b>Ask 3 (3a).</b> No campaign is staged: the candidate plays through the backing party's role until the first vote; the card and the brief
    /// say it plainly. A Polish campaign is PL-C's (filed), not this start's.</item>
    /// </list>
    /// The start's key (<see cref="KeyOf"/>) rides the save on the player's country (<see cref="Country.PlayerStart"/>) beside the candidate
    /// (<see cref="Country.PlayerCandidate"/>) - format 50.
    /// </summary>
    public static class PresidentialStart
    {
        /// <summary>SOURCED - the Marshal's order, Postanowienie Marszałka Sejmu z dnia 15 stycznia 2025 r. w sprawie zarządzenia wyborów Prezydenta
        /// Rzeczypospolitej Polskiej, Dz.U. 2025 poz. 48, announced, promulgated and in force 2025-01-15 (its ELI metadata,
        /// `ElectionsData/poland/raw/president_start_2025/eli_meta_DU_2025_48.json`); the campaign opens on its publication (Kodeks wyborczy art. 104;
        /// `ElectionsData/poland/presidential_calendar_2025.md` §1). RULED (I14, ask 4 (4b)): Poland's presidential start opens on it.</summary>
        public static readonly DateTime PolandOpens = new DateTime(2025, 1, 15);

        /// <summary>Whether a start point is a candidate's start - Poland's presidential card, playable (the USA's presidential start seats a party, and its own
        /// nominee is SP-6a's; France's is locked until S8).</summary>
        public static bool IsCandidateStart(StartPoints.StartPoint p) => p.Playable && p.Presidential && p.Country == CountryId.Poland;

        /// <summary>The start's key as the save carries it: the election's kind and its year ("PRESIDENTIAL ELECTION 2025"). Null on the country's default
        /// start, which is every save cut before format 50.</summary>
        public static string KeyOf(StartPoints.StartPoint p) => p.Kind + " " + p.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>One candidate the picker shows: the name as the game prints it, the backing party (null where none sits in the Sejm), whether the
        /// player may pick the candidate, the party's role at the start in the picker's words, and - where not pickable - why.</summary>
        public readonly struct Offer
        {
            public readonly string Name, Surname, Party, Role, Reason;
            public readonly bool Pickable;
            public Offer(string name, string surname, string party, bool pickable, string role, string reason)
            {
                Name = name; Surname = surname; Party = party; Pickable = pickable; Role = role; Reason = reason;
            }
        }

        /// <summary>The one line a candidate of record no Sejm party backs carries on the picker - the plan's words (ask 1, Code's recommendation, ruled).</summary>
        public const string UnbackedReason = "NO PARTY IN THE SEJM BACKS THE CANDIDATE · NOTHING YOU COULD MOVE";

        /// <summary>The field of record the start offers, in the record's order: the five a roster party backs pickable, each with its party's role on the
        /// start's day; the rest not pickable, with <see cref="UnbackedReason"/>. Empty for a start that is not a candidate's.</summary>
        public static List<Offer> Offers(StartPoints.StartPoint p)
        {
            var offers = new List<Offer>();
            if (!IsCandidateStart(p)) { return offers; }
            WorldClock.TryGovernmentAt(p.Country, p.Opens, out WorldClock.GovernmentOfRecord government);
            foreach (PresidencyOfRecord.CandidateOfRecord c in PresidencyOfRecord.CandidatesOf(p.Country, p.Year))
            {
                string name = PresidencyOfRecord.NameOfRecord(p.Country, p.Year, c.Surname);
                bool seated = c.BackingParty != null && SitsAtStart(p, c.BackingParty);
                offers.Add(new Offer(name, c.Surname, c.BackingParty, seated, seated ? RoleWords(government, c.BackingParty) : null, seated ? null : UnbackedReason));
            }
            return offers;
        }

        /// <summary>The offer for <paramref name="name"/> on start <paramref name="p"/>; false where the start offers no such candidate.</summary>
        public static bool TryOffer(StartPoints.StartPoint p, string name, out Offer offer)
        {
            foreach (Offer o in Offers(p)) { if (o.Name == name) { offer = o; return true; } }
            offer = default;
            return false;
        }

        /// <summary>
        /// Seats the player as <paramref name="name"/> on start <paramref name="p"/>: the backing party as the player's party (its approval stock opened at the
        /// country's, as the party picker seats one), the candidate and the start's key on the country. False, with why, where the candidate is not pickable
        /// or the country is not the start's.
        /// </summary>
        public static bool Seat(Country country, StartPoints.StartPoint p, string name, out string why)
        {
            why = null;
            if (country == null || country.Id != p.Country) { why = "the start is another country's"; return false; }
            if (!TryOffer(p, name, out Offer offer)) { why = "the start offers no such candidate"; return false; }
            if (!offer.Pickable) { why = offer.Reason; return false; }
            country.PlayerPartyAbbrev = offer.Party;
            country.PartyApprovalRating = country.State.ApprovalRating;
            country.PlayerCandidate = offer.Name;
            country.PlayerStart = KeyOf(p);
            return true;
        }

        /// <summary>Whether <paramref name="party"/> holds a seat in the chamber seated on the start's day (the 10th Sejm, by `WorldClock.SeatedVintage`).</summary>
        private static bool SitsAtStart(StartPoints.StartPoint p, string party)
        {
            Dictionary<string, int> seats = PartySystems.InitialSeats(p.Country, WorldClock.SeatedVintage(p.Country, p.Opens));
            return seats.TryGetValue(party, out int n) && n > 0;
        }

        /// <summary>The party's role in the government of record on the start's day, in the picker's words: the prime minister's party (the cabinet's head),
        /// a junior partner, a support party, or the opposition.</summary>
        public static string RoleWords(WorldClock.GovernmentOfRecord government, string party)
        {
            if (government.Cabinet != null && government.Cabinet.Length > 0)
            {
                if (government.Cabinet[0] == party) { return "THE PRIME MINISTER'S PARTY"; }
                if (Array.IndexOf(government.Cabinet, party) >= 0) { return "JUNIOR PARTNER"; }
            }
            if (government.Support != null && Array.IndexOf(government.Support, party) >= 0) { return "IN SUPPORT"; }
            return "IN OPPOSITION";
        }
    }
}
