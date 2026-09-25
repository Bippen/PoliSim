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
        /// to a coalition" [BF73]; "one-to-one proportion" [WD01]; "near-perfect relationship" [WD06]), with no formateur premium [WD06]. The six
        /// portfolios apportioned by largest remainder; the prime minister's party keeps the head of government and takes Finance first (the
        /// premise); the rest handed out in the enum's order to the parties by size. STATED, UNSIZED: the literature's deviation - the large party
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
        /// <summary>PS-3i (§636): discharged, serving on as a caretaker until the next government (RF 6 kap. 9 §) - no motion is taken up against it, it orders no extra election.</summary>
        public bool Caretaker;
        public DateTime CaretakerSince = DateTime.MinValue;
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

        /// <summary>Allocates the six portfolios among the cabinet's parties by their seat shares of the cabinet (Gamson's law, above), the prime minister's party taking Finance first.</summary>
        public void AllocatePortfolios(Country country)
        {
            Portfolios.Clear();
            if (Cabinet.Count == 0) { return; }
            var all = (CabinetPortfolio[])Enum.GetValues(typeof(CabinetPortfolio));
            int total = 0;
            var seats = new Dictionary<string, int>();
            foreach (string party in Cabinet) { int held = country.ParliamentSeats != null && country.ParliamentSeats.TryGetValue(party, out int n) ? n : 0; seats[party] = held; total += held; }
            var count = new Dictionary<string, int>();
            var remainder = new List<(string Party, double Rem)>();
            int given = 0;
            foreach (string party in Cabinet)
            {
                double quota = total > 0 ? all.Length * (double)seats[party] / total : all.Length / (double)Cabinet.Count;
                int floor = (int)Math.Floor(quota);
                count[party] = floor; given += floor;
                remainder.Add((party, quota - floor));
            }
            remainder.Sort((a, b) => b.Rem != a.Rem ? b.Rem.CompareTo(a.Rem) : seats[b.Party].CompareTo(seats[a.Party]));   // the larger remainder first, a tie to the larger party
            for (int i = 0; given < all.Length && remainder.Count > 0; i = (i + 1) % remainder.Count) { count[remainder[i].Party]++; given++; }
            if (PmParty != null && count.TryGetValue(PmParty, out int pmCount) && pmCount == 0)
            {
                // The head of government's party holds a portfolio whatever its share: one is taken from the party with the most.
                string richest = null; foreach (KeyValuePair<string, int> kv in count) { if (richest == null || kv.Value > count[richest]) { richest = kv.Key; } }
                if (richest != null && count[richest] > 0) { count[richest]--; count[PmParty] = 1; }
            }
            var order = new List<string>(Cabinet);
            order.Sort((a, b) => seats[b].CompareTo(seats[a]));
            foreach (string party in order) { Portfolios[party] = new List<CabinetPortfolio>(); }
            var pool = new List<CabinetPortfolio>(all);
            if (PmParty != null && count.TryGetValue(PmParty, out int pmTake) && pmTake > 0) { Portfolios[PmParty].Add(CabinetPortfolio.FinanceTreasury); pool.Remove(CabinetPortfolio.FinanceTreasury); }
            foreach (string party in order)
            {
                while (Portfolios[party].Count < count[party] && pool.Count > 0) { Portfolios[party].Add(pool[0]); pool.RemoveAt(0); }
            }
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
        public static GovernmentRecord WhatIfGoverning(Country country, string party, DateTime start)
        {
            GovernmentRecord ofRecord = AtStart(country, start);
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
            // K-1f's premise: a declared own-leader candidacy standing in the cabinet leads it; else the largest cabinet party.
            foreach ((string abbrev, string _, string _) in DeclaredRedLines.CandidaciesAt(country.Id, formedOn))
            {
                if (record.Cabinet.Contains(abbrev)) { record.PmParty = abbrev; break; }
            }
            record.PmParty ??= Largest(country, record.Cabinet);
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

        private static string Largest(Country country, List<string> cabinet)
        {
            string best = null; int bestSeats = -1;
            foreach (string abbrev in cabinet)
            {
                int seats = country.ParliamentSeats.TryGetValue(abbrev, out int n) ? n : 0;
                if (seats > bestSeats) { best = abbrev; bestSeats = seats; }
            }
            return best;
        }
    }
}
