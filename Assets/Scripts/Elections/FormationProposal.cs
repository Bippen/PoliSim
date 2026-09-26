using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// §646 (the formateur, POLITICAL_SYSTEM_SPEC.md §5.3's premises 1-3): A FORMATEUR'S PROPOSAL - the cabinet (its parties and the posts each
    /// holds) and the support agreements (its supporters and which of each one's tabled demands the formateur accepts, by the demand's key).
    /// </summary>
    public sealed class FormationProposal
    {
        public string Formateur;
        public List<string> CabinetParties = new List<string>();
        public Dictionary<string, List<CabinetPortfolio>> Posts = new Dictionary<string, List<CabinetPortfolio>>();
        public List<string> Supporters = new List<string>();
        /// <summary>For each supporter, the keys (<see cref="SupportAgreement.KeyOf"/>) of its tabled demands the formateur accepts.</summary>
        public Dictionary<string, List<string>> AcceptedDemands = new Dictionary<string, List<string>>();
        /// <summary>§646 (the reader): each supporter's demands AS TABLED to this proposal, frozen when it is drafted or submitted - the vote and the
        /// government it installs read these, so a demand delivered while the proposal waits on its vote cannot re-order what was accepted.</summary>
        public Dictionary<string, List<AgreementItem>> Tabled = new Dictionary<string, List<AgreementItem>>();

        /// <summary>Freeze the tabled demands of every supporter not yet frozen, as the supporter tables them on <paramref name="date"/>.</summary>
        public void FreezeTabled(Country country, DateTime date, World world)
        {
            foreach (string supporter in Supporters)
            {
                if (Tabled.ContainsKey(supporter)) { continue; }
                Tabled[supporter] = SupportAgreement.Demand(country, supporter, Formateur, date, world).Items;
            }
        }

        /// <summary>A supporter's tabled demands - frozen where the proposal froze them, else as it tables them today.</summary>
        public List<AgreementItem> TabledOf(Country country, string supporter, DateTime date, World world) =>
            Tabled.TryGetValue(supporter, out List<AgreementItem> frozen) ? frozen : SupportAgreement.Demand(country, supporter, Formateur, date, world).Items;

        public int PostsOf(string party) => Posts.TryGetValue(party, out List<CabinetPortfolio> held) ? held.Count : 0;
    }

    /// <summary>§646: one AI party's answer to a proposal - accept or refuse, with its reason in the formation's own terms.</summary>
    public sealed class PartyAnswer
    {
        public string Party;
        public bool InCabinet;
        public bool Accepts;
        public string Reason;
        /// <summary>A partner's: the posts offered, the posts Gamson's law allocates it in this cabinet, its payoff here and the best the formation would give it elsewhere.</summary>
        public int PostsOffered, PostsExpected;
        public double Payoff, Alternative;
    }

    /// <summary>§646: the chamber's answer to a proposal - every invited party's, and the investiture the proposal would face.</summary>
    public sealed class ProposalVerdict
    {
        public readonly List<PartyAnswer> Answers = new List<PartyAnswer>();
        public bool AllAccept;
        /// <summary>The investiture on the cabinet with the supporters that accepted - its vote party by party (<see cref="CoalitionFormation.Evaluate"/>). Null where the proposal is not well formed.</summary>
        public CoalitionFormation.CabinetEvaluation Investiture;
        public IReadOnlyList<PoliticalParty> Parties;
        /// <summary>Every invited party accepts and the investiture wins under the country's rule.</summary>
        public bool Passes => AllAccept && Investiture != null && Investiture.Wins;
        public string Reason;
    }

    /// <summary>
    /// §646: THE FORMATEUR'S EVALUATOR - how each AI party answers a proposal and what investiture it would face, derived from the formation's own
    /// model, never from an authored willingness (premise 3):
    /// <list type="bullet">
    /// <item>A PARTNER refuses a cabinet a red line falls inside of, and - K-1f - a party whose own leader is its declared candidate sits only in a cabinet it leads, refusing a
    /// partner's seat under any other formateur. Otherwise it weighs its payoff - the formation's (<see cref="CoalitionFormation.Payoff"/>: its seat share of the
    /// cabinet, scaled by the cabinet's cohesion), scaled down by the posts offered against the posts Gamson's law allocates it in this cabinet
    /// (<see cref="GovernmentRecord.GamsonPosts"/>; premise 2: offering less lowers acceptance, and offering more buys nothing above the law) - against
    /// the best payoff a government the formation would settle on gives it (<see cref="CoalitionFormation.Holding"/>), refusing where that is better by more than the formation's own margin
    /// (<see cref="CoalitionFormation.DefectionMargin"/>), as a party walks out of a government in it.</item>
    /// <item>A SUPPORTER refuses on the formation's first conditions of support (<see cref="CoalitionFormation.SupportRefusal"/>); where a government
    /// the formation would form seats it in a cabinet by more than the margin (the same walk-out test - support buys no posts); and where the formateur
    /// refuses more of its tabled demands than the share of broken items it tolerates (<see cref="SupportAgreement.BrokenShareTolerated"/>: a demand
    /// refused at the outset read as a promise broken, no new figure). Among the supporters that pass, two that red-line each other cannot both stay
    /// (<see cref="CoalitionFormation.SharedSupport"/>).</item>
    /// <item>THE INVESTITURE is <see cref="CoalitionFormation.Evaluate"/> on the proposed cabinet with the supporters that stayed - the formation's own
    /// vote under the country's own rule.</item>
    /// </list>
    /// </summary>
    public static class Formateur
    {
        /// <summary>The prepared chamber for a country's sitting seats and a vintage's declarations (with any refusal lines a motion or a decline made).</summary>
        public static CoalitionFormation.Chamber ChamberOf(Country country, ElectionVintage vintage, IReadOnlyList<RedLine> extraLines, out IReadOnlyList<PoliticalParty> parties)
        {
            parties = PartySystems.For(country.Id);
            var seats = new int[parties.Count];
            for (int p = 0; p < parties.Count; p++) { seats[p] = country.ParliamentSeats != null && country.ParliamentSeats.TryGetValue(parties[p].Abbrev, out int held) ? held : 0; }
            List<RedLine> lines = DeclaredRedLines.For(country.Id, parties, vintage);
            if (extraLines != null) { lines.AddRange(extraLines); }
            return CoalitionFormation.Prepare(seats, GovernmentFormation.Compatibility(parties), lines, ChamberRules.UsesNegativeParliamentarism(country.Id), DeclaredRedLines.InOrAgainstFor(country.Id, parties, vintage));
        }

        /// <summary>Every invited party's answer to <paramref name="proposal"/>, and the investiture it would face, on <paramref name="vintage"/>'s declarations.</summary>
        public static ProposalVerdict Answer(Country country, FormationProposal proposal, DateTime date, World world, ElectionVintage vintage, IReadOnlyList<RedLine> extraLines = null, string playersParty = null)
        {
            var verdict = new ProposalVerdict();
            CoalitionFormation.Chamber chamber = ChamberOf(country, vintage, extraLines, out IReadOnlyList<PoliticalParty> parties);
            verdict.Parties = parties;
            int n = parties.Count;
            int Index(string key) { for (int p = 0; p < n; p++) { if (parties[p].Abbrev == key) { return p; } } return -1; }

            // A well-formed proposal: a known formateur in its own cabinet; known parties, none twice; each portfolio held once, by a cabinet party.
            if (Index(proposal.Formateur) < 0 || !proposal.CabinetParties.Contains(proposal.Formateur)) { verdict.Reason = "the formateur's own party is not in the proposed cabinet"; return verdict; }
            var seen = new HashSet<string>();
            foreach (string key in proposal.CabinetParties) { if (Index(key) < 0 || !seen.Add(key)) { verdict.Reason = "the cabinet names an unknown party or one twice: " + key; return verdict; } }
            foreach (string key in proposal.Supporters) { if (Index(key) < 0 || !seen.Add(key)) { verdict.Reason = "the supporters name an unknown party, one twice, or a cabinet party: " + key; return verdict; } }
            var postsSeen = new HashSet<CabinetPortfolio>();
            foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in proposal.Posts)
            {
                if (!proposal.CabinetParties.Contains(kv.Key)) { verdict.Reason = "a post is offered to a party outside the cabinet: " + kv.Key; return verdict; }
                foreach (CabinetPortfolio post in kv.Value) { if (!postsSeen.Add(post)) { verdict.Reason = "a portfolio is offered twice: " + post; return verdict; } }
            }
            int cabinet = 0;
            foreach (string key in proposal.CabinetParties) { cabinet |= 1 << Index(key); }

            // The alternatives: every government the formation itself would form on this chamber - the best a party could have instead.
            CoalitionResult formation = CoalitionFormation.Form(chamber.Seats, chamber.Compatibility, chamber.Lines, chamber.NegativeRule, chamber.Rules);
            List<GovernmentOption> holding = CoalitionFormation.Holding(formation, chamber.Seats, chamber.Compatibility);   // never a government the formation would not settle on
            double BestElsewhere(int p, out string where)
            {
                double best = 0.0; where = "none";
                foreach (GovernmentOption g in holding)
                {
                    double there = CoalitionFormation.Payoff(p, g, chamber.Seats, chamber.Compatibility, n);
                    if (there > best) { best = there; where = Describe(parties, g.Cabinet); }
                }
                return best;
            }
            Dictionary<string, List<CabinetPortfolio>> gamson = GovernmentRecord.GamsonPosts(country, proposal.CabinetParties, proposal.Formateur);
            var candidacies = new HashSet<string>();
            foreach ((string abbrev, string _, string _) in DeclaredRedLines.Candidacies(country.Id, vintage)) { candidacies.Add(abbrev); }
            int cabinetSeats = CoalitionMath.Seats(chamber.Seats, cabinet);
            var option = new GovernmentOption(cabinet, 0, CoalitionOutcomeKind.MinorityGovernment, cabinetSeats, cabinetSeats, 0, 0.0, 0.0);

            bool all = true;
            foreach (string key in proposal.CabinetParties)
            {
                if (key == proposal.Formateur) { continue; }
                int p = Index(key);
                var answer = new PartyAnswer { Party = key, InCabinet = true, PostsOffered = proposal.PostsOf(key), PostsExpected = gamson.TryGetValue(key, out List<CabinetPortfolio> e) ? e.Count : 0 };
                RedLine? inside = LineInside(chamber, cabinet, p);
                if (key == playersParty)
                {
                    answer.Accepts = true;   // §646 (the reader): the player's own party answers by the player, never by the model
                    answer.Reason = "accepts - the player's party accepted the offer";
                }
                else if (inside.HasValue)
                {
                    answer.Reason = "refuses: a red line falls inside this cabinet - " + inside.Value.Basis;
                }
                else if (candidacies.Contains(key))
                {
                    answer.Reason = "refuses: its own leader is its declared candidate, and it sits only in a cabinet its candidate leads (K-1f)";
                }
                else
                {
                    double here = CoalitionFormation.Payoff(p, option, chamber.Seats, chamber.Compatibility, n);
                    double postsFactor = answer.PostsExpected == 0 ? 1.0 : Math.Min(1.0, (double)answer.PostsOffered / answer.PostsExpected);
                    answer.Payoff = here * postsFactor;
                    answer.Alternative = BestElsewhere(p, out string where);
                    answer.Accepts = answer.Alternative <= answer.Payoff + CoalitionFormation.DefectionMargin;
                    answer.Reason = string.Format(CultureInfo.InvariantCulture, "{0} - offered {1} post(s), Gamson's law allocates it {2} here; worth {3:0.000} to it against {4:0.000} in {5}",
                        answer.Accepts ? "accepts" : "refuses: the formation would give it more elsewhere", answer.PostsOffered, answer.PostsExpected, answer.Payoff, answer.Alternative, where);
                }
                all &= answer.Accepts;
                verdict.Answers.Add(answer);
            }

            int survivors = 0;
            var supporterAnswers = new List<(int Index, PartyAnswer Answer)>();
            foreach (string key in proposal.Supporters)
            {
                int p = Index(key);
                var answer = new PartyAnswer { Party = key, InCabinet = false };
                string refusal = key == playersParty ? null : CoalitionFormation.SupportRefusal(chamber, p, cabinet);
                if (refusal == null && key != playersParty)
                {
                    double elsewhere = BestElsewhere(p, out string where);
                    if (elsewhere > CoalitionFormation.DefectionMargin) { refusal = string.Format(CultureInfo.InvariantCulture, "the formation would seat it in a cabinet instead - {0} (worth {1:0.000} to it; support buys no posts)", where, elsewhere); }
                }
                if (refusal == null && key != playersParty)
                {
                    List<AgreementItem> tabledItems = proposal.TabledOf(country, key, date, world);
                    List<string> accepted = proposal.AcceptedDemands.TryGetValue(key, out List<string> a) ? a : new List<string>();
                    int yes = 0;
                    foreach (AgreementItem item in tabledItems) { if (accepted.Contains(SupportAgreement.KeyOf(item))) { yes++; } }
                    int refused = tabledItems.Count - yes;
                    if (tabledItems.Count > 0 && refused > SupportAgreement.BrokenShareTolerated * tabledItems.Count)
                    {
                        refusal = string.Format(CultureInfo.InvariantCulture, "the formateur accepts {0} of its {1} demands - more refused than it tolerates", yes, tabledItems.Count);
                    }
                }
                answer.Accepts = refusal == null;
                answer.Reason = refusal ?? (key == playersParty ? "accepts - the player's party accepted the offer" : "accepts - it supports the cabinet from outside on its agreement");
                if (answer.Accepts) { survivors |= 1 << p; }
                supporterAnswers.Add((p, answer));
                verdict.Answers.Add(answer);
            }
            int support = CoalitionFormation.SharedSupport(chamber, survivors);
            foreach ((int p, PartyAnswer answer) in supporterAnswers)
            {
                if (answer.Accepts && (support & (1 << p)) == 0) { answer.Accepts = false; answer.Reason = "refuses: it will not share the support with a party it red-lines, which carries more weight"; }
                all &= answer.Accepts;
            }

            verdict.AllAccept = all;
            verdict.Investiture = CoalitionFormation.Evaluate(chamber, cabinet, support);
            verdict.Reason = !all ? "an invited party refuses" : verdict.Investiture.Wins ? "every invited party accepts and the investiture passes" : "every invited party accepts, and the investiture fails";
            return verdict;
        }

        private static RedLine? LineInside(CoalitionFormation.Chamber chamber, int cabinet, int p)
        {
            foreach (RedLine line in chamber.Lines)
            {
                if (line.A >= chamber.N || line.B >= chamber.N) { continue; }
                if ((cabinet & (1 << line.A)) == 0 || (cabinet & (1 << line.B)) == 0) { continue; }
                if (line.A == p || (!line.OneWay && line.B == p)) { return line; }   // a one-way line is its first party's refusal, not the refused party's
            }
            return null;
        }

        private static string Describe(IReadOnlyList<PoliticalParty> parties, int mask)
        {
            var keys = new List<string>();
            for (int p = 0; p < parties.Count; p++) { if ((mask & (1 << p)) != 0) { keys.Add(parties[p].Abbrev); } }
            return string.Join("+", keys);
        }
    }
}
