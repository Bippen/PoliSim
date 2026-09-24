using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>The player's party's role toward the sitting government - the political-system spec's §5.2 four roles.</summary>
    public enum PlayerRole
    {
        /// <summary>No party seated, or no government stored.</summary>
        None,
        /// <summary>The prime minister's party: the player governs.</summary>
        PrimeMinister,
        /// <summary>A junior coalition partner - in the cabinet, not leading it.</summary>
        JuniorPartner,
        /// <summary>A support party: carrying the cabinet from outside (confidence and supply).</summary>
        Support,
        Opposition,
    }

    /// <summary>
    /// PS-3a (2026-09-25, §628; the political-system spec's §5.2-§5.3, §9 stage 3): WHO GOVERNS, STORED. Until this record the game never kept
    /// who governs - it re-formed the government from the seats on every call, and "AI-governed" was a test on the COUNTRY (is it the
    /// player's?), never on the player's role. This is one record per country, written when the world seats the government of record at
    /// its start and again when the formation forms one after the game's own election, saved with the country, and read by the one test
    /// that decides whose levers move the book: <c>SimulationManager.PlayerGoverns</c>.
    ///
    /// <para><b>The prime minister's party is a premise, stated.</b> The model carries no prime minister. Where the record names the head of
    /// government, its party is read from the record (Kristersson (M)); where the formation formed the cabinet, the party of a declared
    /// own-leader candidacy standing in the cabinet leads it (K-1f's premise), else the largest cabinet party.</para>
    /// </summary>
    public sealed class GovernmentRecord
    {
        public List<string> Cabinet = new List<string>();
        public List<string> Support = new List<string>();
        /// <summary>The prime minister's party by key - the premise above.</summary>
        public string PmParty;
        public string Outcome;
        public DateTime FormedOn;
        /// <summary>True while the record is the formation's stand-in for a government the record does not yet hold (§605).</summary>
        public bool Provisional;
        public string Basis;

        public PlayerRole RoleOf(string abbrev)
        {
            if (string.IsNullOrEmpty(abbrev)) { return PlayerRole.None; }
            if (abbrev == PmParty) { return PlayerRole.PrimeMinister; }
            if (Cabinet.Contains(abbrev)) { return PlayerRole.JuniorPartner; }
            if (Support.Contains(abbrev)) { return PlayerRole.Support; }
            return PlayerRole.Opposition;
        }

        /// <summary>The government the world seats at a country's start: the record's where its cabinet is sourced, else the formation's on the seated chamber, provisional.</summary>
        public static GovernmentRecord AtStart(Country country, DateTime start)
        {
            if (SeatedGovernment.TryAt(country.Id, start, out SeatedGovernment.Record record) && record.Standing == SeatedGovernment.Standing.Installed && record.Cabinet != null)
            {
                var installed = new GovernmentRecord { FormedOn = record.AsOf, Provisional = false, Basis = record.Basis, Outcome = "of record" };
                installed.Cabinet.AddRange(record.Cabinet);
                if (record.Support != null) { installed.Support.AddRange(record.Support); }
                installed.PmParty = HeadParty(country.Id, start) ?? Largest(country, installed.Cabinet);
                return installed;
            }
            // No government of record on this date at all (France, the USA - the record holds no cabinet for them): NO record, and the player governs their
            // own country, as before. A stand-in formed on a chamber whose record names no government would seat a cabinet the record never held (the review).
            if (!WorldClock.TryGovernmentAt(country.Id, start, out WorldClock.GovernmentOfRecord _)) { return null; }
            GovernmentFormation.View formed = GovernmentFormation.ViewOf(country);
            return FromView(country, formed, start, provisional: true, basis: "the formation's result on the seated chamber - the government of record names no cabinet on this date (§605)");
        }

        /// <summary>The government the formation formed after the game's own election (or none: a record with an empty cabinet and the reason).</summary>
        public static GovernmentRecord FromView(Country country, GovernmentFormation.View view, DateTime formedOn, bool provisional = false, string basis = null)
        {
            var record = new GovernmentRecord { FormedOn = formedOn, Provisional = provisional, Basis = basis ?? "the formation on the chamber the game elected" };
            if (view == null || !view.HasGovernment) { record.Outcome = "none"; record.Basis = view?.Reason ?? record.Basis; return record; }
            record.Outcome = view.Outcome.ToString();
            foreach ((string abbrev, int _) in view.Cabinet) { record.Cabinet.Add(abbrev); }
            foreach ((string abbrev, int _) in view.Support) { record.Support.Add(abbrev); }
            // K-1f's premise: a declared own-leader candidacy standing in the cabinet leads it; else the largest cabinet party.
            foreach ((string abbrev, string _, string _) in DeclaredRedLines.CandidaciesAt(country.Id, formedOn))
            {
                if (record.Cabinet.Contains(abbrev)) { record.PmParty = abbrev; break; }
            }
            record.PmParty ??= Largest(country, record.Cabinet);
            return record;
        }

        /// <summary>The head of government's party from the record's own line, e.g. "Ulf Kristersson (M)" - null where the record names none.</summary>
        private static string HeadParty(CountryId id, DateTime date)
        {
            if (!WorldClock.TryGovernmentAt(id, date, out WorldClock.GovernmentOfRecord government) || string.IsNullOrEmpty(government.Head)) { return null; }
            int open = government.Head.IndexOf('(');
            int close = open >= 0 ? government.Head.IndexOf(')', open) : -1;
            if (open < 0 || close < 0) { return null; }
            string key = government.Head.Substring(open + 1, close - open - 1).Trim();
            foreach (PoliticalParty party in PartySystems.For(id)) { if (party.Abbrev == key) { return key; } }
            return null;
        }

        /// <summary>One log line for a country's record at a date, or the absence of one.</summary>
        public static string Describe(CountryId id, DateTime date, Country country)
        {
            GovernmentRecord g = country?.Government;
            if (g == null) { return $"ROLE: {id} at {date:yyyy-MM-dd} - no government of record; the player governs their own country"; }
            return $"ROLE: {id} at {date:yyyy-MM-dd} - the government {(g.Cabinet.Count > 0 ? string.Join("+", g.Cabinet) : "none")} led by {g.PmParty ?? "-"}{(g.Support.Count > 0 ? " with " + string.Join("+", g.Support) : string.Empty)}{(g.Provisional ? " (provisional)" : string.Empty)}; the player's {country.PlayerPartyAbbrev ?? "(no party)"} is {g.RoleOf(country.PlayerPartyAbbrev)}";
        }

        private static string Largest(Country country, List<string> cabinet)
        {
            string best = null; int bestSeats = -1;
            foreach (string abbrev in cabinet)
            {
                int seats = country.ParliamentSeats.TryGetValue(abbrev, out int n) ? n : 0;
                if (seats > bestSeats) { best = abbrev; bestSeats = seats; }
            }
            return best;
        }
    }
}
