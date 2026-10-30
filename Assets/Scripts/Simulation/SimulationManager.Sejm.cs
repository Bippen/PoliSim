using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// AF-8 (Elias's ruling q8, 2026-10-18): POLAND'S GOVERNMENT CHANGES BY THE STATUTE'S STEPS, not on polling day - the procedure and its dates are
    /// <see cref="Elections.SejmFormation"/>'s. The night opens it on the outgoing government (<see cref="OpenSejmFormation"/>); each day of the player's
    /// country runs the steps that fall due (<see cref="AdvanceSejmFormation"/>, from the confidence day): the resignation at the first sitting (Art. 162
    /// ust. 1, the cabinet carrying on under ust. 3), the President's designation and appointment (Art. 154 ust. 1), the confidence vote (ust. 2), the Sejm's
    /// own election and the appointment of the cabinet it elects (ust. 3); and K302 (Elias, 2026-10-29), Art. 155: the President's own appointment and the
    /// Sejm's confidence by a simple majority (ust. 1), refused, the shortened term and the elections it orders (ust. 2, Art. 98 ust. 5) - the player's
    /// extra election (`ScheduleExtraElection`). A cabinet installed here goes through the manager's install - its arrival budget reset and a window the
    /// player no longer governs closed (§698), which the night's install never did for Poland (PF-19's Polish path).
    /// </summary>
    public partial class SimulationManager
    {
        /// <summary>Whether the Sejm's steps decide who governs after this country's election - the player's Poland.</summary>
        public bool SejmStepsApply(CountryId countryId) => PlayerCountryId.HasValue && PlayerCountryId.Value == countryId && Elections.SejmFormation.Applies(countryId);

        /// <summary>The night's call: the Sejm is elected, the procedure opens on the outgoing government, and nothing is installed today.</summary>
        public Elections.SejmFormation OpenSejmFormation(Country country, System.DateTime pollingDay)
        {
            if (country?.Government == null || !SejmStepsApply(country.Id)) { return null; }
            Elections.SejmFormation f = Elections.SejmFormation.OpenAfter(country, pollingDay);   // K302: a shortened term's Sejm first sits by Art. 98 ust. 5
            country.Government.Sejm = f;
            Debug.Log($"SEJM: {country.Id} - elected on {pollingDay:yyyy-MM-dd}; the first sitting {f.FirstSitting:yyyy-MM-dd}{(f.OfRecord ? " (the record's)" : Elections.SejmFormation.ElectedAfterShortenedTerm(country, pollingDay) ? " (Art. 98 ust. 5's last day, after a shortened term)" : " (Art. 109 ust. 2's last day)")}; {country.Government.PmParty} governs until then");
            return f;
        }

        /// <summary>The steps due today, in order (the confidence vote and the Sejm's election can fall on one day, as on 2023-12-11). True while a
        /// procedure is open, so nothing else of the confidence day runs on a government in the middle of its formation.</summary>
        private bool AdvanceSejmFormation(Country country, Elections.GovernmentRecord g)
        {
            Elections.SejmFormation f = g.Sejm;
            if (f == null || !f.Open) { return false; }
            for (int guard = 0; guard < 8 && f.Open && CurrentDate >= f.NextOn; guard++) { SejmStep(country, f); }
            return true;
        }

        private void SejmStep(Country country, Elections.SejmFormation f)
        {
            Elections.GovernmentRecord g = country.Government;
            bool ofRecord = Elections.SejmFormation.TryOfRecord(country.Id, f.PollingDay, out Elections.SejmFormation.StepsOfRecord record);
            switch (f.Stage)
            {
                case Elections.SejmStage.AwaitingSitting:
                {
                    // Art. 162 ust. 1 and 3: the cabinet resigns at the first sitting and carries on until a new one is appointed
                    g.Discharge(CurrentDate);
                    f.Log.Add($"{CurrentDate:yyyy-MM-dd}: the new Sejm's first sitting - {PoliSim.Wording.EnglishText.Possessive(g.PmParty, false, true)} cabinet resigns and carries on until a new one is appointed (Art. 162 ust. 1, 3)");
                    // Art. 154 ust. 1: the President designates - the record's designee where the record names one, else the formation's projection (DECLARED)
                    if (ofRecord && Elections.WorldClock.TryGovernmentAt(country.Id, record.Appointed, out Elections.WorldClock.GovernmentOfRecord designee) && designee.Cabinet != null)
                    {
                        SetDesignee(f, new List<string>(designee.Cabinet), new List<string>(designee.Support ?? new string[0]), PartyOfHead(designee.Head) ?? designee.Cabinet[0]);
                        f.Log.Add($"{CurrentDate:yyyy-MM-dd}: the President designates {designee.Head}, the record's designee (M.P. 2023 poz. 1222) (Art. 154 ust. 1)");
                    }
                    else
                    {
                        Elections.GovernmentFormation.View view = Projection(country, f);
                        if (view == null || !view.HasGovernment)
                        {
                            // no designee the chamber could carry: ust. 1's fourteen days pass, and the Sejm's own step follows their expiry (ust. 3)
                            f.Log.Add($"{CurrentDate:yyyy-MM-dd}: no cabinet the Sejm could carry - {view?.Reason ?? "no formation"}; Art. 154 ust. 1's fourteen days pass without an appointment");
                            f.Stage = Elections.SejmStage.SejmElects;
                            f.NextOn = CurrentDate.AddDays(Elections.SejmFormation.StepWindowDays);
                            return;
                        }
                        SetDesignee(f, Keys(view.Cabinet), Keys(view.Support), Elections.GovernmentRecord.FromView(country, view, CurrentDate, world: _world).PmParty);
                        f.Log.Add($"{CurrentDate:yyyy-MM-dd}: the President designates {PoliSim.Wording.EnglishText.Possessive(f.PmParty, false, true)} candidate for {string.Join("+", f.Cabinet)} - the formation's projection on the new Sejm (DECLARED: no record names the designee) (Art. 154 ust. 1)");
                    }
                    f.Stage = Elections.SejmStage.Designated;
                    f.NextOn = ofRecord ? record.Appointed : CurrentDate.AddDays(Elections.SejmFormation.StepWindowDays);
                    return;
                }
                case Elections.SejmStage.Designated:
                {
                    // Art. 154 ust. 1: the President appoints the designated cabinet
                    if (SameCabinet(g, f)) { g.Reappoint(CurrentDate); }
                    else { InstallSejmCabinet(country, f, $"appointed by the President on the designation of {PoliSim.Wording.EnglishText.Possessive(f.PmParty, false, true)} candidate (Konstytucja Art. 154 ust. 1), the Sejm's confidence to follow (ust. 2)"); }
                    f.Log.Add($"{CurrentDate:yyyy-MM-dd}: the President appoints {string.Join("+", f.Cabinet)} led by {f.PmParty} (Art. 154 ust. 1){(ofRecord ? " - the record's day (M.P. 2023 poz. 1287, 1288)" : string.Empty)}");
                    f.Stage = Elections.SejmStage.AwaitingConfidence;
                    f.NextOn = ofRecord ? record.ConfidenceVote : CurrentDate.AddDays(Elections.SejmFormation.StepWindowDays);
                    return;
                }
                case Elections.SejmStage.AwaitingConfidence:
                {
                    bool carried = Elections.SejmFormation.Carries(country, f.Cabinet, f.Support, out int yes, out int members);
                    f.Log.Add($"{CurrentDate:yyyy-MM-dd}: the Sejm's confidence in {string.Join("+", f.Cabinet)} - {yes} of {members} members for: {(carried ? "carried" : "refused")} (Art. 154 ust. 2)");
                    Debug.Log($"SEJM: {country.Id} - the confidence vote on {string.Join("+", f.Cabinet)} led by {f.PmParty}: {yes} of {members}, {(carried ? "carried" : "refused")}");
                    if (carried) { Conclude(f, "the cabinet holds the Sejm's confidence"); return; }
                    // Art. 162 ust. 2 pkt 1: refused, the cabinet resigns and carries on; ust. 3 of Art. 154: the Sejm elects within fourteen days
                    country.Government.Discharge(CurrentDate);
                    f.Stage = Elections.SejmStage.SejmElects;
                    f.NextOn = ofRecord ? record.SejmElected : CurrentDate;
                    return;
                }
                case Elections.SejmStage.SejmElects:
                {
                    // A Sejm of record elects the record's cabinet - its parties and its leader from the government of record appointed on the step's day
                    // (q8: "on the record's date where it has one", as the designee is the record's) - where those parties hold a majority of the game's own
                    // Sejm; short of it, or where the record names none, the formation's projection stands as for a Sejm the game elects (DECLARED).
                    // The rule is SejmFormation.TryRecordCabinet's, which the night's EXPECTED TO FORM reads too, so the two cannot disagree.
                    if (ofRecord && Elections.SejmFormation.TryRecordCabinet(country, f.PollingDay, out List<string> recordCabinet, out List<string> recordSupport, out string recordPm,
                        out string recordHead, out bool recordCarries, out int recordYes, out int recordMembers))
                    {
                        if (recordCarries)
                        {
                            SetDesignee(f, recordCabinet, recordSupport, recordPm);
                            f.Log.Add($"{CurrentDate:yyyy-MM-dd}: the Sejm elects {recordHead}, the record's, and {string.Join("+", f.Cabinet)}, {recordYes} of {recordMembers} members on the game's own seats (Art. 154 ust. 3) - the record's day (M.P. 2023 poz. 1380)");
                            f.Stage = Elections.SejmStage.AwaitingAppointment;
                            f.NextOn = record.SejmAppointed;
                            return;
                        }
                        f.Log.Add($"{CurrentDate:yyyy-MM-dd}: the record's cabinet, {string.Join("+", recordCabinet)}, holds {recordYes} of {recordMembers} members of the game's Sejm, short of a majority - the formation's projection is put instead");
                        Debug.Log($"SEJM: {country.Id} - the record's cabinet {string.Join("+", recordCabinet)} holds {recordYes} of {recordMembers}; the projection is put to the Sejm instead");
                    }
                    Elections.GovernmentFormation.View view = Projection(country, f);
                    bool carried = view != null && view.HasGovernment && Elections.SejmFormation.Carries(country, Keys(view.Cabinet), Keys(view.Support), out _, out _);
                    if (!carried)
                    {
                        // K302: Art. 155 ust. 1 - "W razie niepowołania Rady Ministrów w trybie art. 154 ust. 3 Prezydent Rzeczypospolitej w ciągu 14 dni powołuje
                        // Prezesa Rady Ministrów": ust. 3's own fourteen days run out, then the President appoints on the last of his (DECLARED, each step on its
                        // window's last day)
                        f.Stage = Elections.SejmStage.PresidentAppoints;
                        f.NextOn = CurrentDate.AddDays(Elections.SejmFormation.PresidentAppointsAfterSejmVoteDays);
                        f.Log.Add($"{CurrentDate:yyyy-MM-dd}: the Sejm elects no prime minister (Art. 154 ust. 3) - its fourteen days run to {CurrentDate.AddDays(Elections.SejmFormation.StepWindowDays):yyyy-MM-dd}, and the President appoints a cabinet on {f.NextOn:yyyy-MM-dd} (Art. 155 ust. 1)");
                        Debug.Log($"SEJM: {country.Id} - the Sejm elects no prime minister on {CurrentDate:yyyy-MM-dd}; the President appoints under Art. 155 ust. 1 on {f.NextOn:yyyy-MM-dd}");
                        return;
                    }
                    SetDesignee(f, Keys(view.Cabinet), Keys(view.Support), Elections.GovernmentRecord.FromView(country, view, CurrentDate, world: _world).PmParty);
                    f.Log.Add($"{CurrentDate:yyyy-MM-dd}: the Sejm elects {PoliSim.Wording.EnglishText.Possessive(f.PmParty, false, true)} candidate prime minister and {string.Join("+", f.Cabinet)}, {view.SupportedSeats} of {view.TotalSeats} members (Art. 154 ust. 3){(ofRecord ? " - the record's day (M.P. 2023 poz. 1380)" : string.Empty)}");
                    f.Stage = Elections.SejmStage.AwaitingAppointment;
                    f.NextOn = ofRecord ? record.SejmAppointed : CurrentDate.AddDays(Elections.SejmFormation.AppointmentAfterSejmElectionDays);
                    return;
                }
                case Elections.SejmStage.AwaitingAppointment:
                {
                    Elections.GovernmentRecord formed = InstallSejmCabinet(country, f, $"elected by the Sejm, {PoliSim.Wording.EnglishText.Possessive(f.PmParty, false, true)} candidate and the cabinet he proposed, and appointed by the President (Konstytucja Art. 154 ust. 3)");
                    f.Log.Add($"{CurrentDate:yyyy-MM-dd}: the President appoints the cabinet the Sejm elected - {string.Join("+", formed.Cabinet)} led by {formed.PmParty} takes office (Art. 154 ust. 3){(ofRecord ? " - the record's day (M.P. 2023 poz. 1382, 1383)" : string.Empty)}");
                    Conclude(f, "the cabinet the Sejm elected governs");
                    return;
                }
                case Elections.SejmStage.PresidentAppoints:
                {
                    // K302: Art. 155 ust. 1 - the President appoints the prime minister and, on his proposal, the cabinet: the formation's best of the cabinets the
                    // Sejm would carry by a simple majority (`SejmFormation.PresidentAppoints`, DECLARED as Art. 154 ust. 1's designee is); it governs from today,
                    // the Sejm's confidence to follow within fourteen days
                    Elections.SejmFormation.PresidentsCabinet named = Elections.SejmFormation.PresidentAppoints(country, Elections.DeclarationReading.OfElection(country.Id, f.PollingDay));
                    if (named.Cabinet.Count == 0)
                    {
                        ShortenTerm(country, f, "the President finds no admissible cabinet to appoint (Art. 155 ust. 1)");   // DECLARED: nothing the Sejm could vote on
                        return;
                    }
                    SetDesignee(f, named.Cabinet, named.Support, named.PmParty);
                    if (SameCabinet(g, f)) { g.Reappoint(CurrentDate); }
                    else { InstallSejmCabinet(country, f, $"appointed by the President, {PoliSim.Wording.EnglishText.Possessive(f.PmParty, false, true)} prime minister and the cabinet he proposed (Konstytucja Art. 155 ust. 1), the Sejm's confidence by a simple majority to follow"); }
                    f.Log.Add($"{CurrentDate:yyyy-MM-dd}: the President appoints {string.Join("+", f.Cabinet)} led by {f.PmParty}{(f.Support.Count > 0 ? " with " + string.Join("+", f.Support) : string.Empty)} (Art. 155 ust. 1) - {named.Choice()}");
                    f.Stage = Elections.SejmStage.AwaitingPresidentsConfidence;
                    f.NextOn = CurrentDate.AddDays(Elections.SejmFormation.StepWindowDays);
                    return;
                }
                case Elections.SejmStage.AwaitingPresidentsConfidence:
                {
                    // K302: "Sejm w ciągu 14 dni od dnia powołania ... udziela jej wotum zaufania większością głosów w obecności co najmniej połowy ustawowej liczby posłów"
                    Elections.SejmFormation.PresidentsCabinet vote = Elections.SejmFormation.SimpleMajority(country, f.Cabinet, f.Support, Elections.DeclarationReading.OfElection(country.Id, f.PollingDay));
                    f.Log.Add($"{CurrentDate:yyyy-MM-dd}: the Sejm's confidence in the President's cabinet - {vote.Line()}");
                    Debug.Log($"SEJM: {country.Id} - the confidence vote on the President's cabinet {vote.Line()}");
                    if (vote.Carried) { Conclude(f, "the President's cabinet holds the Sejm's confidence by a simple majority (Art. 155 ust. 1)"); return; }
                    // Art. 162 ust. 2 pkt 1: refused, the President's cabinet resigns, and carries on under ust. 3 - the caretaker until the new Sejm's cabinet, as an AI
                    // Poland's is (`FormAsAi` installs and discharges it on the first sitting)
                    country.Government.Discharge(CurrentDate);
                    ShortenTerm(country, f, $"the Sejm refused the President's cabinet its confidence, {vote.For} for and {vote.Against} against (Art. 155 ust. 1)");
                    return;
                }
            }
        }

        /// <summary>K302: Art. 155 ust. 2 - "Prezydent Rzeczypospolitej skraca kadencję Sejmu i zarządza wybory": the term shortened today and the elections ordered on
        /// Art. 98 ust. 5's day (`ScheduleExtraElection`, the player's extra election, `SejmFormation.ShortenedTermElectionDay`); the procedure concludes with the
        /// caretaker in office (Art. 162 ust. 3) until the new Sejm's cabinet.</summary>
        private void ShortenTerm(Country country, Elections.SejmFormation f, string why)
        {
            ScheduleExtraElection(country, why + " - the President shortens the Sejm's term and orders elections (Konstytucja Art. 155 ust. 2) within forty-five days (Art. 98 ust. 5)");
            Conclude(f, $"{why}; the term is shortened and elections ordered for {ExtraElectionDate:yyyy-MM-dd} (Art. 155 ust. 2, Art. 98 ust. 5) - the caretaker serves on until the new Sejm's cabinet");
        }

        /// <summary>The formation on the new Sejm under the election's own declarations - the projection the night names.</summary>
        private static Elections.GovernmentFormation.View Projection(Country country, Elections.SejmFormation f) => Projection(country, f.PollingDay);

        /// <summary>GATE-18 q20: the same on an election's polling day - the one the steps read and the night's projection reads (`ProjectFormation`).</summary>
        private static Elections.GovernmentFormation.View Projection(Country country, System.DateTime pollingDay) =>
            Elections.GovernmentFormation.ViewOf(country, Elections.DeclarationReading.OfElection(country.Id, pollingDay));

        /// <summary>Installs the cabinet the procedure names as a new government record, the procedure moving with it.</summary>
        private Elections.GovernmentRecord InstallSejmCabinet(Country country, Elections.SejmFormation f, string basis)
        {
            Elections.GovernmentRecord g = country.Government;
            Elections.GovernmentFormation.View view = Projection(country, f);
            Elections.GovernmentRecord formed;
            if (view != null && view.HasGovernment && SameKeys(Keys(view.Cabinet), f.Cabinet))
            {
                formed = Elections.GovernmentRecord.FromView(country, view, CurrentDate, basis: basis, world: _world);
            }
            else
            {
                // the record's designee (no formation stands behind it): its parties as the record names them, the prime minister's party the record's
                formed = Elections.GovernmentRecord.OfNamedCabinet(f.PmParty, f.Cabinet, f.Support, CurrentDate, basis);
                formed.Outcome = (Elections.SejmFormation.Carries(country, f.Cabinet, null, out _, out _) ? Elections.CoalitionOutcomeKind.MajorityCoalition : Elections.CoalitionOutcomeKind.MinorityGovernment).ToString();
                formed.AllocatePortfolios(country);
                formed.FormAgreements(country, CurrentDate, _world);
            }
            formed.Kind = g.Kind;
            formed.Executive = g.Executive;
            formed.Sejm = f;
            g.Sejm = null;
            HashSet<CabinetPortfolio> chairsBefore = CabinetSystem.PlayersChairs(country);   // K59
            country.Government = formed;
            CabinetSystem.SeatOnEntry(country, chairsBefore);   // K59: the Sejm's cabinet seats the chairs it hands the player
            ResetArrivalBudgetWindow(country.Id);
            CloseBudgetWindowIfNotGoverning(country);   // §698 (the review's defect 2): a window the player no longer governs is closed
            Debug.Log($"SEJM: {country.Id} - installed {string.Join("+", formed.Cabinet)} led by {formed.PmParty}{(formed.Support.Count > 0 ? " with " + string.Join("+", formed.Support) : string.Empty)} on {CurrentDate:yyyy-MM-dd}");
            return formed;
        }

        private void Conclude(Elections.SejmFormation f, string why)
        {
            f.Stage = Elections.SejmStage.Concluded;
            f.Log.Add($"{CurrentDate:yyyy-MM-dd}: concluded - {why}");
            Debug.Log($"SEJM: concluded on {CurrentDate:yyyy-MM-dd} - {why}");
        }

        private static void SetDesignee(Elections.SejmFormation f, List<string> cabinet, List<string> support, string pmParty)
        {
            f.Designate(cabinet, support, pmParty);
        }

        private static bool SameCabinet(Elections.GovernmentRecord g, Elections.SejmFormation f) => g != null && g.PmParty == f.PmParty && SameKeys(g.Cabinet, f.Cabinet);

        private static bool SameKeys(List<string> a, List<string> b)
        {
            if (a == null || b == null || a.Count != b.Count) { return false; }
            foreach (string k in a) { if (!b.Contains(k)) { return false; } }
            return true;
        }

        private static List<string> Keys(List<(string Abbrev, int Seats)> parties)
        {
            var keys = new List<string>();
            foreach ((string abbrev, int _) in parties) { keys.Add(abbrev); }
            return keys;
        }

        private static string PartyOfHead(string head) => Elections.SejmFormation.PartyOfHead(head);
    }
}
