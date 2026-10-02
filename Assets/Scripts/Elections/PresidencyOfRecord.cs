using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-5 with S7, part one: A DIRECTLY ELECTED PRESIDENT OF RECORD, BY DATE - who holds the office the spec models (Poland's president, whose
    /// veto is live since §761 - `PresidentialVeto`), from the day the oath was taken (Art. 128 ust. 1: the term begins on taking office) to the next's, each with the
    /// election that put them there. Sourced in `ElectionsData/poland/records_by_date.md` §2d (the National Assembly's oath protocols) and
    /// `presidential_returns.md` (the PKW's notices). A Poland run from its start (2023-02-19) sits in Duda's second term and crosses the 2025
    /// election; the president of record holds until the game's own presidential election is simulated (part two).
    /// </summary>
    public static class PresidencyOfRecord
    {
        public readonly struct President
        {
            public readonly string Name;
            public readonly DateTime TookOffice;
            /// <summary>The day the successor took office; <see cref="DateTime.MaxValue"/> while in office on the record's last day.</summary>
            public readonly DateTime Until;
            public readonly DateTime FirstVote;
            public readonly DateTime RunOff;
            public readonly string Basis;
            /// <summary>§761 (PS-5, Elias's ruling B1): the party whose deputies' vote the veto reads - "his backing party". The committee is the voters'
            /// (Kodeks wyborczy art. 84 § 3: a party cannot nominate); the backing is the party that put him forward.</summary>
            public readonly string BackingParty;
            /// <summary>Where <see cref="BackingParty"/> comes from - its source, or the reading DECLARED where none is held.</summary>
            public readonly string BackingBasis;
            public President(string name, DateTime tookOffice, DateTime until, DateTime firstVote, DateTime runOff, string basis, string backingParty, string backingBasis)
            {
                Name = name; TookOffice = tookOffice; Until = until; FirstVote = firstVote; RunOff = runOff; Basis = basis; BackingParty = backingParty; BackingBasis = backingBasis;
            }
        }

        private static DateTime D(int y, int m, int d) => new DateTime(y, m, d);

        private static readonly President[] Poland =
        {
            new President("Andrzej Duda", D(2020, 8, 6), D(2025, 8, 6), D(2020, 6, 28), D(2020, 7, 12),
                "the run-off of 2020-07-12 (Dz.U. 2020 poz. 1238; the first vote Dz.U. 2020 poz. 1163); the oath before the National Assembly 2020-08-06 [ZN-2020] [ZN-2020-META]",
                "PiS", "DECLARED - the game's reading (B1's backtest reads PiS for both presidents, §757); his 2020 backing is not sourced (presidential_returns.md, NOT REACHED)"),
            new President("Karol Nawrocki", D(2025, 8, 6), DateTime.MaxValue, D(2025, 5, 18), D(2025, 6, 1),
                "the run-off of 2025-06-01 (Dz.U. 2025 poz. 714; the first vote Dz.U. 2025 poz. 652); the oath before the National Assembly 2025-08-06 [ZN-2025-CONV] [ZN-2025] [PKW-PREZ-2025]",
                "PiS", "[SECONDARY] the PiS site's #Nawrocki2025 page (presidential_returns.md: the party's decision to put him forward)"),
        };

        /// <summary>The country's presidents of record in order; empty where none is directly elected in the record the game holds.</summary>
        public static IReadOnlyList<President> Of(CountryId id) => id == CountryId.Poland ? Poland : Array.Empty<President>();

        /// <summary>The president of record on <paramref name="date"/>; false before the first the record holds, or where none is held.</summary>
        public static bool TryAt(CountryId id, DateTime date, out President president)
        {
            foreach (President p in Of(id)) { if (date >= p.TookOffice && date < p.Until) { president = p; return true; } }
            president = default;
            return false;
        }
    }
}
