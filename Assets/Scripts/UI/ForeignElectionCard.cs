using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;

namespace PoliSim.UI
{
    /// <summary>
    /// S6 (Design's return of 2026-10-30, `send/s6_return/RETURN.md` and `S6 Foreign Election.dc.html`; K320): <b>A FOREIGN ELECTION'S CARD, READ</b> - its state,
    /// the count, who governs, the formation's steps and the dates, every figure read off the engine's records of that election and government: the count as
    /// `WorldCalendar.Result` holds it, the country's government, and the formation's projection the world ran (`SimulationManager.LastCalendarFormation`) where
    /// it is still held for that election. Pure: it reads, stores nothing and writes nothing; the Docket draws it (`GameController.ForeignResultCard.cs`).
    /// <list type="bullet">
    /// <item><description><b>The states</b> (RETURN.md's table, with the two its table leaves out): UPCOMING (not yet held, from its campaign's start), HELD (a count
    /// no procedure forms a government after - the US House - or a count a later one superseded), FORMING (the formation owed or running), FORMED (the government
    /// the procedure formed after the count), CARETAKER (the procedure ended with none - S11's stamp for the same state).</description></item>
    /// <item><description><b>The steps</b>: the cell the government formed at is read off its own record - the words the formation wrote in its basis
    /// (<see cref="StepOfBasis"/>), saved with the game; the earlier cells' parties and the vote come from the projection this world ran, which is not saved:
    /// after a load the earlier cells draw refused without a mark (S11's fallback) and the vote is not printed. Where neither holds a step, the row says ABSENT.</description></item>
    /// </list>
    /// </summary>
    public sealed class ForeignElectionCard
    {
        public enum State { Upcoming, Held, Forming, Formed, Caretaker }

        public WorldCalendar.Result Result;
        public State Is;
        /// <summary>The state's stamp word.</summary>
        public string Stamp;
        /// <summary>The entry's state at the right of its caption (RETURN.md: "in 38 days" / "held · day 12" / "formed 12 Apr 2029").</summary>
        public string When;
        /// <summary>The count's head ("The Bundestag", "The House", "The Camera" - whose seats the rows are).</summary>
        public string ChamberHead;
        public int Total, Majority;

        // ---- who governs ----
        public readonly List<string> CabinetParties = new List<string>();
        public string Standing;
        public int CabinetSeats = -1;
        /// <summary>The steps' label (the procedure's article); null where no formation follows the count (the US House).</summary>
        public string StepLabel;
        public int StepLimit;
        public readonly List<GovernmentFormation.RoundCell> Cells = new List<GovernmentFormation.RoundCell>();
        /// <summary>No record holds the steps of this formation (see the class).</summary>
        public bool StepsAbsent;
        public string StepFigure;
        /// <summary>One fragment on how it ended (RETURN.md: "first ballot · 318 for").</summary>
        public string Ended;

        // ---- the dates ----
        public readonly List<(string Label, string Value)> Dates = new List<(string, string)>();

        // ---- the slips (K320 (5): the words the stand-in carried at rest move here) ----
        public SlipContent EntrySlip, CountSlip, GovernsSlip;
        public readonly Dictionary<string, SlipContent> RowSlips = new Dictionary<string, SlipContent>();

        /// <summary>The count was held: the rows carry seats (an upcoming card's print "—").</summary>
        public bool Counted => Result != null && !Result.Upcoming;

        /// <summary>SOURCED - Art. 63 GG's three steps (Abs. 2 the first ballot, Abs. 3 the fourteen days, Abs. 4 the last ballot), as the projection counts them.</summary>
        private const int ChancellorBallots = 3;

        /// <summary>SOURCED - the Sejm's three steps (Konstytucja Art. 154 ust. 1-2, ust. 3, Art. 155 ust. 1), as `SejmNightSteps` counts them.</summary>
        private const int SejmSteps = 3;

        public static string Day(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

        private static string Short(DateTime d) => d.ToString("d MMM", CultureInfo.InvariantCulture);

        private static string N(int n) => n.ToString(CultureInfo.InvariantCulture);

        /// <summary>A seat figure, or "—" where none is held.</summary>
        private static string S(int n) => n < 0 ? "—" : N(n);

        /// <summary>
        /// The card of <paramref name="r"/> on <paramref name="today"/>, in <paramref name="country"/> (the country that held it, in the world the player plays);
        /// <paramref name="formation"/>: that country's last formation as the world ran it (null where the world holds none - a load), read only where it is this
        /// election's.
        /// </summary>
        public static ForeignElectionCard Read(WorldCalendar.Result r, Country country, DateTime today, FormationProjection formation)
        {
            var c = new ForeignElectionCard { Result = r, Total = r.TotalSeats };
            var seats = new List<int>();
            foreach (WorldCalendar.Row row in r.Rows) { seats.Add(r.Upcoming ? row.SeatsBefore : row.Seats); }
            c.Majority = r.TotalSeats > 0 ? CoalitionMath.Majority(seats.ToArray()) : 0;
            c.ChamberHead = r.Chamber == "House" ? "The House" : r.Country == CountryId.Italy ? "The Camera" : "The " + r.Chamber;
            if (formation != null && formation.PollingDay.Date != r.Day.Date) { formation = null; }
            GovernmentRecord g = country?.Government;

            ElectionRecord counted = null;
            if (r.Upcoming) { c.Is = State.Upcoming; }
            else if (!r.Formation || g == null) { c.Is = State.Held; }
            else if (!WorldCalendar.TryLatestCount(country, out counted) || counted.Date.Date != r.Day.Date) { c.Is = State.Held; counted = null; }
            else if (WorldCalendar.FormationEnded(country, counted) == null) { c.Is = State.Forming; }
            else { c.Is = WorldCalendar.FormedAfter(country, counted) ? State.Formed : State.Caretaker; }
            c.Stamp = c.Is == State.Upcoming ? "UPCOMING" : c.Is == State.Held ? "HELD" : c.Is == State.Forming ? "FORMING" : c.Is == State.Formed ? "FORMED" : "CARETAKER";

            int since = (int)(today.Date - r.Day.Date).TotalDays, until = -since;
            switch (c.Is)
            {
                case State.Upcoming: c.When = until <= 0 ? "today" : until == 1 ? "in 1 day" : "in " + N(until) + " days"; break;
                case State.Formed: c.When = "formed " + Day(g.FormedOn); break;
                case State.Caretaker: c.When = "caretaker since " + Day(g.CaretakerSince); break;
                default: c.When = since <= 0 ? "held today" : "held · day " + N(since); break;
            }

            if (r.Formation) { ReadFormation(c, r, country, g, counted, today, formation); }
            else { ReadHouse(c, r, country, g, today); }
            ReadSlips(c, r, country, g, today, formation);
            return c;
        }

        // ------------------------------------------------------------------------------------------------------------------------------- who governs

        /// <summary>A chamber whose government its procedure forms: the government sitting (the outgoing one, the one formed, the caretaker), its seats in the chamber
        /// seated now, the steps, how it ended, the dates.</summary>
        private static void ReadFormation(ForeignElectionCard c, WorldCalendar.Result r, Country country, GovernmentRecord g, ElectionRecord counted, DateTime today, FormationProjection formation)
        {
            CountryId id = r.Country;
            c.StepLabel = StepLabelOf(id);
            c.StepLimit = StepLimitOf(id);
            if (g != null)
            {
                c.CabinetParties.AddRange(g.Cabinet);
                c.CabinetSeats = SeatsOf(country, g.Cabinet);
            }
            string kind = g != null && g.Cabinet.Count > 1 ? "coalition" : "single party";
            int since = (int)(today.Date - r.Day.Date).TotalDays;
            switch (c.Is)
            {
                case State.Upcoming:
                    c.Standing = "Outgoing · " + kind;
                    Pad(c, 0);
                    c.StepFigure = "—";
                    c.Ended = g?.PmParty != null ? "led by " + PartySystems.ShortName(id, g.PmParty) : "the outgoing cabinet";
                    c.Dates.Add(("Polling day", Day(r.Day)));
                    c.Dates.Add(("Seated", Day(r.Seated)));
                    return;
                case State.Held:
                    // a count a later one superseded: who governs belongs to the later count's card
                    c.CabinetParties.Clear();
                    c.CabinetSeats = -1;
                    c.Standing = "A later count";
                    c.StepLabel = null;
                    c.Ended = WorldCalendar.TryLatestCount(country, out ElectionRecord later) ? "see " + Day(later.Date) : string.Empty;
                    c.Dates.Add(("Held", Day(r.Day)));
                    c.Dates.Add(("Seated", Day(r.Seated)));
                    return;
                case State.Forming:
                    c.Standing = (g != null && g.Caretaker ? "Caretaker · " : "Outgoing · ") + kind;
                    if (g?.Round != null && g.Round.Open && g.Round.Bundestag)
                    {
                        // Art. 63 past its first ballot (`SpeakerRound.Phase`, saved): the steps behind refused, the current one the party asked
                        int phase = Math.Max(2, Math.Min(ChancellorBallots, g.Round.Phase));
                        for (int i = 1; i < phase; i++) { c.Cells.Add(new GovernmentFormation.RoundCell(i == 1 ? formation?.FirstAsked : null, GovernmentFormation.RoundCellState.Refused)); }
                        c.Cells.Add(new GovernmentFormation.RoundCell(g.Round.Asked, GovernmentFormation.RoundCellState.Current));
                        Pad(c, c.Cells.Count);
                        c.Ended = phase == 2 && g.Round.SecondPhaseUntil != DateTime.MinValue ? "fourteen days · to " + Short(g.Round.SecondPhaseUntil) : "last ballot next";
                    }
                    else
                    {
                        Pad(c, 0);
                        c.Ended = "outgoing serves on";
                    }
                    c.StepFigure = "day " + N(Math.Max(0, since));
                    c.Dates.Add(("Held", Day(r.Day)));
                    c.Dates.Add(("Seated", Day(r.Seated)));
                    c.Dates.Add(("Formed", "—"));
                    return;
                case State.Formed:
                {
                    c.Standing = OutcomeWords(g);
                    int step = StepOfBasis(id, g.Basis);
                    if (id == CountryId.Poland && step == 0 && formation != null) { step = SejmNightSteps.StepOfProjection(formation); }
                    if (step == 0 && formation?.View != null)
                    {
                        // a basis whose words this reader does not know: the projection's own current cell, where the world still holds it
                        for (int i = 0; i < formation.View.Round.Count; i++) { if (formation.View.Round[i].State == GovernmentFormation.RoundCellState.Current) { step = i + 1; } }
                    }
                    if (step > 0)
                    {
                        for (int i = 1; i <= step && i <= c.StepLimit; i++)
                        {
                            GovernmentFormation.RoundCell? seen = CellOf(formation, i);
                            if (i < step) { c.Cells.Add(new GovernmentFormation.RoundCell(seen.HasValue && seen.Value.State != GovernmentFormation.RoundCellState.NotReached ? seen.Value.Asked : null, GovernmentFormation.RoundCellState.Refused)); }
                            else { c.Cells.Add(new GovernmentFormation.RoundCell(g.PmParty, GovernmentFormation.RoundCellState.Current)); }
                        }
                        Pad(c, c.Cells.Count);
                    }
                    else { c.StepsAbsent = true; }
                    c.StepFigure = N(Math.Max(0, (int)(g.FormedOn.Date - r.Day.Date).TotalDays)) + " days";
                    c.Ended = step > 0 ? StepWord(id, step) + VoteWords(id, step, formation) : string.Empty;
                    c.Dates.Add(("Held", Day(r.Day)));
                    c.Dates.Add(("Seated", Day(r.Seated)));
                    c.Dates.Add(("Formed", Day(g.FormedOn)));
                    return;
                }
                default:   // Caretaker
                {
                    c.Standing = "Caretaker · " + kind;
                    if (formation?.View != null && formation.View.Round.Count > 0) { foreach (GovernmentFormation.RoundCell cell in formation.View.Round) { c.Cells.Add(cell); } }
                    else if (id == CountryId.Poland && formation != null)
                    {
                        // S11's fallback where none is expected: the earlier steps refused without a mark, the third the President's refused cabinet
                        c.Cells.Add(new GovernmentFormation.RoundCell(null, GovernmentFormation.RoundCellState.Refused));
                        c.Cells.Add(new GovernmentFormation.RoundCell(null, GovernmentFormation.RoundCellState.Refused));
                        c.Cells.Add(new GovernmentFormation.RoundCell(formation.RefusedPmParty, GovernmentFormation.RoundCellState.LastRefused));
                    }
                    else { c.StepsAbsent = true; }
                    if (!c.StepsAbsent) { Pad(c, c.Cells.Count); }
                    c.StepFigure = N(Math.Max(0, (int)(g.CaretakerSince.Date - r.Day.Date).TotalDays)) + " days";
                    bool extra = WorldCalendar.TryExtraElection(country, out DateTime extraDay, out DateTime _, out string _);
                    c.Ended = extra ? "new election " + Day(extraDay) : "no government formed";
                    c.Dates.Add(("Held", Day(r.Day)));
                    c.Dates.Add(("Seated", Day(r.Seated)));
                    c.Dates.Add(extra ? ("New poll", Day(extraDay)) : ("Formed", "—"));
                    return;
                }
            }
        }

        /// <summary>The US House: no formation (RETURN.md: "Its middle pane prints the chamber majority ... and no formation · seated 3 Jan 2027").</summary>
        private static void ReadHouse(ForeignElectionCard c, WorldCalendar.Result r, Country country, GovernmentRecord g, DateTime today)
        {
            string top = null;
            int topSeats = 0;
            foreach (WorldCalendar.Row row in r.Rows)
            {
                int s = r.Upcoming ? row.SeatsBefore : row.Seats;
                if (s > topSeats) { top = row.Key; topSeats = s; }
            }
            bool majority = top != null && topSeats >= c.Majority;
            if (top != null) { c.CabinetParties.Add(top); }
            c.CabinetSeats = top != null ? topSeats : -1;
            c.Standing = majority ? "House majority · " + PartySystems.ShortName(r.Country, top) : "No House majority";
            c.StepLabel = null;
            c.Ended = r.Seated != DateTime.MinValue ? "no formation · seated " + Day(r.Seated) : "no formation";
            c.Dates.Add((r.Upcoming ? "Polling day" : "Held", Day(r.Day)));
            if (r.Seated != DateTime.MinValue) { c.Dates.Add(("Seated", Day(r.Seated))); }
        }

        private static void Pad(ForeignElectionCard c, int from)
        {
            for (int i = from; i < c.StepLimit; i++) { c.Cells.Add(new GovernmentFormation.RoundCell(null, GovernmentFormation.RoundCellState.NotReached)); }
        }

        /// <summary>The projection's own cell <paramref name="step"/> (1-based), where the projection holds one.</summary>
        private static GovernmentFormation.RoundCell? CellOf(FormationProjection formation, int step)
        {
            List<GovernmentFormation.RoundCell> cells = formation?.View?.Round;
            return cells != null && step >= 1 && step <= cells.Count ? cells[step - 1] : (GovernmentFormation.RoundCell?)null;
        }

        /// <summary>The vote the projection took at <paramref name="step"/>, as " · N for" - the Bundestag's and the Riksdag's board vote, Italy's Camera, the Sejm's
        /// confidence (`FormationProjection.VotesFor`); empty where the projection is not held (a load) or its vote is another step's (the Bundestag's first
        /// ballot, where the government came from the fourteen days or the last ballot).</summary>
        private static string VoteWords(CountryId id, int step, FormationProjection formation)
        {
            if (formation == null || !formation.Expected) { return string.Empty; }
            if (id == CountryId.Poland) { return SejmNightSteps.StepOfProjection(formation) == step && formation.VotesFor > 0 ? " · " + N(formation.VotesFor) + " for" : string.Empty; }
            if (formation.View == null || formation.View.Votes.Count == 0) { return string.Empty; }
            if (id == CountryId.Germany && step != 1) { return string.Empty; }
            return " · " + N(formation.View.Votes[0].For) + " for";
        }

        /// <summary>
        /// The step the government formed at, read off the words the formation wrote in its basis - saved with the game, so it survives a load the projection does
        /// not: the AI install's (`SimulationManager.FormAsAi`: "... - {Step}", the projection's own step words) and the chancellor's election's own install
        /// (`SimulationManager.Install`: "(Art. 63 Abs. 2|3 GG)", the last ballot's "Art. 63 Abs. 4"). 0 where the basis names none this reader knows.
        /// </summary>
        public static int StepOfBasis(CountryId id, string basis)
        {
            if (string.IsNullOrEmpty(basis)) { return 0; }
            switch (id)
            {
                case CountryId.Germany:
                    if (basis.IndexOf("Art. 63 Abs. 4", StringComparison.Ordinal) >= 0) { return 3; }
                    if (basis.IndexOf("Art. 63 Abs. 3", StringComparison.Ordinal) >= 0) { return 2; }
                    return basis.IndexOf("FIRST BALLOT", StringComparison.Ordinal) >= 0 || basis.IndexOf("Art. 63 Abs. 2", StringComparison.Ordinal) >= 0 ? 1 : 0;
                case CountryId.Poland:
                    if (basis.IndexOf("ART. 155 UST. 1", StringComparison.Ordinal) >= 0) { return 3; }
                    if (basis.IndexOf("ART. 154 UST. 3", StringComparison.Ordinal) >= 0) { return 2; }
                    return basis.IndexOf("ART. 154 UST. 2", StringComparison.Ordinal) >= 0 ? 1 : 0;
                case CountryId.Sweden:
                case CountryId.Italy:
                    Match m = Regex.Match(basis, id == CountryId.Sweden ? @"PROPOSAL (\d+) OF AT MOST" : @"MANDATE (\d+) OF AT MOST");
                    return m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0;
                default:
                    return 0;
            }
        }

        /// <summary>The steps' label - the procedure's own article, as each procedure cites itself (`FormationProjection.Procedure`).</summary>
        public static string StepLabelOf(CountryId id) =>
            id == CountryId.Germany ? "Art. 63" : id == CountryId.Sweden ? "RF 6 kap." : id == CountryId.Italy ? "Art. 92, 94" : id == CountryId.Poland ? "Art. 154–155" : null;

        /// <summary>How many steps the procedure takes at most - the Speaker's proposals (RF 6 kap. 5 §), Italy's mandates (K155), Art. 63's ballots, the Sejm's steps.</summary>
        public static int StepLimitOf(CountryId id) =>
            id == CountryId.Sweden ? SpeakerRound.ProposalLimit : id == CountryId.Italy ? ConfidenceProcedure.ItalianMandates : id == CountryId.Poland ? SejmSteps : ChancellorBallots;

        /// <summary>A step in words, as the nights name them (the night's step word; S11's steps).</summary>
        public static string StepWord(CountryId id, int step) =>
            id == CountryId.Germany ? (step == 1 ? "first ballot" : step == 2 ? "fourteen days" : "last ballot")
            : id == CountryId.Poland ? (step == 1 ? "President's step" : step == 2 ? "Sejm's step" : "second step")
            : id == CountryId.Italy ? "mandate " + N(step) : "proposal " + N(step);

        /// <summary>The procedure's rule in one fragment, for the slip.</summary>
        private static string RuleWords(CountryId id) =>
            id == CountryId.Germany ? "Art. 63: three ballots at most"
            : id == CountryId.Sweden ? "RF 6 kap.: four proposals at most"
            : id == CountryId.Italy ? "Three mandates, then dissolution (art. 88)"
            : id == CountryId.Poland ? "Art. 154–155: three steps at most" : string.Empty;

        /// <summary>The government's standing in the election night's vocabulary (K284, `EnumNames.Of`).</summary>
        private static string OutcomeWords(GovernmentRecord g) =>
            g != null && Enum.TryParse(g.Outcome, out CoalitionOutcomeKind kind) ? EnumNames.Of(kind) : "Government formed";

        private static int SeatsOf(Country country, IEnumerable<string> keys)
        {
            int total = 0;
            if (country?.ParliamentSeats == null) { return 0; }
            foreach (string k in keys) { if (country.ParliamentSeats.TryGetValue(k, out int s)) { total += s; } }
            return total;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------- the slips

        /// <summary>K320 (5): what the stand-in said at rest - the title's verb and the midterm, the headline's clause, the foot's two captions, who governs in a
        /// sentence - moves to the entry's, the count's, a row's and who governs' slips; nothing is deleted.</summary>
        private static void ReadSlips(ForeignElectionCard c, WorldCalendar.Result r, Country country, GovernmentRecord g, DateTime today, FormationProjection formation)
        {
            CountryId id = r.Country;
            string name = country?.Name ?? id.ToString();
            string open = r.Upcoming ? "Open shows the chamber now" : "Results opens the card";
            string cabinet = g != null && g.Cabinet.Count > 0 ? FormationProjection.Join(id, g.Cabinet) : "no cabinet";

            // the entry
            c.EntrySlip = new SlipContent(name + " · " + r.Chamber);
            if (r.Rows.Count == 0)
            {
                c.EntrySlip.Add(r.Headline).Add(g != null && g.Kind != WorldClock.ExecutiveKind.Presidency ? cabinet + " · the cabinet in office" : r.Governs).Add("Information · holds no clock");
            }
            else if (r.Upcoming)
            {
                c.EntrySlip.Add("Polling day · " + Day(r.Day)).Add((r.Formation ? cabinet + " · " : c.Standing + ", ") + S(c.CabinetSeats) + " of " + N(c.Total) + " now").Add("Seated " + Day(r.Seated)).Add(open + " · holds no clock");
            }
            else if (!r.Formation)
            {
                // the US House: the stand-in's title said "at the midterm", its line "the Senate class counted, no Senate seated abroad"
                if (r.Elected.IndexOf("midterm", StringComparison.Ordinal) >= 0) { c.EntrySlip.Add("At the midterm"); }
                c.EntrySlip.Add("Senate class counted").Add("No Senate seated abroad").Add(open + " · holds no clock");
            }
            else
            {
                WorldCalendar.Row first = r.Rows[0];
                c.EntrySlip.Add(first.Name + " first, " + N(first.Seats) + " of " + N(c.Total));
                c.EntrySlip.Add(c.Is == State.Formed ? "Formed " + Day(g.FormedOn) + " · " + cabinet
                    : c.Is == State.Caretaker ? "Caretaker · " + c.Ended
                    : c.Is == State.Forming ? "Forming · " + cabinet + " serves on" : "A later count since");
                if (r.OfRecord) { c.EntrySlip.Add("Chamber of record · no prediction"); }
                else if (r.SenatoLargest != null) { c.EntrySlip.Add("Senato: " + PartySystems.ShortName(id, r.SenatoLargest) + " " + N(r.SenatoSeats) + " of " + N(r.SenatoTotal)); }
                c.EntrySlip.Add(open + " · holds no clock");
            }

            // the count
            c.CountSlip = new SlipContent(c.ChamberHead + " · " + N(c.Total));
            if (r.Upcoming)
            {
                c.CountSlip.Add("Not yet held · polling " + Day(r.Day)).Add("The chamber sitting now").Add("The tick: " + N(c.Majority) + ", a majority").Add("Seats print — until counted");
            }
            else
            {
                c.CountSlip.Add("Change against the chamber before").Add("Share and swing in points");
                if (!r.Formation) { c.CountSlip.Add("Senate class counted · not seated"); }
                else { c.CountSlip.Add("The tick: " + N(c.Majority) + ", a majority"); }
                if (r.OfRecord) { c.CountSlip.Add("Chamber of record · no prediction"); }
                else if (r.SenatoLargest != null) { c.CountSlip.Add("Senato: " + PartySystems.ShortName(id, r.SenatoLargest) + " " + N(r.SenatoSeats) + " of " + N(r.SenatoTotal)); }
                else if (r.Seated != DateTime.MinValue) { c.CountSlip.Add("Seated " + Day(r.Seated)); }
            }

            // each row: its swing, which the row at rest does not print (RETURN.md's row: mark, code, seats, change, share)
            foreach (WorldCalendar.Row row in r.Rows)
            {
                var slip = new SlipContent(row.Name + " · " + N(r.Upcoming ? row.SeatsBefore : row.Seats));
                if (r.Upcoming) { slip.Add(N(row.SeatsBefore) + " seats now").Add("Not yet counted"); }
                else
                {
                    int change = row.Seats - row.SeatsBefore;
                    slip.Add(Signed(change) + " on the chamber before, " + N(row.SeatsBefore));
                    if (double.IsNaN(row.Share)) { slip.Add("Share not on record"); }
                    else if (double.IsNaN(row.ShareBefore)) { slip.Add(Pct(row.Share) + " % · no earlier share"); }
                    else { slip.Add(Pct(row.Share) + " % · swing " + SignedPoints(100.0 * (row.Share - row.ShareBefore)) + " pts"); }
                }
                c.RowSlips[row.Key] = slip;
            }

            // who governs
            switch (c.Is)
            {
                case State.Upcoming when r.Formation:
                    c.GovernsSlip = new SlipContent("Who governs now").Add(cabinet + " · " + S(c.CabinetSeats) + " of " + N(c.Total)).Add(OutcomeWords(g))
                        .Add("In office since " + Day(g?.FormedOn ?? r.Day)).Add("Until the new chamber forms");
                    break;
                case State.Held when !r.Formation:
                case State.Upcoming:
                    c.GovernsSlip = new SlipContent("Who governs · the House").Add(c.Standing + ", " + S(c.CabinetSeats) + " of " + N(c.Total)).Add(c.Ended)
                        .Add(UsCongressElection.IsMidterm(r.Day) ? "No presidential vote this year" : "The president counted beside it").Add(WorldCalendar.GovernsLine(country, today));
                    break;
                case State.Held:
                    c.GovernsSlip = new SlipContent("A later count").Add("Who governs is on its card").Add(WorldCalendar.GovernsLine(country, today));
                    break;
                case State.Forming:
                    c.GovernsSlip = new SlipContent("Forming · day " + N(Math.Max(0, (int)(today.Date - r.Day.Date).TotalDays)))
                        .Add(cabinet + " serves on · " + S(c.CabinetSeats) + " of " + N(c.Total));
                    if (g?.Round != null && g.Round.Open && g.Round.Bundestag)
                    {
                        c.GovernsSlip.Add("First ballot failed · Art. 63 Abs. 3");
                        if (g.Round.Asked != null) { c.GovernsSlip.Add("Asked now · " + PartySystems.ShortName(id, g.Round.Asked)); }
                    }
                    else { c.GovernsSlip.Add("Formation at the first sitting, " + Short(r.Seated)).Add("Run whole that day · DECLARED"); }
                    c.GovernsSlip.Add(RuleWords(id));
                    break;
                case State.Formed:
                {
                    c.GovernsSlip = new SlipContent("Formed · " + Day(g.FormedOn)).Add(cabinet + " · " + S(c.CabinetSeats) + " of " + N(c.Total));
                    GovernmentFormation.NightVote vote = formation?.View != null && formation.View.Votes.Count > 0 ? formation.View.Votes[0] : null;
                    if (!string.IsNullOrEmpty(c.Ended) && vote != null && c.Ended.EndsWith(" for", StringComparison.Ordinal) && id != CountryId.Poland)
                    {
                        c.GovernsSlip.Add(N(vote.For) + " for · " + N(vote.Against) + " against · " + N(vote.Needed) + (vote.CountsAgainst ? " refuses" : " needed"));
                    }
                    else if (formation == null) { c.GovernsSlip.Add("Vote · read in the session that ran it"); }
                    c.GovernsSlip.Add(g.PmParty != null ? "Led by " + PartySystems.ShortName(id, g.PmParty) + (g.Support.Count > 0 ? " · support " + FormationProjection.Join(id, g.Support) : string.Empty) : OutcomeWords(g));
                    c.GovernsSlip.Add(RuleWords(id));
                    break;
                }
                default:   // Caretaker
                {
                    c.GovernsSlip = new SlipContent("Caretaker · since " + Day(g.CaretakerSince)).Add(cabinet + " serves on · " + S(c.CabinetSeats) + " of " + N(c.Total));
                    string failure = WorldCalendar.FormationFailure(g, g.CaretakerSince);
                    if (failure != null && SejmNightSteps.CountedWords(failure) <= 10) { c.GovernsSlip.Add(Upper(failure)); }
                    c.GovernsSlip.Add(Upper(c.Ended)).Add(RuleWords(id));
                    break;
                }
            }
            if (c.StepsAbsent && c.GovernsSlip.Lines.Count < 4) { c.GovernsSlip.Add("ABSENT · the steps not saved"); }
        }

        private static string Upper(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        private static string Pct(double share) => (100.0 * share).ToString("0.0", CultureInfo.InvariantCulture);

        /// <summary>A seat change as the card prints it: ▲ GOOD, ▼ BAD, ±0 level (RETURN.md's row).</summary>
        public static string Arrowed(int change) => change > 0 ? "▲" + N(change) : change < 0 ? "▼" + N(-change) : "±0";

        private static string Signed(int change) => change > 0 ? "+" + N(change) : change < 0 ? "−" + N(-change) : "±0";

        private static string SignedPoints(double pp) => (pp >= 0.0 ? "+" : "−") + Math.Abs(pp).ToString("0.0", CultureInfo.InvariantCulture);

        // ------------------------------------------------------------------------------------------------------------------------- the polling days

        /// <summary>One row of the polling days (RETURN.md 6c, right): a country's chamber or office, its next polling day, how far off, its state.</summary>
        public sealed class PollingRow
        {
            public CountryId Country;
            public string Code;
            public string Chamber;
            /// <summary>MinValue where the game holds no election for it (France, K162): the row draws ABSENT.</summary>
            public DateTime Day = DateTime.MinValue;
            public string In;
            /// <summary>YOURS, UPCOMING or FORMING; null where the day is ABSENT and the row is not the player's.</summary>
            public string Stamp;
            public bool Own;
            public SlipContent Slip;
        }

        /// <summary>
        /// THE NEXT POLLING DAY OF EVERY COUNTRY (DERIVED): each chamber's from the statute's rule as the engine reads it, an extra election its procedure owes
        /// replacing it the day it is ordered (`WorldCalendar.TryNextChamberDay`), the US Congress and president (`WorldClock.TryNextPollingDay`), Poland's president
        /// (`PresidentialElection.TryNextFirstVote`), the player's own from the manager's (<paramref name="ownChamberDay"/>, its extra election with it). France holds
        /// none (K162): its two rows draw ABSENT. The player's rows lead; the rest follow by date.
        /// </summary>
        public static List<PollingRow> PollingDays(World world, CountryId player, DateTime today, DateTime ownChamberDay)
        {
            var own = new List<PollingRow>();
            var rest = new List<PollingRow>();
            if (world == null) { return own; }
            foreach (Country c in world.Countries)
            {
                bool mine = c.Id == player;
                List<PollingRow> into = mine ? own : rest;
                string code = MapRenderer.TagOf(c.Id);
                if (WorldCalendar.SeatedByRecordAlone(c.Id))
                {
                    foreach (string office in new[] { "Président", "Assemblée" })
                    {
                        into.Add(new PollingRow { Country = c.Id, Code = code, Chamber = office, Own = mine, Stamp = mine ? "YOURS" : null,
                            Slip = new SlipContent(code + " · " + office).Add("No election modelled · K162").Add("Record seated to " + Day(WorldCalendar.FranceRecordEnds)).Add("Held as of record after it") });
                    }
                    continue;
                }
                if (UsCongressElection.ElectsIn(c.Id))
                {
                    DateTime from = today.Date;
                    if (WorldClock.TryNextPollingDay(c.Id, from, out DateTime us) && HeldUs(c, us)) { WorldClock.TryNextPollingDay(c.Id, us.AddDays(1), out us); }
                    if (mine && ownChamberDay != DateTime.MinValue) { us = ownChamberDay; }
                    if (us != DateTime.MinValue)
                    {
                        if (!UsCongressElection.IsMidterm(us)) { into.Add(Row(c, code, "President", us, today, mine, "The president · electors by state", "The statute's day · DERIVED")); }
                        into.Add(Row(c, code, "Congress", us, today, mine, UsCongressElection.IsMidterm(us) ? "The House · at the midterm" : "The House · with the president", "Senate class counted · not seated"));
                    }
                    continue;
                }
                if (mine && ownChamberDay != DateTime.MinValue)
                {
                    into.Add(Row(c, code, WorldCalendar.ChamberWord(c.Id), ownChamberDay, today, true, "Your own polling day", "The statute's day · DERIVED"));
                }
                else if (!mine && WorldCalendar.TryNextChamberDay(c, today, out DateTime day, out bool extra))
                {
                    PollingRow row = Row(c, code, WorldCalendar.ChamberWord(c.Id), day, today, false, extra ? "Extra election · K161" : "The statute's day · DERIVED", "Seated " + Day(WorldCalendar.FirstSittingAfter(c, day)));
                    if (WorldCalendar.TryLatestCount(c, out ElectionRecord latest) && WorldCalendar.FormationEnded(c, latest) == null)
                    {
                        row.Stamp = "FORMING";
                        row.Slip.Add("Forming after " + Day(latest.Date));
                    }
                    into.Add(row);
                }
                if (PresidentialElection.TryNextFirstVote(c, today, out DateTime first, out DateTime termEnds, out string _))
                {
                    into.Add(Row(c, code, "President", first, today, mine, "First round · run-off if no majority", "Term ends " + Day(termEnds)));
                }
            }
            own.Sort((a, b) => Order(a).CompareTo(Order(b)));
            rest.Sort((a, b) => Order(a) != Order(b) ? Order(a).CompareTo(Order(b)) : string.CompareOrdinal(a.Code + a.Chamber, b.Code + b.Chamber));
            own.AddRange(rest);
            return own;
        }

        private static DateTime Order(PollingRow r) => r.Day == DateTime.MinValue ? DateTime.MaxValue : r.Day;

        private static PollingRow Row(Country c, string code, string chamber, DateTime day, DateTime today, bool mine, string first, string second) =>
            new PollingRow
            {
                Country = c.Id, Code = code, Chamber = chamber, Day = day.Date, In = InWords(today, day), Own = mine, Stamp = mine ? "YOURS" : "UPCOMING",
                Slip = new SlipContent(code + " · " + chamber).Add(Day(day) + " · in " + InWords(today, day)).Add(first).Add(second),
            };

        private static bool HeldUs(Country country, DateTime day)
        {
            if (country.UsCongressElections != null) { foreach (UsCongressElection.Contest k in country.UsCongressElections) { if (k.PollingDay.Date == day.Date) { return true; } } }
            return false;
        }

        /// <summary>How far off a day is, in the polling days' column (RETURN.md: "58 days", "13 mo", "2 y 5 mo"): days under two months, months under two years,
        /// then years and months.</summary>
        public static string InWords(DateTime today, DateTime day)
        {
            int days = (int)(day.Date - today.Date).TotalDays;
            if (days <= 0) { return "today"; }
            if (days < 60) { return days == 1 ? "1 day" : N(days) + " days"; }
            int months = (day.Year - today.Year) * 12 + day.Month - today.Month - (day.Day < today.Day ? 1 : 0);
            if (months < 24) { return N(months) + " mo"; }
            int y = months / 12, m = months % 12;
            return m == 0 ? N(y) + " y" : N(y) + " y " + N(m) + " mo";
        }
    }
}
