using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// AF-11 (Elias's ruling q11 of 2026-10-18: <i>"every lever the UI writes gets a manager verb, and the impact ledger reads it"</i>) - **the levers'
    /// proof gate**, in <see cref="PolicyImpactLedgerDiagnostic"/>'s discipline, on the GAME's order: the turn's days run, the levers are pulled between
    /// them through <see cref="SimulationManager"/>'s verbs, and at the boundary the ledger advances, then the real game, then the no-policy shadow.
    ///
    /// <list type="number">
    /// <item><description><b>EVERY VERB ACTS</b> - each lever of the script pulls (the verb returns true) and the real game holds what it wrote.</description></item>
    /// <item><description>⚠ <b>THE LEDGER IS PASSIVE.</b> The real game pulling the levers with the ledger watching (its counterfactual worlds forked at the
    /// levers, inside the turn) is byte-identical to the same game with no ledger - every country's state, the policy rate, and the levers' own state.</description></item>
    /// <item><description>⚠ <b>THE LEVER'S FAMILY IS EXACT.</b> For each lever family, the ledger's except-world (forked at the family's first lever, handed
    /// every other family's lever on its day) is byte-identical to a world run from the seed that pulls every lever but that family's. And ONE DIAL
    /// (the review's M1): a Taxes override in the decision from turn <see cref="TaxesFromTurn"/>, forked where the game forks it - after the turn's days,
    /// before its boundary - whose except-world must equal the seed's world without it: the realignment of the dial families' forks, proved on the game's order.</description></item>
    /// <item><description><b>THE LEDGER ATTRIBUTES THEM.</b> Each lever family is a line, the lines sum to the divergence, and the central bank's head moves
    /// a stat (its rate bias reaches the economy).</description></item>
    /// </list>
    ///
    /// <para>⚠ <b>What it does not claim, stated.</b> Since FORK-1 (J8) the harness runs the player's day tick (`AdvanceCountryDayTick`) after each day, as the
    /// game does and as the ledger's forks now do (`ForkDayTickDiagnostic` holds that a fork with no policy difference equals the played world); the claim
    /// here is still the levers' - since FORK-2 the harness holds the player's election as the controller does, and the answers' replay (a bill, a meeting)
    /// is `ForkAnswersDiagnostic`'s claim. It runs in the Editor,
    /// where a manager's `Awake` does not run; the inside-the-turn fork's inactive host is what keeps play mode the same (<see cref="ShadowBaseline"/>).</para>
    /// </summary>
    public static class PlayerLeverLedgerDiagnostic
    {
        private const int Seed = 777;
        private const int Turns = 6;
        /// <summary>The turn whose decision first carries the Taxes dial (and every later one).</summary>
        private const int TaxesFromTurn = 2;
        private static readonly CountryId Player = CountryId.USA;
        private const CabinetPortfolio ProbePortfolio = CabinetPortfolio.HealthSocialAffairs;

        /// <summary>The script: each lever on a day of a turn (days run before it; 0 = before the turn's first day). The fleet's second order is on day 0
        /// of a turn whose family already has its world - the "already reached" path of the replay.</summary>
        private static readonly (int Turn, int Day, PlayerLeverKind Kind)[] Script =
        {
            (1, 100, PlayerLeverKind.PartnerTariffOverride),
            (1, 200, PlayerLeverKind.FleetOrder),
            (2, 50, PlayerLeverKind.CentralBankHead),
            (2, 300, PlayerLeverKind.MinisterAppointment),
            (3, 10, PlayerLeverKind.MinisterReshuffle),
            (4, 0, PlayerLeverKind.FleetOrder),
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            var failures = new List<string>();
            sb.Append("=== AF-11 (q11): the levers' manager verbs and the impact ledger that reads them ===\n");
            sb.Append(F("    {0} turns of {1}, the levers: {2}\n", Turns, Player, string.Join(" · ", Array.ConvertAll(Script, s => $"{s.Kind} turn {s.Turn} day {s.Day}"))));
            sb.Append(F("    the dial: a Taxes override in the decision from turn {0}, forked at that boundary as the game forks it\n", TaxesFromTurn));

            using (SimulationManager.EpochScope())
            {
                var leverFamilies = (Dictionary<PlayerLeverKind, string>)typeof(PolicyImpactLedger).GetField("LeverFamilies", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
                if (leverFamilies == null) { Fail(failures, "PolicyImpactLedger.LeverFamilies was not found - the lever families moved or were renamed"); Finish(sb, failures); return; }

                // ---- 1 and 2: the real game with the ledger watching, and without ----
                Game watched = Play(skipFamily: null, leverFamilies, withLedger: true, failures);
                Game alone = Play(skipFamily: null, leverFamilies, withLedger: false, failures);
                try
                {
                    sb.Append(failures.Count == 0 ? "    1. the verbs        every lever pulled through its verb, twice\n" : "    1. the verbs        ⚠ REFUSED - see the faults\n");
                    string a = Fingerprint(watched.World), b = Fingerprint(alone.World);
                    if (a != b) { Fail(failures, "the real game DIFFERS with the ledger watching its levers - the explanation layer changed what it explains" + FirstDifference(a, b)); }
                    sb.Append(a == b ? F("    2. the ledger       PASSIVE - the real game byte-identical over {0} turns with the ledger's worlds forked at the levers and without\n", Turns) : "    2. the ledger       ⚠ NOT PASSIVE - see the faults\n");
                    string levers = LeverState(watched.World.GetCountry(Player));
                    sb.Append("       the levers as held: ").Append(levers).Append('\n');

                    // ---- 3: each family's except-world against the seed's ----
                    var except = (Dictionary<string, ShadowBaseline>)typeof(PolicyImpactLedger).GetField("_except", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(watched.Ledger);
                    var families = new List<string> { "Taxes" };   // the dial, forked at a boundary
                    foreach (var s in Script) { string fam = leverFamilies[s.Kind]; if (!families.Contains(fam)) { families.Add(fam); } }
                    foreach (string family in families)
                    {
                        if (!except.TryGetValue(family, out ShadowBaseline world)) { Fail(failures, $"the ledger forked no '{family}' world - its levers were never read"); continue; }
                        Game seed = Play(skipFamily: family, leverFamilies, withLedger: false, failures);
                        try
                        {
                            string fromSeed = Fingerprint(seed.World), forked = Fingerprint(world.World);
                            bool exact = fromSeed == forked;
                            if (!exact) { Fail(failures, $"the '{family}' except-world is NOT the seed's world without that family's levers" + FirstDifference(fromSeed, forked)); }
                            sb.Append(F("    3. {0,-16} {1} - its levers {2}; held there: {3}\n", family, exact ? "EXACT" : "⚠ NOT EXACT", exact ? "absent, every other lever on its day" : "see the faults", LeverState(world.CountryFor(Player))));
                        }
                        finally { seed.Dispose(); }
                    }
                    if (except.Count != families.Count) { Fail(failures, $"the ledger holds {except.Count} except-world(s) for {families.Count} lever famil(ies) and no dial - a world no lever touched"); }

                    // ---- 4: the attribution ----
                    string[] stats = { "GDP", "Inflation", "Unemployment", "ApprovalRating", "Budget", "GovernmentDebt" };
                    float worstBreak = 0f, monetary = 0f;
                    var seen = new HashSet<string>();
                    foreach (string stat in stats)
                    {
                        List<ImpactLine> lines = watched.Ledger.LinesFor(watched.World.GetCountry(Player), stat, out float divergence);
                        float sum = 0f;
                        var row = new StringBuilder();
                        foreach (ImpactLine line in lines)
                        {
                            sum += line.Contribution;
                            seen.Add(line.Family);
                            if (line.Family == "Monetary") { monetary = Mathf.Max(monetary, Mathf.Abs(line.Contribution)); }
                            row.Append(line.Family).Append(' ').Append(line.Contribution.ToString("G4", CultureInfo.InvariantCulture)).Append(" · ");
                        }
                        worstBreak = Mathf.Max(worstBreak, Mathf.Abs(sum - divergence));
                        sb.Append(F("       {0,-15} divergence {1:G4}: {2}\n", stat, divergence, row.ToString().TrimEnd(' ', '·')));
                    }
                    foreach (string family in families) { if (!seen.Contains(family)) { Fail(failures, $"the ledger prints no '{family}' line"); } }
                    if (worstBreak >= 1e-3f) { Fail(failures, F("the lines do not sum to the divergence - worst break {0:G6}", worstBreak)); }
                    if (monetary <= 0f) { Fail(failures, "the central bank's head moved none of the six stats in the ledger - the lever reached no counterfactual difference"); }
                    sb.Append(F("    4. the attribution  every lever family a line, the identity's worst break {0:G3}, the head's largest line {1:G4}\n", worstBreak, monetary));
                }
                finally
                {
                    watched.Dispose();
                    alone.Dispose();
                }

                // ---- 5: the Fed's window (the K15-K17 review's H-1) ----
                FedWindow(sb, failures, leverFamilies, oath: false);
                FedWindow(sb, failures, leverFamilies, oath: true);   // the K15-K17 delta review's H-A: the role gate closing inside the window (L-E: the gate as an oath closes it, not the presidency's change itself)
            }

            Finish(sb, failures);
        }

        /// <summary>The window's planted term end, the day the player lodges on it (the hold's day), a family forked BEFORE it and one forked INSIDE the window,
        /// and the day the forks are caught up to and read - all inside one turn, the vote day (the term day plus `UsFedConsent.NominationToVoteDays`) too.</summary>
        private const int FedTermDay = 20, FedForkBefore = 10, FedForkInside = 60, FedReadDay = 80;

        /// <summary>The delta review's H-A: in the oath's case, the day the player leaves the presidency inside the window (before the vote day), and the day the
        /// inside family is forked then (still inside the window, before the oath).</summary>
        private const int FedOathDay = 40, FedForkInsideBeforeOath = 30;

        /// <summary>
        /// 5. The K15-K17 review's H-1: <b>A COUNTERFACTUAL FOLLOWS THE PLAYED WORLD THROUGH THE FED'S WINDOW.</b> The played USA's chair term is planted to end
        /// on day <c>FedTermDay</c>; the player lodges a nominee that day (the verb, the hold's day), the Vice Chair acting until the vote day. A tariff lever on
        /// day <c>FedForkBefore</c> forks its family's world BEFORE the term end (the head lever replayed into it on the term day), a fleet order on day
        /// <c>FedForkInside</c> forks its family's world INSIDE the window (the nomination pending in the copy). Read on day <c>FedReadDay</c> (each fork caught
        /// up) and again after the boundary (the vote held), each of the two holds the played world's Fed state exactly - the chair, its term, the Vice Chair's
        /// flag, the pending nominee and its vote day, the Senate's votes - where the old build had the AI's pick seated in the first and the nomination lapsed
        /// in the second. The head family's own world (its lever withheld) takes the AI's keyed pick the morning after the term day, by the AI's rule for an
        /// unanswered wait, and seats it as of the term day.
        /// <para><paramref name="oath"/> (the delta review's H-A; L-E declared: this closes the ROLE GATE, all the manager reads, and does not change the President
        /// of the day - the date-driven handover is not exercised, and under the crossing rule the count confirms either way): the player leaves the presidency on day <c>FedOathDay</c>, inside the window - the other major
        /// party seated in the player's place in the played world and in every fork on the same day, each fork caught up to it first - so the nomination
        /// LAPSES and the AI president's keyed pick is seated the next day, in the played world and identically in every fork that replays the head lever.</para>
        /// </summary>
        private static void FedWindow(StringBuilder sb, List<string> failures, Dictionary<PlayerLeverKind, string> leverFamilies, bool oath)
        {
            SimulationRandom.Seed(Seed);
            var game = new Game { Host = new GameObject(oath ? "AF-11 the Fed's window, the oath" : "AF-11 the Fed's window") };
            int insideDay = oath ? FedForkInsideBeforeOath : FedForkInside;
            DateTime oathNextDay = DateTime.MinValue;
            try
            {
                SimulationManager sim = game.Host.AddComponent<SimulationManager>();
                game.World = WorldFactory.CreateDefault();
                sim.SetWorld(game.World);
                sim.PlayerCountryId = Player;
                game.None = new ShadowBaseline(SimulationRandom.MasterSeed);
                game.Ledger = new PolicyImpactLedger(game.None);
                sim.ImpactLedger = game.Ledger;
                Country usa = game.World.GetCountry(Player);
                DateTime termDay = sim.CurrentDate.Date.AddDays(FedTermDay);
                FedChair outgoing = usa.CurrentFedChair;
                outgoing.TermEnds = termDay;   // planted: the seeded term ends inside the turn
                string headFamily = leverFamilies[PlayerLeverKind.CentralBankHead], beforeFamily = leverFamilies[PlayerLeverKind.PartnerTariffOverride], insideFamily = leverFamilies[PlayerLeverKind.FleetOrder];
                var except = (Dictionary<string, ShadowBaseline>)typeof(PolicyImpactLedger).GetField("_except", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game.Ledger);
                string why = null;
                bool lodged = false, before = false, inside = false;
                var mid = new List<string>();
                for (int d = 1; d <= SimulationManager.DaysPerTurn; d++)
                {
                    sim.AdvanceDay();
                    sim.AdvanceCountryDayTick(Player);
                    if (d < SimulationManager.DaysPerTurn) { sim.HoldPlayerElection(); }
                    if (d == FedForkBefore) { before = usa.TradePartners.Count > 0 && sim.SetPartnerTariffOverride(Player, usa.TradePartners[0].PartnerId, out why); if (!before) { Fail(failures, "5. the tariff lever before the window was refused: " + (why ?? "no trade partner")); } }
                    if (d == FedTermDay)
                    {
                        bool held = sim.CentralBankHeadAwaitsPlayer(Player);
                        lodged = held && sim.AppointCentralBankHead(Player, new FedChair("H-1 probe chair", FedChairPhilosophy.Hawkish, "the diagnostic's probe", 0.5f), out why);
                        if (!lodged) { Fail(failures, F("5. the head lever on the term day was not lodged (the hold {0}): {1}", held, why ?? "no reason")); }
                    }
                    if (d == insideDay)
                    {
                        int tech = OrderableTechnology(usa);
                        inside = tech >= 0 && sim.PlaceFleetOrder(Player, tech, EnergyFleet.StepMw(Player), usa.CalendarYear, out why);
                        if (!inside) { Fail(failures, "5. the fleet order inside the window was refused: " + (why ?? "no technology orderable")); }
                    }
                    if (oath && d == FedOathDay)
                    {
                        if (!Congress.PresidentOn(usa, sim.CurrentDate, out _, out string pp, out _) || string.IsNullOrEmpty(pp)) { Fail(failures, "5. the oath: no president found"); }
                        string outParty = pp == "REP" ? "DEM" : "REP";
                        foreach (string family in new[] { beforeFamily, insideFamily, headFamily })
                        {
                            if (!except.TryGetValue(family, out ShadowBaseline fork)) { continue; }
                            fork.CatchUp(FedOathDay);
                            fork.CountryFor(Player).PlayerPartyAbbrev = outParty;
                        }
                        usa.PlayerPartyAbbrev = outParty;   // planted: the player out of the White House, in every world on the same day
                        oathNextDay = sim.CurrentDate.Date.AddDays(1);
                        if (sim.PlayerMayIntroduce(Player, out _)) { Fail(failures, "5. the oath: the role gate is still open with " + outParty + " in the player's place"); }
                    }
                    if (d == FedReadDay)
                    {
                        string played = FedState(usa);
                        foreach (string family in new[] { beforeFamily, insideFamily })
                        {
                            if (!except.TryGetValue(family, out ShadowBaseline fork)) { Fail(failures, $"5. the ledger forked no '{family}' world in the Fed's window"); continue; }
                            fork.CatchUp(FedReadDay);
                            string forked = FedState(fork.CountryFor(Player));
                            if (forked != played) { Fail(failures, $"5. day {FedReadDay}, the '{family}' world's Fed is NOT the played world's - played [{played}], forked [{forked}]"); }
                            mid.Add(F("{0} {1}", family, forked == played ? "EXACT" : "⚠ NOT EXACT"));
                        }
                        if (except.TryGetValue(headFamily, out ShadowBaseline own))
                        {
                            own.CatchUp(FedReadDay);
                            Country h = own.CountryFor(Player);
                            FedChair ai = CentralBankTerms.AiNominee(CentralBankTerms.Fed, outgoing, termDay);
                            bool aiSeated = h.CurrentFedChair != null && h.CurrentFedChair.Name == ai?.Name && h.CurrentFedChair.TermBegan == termDay && !h.FedViceChairActing && h.FedNominationPending == null;
                            if (!aiSeated) { Fail(failures, $"5. the '{headFamily}' world (its lever withheld) did not take the AI's keyed pick {ai?.Name} as of the term day: [{FedState(h)}]"); }
                            mid.Add(F("{0} the AI's pick {1}", headFamily, aiSeated ? "seated as of the term day" : "⚠ NOT SEATED"));
                        }
                        else { Fail(failures, $"5. the ledger forked no '{headFamily}' world at the term day's lever"); }
                    }
                }
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in game.World.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                game.Ledger.AdvanceTurn(sim, game.World, Player, decisions);
                sim.AdvanceTurn(decisions);
                game.None.AdvanceTurn();
                EnergyMarket.BeginTurn(game.World);
                string end = FedState(usa);
                FedChair aiPick = CentralBankTerms.AiNominee(CentralBankTerms.Fed, outgoing, termDay);
                bool voted = oath
                    ? usa.FedSenateVotes.Count == 0 && usa.FedNominationPending == null && usa.CurrentFedChair?.Name == aiPick?.Name && usa.CurrentFedChair.TermBegan == oathNextDay && !usa.FedViceChairActing
                    : usa.FedSenateVotes.Count == 1 && usa.FedSenateVotes[0].Confirmed && usa.CurrentFedChair?.Name == "H-1 probe chair" && !usa.FedViceChairActing;
                if (!voted) { Fail(failures, oath ? $"5. the oath: the played world's nomination did not lapse with the AI's pick {aiPick?.Name} seated the day after the oath: [{end}]" : $"5. the played world's nominee was not confirmed on its vote day: [{end}]"); }
                var after = new List<string>();
                foreach (string family in new[] { beforeFamily, insideFamily })
                {
                    if (!except.TryGetValue(family, out ShadowBaseline fork)) { continue; }
                    string forked = FedState(fork.CountryFor(Player));
                    if (forked != end) { Fail(failures, $"5. after the boundary, the '{family}' world's Fed is NOT the played world's - played [{end}], forked [{forked}]"); }
                    after.Add(F("{0} {1}", family, forked == end ? "EXACT" : "⚠ NOT EXACT"));
                }
                if (oath) { sb.Append("    5. the role gate closing on day " + FedOathDay + " (the player's party out of the White House, as an oath closes it; the President of the day unchanged - declared, the second delta review's L-E), in every world\n"); }
                sb.Append(F("    5. the Fed's window the term planted to end {0:yyyy-MM-dd}, the nominee lodged that day{1}; on day {2}: {3}; after the boundary: {4} - played [{5}]\n",
                    termDay, lodged ? string.Empty : " (⚠ NOT LODGED)", FedReadDay, string.Join(" · ", mid), string.Join(" · ", after), end));
            }
            finally { game.Dispose(); }
        }

        /// <summary>The Fed's state as the window compares it: the chair, its term, the Vice Chair's flag, the pending nominee and its vote day, the Senate's votes.</summary>
        private static string FedState(Country c)
        {
            if (c == null) { return "NO USA"; }
            var sb = new StringBuilder();
            sb.Append(c.CurrentFedChair?.Name ?? "no chair").Append(' ').Append(c.CurrentFedChair?.TermBegan.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-")
              .Append(" acting ").Append(c.FedViceChairActing ? "yes" : "no")
              .Append(" pending ").Append(c.FedNominationPending == null ? "none" : (c.FedNominationPending.Nominee?.Name ?? "?") + " " + c.FedNominationPending.VoteOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
              .Append(" votes");
            if (c.FedSenateVotes != null) { foreach (FedSenateVote v in c.FedSenateVotes) { sb.Append(' ').Append(v.Nominee).Append(' ').Append(v.For).Append('-').Append(v.Against).Append(v.Confirmed ? " confirmed" : " refused"); } }
            return sb.ToString();
        }


        private sealed class Game : IDisposable
        {
            public GameObject Host;
            public World World;
            public PolicyImpactLedger Ledger;
            public ShadowBaseline None;
            public void Dispose()
            {
                Ledger?.Dispose();
                None?.Dispose();
                if (Host != null) { UnityEngine.Object.DestroyImmediate(Host); }
                EnergyMarket.ResetTurnState();
            }
        }

        /// <summary>A game on the game's order, pulling every lever of the script but <paramref name="skipFamily"/>'s.</summary>
        private static Game Play(string skipFamily, Dictionary<PlayerLeverKind, string> leverFamilies, bool withLedger, List<string> failures)
        {
            SimulationRandom.Seed(Seed);
            var game = new Game { Host = new GameObject("AF-11 " + (skipFamily ?? (withLedger ? "watched" : "alone"))) };
            SimulationManager sim = game.Host.AddComponent<SimulationManager>();
            game.World = WorldFactory.CreateDefault();
            sim.SetWorld(game.World);
            sim.PlayerCountryId = Player;   // no party seated: the instrument's hand on the country, as every Editor tool that names a player country
            sim.IsCounterfactual = skipFamily != null;   // AF-7: the seed's world without a family stands for the ledger's except-world, a counterfactual - a head's term ends there in the AI's keyed pick, as in the fork
            if (withLedger)
            {
                game.None = new ShadowBaseline(SimulationRandom.MasterSeed);
                game.Ledger = new PolicyImpactLedger(game.None);
                sim.ImpactLedger = game.Ledger;
            }

            float? taxTarget = null;
            for (int t = 0; t < Turns; t++)
            {
                PullDue(sim, game.World, t, 0, skipFamily, leverFamilies, failures);
                for (int d = 1; d <= SimulationManager.DaysPerTurn; d++)
                {
                    if (skipFamily != null) { sim.AnswerWaitsAsTheAi(Player); }   // the K15-K17 review's H-1: the start of the day, as the ledger's except-world answers what no lever answered (ShadowBaseline.AnswersWaitsAsTheAi)
                    sim.AdvanceDay();
                    sim.AdvanceCountryDayTick(Player);   // FORK-1 (J8): the game's day - the ledger's forks run it too
                    if (d < SimulationManager.DaysPerTurn) { sim.HoldPlayerElection(); }   // FORK-2: the controller's election call on the player's polling day - the ledger's forks hold it too
                    PullDue(sim, game.World, t, d, skipFamily, leverFamilies, failures);
                }
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in game.World.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                if (t >= TaxesFromTurn && skipFamily != "Taxes")
                {
                    // the dial: the income tax three points over its rate at the first such boundary, held there - read once, before any world could differ on it
                    if (!taxTarget.HasValue)
                    {
                        foreach (TaxLine line in game.World.GetCountry(Player).TaxLines) { if (line.Type == TaxType.IncomeTax && line.IsImplemented) { taxTarget = line.Rate + 3f; } }
                        if (!taxTarget.HasValue) { Fail(failures, "the player's income tax is not implemented - the Taxes dial was not exercised"); taxTarget = float.NaN; }
                    }
                    if (!float.IsNaN(taxTarget.Value)) { decisions[Player] = new PolicyDecision(); decisions[Player].TaxRateOverrides[TaxType.IncomeTax] = taxTarget.Value; }
                }
                game.Ledger?.AdvanceTurn(sim, game.World, Player, decisions);
                sim.AdvanceTurn(decisions);
                game.None?.AdvanceTurn();
                EnergyMarket.BeginTurn(game.World);   // the game's own call after the shadow's turn: the played world's market state stands between boundaries
                sim.HoldPlayerElection();   // FORK-2: a polling day that is the turn's last, after the boundary, as the controller holds it
            }
            return game;
        }

        private static void PullDue(SimulationManager sim, World world, int turn, int day, string skipFamily, Dictionary<PlayerLeverKind, string> leverFamilies, List<string> failures)
        {
            foreach (var s in Script)
            {
                if (s.Turn != turn || s.Day != day || leverFamilies[s.Kind] == skipFamily) { continue; }
                Country player = world.GetCountry(Player);
                string refused = null;
                bool pulled;
                switch (s.Kind)
                {
                    case PlayerLeverKind.PartnerTariffOverride:
                        pulled = sim.SetPartnerTariffOverride(Player, player.TradePartners[0].PartnerId, out refused);
                        break;
                    case PlayerLeverKind.FleetOrder:
                        int tech = OrderableTechnology(player);
                        pulled = tech >= 0 && sim.PlaceFleetOrder(Player, tech, EnergyFleet.StepMw(Player), player.CalendarYear, out refused);
                        if (tech < 0) { refused = "no technology of the layer is orderable for " + Player; }
                        break;
                    case PlayerLeverKind.CentralBankHead:
                        float bias = (player.CurrentFedChair?.RateBias ?? 0f) + 1f;
                        pulled = sim.AppointCentralBankHead(Player, new FedChair("AF-11 probe head", FedChairPhilosophy.Hawkish, "the diagnostic's probe", bias), out refused);
                        break;
                    case PlayerLeverKind.MinisterAppointment:
                        var minister = new CabinetMinister("AF-11 probe minister", ProbePortfolio, CabinetMinisterPhilosophy.Pragmatic, "the diagnostic's probe", 0f) { Efficiency = 50f, Popularity = 60f };
                        pulled = sim.AppointMinister(Player, ProbePortfolio, minister, out refused);
                        break;
                    case PlayerLeverKind.MinisterReshuffle:
                        pulled = sim.ReshuffleMinister(Player, ProbePortfolio, "AF-11 probe reshuffle", out refused);
                        break;
                    default:
                        pulled = false; refused = "the script names a lever the diagnostic does not pull";
                        break;
                }
                if (!pulled) { Fail(failures, $"{s.Kind} (turn {s.Turn}, day {s.Day}) was refused: {refused ?? "no reason"}"); }
            }
        }

        private static int OrderableTechnology(Country player)
        {
            foreach (int tech in new[] { 4, 5, 1 }) { if (EnergyFleet.CannotPlaceWhy(player, tech, EnergyFleet.StepMw(Player), player.CalendarYear) == null) { return tech; } }   // wind, solar, gas - the labels' order (EnergyFleet.LeadTimeYears); the first a step of which the queue takes
            return -1;
        }

        /// <summary>Every country's state (each float, round-trip), its policy rate, and the player's levers' own state.</summary>
        private static string Fingerprint(World world)
        {
            var sb = new StringBuilder();
            foreach (Country c in world.Countries)
            {
                sb.Append(c.Id).Append(':');
                foreach (FieldInfo f in typeof(EconomyState).GetFields())
                {
                    if (f.FieldType != typeof(float)) { continue; }
                    sb.Append(f.Name).Append('=').Append(((float)f.GetValue(c.State)).ToString("R", CultureInfo.InvariantCulture)).Append('|');
                }
                sb.Append("rate=").Append((c.CurrencyZone?.InterestRate ?? 0f).ToString("R", CultureInfo.InvariantCulture)).Append('\n');
            }
            sb.Append(LeverState(world.GetCountry(Player))).Append('\n');
            return sb.ToString();
        }

        private static string LeverState(Country c)
        {
            if (c == null) { return "NO COUNTRY"; }
            var sb = new StringBuilder();
            sb.Append("head ").Append(c.CurrentFedChair == null ? "none" : c.CurrentFedChair.Name + " " + c.CurrentFedChair.RateBias.ToString("R", CultureInfo.InvariantCulture));
            sb.Append("; overrides ");
            foreach (TradePartner link in c.TradePartners) { if (link.HasPlayerTariffOverride) { sb.Append(link.PartnerId).Append('=').Append(link.PlayerTariffOverride.ToString("R", CultureInfo.InvariantCulture)).Append(' '); } }
            sb.Append("; fleet orders ");
            if (c.FleetOrders != null) { foreach (EnergyFleet.Order o in c.FleetOrders) { if (o.Reason == null) { sb.Append(o.Technology).Append(':').Append(o.Mw.ToString("R", CultureInfo.InvariantCulture)).Append('@').Append(o.OrderedTurn).Append(o.Landed ? "L " : " "); } } }
            sb.Append("; ").Append(ProbePortfolio).Append(' ').Append(c.CabinetMinisters.TryGetValue(ProbePortfolio, out CabinetMinister m) ? m.Name : "vacant");
            return sb.ToString();
        }

        private static string FirstDifference(string a, string b)
        {
            string[] x = a.Split('\n'), y = b.Split('\n');
            for (int i = 0; i < Math.Min(x.Length, y.Length); i++)
            {
                if (x[i] == y[i]) { continue; }
                string[] p = x[i].Split('|'), q = y[i].Split('|');
                for (int j = 0; j < Math.Min(p.Length, q.Length); j++) { if (p[j] != q[j]) { return $" - first at line {i}: {p[j]} against {q[j]}"; } }
                return $" - first at line {i}";
            }
            return " - the lengths differ";
        }

        private static void Fail(List<string> failures, string message) => failures.Add(message);

        private static void Finish(StringBuilder sb, List<string> failures)
        {
            foreach (string f in failures) { sb.Append("    ⚠ FAULT  ").Append(f).Append('\n'); }
            sb.Append(F("\n=== PlayerLeverLedgerDiagnostic: {0} ===\n", failures.Count == 0 ? "ALL ASSERTIONS PASS" : failures.Count + " FAULT(S)"));
            if (failures.Count == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
