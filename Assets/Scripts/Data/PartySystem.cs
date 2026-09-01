using System.Collections.Generic;
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
        /// <summary>The abbreviation the country's own authority uses. The persisted key.</summary>
        public readonly string Abbrev;
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
        /// <para><b>Name and office only.</b> `party_leaders_2022.md` states the rule this follows:
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


        public PoliticalParty(string abbrev, string name, float lrEcon, float galtan, int seedSeats,
            string markName = null, float euPosition = float.NaN, PartyLeader[] leaders = null,
            float lrGen = float.NaN)
        {
            Abbrev = abbrev;
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
        /// C-D3: the leaders each party had **on 2022-09-11**, the election this prototype replays.
        ///
        /// <para><b>SOURCED</b> — every name and office is `ElectionsData/sweden/party_leaders_2022.md`,
        /// which takes each from the party's OWN website as it stood within days of the election, through
        /// the Internet Archive so the citation carries an exact capture timestamp. **A party is the
        /// authority on who leads it.**</para>
        ///
        /// <para>⚠ <b>The vintage is 2022 and is deliberately not current.</b> C, L, S, MP and V have all
        /// changed leader since. A "current leaders" set is a different item with a different vintage, and
        /// mixing the two would be exactly the basis-mixing the cross-check gate forbids.</para>
        ///
        /// <para>⚠ <b>MP carries TWO, and that is the whole point of the item.</b> Both are named; neither
        /// is seated in a debate. See <see cref="PoliticalParty.ResolveDebateSeat"/>.</para>
        /// </summary>
        private static PartyLeader[] One(string name, string office) => new[] { new PartyLeader(name, office) };

        // ---- Sweden: Riksdag 2022. Units are PARTIES; every one has a CHES position. Sums to 349.
        private static readonly PoliticalParty[] SwedenParties =
        {
            new PoliticalParty("S",  "Arbetarepartiet-Socialdemokraterna", 3.68f, 4.74f, 107, "mark_party_se_s", euPosition: 5.74f, lrGen: 3.74f,
                leaders: One("Magdalena Andersson", "partiordforande")),
            new PoliticalParty("SD", "Sverigedemokraterna",                6.32f, 9.00f,  73, euPosition: 2.68f, lrGen: 8.53f,
                leaders: One("Jimmie Akesson", "partiledare")),
            new PoliticalParty("M",  "Moderaterna",                        7.89f, 6.47f,  68, euPosition: 5.74f, lrGen: 7.58f,
                leaders: One("Ulf Kristersson", "partiledare")),
            new PoliticalParty("V",  "Vansterpartiet",                     1.89f, 2.42f,  24, "mark_party_se_v", euPosition: 3.32f, lrGen: 1.58f,
                leaders: One("Nooshi Dadgostar", "partiledare")),
            new PoliticalParty("C",  "Centerpartiet",                      7.84f, 2.95f,  24, euPosition: 6.11f, lrGen: 5.95f,
                leaders: One("Annie Loof", "partiledare")),
            new PoliticalParty("KD", "Kristdemokraterna",                  7.26f, 7.79f,  19, euPosition: 5.35f, lrGen: 8.00f,
                leaders: One("Ebba Busch", "partiledare")),
            // ⚠ TWO leaders, carried as two. Taking "the first" would drop Per Bolund, a real named
            // person, which the ruling forbids outright.
            new PoliticalParty("MP", "Miljopartiet de grona",              3.16f, 1.95f,  18, euPosition: 5.32f, lrGen: 2.74f,
                leaders: new[] { new PartyLeader("Marta Stenevi", "sprakror"), new PartyLeader("Per Bolund", "sprakror") }),
            new PoliticalParty("L",  "Liberalerna",                        7.32f, 4.47f,  16, euPosition: 6.84f, lrGen: 6.74f,
                leaders: One("Johan Pehrson", "partiledare")),
        };

        // ---- Germany: Bundestag 2025. Units are PARTIES; every one has a CHES position. Sums to 630.
        // BSW (4.98 %) and FDP (4.3 %) missed the threshold and hold NO seats - carried at zero
        // rather than omitted, because a party that just missed is part of the system the player
        // is looking at, and dropping it would hide the closest thing to a threshold story there is.
        private static readonly PoliticalParty[] GermanyParties =
        {
            new PoliticalParty("CDU",   "Christlich Demokratische Union",   6.58f, 6.56f, 164, euPosition: 6.42f, lrGen: 6.58f),
            new PoliticalParty("AfD",   "Alternative fur Deutschland",      7.63f, 9.39f, 152, euPosition: 1.89f, lrGen: 9.26f),
            new PoliticalParty("SPD",   "Sozialdemokratische Partei",       3.47f, 3.61f, 120, euPosition: 6.37f, lrGen: 3.47f),
            new PoliticalParty("Grune", "Bundnis 90/Die Grunen",            3.37f, 1.61f,  85, euPosition: 6.79f, lrGen: 3.06f),
            new PoliticalParty("Linke", "Die Linke",                        1.37f, 2.29f,  64, euPosition: 3.72f, lrGen: 1.42f),
            new PoliticalParty("CSU",   "Christlich-Soziale Union",         6.77f, 7.54f,  44, euPosition: 5.50f, lrGen: 7.57f),
            new PoliticalParty("SSW",   "Sudschleswigscher Wahlerverband",  float.NaN, float.NaN, 1),
            new PoliticalParty("BSW",   "Bundnis Sahra Wagenknecht",        2.78f, 7.06f,   0, euPosition: 2.42f, lrGen: 3.63f),
            new PoliticalParty("FDP",   "Freie Demokratische Partei",       7.58f, 3.22f,   0, euPosition: 5.84f, lrGen: 6.06f),
        };

        // ---- Poland: Sejm 2023. Units are the ELECTORAL COMMITTEES the PKW reports. Sums to 460.
        // TD is the Trzecia Droga committee (Polska 2050 + PSL); CHES scores its two components
        // separately, so the COMMITTEE carries no single position - absence, not an average of two
        // parties that ran together but are not one party.
        private static readonly PoliticalParty[] PolandParties =
        {
            new PoliticalParty("PiS",  "Prawo i Sprawiedliwosc",   2.52f, 8.45f, 194, euPosition: 3.10f, lrGen: 7.64f),
            new PoliticalParty("KO",   "Koalicja Obywatelska",     6.17f, 3.66f, 157, euPosition: 6.63f, lrGen: 4.93f),
            new PoliticalParty("TD",   "Trzecia Droga",            float.NaN, float.NaN, 65),
            new PoliticalParty("NL",   "Nowa Lewica",              2.32f, 1.75f,  26, euPosition: 6.90f, lrGen: 2.41f),
            new PoliticalParty("Konf", "Konfederacja",             8.96f, 8.41f,  18, euPosition: 1.52f, lrGen: 9.39f),
        };

        // ---- France: Assemblee nationale 2024. UNITS ARE THE INTERIOR MINISTRY'S NUANCES, not parties,
        // because that is what the Ministry reports seats for and the whole NFP left ran as ONE joint
        // candidacy (UG, 178) that the source DOES NOT SPLIT into LFI / PS / EELV. Attaching CHES
        // positions to LFI, PS and EELV here would be inventing a seat split nobody published.
        // The nuances that ARE a single party carry that party's CHES position; the rest carry NONE.
        // Sums to 577.
        private static readonly PoliticalParty[] FranceParties =
        {
            new PoliticalParty("UG",  "Union de la gauche (NFP joint candidacies)", float.NaN, float.NaN, 178),
            new PoliticalParty("ENS", "Ensemble (majorite presidentielle)",          6.18f, 4.09f, 150, euPosition: 6.27f, lrGen: 6.27f),
            new PoliticalParty("RN",  "Rassemblement National",                      6.00f, 8.36f, 125, euPosition: 2.18f, lrGen: 8.82f),
            new PoliticalParty("LR",  "Les Republicains",                            7.82f, 7.18f,  39, euPosition: 5.27f, lrGen: 7.73f),
            new PoliticalParty("DVD", "Divers droite",                               float.NaN, float.NaN, 27),
            new PoliticalParty("UXD", "Union de l'extreme droite (RN-Ciotti)",       float.NaN, float.NaN, 17),
            new PoliticalParty("DVG", "Divers gauche",                               float.NaN, float.NaN, 12),
            new PoliticalParty("REG", "Regionaliste",                                float.NaN, float.NaN,  9),
            new PoliticalParty("HOR", "Horizons",                                    float.NaN, float.NaN,  6),
            new PoliticalParty("DVC", "Divers centre",                               float.NaN, float.NaN,  6),
            new PoliticalParty("UDI", "Union des democrates et independants",        float.NaN, float.NaN,  3),
            new PoliticalParty("SOC", "Parti socialiste (outside the UG banner)",    3.36f, 2.73f,   2, euPosition: 6.27f, lrGen: 3.45f),
            new PoliticalParty("ECO", "Ecologistes",                                 2.30f, 1.70f,   1, euPosition: 6.20f, lrGen: 2.30f),
            new PoliticalParty("DIV", "Divers",                                      float.NaN, float.NaN,  1),
            new PoliticalParty("EXD", "Extreme droite",                              float.NaN, float.NaN,  1),
        };

        // ---- Italy: Camera dei deputati 2022. Units are the LISTS Eligendo reports. Sums to 400.
        // EVERY per-list seat total in the returns file on disk is [UNCONFIRMED] - Italy elects on a
        // mixed system and the uninominal seats are attributed to lists differently by different
        // sources. THE FLAG IS CARRIED, not dropped: this is the one chamber here whose composition
        // the project has not confirmed against a primary total.
        private static readonly PoliticalParty[] ItalyParties =
        {
            new PoliticalParty("FdI",   "Fratelli d'Italia",              6.40f, 9.13f, 119, euPosition: 3.27f, lrGen: 8.47f),
            new PoliticalParty("PD",    "Partito Democratico",             2.93f, 2.33f,  69, euPosition: 6.80f, lrGen: 2.93f),
            new PoliticalParty("Lega",  "Lega per Salvini Premier",        6.80f, 8.87f,  66, euPosition: 1.60f, lrGen: 8.67f),
            new PoliticalParty("M5S",   "Movimento 5 Stelle",              2.87f, 3.27f,  52, euPosition: 4.07f, lrGen: 3.33f),
            new PoliticalParty("FI",    "Forza Italia",                    7.40f, 6.07f,  45, euPosition: 5.33f, lrGen: 6.73f),
            new PoliticalParty("AzIV",  "Azione - Italia Viva",            5.21f, 3.46f,  21, euPosition: 6.79f, lrGen: 4.43f),
            new PoliticalParty("AVS",   "Alleanza Verdi e Sinistra",       float.NaN, float.NaN, 12),
            new PoliticalParty("NM",    "Noi Moderati",                    float.NaN, float.NaN,  7),
            new PoliticalParty("SVP",   "Sudtiroler Volkspartei - PATT",   float.NaN, float.NaN,  3),
            new PoliticalParty("PlusE", "Piu Europa",                      float.NaN, float.NaN,  2),
            new PoliticalParty("IC",    "Impegno Civico",                  float.NaN, float.NaN,  1),
            new PoliticalParty("ScN",   "Sud chiama Nord",                 float.NaN, float.NaN,  1),
            // The last two seats are OUTSIDE the Area Italia basis every row above uses, and the
            // returns file names them: one MAIE deputy in the circoscrizione Estero (the overseas
            // constituency) and one Union Valdotaine deputy in the Valle d'Aosta college. Without
            // them the chamber sums to 398, and rounding 398 up to 400 by inflating a party would
            // have been inventing seats to make a total come out.
            new PoliticalParty("MAIE",  "MAIE (circoscrizione Estero)",    float.NaN, float.NaN,  1),
            new PoliticalParty("UV",    "Union Valdotaine (VdA college)",  float.NaN, float.NaN,  1),
        };

        // ---- USA: House of Representatives, 119th Congress (2024). Sums to 435.
        // Positions are GPS 2019, NOT CHES: the positions file records that CHES-USA is unpublished
        // and that GPS is pre-2020 and has no general left-right or EU item. The substitution is
        // named there and named again here so it cannot be read as CHES by a later reader.
        private static readonly PoliticalParty[] UsaParties =
        {
            new PoliticalParty("REP", "Republican Party", 8.23f, 8.30f, 220, "mark_party_us_rep"),
            new PoliticalParty("DEM", "Democratic Party", 3.73f, 2.41f, 215, "mark_party_us_dem"),
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
        /// </summary>
        public static bool TryHistory(CountryId id, out double[] latest, out double[] previous)
        {
            switch (id)
            {
                case CountryId.Sweden:
                    // 2022 and 2018 final shares, Valmyndigheten (returns_2022.md, priors/previous_elections.md),
                    // in For(Sweden) order: S, SD, M, V, C, KD, MP, L.
                    latest   = new[] { 30.33, 20.54, 19.10, 6.75, 6.71, 5.34, 5.08, 4.61 };
                    previous = new[] { 28.26, 17.53, 19.84, 8.00, 8.61, 6.32, 4.41, 5.49 };
                    return true;
                case CountryId.Germany:
                    // 2025 Zweitstimmen shares (returns_2025.md) and 2021 shares derived from the
                    // per-Land absolute counts on disk (land_votes_2021.csv, valid 46,298,387), in
                    // For(Germany) order: CDU, AfD, SPD, Grune, Linke, CSU, SSW, BSW, FDP.
                    // BSW is a TRUE ZERO in 2021 - the party did not exist - not a missing figure.
                    latest   = new[] { 22.60, 20.80, 16.40, 11.60, 8.80, 6.00, 0.20, 4.98, 4.30 };
                    previous = new[] { 18.95, 10.39, 25.71, 14.72, 4.87, 5.19, 0.12, 0.00, 11.43 };
                    return true;
                default:
                    latest = null; previous = null; return false;
            }
        }
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
        /// ⚠ **ONLY THE FOUR BACKTESTED COUNTRIES HAVE ONE.** France and the USA were never fitted,
        /// because neither is a proportional system and the backtest is a share model. `HasElectorate`
        /// is false for them, and `NationalElection` refuses those two anyway.
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
                default:
                    electorate = default; economicWeight = 0.0; return false;
            }
        }
        /// <summary>The country's seat-holding units, in seat order at its most recent election.</summary>
        public static IReadOnlyList<PoliticalParty> For(CountryId id)
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
        public static Dictionary<string, int> InitialSeats(CountryId id)
        {
            var seats = new Dictionary<string, int>();
            foreach (PoliticalParty p in For(id)) { seats[p.Abbrev] = p.SeedSeats; }
            return seats;
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
