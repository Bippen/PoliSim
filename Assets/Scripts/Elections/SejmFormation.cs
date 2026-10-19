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
    /// absolute majority of the votes cast is then a majority of the members. Art. 155 (the President's own appointment, a simple majority) and the
    /// shortened term it ends in (Art. 155 ust. 2, Art. 98 ust. 4) are NOT modelled: where the Sejm elects no one, the caretaker serves on, stated.</para>
    /// <para>It rides the government record it runs on (saved with it, additive); the player's country only, the formation having no AI path in Poland.</para>
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
        public static SejmFormation OpenAfter(CountryId id, DateTime pollingDay)
        {
            var f = new SejmFormation { PollingDay = pollingDay.Date, FirstSitting = FirstSittingAfter(id, pollingDay), Stage = SejmStage.AwaitingSitting, OfRecord = TryOfRecord(id, pollingDay, out _) };
            f.NextOn = f.FirstSitting;
            f.Log.Add($"{pollingDay:yyyy-MM-dd}: the Sejm is elected; it first sits on {f.FirstSitting:yyyy-MM-dd} ({(f.OfRecord ? "the record's date [API-TERM]" : "Art. 109 ust. 2's last day")}), where the cabinet resigns (Art. 162 ust. 1)");
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
    }
}
