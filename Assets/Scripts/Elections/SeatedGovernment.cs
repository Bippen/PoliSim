using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// K-1 part (4), ordered 2026-09-23: **WHO GOVERNS ON DAY ONE.** *"The Riksdag convenes 28 September; until the real government
    /// is installed, the formation model's result on the 2026 chamber stands in, marked provisional, replaced when the Riksdag votes."*
    ///
    /// <para><b>What is stored, and what is not.</b> The game has never stored a government: `GovernmentFormation` derives it from
    /// the chamber and the declarations every time it is asked, and that stays true. What this adds is the STANDING of the seeded
    /// chamber's government - a sourced, dated fact about the world outside the game - per country:</para>
    /// <list type="bullet">
    /// <item><description><see cref="Standing.Provisional"/>: the real government is not yet installed, so the formation model's result
    /// stands in and every surface that names it says PROVISIONAL. Sweden, from K-1: the chamber elected 2026-09-13 had not voted
    /// on a prime minister when the seed was cut (`ElectionsData/sweden/2026/government_2026.md`).</description></item>
    /// <item><description><see cref="Standing.Installed"/>: the real government is on record - its cabinet and its support, sourced -
    /// and it is the day-one government instead of the formation's. No country carries one yet. When the Riksdag votes, the record
    /// is written here from the vote and the refresh is data only (K-1b); <see cref="GovernmentFormation"/> already reads it.</description></item>
    /// </list>
    ///
    /// <para><b>How long it holds.</b> The standing belongs to the SEEDED chamber. Once the game holds an election of its own, the
    /// chamber and its government are the game's, and neither mark applies.</para>
    /// </summary>
    public static class SeatedGovernment
    {
        public enum Standing { Provisional, Installed }

        public readonly struct Record
        {
            public readonly Standing Standing;
            /// <summary>The cabinet's parties by key - null while provisional (the formation's result stands in).</summary>
            public readonly string[] Cabinet;
            /// <summary>The parties carrying it from outside, by key - null while provisional.</summary>
            public readonly string[] Support;
            /// <summary>The source and its date: for a provisional record, why the real government is not yet on record.</summary>
            public readonly string Basis;
            public readonly DateTime AsOf;

            public Record(Standing standing, string[] cabinet, string[] support, string basis, DateTime asOf)
            {
                if (string.IsNullOrEmpty(basis)) { throw new ArgumentException("a seated government's standing needs its source"); }
                if (standing == Standing.Installed && (cabinet == null || cabinet.Length == 0)) { throw new ArgumentException("an installed government names its cabinet"); }
                Standing = standing; Cabinet = cabinet; Support = support; Basis = basis; AsOf = asOf;
            }
        }

        /// <summary>The seeded chamber's government standing, where one is on record.</summary>
        public static bool TryFor(CountryId id, out Record record)
        {
            switch (id)
            {
                case CountryId.Sweden:
                    record = new Record(Standing.Provisional, null, null,
                        "PROVISIONAL as of 2026-09-23: the Riksdag elected 2026-09-13 has chosen no prime minister. Kristersson was dismissed at his own "
                        + "request on 2026-09-17 and his ministers serve as a caretaker government (övergångsregering) until a new one takes office "
                        + "([RG-ART], [RD-N17b]); the new Riksdag convenes 2026-09-28 (RF 3:10, [RD-N19]); the talman gave Andersson (S) a sounding "
                        + "mandate on 2026-09-18 ([RD-N18]); a prime minister can be chosen at the earliest after the opening on 2026-09-29 ([RG-ART], "
                        + "RF 6:4-6:5). Until then the formation model's result on the 2026 chamber stands in. See ElectionsData/sweden/2026/government_2026.md.", new DateTime(2026, 9, 23));
                    return true;
                default:
                    record = default;
                    return false;
            }
        }

        /// <summary>Whether the game has held an election of its own in this country - after which its chamber, and its government, are the game's.</summary>
        public static bool HeldElection(Country country)
        {
            if (country?.ElectionHistory == null) { return false; }
            foreach (ElectionRecord record in country.ElectionHistory) { if (record.Method != ElectionMethod.NotImplemented) { return true; } }
            return false;
        }

        /// <summary>Whether the country's government is the provisional stand-in: its seed says so and the game has not yet voted its own chamber in.</summary>
        public static bool IsProvisional(Country country) =>
            country != null && TryFor(country.Id, out Record record) && record.Standing == Standing.Provisional && !HeldElection(country);

        /// <summary>The installed government for the seeded chamber, where the real one is on record and the game has not replaced the chamber.</summary>
        public static bool TryInstalled(Country country, out Record record)
        {
            record = default;
            if (country == null || !TryFor(country.Id, out Record onRecord) || onRecord.Standing != Standing.Installed || HeldElection(country)) { return false; }
            record = onRecord;
            return true;
        }

        /// <summary>
        /// An installed record as the formation's result on a chamber: its cabinet and support as masks over <paramref name="parties"/>,
        /// the outcome named by the arithmetic (§29's own kinds), and everyone else counted against - a real investiture's opposition
        /// is its vote record, which this does not carry. False, with the reason, when the record names a party the chamber does not seat.
        /// </summary>
        public static bool TryAsResult(Record record, IReadOnlyList<PoliticalParty> parties, int[] seats, out CoalitionResult result, out string reason)
        {
            result = null; reason = null;
            int cabinet = Mask(record.Cabinet, parties, out string missing);
            if (missing != null) { reason = $"the installed cabinet names '{missing}', which this chamber does not seat"; return false; }
            int support = Mask(record.Support, parties, out missing);
            if (missing != null) { reason = $"the installed support names '{missing}', which this chamber does not seat"; return false; }
            int total = 0, cabinetSeats = 0, supportSeats = 0;
            for (int p = 0; p < parties.Count; p++)
            {
                total += seats[p];
                if ((cabinet & (1 << p)) != 0) { cabinetSeats += seats[p]; }
                else if ((support & (1 << p)) != 0) { supportSeats += seats[p]; }
            }
            int majority = CoalitionMath.Majority(seats);
            int supported = cabinetSeats + supportSeats;
            CoalitionOutcomeKind kind = cabinetSeats >= majority ? CoalitionOutcomeKind.MajorityCoalition
                : support != 0 && supported >= majority ? CoalitionOutcomeKind.ConfidenceAndSupply
                : CoalitionOutcomeKind.MinorityGovernment;
            var government = new GovernmentOption(cabinet, support, kind, cabinetSeats, supported, total - supported, 0.0, 0.0);
            result = new CoalitionResult { Outcome = kind, Government = government, Majority = majority, NegotiatingPower = CoalitionMath.NegotiatingPower(seats) };
            result.Viable.Add(government);
            return true;
        }

        private static int Mask(string[] keys, IReadOnlyList<PoliticalParty> parties, out string missing)
        {
            missing = null;
            int mask = 0;
            if (keys == null) { return 0; }
            foreach (string key in keys)
            {
                int found = -1;
                for (int p = 0; p < parties.Count; p++) { if (parties[p].Abbrev == key) { found = p; break; } }
                if (found < 0) { missing = key; return 0; }
                mask |= 1 << found;
            }
            return mask;
        }
    }
}
