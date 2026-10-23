using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;

namespace PoliSim.Simulation
{
    /// <summary>
    /// PS-6 US-35 (D-US part two's board 30b, a PROPOSAL Elias read as the go-ahead on 2026-10-23): <b>A BILL'S PATH ON THE SIGNING PLATE, READ FROM ITS
    /// RECORDS.</b> Five stations - the two chambers in the order the bill began (House first, or the Senate first for a kind that starts there), the
    /// President, the override, the law - each with what the records hold of it: a chamber's count and the line it met; the President's answer (on the
    /// player-president's desk, vetoed, or answered by signature); the override's votes where a veto made it real, conditional before; and the law's day or the
    /// bill's death. Under them, for a bill on the desk, <b>the ten days, Sundays excepted</b> (Art. I §7 cl. 2) - the presentment day, each counted day
    /// numbered, each Sunday apart and unnumbered, and the day it is law unanswered - read from `PresidentialDocket`'s own arithmetic, never counted twice.
    /// Pure: it reads records and dates and writes nothing. <b>The pocket veto is not modelled</b> (the game holds Congress in session), and the plate says so.
    /// </summary>
    public static class UsBillPath
    {
        /// <summary>A station's state, as board 30b inks it: passed (ink), the current one (amber), conditional (dashed), failed (the bad ink), carried to law (the
        /// good ink), not reached (dashed and dimmed).</summary>
        public enum Mark { Done, Now, Conditional, Failed, Passed, Off }

        /// <summary>One station: its label, its status in the plate's capitals, the line under it (its slip's), and its mark.</summary>
        public sealed class Station
        {
            public string Label;
            public string Status;
            public string Sub;
            public Mark Mark;
        }

        /// <summary>One cell of the ten days: the day, its number in the count (0 the presentment day, 1 to 10 the counted days), or a Sunday (not counted).</summary>
        public readonly struct Day
        {
            public readonly DateTime Date;
            public readonly int Number;
            public readonly bool Sunday;

            public Day(DateTime date, int number, bool sunday) { Date = date; Number = number; Sunday = sunday; }
        }

        /// <summary>Whether a record is a Congress division a path can be read for: a US division of either chamber.</summary>
        public static bool Applies(Country country, DivisionRecord record) =>
            country != null && record != null && country.Id == CountryId.USA && record.Contest == null && (record.OtherChamber != null || Congress.Applies(country));

        /// <summary>Whether a record is a chamber's vote on the president's veto - its line the two thirds, never a cloture line.</summary>
        public static bool OnVeto(DivisionRecord record) => record != null && record.Required > 0 && !record.Cloture;

        /// <summary>
        /// THE TEN DAYS of a bill presented on <paramref name="presentedOn"/>: the presentment day (number 0), then every day to the period's last day
        /// (`PresidentialDocket.LastDay`), each Sunday apart and unnumbered, the others numbered 1 to 10. The law day is the day after the last
        /// (`PresidentialDocket.LawDay`). Empty for a country with no period.
        /// </summary>
        public static List<Day> TenDays(CountryId country, DateTime presentedOn)
        {
            var days = new List<Day>();
            DateTime last = PresidentialDocket.LastDay(country, presentedOn);
            if (last == DateTime.MinValue || !PresidentialDocket.TryPeriodOf(country, out _, out bool sundaysExcepted)) { return days; }
            days.Add(new Day(presentedOn.Date, 0, false));
            int n = 0;
            for (DateTime d = presentedOn.Date.AddDays(1); d <= last; d = d.AddDays(1))
            {
                bool sunday = sundaysExcepted && d.DayOfWeek == DayOfWeek.Sunday;
                if (!sunday) { n++; }
                days.Add(new Day(d, sunday ? -1 : n, sunday));
            }
            return days;
        }

        /// <summary>The counted days left after <paramref name="today"/> for a bill presented on <paramref name="presentedOn"/> - its period's days still to run, a
        /// Sunday not counted where excepted; 0 on and after the last day.</summary>
        public static int CountedDaysLeft(CountryId country, DateTime presentedOn, DateTime today)
        {
            int left = 0;
            foreach (Day d in TenDays(country, presentedOn)) { if (d.Number > 0 && d.Date > today.Date) { left++; } }
            return left;
        }

        /// <summary>PS-6 US-35 (the review's M3): the Docket row's countdown in the plate's capitals - LAW UNSIGNED IN N DAYS while counted days remain after
        /// today, LAW UNSIGNED TOMORROW on the period's last day (the bill is still on the desk that day, and law the next), never "in 0 days".</summary>
        public static string UnsignedWords(CountryId country, DateTime presentedOn, DateTime today)
        {
            int left = CountedDaysLeft(country, presentedOn, today);
            return left <= 0 ? "LAW UNSIGNED TOMORROW" : "LAW UNSIGNED IN " + left.ToString(CultureInfo.InvariantCulture) + (left == 1 ? " DAY" : " DAYS");
        }

        /// <summary>
        /// THE STATIONS of <paramref name="record"/>, the record the plate carries: where it is a vote on a veto, the bill's two chambers passed it, the President
        /// vetoed it and the override stands or falls; else the chambers' own counts, and where both passed it, the President - on the player-president's desk
        /// where <paramref name="onDesk"/> (the override then conditional, the law's day <paramref name="lawDay"/> if unanswered), else answered by signature.
        /// Null where `Applies` is false.
        /// </summary>
        public static List<Station> Of(Country country, DivisionRecord record, bool onDesk, DateTime lawDay, bool countsTenDays = true)
        {
            if (!Applies(country, record)) { return null; }
            var path = new List<Station>();
            int origin = record.Origin == Congress.Senate ? Congress.Senate : Congress.House;
            int other = origin == Congress.Senate ? Congress.House : Congress.Senate;
            string Day(DateTime d) => d.ToString("d MMM", CultureInfo.InvariantCulture).ToUpperInvariant();
            string Year(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();

            if (OnVeto(record))
            {
                // the vote on the veto: the bill passed both chambers and was vetoed; the origin voted on the veto first, the other only where the origin overrode
                DivisionRecord first = record.OtherChamber ?? record;
                DivisionRecord second = record.OtherChamber != null ? record : null;
                path.Add(new Station { Label = Chamber(country, origin), Status = "PASSED", Sub = "it passed and was sent on", Mark = Mark.Done });
                path.Add(new Station { Label = Chamber(country, other), Status = "PASSED", Sub = "it passed and was sent to the President", Mark = Mark.Done });
                path.Add(new Station { Label = "The President", Status = "VETOED " + Day(record.Date), Sub = "returned with objections", Mark = Mark.Failed });
                bool overridden = second != null && first.Passed && second.Passed;
                string Votes(DivisionRecord r) => Chamber(country, r.Chamber) + " " + YeasWords(country, r);
                // the review's M1: the chamber that SUSTAINED the veto, with its own votes - the second where the origin overrode and the other did not
                DivisionRecord sustaining = second != null && !second.Passed ? second : first;
                path.Add(overridden
                    ? new Station { Label = "Override", Status = "OVERRIDDEN", Sub = Votes(first) + " · " + Votes(second), Mark = Mark.Passed }
                    : new Station { Label = "Override", Status = "SUSTAINED", Sub = Votes(sustaining) + " for · " + sustaining.Required.ToString(CultureInfo.InvariantCulture) + " needed"
                        + (second != null ? " · " + Chamber(country, first.Chamber) + " overrode it" : string.Empty), Mark = Mark.Failed });
                path.Add(overridden
                    ? new Station { Label = "Law", Status = "LAW " + Year(record.Date), Sub = "over the veto", Mark = Mark.Passed }
                    : new Station { Label = "Law", Status = "DIES", Sub = "the veto stands", Mark = Mark.Off });
                return path;
            }

            DivisionRecord one = record.OtherChamber != null ? record.OtherChamber : record.Chamber == origin ? record : null;
            DivisionRecord two = record.OtherChamber != null ? record : record.Chamber == other ? record : null;
            path.Add(ChamberStation(country, origin, one));
            path.Add(one != null && !one.Passed ? Unreached(Chamber(country, other)) : ChamberStation(country, other, two));
            bool sent = one != null && one.Passed && two != null && two.Passed;
            if (!sent)
            {
                path.Add(Unreached("The President"));
                path.Add(Unreached("Override"));
                path.Add(new Station { Label = "Law", Status = "DIES", Sub = "stopped in Congress", Mark = Mark.Off });
                return path;
            }
            if (onDesk)
            {
                path.Add(new Station { Label = "The President", Status = "ON YOUR DESK", Sub = "presented " + Day(record.Date) + (countsTenDays ? " · ten days, Sundays excepted" : " · law on its own deadline"), Mark = Mark.Now });
                path.Add(new Station { Label = "Override", Status = "ONLY IF VETOED", Sub = "two thirds of each chamber voting", Mark = Mark.Conditional });
                path.Add(new Station { Label = "Law", Status = lawDay == DateTime.MinValue ? "-" : Year(lawDay), Sub = "if unanswered · today if signed", Mark = Mark.Conditional });
                return path;
            }
            path.Add(new Station { Label = "The President", Status = "SIGNED", Sub = "no veto", Mark = Mark.Done });
            path.Add(Unreached("Override"));
            path.Add(new Station { Label = "Law", Status = "LAW " + Year(record.Date), Sub = "signed", Mark = Mark.Passed });
            return path;
        }

        private static Station Unreached(string label) => new Station { Label = label, Status = "NOT REACHED", Sub = "the bill did not come this far", Mark = Mark.Off };

        /// <summary>A chamber's station from its division - its count and the line it met (a majority of those voting; cloture at three fifths of the senators
        /// sworn; a majority by reconciliation, no cloture).</summary>
        private static Station ChamberStation(Country country, int chamber, DivisionRecord d)
        {
            string label = Chamber(country, chamber);
            if (d == null) { return Unreached(label); }
            int yea = 0, nay = 0;
            foreach (DivisionSide s in d.Sides) { if (s.Side > 0) { yea += s.Seats; } else if (s.Side < 0) { nay += s.Seats; } }
            string count = d.Sides.Count == 0 ? "UNCONTESTED" : yea.ToString(CultureInfo.InvariantCulture) + "–" + nay.ToString(CultureInfo.InvariantCulture);
            string line = d.Cloture ? "cloture: " + d.Required.ToString(CultureInfo.InvariantCulture) + " of " + d.Sworn.ToString(CultureInfo.InvariantCulture) + " sworn"
                : d.Chamber == Congress.Senate ? "reconciliation: a majority, no cloture"
                : "a majority of those voting";
            return new Station { Label = label, Status = (d.Passed ? "PASSED " : "FAILED ") + count, Sub = line + " · " + d.Date.ToString("d MMM", CultureInfo.InvariantCulture), Mark = d.Passed ? Mark.Done : Mark.Failed };
        }

        /// <summary>The yeas a record holds: its FOR side; where it recorded no sides (passed unasked, every member for - `Congress`'s override count), its
        /// whole base - a vote on the veto's members (its Sworn), a passage's base as the override counts it (`OverrideBase`: the House seated, which a House
        /// passage does not record). The review's M2: -1 where the record holds no count at all, never a zero it did not count.</summary>
        /// <summary>The review's M2 (moved here from the plate's file, s905 - its one runtime caller is the plate, its other the diagnostic): a chamber's yeas on the passage as the desk's Veto line says them - the count the record holds, or that none is recorded.</summary>
        public static string PassedWith(Country country, DivisionRecord passage)
        {
            int y = UsBillPath.Yeas(country, passage);
            return y < 0 ? "an unrecorded count" : y.ToString(CultureInfo.InvariantCulture);
        }

        public static int Yeas(Country country, DivisionRecord r)
        {
            if (r == null) { return -1; }
            if (r.Sides.Count == 0)
            {
                int whole = r.Sworn > 0 ? r.Sworn : OverrideBase(country, r);
                return whole > 0 ? whole : -1;
            }
            int yea = 0;
            foreach (DivisionSide s in r.Sides) { if (s.Side > 0) { yea += s.Seats; } }
            return yea;
        }

        /// <summary>A record's yeas in a line's words - the figure, or "not recorded" where it holds none.</summary>
        public static string YeasWords(Country country, DivisionRecord r)
        {
            int y = Yeas(country, r);
            return y < 0 ? "not recorded" : y.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>The base an override would be counted on in each chamber, for a passage the President has not answered: the House's passage sides (the
        /// chamber seated where it recorded none), the Senate's sides or the senators sworn, whichever is more - `Congress`'s override count, read for the plate.</summary>
        public static int OverrideBase(Country country, DivisionRecord passage)
        {
            if (passage == null) { return 0; }
            int sides = 0;
            foreach (DivisionSide s in passage.Sides) { sides += s.Seats; }
            return passage.Chamber == Congress.Senate ? Math.Max(sides, passage.Sworn) : sides > 0 ? sides : Congress.Sworn(country?.ParliamentSeats);
        }

        private static string Chamber(Country country, int chamber) => "The " + Congress.ChamberName(country.Id, chamber);
    }
}
