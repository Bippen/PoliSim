using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;

namespace PoliSim.UI
{
    /// <summary>One slip (19b): its head - the glyph's or the anchor's own word, so every slip is its own legend entry - and its lines. A marked term
    /// is written <c>[[TERM]]</c> in a line; resting on it opens the term's slip, level 2.</summary>
    public sealed class SlipContent
    {
        public readonly string Head;
        public readonly List<string> Lines = new List<string>();
        public SlipContent(string head) { Head = head; }
        public SlipContent Add(string line) { Lines.Add(line); return this; }

        private static readonly Regex Marked = new Regex(@"\[\[(.+?)\]\]", RegexOptions.CultureInvariant);

        /// <summary>The marked terms in a line, in order.</summary>
        public static IEnumerable<string> TermsIn(string line) { foreach (Match m in Marked.Matches(line)) { yield return m.Groups[1].Value; } }

        /// <summary>A line as it prints: the marks removed, the term's words kept.</summary>
        public static string Plain(string line) => line.Replace("[[", string.Empty).Replace("]]", string.Empty);
    }

    /// <summary>
    /// §666 (UI v3.3 §1 and §4.1, boards 19b and 20a): **PEOPLE'S SLIPS, BUILT FROM THE MODEL AND NOTHING ELSE.** Every anchor the page keeps at rest
    /// has its level-1 slip here; every term a slip marks has its level-2 slip here. The page draws from this book and `PeopleSlipReachabilityCheck`
    /// reads the same book, so the check cannot pass on a slip the page does not open. Every word that left the page at rest (measured as the dry
    /// film's removed draws) is on a slip within two levels.
    ///
    /// <para><b>§732 (UI v3.5): THE WHOLE PAGE.</b> The cohort block's anchors (<see cref="Build"/>) and, through <see cref="BuildPage"/>, every tile of
    /// the five families the plates carried - its census (the unit, the source and its year, the honesty class, the stated range, where the country
    /// ranks among the six at seed, what reaches it) - and the kept employment card. A family's plate printed its census behind the † and its name,
    /// figure and graphics at rest; a v3.5 tile prints its name and figure, and this slip is the rest: the † dense view prints it as the tile's
    /// dotted line.</para>
    /// </summary>
    public static class PeopleSlips
    {
        public sealed class Book
        {
            public readonly Dictionary<string, SlipContent> Anchors = new Dictionary<string, SlipContent>();
            public readonly Dictionary<string, SlipContent> Terms = new Dictionary<string, SlipContent>();
        }

        public static string Millions(float millions) => millions >= 1f
            ? millions.ToString("0.00", CultureInfo.InvariantCulture) + "M"
            : (millions * 1000f).ToString("0", CultureInfo.InvariantCulture) + "k";

        /// <summary>The turnout the band's first eligible age falls in, or NaN.</summary>
        public static double BandTurnout(CohortVoterGroups.Group[] groups, int eligibleFrom)
        {
            foreach (CohortVoterGroups.Group g in groups) { if (eligibleFrom >= g.FromAge && eligibleFrom <= g.ToAge) { return g.TurnoutBase; } }
            return double.NaN;
        }

        private static string P1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

        /// <summary>A plate's figure as the plates printed it: "absent" where the model holds none (a negative), never a zero.</summary>
        public static string Figure(float value, int decimals, string symbol = "")
            => value < 0f ? "absent" : value.ToString(decimals == 0 ? "0" : "0." + new string('0', decimals), CultureInfo.InvariantCulture) + symbol;

        /// <summary>
        /// §732: the WHOLE PAGE's book - the cohort block where the country carries the substrate, then the five families and the employment card.
        /// <paramref name="world"/> gives the peers' seeds the ranks are read against; null leaves the ranks out.
        /// </summary>
        public static Book BuildPage(Country country, World world)
        {
            Book book;
            PopulationCohorts cohorts = country.Cohorts;
            if (cohorts != null)
            {
                CohortVoterGroups.Group[] groups = CohortVoterGroups.For(country);
                bool sourced = groups.Length > 0 && !double.IsNaN(groups[0].TurnoutBase);
                book = Build(cohorts, groups, CohortVoterGroups.VotingAge(country.Id), sourced);
            }
            else
            {
                book = new Book();
                book.Anchors["head:population"] = new SlipContent("POPULATION").Add("THIS COUNTRY CARRIES NO COHORT SUBSTRATE YET - THE INSTRUMENTS DRAW WHEN IT DOES");
                AddKey(book);   // the families' slips mark the honesty classes
            }
            AddHealth(book, country, world);
            AddEducation(book, country, world);
            AddInfrastructure(book, country, world);
            AddEnvironment(book, country, world);
            AddMigrationPoverty(book, country, world);
            AddEmployment(book, country);
            return book;
        }

        public static Book Build(PopulationCohorts cohorts, CohortVoterGroups.Group[] groups, int votingAge, bool turnoutSourced)
        {
            var book = new Book();
            float total = cohorts.Total;
            int peak = 0;
            for (int i = 1; i < PopulationCohorts.CohortCount; i++) { if (cohorts.Counts[i] > cohorts.Counts[peak]) { peak = i; } }
            int votingBandFrom = votingAge - votingAge % 5;
            double eligible = CohortVoterGroups.EligiblePopulation(cohorts, votingAge);
            float working = total > 0f ? cohorts.InAgeRange(15, 64) / total * 100f : 0f;

            // Level 2: the terms.
            book.Terms["WORKING AGE"] = new SlipContent("WORKING AGE · 15–64")
                .Add("15 — WORKING AGE BEGINS · 65 — OLD AGE · THE DASHED LINES BOUND IT")
                .Add("BOTH ARE THE MODEL'S · THE DEPENDENCY RATIOS DIVIDE BY IT")
                .Add(P1(working) + "% OF ALL");
            AddKey(book);
            // §732: the voter groups' shares, which the electorate's bar carried at rest, as the eligible's level 2
            var voterGroups = new SlipContent("VOTER GROUPS · POPULATION SHARE OF THE ELIGIBLE");
            for (int i = 0; i < groups.Length; i++)
            {
                string line = groups[i].Name + " " + (groups[i].PopulationShare * 100.0).ToString("0", CultureInfo.InvariantCulture) + " · % OF THE ELIGIBLE";
                if (turnoutSourced && !double.IsNaN(groups[i].TurnoutBase)) { line += " · TURNOUT " + groups[i].TurnoutBase.ToString("0", CultureInfo.InvariantCulture) + "% ◇ 2014"; }
                voterGroups.Add(line);
            }
            book.Terms["VOTER GROUPS"] = voterGroups;

            // Level 1: the population's anchors - the head (its figure is the total) and every band; a band's turnout row opens its band's slip.
            book.Anchors["head:population"] = new SlipContent("POPULATION BY BAND · " + Millions(total) + " · [[DERIVED]]")
                .Add("COHORT SUBSTRATE · 21 FIVE-YEAR BANDS · THE OPEN BAND LAST")
                .Add("TOTAL " + Millions(total) + " · THE BANDS SUM TO IT EXACTLY")
                .Add("[[WORKING AGE]] BETWEEN THE DASHED LINES");
            for (int i = 0; i < PopulationCohorts.CohortCount; i++)
            {
                int from = i * PopulationCohorts.CohortWidth;
                int to = i == PopulationCohorts.OpenBandIndex ? 999 : from + PopulationCohorts.CohortWidth - 1;
                var band = new SlipContent(PopulationCohorts.Label(i) + (i == peak ? " · PEAK BAND" : string.Empty))
                    .Add(PopulationCohorts.Label(i) + " · " + Millions(cohorts.Counts[i]) + " · " + (total > 0f ? P1(cohorts.Counts[i] / total * 100f) : "0.0") + "% OF ALL");
                if (to < votingAge) { band.Add("UNDER THE VOTING AGE - NO TURNOUT"); }
                else if (!turnoutSourced) { band.Add("TURNOUT · NO SOURCE"); }
                else
                {
                    double t = BandTurnout(groups, from < votingAge ? votingAge : from);
                    if (!double.IsNaN(t)) { band.Add("TURNOUT " + t.ToString("0", CultureInfo.InvariantCulture) + "% ◇ [[DATED]] 2014"); }
                }
                if (from >= 15 && from < 65) { band.Add("[[WORKING AGE]] · " + working.ToString("0", CultureInfo.InvariantCulture) + "% OF ALL"); }
                if (i == peak) { band.Add("THE LARGEST OF 21 BANDS · THE AXIS ENDS AT IT"); }
                book.Anchors["band:" + i.ToString(CultureInfo.InvariantCulture)] = band;
            }

            // The turnout lane's head (◇).
            book.Anchors["head:turnout"] = turnoutSourced
                ? new SlipContent("TURNOUT BY AGE · [[DATED]]")
                    .Add("[[SOURCED]] · SCB 2014 · A PUBLISHED SERIES")
                    .Add("DRAWN ONLY WHERE A BAND IS ELIGIBLE; NO LANE WHERE NOT")
                    .Add($"VOTING AGE {votingAge} — INSIDE THE {votingBandFrom}–{votingBandFrom + 4} BAND, SPLIT PRO RATA")
                : new SlipContent("TURNOUT BY AGE · NO SOURCE")
                    .Add("NO SOURCE FOR THIS COUNTRY - NO LANE IS DRAWN RATHER THAN ONE DRAWN FROM A GUESS");

            // Dependency: the head, the four figures and the age split.
            book.Anchors["head:dependency"] = new SlipContent("DEPENDENCY · AS THE SUBSTRATE DERIVES IT · [[DERIVED]]")
                .Add("OLD-AGE · TOTAL · 0–19 · 65+ - EACH OVER THE [[WORKING AGE]]");
            book.Anchors["fig:oldage"] = new SlipContent("OLD-AGE · " + P1(cohorts.OldAgeDependencyRatio)).Add("OLD-AGE · 65+ ⁄ 15–64 × 100").Add("OVER THE [[WORKING AGE]] · [[DERIVED]]");
            book.Anchors["fig:total"] = new SlipContent("TOTAL · " + P1(cohorts.TotalDependencyRatio)).Add("TOTAL · (0–14 + 65+) ⁄ 15–64 × 100").Add("OVER THE [[WORKING AGE]] · [[DERIVED]]");
            book.Anchors["fig:school"] = new SlipContent("0–19 · " + P1(cohorts.SchoolAgeShare) + "%").Add("SCHOOL-AGE SHARE (0–19)").Add("OF ALL · [[DERIVED]]");
            book.Anchors["fig:elderly"] = new SlipContent("65+ · " + P1(cohorts.ElderlyShare) + "%").Add("ELDERLY SHARE (65+)").Add("OF ALL · [[DERIVED]]");
            float young = total > 0f ? cohorts.InAgeRange(0, 14) / total * 100f : 0f;
            float old = total > 0f ? cohorts.InAgeRange(65, 999) / total * 100f : 0f;
            book.Anchors["card:agesplit"] = new SlipContent("AGE SPLIT · SHARES OF ALL")
                .Add($"0–14 {young:0}% · 15–64 {working:0}% · 65+ {old:0}%")
                .Add("UNDER 15 · [[WORKING AGE]] · 65 AND OVER · [[DERIVED]]");

            // The electorate: the head and the four figures; the voter groups are the eligible's level 2.
            book.Anchors["head:electorate"] = new SlipContent("THE ELECTORATE · [[DERIVED]]")
                .Add($"THE ELIGIBLE, BY [[VOTER GROUPS]] · {groups.Length} GROUPS");
            book.Anchors["fig:eligible"] = new SlipContent("ELIGIBLE · " + Millions((float)eligible))
                .Add($"THE POPULATION AT OR OVER THE VOTING AGE, {votingAge} · [[DERIVED]]")
                .Add("BY [[VOTER GROUPS]]");
            book.Anchors["fig:ofall"] = new SlipContent("OF ALL · " + (total > 0f ? P1(eligible / total * 100.0) : "—") + "%").Add("ELIGIBLE, AS A SHARE OF THE POPULATION");
            book.Anchors["fig:votingage"] = new SlipContent("VOTING AGE · " + votingAge.ToString(CultureInfo.InvariantCulture)).Add("VOTING AGE · [[SOURCED]] · CONSTITUTION");
            if (turnoutSourced)
            {
                double votes = 0.0;
                foreach (CohortVoterGroups.Group g in groups) { votes += g.PopulationShare * eligible * g.TurnoutBase / 100.0; }
                book.Anchors["fig:votes"] = new SlipContent("VOTES · ≈ " + Millions((float)votes))
                    .Add("WHAT VOTES · ELIGIBLE × TURNOUT BY BAND")
                    .Add("IF EACH BAND VOTED AT ITS 2014 RATE — A DERIVATION FROM TWO [[SOURCED]] SERIES, NOT A FORECAST");
            }
            else
            {
                book.Anchors["fig:votes"] = new SlipContent(SymbolRegistry.Word(Symbol.Absent)).Add("LIKELY VOTES · NO TURNOUT SOURCE FOR THIS COUNTRY").Add("NOTHING IS DERIVED FROM A GUESS");
            }
            return book;
        }

        /// <summary>Level 2: the honesty key, as the coalition page prints it - every class a slip marks opens it.</summary>
        private static void AddKey(Book book)
        {
            var key = new SlipContent("KEY · HONESTY, AS THE COALITION PAGE PRINTS IT")
                .Add("DERIVED — THE MODEL'S OWN ARITHMETIC OVER THE COHORTS")
                .Add("DECLARED — AUTHORED AND SAID SO")
                .Add("SOURCED — A PUBLISHED SERIES, WITH ITS YEAR")
                .Add("MEASURED — READ OFF THE RUNNING MODEL")
                .Add("◇ DATED — A SERIES OLDER THAN THE GAME");
            book.Terms["DERIVED"] = key;
            book.Terms["SOURCED"] = key;
            book.Terms["DATED"] = key;
        }

        // ---- §732: the families' tiles ----

        private static readonly CountryId[] PeerOrder = { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.USA };

        /// <summary>The other five's seeds for one reading, those that report it (a negative is a country with no series).</summary>
        private static float[] Peers(World world, CountryId self, System.Func<Country, float> read)
        {
            var peers = new List<float>();
            if (world == null) { return peers.ToArray(); }
            foreach (CountryId id in PeerOrder)
            {
                if (id == self) { continue; }
                Country c = world.GetCountry(id);
                if (c == null) { continue; }
                float v = read(c);
                if (v >= 0f) { peers.Add(v); }
            }
            return peers.ToArray();
        }

        /// <summary>Where the country stands among those that report (1 = best) - the plates' six pips, as words.</summary>
        private static string Rank(float own, float[] peers, bool lowerIsBetter)
        {
            if (own < 0f || peers == null) { return null; }
            int rank = 1;
            foreach (float p in peers) { if (lowerIsBetter ? p < own : p > own) { rank++; } }
            return $"RANKS {rank} OF {peers.Length + 1} THAT REPORT · THE OTHER FIVE AT SEED";
        }

        private static string Range(float low, float high, bool lowerIsBetter, string unit = "") =>
            $"THE FAMILY'S STATED RANGE {Figure(low, 0)}–{Figure(high, 0)}{unit} · " + (lowerIsBetter ? "LOWER IS BETTER" : "HIGHER IS BETTER");

        /// <summary>One reading's census: its head, then the definition, the source with its honesty class, the range and rank where it has them, what
        /// reaches it, and whether its coupling is a draft.</summary>
        private static SlipContent Reading(string head, string definition, string source, string honesty, string range, string rank, string reachedBy, bool couplingDraft)
        {
            var slip = new SlipContent(head).Add(definition).Add(source + " · [[" + honesty + "]]");
            if (range != null) { slip.Add(range); }
            if (rank != null) { slip.Add(rank); }
            if (reachedBy != null) { slip.Add("REACHED BY · " + reachedBy); }
            if (couplingDraft) { slip.Add("THE COUPLING IS A DRAFT UNTIL MEASURED"); }
            return slip;
        }

        /// <summary>A gap's slip: the glyph's word as its head (the slip is the glyph's legend), the reading's name and its sentence.</summary>
        private static SlipContent Gap(Symbol glyph, string name, string sentence, string source, string reachedBy = null)
        {
            var slip = new SlipContent(SymbolRegistry.Word(glyph)).Add(name).Add(sentence).Add(source);
            if (reachedBy != null) { slip.Add(reachedBy); }
            return slip;
        }

        private static string Year(int year) => year > 0 ? " · " + year.ToString(CultureInfo.InvariantCulture) : string.Empty;

        private static void AddHealth(Book book, Country country, World world)
        {
            HealthSeeds h = country.Health;
            if (h == null || !h.Seeded)
            {
                book.Anchors["section:health"] = new SlipContent("HEALTH").Add("THIS COUNTRY CARRIES NO HEALTH FAMILY - THE SPINE COVERS SIX, AND THIS IS NOT ONE OF THEM");
                return;
            }
            EconomyState s = country.State;
            string vintage = h.CoverageYear > 0 ? h.CoverageYear + "–25" : "2022–25";
            book.Anchors["section:health"] = new SlipContent("HEALTH · " + vintage)
                .Add("SOURCED · OECD · " + vintage)
                .Add("SEEDS: OECD SDMX, LATEST OBSERVATION PER COUNTRY, SEX TOTAL")
                .Add("COUPLINGS: THE HEALTH SPINE'S TABLES, DRAFT UNTIL MEASURED");
            const string effectiveness = "EFFECTIVENESS (C7) ▸";
            book.Anchors["health:coverage"] = Reading("COVERAGE · " + Figure(s.HealthCoverage, 1, "%"), "% OF POPULATION · CORE SERVICES", "OECD HEALTH_PROT · TPRIBASI" + Year(h.CoverageYear), "SOURCED",
                    Range(0f, h.CoverageCeiling, false), Rank(s.HealthCoverage, Peers(world, country.Id, c => c.Health != null && c.Health.Seeded ? c.Health.Coverage : -1f), false), "HEALTH LINE — HEAD ▸", true)
                .Add("RETIREE COVERAGE · " + Figure(HealthFamily.RetireeCoverage(country), 1, "%") + " OF THE 65+ COHORT · DERIVED · F2 SUBSTRATE × COVERAGE");
            float publicShare = HealthFamily.PublicCoverageNow(country);
            book.Anchors["health:public"] = Reading("PUBLIC SHARE · " + Figure(publicShare, 1, "%"), "… OF WHICH PUBLIC · % OF POPULATION · GOVERNMENT / COMPULSORY", "OECD HEALTH_PROT · COVGCMED ÷ TPRIBASI", "DERIVED",
                Range(0f, 100f, false), Rank(publicShare, Peers(world, country.Id, c => c.Health != null && c.Health.Seeded ? c.Health.CoveragePublic : -1f), false), "READOUT · NOTHING REACHES IT", false);
            book.Anchors["health:mortality"] = Reading("TREATABLE DEATHS · " + Figure(s.TreatableMortality, 0, " ⁄100k"), "QUALITY · TREATABLE MORTALITY · DEATHS / 100 000 · AGE-STD", "OECD HEALTH_STAT · TRTM" + Year(h.TreatableMortalityYear), "SOURCED",
                Range(40f, 120f, true), Rank(s.TreatableMortality, Peers(world, country.Id, c => c.Health != null && c.Health.Seeded ? c.Health.TreatableMortality : -1f), true),
                "HEALTH LINE — AGE-COST ▸ · EFFICIENCY ▸ · " + effectiveness, true);
            book.Anchors["health:cataract"] = h.HasWaits
                ? Reading("CATARACT WAIT · " + Figure(s.WaitCataractDays, 0, " d") + " ◇", "WAITING · CATARACT · MEAN DAYS · SPECIALIST TO TREATMENT", "OECD DF_WAITING · CM131_138" + Year(h.WaitYear), "SOURCED",
                        Range(0f, 180f, true), Rank(s.WaitCataractDays, Peers(world, country.Id, c => c.Health != null && c.Health.Seeded ? c.Health.WaitCataract : -1f), true), effectiveness, true)
                    .Add("WAITING · KNEE REPLACEMENT · " + Figure(s.WaitKneeDays, 0, " d") + " · OECD DF_WAITING · CM8154" + Year(h.WaitYear))
                : Gap(Symbol.Absent, "CATARACT WAIT · WAITING · KNEE REPLACEMENT", "NO COMPARABLE SERIES PUBLISHED · SE IT PL REPORT", "OECD DF_WAITING · NO ROWS FOR " + country.Id.ToString().ToUpperInvariant(),
                    "NOT SIMULATED — EFFECTIVENESS REACHES QUALITY DIRECTLY");
            for (int i = 0; i < HealthFamily.SupportingNames.Length; i++)
            {
                book.Anchors["health:sup:" + i.ToString(CultureInfo.InvariantCulture)] = h.HasSupporting
                    ? new SlipContent(HealthFamily.SupportingNames[i] + " · " + Figure(HealthFamily.SupportingNow(country, i), 1))
                        .Add(HealthFamily.SupportingUnits[i])
                        .Add("SUPPORTING READOUTS · MOVED BY THE QUALITY KEY · NEVER COUPLED")
                        .Add("OECD HCQO · DF_PC · DF_AC" + Year(h.SupportingYear) + " · [[SOURCED]]")
                    : Gap(Symbol.Absent, HealthFamily.SupportingNames[i], "THE HCQO FLOWS HOLD NO ROWS FOR THIS COUNTRY", "OECD HCQO · DF_PC · DF_AC");
            }
        }

        private static void AddEducation(Book book, Country country, World world)
        {
            EducationSeeds e = country.Education;
            if (e == null || !e.Seeded)
            {
                book.Anchors["section:education"] = new SlipContent("EDUCATION").Add("THIS COUNTRY CARRIES NO EDUCATION FAMILY - THE SPINE COVERS SIX, AND THIS IS NOT ONE OF THEM");
                return;
            }
            EconomyState s = country.State;
            book.Anchors["section:education"] = new SlipContent("EDUCATION · 2023–25")
                .Add("SOURCED · OECD EAG · EUROSTAT · 2023–25")
                .Add("STUDENTS PER TEACHER IS THE EDUCATION MINISTER'S CARD'S KEY, NOT A PAGE ROW")
                .Add("COUPLINGS: THE EDUCATION SPINE'S TABLES, DRAFT UNTIL MEASURED");
            book.Anchors["education:pisa"] = Gap(Symbol.Billed, "PISA SCORE · ACADEMIC SCORE · PISA · PISA MEAN · HIGHER IS BETTER", "THE TABLES SIT BEHIND THE WWW HOST, NOT ON THE SDMX API",
                "OECD PISA 2022 · VOL. I · TABLES I.B1.2.1–3", "REACHED BY · STUDENTS PER TEACHER ▸ · EFFECTIVENESS (C7) ▸");
            book.Anchors["education:graduation"] = Gap(Symbol.Billed, "GRADUATION RATE · % AT TYPICAL AGE · UPPER SEC.", "NO OECD FLOW HOLDS THE RATE; THE FLOWS HOLD COUNTS",
                "OECD EAG 2024 · TABLE B3.1", "REACHED BY · EARLY LEAVERS ▸");
            book.Anchors["education:leavers"] = e.HasEarlyLeavers
                ? Reading("EARLY SCHOOL LEAVERS · " + Figure(s.EarlyLeavers, 1, "%"), "EARLY LEAVERS · % OF 18–24 · LEFT EDUCATION", "EUROSTAT edat_lfse_14" + Year(e.EarlyLeaversYear), "SOURCED",
                    Range(0f, 20f, true, "%"), Rank(s.EarlyLeavers, Peers(world, country.Id, c => c.Education != null && c.Education.Seeded ? c.Education.EarlyLeavers : -1f), true),
                    "YOUTH UNEMPLOYMENT ▸ · EDUCATION LINE — PER PUPIL ▸", true)
                : Gap(Symbol.Absent, "EARLY SCHOOL LEAVERS · % OF 18–24 · LEFT EDUCATION", "NCES STATUS DROPOUT IS ANOTHER DEFINITION ON ANOTHER AGE BAND · SE DE FR IT PL REPORT",
                    "EUROSTAT edat_lfse_14 · NO USA", "NOT SIMULATED — YOUTH UNEMPLOYMENT REACHES ATTAINMENT DIRECTLY");
            float atLeast = EducationFamily.AtLeastUpperSecondary(country);
            book.Anchors["education:attainment"] = Reading("UPPER SECONDARY OR MORE · " + Figure(atLeast, 1, "%"), Figure(atLeast, 1, "%") + " ≥ UPPER SEC. · AGED 25–64",
                    "OECD EAG · LSO_NEAC_DISTR_EA" + Year(e.AttainmentYear), "SOURCED", null,
                    Rank(s.AttainmentTertiary, Peers(world, country.Id, c => c.Education != null && c.Education.Seeded ? c.Education.Tertiary : -1f), false)?.Replace("RANKS", "TERTIARY RANKS"),
                    "THE STOCK-FLOW LINE ▸ · EARLY LEAVERS ▸", true)
                .Add("ATTAINMENT BY LEVEL · 25–64 · % OF 25–64 · ISCED 0–2 · 3–4 · 5–8")
                .Add($"BELOW UPPER SECONDARY {Figure(s.AttainmentBelowUpperSecondary, 0, "%")} · UPPER SECONDARY {Figure(s.AttainmentUpperSecondary, 0, "%")} · TERTIARY {Figure(s.AttainmentTertiary, 0, "%")}");
        }

        private static void AddInfrastructure(Book book, Country country, World world)
        {
            InfrastructureSeeds f = country.Infrastructure;
            if (f == null || !f.Seeded)
            {
                book.Anchors["section:infrastructure"] = new SlipContent("INFRASTRUCTURE").Add("THIS COUNTRY CARRIES NO INFRASTRUCTURE FAMILY - THE SPINE COVERS SIX, AND THIS IS NOT ONE OF THEM");
                return;
            }
            EconomyState s = country.State;
            book.Anchors["section:infrastructure"] = new SlipContent("INFRASTRUCTURE · 2019–24")
                .Add("SOURCED · WEF GCR · OECD · 2019–24")
                .Add("SEEDS: THE WEF GCI 4.0 2019 DATASET, VERIFIED BY CONTENT")
                .Add("THE ROAD-QUALITY SURVEY ENDED WITH THAT EDITION - THE FIGURE IS DATED AND SAYS SO")
                .Add("COUPLINGS: THE INFRASTRUCTURE SPINE'S TABLES, DRAFT UNTIL MEASURED");
            book.Anchors["infra:quality"] = Reading("ROAD QUALITY · " + Figure(s.RoadQuality, 1) + " ◇ [[DATED]] 2019", "SCORE 0–100 · SURVEY 2018–19 · DATED 2019",
                "WEF GCR 2019 · ROADINF · RANK " + f.RoadQualityRank + " OF 141", "SOURCED", Range(50f, 100f, false),
                Rank(s.RoadQuality, Peers(world, country.Id, c => c.Infrastructure != null && c.Infrastructure.Seeded ? c.Infrastructure.RoadQuality : -1f), false), "INFRASTRUCTURE LINE — PER HEAD ▸ · DECAY ▸", true);
            book.Anchors["infra:connectivity"] = Reading("ROAD CONNECTIVITY · " + Figure(s.RoadConnectivity, 1), "INDEX 0–100 · TEN-CITY TRAVEL SPEEDS", "WEF GCR 2019 · ROADQUALIDX", "SOURCED", Range(70f, 100f, false),
                Rank(s.RoadConnectivity, Peers(world, country.Id, c => c.Infrastructure != null && c.Infrastructure.Seeded ? c.Infrastructure.RoadConnectivity : -1f), false), "ROAD QUALITY ▸ · POPULATION ▸", true);
            book.Anchors["infra:congestion"] = Gap(Symbol.Billed, "CONGESTION · % EXTRA TRAVEL TIME · PER CITY", "A PAGE AND A CHART, NO DATA · CONNECTIVITY STANDS IN",
                "TOMTOM INDEX 2024 · INRIX SCORECARD 2024", "REACHED BY · ROAD CONNECTIVITY ▸");
            book.Anchors["infra:length"] = Gap(Symbol.Absent, "ROAD LENGTH · KM · THE NETWORK", "NO OPEN FILE PUBLISHES IT · NEVER 0, NEVER A DASH", "IRF WORLD ROAD STATISTICS · PAID", "NOT SIMULATED");
        }

        private static void AddEnvironment(Book book, Country country, World world)
        {
            EnvironmentSeeds e = country.Environment;
            if (e == null || !e.Seeded)
            {
                book.Anchors["section:environment"] = new SlipContent("ENVIRONMENT").Add("THIS COUNTRY CARRIES NO ENVIRONMENT FAMILY - THE SPINE COVERS SIX, AND THIS IS NOT ONE OF THEM");
                return;
            }
            EconomyState s = country.State;
            float ghg = EnvironmentFamily.GhgPerCapitaNow(country);
            float rest = System.Math.Max(0f, ghg - s.PowerCo2PerCapita - s.TransportCo2PerCapita);
            var section = new SlipContent("ENVIRONMENT · 2023–24")
                .Add("SOURCED · EDGAR · WORLD BANK · 2023–24")
                .Add("SEEDS: THE EDGAR 2024 GHG BOOKLET, VERIFIED BY CONTENT, OVER WORLD BANK POPULATIONS 2023 · THE HEADLINE IS ALL GASES, THE KEYS CO₂");
            // §732: the plate's three electricity rows are the Energy page's - drawn there at rest, with the stack behind each price
            float[] mix = e.HasMix ? (e.PowerFromDispatch ? EnergyMarket.MixSharesNow(country) : e.MixShares) : null;
            section.Add("ELECTRICITY BY SOURCE" + (mix != null ? " · " + Figure(mix[0] + mix[1], 0, "% FOSSIL") : string.Empty) + " · ELECTRICITY PRICE, HOUSEHOLDS · ELECTRICITY PRICE, INDUSTRY — ON THE ENERGY PAGE");
            if (EnergyLayer.Has(country.Id))
            {
                EnergyLayer.Co2 decomposition = EnergyLayer.Decomposition(country.Id, e.PowerCo2PerCapita);
                section.Add($"THE ENERGY LAYER AT THE {EnergyLayer.Year} SEED: THE POWER PLANTS' OWN COMBUSTION IS {decomposition.DerivedShare * 100f:0} % OF THE POWER FIGURE, THE REST HEAT PLANTS, CHP HEAT AND REFINERIES");
            }
            section.Add("THE CARBON TAX'S BASE IS THE TAXED CO₂ - TRANSPORT PER HEAD × POPULATION; THE POWER FLEET PAYS THE ETS AND IS EXEMPT OF THE TAX BY STATUTE");
            book.Anchors["section:environment"] = section;
            book.Anchors["env:ghg"] = Reading("EMISSIONS PER PERSON · " + Figure(ghg, 2, " t"), "t CO2-eq · ALL GASES", "EDGAR 2024 · GHG PER CAPITA · 2023", "DERIVED", Range(0f, 20f, true, " t"),
                    Rank(ghg, Peers(world, country.Id, c => c.Environment != null && c.Environment.Seeded ? c.Environment.GhgPerCapita : -1f), true), "POWER ▸ · TRANSPORT ▸ · THE REST HELD AT SEED", false)
                .Add($"… THE SPLIT · ELECTRICITY {Figure(s.PowerCo2PerCapita, 2, " t")} · TRANSPORT {Figure(s.TransportCo2PerCapita, 2, " t")} · EVERYTHING ELSE, THE REST AT SEED {Figure(rest, 2, " t")}");
            book.Anchors["env:power"] = Reading("ELECTRICITY CO2 / HEAD · " + Figure(s.PowerCo2PerCapita, 2, " t"), "t CO2 · POWER ÷ POPULATION", "EDGAR 2024 · POWER · WB POP · 2023", "SOURCED", Range(0f, 5f, true, " t"),
                Rank(s.PowerCo2PerCapita, Peers(world, country.Id, c => c.Environment != null && c.Environment.Seeded ? c.Environment.PowerCo2PerCapita : -1f), true), "DISPATCH ▸ · THE ETS PRICE, NOT THE CARBON TAX", true);
            book.Anchors["env:transport"] = Reading("TRANSPORT CO2 / HEAD · " + Figure(s.TransportCo2PerCapita, 2, " t"), "t CO2 · TRANSPORT ÷ POPULATION", "EDGAR 2024 · TRANSPORT · WB POP · 2023", "SOURCED", Range(0f, 6f, true, " t"),
                Rank(s.TransportCo2PerCapita, Peers(world, country.Id, c => c.Environment != null && c.Environment.Seeded ? c.Environment.TransportCo2PerCapita : -1f), true), "CARBON TAX ▸ · INFRASTRUCTURE LINE ▸", true);
        }

        private static void AddMigrationPoverty(Book book, Country country, World world)
        {
            MigrationPovertySeeds m = country.MigrationPoverty;
            if (m == null || !m.Seeded)
            {
                book.Anchors["section:migration"] = new SlipContent("IMMIGRATION · POVERTY DEPTH").Add("THIS COUNTRY CARRIES NO IMMIGRATION AND POVERTY FAMILY - THE SPINE COVERS SIX, AND THIS IS NOT ONE OF THEM");
                return;
            }
            EconomyState s = country.State;
            bool stock = m.MigrationIsStock;
            book.Anchors["section:migration"] = new SlipContent("IMMIGRATION · POVERTY DEPTH · 2022–24")
                .Add("SOURCED · EUROSTAT · OECD · DHS · BLS · 2022–24")
                .Add("TWO DEFINITIONS WHERE THE SOURCES HAVE TWO - THE FIVE'S FLOW AND THE USA'S STOCK NEVER SHARE AN AXIS")
                .Add("HOMELESSNESS CARRIES EACH COUNTRY'S OWN DEFINITION AND YEAR · THE RANKS ARE THE SAME-DEFINITION COUNTRIES ONLY");
            book.Anchors["mig:irregular"] = stock
                ? Reading("IRREGULAR MIGRATION · " + Figure(s.IrregularMigrationPer10k, 0) + " ‡ TWO DEFINITIONS", "PER 10 000 · STOCK · UNAUTHORIZED RESIDENTS", "DHS OHSS · 1 JAN " + m.MigrationYear + " · 10.99 M", "SOURCED",
                    Range(0f, 400f, true), null, "IMMIGRATION POLICY ▸ · BORDER ENFORCEMENT ▸", true)
                : Reading("IRREGULAR MIGRATION · " + Figure(s.IrregularMigrationPer10k, 1) + " ‡ TWO DEFINITIONS", "PER 10 000 · FLOW · FOUND ILLEGALLY PRESENT", "EUROSTAT migr_eipre" + Year(m.MigrationYear), "SOURCED",
                    Range(0f, 40f, true), Rank(s.IrregularMigrationPer10k, Peers(world, country.Id, c => c.MigrationPoverty != null && c.MigrationPoverty.Seeded && !c.MigrationPoverty.MigrationIsStock ? c.MigrationPoverty.IrregularMigrationPer10k : -1f), true),
                    "IMMIGRATION POLICY ▸ · BORDER ENFORCEMENT ▸", true);
            string gapUnit = m.PovertyGapIsOecd ? "% BELOW THE 60 % LINE · OECD" : "% BELOW THE 60 % LINE · OECD'S OWN READING " + Figure(m.PovertyGapOecd, 1);
            book.Anchors["mig:gap"] = Reading("POVERTY GAP · " + Figure(s.PovertyGap, 1, "%"), gapUnit,
                (m.PovertyGapIsOecd ? "OECD IDD · PG_INC_DISP · PL_60" : "EUROSTAT ilc_li11 · MED_EI · B_60") + Year(m.PovertyGapYear), "SOURCED", Range(0f, 50f, true, "%"),
                Rank(s.PovertyGap, Peers(world, country.Id, c => c.MigrationPoverty != null && c.MigrationPoverty.Seeded && c.MigrationPoverty.PovertyGapIsOecd == m.PovertyGapIsOecd ? c.MigrationPoverty.PovertyGap : -1f), true),
                "WELFARE GENEROSITY ▸ · MINIMUM WAGE ▸", true);
            book.Anchors["mig:underemployment"] = Reading("UNDEREMPLOYMENT · " + Figure(s.Underemployment, 2, "%"), "% OF EMPLOYMENT" + (stock ? " · 16+ BLS" : " · 20–64"),
                (stock ? "BLS LNS12032194 ÷ LNS12000000" : "EUROSTAT lfsi_sup_a ÷ lfsi_emp_a") + Year(m.UnderemploymentYear), "SOURCED", Range(0f, 6f, true, "%"),
                Rank(s.Underemployment, Peers(world, country.Id, c => c.MigrationPoverty != null && c.MigrationPoverty.Seeded ? c.MigrationPoverty.Underemployment : -1f), true), "UNEMPLOYMENT GAP ▸", true);
            book.Anchors["mig:homeless"] = Reading("HOMELESSNESS · " + Figure(s.HomelessPer10k, 0), "PER 10 000 · " + m.HomelessDefinition, "OECD AHD HC3.1.A1" + Year(m.HomelessYear), "SOURCED", Range(0f, 60f, true),
                Rank(s.HomelessPer10k, Peers(world, country.Id, c => c.MigrationPoverty != null && c.MigrationPoverty.Seeded ? c.MigrationPoverty.HomelessPer10k : -1f), true), "HOUSING LINE — PER HEAD ▸ · HOUSING OVERBURDEN ▸", true);
        }

        /// <summary>§732: the employment card the composition does not draw, kept and asked - the eight sectors' share of all employment, the rest OTHER.</summary>
        private static void AddEmployment(Book book, Country country)
        {
            float eight = 0f;
            var each = new List<string>();
            foreach (Sector sector in country.Sectors)
            {
                eight += sector.EmploymentShare;
                each.Add(DisplayName.Spaced(sector.Type.ToString()).ToUpperInvariant() + " " + Figure(sector.EmploymentShare, 1, "%"));
            }
            book.Anchors["section:employment"] = new SlipContent("EMPLOYMENT · SHARE BY SECTOR · [[DERIVED]]")
                .Add("EACH SEGMENT'S LENGTH IS THE SECTOR'S SHARE OF ALL EMPLOYMENT")
                .Add("LAWS › SECTORS CARRIES EACH SECTOR'S ROW");
            book.Anchors["jobs:card"] = new SlipContent("SHARE OF EMPLOYMENT · " + Figure(eight, 1, "%")).Add("THE EIGHT SECTORS' SHARE OF ALL EMPLOYMENT")
                .Add(string.Join(" · ", each))
                .Add("EMPLOYMENT · SHARE BY SECTOR · [[DERIVED]]");
            book.Anchors["jobs:other"] = new SlipContent("OTHER · " + Figure(System.Math.Max(0f, 100f - eight), 1, "%")).Add("EMPLOYMENT IN NO SECTOR OF THE EIGHT").Add("100 − THE EIGHT");
        }
    }
}
