using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>The player's party's role toward the sitting government - the political-system spec's §5.2 four roles.</summary>
    public enum PlayerRole
    {
        /// <summary>No party seated, or no government stored.</summary>
        None,
        /// <summary>The prime minister's party: the player governs.</summary>
        PrimeMinister,
        /// <summary>A junior coalition partner - in the cabinet, not leading it.</summary>
        JuniorPartner,
        /// <summary>A support party: carrying the cabinet from outside (confidence and supply).</summary>
        Support,
        Opposition,
    }

    /// <summary>
    /// PS-3a (2026-09-25, §628; the political-system spec's §5.2-§5.3, §9 stage 3): WHO GOVERNS, STORED. Until this record the game never kept
    /// who governs - it re-formed the government from the seats on every call, and "AI-governed" was a test on the COUNTRY (is it the
    /// player's?), never on the player's role. This is one record per country, written when the world seats the government of record at
    /// its start and again when the formation forms one after the game's own election, saved with the country, and read by the one test
    /// that decides whose levers move the book: <c>SimulationManager.PlayerGoverns</c>.
    ///
    /// <para><b>The prime minister's party is a premise, stated.</b> The model carries no prime minister. Where the record names the head of
    /// government, its party is read from the record (Kristersson (M)); where the formation formed the cabinet, the party of a declared
    /// own-leader candidacy standing in the cabinet leads it (K-1f's premise), else the largest cabinet party.</para>
    /// </summary>
    public sealed class GovernmentRecord
    {
        public List<string> Cabinet = new List<string>();
        public List<string> Support = new List<string>();
        /// <summary>The prime minister's party by key - the premise above.</summary>
        public string PmParty;
        public string Outcome;
        public DateTime FormedOn;
        /// <summary>True while the record is the formation's stand-in for a government the record does not yet hold (§605).</summary>
        public bool Provisional;
        public string Basis;
        /// <summary>PS-3b (§629): a cabinet answerable to the chamber, or a presidency - the USA's, where the president's party is the government and PmParty is that party.</summary>
        public WorldClock.ExecutiveKind Kind;
        /// <summary>PS-3b (§629): the president of record where one is elected apart from the chamber (the USA's, France's), by the record's own line; null for the parliamentary four.</summary>
        public string Executive;
        /// <summary>PS-3f (§633, ruled): the breaks recorded against this government - a support party voting its own alternative budget over the government's frames, dated; their consequence arrives with the support agreements (PS-3 part 5).</summary>
        public List<string> Breaks = new List<string>();
        /// <summary>
        /// PS-3g (§634): THE PORTFOLIOS BY PARTY - Gamson's law, sourced (`docs/reference/GAMSON_PORTFOLIOS.md`): a cabinet party's share of the
        /// portfolios is its share of the coalition's seats ("a share of the payoff proportional to the amount of resources which they contribute
        /// to a coalition" [BF73]; "one-to-one proportion" [WD01]; "near-perfect relationship" [WD06]), with no formateur premium [WD06]. §706
        /// (ruled 2026-10-01): each post WEIGHED by Druckman &amp; Warwick's published salience (<see cref="PortfolioSalience"/>), the prime
        /// minister's party credited the head of government's weight, the posts heaviest first to the party with the most entitlement outstanding
        /// (<see cref="AllocatePortfolios"/>) - Finance can go to a partner (the record: 2021's FDP, 2025's SPD). STATED, UNSIZED: the literature's deviation - the large party
        /// underpaid, the small overpaid [BF73] [WD01] - is on no abstract as a figure, so the model pays pure proportion and says so; Sweden's real
        /// cabinet (M 13, KD 6, L 5 of 24 for seat shares 0.66/0.18/0.16, `sweden/portfolios.md`) shows the direction. Which portfolio a party
        /// takes follows its manifesto's emphasis in the literature [BDD11] - unsourced per party here, so the enum's order stands as the premise.
        /// </summary>
        public Dictionary<string, List<CabinetPortfolio>> Portfolios = new Dictionary<string, List<CabinetPortfolio>>();
        /// <summary>PS-3h (§635): the support agreements - one per support party, its demands chosen from its own positions (<see cref="SupportAgreement.Demand"/>), each item owed, delivered or broken.</summary>
        public List<SupportAgreement> Agreements = new List<SupportAgreement>();
        /// <summary>PS-3i (§636): the day the chamber declared no confidence in this government's prime minister (MinValue for none), and the party that moved it.</summary>
        public DateTime NoConfidenceOn = DateTime.MinValue;
        public string NoConfidenceMover;
        /// <summary>PS-3i (§636): discharged, serving on as a caretaker until the next government (RF 6 kap. 9 §) - no motion is taken up against it, and it cannot itself decide an extra election (RF 3 kap. 11 §); a Speaker's round that breaks off can order one (6 kap. 5 §, §640, §646).</summary>
        public bool Caretaker;
        public DateTime CaretakerSince = DateTime.MinValue;
        /// <summary>PS-3i ruling (3) (2026-09-25), §646: the polling day of the last election after which a Speaker's round opened on this government's watch
        /// (RF 6 kap. 5 §) - so each election opens one round. MinValue: none yet.</summary>
        public DateTime ProcedureResumedAfter = DateTime.MinValue;
        /// <summary>§641 (the reader): the refusals a motion made that stand in every Speaker's round until the next election, as "MOVER>PM" by party key -
        /// the player's party will not carry the prime minister it brought down. Carried from a fallen government to the one the round forms; an election's
        /// formation starts without them. Empty in an older save, which is what it held.</summary>
        public List<string> StandingRefusals = new List<string>();
        /// <summary>§646: the Speaker's round this government serves through as a caretaker (premise 8), or null. Saved (format 35).</summary>
        public SpeakerRound Round;

        /// <summary>
        /// §642 (the ultrareview of PR #1): WHO GOVERNS HAS A VERSION. A government formed, installed or loaded is a new record; every change made to a
        /// record in place - a supporter's withdrawal, a partner leaving, the Speaker's discharge - goes through the methods below, and each bumps
        /// the version. A reader that caches on who governs (`ChamberVerdicts`) keys on the record and its version, so no change can pass under it. Not
        /// saved: a loaded record is a new record.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore] public int Version { get; private set; }

        /// <summary>A support party withdraws: struck from the support. False when it was not supporting.</summary>
        public bool WithdrawSupport(string party)
        {
            if (!Support.Remove(party)) { return false; }
            Version++;
            return true;
        }

        /// <summary>A junior partner leaves the cabinet; the portfolios are re-apportioned among those who stay. False when it was not in the cabinet.</summary>
        public bool LeaveCabinet(string party, Country country)
        {
            if (!Cabinet.Remove(party)) { return false; }
            Version++;   // before the portfolios, so a throw there cannot leave the cabinet changed and the version not (the second reading)
            AllocatePortfolios(country);
            return true;
        }

        /// <summary>The Speaker discharges the government; it serves on as a caretaker from <paramref name="on"/> (RF 6 kap. 9 §, 11 §) - and, §646, the
        /// outgoing government through a Speaker's round after an election (premise 8).</summary>
        public void Discharge(DateTime on)
        {
            Caretaker = true;
            CaretakerSince = on;
            Version++;
        }

        /// <summary>The agreement a support party holds, or null.</summary>
        public SupportAgreement AgreementOf(string party) { foreach (SupportAgreement a in Agreements) { if (a.Supporter == party) { return a; } } return null; }

        /// <summary>Forms one agreement per support party from its own positions (the spec's §5.3).</summary>
        public void FormAgreements(Country country, DateTime formedOn, World world = null)
        {
            Agreements.Clear();
            foreach (string supporter in Support) { Agreements.Add(SupportAgreement.Demand(country, supporter, PmParty, formedOn, world)); }
        }

        public bool HoldsPortfolio(string party, CabinetPortfolio portfolio) => !string.IsNullOrEmpty(party) && Portfolios.TryGetValue(party, out List<CabinetPortfolio> held) && held.Contains(portfolio);

        /// <summary>The portfolios a party holds, as the desk names them (FINANCE, INTERIOR …), or "NONE".</summary>
        public string PortfoliosOf(string party)
        {
            if (string.IsNullOrEmpty(party) || !Portfolios.TryGetValue(party, out List<CabinetPortfolio> held) || held.Count == 0) { return "NONE"; }
            var names = new List<string>(held.Count);
            foreach (CabinetPortfolio p in held) { names.Add(Effectiveness.ShortName(p).ToUpperInvariant()); }
            return string.Join(", ", names);
        }

        /// <summary>
        /// §706 (Elias's ruling of 2026-10-01: the Treasury lock lifted, Finance weighed): allocates the six portfolios by Gamson's law WITH
        /// SALIENCE - each post at Druckman &amp; Warwick's published weight (<see cref="PortfolioSalience"/>), the head of government's weight
        /// credited to its party. [AUTHORED-DRAFT] the method: a party's entitlement is its share of the cabinet's seats times the total weight;
        /// the posts, heaviest first, each go to the party with the most entitlement outstanding (a near-tie to the larger party, §711); the head's party holds
        /// at least one post (the rule before, kept - its levers pass the gates anyway, the post is its minister's). Finance goes where the weights put it: to the partner in
        /// the 2025 and 2021 chambers, as the record has it (PortfolioSalienceDiagnostic), and it carries its levers to whoever holds it (§634).
        /// </summary>
        /// <summary>§711 [AUTHORED-DRAFT], the play-calibration list's 25th entry: an entitlement gap smaller than this share of a post's own weight is a
        /// near-tie, and the post goes to the larger party (Elias's ruling of 2026-10-01, item 2). A tenth of the post: the 2026 Riksdag's Finance
        /// gap (0.056 of 1.68, a thirtieth) is one; the 2021 and 2025 Bundestags' Finance gaps (0.32 and 1.30 of 1.58) are not.</summary>
        public const double NearTieShare = 0.10;

        public void AllocatePortfolios(Country country)
        {
            Portfolios.Clear();
            if (Cabinet.Count == 0) { return; }
            var all = (CabinetPortfolio[])Enum.GetValues(typeof(CabinetPortfolio));
            int total = 0;
            var seats = new Dictionary<string, int>();
            foreach (string party in Cabinet) { int held = country.ParliamentSeats != null && country.ParliamentSeats.TryGetValue(party, out int n) ? n : 0; seats[party] = held; total += held; }
            double head = PortfolioSalience.HeadWeight(country.Id);
            double weightTotal = head;
            foreach (CabinetPortfolio p in all) { weightTotal += PortfolioSalience.Weight(country.Id, p); }
            var outstanding = new Dictionary<string, double>();
            foreach (string party in Cabinet)
            {
                double share = total > 0 ? seats[party] / (double)total : 1.0 / Cabinet.Count;
                outstanding[party] = share * weightTotal - (party == PmParty ? head : 0.0);
            }
            var order = new List<string>(Cabinet);
            order.Sort((a, b) => seats[b].CompareTo(seats[a]));
            foreach (string party in order) { Portfolios[party] = new List<CabinetPortfolio>(); }
            var posts = new List<CabinetPortfolio>(all);
            posts.Sort((a, b) => PortfolioSalience.Weight(country.Id, b).CompareTo(PortfolioSalience.Weight(country.Id, a)) is int c && c != 0 ? c : ((int)a).CompareTo((int)b));   // heaviest first, a tie in the enum's order
            foreach (CabinetPortfolio post in posts)
            {
                // §711 (Elias's ruling of 2026-10-01, item 2: "Near-ties in portfolio allocation go to the larger party"): the post goes to the larger
                // party of any whose outstanding entitlement is within a near-tie of the most outstanding - [AUTHORED-DRAFT] a near-tie is less than
                // NearTieShare of the post's own weight (the play-calibration list's 25th entry). Order is by seats, so the first such party is the larger.
                double most = double.MinValue;
                foreach (string party in order) { most = Math.Max(most, outstanding[party]); }
                double nearTie = NearTieShare * PortfolioSalience.Weight(country.Id, post);
                string taker = null;
                foreach (string party in order) { if (outstanding[party] >= most - nearTie - 1e-9) { taker = party; break; } }
                Portfolios[taker].Add(post);
                outstanding[taker] -= PortfolioSalience.Weight(country.Id, post);
            }
            if (PmParty != null && Portfolios.TryGetValue(PmParty, out List<CabinetPortfolio> pmHeld) && pmHeld.Count == 0)
            {
                // The head of government's party holds a portfolio whatever its share: the lightest post of the party holding the most weight moves to it.
                string richest = null; double richestWeight = -1.0;
                foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in Portfolios)
                {
                    double w = PortfolioSalience.Of(country.Id, kv.Value);
                    if (kv.Value.Count > 1 && w > richestWeight) { richest = kv.Key; richestWeight = w; }
                }
                if (richest != null)
                {
                    CabinetPortfolio lightest = Portfolios[richest][0];
                    foreach (CabinetPortfolio p in Portfolios[richest]) { if (PortfolioSalience.Weight(country.Id, p) < PortfolioSalience.Weight(country.Id, lightest)) { lightest = p; } }
                    Portfolios[richest].Remove(lightest);
                    pmHeld.Add(lightest);
                }
            }
        }

        /// <summary>
        /// §646 (R7): the government a Speaker's round installs - the proposal's cabinet, its posts as offered, the supporters that accepted and, for each,
        /// its tabled demands the formateur accepted; the prime minister the formateur's party; the round's standing refusals carried (§641).
        /// </summary>
        public static GovernmentRecord FromProposal(Country country, FormationProposal proposal, IEnumerable<string> supporters, CoalitionOutcomeKind kind,
            DateTime formedOn, string basis, WorldClock.ExecutiveKind executiveKind, string executive, IEnumerable<string> refusals, World world)
        {
            var record = new GovernmentRecord { FormedOn = formedOn, Provisional = false, Kind = executiveKind, Executive = executive, PmParty = proposal.Formateur, Outcome = kind.ToString(), Basis = basis };
            record.Cabinet.AddRange(proposal.CabinetParties);
            record.Support.AddRange(supporters);
            foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in proposal.Posts) { record.Portfolios[kv.Key] = new List<CabinetPortfolio>(kv.Value); }
            foreach (string supporter in record.Support)
            {
                // The demands as tabled to the proposal (frozen when it was drafted or submitted), the accepted ones kept - copied, so the record's
                // tracking never writes into the proposal.
                List<string> accepted = proposal.AcceptedDemands.TryGetValue(supporter, out List<string> a) ? a : new List<string>();
                var tabled = new SupportAgreement { Supporter = supporter, FormedOn = formedOn, Basis = "the demands it tabled to the formateur's proposal, those the formateur accepted (the formation sheet, §5.3)" };
                foreach (AgreementItem item in proposal.TabledOf(country, supporter, formedOn, world)) { if (accepted.Contains(SupportAgreement.KeyOf(item))) { tabled.Items.Add(item); } }
                record.Agreements.Add(tabled.Copy());
            }
            if (refusals != null) { record.StandingRefusals.AddRange(refusals); }
            return record;
        }

        /// <summary>§646: the posts Gamson's law allocates each party of a proposed cabinet (<see cref="AllocatePortfolios"/>), each post weighed by its salience (§706) - what a partner expects.</summary>
        public static Dictionary<string, List<CabinetPortfolio>> GamsonPosts(Country country, IEnumerable<string> cabinet, string pmParty)
        {
            var scratch = new GovernmentRecord { PmParty = pmParty };
            scratch.Cabinet.AddRange(cabinet);
            scratch.AllocatePortfolios(country);
            return scratch.Portfolios;
        }

        public PlayerRole RoleOf(string abbrev)
        {
            if (string.IsNullOrEmpty(abbrev)) { return PlayerRole.None; }
            if (abbrev == PmParty) { return PlayerRole.PrimeMinister; }
            if (Cabinet.Contains(abbrev)) { return PlayerRole.JuniorPartner; }
            if (Support.Contains(abbrev)) { return PlayerRole.Support; }
            return PlayerRole.Opposition;
        }

        /// <summary>
        /// The government the world seats at a country's start: the record's where its cabinet is sourced (the USA's is its president and their party, France's
        /// its cabinet under its president), else the formation's on the seated chamber, provisional. PS-3b (§629): a date with NO government of record THROWS -
        /// a stand-in formed on a chamber whose record names no government would seat a cabinet the record never held, and a null would let the player's role
        /// default silently; the world does not open there.
        /// </summary>
        public static GovernmentRecord AtStart(Country country, DateTime start, World world = null)
        {
            if (!WorldClock.TryGovernmentAt(country.Id, start, out WorldClock.GovernmentOfRecord ofRecord))
            {
                throw new InvalidOperationException($"{country.Id} has no government of record on {start:yyyy-MM-dd} (WorldClock.Governments): who governs is unknown, and the player's role is never defaulted (PS-3b, §629)");
            }
            if (SeatedGovernment.TryAt(country.Id, start, out SeatedGovernment.Record record) && record.Standing == SeatedGovernment.Standing.Installed && record.Cabinet != null)
            {
                var installed = new GovernmentRecord { FormedOn = record.AsOf, Provisional = false, Basis = record.Basis, Outcome = "of record", Kind = ofRecord.Kind, Executive = ofRecord.President };
                installed.Cabinet.AddRange(record.Cabinet);
                if (record.Support != null) { installed.Support.AddRange(record.Support); }
                installed.PmParty = HeadParty(country.Id, start) ?? Largest(country, installed.Cabinet);
                installed.AllocatePortfolios(country);
                installed.FormAgreements(country, start, world);
                return installed;
            }
            GovernmentFormation.View formed = GovernmentFormation.ViewOf(country);
            GovernmentRecord standIn = FromView(country, formed, start, provisional: true, basis: "the formation's result on the seated chamber - the government of record names no cabinet on this date (§605)", world: world);
            standIn.Kind = ofRecord.Kind; standIn.Executive = ofRecord.President;
            standIn.AllocatePortfolios(country);
            return standIn;
        }

        /// <summary>
        /// PS-3d (§631, ruled): FRANCE'S GOVERNING MODE SEATS THE PLAYER'S PARTY AS THE PRIME MINISTER'S, governing on the 2024 Assembly of record - a what-if
        /// the card states plainly. Player path only: as an AI country France keeps its provisional stand-in (<see cref="AtStart"/>) until the cabinets' parties
        /// are sourced (france records G4). The president of record stays above the what-if cabinet.
        /// </summary>
        public static GovernmentRecord WhatIfGoverning(Country country, string party, DateTime start, World world = null)
        {
            GovernmentRecord ofRecord = AtStart(country, start, world);
            var whatIf = new GovernmentRecord { FormedOn = start, Provisional = false, Outcome = "what-if", Kind = ofRecord.Kind, Executive = ofRecord.Executive, PmParty = party,
                Basis = $"WHAT-IF (ruled, §631): the player's {party} governs on the chamber of record; the real cabinet on this date is {ofRecord.Basis}" };
            whatIf.Cabinet.Add(party);
            whatIf.AllocatePortfolios(country);
            return whatIf;
        }

        /// <summary>The government the formation formed after the game's own election (or none: a record with an empty cabinet and the reason).</summary>
        public static GovernmentRecord FromView(Country country, GovernmentFormation.View view, DateTime formedOn, bool provisional = false, string basis = null, World world = null)
        {
            var record = new GovernmentRecord { FormedOn = formedOn, Provisional = provisional, Basis = basis ?? "the formation on the chamber the game elected" };
            if (view == null || !view.HasGovernment) { record.Outcome = "none"; record.Basis = view?.Reason ?? record.Basis; return record; }
            record.Outcome = view.Outcome.ToString();
            foreach ((string abbrev, int _) in view.Cabinet) { record.Cabinet.Add(abbrev); }
            foreach ((string abbrev, int _) in view.Support) { record.Support.Add(abbrev); }
            // K-1f's premise: a declared own-leader candidacy standing in the cabinet leads it; else the largest cabinet party. §705: of two or more
            // (a German cabinet may hold more than one candidacy), the largest party's.
            var led = new List<string>();
            foreach ((string abbrev, string _, string _) in DeclaredRedLines.CandidaciesAt(country.Id, formedOn)) { if (record.Cabinet.Contains(abbrev)) { led.Add(abbrev); } }
            record.PmParty = led.Count > 0 ? Largest(country, led, byGroup: true) : Largest(country, record.Cabinet);   // §705: a candidacy stands on its Fraktion's seats
            record.AllocatePortfolios(country);
            record.FormAgreements(country, formedOn, world);
            return record;
        }

        /// <summary>The head of government's party from the record's own line, e.g. "Ulf Kristersson (M)" - null where the record names none.</summary>
        private static string HeadParty(CountryId id, DateTime date)
        {
            if (!WorldClock.TryGovernmentAt(id, date, out WorldClock.GovernmentOfRecord government) || string.IsNullOrEmpty(government.Head)) { return null; }
            int open = government.Head.IndexOf('(');
            int close = open >= 0 ? government.Head.IndexOf(')', open) : -1;
            if (open < 0 || close < 0) { return null; }
            string key = government.Head.Substring(open + 1, close - open - 1).Trim();
            foreach (PoliticalParty party in PartySystems.For(id)) { if (party.Abbrev == key) { return key; } }
            return null;
        }

        /// <summary>One log line for a country's record at a date, or the absence of one.</summary>
        public static string Describe(CountryId id, DateTime date, Country country)
        {
            GovernmentRecord g = country?.Government;
            if (g == null) { return $"ROLE: {id} at {date:yyyy-MM-dd} - NO GOVERNMENT STORED; the player's role is unknown (PS-3b, §629: a defect - every path that opens a world stores one)"; }
            string executive = g.Kind == WorldClock.ExecutiveKind.Presidency ? $"the president {g.Executive}, the administration's party {g.PmParty ?? "-"}"
                : $"the government {(g.Cabinet.Count > 0 ? string.Join("+", g.Cabinet) : "none")} led by {g.PmParty ?? "-"}{(g.Support.Count > 0 ? " with " + string.Join("+", g.Support) : string.Empty)}{(g.Executive != null ? " under the president " + g.Executive : string.Empty)}";
            return $"ROLE: {id} at {date:yyyy-MM-dd} - {executive}{(g.Provisional ? " (provisional)" : string.Empty)}; the player's {country.PlayerPartyAbbrev ?? "(no party)"} is {g.RoleOf(country.PlayerPartyAbbrev)}";
        }

        private static string Largest(Country country, List<string> cabinet, bool byGroup = false)
        {
            string best = null; int bestSeats = -1;
            foreach (string abbrev in cabinet)
            {
                int seats = country.ParliamentSeats.TryGetValue(abbrev, out int n) ? n : 0;
                if (byGroup)
                {
                    foreach ((string a, string b) in ChamberRules.JointGroups(country.Id))
                    {
                        string partner = a == abbrev ? b : b == abbrev ? a : null;
                        if (partner != null && seats > 0 && country.ParliamentSeats.TryGetValue(partner, out int m)) { seats += m; }
                    }
                }
                if (seats > bestSeats) { best = abbrev; bestSeats = seats; }
            }
            return best;
        }
    }
}
