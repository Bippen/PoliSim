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
        /// <summary>The head of government's party - the prime minister's, the chancellor's, or in a presidency the president's (PS-6 US-7): the player governs.</summary>
        PrimeMinister,
        /// <summary>A junior coalition partner - in the cabinet, not leading it.</summary>
        JuniorPartner,
        /// <summary>A support party: carrying the cabinet from outside (confidence and supply).</summary>
        Support,
        Opposition,
        /// <summary>PS-6 US-7: the party whose candidate the game elected president, the oath still ahead. Toward the sitting government it is in opposition
        /// until it takes office, so <see cref="GovernmentRecord.RoleOf"/> - every test of who governs - never returns it; <see cref="GovernmentRecord.RoleOn"/>
        /// does, for the words the desk's chip and the role gate say.</summary>
        PresidentElect,
        /// <summary>PS-6 US-19 (R-US7 (a)): a party out of the White House that holds the House (and perhaps the Senate too) - it introduces ordinary and revenue
        /// bills there. Toward the sitting government it is in opposition, so <see cref="GovernmentRecord.RoleOf"/> never returns it; <see cref="GovernmentRecord.RoleOn"/>
        /// does, as it does the president-elect.</summary>
        HouseMajority,
        /// <summary>PS-6 US-19 (R-US7 (a)): a party out of the White House that holds the Senate and not the House - it introduces ordinary bills there, and no
        /// revenue bill (Art. I §7 cl. 1). <see cref="GovernmentRecord.RoleOn"/> alone returns it.</summary>
        SenateMajority,
        /// <summary>PS-6 US-19 (R-US7 (a)): a party holding neither the presidency nor a chamber - it votes and introduces nothing (the plan's risk 18, stated).
        /// <see cref="GovernmentRecord.RoleOn"/> alone returns it.</summary>
        Minority,
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
        /// <summary>GA-3-4 (2026-10-14): a government of record whose label <see cref="Relabel"/> replaced with the outcome the seats give - set as the
        /// "of record" label goes, because the economic vote measures such a government's term from the record's row (`EconomicVote.TookOffice`). False
        /// on every record never relabelled, a government of record among them (its label still says so - an older save's too): the provenance is
        /// this flag OR that label, and `TookOffice` reads the two together.</summary>
        public bool OfRecord;
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
        /// (<see cref="AllocatePortfolios"/>) - Finance can go to a partner (the record: 2021's FDP, 2025's SPD; where the method puts it is
        /// PortfolioSalienceDiagnostic's to print). AF-6 (q6, ruled 2026-10-18): the literature's deviation - the large party underpaid, the small
        /// overpaid [BF73] [WD01] - is paid as READ: the rounding's residual goes to the partners, not the head's party, each post to the partner left least above its due in proportion; no abstract sizes it,
        /// so nothing beyond the residual is paid; Sweden's real cabinet (M 13, KD 6, L 5 of 24 for seat shares 0.66/0.18/0.16, `sweden/portfolios.md`)
        /// shows the direction. Which portfolio a party
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
        /// <summary>COAL-2 (the second review's N2): the day <see cref="Reappoint"/> last appointed this record again (MinValue: never) - a new appointment of the
        /// same record, which the coalition verbs' government identity reads (`CoalitionVerbs.GovernmentKey`). Additive in the save: an older one carries none.</summary>
        public DateTime ReappointedOn = DateTime.MinValue;
        /// <summary>PS-3i ruling (3) (2026-09-25), §646: the polling day of the last election after which a Speaker's round opened on this government's watch
        /// (RF 6 kap. 5 §) - so each election opens one round. MinValue: none yet.</summary>
        public DateTime ProcedureResumedAfter = DateTime.MinValue;
        /// <summary>§641 (the reader): the refusals a motion made that stand in every Speaker's round until the next election, as "MOVER>PM" by party key -
        /// the player's party will not carry the prime minister it brought down. Carried from a fallen government to the one the round forms; an election's
        /// formation starts without them. Empty in an older save, which is what it held.</summary>
        public List<string> StandingRefusals = new List<string>();
        /// <summary>§646: the Speaker's round this government serves through as a caretaker (premise 8), or null. Saved (format 35).</summary>
        public SpeakerRound Round;
        /// <summary>AF-8 (ruling q8): the Sejm's formation after an election (Konstytucja Art. 162, 154) this government serves through, or null - moved to the
        /// cabinet each appointment installs, and kept on it once concluded. Additive in the save: an older one carries none.</summary>
        public SejmFormation Sejm;

        /// <summary>
        /// §642 (the ultrareview of PR #1): WHO GOVERNS HAS A VERSION. A government formed, installed or loaded is a new record; every change made to a
        /// record in place - a supporter's withdrawal, a partner leaving, the Speaker's discharge - goes through the methods below, and each bumps
        /// the version. A reader that caches on who governs (`ChamberVerdicts`) keys on the record and its version, so no change can pass under it. Not
        /// saved: a loaded record is a new record.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore] public int Version { get; private set; }

        /// <summary>A support party withdraws: struck from the support, and the record relabelled by what it now stands on (GA-3-4). False when it was not supporting.</summary>
        public bool WithdrawSupport(string party, Country country)
        {
            if (!Support.Remove(party)) { return false; }
            Version++;
            Relabel(country);
            return true;
        }

        /// <summary>A junior partner leaves the cabinet; the portfolios are re-apportioned among those who stay, and the record relabelled by what it
        /// now stands on (GA-3-4). False when it was not in the cabinet.</summary>
        public bool LeaveCabinet(string party, Country country)
        {
            if (!Cabinet.Remove(party)) { return false; }
            Version++;   // before the portfolios, so a throw there cannot leave the cabinet changed and the version not (the second reading)
            AllocatePortfolios(country);
            Relabel(country);
            return true;
        }

        /// <summary>
        /// GA-3-4 (2026-10-14): the outcome the record stands on, read off the chamber's seats after a supporter withdraws or a partner leaves - a
        /// cabinet with a majority of its own (`MajorityCoalition`), a cabinet its remaining support carries to one (`ConfidenceAndSupply`), or a
        /// minority (`MinorityGovernment`). The label it replaces (a government "of record" with SD's support gone sat on 103 of 349 seats still so
        /// labelled; the SPD and the Greens on 324 of 736 after the FDP left) said a majority the seats no longer gave. What follows the exit is
        /// GA-0-16's question; this only names what stands. A government of record keeps its provenance in <see cref="OfRecord"/>; France's what-if
        /// and a presidency (whose government is not a chamber's majority) keep their labels. Private: only the two exits above call it, each after
        /// bumping the version (§642's contract).
        /// </summary>
        private void Relabel(Country country)
        {
            if (Outcome == "what-if" || Outcome == "none" || Kind == WorldClock.ExecutiveKind.Presidency) { return; }
            if (country?.ParliamentSeats == null || country.ParliamentSeats.Count == 0) { return; }
            if (Outcome == "of record") { OfRecord = true; }
            int total = 0, cabinet = 0, support = 0;
            foreach (KeyValuePair<string, int> seat in country.ParliamentSeats)
            {
                total += seat.Value;
                if (Cabinet.Contains(seat.Key)) { cabinet += seat.Value; }
                else if (Support.Contains(seat.Key)) { support += seat.Value; }
            }
            int majority = total / 2 + 1;
            Outcome = cabinet >= majority ? CoalitionOutcomeKind.MajorityCoalition.ToString()
                : Support.Count > 0 && cabinet + support >= majority ? CoalitionOutcomeKind.ConfidenceAndSupply.ToString()
                : CoalitionOutcomeKind.MinorityGovernment.ToString();
        }

        /// <summary>The Speaker discharges the government; it serves on as a caretaker from <paramref name="on"/> (RF 6 kap. 9 §, 11 §) - and, §646, the
        /// outgoing government through a Speaker's round after an election (premise 8).</summary>
        public void Discharge(DateTime on)
        {
            Caretaker = true;
            CaretakerSince = on;
            Version++;
        }

        /// <summary>AF-8 (Konstytucja Art. 154 ust. 1): the President appoints the cabinet that resigned as the new one - the record's designee of 2023 was the
        /// outgoing prime minister - so it governs again from <paramref name="on"/>, no longer a caretaker - the same record, its term unbroken as the record's row is.</summary>
        public void Reappoint(DateTime on)
        {
            Caretaker = false;
            CaretakerSince = DateTime.MinValue;
            ReappointedOn = on;
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
        /// §706 (Elias's ruling of 2026-10-01: the Treasury lock lifted, Finance weighed): allocates every portfolio by Gamson's law WITH
        /// SALIENCE - each post at Druckman &amp; Warwick's published weight (<see cref="PortfolioSalience"/>), the head of government's weight
        /// credited to its party. [AUTHORED-DRAFT] the method: a party's entitlement is its share of the cabinet's seats times the total weight;
        /// the posts, heaviest first, each go to the party with the most entitlement outstanding that covers the post's whole weight (a near-tie to the
        /// larger party, §711); AF-6 (q6): the posts no entitlement covers - the rounding's residual - go to the partners rather than the head's
        /// party, Warwick &amp; Druckman's small-party bonus as read (the loop's note); the head's party holds
        /// at least one post (the rule before, kept - its levers pass the gates anyway, the post is its minister's). Finance goes where the weights put it,
        /// since §753 the weights DATED by the record's own day (<see cref="FormedOn"/>); which party that is, per chamber and against the record, is
        /// PortfolioSalienceDiagnostic's to print; it carries its levers to whoever holds it (§634).
        /// </summary>
        /// <summary>§711 [AUTHORED-DRAFT], the play-calibration list's 25th entry: an entitlement gap smaller than this share of a post's own weight is a
        /// near-tie, and the post goes to the larger party (Elias's ruling of 2026-10-01, item 2). A tenth of the post: the 2026 Riksdag's Finance
        /// gap (0.056 of 1.68, a thirtieth) was one and the Bundestags' Finance gaps were not (§753); since AF-6 the near-tie is read among the parties whose entitlement covers the post.</summary>
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
            foreach (CabinetPortfolio p in all) { weightTotal += PortfolioSalience.Weight(country.Id, p, FormedOn); }
            var outstanding = new Dictionary<string, double>();
            var due = new Dictionary<string, double>();   // AF-6: each party's whole Gamson entitlement, the residual pass's yardstick
            foreach (string party in Cabinet)
            {
                double share = total > 0 ? seats[party] / (double)total : 1.0 / Cabinet.Count;
                due[party] = share * weightTotal;
                outstanding[party] = share * weightTotal - (party == PmParty ? head : 0.0);
            }
            var order = new List<string>(Cabinet);
            order.Sort((a, b) => seats[b].CompareTo(seats[a]));
            foreach (string party in order) { Portfolios[party] = new List<CabinetPortfolio>(); }
            var posts = new List<CabinetPortfolio>(all);
            posts.Sort((a, b) => PortfolioSalience.Weight(country.Id, b, FormedOn).CompareTo(PortfolioSalience.Weight(country.Id, a, FormedOn)) is int c && c != 0 ? c : ((int)a).CompareTo((int)b));   // heaviest first, a tie in the enum's order
            var residual = new List<CabinetPortfolio>();
            foreach (CabinetPortfolio post in posts)
            {
                // §711 (Elias's ruling of 2026-10-01, item 2: "Near-ties in portfolio allocation go to the larger party"): the post goes to the larger
                // party of any whose outstanding entitlement is within a near-tie of the most outstanding - [AUTHORED-DRAFT] a near-tie is less than
                // NearTieShare of the post's own weight (the play-calibration list's 25th entry). Order is by seats, so the first such party is the larger.
                // AF-6 (q6, "Warwick & Druckman's small-party bonus as read"): this pass pays only WHOLE entitlements - a post goes only to a party whose
                // outstanding entitlement covers its whole weight; a post no party's entitlement covers is the rounding's residual, paid below.
                double weight = PortfolioSalience.Weight(country.Id, post, FormedOn);
                double most = double.MinValue;
                foreach (string party in order) { if (outstanding[party] >= weight - 1e-9) { most = Math.Max(most, outstanding[party]); } }
                if (most == double.MinValue) { residual.Add(post); continue; }
                double nearTie = NearTieShare * weight;
                string taker = null;
                foreach (string party in order) { if (outstanding[party] >= weight - 1e-9 && outstanding[party] >= most - nearTie - 1e-9) { taker = party; break; } }
                Portfolios[taker].Add(post);
                outstanding[taker] -= weight;
            }
            // AF-6 (q6): THE SMALL-PARTY BONUS AS READ (`docs/reference/GAMSON_PORTFOLIOS.md`, "Reading for the model": "the residual of the rounding (and
            // the observed bonus of element 3) going to the smaller partners rather than to the formateur"; [BF73] "large parties tend to be proportionately
            // underpaid and small parties overpaid"; [WD01] "slight deviations from proportionality coming at the expense of larger parties that lead
            // coalition negotiations"). No abstract sizes the bonus, so it is the residual's direction alone: each residual post, heaviest first, goes to a
            // partner other than the head's party - the one whose holding with the post stands least above its due in PROPORTION ((held + post) / due least;
            // an exact tie to the smaller), Gamson's own measure being a share against a share. [AUTHORED-DRAFT] the review's M4: by most entitlement
            // outstanding, the first cut paid a 7-seat partner a post worth 2.2 times its due (Italy 2022's Noi Moderati); a cabinet of one party takes its own.
            var partners = new List<string>();
            foreach (string party in order) { if (party != PmParty) { partners.Add(party); } }
            if (partners.Count == 0) { partners.AddRange(order); }
            foreach (CabinetPortfolio post in residual)
            {
                string taker = null;
                double weight = PortfolioSalience.Weight(country.Id, post, FormedOn);
                double least = double.MaxValue;
                for (int i = partners.Count - 1; i >= 0; i--)
                {
                    string party = partners[i];
                    double over = due[party] > 0.0 ? (weight - outstanding[party]) / due[party] : double.MaxValue / 2.0;   // (held + post) / due - 1; a seatless party last
                    if (taker == null || over < least - 1e-9) { taker = party; least = over; }
                }
                Portfolios[taker].Add(post);
                outstanding[taker] -= weight;
            }
            if (PmParty != null && Portfolios.TryGetValue(PmParty, out List<CabinetPortfolio> pmHeld) && pmHeld.Count == 0)
            {
                // The head of government's party holds a portfolio whatever its share: the lightest post of the party holding the most weight moves to it.
                string richest = null; double richestWeight = -1.0;
                foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in Portfolios)
                {
                    double w = PortfolioSalience.Of(country.Id, kv.Value, FormedOn);
                    if (kv.Value.Count > 1 && w > richestWeight) { richest = kv.Key; richestWeight = w; }
                }
                if (richest != null)
                {
                    CabinetPortfolio lightest = Portfolios[richest][0];
                    foreach (CabinetPortfolio p in Portfolios[richest]) { if (PortfolioSalience.Weight(country.Id, p, FormedOn) < PortfolioSalience.Weight(country.Id, lightest, FormedOn)) { lightest = p; } }
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

        /// <summary>§646: the posts Gamson's law allocates each party of a proposed cabinet (<see cref="AllocatePortfolios"/>), each post weighed by its salience (§706) - what a partner expects.
        /// §753: the weights are the ministries' as they stand on <paramref name="asOf"/> (the formation's day; the world's epoch where none is named).</summary>
        public static Dictionary<string, List<CabinetPortfolio>> GamsonPosts(Country country, IEnumerable<string> cabinet, string pmParty, DateTime? asOf = null)
        {
            var scratch = new GovernmentRecord { PmParty = pmParty, FormedOn = asOf ?? Simulation.SimulationManager.EpochDate };
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
        /// PS-6 US-7: a party's role ON A DATE, for the words a player reads - <see cref="RoleOf"/> toward the sitting government, except that a party in
        /// opposition whose candidate the game elected president, the oath still ahead (<see cref="UsPresidentialElection.TryPresidentElect"/>), is the
        /// <see cref="PlayerRole.PresidentElect"/>. It governs from the oath, not before, so every test of who governs reads <see cref="RoleOf"/>; the desk's
        /// chip and the role gate's reason read this. A president-elect of the sitting president's own party governs already, and reads as the head's party.
        /// PS-6 US-19 (R-US7 (a)): in a presidency whose Congress the game seats - both chambers, the player's USA (`Simulation.Congress.Applies`) - a party out
        /// of the White House that is not the president-elect reads by the chambers it holds that day: <see cref="PlayerRole.HouseMajority"/> where it holds the
        /// House, <see cref="PlayerRole.SenateMajority"/> where it holds the Senate alone, <see cref="PlayerRole.Minority"/> where it holds neither - so no US
        /// party reads as in opposition. An AI USA seats no Senate, and its roles stay the government's.
        /// </summary>
        public static PlayerRole RoleOn(Country country, string abbrev, DateTime date)
        {
            GovernmentRecord g = country?.Government;
            if (g == null || string.IsNullOrEmpty(abbrev)) { return PlayerRole.None; }
            PlayerRole role = g.RoleOf(abbrev);
            if (role == PlayerRole.Opposition && UsPresidentialElection.TryPresidentElect(country, date, out UsPresidentialElection.Contest elect) && elect.WinnerParty == abbrev)
            {
                return PlayerRole.PresidentElect;
            }
            if (role == PlayerRole.Opposition && g.Kind == WorldClock.ExecutiveKind.Presidency && Simulation.Congress.Applies(country))
            {
                return Simulation.Congress.HoldsHouse(country, abbrev) ? PlayerRole.HouseMajority
                    : Simulation.Congress.HoldsSenate(country, abbrev, date) ? PlayerRole.SenateMajority
                    : PlayerRole.Minority;
            }
            return role;
        }

        /// <summary>
        /// PS-6 US-19: whether the presidency in office is the game's own - its president sworn at the oath of a contest the game held (the government labelled
        /// <see cref="UsPresidentialElection.ElectedOutcome"/>) - rather than the record's. "Winning makes the player president" (the political-system spec's §6):
        /// the chip reads PRESIDENT where the player's party won it, and THE PRESIDENT'S PARTY where the president in office is the record's and of the player's
        /// party (DECLARED: the player is the party's nominee, R-US2, and a president of record is not one the player elected). A method, not a property: the
        /// record is saved whole, and a computed property would ride the save.
        /// </summary>
        public bool PresidencyIsTheGames() => Kind == WorldClock.ExecutiveKind.Presidency && Outcome == UsPresidentialElection.ElectedOutcome;

        /// <summary>
        /// PS-6 US-7: THE GOVERNMENT A PRESIDENT FORMS AT THE OATH - a presidency (the plan: "a government of the presidency kind, its cabinet the president's
        /// party, Gamson's allocation not applied"): the president's party alone in the cabinet and leading it, no support and no agreements, every portfolio
        /// the party's. Gamson's law divides a coalition's posts by its parties' seats, and a presidency has no coalition to divide them among: the department heads
        /// are the president's to nominate - Art. II §2 ([CONST]): "he shall nominate, and by and with the Advice and Consent of the Senate, shall appoint ... all
        /// other Officers of the United States" (the Senate's consent not modelled, stated - PS-6 US-15 seats the Senate of record by date, and it votes on no
        /// nomination). <paramref name="outcome"/> labels it
        /// (<see cref="UsPresidentialElection.ElectedOutcome"/> for the game's oath).
        /// </summary>
        public static GovernmentRecord OfPresidency(string executive, string party, DateTime tookOffice, string outcome, string basis)
        {
            var record = new GovernmentRecord { FormedOn = tookOffice, Provisional = false, Kind = WorldClock.ExecutiveKind.Presidency, Executive = executive, PmParty = party, Outcome = outcome, Basis = basis };
            record.Cabinet.Add(party);
            record.Portfolios[party] = new List<CabinetPortfolio>((CabinetPortfolio[])Enum.GetValues(typeof(CabinetPortfolio)));
            return record;
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
                installed.PmParty = HeadParty(country.Id, start) ?? LargestAt(country.Id, start, installed.Cabinet);   // PS-7: on the chamber the record seats on the start's day, whatever epoch the world was opened on
                // PS-7 (K207): a government of record named as a caretaker on the start's day is installed discharged from the row's first day (Italy's Draghi,
                // 21 Jul 2022) - the same words the reference reads a caretaker head by
                if (ofRecord.Head != null && ofRecord.Head.IndexOf("caretaker", StringComparison.OrdinalIgnoreCase) >= 0) { installed.Discharge(ofRecord.From); }
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

        /// <summary>AF-8: a record of a cabinet named outright - a designee or a cabinet of record with no formation behind it (Poland's Art. 154 steps,
        /// `SimulationManager.Sejm.cs`) - its parties, supporters and prime minister's party as given. Built here, by the record's own method, so every write
        /// to who governs stays inside GovernmentRecord (ChamberVerdictCacheCheck's guard).</summary>
        public static GovernmentRecord OfNamedCabinet(string pmParty, IEnumerable<string> cabinet, IEnumerable<string> support, DateTime formedOn, string basis)
        {
            var record = new GovernmentRecord { FormedOn = formedOn, Basis = basis, PmParty = pmParty };
            record.Cabinet.AddRange(cabinet);
            record.Support.AddRange(support);
            return record;
        }

        /// <summary>PS-8: the government an AI country's procedure forms after its count (`SimulationManager.WorldCalendar.cs`) - the projection's cabinet, its supporters
        /// and its prime minister's party as the procedure named them (the Speaker's formateur, the Bundestag's elected candidate, the Sejm's step, the granted
        /// mandate), the posts by Gamson's law and the supporters' agreements as any formed government's; the outcome read off the seats it stands on (GA-3-4's
        /// label), so a projection's view and the seats can never disagree. The executive kind and the head of state above the cabinet carried from the government
        /// it replaces, as the player's installs carry them (L1 of the s938 review). Built here so every write to who governs stays inside the record's methods.</summary>
        public static GovernmentRecord FromProjection(Country country, FormationProjection projection, DateTime formedOn, string basis, World world = null)
        {
            GovernmentRecord outgoing = country.Government;
            var record = new GovernmentRecord { FormedOn = formedOn, Provisional = false, Basis = basis, PmParty = projection.PmParty,
                Kind = outgoing?.Kind ?? WorldClock.ExecutiveKind.Cabinet, Executive = outgoing?.Executive };
            record.Cabinet.AddRange(projection.Cabinet);
            record.Support.AddRange(projection.Support);
            if (record.PmParty == null && record.Cabinet.Count > 0) { record.PmParty = Largest(country, record.Cabinet); }
            record.Relabel(country);
            record.AllocatePortfolios(country);
            record.FormAgreements(country, formedOn, world);
            return record;
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

        /// <summary>PS-7: a non-party head's stand-in PM party (Italy's Draghi) - the cabinet's largest party on the chamber the record seats on <paramref name="date"/>
        /// (`WorldClock.SeatedVintage`; the XVIII Camera at Italy's start), read off the record's table, never off a world opened on another epoch; ties to the
        /// earlier party in the cabinet's order.</summary>
        private static string LargestAt(CountryId id, DateTime date, List<string> cabinet)
        {
            Dictionary<string, int> seats = PartySystems.InitialSeats(id, WorldClock.SeatedVintage(id, date));
            string best = null; int bestSeats = -1;
            foreach (string abbrev in cabinet) { int n = seats.TryGetValue(abbrev, out int s) ? s : 0; if (n > bestSeats) { best = abbrev; bestSeats = n; } }
            return best;
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
