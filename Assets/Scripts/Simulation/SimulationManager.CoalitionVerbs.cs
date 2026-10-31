using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// COAL-2 (RULINGS_2026-10-18 q1, q19; Elias's K136 and K137 of 2026-10-27; the political-system spec's §5.2; Design's §9 return of 2026-10-30, one
    /// grammar): <b>THE COALITION VERBS</b> - the player's threat to leave (<see cref="Threaten"/>, <see cref="WithdrawThreat"/>), the head's dismissal of a
    /// partner (<see cref="Dismiss"/>), a supporter's enforcement of its agreement (<see cref="Enforce"/>) and a party's re-declaration of its bloc after a count
    /// (<see cref="Redeclare"/>; the AI's by K137, <see cref="RedeclareAfterCount"/>). Each verb is the manager's, answered through FORK-2's
    /// <see cref="Answered"/> so every except-world gives it again on its day; each has its gate as a reader (the K197 idiom: the desk draws the verb disabled
    /// with the verb's own reason). The open motions settle by the day loop (<see cref="AdvanceCoalitionVerbsDay"/>), in the player's country only - as every
    /// AI motion and COAL-1's exit are (K239) - so a world with no player country holds none and nothing here runs. The rules are
    /// <see cref="Elections.CoalitionVerbs"/>'s. Deterministic: no stream is drawn.
    /// <para><b>What the rulings do not give, and is not built</b> (stated, not modelled): an AI partner's threat (K130 rules when an AI partner leaves, at a
    /// budget vote on its projected vote, and nothing before it - no trigger, demand or deadline for a threat); an AI head's dismissal of a partner; an AI
    /// government's answer to a demand (AI governments table no law in this model, so a demand is delivered only through a path the model has - a bill the
    /// player's party carries, a law already moving, a dial a portfolio's holder moves); an AI supporter's enforcement beyond §654's rule that an owed item
    /// breaks at its second budget vote.</para>
    /// </summary>
    public partial class SimulationManager
    {
        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // The gates, as readers
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>The tests every coalition verb makes first: the player's country, a stored government, a country whose verbs are modelled (K133), and the
        /// party a party in the chamber (SP-7b: a player-president's party is the AI's). Null where they pass, else the reason.</summary>
        private string CoalitionVerbRefusal(CountryId countryId, out Country country)
        {
            country = _world?.GetCountry(countryId);
            if (country?.Government == null || !PlayerCountryId.HasValue || PlayerCountryId.Value != countryId) { return "NOT THE PLAYER'S COUNTRY"; }
            if (!Elections.CoalitionVerbs.Applies(countryId)) { return "THIS COUNTRY'S COALITION VERBS ARE NOT YET MODELLED"; }
            if (country.PlayerHoldsPresidency) { return PresidentOfficeWords; }
            return null;
        }

        /// <summary>COAL-2: whether <see cref="Threaten"/> would be accepted today, and why not. A threat is a promise to leave, so where leaving is refused the
        /// threat is too (<see cref="CanLeaveGovernment"/>'s own reason); one threat stands at a time; and a threat needs a demand the lead's line allows.</summary>
        public bool CanThreaten(CountryId countryId, out string refusedBecause)
        {
            refusedBecause = CoalitionVerbRefusal(countryId, out Country country);
            if (refusedBecause != null) { return false; }
            GovernmentRecord g = country.Government;
            if (g.RoleOf(country.PlayerPartyAbbrev) != PlayerRole.JuniorPartner) { refusedBecause = "YOUR PARTY IS NOT A JUNIOR PARTNER"; return false; }
            if (Elections.CoalitionVerbs.OpenOf(country, CoalitionVerb.Threaten, country.PlayerPartyAbbrev) != null) { refusedBecause = "YOUR THREAT STANDS UNTIL ITS DAY"; return false; }
            if (!CanLeaveGovernment(countryId, out string leave)) { refusedBecause = leave; return false; }
            if (ThreatDemands(countryId).Count == 0) { refusedBecause = "NO DEMAND OF YOURS THE LEAD'S LINE ALLOWS"; return false; }
            refusedBecause = null;
            return true;
        }

        /// <summary>COAL-2: the demands the player's party may name in a threat today - its own, the lead's compatibility allowing each (the spec's §5.3 rule
        /// for a supporter's demands, `Elections.CoalitionVerbs.DemandsOf`). Empty outside a junior partner's seat.</summary>
        public List<AgreementItem> ThreatDemands(CountryId countryId)
        {
            Country country = _world?.GetCountry(countryId);
            GovernmentRecord g = country?.Government;
            if (g == null || string.IsNullOrEmpty(country.PlayerPartyAbbrev) || g.RoleOf(country.PlayerPartyAbbrev) != PlayerRole.JuniorPartner) { return new List<AgreementItem>(); }
            return Elections.CoalitionVerbs.DemandsOf(country, country.PlayerPartyAbbrev, g.PmParty, g.FormedOn, _world);
        }

        /// <summary>COAL-2: whether <see cref="WithdrawThreat"/> would be accepted today.</summary>
        public bool CanWithdrawThreat(CountryId countryId, out string refusedBecause)
        {
            refusedBecause = CoalitionVerbRefusal(countryId, out Country country);
            if (refusedBecause != null) { return false; }
            if (Elections.CoalitionVerbs.OpenOf(country, CoalitionVerb.Threaten, country.PlayerPartyAbbrev) == null) { refusedBecause = "NO THREAT OF YOURS STANDS"; return false; }
            return true;
        }

        /// <summary>COAL-2: whether <see cref="Dismiss"/> would put <paramref name="partner"/> out today, and why not. DECLARED (Code's reading): the head may
        /// dismiss in any season, as the player may leave in any (K270); a caretaker dismisses no one; a parliamentary group's member is not put out alone
        /// (§705, the leaving's own rule read from the other side).</summary>
        public bool CanDismiss(CountryId countryId, string partner, out string refusedBecause)
        {
            refusedBecause = CoalitionVerbRefusal(countryId, out Country country);
            if (refusedBecause != null) { return false; }
            GovernmentRecord g = country.Government;
            if (g.RoleOf(country.PlayerPartyAbbrev) != PlayerRole.PrimeMinister) { refusedBecause = "ONLY THE HEAD OF GOVERNMENT'S PARTY DISMISSES A PARTNER"; return false; }
            if (g.Caretaker) { refusedBecause = "A CARETAKER GOVERNMENT DISMISSES NO PARTNER"; return false; }
            if (string.IsNullOrEmpty(partner) || !g.Cabinet.Contains(partner) || partner == g.PmParty)
            {
                refusedBecause = (string.IsNullOrEmpty(partner) ? "THE PARTY" : PartySystems.ShortName(countryId, partner).ToUpperInvariant()) + " IS NOT A PARTNER IN YOUR CABINET";
                return false;
            }
            if (SeatedGroupPartner(country, partner) is string groupPartner && g.Cabinet.Contains(groupPartner))
            {
                refusedBecause = PartySystems.ShortName(countryId, partner).ToUpperInvariant() + " SITS IN ONE FRAKTION WITH " + PartySystems.ShortName(countryId, groupPartner).ToUpperInvariant() + " - IT IS NOT DISMISSED ALONE";
                return false;
            }
            refusedBecause = null;
            return true;
        }

        /// <summary>COAL-2: whether <see cref="Enforce"/> would be accepted today - a support party with an agreement, one of whose items is still owed, and no
        /// demand of its own already standing.</summary>
        public bool CanEnforce(CountryId countryId, out string refusedBecause)
        {
            refusedBecause = CoalitionVerbRefusal(countryId, out Country country);
            if (refusedBecause != null) { return false; }
            GovernmentRecord g = country.Government;
            if (g.RoleOf(country.PlayerPartyAbbrev) != PlayerRole.Support) { refusedBecause = "YOUR PARTY IS NOT A SUPPORT PARTY"; return false; }
            SupportAgreement agreement = g.AgreementOf(country.PlayerPartyAbbrev);
            if (agreement == null) { refusedBecause = "YOUR PARTY HOLDS NO AGREEMENT"; return false; }
            if (Elections.CoalitionVerbs.OpenOf(country, CoalitionVerb.Enforce, country.PlayerPartyAbbrev) != null) { refusedBecause = "YOUR DEMAND STANDS UNTIL ITS DAY"; return false; }
            if (Elections.CoalitionVerbs.OwedOf(agreement).Count == 0) { refusedBecause = "NOTHING IS OWED UNDER YOUR AGREEMENT"; return false; }
            refusedBecause = null;
            return true;
        }

        /// <summary>
        /// COAL-2 (K137, Design's "after any count, every party may RE-DECLARE its bloc"): whether the player's party may re-declare <paramref name="side"/> today.
        /// DECLARED (Code's readings): once a count has formed its government (<see cref="Elections.CoalitionVerbs.FormedAfter"/>), once per count; a party in
        /// the cabinet or behind it declares FOR it only - against its own government it leaves or withdraws first.
        /// </summary>
        public bool CanRedeclare(CountryId countryId, BlocSide side, out string refusedBecause)
        {
            refusedBecause = CoalitionVerbRefusal(countryId, out Country country);
            if (refusedBecause != null) { return false; }
            GovernmentRecord g = country.Government;
            System.DateTime count = Elections.CoalitionVerbs.LatestCount(country, CurrentDate);
            if (count == System.DateTime.MinValue) { refusedBecause = "NO COUNT HELD YET - A PARTY RE-DECLARES AFTER ONE"; return false; }
            if (!Elections.CoalitionVerbs.FormedAfter(g, count)) { refusedBecause = "THE COUNT'S GOVERNMENT IS NOT YET FORMED"; return false; }
            CoalitionMotion mine = Elections.CoalitionVerbs.LatestOf(country, CoalitionVerb.Redeclare, country.PlayerPartyAbbrev);
            if (mine != null && mine.ByPlayer && mine.On.Date >= count.Date) { refusedBecause = "YOU RE-DECLARED AFTER THIS COUNT"; return false; }
            PlayerRole role = g.RoleOf(country.PlayerPartyAbbrev);
            if (side != BlocSide.For && role == PlayerRole.JuniorPartner) { refusedBecause = "LEAVE THE GOVERNMENT FIRST"; return false; }
            if (side != BlocSide.For && role == PlayerRole.PrimeMinister) { refusedBecause = "THE GOVERNMENT DECLARES FOR ITSELF"; return false; }
            if (side != BlocSide.For && role == PlayerRole.Support) { refusedBecause = "WITHDRAW YOUR SUPPORT FIRST"; return false; }
            refusedBecause = null;
            return true;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // The verbs
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>COAL-2: the player's junior partner threatens to leave unless <paramref name="demandKey"/> (one of <see cref="ThreatDemands"/>, by its
        /// `SupportAgreement.KeyOf`) is delivered within <paramref name="days"/> (one of `CoalitionVerbs.DeadlineDays`). Nothing changes until its day.</summary>
        public bool Threaten(CountryId countryId, string demandKey, int days, out string refusedBecause)
        {
            string why = null;
            bool acted = Answered(new PlayerAnswer("a threat to leave", countryId, sim => sim.Threaten(countryId, demandKey, days, out _)), () => ThreatenNow(countryId, demandKey, days, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="Threaten"/>'s act.</summary>
        private bool ThreatenNow(CountryId countryId, string demandKey, int days, out string refusedBecause)
        {
            if (!CanThreaten(countryId, out refusedBecause)) { return false; }
            if (System.Array.IndexOf(Elections.CoalitionVerbs.DeadlineDays, days) < 0) { refusedBecause = "A DEADLINE OF 30, 60 OR 90 DAYS"; return false; }
            AgreementItem demand = null;
            foreach (AgreementItem item in ThreatDemands(countryId)) { if (SupportAgreement.KeyOf(item) == demandKey) { demand = item; break; } }
            if (demand == null) { refusedBecause = "NOT A DEMAND THE LEAD'S LINE ALLOWS"; return false; }
            Country country = _world.GetCountry(countryId);
            GovernmentRecord g = country.Government;
            var threat = new CoalitionMotion
            {
                Verb = CoalitionVerb.Threaten, Actor = country.PlayerPartyAbbrev, Target = g.PmParty, Demand = demandKey, DemandName = demand.Name, DialTarget = demand.Target,
                On = CurrentDate, Deadline = CurrentDate.AddDays(days), State = MotionState.Open, ByPlayer = true, Government = Elections.CoalitionVerbs.GovernmentKey(g),
                Basis = "COAL-2: a demand from the party's own positions, the lead's compatibility allowing it (the spec's §5.3)",
            };
            country.CoalitionMotions.Add(threat);
            g.Breaks.Add(string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd}: {1} threatened to leave the government unless '{2}' by {3:yyyy-MM-dd} (COAL-2)", CurrentDate, threat.Actor, threat.DemandName, threat.Deadline));
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "COALITION: {0} - {1} threatens to leave unless '{2}' by {3:yyyy-MM-dd}", countryId, threat.Actor, threat.DemandName, threat.Deadline));
            return true;
        }

        /// <summary>COAL-2: the player takes the standing threat back - withdrawn, nothing changes.</summary>
        public bool WithdrawThreat(CountryId countryId, out string refusedBecause)
        {
            string why = null;
            bool acted = Answered(new PlayerAnswer("a threat withdrawn", countryId, sim => sim.WithdrawThreat(countryId, out _)), () => WithdrawThreatNow(countryId, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="WithdrawThreat"/>'s act.</summary>
        private bool WithdrawThreatNow(CountryId countryId, out string refusedBecause)
        {
            if (!CanWithdrawThreat(countryId, out refusedBecause)) { return false; }
            Country country = _world.GetCountry(countryId);
            CoalitionMotion threat = Elections.CoalitionVerbs.OpenOf(country, CoalitionVerb.Threaten, country.PlayerPartyAbbrev);
            Settle(threat, MotionState.Withdrawn, "withdrawn by " + threat.Actor);
            Debug.Log($"COALITION: {countryId} - {threat.Actor} withdrew its threat");
            return true;
        }

        /// <summary>COAL-2: the player's party, heading the government, dismisses <paramref name="partner"/> - struck from the cabinet, the posts re-apportioned
        /// among those who stay (a chair the dismissal hands the player seated at the average, K59), and, a partner's exit, the country's confidence procedure
        /// opened (q21, K131; the dismissed partner votes by its own lines, K132).</summary>
        public bool Dismiss(CountryId countryId, string partner, out string refusedBecause)
        {
            string why = null;
            bool acted = Answered(new PlayerAnswer("a partner dismissed", countryId, sim => sim.Dismiss(countryId, partner, out _)), () => DismissNow(countryId, partner, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="Dismiss"/>'s act.</summary>
        private bool DismissNow(CountryId countryId, string partner, out string refusedBecause)
        {
            if (!CanDismiss(countryId, partner, out refusedBecause)) { return false; }
            Country country = _world.GetCountry(countryId);
            GovernmentRecord g = country.Government;
            HashSet<CabinetPortfolio> chairsBefore = CabinetSystem.PlayersChairs(country);
            g.LeaveCabinet(partner, country);   // the posts re-apportioned among those who stay, the record relabelled by its seats (GA-3-4)
            CabinetSystem.SeatOnEntry(country, chairsBefore);   // K59: a chair the dismissal hands the player is seated at the average
            g.Breaks.Add(Elections.CoalitionVerbs.DismissedLine(CurrentDate, g.PmParty, partner));
            country.CoalitionMotions.Add(new CoalitionMotion
            {
                Verb = CoalitionVerb.Dismiss, Actor = g.PmParty, Target = partner, On = CurrentDate, State = MotionState.Carried, SettledOn = CurrentDate, ByPlayer = true,
                Outcome = Elections.CoalitionVerbs.StandingLine(country), Basis = "COAL-2: the spec's §5.2, the head's dismissal",
            });
            Debug.Log($"COALITION: {countryId} - {g.PmParty} dismissed {partner}; {string.Join("+", g.Cabinet)} governs as {g.Outcome}");
            ExitOpensConfidenceProcedure(country, partner);   // q21 (K131, K132): a partner's exit opens the country's confidence procedure
            return true;
        }

        /// <summary>COAL-2: the player's support party calls <paramref name="itemKey"/> of its agreement (an owed item, by `SupportAgreement.KeyOf`) due within
        /// <paramref name="days"/>. DECLARED: delivered by then, it is met; not, the government has refused it and the item breaks that day.</summary>
        public bool Enforce(CountryId countryId, string itemKey, int days, out string refusedBecause)
        {
            string why = null;
            bool acted = Answered(new PlayerAnswer("an agreement enforced", countryId, sim => sim.Enforce(countryId, itemKey, days, out _)), () => EnforceNow(countryId, itemKey, days, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="Enforce"/>'s act.</summary>
        private bool EnforceNow(CountryId countryId, string itemKey, int days, out string refusedBecause)
        {
            if (!CanEnforce(countryId, out refusedBecause)) { return false; }
            if (System.Array.IndexOf(Elections.CoalitionVerbs.DeadlineDays, days) < 0) { refusedBecause = "A DEADLINE OF 30, 60 OR 90 DAYS"; return false; }
            Country country = _world.GetCountry(countryId);
            GovernmentRecord g = country.Government;
            SupportAgreement agreement = g.AgreementOf(country.PlayerPartyAbbrev);
            AgreementItem item = Elections.CoalitionVerbs.ItemOf(agreement, itemKey);
            if (item == null || item.State != AgreementState.Owed) { refusedBecause = "NOT AN ITEM STILL OWED UNDER YOUR AGREEMENT"; return false; }
            var demand = new CoalitionMotion
            {
                Verb = CoalitionVerb.Enforce, Actor = country.PlayerPartyAbbrev, Target = g.PmParty, Demand = itemKey, DemandName = item.Name, DialTarget = item.Target,
                On = CurrentDate, Deadline = CurrentDate.AddDays(days), State = MotionState.Open, ByPlayer = true, Government = Elections.CoalitionVerbs.GovernmentKey(g),
                Basis = "COAL-2: the spec's §5.2, the supporter enforces its agreement",
            };
            country.CoalitionMotions.Add(demand);
            g.Breaks.Add(string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd}: {1} called '{2}' of its agreement due by {3:yyyy-MM-dd} (COAL-2)", CurrentDate, demand.Actor, demand.DemandName, demand.Deadline));
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "COALITION: {0} - {1} enforces '{2}' by {3:yyyy-MM-dd}", countryId, demand.Actor, demand.DemandName, demand.Deadline));
            return true;
        }

        /// <summary>COAL-2 (K137): the player's party re-declares its side toward the government the last count formed - recorded from today; in Sweden its bloc
        /// from today (`CoalitionVerbs.BlocOfSide`).</summary>
        public bool Redeclare(CountryId countryId, BlocSide side, out string refusedBecause)
        {
            string why = null;
            bool acted = Answered(new PlayerAnswer("a bloc re-declared", countryId, sim => sim.Redeclare(countryId, side, out _)), () => RedeclareNow(countryId, side, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: <see cref="Redeclare"/>'s act.</summary>
        private bool RedeclareNow(CountryId countryId, BlocSide side, out string refusedBecause)
        {
            if (!CanRedeclare(countryId, side, out refusedBecause)) { return false; }
            Country country = _world.GetCountry(countryId);
            GovernmentRecord g = country.Government;
            int governmentBloc = BlocStanding(country, g.PmParty);
            country.CoalitionMotions.Add(RedeclarationOf(country, g, country.PlayerPartyAbbrev, side, governmentBloc, true,
                "COAL-2 (K137): the player's own re-declaration after the count of " + Elections.CoalitionVerbs.LatestCount(country, CurrentDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            Debug.Log($"COALITION: {countryId} - {country.PlayerPartyAbbrev} re-declared {Elections.CoalitionVerbs.SideWord(side)} {string.Join("+", g.Cabinet)}");
            return true;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // The day loop
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// COAL-2: the coalition verbs' day in the player's country - the open motions settle (<see cref="SettleCoalitionMotions"/>), then, once a count has
        /// formed its government, every party re-declares by K137 (<see cref="RedeclareAfterCount"/>). Nothing for any other country, and nothing in a world
        /// with no player country.
        /// </summary>
        private void AdvanceCoalitionVerbsDay(CountryId countryId)
        {
            if (!PlayerCountryId.HasValue || PlayerCountryId.Value != countryId || !Elections.CoalitionVerbs.Applies(countryId)) { return; }
            Country country = _world?.GetCountry(countryId);
            if (country?.Government == null) { return; }
            SettleCoalitionMotions(country);
            RedeclareAfterCount(country);
        }

        /// <summary>
        /// COAL-2: each open motion read against the country. Any open motion lapses (withdrawn) once another government governs than the one it was made to
        /// (the review's M1, <see cref="Elections.CoalitionVerbs.GovernmentKey"/>: a record a count re-forms, even of the same parties, or one discharged as a
        /// caretaker). A threat also lapses once its threatener is no longer a partner of its target's cabinet; is struck as met once its demand is delivered;
        /// and on its deadline, undelivered, is carried out - the threatener leaves by the leaving's own act (<see cref="LeaveGovernmentNow"/>: its gates, the
        /// exit opening the confidence procedure), or lapses with the leaving's refusal where that refuses. An enforcement also lapses once its supporter no
        /// longer supports; is met once its item is delivered - read live, as the threat's demand is (the review's L3: the tracker does not run in a round or a
        /// declaration's week); is refused once the item broke (§654's count may break it first) or, still owed on its deadline, is refused and breaks that day.
        /// </summary>
        private void SettleCoalitionMotions(Country country)
        {
            GovernmentRecord g = country.Government;
            foreach (CoalitionMotion m in new List<CoalitionMotion>(country.CoalitionMotions))
            {
                if (!m.IsOpen) { continue; }
                if (m.Government != null && m.Government != Elections.CoalitionVerbs.GovernmentKey(g))
                {
                    // the second review's N2: the words true for the day the key changed - a resignation, or another government appointed
                    Settle(m, MotionState.Withdrawn, g.Caretaker ? "lapsed: the government it was made to has resigned and serves on as a caretaker"
                        : "lapsed: the government it was made to no longer governs - another was appointed");
                    continue;
                }
                if (m.Verb == CoalitionVerb.Threaten)
                {
                    if (g.PmParty != m.Target || !g.Cabinet.Contains(m.Actor)) { Settle(m, MotionState.Withdrawn, "lapsed: " + m.Actor + " is no longer a partner in " + PoliSim.Wording.EnglishText.Possessive(m.Target, false, true) + " cabinet"); continue; }
                    if (Elections.CoalitionVerbs.Delivered(country, m)) { Settle(m, MotionState.Met, "met: '" + m.DemandName + "' delivered - " + m.Actor + " stays"); continue; }
                    if (CurrentDate.Date < m.Deadline.Date) { continue; }
                    string refused = m.Actor != country.PlayerPartyAbbrev ? "the threatener is not the player's party" : CanLeaveGovernment(country.Id, out string leave) ? null : leave.ToLowerInvariant();
                    if (refused != null) { Settle(m, MotionState.Withdrawn, "lapsed on its day: " + refused); continue; }
                    Settle(m, MotionState.Carried, "carried out: '" + m.DemandName + "' undelivered on its day - " + m.Actor + " leaves");
                    LeaveGovernmentNow(country.Id, out string _, "COAL-2: the threat of " + m.On.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " carried out");   // the review's L4: the leaving's own act, never a copy (FORK-2's replay stays out: the threat was the answer)
                    g = country.Government;
                    continue;
                }
                if (m.Verb == CoalitionVerb.Enforce)
                {
                    SupportAgreement agreement = g.AgreementOf(m.Actor);
                    AgreementItem item = Elections.CoalitionVerbs.ItemOf(agreement, m.Demand);
                    if (!g.Support.Contains(m.Actor) || item == null) { Settle(m, MotionState.Withdrawn, "lapsed: " + m.Actor + " no longer supports " + PoliSim.Wording.EnglishText.Possessive(g.PmParty, false, true) + " cabinet under that agreement"); continue; }
                    if (item.State == AgreementState.Delivered || (item.State == AgreementState.Owed && Elections.CoalitionVerbs.Delivered(country, m))) { Settle(m, MotionState.Met, "met: '" + m.DemandName + "' delivered - support goes on"); continue; }
                    if (item.State == AgreementState.Broken) { Settle(m, MotionState.Refused, "refused: '" + m.DemandName + "' broken on " + item.BrokenOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)); continue; }
                    if (CurrentDate.Date < m.Deadline.Date) { continue; }
                    item.State = AgreementState.Broken;
                    item.BrokenOn = CurrentDate;
                    g.Breaks.Add($"{CurrentDate:yyyy-MM-dd}: the government refused {PoliSim.Wording.EnglishText.Possessive(m.Actor, false, true)} demand - '{m.DemandName}' undelivered on its day, broken (COAL-2)");
                    Settle(m, MotionState.Refused, "refused: '" + m.DemandName + "' undelivered on its day - broken; " + m.Actor + " may withdraw");
                }
            }
        }

        /// <summary>A motion settled today, with its line, logged.</summary>
        private void Settle(CoalitionMotion m, MotionState state, string outcome)
        {
            m.State = state;
            m.SettledOn = CurrentDate;
            m.Outcome = outcome;
            if (!IsCounterfactual) { Debug.Log($"COALITION: {m.Verb} by {m.Actor} - {outcome}"); }
        }

        /// <summary>COAL-2: a leaving on the country's record of verbs - the player's (K197, a threat carried out) or an AI partner's (K130) - so the Docket and
        /// the strip read every country's leavings the same way.</summary>
        private void RecordLeaving(Country country, string party, string head, bool byPlayer, string basis)
        {
            GovernmentRecord g = country.Government;
            country.CoalitionMotions.Add(new CoalitionMotion
            {
                Verb = CoalitionVerb.Leave, Actor = party, Target = head, On = CurrentDate, State = MotionState.Carried, SettledOn = CurrentDate, ByPlayer = byPlayer,
                Outcome = g == null ? null : Elections.CoalitionVerbs.StandingLine(country), Basis = basis,
            });
        }

        /// <summary>
        /// K137 (Elias, 2026-10-27: "a party re-declares its bloc after each count, from the formation payoff (the one formation code, K119)") as K330 narrows it
        /// (Code's recommendation under Elias's standing instruction, on the COAL-2 review's H2). On the first day the government the latest count formed is in
        /// office (<see cref="Elections.CoalitionVerbs.FormedAfter"/>: the procedure concluded, the one formation code's outcome), each seated party but the
        /// player's re-declares toward it by <see cref="Elections.CoalitionVerbs.SideAfterCount"/>: FOR where it sits in the cabinet or its recorded support;
        /// AGAINST only where its own red lines exclude it from that cabinet (`GovernmentFormation.RedLinedFrom` on the day, COAL-1's declared opposition);
        /// every other party re-declares nothing and keeps its standing bloc - being outside a cabinet is not a side (the review measured the unnarrowed rule
        /// putting V, MP and C on the other bloc after an S minority formed). Once per count; a count whose procedure forms no government re-declares nothing
        /// (the next count's does). DECLARED (Code's readings, returned as the plan's prereg): the player's country only, as COAL-1 and every AI motion (K239);
        /// the player's party re-declares by its own verb; NEITHER is the player's alone. Sweden's bloc of a side is K331's
        /// (<see cref="Elections.CoalitionVerbs.BlocOfSide"/>): FOR the head's bloc; AGAINST never the other bloc - out of the head's into none, else the party's
        /// own kept. Elsewhere the sides are recorded and drawn and no mechanism reads them (the model reads blocs in Sweden only, K53).
        /// </summary>
        private void RedeclareAfterCount(Country country)
        {
            System.DateTime count = Elections.CoalitionVerbs.LatestCount(country, CurrentDate);
            GovernmentRecord g = country.Government;
            // the second review's N1: the guard is the country's own, saved - a load or a fork never re-declares toward a successor the continuous game did not
            if (count == country.RedeclaredAfterCount || !Elections.CoalitionVerbs.FormedAfter(g, count)) { return; }
            country.RedeclaredAfterCount = count;
            foreach (CoalitionMotion m in country.CoalitionMotions) { if (m.Verb == CoalitionVerb.Redeclare && !m.ByPlayer && m.On.Date >= count.Date) { return; } }   // a save from before the field: the count's sides on the record
            string player = PartyInChamber(country);
            int governmentBloc = BlocStanding(country, g.PmParty);
            HashSet<string> redLined = Elections.GovernmentFormation.RedLinedFrom(country, g.Cabinet, CurrentDate);
            string basis = "COAL-2 (K137, K330): after the count of " + count.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ", toward the government it formed - " + string.Join("+", g.Cabinet) + (g.Support.Count > 0 ? " with " + string.Join("+", g.Support) : string.Empty);
            var sides = new List<string>();
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                if (party.Abbrev == player || SeatsOf(country, party.Abbrev) <= 0) { continue; }
                BlocSide? side = Elections.CoalitionVerbs.SideAfterCount(g, party.Abbrev, redLined);
                if (!side.HasValue) { continue; }   // K330: outside the government and not red-lined from it - nothing re-declared, the standing bloc kept
                country.CoalitionMotions.Add(RedeclarationOf(country, g, party.Abbrev, side.Value, governmentBloc, false, basis));
                sides.Add(party.Abbrev + " " + Elections.CoalitionVerbs.SideWord(side.Value));
            }
            if (!IsCounterfactual) { Debug.Log($"COALITION: {country.Id} - K137 (K330) re-declared after the count of {count:yyyy-MM-dd}: {(sides.Count > 0 ? string.Join(", ", sides) : "none")}"); }
        }

        /// <summary>A re-declaration of <paramref name="side"/> by <paramref name="party"/> toward <paramref name="g"/>, dated today, its Sweden bloc mapped (the
        /// party's standing bloc kept where the government's head stands in no bloc).</summary>
        private CoalitionMotion RedeclarationOf(Country country, GovernmentRecord g, string party, BlocSide side, int governmentBloc, bool byPlayer, string basis) => new CoalitionMotion
        {
            Verb = CoalitionVerb.Redeclare, Actor = party, Target = g.PmParty, Demand = Elections.CoalitionVerbs.SideWord(side), DemandName = string.Join("+", g.Cabinet),
            On = CurrentDate, State = MotionState.Carried, SettledOn = CurrentDate, ByPlayer = byPlayer, Government = Elections.CoalitionVerbs.GovernmentKey(g),
            Bloc = country.Id == CountryId.Sweden ? Elections.CoalitionVerbs.BlocOfSide(side, governmentBloc, BlocStanding(country, party)) : -1, Basis = basis,
        };

        /// <summary>A party's bloc standing today in the played world - re-declared (K137) or of record (K53).</summary>
        private int BlocStanding(Country country, string party) =>
            Elections.CoalitionVerbs.TryRedeclaredBloc(country, party, CurrentDate, out int bloc) ? bloc : NationalElection.BlocOf(country.Id, party, CurrentDate);
    }
}
