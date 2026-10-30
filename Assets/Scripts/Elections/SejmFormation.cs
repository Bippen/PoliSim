using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>AF-8: where Poland's government formation after a Sejm election stands (Konstytucja Art. 162, 154).</summary>
    public enum SejmStage
    {
        /// <summary>The count is in; the outgoing cabinet governs until the new Sejm's first sitting (Art. 109 ust. 2, Art. 162 ust. 1).</summary>
        AwaitingSitting,
        /// <summary>Art. 154 ust. 1: a prime minister is designated; the President appoints the cabinet on <see cref="SejmFormation.NextOn"/>.</summary>
        Designated,
        /// <summary>Art. 154 ust. 2: the appointed cabinet asks the Sejm's confidence on <see cref="SejmFormation.NextOn"/>.</summary>
        AwaitingConfidence,
        /// <summary>Art. 154 ust. 3: the Sejm elects a prime minister and the cabinet he proposes on <see cref="SejmFormation.NextOn"/>.</summary>
        SejmElects,
        /// <summary>Art. 154 ust. 3, last sentence: the President appoints the cabinet the Sejm elected on <see cref="SejmFormation.NextOn"/>.</summary>
        AwaitingAppointment,
        Concluded,
        /// <summary>K302: Art. 155 ust. 1 - the Sejm elected no cabinet under Art. 154 ust. 3; the President appoints one on <see cref="SejmFormation.NextOn"/>.
        /// Appended after <see cref="Concluded"/>: a save carries the stage as its number.</summary>
        PresidentAppoints,
        /// <summary>K302: Art. 155 ust. 1 - the President's cabinet asks the Sejm's confidence on <see cref="SejmFormation.NextOn"/>, carried by a simple majority;
        /// refused, the President shortens the Sejm's term and orders elections (ust. 2).</summary>
        AwaitingPresidentsConfidence,
    }

    /// <summary>
    /// AF-8 (Elias's ruling q8, 2026-10-18: "Poland's government changes by the statute's steps: the cabinet resigns at the new Sejm's first sitting
    /// (Art. 162); the first sitting falls within 30 days (Art. 109(2)), on the record's date where it has one; then Art. 154's steps"). THE SEJM'S
    /// FORMATION, DATED. The constitution as the Constitutional Tribunal serves it ([TK-KONST], `ElectionsData/poland/raw/records/trybunal_konstytucja.html`):
    /// <para>Art. 109 ust. 2: "Pierwsze posiedzenia Sejmu i Senatu Prezydent Rzeczypospolitej zwołuje na dzień przypadający w ciągu 30 dni od dnia wyborów".
    /// Art. 162 ust. 1: "Prezes Rady Ministrów składa dymisję Rady Ministrów na pierwszym posiedzeniu nowo wybranego Sejmu"; ust. 2 pkt 1: also where the
    /// Sejm withholds its confidence; ust. 3: "Prezydent Rzeczypospolitej, przyjmując dymisję Rady Ministrów, powierza jej dalsze sprawowanie obowiązków
    /// do czasu powołania nowej Rady Ministrów" - the outgoing cabinet carries on, a caretaker.</para>
    /// <para>Art. 154 ust. 1: "Prezydent Rzeczypospolitej desygnuje Prezesa Rady Ministrów, który proponuje skład Rady Ministrów. Prezydent Rzeczypospolitej
    /// powołuje Prezesa Rady Ministrów wraz z pozostałymi członkami Rady Ministrów w ciągu 14 dni od dnia pierwszego posiedzenia Sejmu"; ust. 2: within 14
    /// days of the appointment the cabinet asks the Sejm's confidence, carried "bezwzględną większością głosów w obecności co najmniej połowy ustawowej
    /// liczby posłów"; ust. 3: failing either, "Sejm w ciągu 14 dni od upływu terminów określonych w ust. 1 lub ust. 2 wybiera Prezesa Rady Ministrów oraz
    /// proponowanych przez niego członków Rady Ministrów" by the same majority, and the President appoints the cabinet so elected.</para>
    /// <para><b>The record's dates where the record has them</b> (the 10th term, `ElectionsData/poland/records_by_date.md` §2a-2b): first sitting
    /// 2023-11-13 [API-TERM]; resignation accepted and Morawiecki designated the same day (M.P. 2023 poz. 1221, 1222) [ELI-MP-2023]; appointed with his
    /// cabinet 2023-11-27 (poz. 1287, 1288); the confidence vote lost 2023-12-11 [API-V1] and the resignation accepted (poz. 1379); the Sejm elected Tusk
    /// the same day (poz. 1380; his cabinet 2023-12-12, poz. 1381); appointed 2023-12-13 (poz. 1382, 1383). The DESIGNEE is the record's too (the
    /// government of record on the appointment's day), and so is the cabinet the Sejm elects under ust. 3 (the government of record appointed after it) where
    /// its parties hold a majority of the game's own Sejm - else the projection. <b>Each vote's outcome is the game's own chamber's</b>, never the record's: a game whose count
    /// gives the record's designee a majority keeps him.</para>
    /// <para><b>DECLARED, where no record dates a step</b> (a Sejm the game elects): the first sitting on Art. 109 ust. 2's last day (ruling E2, §773); each
    /// Art. 154 step on its window's last day, as the 10th term's appointment (the sitting + 14) and confidence vote (the appointment + 14) both were; the
    /// Sejm's own election on the day the step before it fails, as on 2023-12-11; the appointment after it at the record's interval
    /// (<see cref="AppointmentAfterSejmElectionDays"/>). The designee where no record names one is the formation's projection on the new chamber
    /// (<see cref="GovernmentFormation.ViewOf(Country, DeclarationReading)"/>, the election's own declarations) - the President's choice has no statute,
    /// stated and ASKED. A vote counts the designated cabinet's and its supporters' seats for, every other seat against, every member present: the
    /// absolute majority of the votes cast is then a majority of the members.</para>
    /// <para><b>K302 (Elias, 2026-10-29): ART. 155, the Sejm's procedure to its end.</b> Art. 155 ust. 1: "W razie niepowołania Rady Ministrów w trybie art. 154
    /// ust. 3 Prezydent Rzeczypospolitej w ciągu 14 dni powołuje Prezesa Rady Ministrów i na jego wniosek pozostałych członków Rady Ministrów oraz odbiera od nich
    /// przysięgę. Sejm w ciągu 14 dni od dnia powołania Rady Ministrów przez Prezydenta Rzeczypospolitej udziela jej wotum zaufania większością głosów w obecności
    /// co najmniej połowy ustawowej liczby posłów"; ust. 2: "W razie nieudzielenia Radzie Ministrów wotum zaufania w trybie określonym w ust. 1, Prezydent
    /// Rzeczypospolitej skraca kadencję Sejmu i zarządza wybory." The President's cabinet is <see cref="PresidentAppoints(Country, DeclarationReading)"/>'s and the
    /// vote on it <see cref="SimpleMajority(Country, IReadOnlyCollection{string}, IReadOnlyCollection{string}, DeclarationReading)"/>'s; the shortened term's
    /// polling day is <see cref="ShortenedTermElectionDay"/> (Art. 98 ust. 5) and the Sejm it elects first sits by <see cref="FirstSittingAfter(Country, DateTime)"/>.
    /// Its days, DECLARED as the Art. 154 steps' are: Art. 154 ust. 3's fourteen days run out after the Sejm's vote, then the President's fourteen, each step on
    /// its window's last day (<see cref="PresidentAppointsAfterSejmVoteDays"/>); the confidence vote on its window's last day; the shortening on the day it is
    /// refused. Not modelled, stated: the Marshals' opinions the President takes before shortening a term (Art. 98 ust. 4), and the bar on shortening it in a
    /// state of emergency (Art. 228 ust. 7). <b>The caretaker</b> after a refusal is the President's refused cabinet in both paths: Art. 162 ust. 2 pkt 1 ("w razie
    /// nieuchwalenia przez Sejm wotum zaufania") makes it resign and ust. 3 entrusts it with carrying on "do czasu powołania nowej Rady Ministrów" - the player's
    /// steps install it and discharge it fourteen days later, an AI Poland installs it and discharges it on the same first sitting (`SimulationManager.FormAsAi`).
    /// <b>Repeated shortened terms are not limited</b>: the constitution sets no bound, so a Sejm whose election again carries no cabinet is shortened again.</para>
    /// <para>It rides the government record it runs on (saved with it, additive) in the player's country; an AI Poland runs the same steps whole on its new
    /// Sejm's first sitting (`SimulationManager.ProjectSejm`, `WorldCalendar.TryExtraElection`).</para>
    /// </summary>
    public sealed class SejmFormation
    {
        /// <summary>Art. 154 ust. 1, 2 and 3: each step's window, in days.</summary>
        public const int StepWindowDays = 14;
        /// <summary>DECLARED: the record's interval from the Sejm's election of the prime minister to the President's appointment - 2023-12-11 to 2023-12-13 (poz. 1380, 1382).</summary>
        public const int AppointmentAfterSejmElectionDays = 2;

        public DateTime PollingDay;
        public DateTime FirstSitting;
        public SejmStage Stage;
        /// <summary>The day the next step falls.</summary>
        public DateTime NextOn;
        /// <summary>True where the steps are dated by the record (an election of record held by the game on its own day).</summary>
        public bool OfRecord;
        /// <summary>The designated (or elected) cabinet, its supporters and the prime minister's party, by key.</summary>
        public List<string> Cabinet = new List<string>();
        public List<string> Support = new List<string>();
        public string PmParty;

        /// <summary>Names the cabinet the procedure carries - its parties, supporters and prime minister's party (Art. 154's designee, or the cabinet the
        /// Sejm elects). The procedure's own lists, not who governs: the government record moves only when the cabinet is installed.</summary>
        public void Designate(List<string> cabinet, List<string> support, string pmParty)
        {
            Cabinet = cabinet;
            Support = support;
            PmParty = pmParty;
        }
        /// <summary>The steps as they fell, dated.</summary>
        public List<string> Log = new List<string>();

        [Newtonsoft.Json.JsonIgnore] public bool Open => Stage != SejmStage.Concluded;

        /// <summary>Whether the Sejm's steps decide who governs after an election in this country - Poland's.</summary>
        public static bool Applies(CountryId id) => id == CountryId.Poland;

        /// <summary>The steps of record for one term - the 10th's, the one Sejm election inside a Polish game's window that the record has dated.</summary>
        public readonly struct StepsOfRecord
        {
            public readonly DateTime PollingDay, Appointed, ConfidenceVote, SejmElected, SejmAppointed;
            public readonly string Basis;

            public StepsOfRecord(DateTime pollingDay, DateTime appointed, DateTime confidenceVote, DateTime sejmElected, DateTime sejmAppointed, string basis)
            {
                PollingDay = pollingDay; Appointed = appointed; ConfidenceVote = confidenceVote; SejmElected = sejmElected; SejmAppointed = sejmAppointed; Basis = basis;
            }
        }

        /// <summary>The record's steps after <paramref name="pollingDay"/>'s election, false where the record dates none (a Sejm the game elects).
        /// The first sitting is the chamber of record's (`WorldClock.Chambers`), the appointment after the Sejm's election the government of record's day.</summary>
        public static bool TryOfRecord(CountryId id, DateTime pollingDay, out StepsOfRecord steps)
        {
            steps = default;
            if (id != CountryId.Poland || pollingDay.Date != WorldClock.LatestElectionDay(id)) { return false; }   // the 10th term's election, 15 Oct 2023
            DateTime appointedBySejm = DateTime.MinValue;
            foreach (WorldClock.GovernmentOfRecord g in WorldClock.Governments(id)) { if (g.From > pollingDay.Date && (appointedBySejm == DateTime.MinValue || g.From < appointedBySejm)) { appointedBySejm = g.From; } }
            if (appointedBySejm == DateTime.MinValue) { return false; }
            steps = new StepsOfRecord(pollingDay.Date, new DateTime(2023, 11, 27), new DateTime(2023, 12, 11), new DateTime(2023, 12, 11), appointedBySejm,
                "poland/records_by_date.md §2a-2b: appointed 2023-11-27 (M.P. 2023 poz. 1287, 1288) [ELI-MP-2023]; confidence refused 2023-12-11, 190 for of 456 voting [API-V1]; the Sejm's election 2023-12-11 (poz. 1380) [API-V1]; appointed 2023-12-13 (poz. 1382, 1383) [ELI-MP-2023]");
            return true;
        }

        /// <summary>The new Sejm's first sitting: the chamber of record's where the record holds the election, else Art. 109 ust. 2's last day (ruling E2).</summary>
        public static DateTime FirstSittingAfter(CountryId id, DateTime pollingDay)
        {
            foreach (WorldClock.ChamberOfRecord c in WorldClock.Chambers(id)) { if (c.ElectionDay == pollingDay.Date) { return c.Convened; } }
            return pollingDay.Date.AddDays(WorldClock.PolandFirstSittingAfterDays);
        }

        /// <summary>The procedure opened on an election's polling day: the outgoing cabinet governs until the first sitting.</summary>
        public static SejmFormation OpenAfter(CountryId id, DateTime pollingDay) => OpenedOn(id, pollingDay, FirstSittingAfter(id, pollingDay), false);

        /// <summary>K302: <see cref="OpenAfter(CountryId, DateTime)"/> on the country's own counts - a shortened term's Sejm first sits on Art. 98 ust. 5's fifteenth day.</summary>
        public static SejmFormation OpenAfter(Country country, DateTime pollingDay) =>
            OpenedOn(country.Id, pollingDay, FirstSittingAfter(country, pollingDay), ElectedAfterShortenedTerm(country, pollingDay));

        private static SejmFormation OpenedOn(CountryId id, DateTime pollingDay, DateTime firstSitting, bool shortened)
        {
            var f = new SejmFormation { PollingDay = pollingDay.Date, FirstSitting = firstSitting, Stage = SejmStage.AwaitingSitting, OfRecord = TryOfRecord(id, pollingDay, out _) };
            f.NextOn = f.FirstSitting;
            f.Log.Add($"{pollingDay:yyyy-MM-dd}: the Sejm is elected; it first sits on {f.FirstSitting:yyyy-MM-dd} ({(f.OfRecord ? "the record's date [API-TERM]" : shortened ? "Art. 98 ust. 5's last day, after a shortened term" : "Art. 109 ust. 2's last day")}), where the cabinet resigns (Art. 162 ust. 1)");
            return f;
        }

        /// <summary>
        /// The cabinet a Sejm OF RECORD elects under Art. 154 ust. 3 - one rule the night's EXPECTED TO FORM and the procedure's install both read, so
        /// the two cannot disagree: the government of record appointed on the step's day (its parties, its supporters, its head and the head's party),
        /// put to the game's own Sejm. False where the record dates no steps for this election or names no cabinet; <paramref name="carries"/> is the
        /// game's vote on it - false sends both to the formation's projection. DECLARED, and put to Elias: a player's party that is in the record's
        /// cabinet is seated in it as on the record - where elsewhere the model never drafts the player's party unasked.
        /// </summary>
        public static bool TryRecordCabinet(Country country, DateTime pollingDay, out List<string> cabinet, out List<string> support, out string pmParty, out string head,
            out bool carries, out int yes, out int members)
        {
            cabinet = null; support = null; pmParty = null; head = null; carries = false; yes = 0; members = 0;
            if (country == null || !TryOfRecord(country.Id, pollingDay, out StepsOfRecord steps)) { return false; }
            if (!WorldClock.TryGovernmentAt(country.Id, steps.SejmAppointed, out WorldClock.GovernmentOfRecord elected) || elected.Cabinet == null || elected.Cabinet.Length == 0) { return false; }
            cabinet = new List<string>(elected.Cabinet);
            support = new List<string>(elected.Support ?? new string[0]);
            head = elected.Head;
            pmParty = PartyOfHead(elected.Head) ?? elected.Cabinet[0];
            carries = Carries(country, cabinet, support, out yes, out members);
            return true;
        }

        /// <summary>The party key in a record's head line, "Mateusz Morawiecki (PiS)" → "PiS"; null where none is bracketed.</summary>
        public static string PartyOfHead(string head)
        {
            int open = head?.IndexOf('(') ?? -1;
            int close = open >= 0 ? head.IndexOf(')', open) : -1;
            return open >= 0 && close > open ? head.Substring(open + 1, close - open - 1).Trim() : null;
        }

        /// <summary>The Sejm's vote on a cabinet: its parties and supporters for, every other seat against, every member present - carried by an absolute
        /// majority of the votes cast (Art. 154 ust. 2-3), which is then a majority of the members.</summary>
        public static bool Carries(Country country, IReadOnlyCollection<string> cabinet, IReadOnlyCollection<string> support, out int yes, out int members)
        {
            yes = 0; members = 0;
            if (country?.ParliamentSeats == null) { return false; }
            foreach (KeyValuePair<string, int> seat in country.ParliamentSeats)
            {
                members += seat.Value;
                if ((cabinet != null && Contains(cabinet, seat.Key)) || (support != null && Contains(support, seat.Key))) { yes += seat.Value; }
            }
            return members > 0 && yes * 2 > members;
        }

        private static bool Contains(IReadOnlyCollection<string> keys, string key)
        {
            foreach (string k in keys) { if (k == key) { return true; } }
            return false;
        }

        // ---------------------------------------------------------------------------------------------- K302: Art. 155 and the shortened term

        /// <summary>K302, DECLARED as the Art. 154 steps' days are (each on its window's last day): from the day the Sejm's vote under Art. 154 ust. 3 fails, its
        /// own fourteen days run out ("Sejm w ciągu 14 dni ... wybiera"), then the President's ("w ciągu 14 dni powołuje", Art. 155 ust. 1) - he appoints on
        /// the last of them.</summary>
        public const int PresidentAppointsAfterSejmVoteDays = 2 * StepWindowDays;

        /// <summary>K302, SOURCED - Art. 98 ust. 5 [TK-KONST]: "wyznacza ich datę na dzień przypadający nie później niż w ciągu 45 dni od dnia zarządzenia skrócenia
        /// kadencji Sejmu" - counted here from the order's own day. Kodeks wyborczy art. 195 § 1 ([KW-2011], the code's 2011 original text, not the consolidated
        /// text in force) counts the same forty-five days, for a President's shortening "od dnia ogłoszenia postanowienia Prezydenta Rzeczypospolitej o skróceniu
        /// kadencji Sejmu" - from its publication, on or after the order's day; so a polling day within forty-five days of the order is within them under both
        /// texts, and the constitution's count from the order, the earlier start, governs the model's dates.</summary>
        public const int ShortenedTermElectionWithinDays = 45;

        /// <summary>K302, SOURCED - Art. 98 ust. 5 [TK-KONST]: "Prezydent Rzeczypospolitej zwołuje pierwsze posiedzenie nowo wybranego Sejmu nie później niż na 15
        /// dzień po dniu przeprowadzenia wyborów" (Art. 109 ust. 2's thirty days hold "z wyjątkiem przypadków określonych w art. 98 ust. 3 i 5"): the Sejm a
        /// shortened term's election chooses first sits on that day, its last (DECLARED, as ruling E2 took Art. 109 ust. 2's last day).</summary>
        public const int ShortenedTermFirstSittingAfterDays = 15;

        /// <summary>
        /// K302: the polling day of the elections a shortening ordered on <paramref name="orderedOn"/> gives - the Sunday on or before Art. 98 ust. 5's forty-fifth
        /// day. The constitution asks only for a day no later than the forty-fifth, and Kodeks wyborczy art. 4 § 1 [KW-2011] for a day off work ("Wybory odbywają
        /// się w dniu wolnym od pracy"). <b>DECLARED, the last Sunday in the window</b> - ruling E2's principle (the most recent practice) applied to the one
        /// shortened term on record: the President's order of 7 September 2007, on the Sejm's own resolution of that day (Art. 98 ust. 3, ust. 5 applied
        /// "odpowiednio"), set "niedzielę dnia 21 października 2007 r." - the last Sunday in the forty-five days, the forty-fifth a Monday (Dz.U. 2007 nr 162
        /// poz. 1145 [PREZ-2007], `ElectionsData/poland/raw/shortened_term_2007/`). The rule has no floor of its own: an order on a Wednesday would give the
        /// thirty-ninth day, a day before art. 195 § 2 pkt 1 lit. c's shortened district notice ("do 40 dnia przed dniem wyborów") could follow the order. The
        /// Sejm's steps never order on a Wednesday - every order falls on its first sitting's weekday or fourteen-day multiples of it (a Tuesday after an ordinary
        /// count, a Monday after a shortened term's or on the record's 2023 days), so the polling day is the fortieth or forty-first; `SejmFormationDiagnostic`
        /// (g) fails a thirty-ninth. The one other order, a lapsed shortening ordered again on the day an older save's world finds it
        /// (`SimulationManager.ReorderLapsedShortening`), can fall on any weekday - that edge keeps the rule as it stands, DECLARED.
        /// </summary>
        public static DateTime ShortenedTermElectionDay(DateTime orderedOn)
        {
            DateTime limit = orderedOn.Date.AddDays(ShortenedTermElectionWithinDays);
            return limit.AddDays(-(int)limit.DayOfWeek);
        }

        /// <summary>K302: the first sitting of the Sejm <paramref name="pollingDay"/>'s election chooses, read off <paramref name="country"/>'s own counts - Art. 98
        /// ust. 5's fifteenth day after a shortened term's election (<see cref="ElectedAfterShortenedTerm"/>), else <see cref="FirstSittingAfter(CountryId, DateTime)"/>.
        /// Poland's alone: any other country is refused (its first sitting is `WorldCalendar.FirstSittingAfter`'s by its own statute).</summary>
        public static DateTime FirstSittingAfter(Country country, DateTime pollingDay)
        {
            if (country == null || !Applies(country.Id)) { throw new ArgumentException("the Sejm's first sitting is Poland's alone - " + (country?.Id.ToString() ?? "no country") + " was given", nameof(country)); }
            return ElectedAfterShortenedTerm(country, pollingDay) ? pollingDay.Date.AddDays(ShortenedTermFirstSittingAfterDays) : FirstSittingAfter(country.Id, pollingDay);
        }

        /// <summary>
        /// K302: whether <paramref name="pollingDay"/>'s Sejm election ends a shortened term (Art. 155 ust. 2, Art. 98 ust. 5) - read off the country's own counts, so
        /// a world and its forks answer alike and nothing is saved. Every term begins at its Sejm's first sitting and runs four years (Art. 98 ust. 1, and ust. 6
        /// after a shortening), so an election held before the ordinary polling day of the term it ends (<see cref="WorldClock.FirstSundayOfTermWindow"/>, ruling
        /// D3) ended that term early. The terms are walked from the chamber of record's, each count the game held beginning the next. False for any other country,
        /// and for every election on or before the chamber of record's.
        /// </summary>
        public static bool ElectedAfterShortenedTerm(Country country, DateTime pollingDay)
        {
            if (country?.ElectionHistory == null || !Applies(country.Id)) { return false; }
            DateTime day = pollingDay.Date, ofRecord = WorldClock.LatestElectionDay(country.Id);
            if (day <= ofRecord) { return false; }
            var counts = new List<DateTime>();
            foreach (ElectionRecord e in country.ElectionHistory) { if (NationalElection.CountsParliament(e.Method) && e.Date.Date > ofRecord && e.Date.Date < day && !counts.Contains(e.Date.Date)) { counts.Add(e.Date.Date); } }
            counts.Sort();
            DateTime sitting = FirstSittingAfter(country.Id, ofRecord);
            foreach (DateTime held in counts)
            {
                sitting = held < WorldClock.FirstSundayOfTermWindow(sitting) ? held.AddDays(ShortenedTermFirstSittingAfterDays) : FirstSittingAfter(country.Id, held);
            }
            return day < WorldClock.FirstSundayOfTermWindow(sitting);
        }

        /// <summary>K302 (Art. 98 ust. 6: "W razie skrócenia kadencji Sejmu stosuje się odpowiednio przepis ust. 1"): the ordinary polling day on or after
        /// <paramref name="onOrAfter"/> where the country last voted on <paramref name="lastPolled"/> - each term's own window from its own first sitting, so the
        /// Sejm a shortened term's election chooses begins four years of its own, as Italy's calendar re-anchors on a dissolution (K206). Where no term was
        /// shortened it is `WorldClock.TryNextPollingDay`'s chain, day for day; asked on the last count's own day it answers that day, as the chain does on a
        /// polling day. Poland's alone (<see cref="FirstSittingAfter(Country, DateTime)"/> refuses any other country).</summary>
        public static DateTime OrdinaryAfter(Country country, DateTime lastPolled, DateTime onOrAfter)
        {
            DateTime day = lastPolled.Date;
            DateTime sitting = FirstSittingAfter(country, day);
            while (day < onOrAfter.Date)
            {
                day = WorldClock.FirstSundayOfTermWindow(sitting);
                sitting = FirstSittingAfter(country.Id, day);
            }
            return day;
        }

        /// <summary>
        /// K302: THE PRESIDENT'S CABINET UNDER ART. 155 UST. 1 AND THE SEJM'S VOTE ON IT. The vote, "większością głosów w obecności co najmniej połowy ustawowej
        /// liczby posłów": carried where more vote for than against with half the statutory deputies present (<see cref="PresidentialVeto.Quorum"/>; every seated
        /// member present, as every vote here reads). Its sides are the confidence procedure's (`ConfidenceProcedure.Vote`, Article94's shape): the cabinet and
        /// its supporters for; a party red-lined from the cabinet by a support-blocking line or an in-or-against rule (`CoalitionFormation.RedLinedMask`, what the
        /// investiture's opposition reads, its hold-out term the investiture's alone) against; every other party abstains. A vote's lists, not who governs:
        /// they are filled here alone (<see cref="Count"/>), and the government record moves only when the procedure installs the cabinet.
        /// </summary>
        public sealed class PresidentsCabinet
        {
            public readonly List<string> Cabinet = new List<string>();
            public readonly List<string> Support = new List<string>();
            public string PmParty { get; private set; }
            public int For { get; private set; }
            public int Against { get; private set; }
            public int Abstaining { get; private set; }
            public int Members { get; private set; }
            /// <summary>The formation's own score of the cabinet (`CoalitionFormation.Chamber.BaseScore`).</summary>
            public double Score { get; private set; }
            /// <summary>How many admissible cabinets the Sejm would carry by the simple majority - the President's choice is the best of them; none where the
            /// appointment is refused and the term shortened.</summary>
            public int Carrying { get; private set; }
            /// <summary>Each seated party's side and why.</summary>
            public readonly List<DivisionSide> Sides = new List<DivisionSide>();

            /// <summary>Art. 155 ust. 1: more for than against, half the statutory deputies present.</summary>
            public bool Carried => Members >= PresidentialVeto.Quorum && For > Against;

            public string Line() => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} led by {1}{2}: {3} for, {4} against, {5} abstaining of {6} members - {7}",
                string.Join("+", Cabinet), PmParty, Support.Count > 0 ? " with " + string.Join("+", Support) : string.Empty, For, Against, Abstaining, Members,
                Carried ? "carried, more for than against (Art. 155 ust. 1)" : "refused, no more for than against (Art. 155 ust. 1)");

            /// <summary>Why the President chose this cabinet, in words: the best of those a simple majority would carry, or, where none would, the best-scoring
            /// admissible one.</summary>
            public string Choice() => Carrying > 0
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "the best of {0} cabinet(s) a simple majority would carry", Carrying)
                : "the best-scoring admissible cabinet - no cabinet would carry a simple majority";

            internal PresidentsCabinet(int carrying) { Carrying = carrying; }

            /// <summary>The vote on <paramref name="cabinet"/> and <paramref name="support"/> (masks over the party system): its lists, its sides and counts, and the
            /// prime minister's party - the cabinet's largest (Poland declares no candidacy; `GovernmentRecord.FromView`'s rule).</summary>
            internal void Count(CoalitionFormation.Chamber chamber, IReadOnlyList<PoliticalParty> parties, int cabinet, int support, int against)
            {
                Score = cabinet > 0 && cabinet < chamber.BaseScore.Length ? chamber.BaseScore[cabinet] : double.NaN;
                int pmSeats = -1;
                for (int p = 0; p < parties.Count; p++)
                {
                    int seats = chamber.Seats[p];
                    bool inCabinet = (cabinet & (1 << p)) != 0, supports = !inCabinet && (support & (1 << p)) != 0;
                    if (inCabinet) { Cabinet.Add(parties[p].Abbrev); }
                    if (supports) { Support.Add(parties[p].Abbrev); }
                    if (inCabinet && seats > pmSeats) { PmParty = parties[p].Abbrev; pmSeats = seats; }
                    if (seats <= 0) { continue; }
                    Members += seats;
                    int side; string reason;
                    if (inCabinet) { side = 1; reason = "in the President's cabinet"; }
                    else if (supports) { side = 1; reason = "carries it from outside"; }
                    else if ((against & (1 << p)) != 0) { side = -1; reason = "red-lined from a cabinet party - votes against"; }
                    else { side = 0; reason = "abstains - no line bars its support, and it does not carry the cabinet"; }
                    if (side > 0) { For += seats; } else if (side < 0) { Against += seats; } else { Abstaining += seats; }
                    Sides.Add(new DivisionSide { Abbrev = parties[p].Abbrev, ShortName = parties[p].ShortName, Seats = seats, Side = side, Alignment = side, Reason = reason });
                }
            }
        }

        /// <summary>
        /// K302: whom the President appoints under Art. 155 ust. 1 - the formation's own preference (its score, `CoalitionFormation.Chamber.BaseScore`) among the
        /// admissible cabinets, each with the supporters the formation draws for it, that the Sejm would carry by the simple majority; where none would, the
        /// best-scoring admissible cabinet, refused. DECLARED, and put to Elias as Art. 154 ust. 1's designee is: the constitution leaves the President's choice
        /// free, so the President appoints the cabinet the formation itself prefers among those Art. 155's majority carries. The formation's defection rounds
        /// (a party in or behind a government walking out for another) are Art. 154's negotiation and are not taken here: measured, on a chamber where Art. 155
        /// is reached with the MN's one seat in it, they leave the MN alone - its supporter's place in every other cabinet is worth nothing to it, and its own
        /// one-seat cabinet carries 1 to 0 with every other member abstaining. Pure: it reads the chamber and the reading, and writes nothing.
        /// </summary>
        public static PresidentsCabinet PresidentAppoints(Country country, DeclarationReading reading)
        {
            IReadOnlyList<PoliticalParty> parties = PartySystems.For(country.Id);
            return PresidentAppoints(country.Id, parties, SeatsOf(country, parties), reading);
        }

        /// <summary>K302: <see cref="PresidentAppoints(Country, DeclarationReading)"/> on a chamber given as seats in the party system's order;
        /// <paramref name="extraLines"/>: lines beside the reading's, for measuring a reading the game does not hold (the formation's own hook, `GovernmentFormation`'s
        /// extra lines) - the game passes none.</summary>
        public static PresidentsCabinet PresidentAppoints(CountryId id, IReadOnlyList<PoliticalParty> parties, int[] seats, DeclarationReading reading, IReadOnlyList<RedLine> extraLines = null)
        {
            CoalitionFormation.Chamber chamber = Prepared(id, parties, seats, reading, out List<InOrAgainst> rules, extraLines);
            GovernmentOption? best = null, bestCarried = null;
            int carrying = 0;
            foreach (int cabinet in chamber.Admissible)   // ascending masks: of equal scores the first stands, the formation's own order
            {
                CoalitionFormation.CabinetEvaluation e = CoalitionFormation.Evaluate(chamber, cabinet);
                int against = AgainstMask(chamber, cabinet, e.SupportMask, rules);
                var option = new GovernmentOption(cabinet, e.SupportMask, e.Kind, e.CabinetSeats, e.SupportedSeats, CoalitionMath.Seats(seats, against), e.Cohesion, e.Score);
                if (best == null || option.Score > best.Value.Score) { best = option; }
                if (chamber.TotalSeats < PresidentialVeto.Quorum || option.SupportedSeats <= option.OpposedSeats) { continue; }
                carrying++;
                if (bestCarried == null || option.Score > bestCarried.Value.Score) { bestCarried = option; }
            }
            GovernmentOption? pick = bestCarried ?? best;
            var chosen = new PresidentsCabinet(carrying);
            if (pick != null) { chosen.Count(chamber, parties, pick.Value.Cabinet, pick.Value.Support, AgainstMask(chamber, pick.Value.Cabinet, pick.Value.Support, rules)); }
            return chosen;
        }

        /// <summary>K302: the Sejm's vote under Art. 155 ust. 1 on a named cabinet and its supporters - the vote <see cref="PresidentAppoints(Country, DeclarationReading)"/>
        /// counts, taken on its day on the chamber as it sits.</summary>
        public static PresidentsCabinet SimpleMajority(Country country, IReadOnlyCollection<string> cabinet, IReadOnlyCollection<string> support, DeclarationReading reading)
        {
            IReadOnlyList<PoliticalParty> parties = PartySystems.For(country.Id);
            CoalitionFormation.Chamber chamber = Prepared(country.Id, parties, SeatsOf(country, parties), reading, out List<InOrAgainst> rules);
            int cabinetMask = 0, supportMask = 0;
            for (int p = 0; p < parties.Count; p++)
            {
                if (cabinet != null && Contains(cabinet, parties[p].Abbrev)) { cabinetMask |= 1 << p; }
                else if (support != null && Contains(support, parties[p].Abbrev)) { supportMask |= 1 << p; }
            }
            var named = new PresidentsCabinet(-1);
            named.Count(chamber, parties, cabinetMask, supportMask, AgainstMask(chamber, cabinetMask, supportMask, rules));
            return named;
        }

        private static int[] SeatsOf(Country country, IReadOnlyList<PoliticalParty> parties)
        {
            var seats = new int[parties.Count];
            for (int p = 0; p < parties.Count; p++) { seats[p] = country.ParliamentSeats != null && country.ParliamentSeats.TryGetValue(parties[p].Abbrev, out int held) ? held : 0; }
            return seats;
        }

        /// <summary>The chamber as the formation prepares it for the election's reading (`GovernmentFormation`'s own inputs: the compatibility, the reading's
        /// lines and platforms, the parliamentary groups, the country's investiture rule).</summary>
        private static CoalitionFormation.Chamber Prepared(CountryId id, IReadOnlyList<PoliticalParty> parties, int[] seats, DeclarationReading reading, out List<InOrAgainst> rules,
            IReadOnlyList<RedLine> extraLines = null)
        {
            rules = reading.Platforms(id, parties);
            List<RedLine> lines = reading.Lines(id, parties);
            if (extraLines != null) { lines.AddRange(extraLines); }
            return CoalitionFormation.Prepare(seats, GovernmentFormation.Compatibility(parties), lines, ChamberRules.UsesNegativeParliamentarism(id), rules,
                ChamberRules.JointMasks(id, parties, seats));
        }

        /// <summary>The parties that vote against a cabinet under Art. 155 ust. 1: red-lined from it (a support-blocking line to a member, or an in-or-against rule),
        /// a parliamentary group outside it as one.</summary>
        private static int AgainstMask(CoalitionFormation.Chamber chamber, int cabinet, int support, IReadOnlyList<InOrAgainst> rules)
        {
            int against = CoalitionFormation.RedLinedMask(cabinet, chamber.N, chamber.Lines, rules) & ~support & ~chamber.SeatlessMask;
            return CoalitionFormation.JointAlike(against, cabinet, chamber.Joint, chamber.Seats);
        }
    }
}
