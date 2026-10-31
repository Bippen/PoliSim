using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// COAL-1 (RULINGS_2026-10-18 q1, q21; Elias's rulings K130-K133, 2026-10-27; the audit's GA-4-1): <b>A PARTNER'S EXIT OPENS THE COUNTRY'S CONFIDENCE
    /// PROCEDURE.</b> An AI coalition partner weighs leaving at each budget vote (<see cref="WeighPartnerExits"/>, K130 - its own projected vote, read by
    /// <see cref="Elections.CoalitionExit"/>); the player's partner leaves by the player's verb (<see cref="LeaveGovernment"/>). Either exit runs
    /// <see cref="ExitOpensConfidenceProcedure"/>: where the DECLARED opposition - the parties red-lined from the remaining cabinet by their own lines - can carry
    /// one, it moves no confidence - the Riksdag's motion, the Bundestag's Art. 67 or the Sejm's Art. 158 constructive vote - with the s641 mover-preference gate
    /// lifted (K131), the departing partner voting by its own declared lines through the existing vote (K132) and never moving; otherwise the cabinet governs as
    /// the minority the record now names (`GovernmentRecord.LeaveCabinet` relabels it), and its next budget is voted on its new support - every division reads
    /// who governs from the stored record (`GovernmentFormation.TryGovernment`). Sweden, Germany and Poland (K133); the player's country only, as every AI
    /// motion; deterministic - no stream is drawn.
    /// </summary>
    public partial class SimulationManager
    {
        /// <summary>COAL-1 (the review's M2; SP-7b, I14 (2a)): the player's party as a party in the chamber - none while the player holds the presidency, when
        /// his party is the AI's (it may leave by K130, move Art. 158 and sit in an AI successor's cabinet, as every AI party).</summary>
        private static string PartyInChamber(Country country) => country.PlayerHoldsPresidency ? null : country.PlayerPartyAbbrev;

        /// <summary>
        /// K130 (Elias, 2026-10-27): at a budget vote, each AI partner of the cabinet - never the head of government's party, never the player's (it leaves by
        /// the player's verb; a player-president's party is the AI's), never a parliamentary group's member whose partner sits in the cabinet with it (§705: the
        /// CSU does not leave the Union's cabinet alone) - leaves when its projected share outside beats its share inside by more than one sigma_v; never within
        /// <see cref="Elections.CoalitionExit.QuietDays"/> of a polling day. DECLARED readings: "within 90 days of a polling day" is read on both sides of one,
        /// and a polling day is every national one the game holds - the chamber's (an ordered extra election among them) and, where the country elects its
        /// president by the people, the president's rounds (the review's L2: Poland's first vote and run-off); the last is the game's own latest, else the
        /// chamber of record's election; at most ONE partner leaves at a vote, the one whose share outside beats its share inside by the most, a tie to the
        /// key first in ordinal order (the procedure its exit opens decides what follows, and the rest weigh again at the next vote); no exit is weighed while
        /// the government is a caretaker, a declaration's week runs, a round or the Sejm's formation is open, or an extra election is pending.
        /// </summary>
        private void WeighPartnerExits(Country country)
        {
            if (!PlayerCountryId.HasValue || PlayerCountryId.Value != country.Id) { return; }
            Elections.ConfidenceProcedure.Rules rules = Elections.ConfidenceProcedure.RulesOf(country.Id);
            if (rules != Elections.ConfidenceProcedure.Rules.Riksdag && !Elections.ConfidenceProcedure.IsConstructive(country.Id)) { return; }
            Elections.GovernmentRecord g = country.Government;
            if (g == null || g.Caretaker || g.Cabinet.Count < 2 || g.NoConfidenceOn != System.DateTime.MinValue) { return; }
            if ((g.Round != null && g.Round.Open) || ElectionAwaitsRound(country, g) || (g.Sejm != null && g.Sejm.Open)) { return; }
            if (_extraElectionDate != System.DateTime.MinValue && _extraElectionDate >= CurrentDate) { return; }
            if (Elections.CoalitionExit.InQuietWindow(CurrentDate, NextPollingDayOrNone(country), LastPollingDay(country))) { return; }
            string playersParty = PartyInChamber(country);
            string leaving = null;
            double leavingInside = 0.0, leavingOutside = 0.0;
            foreach (string partner in g.Cabinet)
            {
                if (partner == g.PmParty || partner == playersParty) { continue; }
                if (SeatedGroupPartner(country, partner) is string groupPartner && g.Cabinet.Contains(groupPartner)) { continue; }
                if (!Elections.CoalitionExit.TryProjection(country, partner, CurrentDate, out double inside, out double outside)) { continue; }
                if (!Elections.CoalitionExit.Leaves(inside, outside)) { continue; }
                if (leaving == null || outside - inside > leavingOutside - leavingInside || (outside - inside == leavingOutside - leavingInside && string.CompareOrdinal(partner, leaving) < 0))
                {
                    leaving = partner; leavingInside = inside; leavingOutside = outside;
                }
            }
            if (leaving == null) { return; }
            HashSet<CabinetPortfolio> chairsBefore = CabinetSystem.PlayersChairs(country);   // K59 (the review's L4): the departed partner's posts may fall to a junior-partner player
            g.LeaveCabinet(leaving, country);   // the portfolios re-apportioned among those who stay, the record relabelled by its seats (GA-3-4)
            CabinetSystem.SeatOnEntry(country, chairsBefore);   // K59: a chair the exit hands the player is seated at the average, as every install path seats one
            RecordLeaving(country, leaving, g.PmParty, false, "K130: its projected share outside beat its share inside by more than one sigma_v at the budget vote");   // COAL-2: the leaving on the country's record of verbs
            g.Breaks.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd}: {1} left the government at the budget vote - its projected share outside {2:0.0} % against {3:0.0} % inside, more than one sigma_v ({4:0.0} points; COAL-1, K130)",
                CurrentDate, leaving, leavingOutside * 100.0, leavingInside * 100.0, Elections.CoalitionExit.Threshold * 100.0));
            Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture, "CONFIDENCE: {0} - {1} left the government at the budget vote: projected {2:0.0} % outside against {3:0.0} % inside (sigma_v {4:0.0} points)",
                country.Id, leaving, leavingOutside * 100.0, leavingInside * 100.0, Elections.CoalitionExit.Threshold * 100.0));
            ExitOpensConfidenceProcedure(country, leaving);
        }

        /// <summary>
        /// q21, K131, K132: a partner has left - the declared opposition moves no confidence where it can carry one (<see cref="TryAiMotion"/> with the exit named:
        /// the gate lifted, the movers the declared opposition, the departed partner a voter and never the mover); otherwise the cabinet governs as the minority
        /// its record names, and the government's breaks say why nothing was moved (the review's L1): the guard that took no motion up, or what the declared
        /// opposition lacked. The player's country only (the procedure's week, discharge and round are its alone).
        /// </summary>
        private void ExitOpensConfidenceProcedure(Country country, string departed)
        {
            if (!PlayerCountryId.HasValue || PlayerCountryId.Value != country.Id) { return; }
            if (!Elections.ConfidenceProcedure.TakesMotions(country.Id) && !Elections.ConfidenceProcedure.IsConstructive(country.Id)) { return; }
            Elections.GovernmentRecord g = country.Government;
            if (g == null || g.Caretaker || g.Cabinet.Count == 0) { return; }
            string guard = ExitMotionGuard(country, g);   // read before the motion: what would stop TryAiMotion before any mover is weighed
            TryAiMotion(country, departed);
            if (!ReferenceEquals(country.Government, g) || g.NoConfidenceOn != System.DateTime.MinValue) { return; }   // a successor installed (Art. 67, Art. 158), or the Riksdag's week opened
            int members = 0, cabinet = 0, support = 0;
            foreach (KeyValuePair<string, int> seat in country.ParliamentSeats)
            {
                members += seat.Value;
                if (g.Cabinet.Contains(seat.Key)) { cabinet += seat.Value; } else if (g.Support.Contains(seat.Key)) { support += seat.Value; }
            }
            string why = guard ?? ExitMotionLacked(country, g, departed, members);
            g.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: after {departed} left, no motion is moved - {why}; {string.Join("+", g.Cabinet)} governs as {g.Outcome} on {cabinet + support} of {members} seats, and its next budget is voted on that support (COAL-1, K131)");
            Debug.Log($"CONFIDENCE: {country.Id} - after {departed} left, no motion is moved ({why}): {string.Join("+", g.Cabinet)} governs as {g.Outcome}, {cabinet + support} of {members} seats");
        }

        /// <summary>L1: the guard <see cref="TryAiMotion"/> (and <see cref="TryAiConstructiveVote"/>) would return on before weighing any mover - the same tests, in
        /// their order - or null where none stands.</summary>
        private string ExitMotionGuard(Country country, Elections.GovernmentRecord g)
        {
            Elections.ConfidenceProcedure.Rules rules = Elections.ConfidenceProcedure.RulesOf(country.Id);
            if (rules == Elections.ConfidenceProcedure.Rules.Riksdag)
            {
                if (_extraElectionDate != System.DateTime.MinValue && _extraElectionDate >= CurrentDate) { return "an extra election is ordered, and no motion is taken up before the new Riksdag"; }
                if (TryPlayerPollingDay(out System.DateTime riksdagPolling) && riksdagPolling <= CurrentDate.AddDays(Elections.ConfidenceProcedure.ExtraElectionWindowDays)) { return "the next polling day falls within the week, and no motion is taken up in it"; }
                return null;
            }
            if (g.Sejm != null && g.Sejm.Open) { return "the Sejm's formation runs (Art. 154), and no Art. 158 vote is taken up in it"; }
            if ((g.Round != null && g.Round.Open) || ElectionAwaitsRound(country, g)) { return "the chancellor's election runs (Art. 63), and no Art. 67 vote is taken up in it"; }
            if (TryPlayerPollingDay(out System.DateTime polling) && polling <= CurrentDate.AddDays(Elections.ConfidenceProcedure.ExtraElectionWindowDays)) { return "the next polling day falls within the week, and no constructive vote is taken up in it"; }
            return null;
        }

        /// <summary>L1: why no mover carried one, where no guard stood - the declared opposition short of the majority a Riksdag motion needs, or, holding it, none of
        /// its parties one the AI moves for (the player's own party moves by the player's verb, the departed partner moves nothing, a mover needs a tenth of the
        /// members); under a constructive vote, no declared party's candidate elected by the majority it needs.</summary>
        private string ExitMotionLacked(Country country, Elections.GovernmentRecord g, string departed, int members)
        {
            HashSet<string> declared = Elections.GovernmentFormation.RedLinedFrom(country, g.Cabinet, CurrentDate);
            var named = new List<string>();
            int seats = 0;
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                if (!declared.Contains(party.Abbrev)) { continue; }
                int held = SeatsOf(country, party.Abbrev);
                if (held <= 0) { continue; }
                named.Add(party.Abbrev);
                seats += held;
            }
            string who = named.Count > 0 ? string.Join("+", named) : "none";
            if (Elections.ConfidenceProcedure.IsConstructive(country.Id))
            {
                return $"the declared opposition ({who}, {seats} seats) puts up no candidate the chamber elects - none of its parties the AI moves for carries a constructive vote";
            }
            int needed = members / 2 + 1;
            if (seats < needed) { return $"the declared opposition ({who}) holds {seats} of the {needed} seats a motion needs"; }
            return $"the declared opposition ({who}) holds {seats} of the {needed} seats a motion needs, but none of its parties the AI moves for can take one up (the player's party moves by the player's verb, {departed} moves nothing, a mover needs a tenth of the members)";
        }

        /// <summary>K130 (L2): the next national polling day in the player's country - the chamber's (an ordered extra election among them) or the president's
        /// next round, whichever is first; MinValue where none is modelled.</summary>
        private System.DateTime NextPollingDayOrNone(Country country)
        {
            System.DateTime next = TryPlayerPollingDay(out System.DateTime chamber) ? chamber : System.DateTime.MinValue;
            void Earlier(System.DateTime day) { if (day != System.DateTime.MinValue && day >= CurrentDate.Date && (next == System.DateTime.MinValue || day < next)) { next = day; } }
            if (Elections.TwoRoundElection.RuleOf(country.Id) != null)
            {
                if (Elections.PresidentialElection.TryNextFirstVote(country, CurrentDate.Date, out System.DateTime first, out System.DateTime _, out string _)) { Earlier(first); }
                if (country.PresidentialElections != null) { foreach (Elections.PresidentialElection.Contest c in country.PresidentialElections) { if (c.RunOffPending()) { Earlier(c.RunOffOn); } } }
            }
            return next;
        }

        /// <summary>K130 (L2): the last national polling day on or before today - the game's own latest (any election it holds: the chamber's, the president's
        /// rounds), else the chamber of record's election.</summary>
        private System.DateTime LastPollingDay(Country country)
        {
            System.DateTime last = Elections.WorldClock.ChamberAt(country.Id, CurrentDate).ElectionDay;
            if (last > CurrentDate) { last = System.DateTime.MinValue; }
            void Later(System.DateTime day) { if (day != System.DateTime.MinValue && day <= CurrentDate && day > last) { last = day; } }
            if (country.ElectionHistory != null) { foreach (Elections.ElectionRecord held in country.ElectionHistory) { Later(held.Date); } }
            foreach (Elections.PresidentialElection.Contest c in country.PresidentialElections ?? new List<Elections.PresidentialElection.Contest>())
            {
                Later(c.FirstVote);
                if (c.RunOffOn != System.DateTime.MinValue && !c.RunOffPending()) { Later(c.RunOffOn); }
            }
            return last;
        }
    }
}
