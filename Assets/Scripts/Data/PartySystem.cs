using System;
using System.Collections.Generic;
using PoliSim.Data.Generated;
using PoliSim.Elections;

namespace PoliSim.Data
{
    /// <summary>
    /// W-G1 (item 10): ONE seat-holding unit of a country's parliament — a real party, named as its
    /// own election authority names it, with the position CHES or GPS published for it.
    ///
    /// **This replaces `PartyArchetype`, which was four generic fictional archetypes shared by all
    /// six countries.** The archetypes existed because no party data was on disk; it is now
    /// (`ElectionsData/positions/party_positions.md`, CHES 2024 + GPS 2019, 38 parties across the
    /// six, and each country's own returns file for the seats).
    ///
    /// **A unit is what the country's own election authority reports SEATS for**, which is not the
    /// same question in every country and is deliberately not forced to be:
    /// <list type="bullet">
    /// <item><description>Sweden, Germany, Poland and the USA report seats BY PARTY, so their units
    /// are parties and every one carries a CHES/GPS position.</description></item>
    /// <item><description>France reports seats by the Interior Ministry's NUANCE grid, in which the
    /// whole NFP left ran as one joint candidacy (UG, 178 seats) that the source does not split into
    /// LFI / PS / EELV. France's units are therefore nuances, and the ones that are not a single
    /// party carry <b>no position at all</b> — §36's absence, not a centred guess.</description></item>
    /// <item><description>Italy reports by list inside coalitions and its per-party seat totals are
    /// `[UNCONFIRMED]` in the returns file on disk; that flag is carried here rather than
    /// dropped.</description></item>
    /// </list>
    /// </summary>
    /// <summary>
    /// C-D3: one real, named person who leads a party, with the office as **the party itself names it** —
    /// `partiledare`, `partiordförande`, `språkrör`. Sweden's eight use three different words for the job
    /// and the model keeps all three rather than flattening them to "leader".
    /// </summary>
    public readonly struct PartyLeader
    {
        public readonly string Name;
        /// <summary>The office in the party's own word, unfranslated - the file it comes from cites each one to the party's own page.</summary>
        public readonly string Office;

        public PartyLeader(string name, string office)
        {
            Name = name;
            Office = office;
        }
    }

    /// <summary>C-D3: the three outcomes of asking who a party sends to a leaders' debate. ⚠ The two
    /// absences are NOT the same absence and must never be drawn as one.</summary>
    public enum DebateSeat
    {
        /// <summary>One leader, seated.</summary>
        Resolved,
        /// <summary>The party has more than one equal leader and nothing designates one. Both are known and both are named.</summary>
        AbsentByDesign,
        /// <summary>No leader is sourced for this party. The model does not know - it is not claiming nobody leads it.</summary>
        NotSourced
    }

    public readonly struct PoliticalParty
    {
        /// <summary>The abbreviation the country's own authority uses, FOLDED TO ASCII where the published one is not. THE PERSISTED KEY: ten save fields, every
        /// harness fixture, the parliament seat tables, the ink and mark tables and every asset stem hold it (§566, and the hour `Grüne` arrived and took D18's
        /// inventory red with it). It is never what a player reads - <see cref="ShortName"/> is.</summary>
        public readonly string Abbrev;
        /// <summary>§575 (ruled 2026-09-22, §571 item 5): THE DISPLAY SHORT NAME - the election authority's own abbreviation as it publishes it, diacritics and all
        /// (the Bundeswahlleiter's GRÜNE for Bündnis 90/Die Grünen). Every place a player READS an abbreviation takes this; every key, stem and lookup keeps
        /// <see cref="Abbrev"/>. Defaults to the key, which is right for the fifty-two units whose authority publishes an ASCII abbreviation.</summary>
        public readonly string ShortName;
        /// <summary>The name as published.</summary>
        public readonly string Name;
        /// <summary>CHES 2024 `lrecon`, 0 = left / more state, 10 = right / more market. `float.NaN` where no position is published for this unit — absence, never a centred stand-in.</summary>
        public readonly float LrEcon;
        /// <summary>CHES 2024 `galtan`, 0 = libertarian/post-materialist, 10 = traditional/authoritarian. `float.NaN` where absent.</summary>
        public readonly float Galtan;
        /// <summary>Seats held at the most recent real election on disk (each country's own returns file).</summary>
        public readonly int SeedSeats;

        /// <summary>
        /// The identity mark's file stem (e.g. `"mark_party_se_s"`), or **null when no mark has been
        /// drawn for this party** — which is 52 of the 53 today.
        ///
        /// ⚠ **It is NOT derived from the abbreviation, deliberately.** A derived name would claim a
        /// mark for every party, and `PartyMarkCoverageCheck` treats a claimed-but-unresolvable mark
        /// as an ERROR (rightly — it means a call site will draw nothing where art was promised).
        /// A null is a GAP the UI tolerates by design and the check reports as a warning. So this is
        /// set only where the file actually exists, and the 51 blanks are the Design ask's real size.
        /// </summary>
        public readonly string MarkName;

        /// <summary>
        /// CHES 2024 `eu_position`, **1–7** (1 = strongly opposed to European integration, 7 = strongly
        /// in favour) — note the scale, which is not the 0–10 the other two axes use.
        /// `float.NaN` where no position is published.
        ///
        /// ⚠ **RULED IN AS THE OPENNESS AXIS FOR THE TRADE BILL'S VOTE (R-CL2, 2026-08-30), with the
        /// stretch stated rather than hidden.** §4 asks for a trade axis; CHES publishes no trade or
        /// protectionism item, so nothing on disk measures trade directly. `eu_position` is the
        /// nearest published thing and the ruling adopts it — **which asserts that a party's stance on
        /// European integration stands in for its stance on trade openness.** That is an
        /// approximation, and a real one: a party can be europhile and protectionist, or eurosceptic
        /// and free-trading. It is adopted by a ruling with its own record rather than quietly, which
        /// is W-F2's precedent — that item refused to fill three other axes from CHES questions
        /// without exactly such a ruling.
        ///
        /// ⚠ **The USA has NO value and is not given one:** GPS 2019 carries no EU item at all, so
        /// every US party is `NaN` here and the Trade bill falls back to the fiscal axis with that
        /// reason stated at the call site.
        /// </summary>
        public readonly float EuPosition;

        /// <summary>
        /// C-D3 (ruled by Elias 2026-08-31): **the party's leaders — ALL of them, with the office as the
        /// party itself names it.** Empty where no leader is sourced, which is every party outside Sweden.
        ///
        /// <para>⚠ <b>The ruling is that the model carries BOTH where there are two.</b> The Green Party is
        /// led by two <i>språkrör</i>, and the alternative — storing "the leader" and taking the first —
        /// would silently drop a real named person, which the ruling forbids outright. So this is an array
        /// rather than a field, for one party's sake, and that is the right trade.</para>
        ///
        /// <para><b>Name and office only.</b> `party_leaders_2022.md` and `2026/party_leaders_2026.md` state the rule this follows:
        /// sourcing a real person's NAME does not license inventing their CHARACTER. No attributes, no
        /// biography, no relationships live here; `CandidateProfile`'s numbers stay `[AUTHORED-DRAFT]` and
        /// every screen that shows them says so.</para>
        /// </summary>
        public readonly PartyLeader[] Leaders;

        /// <summary>
        /// CHES 2024 `lrgen` — the party's OVERALL ideological position, 0 = extreme left, 10 = extreme
        /// right. `float.NaN` where no position is published.
        ///
        /// <para>⚠ <b>It is a DIFFERENT axis from `LrEcon` and the difference is the point.</b> `lrecon`
        /// is economic left-right alone; `lrgen` is the summary position an expert would give the party
        /// overall. §29's coalition compatibility weights the two differently — the general axis carries
        /// the ideological term, the economic/social/EU triple carries the policy term — so collapsing
        /// them would make two parties that agree on economics but on nothing else read as natural
        /// partners.</para>
        ///
        /// <para>⚠ <b>The USA has NO value and is not given one.</b> GPS 2019 carries no general
        /// left-right item at all — the positions file records both US rows as
        /// *"[UNCONFIRMED — no lrgen item in GPS]"* — so both US parties are `NaN` here. The USA also has
        /// no coalition formation, so nothing downstream needs it; a centred stand-in would be an
        /// invented figure bought for nothing.</para>
        ///
        /// <para>Added at D-5 (a): the office test asks whether the player's party is in the cabinet, and
        /// that needs the same compatibility matrix `CoalitionFilm` proved for Sweden — which reads this
        /// axis. Sourced from `ElectionsData/positions/party_positions.md` for all 31 scored EU units.</para>
        /// </summary>
        public readonly float LrGen;

        // ---- F5 (2026-09-02): the five §4 axes S-4 called "undefined". All five are CHES 2024 columns,
        // read from `CHES_2024_final_v2.csv` for every unit below that carries a CHES position (matched
        // on the unit's own lrecon/galtan pair, never by name), `float.NaN` elsewhere. D-18 held them
        // out until the codebook's endpoints were QUOTED, not inferred; they are quoted here from
        // `CHES.2024.Codebook.pdf` (text extracted 2026-09-02 - the earlier read hit subsetted-font
        // ciphers; `pdftotext` reads it), and the direction of each is the survey's, verbatim.

        /// <summary>CHES 2024 `environment` - "0 = strongly supports environmental protection even at the cost of economic growth … 10 = strongly supports economic growth even at the cost of environmental protection". `float.NaN` where absent.</summary>
        public readonly float Environment;
        /// <summary>CHES 2024 `regions` - "0 = strongly favors political decentralisation … 10 = strongly opposes political decentralisation". `float.NaN` where absent.</summary>
        public readonly float Regions;
        /// <summary>CHES 2024 `spendvtax` - "0 = strongly favors improving public services … 10 = strongly favors reducing taxes". `float.NaN` where absent.</summary>
        public readonly float SpendVsTax;
        /// <summary>CHES 2024 `immigrate_policy` - "0 = strongly favors a liberal policy on immigration … 10 = strongly favors a restrictive policy on immigration". `float.NaN` where absent.</summary>
        public readonly float ImmigratePolicy;
        /// <summary>CHES 2024 `deregulation` - "0 = strongly opposes deregulation of markets … 10 = strongly favors deregulation of markets". `float.NaN` where absent.</summary>
        public readonly float Deregulation;

        // ---- P4-A2 / P4-A3 (Playtest 4, 2026-09-04): three more CHES 2024 columns, read from the same
        // `CHES_2024_final_v2.csv` (fetched 2026-09-04, 279 parties) for every unit below that carries a CHES
        // position - matched on the unit's own lrecon/galtan pair as F5's five were, `spendvtax` re-read as the
        // check that the match held - and quoted from `CHES.2024.Codebook.pdf` (pdftotext, 2026-09-04) verbatim.

        /// <summary>CHES 2024 `redistribution` - "where did these political parties stand on REDISTRIBUTION in 2024? 0 = strongly favors redistribution … 10 = Strongly opposes redistribution". `float.NaN` where absent.</summary>
        public readonly float Redistribution;
        /// <summary>CHES 2024 `people_v_elite` - "Some political parties take the position that 'THE PEOPLE' should have the final say on the most important issues … At the opposite pole are political parties that believe that ELECTED REPRESENTATIVES should make the most important political decisions. 0 = elected office holders should make the most important decisions … 10 = 'the people', not politicians, should make the most important decisions". `float.NaN` where absent. The USA's two carry the Global Party Survey 2019's V8 populist-rhetoric scale instead (US-32, the plan's mapping) - a READING, its wording not this item's: "0 Strongly favors pluralist rhetoric … 10 Strongly favors populist rhetoric" (`UsGps2019`, `ElectionsData/positions/party_positions.md`).</summary>
        public readonly float PeopleVsElite;
        /// <summary>CHES 2024 `anti_elite_salience` - "How salient has ANTI-ESTABLISHMENT and ANTI-ELITE RHETORIC been to each party during 2024? 0 = not important at all … 10 = extremely important". `float.NaN` where absent. ⚠ The USA's two carry none: the Global Party Survey 2019's V9 (how important populist rhetoric is) is read and printed (`UsGps2019`) and HELD under ruling G7 (§797) - installed here it would turn the Republicans' campaign cast populist, a cast move that goes to Elias with its size before it lands (US-32).</summary>
        public readonly float AntiEliteSalience;

        /// <summary>CHES 2024 `civlib_laworder` - "Position on CIVIL LIBERTIES VS. LAW AND ORDER. 0 = strongly favors civil liberties … 10 = strongly favors tough measures to fight crime". `float.NaN` where absent.</summary>
        public readonly float CivLibLawOrder;
        /// <summary>CHES 2024 `nationalism` - "Position towards COSMOPOLITANISM VS. NATIONALISM. 0 = strongly promotes cosmopolitan conceptions of society … 10 = strongly promotes nationalist conceptions of society". `float.NaN` where absent.</summary>
        public readonly float Nationalism;

        /// <summary>True when a populism position is on the record for this party - CHES's own, or GPS 2019's V8 for the USA.</summary>
        public bool HasPopulism => !float.IsNaN(PeopleVsElite);

        public PoliticalParty(string abbrev, string name, float lrEcon, float galtan, int seedSeats,
            string markName = null, float euPosition = float.NaN, PartyLeader[] leaders = null,
            float lrGen = float.NaN, float environment = float.NaN, float regions = float.NaN,
            float spendVsTax = float.NaN, float immigratePolicy = float.NaN, float deregulation = float.NaN,
            float redistribution = float.NaN, float peopleVsElite = float.NaN, float antiEliteSalience = float.NaN,
            float civLibLawOrder = float.NaN, float nationalism = float.NaN, string shortName = null)
        {
            Environment = environment; Regions = regions; SpendVsTax = spendVsTax;
            ImmigratePolicy = immigratePolicy; Deregulation = deregulation;
            Redistribution = redistribution; PeopleVsElite = peopleVsElite; AntiEliteSalience = antiEliteSalience;
            CivLibLawOrder = civLibLawOrder; Nationalism = nationalism;
            Abbrev = abbrev;
            ShortName = string.IsNullOrEmpty(shortName) ? abbrev : shortName;   // §575: the published abbreviation where it differs from the ASCII key
            Name = name;
            LrEcon = lrEcon;
            Galtan = galtan;
            SeedSeats = seedSeats;
            MarkName = markName;
            EuPosition = euPosition;
            LrGen = lrGen;
            Leaders = leaders ?? System.Array.Empty<PartyLeader>();
        }

        /// <summary>
        /// C-D3: **who this party puts in a leaders' debate — or the stated reason nobody can be seated.**
        ///
        /// <para>The ruling, in order: the party's own statutes or its published campaign materials decide;
        /// <b>if neither resolves it, seat NEITHER and state the absence.</b> Never silently drop a real
        /// named person.</para>
        ///
        /// <para>⚠ <b>The two absences are different and are reported differently</b> — C-C8's precedent,
        /// where "no bilateral trade link" had to read differently from "trade of zero".
        /// <see cref="DebateSeat.NotSourced"/> means the model does not know who leads this party;
        /// <see cref="DebateSeat.AbsentByDesign"/> means it knows exactly who leads it, knows there are two
        /// of them, and knows their own statutes make them equal. A screen collapsing those into "no
        /// leader" would state something false about a real party.</para>
        /// </summary>
        public DebateSeat ResolveDebateSeat(out PartyLeader leader, out string reason)
        {
            leader = default;

            if (Leaders == null || Leaders.Length == 0)
            {
                reason = $"No leader is sourced for {Name}. The model does not know who leads this party - "
                         + "which is not a claim that nobody does.";
                return DebateSeat.NotSourced;
            }

            if (Leaders.Length == 1)
            {
                leader = Leaders[0];
                reason = null;
                return DebateSeat.Resolved;
            }

            var named = new string[Leaders.Length];
            for (int i = 0; i < Leaders.Length; i++) { named[i] = $"{Leaders[i].Name} ({Leaders[i].Office})"; }

            reason = $"{Name} has {Leaders.Length} equal leaders - {string.Join(" and ", named)} - and neither its "
                     + "statutes nor its published campaign materials put one of them forward for a debate. "
                     + "Its stadgar elect two equal sprakror of different genders (11.1-11.2) whose task is to "
                     + "represent the party (11.4), with no clause designating one for any particular setting. "
                     + "So neither is seated, and both are named.";
            return DebateSeat.AbsentByDesign;
        }

        /// <summary>Read by `PartyMarkCoverageCheck` through reflection, which is why the name is its and not ours.</summary>
        public string EnglishName => Name;

        /// <summary>True when CHES publishes an EU position for this unit — the openness axis R-CL2
        /// ruled in for the Trade bill. False for every US party (GPS 2019 has no EU item) and for
        /// every unit CHES does not score. A false is "not measured", never "neutral on Europe".</summary>
        public bool HasEuPosition => !float.IsNaN(EuPosition);

        /// <summary>True when CHES/GPS publishes an economic position for this unit. §36: a unit without one is drawn as unmeasured, never as centre.</summary>
        public bool HasPosition => !float.IsNaN(LrEcon);
    }
}

namespace PoliSim.Data
{
    /// <summary>K-1 (2026-09-23): the election a table is read AS OF. The live game reads <see cref="Seated"/>, the chamber it seats
    /// (Sweden: the Riksdag elected 2026-09-13, Valmyndigheten's result fixed 2026-09-19). The backtests that assert a 2022 result pin
    /// <see cref="Sweden2022"/>, so the 2022 evidence stays reproducible after the seed moved. A country with one vintage ignores it.</summary>
    public enum ElectionVintage
    {
        /// <summary>The chamber the world seats: resolved by `WorldClock.Resolve` to the election whose lists seat the chamber of record at the epoch (PS-1, §618).</summary>
        Seated = 0,
        Sweden2022 = 1,
        // PS-1 (2026-09-25): every chamber of record the six `records_by_date.md` date, one member each, so a table can be asked for by its election.
        Sweden2018 = 2,
        Sweden2026 = 3,
        Germany2021 = 4,
        Germany2025 = 5,
        Poland2019 = 6,
        Poland2023 = 7,
        Italy2018 = 8,
        Italy2022 = 9,
        Usa2020 = 10,
        Usa2022 = 11,
        Usa2024 = 12,
        France2022 = 13,
        France2024 = 14,
        /// <summary>PS-6 US-2: the 120th Congress's House, elected 2026-11-03 - after the record's date, so not on record (its table unsourced).</summary>
        Usa2026 = 15,
    }

    /// <summary>
    /// W-G1: the six countries' real party systems and real chamber sizes, replacing
    /// `PartyArchetypeData`'s four shared fictional archetypes and `ParliamentConstants.TotalSeats`
    /// 200 ("an arbitrary round number for a clean visualization").
    ///
    /// **Every figure here is SOURCED.** Seats are each country's own most recent election on disk
    /// (`ElectionsData/<country>/returns_*.md`); positions are `ElectionsData/positions/
    /// party_positions.md` (CHES 2024 for the five European countries, GPS 2019 for the USA, whose
    /// pre-2020 vintage the positions file carries prominently). **Nothing here is invented, and no
    /// unit without a published position is given one.**
    ///
    /// **Every chamber sums to its stated size** — asserted, not assumed: Sweden 349, Germany 630,
    /// France 577, Italy 400, Poland 460, USA 435.
    /// </summary>
    public static class PartySystems
    {
        /// <summary>§575: the DISPLAY short name for a key - the election authority's own abbreviation where it differs from the ASCII key
        /// (<see cref="PoliticalParty.ShortName"/>). For the sites that hold a key and no party: the player's own party, a division's side. An
        /// unknown key prints as itself, because a key the table does not hold is what the reader needs to see.</summary>
        public static string ShortName(CountryId id, string abbrev)
        {
            if (string.IsNullOrEmpty(abbrev)) { return abbrev; }
            foreach (PoliticalParty p in For(id)) { if (string.Equals(p.Abbrev, abbrev, System.StringComparison.Ordinal)) { return p.ShortName; } }
            return abbrev;
        }

        /// <summary>SOURCED chamber sizes: Riksdag 349, Bundestag 630 (2025), Assemblée nationale 577 (Art. LO119), Camera 400, Sejm 460, House 435. Each is stated in that country's returns file on disk.</summary>
        public static int ChamberSeats(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden:  return 349;
                case CountryId.Germany: return 630;
                case CountryId.France:  return 577;
                case CountryId.Italy:   return 400;
                case CountryId.Poland:  return 460;
                case CountryId.USA:     return 435;
                default: return 0;
            }
        }

        /// <summary>
        /// C-D3, re-sourced by K-1e (2026-09-24): the leaders each party has **at the seated chamber's election, 2026-09-13**
        /// (unchanged at 2026-09-24).
        ///
        /// <para><b>SOURCED</b> — every name and office is `ElectionsData/sweden/2026/party_leaders_2026.md`, which takes each
        /// from the party's OWN website, as captured by the Internet Archive on election day and as live on 2026-09-24. **A
        /// party is the authority on who leads it.** Where a party uses both words (<i>partiledare</i> and <i>partiordförande</i> -
        /// S, SD, C, KD and L do), the office carried is the word on the leader's OWN page (the page about that person), and where
        /// that page carries both, the word of its prose: S <i>partiordförande</i>, SD <i>partiledare</i> (its metadata says
        /// partiordförande), C <i>partiledare</i> (her own page, `/elisabeth-thand-ringqvist`, the only word it carries - in its photographs'
        /// captions; its CV subpage says Partiordförande), KD <i>partiledare</i> (her own prose; the page's list of posts says
        /// Partiordförande), L <i>partiledare</i>.</para>
        ///
        /// <para><b>The vintage is the seated chamber's.</b> Since 2022, C (Elisabeth Thand Ringqvist), L (Simona Mohamsson) and
        /// MP (Amanda Lind and Daniel Helldén) changed leader; S, SD, M, V and KD did not. (This doc said "C, L, S, MP and V" until
        /// K-1e - S and V were wrong.) The 2022 roster stays the record in `ElectionsData/sweden/party_leaders_2022.md`.</para>
        ///
        /// <para>⚠ <b>MP carries TWO, and that is the whole point of the item.</b> Both are named; neither
        /// is seated in a debate. See <see cref="PoliticalParty.ResolveDebateSeat"/>.</para>
        /// </summary>
        private static PartyLeader[] One(string name, string office) => new[] { new PartyLeader(name, office) };

        // ---- Sweden: Riksdag 2026 (K-1: Valmyndigheten's decision of 2026-09-19, Dnr VAL-735-2026; 2026/returns_2026.md).
        // Units are PARTIES; every one has a CHES position. Sums to 349. The ORDER is the key order every positional table uses
        // (TryHistory, the campaign cast, CoalitionFilm) - 2022's seat order, kept when the seats moved; seat-ordered views sort themselves.
        private static readonly PoliticalParty[] SwedenParties =
        {
            new PoliticalParty("S",  "Arbetarepartiet-Socialdemokraterna", 3.68f, 4.74f,  99, "mark_party_se_s", euPosition: 5.74f, lrGen: 3.74f, environment: 3.70f, regions: 5.29f, spendVsTax: 2.50f, immigratePolicy: 6.95f, deregulation: 3.67f, redistribution: 3.00f, peopleVsElite: 0.92f, antiEliteSalience: 2.67f, civLibLawOrder: 6.40f, nationalism: 4.50f,
                leaders: One("Magdalena Andersson", "partiordförande")),
            new PoliticalParty("SD", "Sverigedemokraterna",                6.32f, 9.00f,  62, "mark_party_se_sd", euPosition: 2.68f, lrGen: 8.53f, environment: 9.00f, regions: 5.29f, spendVsTax: 5.50f, immigratePolicy: 9.95f, deregulation: 4.57f, redistribution: 5.37f, peopleVsElite: 5.33f, antiEliteSalience: 8.00f, civLibLawOrder: 9.30f, nationalism: 9.50f,
                leaders: One("Jimmie Åkesson", "partiledare")),
            new PoliticalParty("M",  "Moderaterna",                        7.89f, 6.47f,  70, "mark_party_se_m", euPosition: 5.74f, lrGen: 7.58f, environment: 7.60f, regions: 5.71f, spendVsTax: 8.00f, immigratePolicy: 8.47f, deregulation: 8.27f, redistribution: 7.32f, peopleVsElite: 0.92f, antiEliteSalience: 2.33f, civLibLawOrder: 8.70f, nationalism: 7.10f,
                leaders: One("Ulf Kristersson", "partiledare")),
            new PoliticalParty("V",  "Vänsterpartiet",                     1.89f, 2.42f,  30, "mark_party_se_v", euPosition: 3.32f, lrGen: 1.58f, environment: 1.40f, regions: 5.00f, spendVsTax: 0.50f, immigratePolicy: 3.05f, deregulation: 0.87f, redistribution: 1.37f, peopleVsElite: 3.92f, antiEliteSalience: 5.50f, civLibLawOrder: 2.10f, nationalism: 2.10f,
                leaders: One("Nooshi Dadgostar", "partiledare")),
            new PoliticalParty("C",  "Centerpartiet",                      7.84f, 2.95f,  25, "mark_party_se_c", euPosition: 6.11f, lrGen: 5.95f, environment: 2.50f, regions: 4.29f, spendVsTax: 7.50f, immigratePolicy: 3.42f, deregulation: 8.53f, redistribution: 6.58f, peopleVsElite: 2.55f, antiEliteSalience: 2.17f, civLibLawOrder: 3.10f, nationalism: 2.90f,
                leaders: One("Elisabeth Thand Ringqvist", "partiledare")),
            new PoliticalParty("KD", "Kristdemokraterna",                  7.26f, 7.79f,  22, "mark_party_se_kd", euPosition: 5.35f, lrGen: 8.00f, environment: 7.40f, regions: 5.86f, spendVsTax: 6.50f, immigratePolicy: 8.26f, deregulation: 6.80f, redistribution: 6.58f, peopleVsElite: 2.27f, antiEliteSalience: 3.83f, civLibLawOrder: 7.90f, nationalism: 7.20f,
                leaders: One("Ebba Busch", "partiledare")),
            // ⚠ TWO leaders, carried as two. Taking "the first" would drop a real named person - Daniel
            // Helldén since K-1e (Per Bolund in the 2022 record) - which the ruling forbids outright.
            new PoliticalParty("MP", "Miljöpartiet de gröna",              3.16f, 1.95f,  22, "mark_party_se_mp", euPosition: 5.32f, lrGen: 2.74f, environment: 0.10f, regions: 4.43f, spendVsTax: 2.75f, immigratePolicy: 2.16f, deregulation: 3.47f, redistribution: 2.89f, peopleVsElite: 5.67f, antiEliteSalience: 2.67f, civLibLawOrder: 1.60f, nationalism: 1.70f,
                leaders: new[] { new PartyLeader("Amanda Lind", "språkrör"), new PartyLeader("Daniel Helldén", "språkrör") }),
            new PoliticalParty("L",  "Liberalerna",                        7.32f, 4.47f,  19, "mark_party_se_l", euPosition: 6.84f, lrGen: 6.74f, environment: 6.20f, regions: 4.71f, spendVsTax: 6.75f, immigratePolicy: 6.79f, deregulation: 7.33f, redistribution: 6.26f, peopleVsElite: 1.36f, antiEliteSalience: 2.17f, civLibLawOrder: 5.70f, nationalism: 3.90f,
                leaders: One("Simona Mohamsson", "partiledare")),
        };

        // ---- Germany: Bundestag 2025. Units are PARTIES; every one has a CHES position. Sums to 630.
        // BSW (4.98 %) and FDP (4.3 %) missed the threshold and hold NO seats - carried at zero
        // rather than omitted, because a party that just missed is part of the system the player
        // is looking at, and dropping it would hide the closest thing to a threshold story there is.
        private static readonly PoliticalParty[] GermanyParties =
        {
            new PoliticalParty("CDU",   "Christlich Demokratische Union",   6.58f, 6.56f, 164, "mark_party_de_cdu", euPosition: 6.42f, lrGen: 6.58f, environment: 5.80f, regions: 3.67f, spendVsTax: 6.25f, immigratePolicy: 7.21f, deregulation: 6.17f, redistribution: 6.39f, peopleVsElite: 2.64f, antiEliteSalience: 2.75f, civLibLawOrder: 7.11f, nationalism: 5.86f),
            new PoliticalParty("AfD",   "Alternative für Deutschland",      7.63f, 9.39f, 152, "mark_party_de_afd", euPosition: 1.89f, lrGen: 9.26f, environment: 9.00f, regions: 4.25f, spendVsTax: 7.10f, immigratePolicy: 9.95f, deregulation: 6.60f, redistribution: 7.06f, peopleVsElite: 8.00f, antiEliteSalience: 9.00f, civLibLawOrder: 9.11f, nationalism: 9.57f),
            new PoliticalParty("SPD",   "Sozialdemokratische Partei",       3.47f, 3.61f, 120, "mark_party_de_spd", euPosition: 6.37f, lrGen: 3.47f, environment: 3.40f, regions: 6.33f, spendVsTax: 2.33f, immigratePolicy: 5.00f, deregulation: 3.33f, redistribution: 2.89f, peopleVsElite: 3.09f, antiEliteSalience: 2.38f, civLibLawOrder: 4.22f, nationalism: 3.29f),
            new PoliticalParty("Grune", "Bündnis 90/Die Grünen",            3.37f, 1.61f,  85, "mark_party_de_grune", euPosition: 6.79f, lrGen: 3.06f, environment: 0.90f, regions: 5.67f, spendVsTax: 2.17f, immigratePolicy: 2.47f, deregulation: 3.50f, redistribution: 3.06f, peopleVsElite: 4.82f, antiEliteSalience: 2.50f, civLibLawOrder: 1.89f, nationalism: 1.29f, shortName: "GRÜNE"),
            new PoliticalParty("Linke", "Die Linke",                        1.37f, 2.29f,  64, "mark_party_de_linke", euPosition: 3.72f, lrGen: 1.42f, environment: 2.11f, regions: 7.00f, spendVsTax: 1.17f, immigratePolicy: 2.56f, deregulation: 1.50f, redistribution: 0.94f, peopleVsElite: 4.33f, antiEliteSalience: 6.88f, civLibLawOrder: 3.00f, nationalism: 1.71f),
            new PoliticalParty("CSU",   "Christlich-Soziale Union",         6.77f, 7.54f,  44, "mark_party_de_csu", euPosition: 5.50f, lrGen: 7.57f, environment: 6.43f, regions: 2.83f, spendVsTax: 5.88f, immigratePolicy: 7.54f, deregulation: 6.67f, redistribution: 6.93f, peopleVsElite: 3.88f, antiEliteSalience: 3.25f, civLibLawOrder: 7.57f, nationalism: 6.50f),
            new PoliticalParty("SSW",   "Südschleswigscher Wählerverband",  float.NaN, float.NaN, 1, "mark_party_de_ssw"),
            new PoliticalParty("BSW",   "Bündnis Sahra Wagenknecht",        2.78f, 7.06f,   0, "mark_party_de_bsw", euPosition: 2.42f, lrGen: 3.63f, environment: 7.22f, regions: 5.67f, spendVsTax: 2.20f, immigratePolicy: 8.63f, deregulation: 2.17f, redistribution: 2.00f, peopleVsElite: 7.00f, antiEliteSalience: 8.86f, civLibLawOrder: 6.25f, nationalism: 6.86f),
            new PoliticalParty("FDP",   "Freie Demokratische Partei",       7.58f, 3.22f,   0, "mark_party_de_fdp", euPosition: 5.84f, lrGen: 6.06f, environment: 6.70f, regions: 4.33f, spendVsTax: 8.83f, immigratePolicy: 6.06f, deregulation: 8.00f, redistribution: 8.11f, peopleVsElite: 3.90f, antiEliteSalience: 2.38f, civLibLawOrder: 2.67f, nationalism: 2.71f),
        };

        // ---- Poland: Sejm 2023. Units are the ELECTORAL COMMITTEES the PKW reports. Sums to 460.
        // TD is the Trzecia Droga committee (Polska 2050 + PSL); CHES scores its two components separately. §766 (Elias's ruling D2, the
        // joint-list rule of §621): the COMMITTEE takes the seat-weighted mean of its two parties' CHES rows, weighted by the seats each won
        // on the 2023 list (`ElectionsData/poland/td_list_2023.md`).

        /// <summary>§766, SOURCED - the KBW's 2023 candidate file (`raw/td_list_2023/`): the seats Polska 2050's members won on the TD list (30 written
        /// in full, 1 as "PL2050"). The list's 4 non-members and 3 Centrum dla Polski members carry no CHES row and no weight.</summary>
        public const int PolandTdPolska2050Seats = 31;
        /// <summary>§766, SOURCED - the same file: the seats PSL's members won on the TD list (26 written in full, 1 as "PSL").</summary>
        public const int PolandTdPslSeats = 27;

        /// <summary>§766: TD's two member parties as CHES 2024 scores them (Poland's rows `PSL` and `Polska 2050`), typed as every roster row is -
        /// members of the joint list, never seated units of their own.</summary>
        public static readonly (PoliticalParty Member, int Seats)[] PolandTdMembers =
        {
            (new PoliticalParty("PSL", "Polskie Stronnictwo Ludowe", 4.17f, 6.97f, 0, euPosition: 5.38f, lrGen: 6.54f, environment: 5.50f, regions: 4.14f, spendVsTax: 5.17f, immigratePolicy: 7.30f, deregulation: 3.69f, redistribution: 3.85f, peopleVsElite: 5.50f, antiEliteSalience: 3.35f, civLibLawOrder: 5.60f, nationalism: 6.75f), PolandTdPslSeats),
            (new PoliticalParty("Polska 2050", "Polska 2050 Szymona Hołowni", 6.32f, 4.89f, 0, euPosition: 6.31f, lrGen: 5.59f, environment: 3.64f, regions: 3.42f, spendVsTax: 5.92f, immigratePolicy: 5.67f, deregulation: 6.15f, redistribution: 5.27f, peopleVsElite: 5.20f, antiEliteSalience: 2.90f, civLibLawOrder: 3.73f, nationalism: 4.06f), PolandTdPolska2050Seats),
        };

        /// <summary>§772 (Elias's ruling E1: "each candidate sits at their own party's position"): the CHES 2024 units a presidential candidate of record
        /// stands at that the Sejm roster does not seat - Razem (Zandberg's party; it ran inside Lewica's 2023 committee and left its club in 2024). Typed
        /// as every roster row is, from CHES 2024's row - `PolishSejmAllocationDiagnostic` holds each to its row on every field, `PresidentialVoteBacktest` on the run-off's axes.</summary>
        public static readonly PoliticalParty[] PolandUnseatedUnits =
        {
            new PoliticalParty("Razem", "Partia Razem", 0.85f, 1.00f, 0, euPosition: 6.44f, lrGen: 1.04f, environment: 1.43f, regions: 2.92f, spendVsTax: 0.50f, immigratePolicy: 1.81f, deregulation: 2.08f, redistribution: 0.96f, peopleVsElite: 5.40f, antiEliteSalience: 6.25f, civLibLawOrder: 1.07f, nationalism: 0.88f),
        };

        /// <summary>
        /// §766 (Elias's ruling D2; the joint-list rule ruled at §621): a joint list without its own CHES position takes the SEAT-WEIGHTED MEAN of
        /// its member parties' positions, field by field - each member weighted by the seats it won on the list; a field a member does not
        /// publish is averaged over the members that do. The list keeps its own key, name, seats and mark.
        /// </summary>
        public static PoliticalParty JointList(string abbrev, string name, int seedSeats, string markName, (PoliticalParty Member, int Seats)[] members)
        {
            float Mean(System.Func<PoliticalParty, float> field)
            {
                double sum = 0.0; int weight = 0;
                foreach ((PoliticalParty member, int seats) in members) { float v = field(member); if (float.IsNaN(v) || seats <= 0) { continue; } sum += (double)v * seats; weight += seats; }
                return weight > 0 ? (float)(sum / weight) : float.NaN;
            }
            return new PoliticalParty(abbrev, name, Mean(p => p.LrEcon), Mean(p => p.Galtan), seedSeats, markName, euPosition: Mean(p => p.EuPosition),
                lrGen: Mean(p => p.LrGen), environment: Mean(p => p.Environment), regions: Mean(p => p.Regions), spendVsTax: Mean(p => p.SpendVsTax),
                immigratePolicy: Mean(p => p.ImmigratePolicy), deregulation: Mean(p => p.Deregulation), redistribution: Mean(p => p.Redistribution),
                peopleVsElite: Mean(p => p.PeopleVsElite), antiEliteSalience: Mean(p => p.AntiEliteSalience), civLibLawOrder: Mean(p => p.CivLibLawOrder),
                nationalism: Mean(p => p.Nationalism));
        }

        private static readonly PoliticalParty[] PolandParties =
        {
            new PoliticalParty("PiS",  "Prawo i Sprawiedliwość",   2.52f, 8.45f, 194, "mark_party_pl_pis", euPosition: 3.10f, lrGen: 7.64f, environment: 7.71f, regions: 7.93f, spendVsTax: 3.69f, immigratePolicy: 8.75f, deregulation: 3.71f, redistribution: 2.61f, peopleVsElite: 4.57f, antiEliteSalience: 7.90f, civLibLawOrder: 8.31f, nationalism: 9.06f),
            new PoliticalParty("KO",   "Koalicja Obywatelska",     6.17f, 3.66f, 157, "mark_party_pl_ko", euPosition: 6.63f, lrGen: 4.93f, environment: 3.57f, regions: 3.57f, spendVsTax: 5.46f, immigratePolicy: 7.00f, deregulation: 6.71f, redistribution: 5.39f, peopleVsElite: 3.00f, antiEliteSalience: 2.55f, civLibLawOrder: 3.12f, nationalism: 3.56f),
            JointList("TD",            "Trzecia Droga",            65, "mark_party_pl_td", PolandTdMembers),   // §766 (D2): Polska 2050 31 and PSL 27 seats of the list
            new PoliticalParty("NL",   "Nowa Lewica",              2.32f, 1.75f,  26, "mark_party_pl_nl", euPosition: 6.90f, lrGen: 2.41f, environment: 1.86f, regions: 3.00f, spendVsTax: 1.92f, immigratePolicy: 3.15f, deregulation: 3.38f, redistribution: 2.37f, peopleVsElite: 5.00f, antiEliteSalience: 2.90f, civLibLawOrder: 2.07f, nationalism: 1.56f),
            new PoliticalParty("Konf", "Konfederacja",             8.96f, 8.41f,  18, "mark_party_pl_konf", euPosition: 1.52f, lrGen: 9.39f, environment: 8.86f, regions: 7.78f, spendVsTax: 9.38f, immigratePolicy: 9.81f, deregulation: 8.46f, redistribution: 8.67f, peopleVsElite: 8.00f, antiEliteSalience: 8.20f, civLibLawOrder: 7.88f, nationalism: 9.81f),
            // PS-1 (2026-09-25, §618): the 2019 Sejm's lists that are not 2023's committees - seated as elected when the chamber of record is the 9th
            // term (poland/records_by_date.md, the PKW notice Dz.U. 2019 poz. 1955 [PKW-2019]; the record's own advice: "its keys should be the 2019
            // committees' own"). SLD's committee, PSL alone (inside TD in 2023) and MN's one seat. Zero seats at the 2023 election, no CHES position,
            // no mark - carried so the 2019 chamber is the election's lists and not a mapping onto 2023's keys.
            new PoliticalParty("SLD",  "Sojusz Lewicy Demokratycznej", float.NaN, float.NaN, 0),
            new PoliticalParty("PSL",  "Polskie Stronnictwo Ludowe",   float.NaN, float.NaN, 0),
            new PoliticalParty("MN",   "Mniejszość Niemiecka",         float.NaN, float.NaN, 0),
        };

        // ---- France: Assemblee nationale 2024. UNITS ARE THE INTERIOR MINISTRY'S NUANCES, not parties,
        // because that is what the Ministry reports seats for and the whole NFP left ran as ONE joint
        // candidacy (UG, 178) that the source DOES NOT SPLIT into LFI / PS / EELV. Attaching CHES
        // positions to LFI, PS and EELV here would be inventing a seat split nobody published.
        // The nuances that ARE a single party carry that party's CHES position; the rest carry NONE.
        // Sums to 577.
        private static readonly PoliticalParty[] FranceParties =
        {
            new PoliticalParty("UG",  "Union de la gauche (NFP joint candidacies)", float.NaN, float.NaN, 178, "mark_party_fr_ug"),
            new PoliticalParty("ENS", "Ensemble (majorité présidentielle)",          6.18f, 4.09f, 150, "mark_party_fr_ens", euPosition: 6.27f, lrGen: 6.27f, environment: 5.67f, regions: 5.00f, spendVsTax: 6.50f, immigratePolicy: 5.73f, deregulation: 7.00f, redistribution: 6.20f, peopleVsElite: 2.71f, antiEliteSalience: 3.75f, civLibLawOrder: 5.80f, nationalism: 4.00f),
            new PoliticalParty("RN",  "Rassemblement National",                      6.00f, 8.36f, 125, "mark_party_fr_rn", euPosition: 2.18f, lrGen: 8.82f, environment: 8.00f, regions: 5.00f, spendVsTax: 4.33f, immigratePolicy: 9.55f, deregulation: 4.00f, redistribution: 4.70f, peopleVsElite: 7.00f, antiEliteSalience: 8.75f, civLibLawOrder: 8.83f, nationalism: 10.00f),
            new PoliticalParty("LR",  "Les Républicains",                            7.82f, 7.18f,  39, "mark_party_fr_lr", euPosition: 5.27f, lrGen: 7.73f, environment: 7.33f, regions: 6.33f, spendVsTax: 7.33f, immigratePolicy: 8.36f, deregulation: 7.29f, redistribution: 7.40f, peopleVsElite: 3.00f, antiEliteSalience: 5.00f, civLibLawOrder: 7.83f, nationalism: 8.33f),
            new PoliticalParty("DVD", "Divers droite",                               float.NaN, float.NaN, 27, "mark_party_fr_dvd"),
            new PoliticalParty("UXD", "Union de l'extrême droite (RN-Ciotti)",       float.NaN, float.NaN, 17, "mark_party_fr_uxd"),
            new PoliticalParty("DVG", "Divers gauche",                               float.NaN, float.NaN, 12, "mark_party_fr_dvg"),
            new PoliticalParty("REG", "Régionaliste",                                float.NaN, float.NaN,  9, "mark_party_fr_reg"),
            new PoliticalParty("HOR", "Horizons",                                    float.NaN, float.NaN,  6, "mark_party_fr_hor"),
            new PoliticalParty("DVC", "Divers centre",                               float.NaN, float.NaN,  6, "mark_party_fr_dvc"),
            new PoliticalParty("UDI", "Union des démocrates et indépendants",        float.NaN, float.NaN,  3, "mark_party_fr_udi"),
            new PoliticalParty("SOC", "Parti socialiste (outside the UG banner)",    3.36f, 2.73f,   2, "mark_party_fr_soc", euPosition: 6.27f, lrGen: 3.45f, environment: 3.33f, regions: 4.50f, spendVsTax: 1.67f, immigratePolicy: 3.18f, deregulation: 3.43f, redistribution: 2.20f, peopleVsElite: 4.00f, antiEliteSalience: 4.25f, civLibLawOrder: 2.83f, nationalism: 2.75f),
            new PoliticalParty("ECO", "Écologistes",                                 2.30f, 1.70f,   1, "mark_party_fr_eco", euPosition: 6.20f, lrGen: 2.30f, environment: 2.00f, regions: 2.33f, spendVsTax: 1.33f, immigratePolicy: 1.60f, deregulation: 1.29f, redistribution: 1.40f, peopleVsElite: 5.33f, antiEliteSalience: 5.00f, civLibLawOrder: 2.17f, nationalism: 0.67f),
            new PoliticalParty("DIV", "Divers",                                      float.NaN, float.NaN,  1, "mark_party_fr_div"),
            new PoliticalParty("EXD", "Extrême droite",                              float.NaN, float.NaN,  1, "mark_party_fr_exd"),
            // SEAT-1 (Elias's ruling J2, §869; E-47 (b)): the XVIe's nuances that are not the XVIIe's - seated where the chamber of record is the Assembly
            // elected in June 2022 (`SeatsAt(France2022)`, the Ministry's FE page). Zero seats at the 2024 election, no CHES position, no mark: the 2022
            // nuances' own keys, never a mapping onto 2024's (Poland's 2019 lists, §618). NUP is the NUPES joint candidacy, which the Ministry does not
            // split, as it does not split UG.
            new PoliticalParty("NUP", "Nouvelle union populaire écologique et sociale (NUPES joint candidacies)", float.NaN, float.NaN, 0),
            new PoliticalParty("DSV", "Droite souverainiste",                        float.NaN, float.NaN,  0),
        };

        /// <summary>SEAT-1 (Elias's ruling J2 on E-48, §869): AVS's two member parties as CHES 2024 scores them (Italy's rows `SI` and `EV`, country 8), typed
        /// as every roster row is - members of the joint list, never seated units of their own. Declared before the roster, which reads it (static
        /// initializers run in textual order). `ItalySeatingDiagnostic` holds each to its CHES row on every field.</summary>
        public static readonly PoliticalParty[] ItalyAvsMembers =
        {
            new PoliticalParty("SI", "Sinistra Italiana", 1.38f, 1.69f, 0, euPosition: 4.85f, lrGen: 1.23f, environment: 1.00f, regions: 6.08f, spendVsTax: 0.25f, immigratePolicy: 0.77f, deregulation: 1.44f, redistribution: 0.92f, peopleVsElite: 5.00f, antiEliteSalience: 4.25f, civLibLawOrder: 1.75f, nationalism: 0.86f),   // [SOURCED] CHES_2024_final_v2.csv, party_id 838
            new PoliticalParty("EV", "Europa Verde", 2.21f, 1.71f, 0, euPosition: 5.71f, lrGen: 2.14f, environment: 0.14f, regions: 6.50f, spendVsTax: 0.33f, immigratePolicy: 1.31f, deregulation: 2.11f, redistribution: 2.17f, peopleVsElite: 5.00f, antiEliteSalience: 2.25f, civLibLawOrder: 1.50f, nationalism: 0.88f),   // [SOURCED] CHES_2024_final_v2.csv, party_id 850
        };

        /// <summary>
        /// SEAT-1 (Elias's ruling J2 on E-48, §869: "AVS takes the equal-weight mean of SI's and EV's CHES positions, DECLARED: there is no primary split of
        /// its seats ... A primary split, if one is ever found, replaces it"): §621's joint-list rule with every member weighted alike - <see cref="JointList"/>
        /// on a weight of one each, so a field a member does not publish is averaged over the members that do, as there. A primary split, once on disk,
        /// replaces the call with <see cref="JointList"/> on the members' seats.
        /// </summary>
        public static PoliticalParty EqualWeightList(string abbrev, string name, int seedSeats, string markName, PoliticalParty[] members)
        {
            var weighted = new (PoliticalParty Member, int Seats)[members.Length];
            for (int i = 0; i < members.Length; i++) { weighted[i] = (members[i], 1); }   // DECLARED (J2): equal weight - no split of the list's seats is on record
            return JointList(abbrev, name, seedSeats, markName, weighted);
        }

        // ---- Italy: Camera dei deputati 2022. Units are the LISTS Eligendo reports. Sums to 400.
        // EVERY per-list seat total in the returns file on disk is [UNCONFIRMED] - Italy elects on a
        // mixed system and the uninominal seats are attributed to lists differently by different
        // sources. THE FLAG IS CARRIED, not dropped: this is the one chamber here whose composition
        // the project has not confirmed against a primary total.
        private static readonly PoliticalParty[] ItalyParties =
        {
            new PoliticalParty("FdI",   "Fratelli d'Italia",              6.40f, 9.13f, 119, "mark_party_it_fdi", euPosition: 3.27f, lrGen: 8.47f, environment: 7.00f, regions: 5.38f, spendVsTax: 6.00f, immigratePolicy: 9.47f, deregulation: 5.40f, redistribution: 7.07f, peopleVsElite: 5.62f, antiEliteSalience: 7.17f, civLibLawOrder: 8.83f, nationalism: 9.50f),
            new PoliticalParty("PD",    "Partito Democratico",             2.93f, 2.33f,  69, "mark_party_it_pd", euPosition: 6.80f, lrGen: 2.93f, environment: 1.88f, regions: 5.85f, spendVsTax: 2.00f, immigratePolicy: 2.20f, deregulation: 3.20f, redistribution: 2.57f, peopleVsElite: 3.12f, antiEliteSalience: 2.67f, civLibLawOrder: 3.33f, nationalism: 1.50f),
            new PoliticalParty("Lega",  "Lega per Salvini Premier",        6.80f, 8.87f,  66, "mark_party_it_lega", euPosition: 1.60f, lrGen: 8.67f, environment: 8.25f, regions: 2.92f, spendVsTax: 7.25f, immigratePolicy: 9.87f, deregulation: 5.90f, redistribution: 7.71f, peopleVsElite: 6.38f, antiEliteSalience: 7.67f, civLibLawOrder: 8.67f, nationalism: 9.12f),
            new PoliticalParty("M5S",   "Movimento 5 Stelle",              2.87f, 3.27f,  52, "mark_party_it_m5s", euPosition: 4.07f, lrGen: 3.33f, environment: 1.50f, regions: 6.45f, spendVsTax: 2.00f, immigratePolicy: 4.87f, deregulation: 3.40f, redistribution: 1.64f, peopleVsElite: 7.12f, antiEliteSalience: 7.50f, civLibLawOrder: 4.33f, nationalism: 4.38f),
            new PoliticalParty("FI",    "Forza Italia",                    7.40f, 6.07f,  45, "mark_party_it_fi", euPosition: 5.33f, lrGen: 6.73f, environment: 7.00f, regions: 5.08f, spendVsTax: 8.50f, immigratePolicy: 6.47f, deregulation: 7.80f, redistribution: 8.21f, peopleVsElite: 4.75f, antiEliteSalience: 3.00f, civLibLawOrder: 6.33f, nationalism: 6.62f),
            new PoliticalParty("AzIV",  "Azione - Italia Viva",            5.21f, 3.46f,  21, "mark_party_it_aziv", euPosition: 6.79f, lrGen: 4.43f, environment: 4.14f, regions: 4.50f, spendVsTax: 4.33f, immigratePolicy: 3.77f, deregulation: 5.90f, redistribution: 4.77f, peopleVsElite: 2.43f, antiEliteSalience: 1.60f, civLibLawOrder: 2.83f, nationalism: 2.50f),
            EqualWeightList("AVS",     "Alleanza Verdi e Sinistra",       12, "mark_party_it_avs", ItalyAvsMembers),   // SEAT-1 (J2, E-48): SI's and EV's equal-weight mean, DECLARED
            new PoliticalParty("NM",    "Noi Moderati",                    float.NaN, float.NaN,  7, "mark_party_it_nm"),
            new PoliticalParty("SVP",   "Südtiroler Volkspartei - PATT",   float.NaN, float.NaN,  3, "mark_party_it_svp"),
            new PoliticalParty("PlusE", "Più Europa",                      float.NaN, float.NaN,  2, "mark_party_it_pluse"),
            new PoliticalParty("IC",    "Impegno Civico",                  float.NaN, float.NaN,  1, "mark_party_it_ic"),
            new PoliticalParty("ScN",   "Sud chiama Nord",                 float.NaN, float.NaN,  1, "mark_party_it_scn"),
            // The last two seats are OUTSIDE the Area Italia basis every row above uses, and the
            // returns file names them: one MAIE deputy in the circoscrizione Estero (the overseas
            // constituency) and one Union Valdotaine deputy in the Valle d'Aosta college. Without
            // them the chamber sums to 398, and rounding 398 up to 400 by inflating a party would
            // have been inventing seats to make a total come out.
            new PoliticalParty("MAIE",  "MAIE (circoscrizione Estero)",    float.NaN, float.NaN,  1, "mark_party_it_maie"),
            new PoliticalParty("UV",    "Union Valdôtaine (VdA college)",  float.NaN, float.NaN,  1, "mark_party_it_uv"),
            // SEAT-1 (Elias's ruling J2, §869): the XVIII Camera's units that are not 2022's lists - seated where the chamber of record is the Camera elected
            // 4 March 2018 (`SeatsAt(Italy2018)`), by its groups at first constitution and the mixed group's named components, DECLARED. Zero seats at the
            // 2022 election, no CHES position, no mark - their own keys, never a mapping onto 2022's (§618's precedent). J2: "seat each component as its list" -
            // read (`ItalyCamera2018`): a group or component sits as the roster's list whose deputies it gathers or whose name it bears (M5S, Lega, PD, FI and FdI;
            // Minoranze linguistiche, the SVP-PATT list's deputies, as SVP; "+Europa-Centro Democratico" as PlusE; "MAIE-Sogno Italia" as MAIE), the rest as their own.
            new PoliticalParty("LeU",     "Liberi e Uguali (group, from 10 Apr 2018)",                  float.NaN, float.NaN, 0),
            new PoliticalParty("CP",      "Civica Popolare-AP-PSI-Area Civica (mixed-group component)", float.NaN, float.NaN, 0),
            new PoliticalParty("NcI",     "Noi con l'Italia (mixed-group component)",                  float.NaN, float.NaN, 0),
            // the remainder's display abbreviation is three letters (the film: "Misto" overflowed the 16-unit chip at 8 px and the signing plate's mark box at
            // 2560, where M5S and FdI fit); the key stays "Misto" (records, D18's row, the diagnostic), the full name the slips' and the brief's
            new PoliticalParty("Misto",   "Gruppo misto, in no component",                             float.NaN, float.NaN, 0, shortName: "MIS"),
        };

        // ---- USA: House of Representatives, 119th Congress (2024). Sums to 435.
        // Positions are GPS 2019, NOT CHES: the positions file records that CHES-USA is unpublished
        // and that GPS is pre-2020 and has no general left-right or EU item. The substitution is
        // named there and named again here so it cannot be read as CHES by a later reader.
        private static readonly PoliticalParty[] UsaParties =
        {
            // US-32: every figure below is GENERATED - `UsGps2019`, written by Tools/us_gps_prep.pl from the GPS 2019 files kept
            // out of tree (ruling G7, §797) and held to them by GeneratedCatalogCheck. The economic and social pair are V4 and V6 as
            // typed since 2026-08-28 (the re-read found them equal at two decimals). People-versus-elite is V8, the populist-rhetoric
            // scale - the plan's mapping, a READING (its wording is not CHES `people_v_elite`'s; the codebook marks V19 as that
            // item's counterpart, on the reversed scale - put to Elias, not installed). It replaces P4-A3's [AUTHORED-DRAFT]
            // placement, which held the term open until GPS was read. V9 (populist saliency) is read and HELD under G7: installed
            // as `antiEliteSalience` it turns the Republicans' cast populist (CampaignCastDiagnostic prints the move).
            new PoliticalParty("REP", "Republican Party", UsGps2019.RepEconomicV4, UsGps2019.RepSocialV6, 220, "mark_party_us_rep", peopleVsElite: UsGps2019.RepPopulistRhetoricV8),
            new PoliticalParty("DEM", "Democratic Party", UsGps2019.DemEconomicV4, UsGps2019.DemSocialV6, 215, "mark_party_us_dem", peopleVsElite: UsGps2019.DemPopulistRhetoricV8),
        };

        /// <summary>
        /// W-G1: the last two national elections'' vote SHARES per party, SOURCED from each
        /// country''s own returns file, in the same order as <see cref="For"/>.
        ///
        /// **Why both, and why this matters more than it looks.** The vote model has layers, and
        /// `CompositionHarness` measures them: the bare national model (§8 alone) reads Sweden 2022
        /// at **MAD 3.25 pp**, and adding the loyalty layer over a prior halves it to **1.47 pp**.
        /// The first wiring of this item used the bare layer and produced a Bundestag in which
        /// **BSW took 97 seats having really won none** and Sweden''s M took 33 of its real 68. That
        /// is not a chamber anyone should be shown. The prior and the loyalty derived from these two
        /// vectors are what make the live election resemble the country it claims to be modelling.
        ///
        /// Only Sweden and Germany carry them, because they are the only two with a live electoral
        /// path — and the join has to be clean party-for-party across both elections, which is a
        /// real constraint: BSW did not exist in 2021, so it enters at a true zero rather than a
        /// missing value.
        ///
        /// <para>K-1 (2026-09-23): <b>Sweden carries two vintages.</b> The live game reads <see cref="ElectionVintage.Seated"/> -
        /// 2026 as the prior and 2022→2026 as the loyalty; the backtests that assert a 2022 result pin
        /// <see cref="ElectionVintage.Sweden2022"/> - 2022 and 2018, the pair every figure above was measured on. §696: <b>Germany carries
        /// two as well</b> - 2025 against 2021 on the 21st chamber, and 2021 against 2017 on the 20th, the chamber a game opened on the
        /// snap start sits on.</para>
        /// </summary>
        public static bool TryHistory(CountryId id, out double[] latest, out double[] previous, ElectionVintage vintage = ElectionVintage.Seated)
        {
            if (!TryRealHistory(id, out latest, out previous, vintage)) { return false; }
            // §671 (SP-3): a created party's history, after the real parties' - a newcomer's prior 0 and previous 0, so no loyal base (§2.7, the
            // vote model's own logic); a Splinter's prior is its chosen slice of the parent's last result, MOVED from the parent. None registered, nothing padded.
            IReadOnlyList<CreatedParty> created = CreatedParties.Of(id);
            if (created.Count == 0) { return true; }
            PoliticalParty[] real = RealRoster(id);
            var l = new double[latest.Length + created.Count];
            var p = new double[previous.Length + created.Count];
            Array.Copy(latest, l, latest.Length);
            Array.Copy(previous, p, previous.Length);
            for (int c = 0; c < created.Count; c++)
            {
                if (created[c].Origin != PartyOrigin.Splinter) { continue; }
                int parent = Array.FindIndex(real, x => x.Abbrev == created[c].ParentKey);
                if (parent < 0 || parent >= latest.Length) { continue; }
                double slice = l[parent] * created[c].InheritedSlice;
                l[parent] -= slice;
                l[latest.Length + c] = slice;
            }
            latest = l; previous = p;
            return true;
        }

        /// <summary>
        /// PS-6 US-26 (R-US11 (a), RULED - F8: "US-26 rolls history forward for every modelled country, Poland's standing history gap (§767) included"):
        /// THE HISTORY ROLLED FORWARD. The game's own counts in <paramref name="played"/> held before <paramref name="before"/> (every one, where it is null)
        /// follow the record's pair, and the last two of that sequence are the next election's prior and loyalty pair: a game's first election reads the
        /// record's pair as before, its second its own first count against the record's last election, its third its own two - W-G1's gap, "two elections in one
        /// game read the same history", closed for Sweden, Germany, Poland and the USA alike. The counts are <see cref="OwnCounts"/>'s.
        /// <para>DECLARED, the base: with a count of the game's own, the record's pair is the one seated at the world's epoch, whatever vintage a caller names - a
        /// chamber the record seats after the epoch was elected in this world by the game itself. With none, the very arrays <paramref name="vintage"/>'s history
        /// gives, so a world that has held no election reads exactly what it read before (numerically inert, proved by the dump).</para>
        /// <para>DECLARED, the scale: a count's shares are fractions of the roster's vote, the record's of every vote cast, and <see cref="LoyaltyModel"/> reads a
        /// pair on one scale - so a count is read at the record's last roster total (the vote off the roster held where the epoch's last election left it).</para>
        /// <para>DECLARED, what does not roll: the regional and district bases (Sweden's valkretsar, Germany's Länder, Poland's okręgi) stay the record's - the
        /// uniform swing measures its swing against the base's own national position, so the record's base swung to the new national shares is the rolled chain
        /// exactly wherever no zero floor bit; and each country's electorate (the USA's solved per seated vintage) stays its own.</para>
        /// <paramref name="ownCounts"/>: how many of the pair are the game's own (0, 1 or 2). False where the record holds no pair for the country.
        /// </summary>
        public static bool TryHistory(CountryId id, Country played, DateTime? before, out double[] latest, out double[] previous, out int ownCounts,
            ElectionVintage vintage = ElectionVintage.Seated)
        {
            ownCounts = 0;
            List<(DateTime Day, double[] Shares)> own = played != null && played.Id == id ? OwnCounts(played, before) : null;
            if (own == null || own.Count == 0) { return TryHistory(id, out latest, out previous, vintage); }
            if (!TryHistory(id, out double[] recordLatest, out double[] recordPrevious)) { latest = null; previous = null; return false; }
            double scale = 0.0;
            foreach (double s in recordLatest) { scale += s; }
            var sequence = new List<double[]> { recordPrevious, recordLatest };
            foreach ((DateTime _, double[] shares) in own)
            {
                if (shares.Length != recordLatest.Length) { throw new InvalidOperationException($"{id}: a count of the game's own carries {shares.Length} parties against the roster's {recordLatest.Length}"); }
                var onScale = new double[shares.Length];
                for (int i = 0; i < shares.Length; i++) { onScale[i] = shares[i] * scale; }
                sequence.Add(onScale);
            }
            latest = sequence[sequence.Count - 1];
            previous = sequence[sequence.Count - 2];
            ownCounts = Math.Min(2, own.Count);
            return true;
        }

        /// <summary>
        /// PS-6 US-26: the game's own chamber counts in <paramref name="played"/> polled before <paramref name="before"/> (all of them where it is null), oldest
        /// first, each as shares of the roster's vote in <see cref="For"/>'s order (a party the count did not carry at zero). A country's counts are the national
        /// shares of each election record that counted (<see cref="Country.ElectionHistory"/>, its method not NotImplemented); the USA's chamber path counts
        /// nothing, and its own counts are the House the game elected (<see cref="Country.UsCongressElections"/>): REP at the contest's V and DEM at one less V
        /// (R-US15 (a): the poll is the House vote; V is the poll the count read, REP over REP and DEM). The presidential counts are not a chamber's: Poland's
        /// first vote reads the chamber's poll (which rolls), and the US electoral count's states roll by R-US24 (b) on their own.
        /// Oldest first BY DATE, whatever order the lists hold them in (a stable sort - two counts of one day keep the lists' order). A record with no date (the
        /// Editor's turn-labelled runs) is never before a given day, so an election never reads a count that may be its own; with no day given it is read, first.
        /// </summary>
        public static List<(DateTime Day, double[] Shares)> OwnCounts(Country played, DateTime? before)
        {
            List<(DateTime Day, double[] Shares)> listed = OwnCountsAsListed(played, before);
            var order = new List<int>(listed.Count);
            for (int i = 0; i < listed.Count; i++) { order.Add(i); }
            order.Sort((a, b) => { int byDay = listed[a].Day.CompareTo(listed[b].Day); return byDay != 0 ? byDay : a.CompareTo(b); });
            var counts = new List<(DateTime Day, double[] Shares)>(listed.Count);
            foreach (int i in order) { counts.Add(listed[i]); }
            return counts;
        }

        private static List<(DateTime Day, double[] Shares)> OwnCountsAsListed(Country played, DateTime? before)
        {
            var counts = new List<(DateTime, double[])>();
            if (played == null) { return counts; }
            IReadOnlyList<PoliticalParty> roster = For(played.Id);
            if (UsCongressElection.ElectsIn(played.Id))
            {
                if (played.UsCongressElections == null) { return counts; }
                foreach (UsCongressElection.Contest c in played.UsCongressElections)
                {
                    if (before.HasValue && c.PollingDay.Date >= before.Value.Date) { continue; }
                    if (double.IsNaN(c.V)) { continue; }
                    var v = new double[roster.Count];
                    for (int i = 0; i < roster.Count; i++) { v[i] = roster[i].Abbrev == "REP" ? c.V : roster[i].Abbrev == "DEM" ? 1.0 - c.V : 0.0; }
                    counts.Add((c.PollingDay.Date, v));
                }
                return counts;
            }
            if (played.ElectionHistory == null) { return counts; }
            foreach (ElectionRecord e in played.ElectionHistory)
            {
                if (e == null || e.Method == ElectionMethod.NotImplemented || e.Shares == null || e.Shares.Count == 0) { continue; }
                if (before.HasValue && (e.Date == DateTime.MinValue || e.Date.Date >= before.Value.Date)) { continue; }   // undated: never before a day
                var shares = new double[roster.Count];
                for (int i = 0; i < roster.Count; i++) { shares[i] = e.Shares.TryGetValue(roster[i].Abbrev, out double s) ? s : 0.0; }
                counts.Add((e.Date.Date, shares));
            }
            return counts;
        }

        private static bool TryRealHistory(CountryId id, out double[] latest, out double[] previous, ElectionVintage vintage)
        {
            vintage = Elections.WorldClock.Resolve(id, vintage);   // PS-1 (§618): the seated chamber's election, at the world's epoch
            switch (id)
            {
                case CountryId.Sweden when vintage == ElectionVintage.Sweden2018:
                    latest = null; previous = null; return false;   // 2018 against 2014 is not on disk; no start seats the 2018 Riksdag
                case CountryId.Germany when vintage == ElectionVintage.Germany2021:
                    // §696 (PS-4): the snap start's pair - a game opened on 6 Nov 2024 sits on the 2021 chamber, and its 2025 election reads 2021 as
                    // the prior and 2017->2021 as the loyalty. 2021 as the branch below derives it (the per-Land counts, land_votes_2021.csv);
                    // 2017 Die Bundeswahlleiterin's final Zweitstimmen shares (priors/previous_elections.md, bund-99.html), PUBLISHED TO ONE
                    // DECIMAL, all seven parties mapping CLEAN to 2021. In For(Germany) order: CDU, AfD, SPD, Grune, Linke, CSU, SSW, BSW, FDP.
                    // SSW did not contest 2017 and BSW did not exist - TRUE ZEROS, not missing figures.
                    latest   = new[] { 18.95, 10.39, 25.71, 14.72, 4.87, 5.19, 0.12, 0.00, 11.43 };
                    previous = new[] { 26.8, 12.6, 20.5, 8.9, 9.2, 6.2, 0.0, 0.0, 10.7 };
                    return true;
                case CountryId.Sweden when vintage == ElectionVintage.Sweden2022:
                    // 2022 and 2018 final shares, Valmyndigheten (returns_2022.md, priors/previous_elections.md),
                    // in For(Sweden) order: S, SD, M, V, C, KD, MP, L.
                    latest   = new[] { 30.33, 20.54, 19.10, 6.75, 6.71, 5.34, 5.08, 4.61 };
                    previous = new[] { 28.26, 17.53, 19.84, 8.00, 8.61, 6.32, 4.41, 5.49 };
                    return true;
                case CountryId.Sweden:
                    // 2026 final shares as RD_S.json prints them (Valmyndigheten's decision of 2026-09-19, Dnr VAL-735-2026;
                    // 2026/returns_2026.md) and 2022's, in For(Sweden) order: S, SD, M, V, C, KD, MP, L.
                    latest   = new[] { 28.02, 17.48, 19.85, 8.40, 7.03, 6.17, 6.12, 5.34 };
                    previous = new[] { 30.33, 20.54, 19.10, 6.75, 6.71, 5.34, 5.08, 4.61 };
                    return true;
                case CountryId.Germany:
                    // 2025 Zweitstimmen shares (returns_2025.md) and 2021 shares derived from the
                    // per-Land absolute counts on disk (land_votes_2021.csv, valid 46,298,387), in
                    // For(Germany) order: CDU, AfD, SPD, Grune, Linke, CSU, SSW, BSW, FDP.
                    // BSW is a TRUE ZERO in 2021 - the party did not exist - not a missing figure.
                    latest   = new[] { 22.60, 20.80, 16.40, 11.60, 8.80, 6.00, 0.20, 4.98, 4.30 };
                    previous = new[] { 18.95, 10.39, 25.71, 14.72, 4.87, 5.19, 0.12, 0.00, 11.43 };
                    return true;
                // §767 (Elias's ruling D1): POLAND'S LISTS CARRY OVER BY LINEAGE onto the 2023 roster's keys - KO <- PO, NL <- SLD (Lewica; 2015's
                // Zjednoczona Lewica), TD <- PSL, Konf <- KORWiN, PiS <- PiS; MN its own. Shares from `priors/previous_elections.md` (2019: the PKW notice
                // Dz.U. 2019 poz. 1955; 2015: the PKW's Wyniki_Sejm) and `poland/returns_2023.md`; the backtest's own mapping (ReBacktest). LOW CONFIDENCE,
                // on the record and in the reference view (HistoryNote): only PiS and MN carry over clean, and Polska 2050 has no earlier Sejm result, so
                // TD's history is PSL's alone - a known weakness, stated and not patched. The 2019 lists SLD and PSL carry nothing: their votes are NL's
                // and TD's lineage. For(Poland) order: PiS, KO, TD, NL, Konf, SLD, PSL, MN.
                case CountryId.Poland when vintage == ElectionVintage.Poland2019:
                    latest   = new[] { 43.59, 27.40, 8.55, 12.56, 6.81, 0.0, 0.0, 0.17 };   // 2019
                    previous = new[] { 37.58, 24.09, 5.13, 7.55, 4.76, 0.0, 0.0, 0.18 };    // 2015
                    return true;
                case CountryId.Poland:
                    latest   = new[] { 35.38, 30.70, 14.40, 8.61, 7.16, 0.0, 0.0, 0.12 };   // 2023
                    previous = new[] { 43.59, 27.40, 8.55, 12.56, 6.81, 0.0, 0.0, 0.17 };   // 2019
                    return true;
                case CountryId.USA:
                    return TryUsHouseHistory(vintage, out latest, out previous);   // PS-6 US-6: R-US15 (a), the House national vote - read from the catalog, never typed
                default:
                    latest = null; previous = null; return false;
            }
        }

        /// <summary>
        /// PS-6 US-6 (R-US15 (a), RULED - Elias's ruling G2, `COMPLETED.md` §797): THE US POLL IS THE HOUSE NATIONAL VOTE. A vintage's history is the House
        /// election whose chamber the vintage seats (T-1, the prior) and the House election before it (T-2; with T-1, the loyalty), each party's share of
        /// all votes cast - the Clerk of the House's recapitulation, its Total the denominator (US-5's basis, DECLARED at §788, RULED by G4) - summed from
        /// the generated catalog (<see cref="Elections.Generated.UsPresidentialReturns.House"/>), never typed (§419). A vintage elected after the catalog's
        /// last House election reads the latest the catalog holds (Usa2026 reads 2024 against 2022 - the latest sourced, as the chamber it seats is).
        /// In <see cref="For"/>'s order: REP, DEM. False for a vintage that is not the USA's or a year the catalog cannot pair.
        /// </summary>
        private static bool TryUsHouseHistory(ElectionVintage vintage, out double[] latest, out double[] previous)
        {
            latest = null; previous = null;
            if (!TryUsHistoryYears(vintage, out int t1, out int t2)) { return false; }
            latest = UsHouseShares(t1);
            previous = UsHouseShares(t2);
            return true;
        }

        /// <summary>
        /// PS-6 US-6: the House elections a US vintage's history reads - T-1, the latest the catalog holds on or before the vintage's election, and T-2,
        /// the one before it (see <see cref="TryUsHouseHistory"/>); false where the vintage is not the USA's or the catalog cannot pair it. Public so the
        /// instrument and the card print the years the poll reads.
        /// </summary>
        public static bool TryUsHistoryYears(ElectionVintage vintage, out int t1, out int t2)
        {
            t1 = t2 = 0;
            DateTime elected = Elections.WorldClock.ElectionDayOf(CountryId.USA, vintage);
            if (elected == DateTime.MinValue) { return false; }
            foreach (var h in Elections.Generated.UsPresidentialReturns.House) { if (h.Year <= elected.Year && h.Year > t1) { t1 = h.Year; } }
            foreach (var h in Elections.Generated.UsPresidentialReturns.House) { if (h.Year < t1 && h.Year > t2) { t2 = h.Year; } }
            return t1 > 0 && t2 > 0;
        }

        /// <summary>US-6: a House election's REP and DEM shares of all votes cast, from the catalog's rows for the year - US-5's sums, the same arithmetic
        /// in the same order, so the instrument's history and the poll's are one.</summary>
        private static double[] UsHouseShares(int year)
        {
            long r = 0, d = 0, t = 0;
            foreach (var h in Elections.Generated.UsPresidentialReturns.House) { if (h.Year == year) { r += h.VotesR; d += h.VotesD; t += h.VotesTotal; } }
            return new[] { (double)r / t, (double)d / t };
        }

        /// <summary>
        /// §767 (Elias's ruling D1): the note a country's vote history carries where its lists do not carry over clean - shown on the record and in the
        /// reference view (`WorldClock.Reference.HistoryNote`, the election night's reference), which draw it as LOW CONFIDENCE. Null where the history
        /// maps clean. PS-6 US-6: the USA's maps clean - REP and DEM on every ballot of the history - so it carries none; the basis its history is read
        /// on (R-US15's House vote) is stated by <see cref="HistoryBasis"/>, which no screen draws as a warning.
        /// </summary>
        public static string HistoryNote(CountryId id) => id == CountryId.Poland
            ? "LOW CONFIDENCE - Poland's lists changed between elections; the played vote reads them by lineage: KO from PO, NL from SLD, TD from PSL, "
              + "Konfederacja from KORWiN. Only PiS and the German minority carry over clean, and Polska 2050 has no earlier result - TD's history is PSL's alone."
            : null;

        /// <summary>
        /// PS-6 US-6 (the plan's "`PartySystems.HistoryNote` states the basis", read beside it, since <see cref="HistoryNote"/>'s readers draw LOW CONFIDENCE
        /// and the US history maps clean - DECLARED): the vote a country's poll is, where it is not the chamber's own list vote. The USA's (R-US15 (a),
        /// ruled): the House national vote, which US-6's card prints; null for the countries whose poll is their chamber's list vote.
        /// </summary>
        public static string HistoryBasis(CountryId id) => id == CountryId.USA
            ? "the House national vote: the prior is the House election whose chamber the world seats at its epoch, the loyalty that election against the one "
              + "before - each party's own ballot line, its share of all votes cast by the Clerk of the House's recapitulation; the presidential candidates carry it by "
              + "E1's factors, the electorate held at the four fitted countries' median spread"
            : null;
        /// <summary>
        /// W-G1: the electorate the vote model runs against, per country — mu_econ, mu_soc, sigma,
        /// tau — and its economic weight.
        ///
        /// **These are the project's own FITTED values**, taken from the backtest cases in
        /// `GateReRun` where they were fitted against each country's real result and their fit is
        /// reported as a mean absolute deviation (Sweden 1.47 pp over eight parties). They are not
        /// new numbers and not invented ones; they are the numbers the backtest already stands on,
        /// moved to where the live path can read them.
        ///
        /// ⚠ **THE FOUR BACKTESTED COUNTRIES' ARE FITTED; THE USA'S IS HELD; FRANCE HAS NONE.** France was never
        /// fitted - its system is two rounds in single-member seats and the backtest is a share model - so it
        /// returns false. PS-6 US-6: the USA's is R-US15's, ruled (G2, §797) - the House national vote as the
        /// poll, the spread held at the four countries' median (<see cref="UsSpreadFrom"/>, read here at run
        /// time, never typed), the mean on the DEM-REP segment where the seated history's last House election
        /// falls; solved, not fitted - with two parties a fit pins only the split (US-5, §789). The economic
        /// weight is <see cref="UsEconomicWeight"/>. ⚠ The USA's is the electorate of the seated vintage at the
        /// world's epoch - the same vintage <see cref="TryRealHistory"/> resolves - so a game from the USA's start
        /// holds its start's chamber's for its whole run: a chamber the record seats later moves the seats, not the
        /// history (the standing history gap, §767); the US card prints which. <see cref="UsElectorate"/> gives any vintage's.
        ///
        /// ⚠ **THE ELECTORATE DOES NOT YET MOVE WITH THE SIMULATION.** §8 couples it to the economy;
        /// nothing does that yet, so two elections in one game return the same chamber. That is a
        /// STANDING GAP, named here and in W-G1's records rather than hidden behind a jitter that
        /// would look like change without being it.
        /// </summary>
        public static bool TryElectorate(CountryId id, out VoteModel.Electorate electorate, out double economicWeight)
        {
            switch (id)
            {
                case CountryId.Sweden:
                    electorate = new VoteModel.Electorate(3.25, 6.25, 3.00, 0.50); economicWeight = 0.15; return true;
                case CountryId.Germany:
                    electorate = new VoteModel.Electorate(4.50, 6.50, 1.00, 16.00); economicWeight = 0.80; return true;
                case CountryId.Poland:
                    electorate = new VoteModel.Electorate(3.50, 7.00, 1.50, 8.00); economicWeight = 0.54; return true;
                case CountryId.Italy:
                    electorate = new VoteModel.Electorate(4.25, 7.00, 1.00, 4.00); economicWeight = 0.79; return true;
                case CountryId.USA:
                    economicWeight = UsEconomicWeight;
                    return UsElectorate(Elections.WorldClock.Resolve(id, ElectionVintage.Seated), out electorate, out double _);   // PS-6 US-6: R-US15, ruled
                default:
                    electorate = default; economicWeight = 0.0; return false;
            }
        }

        /// <summary>PS-6 US-6: the USA's economic weight - DECLARED from Gallup's most-important-problem table of July 2026 (economy and cost of living
        /// against immigration; `VoteShareBacktest`'s US case notes it), after every US election the model is held to. Moved from the Editor to here
        /// so the live electorate reads it; the Editor's name for it reads this one.</summary>
        public const double UsEconomicWeight = 0.65;

        /// <summary>PS-6 US-6 (R-US15 (ii), RULED - "the spread held at the four countries' median", G2, §797): the countries whose fitted σ and τ the
        /// USA's spread is the median of, each read from <see cref="TryElectorate"/> at run time. US-5's instrument holds its own list to this one.</summary>
        public static readonly CountryId[] UsSpreadFrom = { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy };

        /// <summary>The USA's electorate per vintage, solved once - a pure function of the catalog, the roster's positions, the four spreads and the weight.</summary>
        private static readonly Dictionary<ElectionVintage, (VoteModel.Electorate Electorate, double Place)> UsSolved = new Dictionary<ElectionVintage, (VoteModel.Electorate, double)>();

        /// <summary>
        /// PS-6 US-6 (R-US15 (a) with (ii), RULED): the US electorate a vintage's history gives - σ and τ each the median of <see cref="UsSpreadFrom"/>'s
        /// (the mean of the middle two of four), the mean on the segment from DEM's point to REP's (the roster's GPS positions) at the
        /// <paramref name="place"/> (0 at DEM, 1 at REP) where <see cref="VoteModel.PredictShares"/> gives REP the share of the two parties T-1 gave it,
        /// found by bisection - eighty halvings, the arithmetic `UsNationalVoteCheck`'s held fit runs, which the factor check holds equal to this.
        /// False for a vintage that is not the USA's. Solved once per vintage: the compass reads it every frame.
        /// </summary>
        public static bool UsElectorate(ElectionVintage vintage, out VoteModel.Electorate electorate, out double place)
        {
            lock (UsSolved)
            {
                if (UsSolved.TryGetValue(vintage, out var solved)) { electorate = solved.Electorate; place = solved.Place; return true; }
            }
            electorate = default; place = double.NaN;
            if (!TryUsHouseHistory(vintage, out double[] latest, out double[] _)) { return false; }
            var sigmas = new List<double>();
            var taus = new List<double>();
            foreach (CountryId from in UsSpreadFrom)
            {
                if (!TryElectorate(from, out VoteModel.Electorate e, out double _)) { return false; }
                sigmas.Add(e.Sigma);
                taus.Add(e.Tau);
            }
            PoliticalParty rep = UsaParties[0], dem = UsaParties[1];
            var points = new[] { new VoteModel.PartyPoint(rep.Abbrev, rep.LrEcon, rep.Galtan), new VoteModel.PartyPoint(dem.Abbrev, dem.LrEcon, dem.Galtan) };
            double target = latest[0] / (latest[0] + latest[1]);
            var spread = new VoteModel.Electorate(0.0, 0.0, Median(sigmas), Median(taus));
            double lo = 0.0, hi = 1.0;
            for (int i = 0; i < 80; i++) { double mid = 0.5 * (lo + hi); if (VoteModel.PredictShares(points, UsAlong(points, spread, mid), UsEconomicWeight)[0] < target) { lo = mid; } else { hi = mid; } }
            place = 0.5 * (lo + hi);
            electorate = UsAlong(points, spread, place);
            lock (UsSolved) { UsSolved[vintage] = (electorate, place); }
            return true;
        }

        /// <summary>US-6: <paramref name="spread"/>'s σ and τ, the mean at <paramref name="place"/> on the segment from DEM's point (points[1]) to REP's (points[0]).</summary>
        private static VoteModel.Electorate UsAlong(VoteModel.PartyPoint[] points, VoteModel.Electorate spread, double place)
            => new VoteModel.Electorate(points[1].Econ + place * (points[0].Econ - points[1].Econ), points[1].Soc + place * (points[0].Soc - points[1].Soc), spread.Sigma, spread.Tau);

        /// <summary>US-6: the median - the middle value, or the mean of the middle two.</summary>
        private static double Median(List<double> values)
        {
            values.Sort();
            return values.Count % 2 == 1 ? values[values.Count / 2] : 0.5 * (values[values.Count / 2 - 1] + values[values.Count / 2]);
        }
        /// <summary>The country's seat-holding units, in its key order - seat order at the election the order was set on (Sweden's is 2022's,
        /// kept by K-1 because every positional table is indexed by it); a view that needs seat order sorts by <see cref="PoliticalParty.SeedSeats"/>.</summary>
        public static IReadOnlyList<PoliticalParty> For(CountryId id)
        {
            // §671 (SP-3): the created parties of this game after the real ones - the very same array where none is registered
            PoliticalParty[] real = RealRoster(id);
            return CreatedParties.Extend(id, real) ?? real;
        }

        /// <summary>§671: the country's real parties, in its key order - what every sourced positional table is indexed by.</summary>
        public static PoliticalParty[] RealRoster(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden:  return SwedenParties;
                case CountryId.Germany: return GermanyParties;
                case CountryId.France:  return FranceParties;
                case CountryId.Italy:   return ItalyParties;
                case CountryId.Poland:  return PolandParties;
                case CountryId.USA:     return UsaParties;
                default: return System.Array.Empty<PoliticalParty>();
            }
        }

        /// <summary>
        /// The seed composition, keyed by the abbreviation the country's own authority uses.
        /// **This dictionary is the PERSISTED shape** that replaces `Dictionary&lt;PartyArchetype, int&gt;`,
        /// which is why W-G1 bumps `SaveVersion`.
        /// </summary>
        public static Dictionary<string, int> InitialSeats(CountryId id) => InitialSeats(id, ElectionVintage.Seated);

        /// <summary>PS-1 (§618): the composition of the chamber a vintage's lists elected, keyed by the roster's abbreviations; `Seated` resolves to the
        /// chamber of record at the world's epoch (`WorldClock.SeatedVintage`). A roster party the election did not seat holds zero.</summary>
        public static Dictionary<string, int> InitialSeats(CountryId id, ElectionVintage vintage)
        {
            ElectionVintage concrete = Elections.WorldClock.Resolve(id, vintage);
            var seats = new Dictionary<string, int>();
            foreach (PoliticalParty p in For(id)) { seats[p.Abbrev] = 0; }
            IReadOnlyList<(string Abbrev, int Seats)> table = SeatsAt(concrete);
            if (table == null)
            {
                foreach (PoliticalParty p in For(id)) { seats[p.Abbrev] = p.SeedSeats; }   // the latest election, the roster's own figures
                return seats;
            }

            foreach ((string abbrev, int n) in table)
            {
                if (!seats.ContainsKey(abbrev)) { throw new System.InvalidOperationException($"{concrete}'s table seats '{abbrev}', which {id}'s roster does not hold"); }
                seats[abbrev] = n;
            }

            return seats;
        }

        /// <summary>True when a vintage's seat table is on disk and in <see cref="SeatsAt"/>. SEAT-1 (Elias's ruling J2, §869) retired E-47's two: Italy's 2018 Camera
        /// is seated by its groups at first constitution (DECLARED - <see cref="SeatingDeclaration"/>), France's 2022 Assembly by the Ministry's nuances (its FE page).</summary>
        public static bool SeatsSourced(ElectionVintage vintage)
        {
            switch (vintage)
            {
                case ElectionVintage.Usa2026:
                    return false;   // PS-6 US-2: elected after the record's date - not on record; the latest sourced table stands (`WorldClock.SeatedVintage`)
                case ElectionVintage.Seated:
                    return false;   // not a table: resolve it first
                default:
                    return true;
            }
        }

        /// <summary>
        /// PS-1 (§618): THE SEAT TABLES BY ELECTION, each a chamber as elected - its lists - from the country's returns file or its
        /// `records_by_date.md` (the source ids in brackets are that record's register). The latest election's table is the roster's
        /// own `SeedSeats` and is listed here too, so every vintage reads through one path. Null for a chamber whose per-list table
        /// is not on disk: the caller seats the latest sourced one and says so (SEAT-1 retired E-47's two; the USA's 120th, elected after the record's date, remains).
        /// </summary>
        public static IReadOnlyList<(string Abbrev, int Seats)> SeatsAt(ElectionVintage vintage)
        {
            switch (vintage)
            {
                // Sweden - Valmyndigheten. 2018: [VAL-18] (sweden/records_by_date.md §1), 349. 2022: returns_2022.md. 2026: 2026/returns_2026.md.
                case ElectionVintage.Sweden2018: return new[] { ("S", 100), ("M", 70), ("SD", 62), ("V", 28), ("C", 31), ("KD", 22), ("MP", 16), ("L", 20) };
                case ElectionVintage.Sweden2022: return new[] { ("S", 107), ("SD", 73), ("M", 68), ("V", 24), ("C", 24), ("KD", 19), ("MP", 18), ("L", 16) };
                case ElectionVintage.Sweden2026: return Roster(CountryId.Sweden);
                // Germany - the Bundeswahlleiterin. 2021: the 26 Sep 2021 determination, 736 (germany/records_by_date.md §1.1; FDP 92 is DERIVED there: 91 + the seat
                // [BT-WW24] says it lost at the 2024-03-01 re-determination). 2025: returns_2025.md, 630.
                case ElectionVintage.Germany2021: return new[] { ("SPD", 206), ("CDU", 152), ("Grune", 118), ("FDP", 92), ("AfD", 83), ("CSU", 45), ("Linke", 39), ("SSW", 1), ("BSW", 0) };
                case ElectionVintage.Germany2025: return Roster(CountryId.Germany);
                // Poland - the PKW. 2019: Dz.U. 2019 poz. 1955 [PKW-2019] (poland/records_by_date.md §1): the 2019 committees' own keys - SLD, PSL, MN. 2023: returns_2023.md.
                case ElectionVintage.Poland2019: return new[] { ("PiS", 235), ("KO", 134), ("SLD", 49), ("PSL", 30), ("Konf", 11), ("MN", 1), ("NL", 0), ("TD", 0) };
                case ElectionVintage.Poland2023: return Roster(CountryId.Poland);
                // Italy - 2022: returns_2022.md (its per-list totals carry the file's own flag). 2018: SEAT-1 (J2) - by its groups at first constitution, DECLARED (`ItalyCamera2018`).
                case ElectionVintage.Italy2022: return Roster(CountryId.Italy);
                case ElectionVintage.Italy2018: return ItalyCamera2018;
                // USA - history.house.gov's party divisions, election-day figures ([HH-DIV]; usa/records_by_date.md §1 - not opening-day sworn counts).
                // ⚠ The 117th's figures sum to 434 of 435: the source's own footnote 6 ([HH-DIV], in the saved page) says New York had not certified
                // the 22nd district before the 117th opened, so the seat stays unassigned rather than given to a party (ChamberSizeAt says 434).
                case ElectionVintage.Usa2020: return new[] { ("DEM", 222), ("REP", 212) };
                case ElectionVintage.Usa2022: return new[] { ("REP", 222), ("DEM", 213) };
                case ElectionVintage.Usa2024: return Roster(CountryId.USA);
                // France - 2024: returns_2024.md (the Ministry's nuances). 2022: SEAT-1 (E-47 (b)) - the Ministry's FE page by nuance (`FranceAssembly2022`).
                case ElectionVintage.France2024: return Roster(CountryId.France);
                case ElectionVintage.France2022: return FranceAssembly2022;
                default: return null;
            }
        }

        /// <summary>The seats a vintage's table accounts for - the chamber's size, except where the record's own figures leave seats unassigned
        /// (the 117th House: 434 of 435 at election day) or the chamber was sized differently (the 20th Bundestag: 736 at its determination).</summary>
        public static int ChamberSizeAt(CountryId id, ElectionVintage vintage)
        {
            switch (vintage)
            {
                case ElectionVintage.Germany2021: return 736;
                case ElectionVintage.Usa2020: return 434;
                case ElectionVintage.Italy2018: return ItalyCamera2018Size;   // SEAT-1: the XVIII Camera, before the 2020 reform's 400
                default: return ChamberSeats(id);
            }
        }

        // =============================================================================================================================================
        // SEAT-1 (Elias's ruling J2, 2026-10-19, §869): THE CHAMBERS OF RECORD THAT NO LIST TABLE SEATS - Italy's XVIII Camera and XIX Senato by their
        // groups at first constitution, DECLARED, and France's XVIe Assembly by the Ministry's nuances (E-47 (b)). Every figure below is re-derived from
        // the stored pages by `ItalySeatingDiagnostic`, which fails while one of them differs.
        // =============================================================================================================================================

        /// <summary>SEAT-1 [SOURCED]: the XVIII Camera's size - 630, every deputy in a group on 27 Mar 2018 (`italy/raw/reseat_2022/dati_camera_leg18_adesioni_gruppi.csv`)
        /// and camera.it's own total for the legislature (`italy/raw/records/camera_leg18_46_gruppi.html`, "Totale 630"). <see cref="ChamberSeats"/>' 400 is the
        /// 2020 reform's, first applied in 2022.</summary>
        public const int ItalyCamera2018Size = 630;

        /// <summary>SEAT-1: the day the XVIII Camera is read at - DECLARED, 20 April 2018, the day the last of the first constitution's mixed-group components
        /// (MAIE-Sogno Italia) was formed (its page in `italy/raw/camera2018/`). The groups were declared on 27 March 2018, Liberi e Uguali's on 10 April; the
        /// table reads the same on every day from 20 April to 7 May 2018 (Fedriga left the Lega group on 8 May, and Sangregorio joined Noi con l'Italia).</summary>
        public static readonly System.DateTime ItalyCamera2018Read = new System.DateTime(2018, 4, 20);

        /// <summary>
        /// SEAT-1 (Elias's ruling J2 on E-46 and E-47 (a): "seat both chambers by their groups at first constitution (the Senato's of 18 Oct 2022, the
        /// Camera's of April 2018), DECLARED as a deviation, since no official source assigns the single-seat winners to lists. Where the chamber's page
        /// names the mixed group's components, seat each component as its list"): <b>THE XVIII CAMERA</b>, 630 deputies, as its groups and the mixed group's
        /// components stood on <see cref="ItalyCamera2018Read"/> [SOURCED]. The groups are the Camera's open data (`italy/raw/reseat_2022/dati_camera_leg18_adesioni_gruppi.csv`,
        /// each deputy's adhesion with its dates); the components are the Camera's historical portal (`italy/raw/camera2018/`: the MISTO group's page names them,
        /// and each component's page dates its members). J2's "seat each component as its list", READ (DECLARED): a group or component sits as the roster's list
        /// whose deputies it gathers or whose name it bears - the five groups; Minoranze linguistiche (Gebhard, Plangger, Rossini, Schullian - the SVP-PATT list's
        /// deputies) as SVP; "+Europa-Centro Democratico" (Magi, Fusacchia, Tabacci) as PlusE; "MAIE-Movimento Associativo Italiani all'Estero-Sogno Italia" (six
        /// deputies) as MAIE. The others are units of their own (the roster's SEAT-1 rows; Noi con l'Italia's lineage to the 2022 Noi Moderati list is asked, not
        /// read by name). One mixed-group member sat in no component that day (Sangregorio): Misto.
        /// </summary>
        public static readonly (string Abbrev, int Seats)[] ItalyCamera2018 =
        {
            ("M5S", 222), ("Lega", 125), ("PD", 111), ("FI", 105), ("FdI", 32), ("LeU", 14),
            ("MAIE", 6), ("SVP", 4), ("CP", 4), ("PlusE", 3), ("NcI", 3), ("Misto", 1),
        };

        /// <summary>SEAT-1 [SOURCED]: the XIX Senato's elected seats - 200 (Constitution art. 57, as amended in 2020), and 200 mandates "elettivo" from
        /// 13 Oct 2022 in the Senato's open data (`italy/raw/reseat_2022/dati_senato_leg19_mandati.csv`).</summary>
        public const int ItalySenato2022Elected = 200;

        /// <summary>SEAT-1 [SOURCED]: the senators for life sitting on 18 Oct 2022 - six (the same file: one "di diritto e a vita", five "a vita, di nomina del
        /// Presidente della Repubblica"). Five were in a group that day (the groups page's 205 is 200 + 5); none is seated: the chamber of record is seated as
        /// ELECTED (CLAUDE.md's world clock), and no list elected them.</summary>
        public const int ItalySenato2022ForLife = 6;

        /// <summary>
        /// SEAT-1 (Elias's ruling J2 on E-46): <b>THE XIX SENATO BY ITS GROUPS AT FIRST CONSTITUTION, 18 OCT 2022</b> [SOURCED] - the groups page as the Internet
        /// Archive captured it that day (`italy/raw/senato2022/wayback_senato_grp_20221018194007.html`, its "Composizione dei Gruppi parlamentari", each group's
        /// total with the senators for life), split into elected and for-life by the Senato's open data (`italy/raw/reseat_2022/dati_senato_leg19_adesioni_gruppi.csv`
        /// and `dati_senato_leg19_mandati.csv`). Each row: the group's key, its name on the page, the 2022 list it sits as (a group bearing a list's name, as the
        /// Camera's - DECLARED), null for a group that is no list's, its elected senators and its senators for life. The page names no component of the Misto
        /// group, so the Misto sits whole (J2: components only "where the chamber's page names" them). <b>Nothing seats it yet</b>: no Italian start falls inside the
        /// XIX, nothing seats Italy's record by its dates, and the Senato is modelled at PS-7, which reads this table.
        /// </summary>
        public static readonly (string Group, string Name, string List, int Elected, int ForLife)[] ItalySenato2022 =
        {
            ("FdI",   "Fratelli d'Italia",                                                                                  "FdI",  63, 0),
            ("PD",    "Partito Democratico - Italia Democratica e Progressista",                                            "PD",   38, 0),
            ("Lega",  "Lega Salvini Premier - Partito Sardo d'Azione",                                                       "Lega", 29, 0),
            ("M5S",   "MoVimento 5 Stelle",                                                                                 "M5S",  28, 0),
            ("FI",    "Forza Italia Berlusconi Presidente",                                                                 "FI",   18, 0),
            ("AzIV",  "Azione-ItaliaViva-RenewEurope",                                                                      "AzIV",  9, 0),
            ("CdI",   "Civici d'Italia - Noi Moderati (UDC - Coraggio Italia - Noi con l'Italia - Italia al Centro) - MAIE", null,    6, 0),
            ("Aut",   "Per le Autonomie (SVP-Patt, Campobase, Sud Chiama Nord)",                                            null,    5, 2),
            ("Misto", "Misto",                                                                                              null,    4, 3),
        };

        /// <summary>SEAT-1: a chamber of record's second chamber by its groups - Italy's XIX Senato (<see cref="ItalySenato2022"/>); null for every other vintage.</summary>
        public static IReadOnlyList<(string Group, string Name, string List, int Elected, int ForLife)> SecondChamberGroupsAt(ElectionVintage vintage) =>
            vintage == ElectionVintage.Italy2022 ? ItalySenato2022 : null;

        /// <summary>
        /// SEAT-1 (Elias's ruling J2 on E-47 (b): "France's FE page is fetched ... Reading it into the record is Code's work"): <b>THE XVIe ASSEMBLY BY THE
        /// MINISTRY'S NUANCES</b> [SOURCED] - the Interior Ministry's "France entière" page of the 2022 legislative elections, its seats ("Nb Sieges") by
        /// nuance after both rounds (`france/raw/legislatives2022/wayback_interieur_FE_20220705224641.html.gz`, the Internet Archive's raw capture of 5 July
        /// 2022, the stored gzip body held to its SHA256SUMS). 577. The page's five nuances that seated nobody (extreme left, the PRG, the Écologistes,
        /// Reconquête, extreme right) are left out; ECO is a roster unit and holds zero. A 2022 nuance with a 2024 nuance's label is that key (DECLARED);
        /// NUP and DSV are the 2022 nuances' own.
        /// </summary>
        public static readonly (string Abbrev, int Seats)[] FranceAssembly2022 =
        {
            ("ENS", 245), ("NUP", 131), ("RN", 89), ("LR", 61), ("DVG", 22), ("DVD", 10), ("REG", 10), ("DVC", 4), ("UDI", 3), ("DIV", 1), ("DSV", 1),
        };

        /// <summary>SEAT-1 (J2: "DECLARED as a deviation"): the declared reading a vintage's table carries, in the brief's words - Italy's XVIII Camera by its
        /// groups; null where the table is the election's own lists.</summary>
        public static string SeatingDeclaration(ElectionVintage vintage) => vintage == ElectionVintage.Italy2018
            ? "it is seated by its parliamentary groups as first constituted, on 20 April 2018, and each mixed-group component the Camera's page names is seated as its list"
              + " - DECLARED, since no official source assigns its single-seat winners to lists; a component's list is read as the one whose deputies it gathers or whose name it bears"
              + " (Minoranze linguistiche as SVP-PATT), and a component that is neither sits as its own"
            : null;

        /// <summary>SEAT-1 (the review's L1): the declared reading a country's positions carry, in the brief's words - Italy's AVS at SI's and EV's equal-weight
        /// mean (J2 on E-48), whose runtime point the vote model's fit did not use; null elsewhere. Said, not reconciled: the backtests keep their typed point.</summary>
        public static string PositionDeclaration(CountryId id) => id == CountryId.Italy
            ? "AVS stands at the equal-weight mean of SI's and EV's positions (lrecon 1.795, galtan 1.70) - DECLARED, no split of its seats being on record; Italy's vote model was fitted on the typed 1.80 and 1.70, the same mean rounded"
            : null;

        /// <summary>SEAT-1 (the review's L3): a key that is a group's remainder, not a party or a list - the XVIII Camera's mixed-group members in no component -
        /// which the brief counts apart from the parties.</summary>
        public static bool IsGroupRemainder(CountryId id, string abbrev) => id == CountryId.Italy && abbrev == "Misto";

        /// <summary>SEAT-1: the same, as the start card's caption carries it - one clause after the chamber's size; null where none is declared.</summary>
        public static string SeatingTag(ElectionVintage vintage) => vintage == ElectionVintage.Italy2018 ? "BY ITS GROUPS OF 20 APR 2018" : null;

        /// <summary>SEAT-1 (the review's M2): the size the seated chamber declares on a date - keyed on the vintage SEATED, never on the seats it is checked
        /// against: once the game has held an election of its own in the country, the chamber's statutory size (<see cref="ChamberSeats"/>); before it, the
        /// size of record of the chamber the record seats - by its own dates where the record seats the chamber by date (the USA's House), else the one
        /// seated at the world's start, which holds until the game counts (Italy's XVIII Camera, 630, past the XIX's first sitting; the 20th Bundestag's
        /// 736). The views compare the seats drawn with it, so a chamber seated at a size its vintage does not carry fails the film's guard.</summary>
        public static int DeclaredSize(Country country, System.DateTime date)
        {
            if (country == null) { return 0; }
            if (Elections.SeatedGovernment.HeldElection(country)) { return ChamberSeats(country.Id); }
            System.DateTime seatedOn = Elections.WorldClock.RecordSeatsChamber(country.Id) ? date : Simulation.SimulationManager.EpochDate;
            return ChamberSizeAt(country.Id, Elections.WorldClock.SeatedVintage(country.Id, seatedOn));
        }

        private static IReadOnlyList<(string Abbrev, int Seats)> Roster(CountryId id)
        {
            var list = new List<(string, int)>();
            foreach (PoliticalParty p in For(id)) { list.Add((p.Abbrev, p.SeedSeats)); }
            return list;
        }

        /// <summary>
        /// PS-1 (§618, ruled): WHO IS PLAYABLE - a party seated in the chamber of record at the start, a party that won seats at the latest election,
        /// or any party that contested the latest election and holds a sourced CHES position (Germany's BSW at its 2024 start). Never narrower than the roster
        /// was before: a seated unit without a position (Poland's TD, France's nuances) stays playable by its seats.
        /// </summary>
        public static bool IsPlayable(Country country, in PoliticalParty party) => IsPlayable(country?.ParliamentSeats, party);

        /// <summary>The same rule against a seat table - the picker's, which reads the chamber at the country's START rather than the selector's world.</summary>
        public static bool IsPlayable(Dictionary<string, int> seated, in PoliticalParty party)
        {
            if (seated != null && seated.TryGetValue(party.Abbrev, out int held) && held > 0) { return true; }
            return party.SeedSeats > 0 || party.HasPosition;
        }

        /// <summary>
        /// DERIVED, and this is the whole derivation: -1 (favours lower taxes) to +1 (favours higher
        /// taxes) — the axis `ParliamentSystem` scores a budget bill against — read off CHES
        /// `lrecon` as `(5 - lrecon) / 5`. lrecon 0 (most state) gives +1, lrecon 10 (most market)
        /// gives -1, lrecon 5 gives 0.
        ///
        /// **It replaces four hand-set `FiscalStance` constants** that `PartyArchetype.cs` itself
        /// called "gameplay-tuning placeholders, not researched figures". The mapping is linear and
        /// stated so a reader can check it against the source in one step; no party is nudged.
        ///
        /// ⚠ **A unit with no published position returns 0 AND `HasPosition` is false.** The caller
        /// must decide what an unmeasured unit does rather than being handed a centrist by default —
        /// §36: absence is not the middle. Every caller here checks `HasPosition` first.
        /// </summary>
        public static float FiscalStance(in PoliticalParty party)
        {
            return party.HasPosition ? (5f - party.LrEcon) / 5f : 0f;
        }

        /// <summary>
        /// C-B3 / R-CL2 — the OPENNESS axis, in exactly the form <see cref="FiscalStance"/> takes and
        /// for the same reason: −1 (favours open trade, so opposes a tariff rise) to +1 (favours
        /// protection, so supports one).
        ///
        /// **The derivation, stated so it can be checked in one step.** CHES `eu_position` runs **1–7**
        /// where the other axes run 0–10, so it is first put on the common scale by
        /// `CoalitionCompatibility.RescaleEu` — **the same function §29's compatibility already uses,
        /// not a second copy of it** — and then read as `(5 − rescaled) / 5`. A strongly
        /// pro-integration party (7 → rescaled 10) gives −1; a strongly eurosceptic one (1 → 0) gives
        /// +1; the midpoint gives 0.
        ///
        /// ⚠ **What this asserts, adopted by ruling rather than quietly:** that a party's stance on
        /// European integration stands in for its stance on trade openness. CHES publishes no trade or
        /// protectionism item, so nothing on disk measures trade directly. See
        /// <see cref="PoliticalParty.EuPosition"/> for the full statement of the stretch.
        ///
        /// ⚠ **A unit with no published EU position returns 0 AND `HasEuPosition` is false** — the same
        /// contract `FiscalStance` has, so a caller must decide what an unmeasured unit does rather
        /// than be handed a centrist. **Every US party is such a unit.**
        /// </summary>
        public static float TradeStance(in PoliticalParty party)
        {
            if (!party.HasEuPosition) { return 0f; }
            double rescaled = CoalitionCompatibility.RescaleEu(party.EuPosition);
            return (float)((5.0 - rescaled) / 5.0);
        }

        /// <summary>
        /// Every seat-holding unit across all six countries, flattened.
        ///
        /// ⚠ **The name and shape of this method are `PartyMarkCoverageCheck`'s, not ours.** That
        /// check discovers party seeds by reflection, looking for a static parameterless
        /// `BuildParties()`, precisely so it does not become a build dependency of a moving type and
        /// so a new country's seed is covered the day it lands with no edit there. Until W-G1 it
        /// found none, printed "PARTY SYSTEM NOT PRESENT — VERIFIED NOTHING" and exited 0.
        /// **It now does real accounting**, which is the verification obligation the D0 collision
        /// map (R3) attached to this wiring: 53 units enumerated, 1 mark delivered, 52 gaps.
        /// </summary>
        public static List<PoliticalParty> BuildParties()
        {
            var all = new List<PoliticalParty>();
            foreach (CountryId id in System.Enum.GetValues(typeof(CountryId)))
            {
                all.AddRange(For(id));
            }

            return all;
        }
    }
}
