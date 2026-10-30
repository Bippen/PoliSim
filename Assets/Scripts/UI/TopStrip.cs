using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;

namespace PoliSim.UI
{
    /// <summary>
    /// K178 (Elias's ruling: the top strip in every country; D-WORKLIST §2, Design's return of 2026-10-28, board 33a): WHAT THE TOP STRIP SAYS, as data - one
    /// strip, seven places fixed left to right, a chip that does not apply absent and its neighbours closing up. The left half is time: (1) the clock, (2)
    /// what holds it, (3) what binds (filled BAD: a funding lapse, a default, a programme's loan withheld), (4) what is coming (filled AMB: a programme year,
    /// a declaration of no confidence the government has still to answer, US funding to a horizon; GOOD for its one day once the market reopens). The
    /// right half is standing: (5) the polling day, (6) the government's standing in words, (7) the player's role. Places 3 and 4 hold at most
    /// <see cref="TimeChipsMax"/> chips together and fold the rest into one "+N" chip whose slip lists them.
    /// <para>Every reading here is a pure function of the country and the day, so `TopStripCrisisDiagnostic` reads it on planted countries; the controller
    /// draws it (the partial GameController.TopStrip.cs). The clock, the holds, the lapse and the role are the controller's own and are drawn there.</para>
    /// <para>MF q33 (K141-K144): <see cref="CrisisPlateOwed"/> is the debt crisis's moment - owed on the day a crisis opens, once per crisis
    /// (<see cref="SovereignCrisisState.Crises"/> counts them, so a second crisis owes it again).</para>
    /// </summary>
    public static class TopStrip
    {
        /// <summary>A chip's face: paper (a state on the dark strip), the three filled grounds, or outlined.</summary>
        public enum Face { Paper, Bad, Amber, Good, Line }

        /// <summary>One chip: its anchor id, its words, its face, its leading icon (a v3.5 icon's short name, or null) and its slip.</summary>
        public sealed class Chip
        {
            public string Id;
            public string Words;
            public Face Face;
            public string Icon;
            public string SlipHead;
            public List<string> Slip = new List<string>();
        }

        /// <summary>Board 33a's overflow rule: at most three chips in places 3 and 4 together.</summary>
        public const int TimeChipsMax = 3;

        /// <summary>Board 33a: within this many days the polling day's date gives way to "in N days".</summary>
        public const int PollingCountdownDays = 30;

        /// <summary>A day as the strip prints it - "13 Sep 2026".</summary>
        public static string Day(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

        private static string N(int n) => n.ToString(CultureInfo.InvariantCulture);

        // ----------------------------------------------------------------------------------------------------------------------------------------------
        // Place 5: the polling day
        // ----------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>The polling day's words: its date, or within <see cref="PollingCountdownDays"/> "in N days"; null for a day already past.</summary>
        public static string PollingWords(DateTime pollingDay, DateTime today)
        {
            int days = (pollingDay.Date - today.Date).Days;
            if (days < 0) { return null; }
            if (days == 0) { return "Polling day today"; }
            if (days <= PollingCountdownDays) { return "in " + N(days) + (days == 1 ? " day" : " days"); }
            return Day(pollingDay);
        }

        /// <summary>Place 5's chip for the player's next polling day (the manager's `TryPlayerPollingDay`, an ordered extra election included), outlined with the ballot.</summary>
        public static Chip PollingChip(DateTime pollingDay, DateTime today)
        {
            string words = PollingWords(pollingDay, today);
            if (words == null) { return null; }
            int days = (pollingDay.Date - today.Date).Days;
            var chip = new Chip { Id = "strip:polling", Words = words, Face = Face.Line, Icon = "ballot", SlipHead = "ELECTION · " + Day(pollingDay).ToUpperInvariant() };
            chip.Slip.Add(days == 0 ? "The country votes today" : N(days) + (days == 1 ? " day" : " days") + " to the polling day");
            chip.Slip.Add("A state, not a control");
            return chip;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------
        // Place 6: the government's standing
        // ----------------------------------------------------------------------------------------------------------------------------------------------

        private static int Seats(IReadOnlyDictionary<string, int> seats, IEnumerable<string> parties)
        {
            int n = 0;
            if (seats == null || parties == null) { return 0; }
            foreach (string p in parties) { if (p != null && seats.TryGetValue(p, out int s)) { n += s; } }
            return n;
        }

        private static int Total(IReadOnlyDictionary<string, int> seats)
        {
            int n = 0;
            if (seats != null) { foreach (int s in seats.Values) { n += s; } }
            return n;
        }

        /// <summary>Whether the US presidency's party holds fewer than both chambers on <paramref name="today"/> - the Desk's own rule (PS-6 US-35), kept in one place.</summary>
        public static bool UsDivided(Country country, DateTime today)
        {
            if (!Congress.PresidentOn(country, today, out _, out string pp, out _) || string.IsNullOrEmpty(pp)) { return false; }
            string house = Congress.HouseMajority(country), senate = Congress.SenateMajority(country, today);
            return (house != null && house != pp) || (senate != null && senate != pp);
        }

        /// <summary>
        /// Place 6's chip, outlined, words only (board 33a): divided or unified in a US Congress; caretaker; cohabitation in Poland (the president in office
        /// backed by a party outside the cabinet - the president's backing party, PresidencyOfRecord); a majority (a coalition majority where the cabinet holds more
        /// than one party; in Italy, whose government needs the confidence of both chambers, Art. 94, the majority in each chamber); or a minority - with
        /// support where a party supports it from outside (France's too: the review of wave/dcrisis, L8). ABSENT FOR WANT OF MODEL DATA: France's censure motion (art. 49 - its rules are unsourced, `ConfidenceProcedure.RulesOf`) and its cohabitation (no French presidency is held). Null where no government is stored, or a presidency without a
        /// Congress seated.
        /// </summary>
        public static Chip StandingChip(Country country, DateTime today)
        {
            GovernmentRecord g = country?.Government;
            if (g == null) { return null; }
            var chip = new Chip { Id = "strip:standing", Face = Face.Line };
            string Short(string key) => PartySystems.ShortName(country.Id, key) ?? key;
            if (Congress.Applies(country))
            {
                bool divided = UsDivided(country, today);
                chip.Words = divided ? "Divided" : "Unified";
                chip.SlipHead = divided ? "DIVIDED GOVERNMENT" : "UNIFIED GOVERNMENT";
                string house = Congress.HouseMajority(country), senate = Congress.SenateMajority(country, today);
                chip.Slip.Add("The House: " + (house == null ? "no majority" : Short(house)) + " · the Senate: " + (senate == null ? "no majority" : Short(senate)));
                chip.Slip.Add(divided ? "The presidency's party holds fewer than both chambers" : "The presidency's party holds both chambers");
                return chip;
            }
            if (g.Kind == WorldClock.ExecutiveKind.Presidency) { return null; }

            int total = Total(country.ParliamentSeats), cabinet = Seats(country.ParliamentSeats, g.Cabinet), support = Seats(country.ParliamentSeats, g.Support);
            // S10 (10a, K312): the cabinet and its support each named as one group - "+", or " · " where a name holds a '+' - through the one helper
            string cabinetWords = g.Cabinet.Count > 0 ? PartySystems.JoinShortNames(g.Cabinet.ConvertAll(Short)) : "none";
            chip.Slip.Add(cabinetWords + " in cabinet, " + N(cabinet) + " of " + N(total));
            if (g.Support.Count > 0) { chip.Slip.Add(PartySystems.JoinShortNames(g.Support.ConvertAll(Short)) + " support from outside: " + N(cabinet + support)); }

            if (g.Caretaker)
            {
                chip.Words = "Caretaker";
                chip.SlipHead = "A CARETAKER GOVERNMENT";
                if (g.CaretakerSince != DateTime.MinValue) { chip.Slip.Add("Serving on as a caretaker since " + Day(g.CaretakerSince)); }
                return chip;
            }
            if (country.Id == CountryId.Poland && PresidentialElection.PresidentAt(country.Id, country.PresidentialElections, today, out PresidencyOfRecord.President president)
                && !string.IsNullOrEmpty(president.BackingParty) && g.Cabinet.Count > 0 && !g.Cabinet.Contains(president.BackingParty))
            {
                chip.Words = "Cohabitation";
                chip.SlipHead = "COHABITATION";
                chip.Slip.Add("The President, " + president.Name + ", was backed by " + Short(president.BackingParty) + ", outside the cabinet");
                return chip;
            }
            bool majority = total > 0 && cabinet * 2 > total;
            if (ConfidenceProcedure.RulesOf(country.Id) == ConfidenceProcedure.Rules.Parlamento && country.SecondChamberSeats != null && country.SecondChamberSeats.Count > 0)
            {
                int senateTotal = Total(country.SecondChamberSeats), senateCabinet = Seats(country.SecondChamberSeats, g.Cabinet);
                bool senateMajority = senateTotal > 0 && senateCabinet * 2 > senateTotal;
                chip.Slip.Add("The Senate: " + N(senateCabinet) + " of " + N(senateTotal));
                if (majority && senateMajority) { chip.Words = "Majority in both chambers"; chip.SlipHead = "A MAJORITY IN BOTH CHAMBERS"; return chip; }
                if (majority || senateMajority) { chip.Words = majority ? "No Senate majority" : "No Chamber majority"; chip.SlipHead = "A MAJORITY IN ONE CHAMBER"; chip.Slip.Add("The government needs the confidence of both chambers"); return chip; }
            }
            if (majority)
            {
                chip.Words = g.Cabinet.Count > 1 ? "Coalition majority" : "Majority";
                chip.SlipHead = g.Cabinet.Count > 1 ? "A COALITION MAJORITY" : "A MAJORITY GOVERNMENT";
                return chip;
            }
            if (g.Support.Count > 0)
            {
                chip.Words = "Minority, with support";
                chip.SlipHead = "A MINORITY GOVERNMENT, WITH SUPPORT";
                return chip;
            }
            chip.Words = "Minority government";
            chip.SlipHead = "A MINORITY GOVERNMENT";
            chip.Slip.Add("No declared support from outside");
            return chip;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------
        // Places 3 and 4: the debt crisis and a declaration of no confidence (the lapse is the controller's, K40)
        // ----------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>The time left through <paramref name="endsOn"/> (inclusive) in the strip's words - "2 y 3 m left", "5 m left", "12 days left".</summary>
        public static string TimeLeft(DateTime today, DateTime endsOn)
        {
            int days = (endsOn.Date - today.Date).Days + 1;
            if (days <= 1) { return "last day"; }
            if (days < 31) { return N(days) + " days left"; }
            int months = (int)Math.Floor(days / (365.25 / 12.0));
            int y = months / 12, m = months % 12;
            if (y == 0) { return N(m) + " m left"; }
            return m == 0 ? N(y) + " y left" : N(y) + " y " + N(m) + " m left";
        }

        /// <summary>The day the market reopens after a crisis ends: the model ends it on the first day past <see cref="SovereignCrisisState.EndsOn"/>
        /// (`SovereignCrisis.AdvanceDay`).</summary>
        public static DateTime RestoredOn(SovereignCrisisState crisis) => crisis.EndsOn.Date.AddDays(1);

        /// <summary>
        /// The debt crisis's chip (D-WORKLIST §1, the after-state's first place): <b>Default · 2 y 3 m left</b> on the BAD ground; <b>Programme · year 1 of 3</b>
        /// on the AMB ground; <b>Loan withheld · to 13 May 2033</b> on the BAD ground while a missed year's disbursement is withheld; <b>Market access
        /// restored</b> on the GOOD ground for the one day the market reopens. Its slip carries `FiscalRules.CrisisLines`. Null with the market open.
        /// <paramref name="binds"/> says which place it takes - 3 (what binds) or 4 (what is coming).
        /// </summary>
        public static Chip CrisisChip(Country country, DateTime today, out bool binds)
        {
            binds = false;
            SovereignCrisisState crisis = country?.Crisis;
            if (crisis == null) { return null; }
            var chip = new Chip { Id = "strip:crisis", Icon = "debt" };
            if (crisis.Phase == CrisisPhase.Default)
            {
                binds = true;
                chip.Face = Face.Bad;
                chip.Words = "Default · " + TimeLeft(today, crisis.EndsOn);
                chip.SlipHead = chip.Words.ToUpperInvariant();
            }
            else if (crisis.Phase == CrisisPhase.Programme && SovereignCrisis.Unfinanced(country, today))
            {
                binds = true;
                chip.Face = Face.Bad;
                chip.Words = "Loan withheld · to " + Day(crisis.WithheldThrough);
                chip.SlipHead = "LOAN WITHHELD";
            }
            else if (crisis.Phase == CrisisPhase.Programme)
            {
                chip.Face = Face.Amber;
                chip.Words = "Programme · year " + N(Math.Max(1, Math.Min(crisis.ProgrammeYear, SovereignCrisis.ProgrammeYears))) + " of " + N(SovereignCrisis.ProgrammeYears);
                chip.SlipHead = chip.Words.ToUpperInvariant();
            }
            else if (crisis.Crises > 0 && today.Date == RestoredOn(crisis))
            {
                chip.Face = Face.Good;
                chip.Words = "Market access restored";
                chip.SlipHead = "MARKET ACCESS RESTORED · " + Day(today).ToUpperInvariant();
                chip.Slip.Add((SovereignCrisis.BranchOf(country.Id) == CrisisPhase.Default ? "The exclusion from the market ended on " : "The programme ended on ") + Day(crisis.EndsOn));
                chip.Slip.Add("The market is open again; a scheduled review that reads the rating at CCC closes it once more");
                return chip;
            }
            else { return null; }
            chip.Slip.AddRange(FiscalRules.CrisisLines(country, today));
            return chip;
        }

        /// <summary>Place 4: a declaration of no confidence the government has still to answer (the Riksdag's and the Bundestag's rules, which take motions -
        /// `ConfidenceProcedure.TakesMotions`): on the AMB ground with the chamber, its day and, on the slip, who moved it and what follows. Null otherwise -
        /// the constructive votes (Art. 67 GG, Art. 158) and Italy's are taken on the day they are moved, so the model holds no vote tabled and waiting.</summary>
        public static Chip ConfidenceChip(Country country, DateTime today)
        {
            GovernmentRecord g = country?.Government;
            if (g == null || g.Caretaker || g.NoConfidenceOn == DateTime.MinValue || !ConfidenceProcedure.TakesMotions(country.Id)) { return null; }
            var chip = new Chip
            {
                Id = "strip:confidence", Face = Face.Amber, Icon = "chamber",
                Words = "No confidence declared · " + g.NoConfidenceOn.ToString("d MMM", CultureInfo.InvariantCulture),
                SlipHead = "NO CONFIDENCE DECLARED · " + Day(g.NoConfidenceOn).ToUpperInvariant(),
            };
            if (!string.IsNullOrEmpty(g.NoConfidenceMover)) { chip.Slip.Add("Moved by " + (PartySystems.ShortName(country.Id, g.NoConfidenceMover) ?? g.NoConfidenceMover)); }
            chip.Slip.Add("Within the week the government orders an extra election, or the prime minister is discharged and the government serves on as a caretaker");
            return chip;
        }

        /// <summary>K197: place 4 for the Sejm's Art. 158 vote - tabled and taken the same day (K195: Art. 158 ust. 2's seven days not waited), so the motion is
        /// "what is coming" for its one day: on the AMB ground with the chamber, the vote's outcome in its words and, on the slip, the division's own title. The
        /// newest vote of the day where there were two; null on every other day (the Docket keeps the outcome for the month, `PolishDeskRecord`).</summary>
        public static Chip Art158Chip(Country country, DateTime today)
        {
            List<DivisionRecord> votes = PolishDeskRecord.RecentArt158(country, today);
            if (votes.Count == 0 || votes[0].Date.Date != today.Date) { return null; }
            DivisionRecord vote = votes[0];
            var chip = new Chip { Id = "strip:art158", Face = Face.Amber, Icon = "chamber", Words = PolishDeskRecord.Art158Words(country, vote) + " today", SlipHead = "ART. 158 · " + Day(today).ToUpperInvariant() };
            chip.Slip.Add(vote.Title);
            chip.Slip.Add("Moved and voted on the same day - the Sejm does not wait the seven days");
            return chip;
        }

        /// <summary>
        /// S11 (Design's return of 2026-10-30, board 11d): place 4 while the Sejm's steps run in the player's Poland - the next step and the day it falls, on the AMB
        /// ground with the chamber: "First sitting · 13 Nov" before it, "Art. 154 · cabinet by 27 Nov", "Confidence vote · by 11 Dec", "Sejm's vote ·
        /// by 25 Dec", "Appointment · 13 Dec", "Art. 155 · cabinet by 8 Jan", "Confidence vote · by 22 Jan · last step". "by" because each step may come on any day
        /// of its window; the engine takes the window's last (K304). Read off the procedure's own stage and day (<see cref="SejmFormation.Stage"/>,
        /// <see cref="SejmFormation.NextOn"/>); null once it has concluded, or for any other country.
        /// </summary>
        public static Chip SejmStepChip(Country country, DateTime today)
        {
            SejmFormation f = SejmDesk.Record(country);
            if (f == null || !f.Open) { return null; }
            string on = f.NextOn.ToString("d MMM", CultureInfo.InvariantCulture);
            string words, head, line;
            switch (f.Stage)
            {
                case SejmStage.AwaitingSitting: words = "First sitting · " + on; head = "THE FIRST SITTING"; line = "The cabinet resigns there and serves on · Art. 162.1"; break;
                case SejmStage.Designated: words = "Art. 154 · cabinet by " + on; head = "THE PRESIDENT'S STEP"; line = "A cabinet appointed within 14 days of the first sitting · Art. 154.1"; break;
                case SejmStage.AwaitingConfidence: words = "Confidence vote · by " + on; head = "THE SEJM'S CONFIDENCE"; line = "An absolute majority, half the deputies present · Art. 154.2"; break;
                case SejmStage.SejmElects: words = "Sejm's vote · by " + on; head = "THE SEJM'S STEP"; line = "Its own prime minister by an absolute majority · Art. 154.3"; break;
                case SejmStage.AwaitingAppointment: words = "Appointment · " + on; head = "THE APPOINTMENT"; line = "The President appoints the cabinet the Sejm elected · Art. 154.3"; break;
                case SejmStage.PresidentAppoints: words = "Art. 155 · cabinet by " + on; head = "THE PRESIDENT'S SECOND STEP"; line = "The President appoints within 14 days · Art. 155.1"; break;
                case SejmStage.AwaitingPresidentsConfidence: words = "Confidence vote · by " + on + " · last step"; head = "THE LAST STEP"; line = "More for than against, or the term is shortened · Art. 155"; break;
                default: return null;
            }
            var chip = new Chip { Id = "strip:sejm", Face = Face.Amber, Icon = "chamber", Words = words, SlipHead = head + " · " + Day(f.NextOn).ToUpperInvariant() };
            chip.Slip.Add(line);
            chip.Slip.Add("The Docket lists each step as it falls");
            return chip;
        }

        /// <summary>Board 33a's overflow: what binds first, then what is coming, at most <see cref="TimeChipsMax"/> shown; the rest fold into
        /// <paramref name="more"/> - a paper "+N" chip whose slip lists them (null when nothing folds).</summary>
        public static List<Chip> FoldTimeChips(IList<Chip> binds, IList<Chip> coming, out Chip more)
        {
            var all = new List<Chip>();
            if (binds != null) { foreach (Chip c in binds) { if (c != null) { all.Add(c); } } }
            if (coming != null) { foreach (Chip c in coming) { if (c != null) { all.Add(c); } } }
            more = null;
            if (all.Count <= TimeChipsMax) { return all; }
            List<Chip> shown = all.GetRange(0, TimeChipsMax);
            more = new Chip { Id = "strip:more", Face = Face.Paper, Words = "+" + N(all.Count - TimeChipsMax), SlipHead = "ALSO" };
            for (int i = TimeChipsMax; i < all.Count; i++) { more.Slip.Add(all[i].Words); }
            return shown;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------
        // The debt crisis's moment
        // ----------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// MF q33's moment (D-WORKLIST §1, boards 32a and 32b): the plate is owed on the day a crisis opens - the scheduled review whose trigger read CCC
        /// (`SovereignCrisis.AdvanceDay`) - while the player has acknowledged fewer crises than the country has entered (<paramref name="seenCrises"/>, the
        /// screen's own count, never saved: a game loaded on that day shows it again, the K42 idiom). A second crisis owes it again.
        /// </summary>
        public static bool CrisisPlateOwed(Country country, DateTime today, int seenCrises)
        {
            SovereignCrisisState crisis = country?.Crisis;
            return crisis != null && crisis.Phase != CrisisPhase.None && crisis.StartedOn.Date == today.Date && crisis.Crises > seenCrises;
        }
    }
}
