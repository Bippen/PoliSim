using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;

namespace PoliSim.UI
{
    /// <summary>
    /// K197: WHAT THE POLISH DESK VERBS LEFT ON THE RECORD, read for the Docket and the top strip - pure functions of the country and the day, so
    /// `PolishDeskVerbsDiagnostic` reads them on the worlds it plants. Two readings, each for <see cref="WorldCalendar.ShownForDays"/> days (the Docket's
    /// "the last month", K163's idiom of information that never holds the clock):
    /// <para>(1) THE SEJM'S ART. 158 VOTES - every constructive vote the Sejm took (the player's motion or an AI's after an exit), read off the division log
    /// the vote appends (`SimulationManager.MoveNoConfidence`, `TryAiConstructiveVote`): a motion whose title is `ConfidenceProcedure.MotionVote.Title`'s
    /// Art. 158 form; the mover is the side the vote records as having moved it.</para>
    /// <para>(2) THE PLAYER'S LEAVING - the day the government's record carries the line the verb writes (`SimulationManager.LeftTheGovernmentLine`), while
    /// that government still governs; a successor installed by an Art. 158 vote replaces the record, and (1) tells that story.</para>
    /// </summary>
    public static class PolishDeskRecord
    {
        /// <summary>The citation every Art. 158 vote's title ends with (`ConfidenceProcedure.MotionVote.Title`).</summary>
        public const string Art158Citation = "(Art. 158 Konstytucja)";

        /// <summary>Whether the country's confidence rules are the Sejm's - the only place these readings apply.</summary>
        public static bool Applies(Country country) => country != null && ConfidenceProcedure.RulesOf(country.Id) == ConfidenceProcedure.Rules.Sejm;

        private static bool Recent(DateTime day, DateTime today) => day.Date <= today.Date && (today.Date - day.Date).TotalDays < WorldCalendar.ShownForDays;

        /// <summary>(1): the Art. 158 votes of the last month, newest first.</summary>
        public static List<DivisionRecord> RecentArt158(Country country, DateTime today)
        {
            var votes = new List<DivisionRecord>();
            if (!Applies(country) || country.Divisions == null) { return votes; }
            foreach (DivisionRecord d in country.Divisions.Entries)
            {
                if (d != null && d.Motion && d.Title != null && d.Title.EndsWith(Art158Citation, StringComparison.Ordinal) && Recent(d.Date, today)) { votes.Add(d); }
            }
            votes.Reverse();
            return votes;
        }

        /// <summary>(1): the party that moved a constructive vote - its recorded side - or null.</summary>
        public static string MoverOf(DivisionRecord vote)
        {
            if (vote?.Sides == null) { return null; }
            foreach (DivisionSide side in vote.Sides) { if (side.Reason != null && side.Reason.StartsWith("moved it", StringComparison.Ordinal)) { return side.Abbrev; } }
            return null;
        }

        /// <summary>(1): the deputies for a recorded vote - the seats of every side that stood for it.</summary>
        public static int ForOf(DivisionRecord vote)
        {
            int n = 0;
            if (vote?.Sides != null) { foreach (DivisionSide side in vote.Sides) { if (side.Side > 0) { n += side.Seats; } } }
            return n;
        }

        /// <summary>(1): the deputies counted in a recorded vote - every side's seats, the title's own denominator (the seated chamber).</summary>
        public static int MembersOf(DivisionRecord vote)
        {
            int n = 0;
            if (vote?.Sides != null) { foreach (DivisionSide side in vote.Sides) { n += side.Seats; } }
            return n;
        }

        /// <summary>(1): the vote's name as the Docket and the strip print it - "Art. 158 · PiS's candidate elected".</summary>
        public static string Art158Words(Country country, DivisionRecord vote)
        {
            string mover = MoverOf(vote);
            string who = mover != null ? PoliSim.Wording.EnglishText.Possessive(PartySystems.ShortName(country.Id, mover) ?? mover, false, true) + " candidate" : "the candidate";
            return "Art. 158 · " + who + (vote.Passed ? " elected" : " not elected");
        }

        /// <summary>(2): the day the player's party left the sitting government, inside the last month - false where the record carries no such line.</summary>
        public static bool TryLeftOn(Country country, DateTime today, out DateTime day)
        {
            day = DateTime.MinValue;
            GovernmentRecord g = country?.Government;
            if (!Applies(country) || g == null || string.IsNullOrEmpty(country.PlayerPartyAbbrev)) { return false; }
            for (int i = g.Breaks.Count - 1; i >= 0; i--)
            {
                string line = g.Breaks[i];
                if (line == null || line.Length < 10 || !DateTime.TryParseExact(line.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime on)) { continue; }
                if (line != SimulationManager.LeftTheGovernmentLine(on, country.PlayerPartyAbbrev) || !Recent(on, today)) { continue; }
                day = on;
                return true;
            }
            return false;
        }

        /// <summary>(2): the government's standing as the leaving left it - "KO+NL govern on 182 of 460 seats, a minority" (the cabinet named as one group,
        /// K312's join through its helper: S10, 10a).</summary>
        public static string StandingWords(Country country)
        {
            GovernmentRecord g = country?.Government;
            if (g == null || country.ParliamentSeats == null) { return string.Empty; }
            int total = 0, held = 0;
            foreach (KeyValuePair<string, int> seat in country.ParliamentSeats)
            {
                total += seat.Value;
                if (g.Cabinet.Contains(seat.Key) || g.Support.Contains(seat.Key)) { held += seat.Value; }
            }
            var names = new List<string>();
            foreach (string k in g.Cabinet) { names.Add(PartySystems.ShortName(country.Id, k) ?? k); }
            string n(int v) => v.ToString(CultureInfo.InvariantCulture);
            return PartySystems.JoinShortNames(names) + (names.Count == 1 ? " governs on " : " govern on ") + n(held) + " of " + n(total) + " seats, " + (held * 2 > total ? "a majority" : "a minority");
        }
    }
}
