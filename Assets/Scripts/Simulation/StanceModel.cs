using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>The CHES 2024 axes a bill can load (P3-A1's map). Openness is `eu_position` rescaled to 0–10 (R-CL2's trade axis).</summary>
    public enum StanceAxis
    {
        LrEcon,
        Galtan,
        SpendVsTax,
        ImmigratePolicy,
        Deregulation,
        Openness
    }

    /// <summary>
    /// P3-A1/A2 (2026-09-03): **what a bill concerns** - the signed move it makes on each CHES axis its dials
    /// belong to (positive toward the axis's 10 end, negative toward its 0 end, in the dial's own units), plus
    /// the spending lines it cuts for the opinion term. The axis weights are the bill's own arithmetic: each
    /// loaded axis carries the share of the bill's absolute movement it holds. Built by the
    /// `ParliamentSystem.Get*BillConcern` methods, one per bill kind, each stating its dial → axis pairings
    /// (the table in `COMPLETED.md` §246, sourced from the codebook's variable definitions).
    /// </summary>
    public sealed class BillConcern
    {
        private readonly Dictionary<StanceAxis, float> _moves = new Dictionary<StanceAxis, float>();
        /// <summary>The lines a bill cuts, as a share of the line (0–1), for the opinion term; empty for a bill that cuts nothing.</summary>
        public readonly List<(SpendingCategory? Category, WelfareProgramType? Program, float CutShare)> Cuts = new List<(SpendingCategory?, WelfareProgramType?, float)>();
        /// <summary>The legacy scalar direction the records and the lean bar keep (each bill kind's own sign convention).</summary>
        public float Direction;

        public IReadOnlyDictionary<StanceAxis, float> Moves => _moves;

        /// <summary>Add a signed move: <paramref name="toward10"/> positive pushes toward the axis's 10 end.</summary>
        public BillConcern Add(StanceAxis axis, float toward10)
        {
            if (Mathf.Approximately(toward10, 0f)) { return this; }
            _moves.TryGetValue(axis, out float current);
            _moves[axis] = current + toward10;
            return this;
        }

        public bool IsEmpty => _moves.Count == 0;

        /// <summary>The loaded axes with the end each move points at (0 or 10) and the share of the bill's movement it carries.</summary>
        public IEnumerable<(StanceAxis Axis, int End, float Weight)> Loaded()
        {
            float total = 0f;
            foreach (KeyValuePair<StanceAxis, float> kv in _moves) { total += Mathf.Abs(kv.Value); }
            if (total <= 0f) { yield break; }
            foreach (KeyValuePair<StanceAxis, float> kv in _moves)
            {
                yield return (kv.Key, kv.Value > 0f ? 10 : 0, Mathf.Abs(kv.Value) / total);
            }
        }

        /// <summary>
        /// The legacy path: a scalar direction on `BillAxis` - fiscal (expansionary positive → the services end of
        /// `spendvtax`) or trade (a tariff rise positive → the closed end of openness) - so every caller that still
        /// hands the scorer one float scores through the same model.
        /// </summary>
        public static BillConcern FromLegacy(float direction, BillAxis axis)
        {
            var concern = new BillConcern { Direction = direction };
            if (Mathf.Approximately(direction, 0f)) { return concern; }
            if (axis == BillAxis.Trade) { concern.Add(StanceAxis.Openness, -direction); }
            else { concern.Add(StanceAxis.SpendVsTax, -direction); }
            return concern;
        }
    }

    /// <summary>One party's stance on one bill, with the reasons that made it - P3-A3 draws them.</summary>
    public readonly struct PartyStance
    {
        public readonly PoliticalParty Party;
        public readonly int Seats;
        /// <summary>−1 … +1 after every term; the seat-weighted sum of these is the decided quantity.</summary>
        public readonly float Alignment;
        /// <summary>+1 FOR, −1 AGAINST, 0 UNDECIDED (inside the band) or UNMEASURED.</summary>
        public readonly int Side;
        public readonly bool Measured;
        public readonly IReadOnlyList<string> Reasons;

        public PartyStance(PoliticalParty party, int seats, float alignment, int side, bool measured, IReadOnlyList<string> reasons)
        {
            Party = party; Seats = seats; Alignment = alignment; Side = side; Measured = measured; Reasons = reasons;
        }
    }

    /// <summary>
    /// P3-A2 (2026-09-03): **the stance model** - `COMPLETED.md` §246 built. A party's stance on a bill is its
    /// position on the axis the bill loads (term 1), pulled by its government's cohesion or held by the
    /// opposition's line (term 2), and charged the public-opinion cost of cutting what its voters depend on
    /// (term 3); undecided is a state (term 4). Every position is CHES 2024 / GPS 2019; every weight is
    /// `[AUTHORED-DRAFT]` with its line; the salience is Eurobarometer 105 / Gallup (SOURCED); the voter profile
    /// is an ecological estimate from two SOURCED valkrets tables, and says so. Deterministic: arithmetic over
    /// the state, no stream drawn.
    /// </summary>
    public static class StanceModel
    {
        /// <summary>[AUTHORED-DRAFT] §246 D-P3-A1c: a party within half a point of the axis midpoint is UNDECIDED, a state and not a zero-sign artefact.</summary>
        public const float UndecidedBand = 0.1f;
        /// <summary>[AUTHORED-DRAFT] §246 term 2: how far a cabinet party is pulled toward its government's bill, scaled by its nearness to the bill.</summary>
        public const float Cohesion = 0.6f;
        /// <summary>[AUTHORED-DRAFT] §246 term 2: a support party (confidence and supply) takes this share of the cabinet's pull.</summary>
        public const float SupportPull = 0.5f;
        /// <summary>[AUTHORED-DRAFT] §246 term 2: how far an opposition party is pulled against a government bill that is nearer the government than itself.</summary>
        public const float OppositionLine = 0.3f;
        /// <summary>[AUTHORED-DRAFT] §246 term 3: the weight of the public-opinion cost.</summary>
        public const float OpinionWeight = 0.5f;
        /// <summary>[AUTHORED-DRAFT] §246 term 3: an issue outside the survey's top five names is unmeasured; it stands at this floor, and the reason says so.</summary>
        public const float SalienceFloor = 0.10f;

        // ------------------------------------------------------------------------------------------
        // Term 3's SOURCED salience: `ElectionsData/salience/issue_salience.md` - Standard Eurobarometer 105
        // (Spring 2026, QA3 "two most important issues", fieldwork 12 March–5 April 2026) for the EU five, Gallup
        // "Most Important Problem" (July 2026) for the USA - mapped onto IssueId as the campaign already maps it
        // (LiveCampaignSetup: Sweden's four). Items the issue set has no slot for (threats to democracy, Ukraine,
        // the international situation, the Middle East, government leadership) are dropped, as W-F3 records;
        // where two items map to one slot the larger stands. Everything else is at the floor.
        // ------------------------------------------------------------------------------------------
        private static readonly Dictionary<CountryId, Dictionary<IssueId, float>> Salience = new Dictionary<CountryId, Dictionary<IssueId, float>>
        {
            { CountryId.Sweden, new Dictionary<IssueId, float> { { IssueId.Climate, 0.26f }, { IssueId.Crime, 0.18f }, { IssueId.Defense, 0.17f }, { IssueId.Education, 0.16f } } },
            { CountryId.Poland, new Dictionary<IssueId, float> { { IssueId.Economy, 0.30f }, { IssueId.Defense, 0.26f }, { IssueId.Healthcare, 0.13f } } },
            { CountryId.Germany, new Dictionary<IssueId, float> { { IssueId.Economy, 0.36f }, { IssueId.Immigration, 0.14f } } },
            { CountryId.France, new Dictionary<IssueId, float> { { IssueId.Economy, 0.40f }, { IssueId.Healthcare, 0.17f }, { IssueId.Taxes, 0.16f }, { IssueId.Crime, 0.15f } } },
            { CountryId.Italy, new Dictionary<IssueId, float> { { IssueId.Economy, 0.31f }, { IssueId.Defense, 0.14f } } },
            { CountryId.USA, new Dictionary<IssueId, float> { { IssueId.Immigration, 0.12f }, { IssueId.Economy, 0.11f } } },
        };

        /// <summary>The country's salience for an issue, or the floor with <paramref name="measured"/> false when the survey's top five do not name it.</summary>
        public static float SalienceOf(CountryId country, IssueId issue, out bool measured)
        {
            if (Salience.TryGetValue(country, out Dictionary<IssueId, float> table) && table.TryGetValue(issue, out float s)) { measured = true; return s; }
            measured = false;
            return SalienceFloor;
        }

        // ------------------------------------------------------------------------------------------
        // Term 3's dependence table: what a spending line means to an age band - [AUTHORED-DRAFT], one row per
        // line with a clear beneficiary band (§246 D-P3-A1f); lines with none carry no dependence. Bands are the
        // substrate's five-year indices (0 = 0–4 … 20 = 100+).
        // ------------------------------------------------------------------------------------------
        private static readonly Dictionary<SpendingCategory, (int From, int To, IssueId Issue)> LineBands = new Dictionary<SpendingCategory, (int, int, IssueId)>
        {
            { SpendingCategory.SocialSecurity, (13, 20, IssueId.Economy) },
            { SpendingCategory.FederalRetirement, (13, 20, IssueId.Economy) },
            { SpendingCategory.Medicare, (13, 20, IssueId.Healthcare) },
            { SpendingCategory.Medicaid, (4, 12, IssueId.Healthcare) },
            { SpendingCategory.HHSDiscretionary, (13, 20, IssueId.Healthcare) },
            { SpendingCategory.IncomeSecurity, (4, 12, IssueId.Economy) },
            { SpendingCategory.Labor, (4, 12, IssueId.Economy) },
            { SpendingCategory.Education, (1, 4, IssueId.Education) },
            { SpendingCategory.Housing, (4, 8, IssueId.Housing) },
            { SpendingCategory.Justice, (0, 20, IssueId.Crime) },
            { SpendingCategory.Defense, (0, 20, IssueId.Defense) },
        };

        private static readonly Dictionary<WelfareProgramType, (int From, int To, IssueId Issue)> ProgramBands = new Dictionary<WelfareProgramType, (int, int, IssueId)>
        {
            { WelfareProgramType.UBI, (4, 12, IssueId.Economy) },
            { WelfareProgramType.NegativeIncomeTax, (4, 12, IssueId.Economy) },
            { WelfareProgramType.MeansTestedWelfare, (4, 12, IssueId.Economy) },
            { WelfareProgramType.UniversalHealthcare, (13, 20, IssueId.Healthcare) },
            { WelfareProgramType.HousingAssistance, (4, 8, IssueId.Housing) },
            { WelfareProgramType.ChildcareSubsidies, (5, 8, IssueId.Economy) },
        };

        /// <summary>A party's position on an axis, with the axis's own fallback (`spendvtax` → `lrecon`; openness → `lrecon` when the chamber has no EU item), or NaN when it has none.</summary>
        public static float Position(in PoliticalParty party, StanceAxis axis, bool opennessAvailable)
        {
            switch (axis)
            {
                case StanceAxis.LrEcon: return party.LrEcon;
                case StanceAxis.Galtan: return party.Galtan;
                case StanceAxis.SpendVsTax: return float.IsNaN(party.SpendVsTax) ? party.LrEcon : party.SpendVsTax;
                case StanceAxis.ImmigratePolicy: return party.ImmigratePolicy;
                case StanceAxis.Deregulation: return party.Deregulation;
                case StanceAxis.Openness: return opennessAvailable ? (float)CoalitionCompatibility.RescaleEu(party.EuPosition) : party.LrEcon;
                default: return float.NaN;
            }
        }

        /// <summary>The axis's column name as the codebook prints it, for a reason line.</summary>
        public static string AxisName(StanceAxis axis)
        {
            switch (axis)
            {
                case StanceAxis.LrEcon: return "lrecon";
                case StanceAxis.Galtan: return "galtan";
                case StanceAxis.SpendVsTax: return "spendvtax";
                case StanceAxis.ImmigratePolicy: return "immigrate_policy";
                case StanceAxis.Deregulation: return "deregulation";
                case StanceAxis.Openness: return "eu_position";
                default: return axis.ToString();
            }
        }

        /// <summary>
        /// Every seated party's stance on the bill, in the party system's order - the one enumeration the verdict,
        /// the seat map, the record and the breakdown all read.
        /// </summary>
        public static List<PartyStance> Stances(Country country, BillConcern concern)
        {
            var result = new List<PartyStance>();
            IReadOnlyList<PoliticalParty> parties = PartySystems.For(country.Id);
            if (parties == null) { return result; }
            bool opennessAvailable = ParliamentSystem.TradeAxisAvailable(country);
            var loaded = new List<(StanceAxis Axis, int End, float Weight)>(concern.Loaded());

            // The government context (term 2): the formed cabinet and its support, and whether this is a government bill.
            bool government = GovernmentFormation.TryGovernment(country, out IReadOnlyList<string> cabinet, out IReadOnlyList<string> support);
            bool governmentBill = government && !string.IsNullOrEmpty(country.PlayerPartyAbbrev) && cabinet.Contains(country.PlayerPartyAbbrev);
            float[] cabinetPosition = governmentBill ? CabinetPositions(country, parties, cabinet, loaded, opennessAvailable) : null;

            foreach (PoliticalParty party in parties)
            {
                int seats = country.ParliamentSeats.TryGetValue(party.Abbrev, out int s) ? s : 0;
                if (seats <= 0) { continue; }
                var reasons = new List<string>();

                // Term 1: the position on each loaded axis, weighted by the bill's own share of movement.
                float alignment = 0f;
                float measuredWeight = 0f;
                for (int i = 0; i < loaded.Count; i++)
                {
                    float p = Position(party, loaded[i].Axis, opennessAvailable);
                    if (float.IsNaN(p))
                    {
                        reasons.Add($"no published {AxisName(loaded[i].Axis)} position");
                        continue;
                    }
                    float a = (loaded[i].End == 10 ? p - 5f : 5f - p) / 5f;
                    alignment += loaded[i].Weight * a;
                    measuredWeight += loaded[i].Weight;
                    reasons.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1:0.0} on the {2} axis, the bill toward its {3} end: {4:+0.00;-0.00}",
                        party.Abbrev, p, AxisName(loaded[i].Axis), loaded[i].End, a));
                }
                if (loaded.Count == 0 || measuredWeight <= 0f)
                {
                    result.Add(new PartyStance(party, seats, 0f, 0, false, reasons));
                    continue;
                }
                alignment /= measuredWeight;   // measured axes only; the unmeasured ones say so above

                // Term 2: cohesion or the opposition's line, on a government bill.
                if (governmentBill)
                {
                    bool inCabinet = cabinet.Contains(party.Abbrev);
                    bool inSupport = !inCabinet && support.Contains(party.Abbrev);
                    if (inCabinet || inSupport)
                    {
                        float distance = Distance(party, loaded, opennessAvailable);
                        float pull = Cohesion * (inSupport ? SupportPull : 1f) * (1f - distance);
                        float before = alignment;
                        alignment += pull * (1f - alignment);
                        reasons.Add(string.Format(CultureInfo.InvariantCulture, "{0} party: cohesion {1:0.00} at distance {2:0.00} from the bill → {3:+0.00;-0.00}",
                            inCabinet ? "cabinet" : "support", pull, distance, alignment - before));
                        if (distance > 0.5f) { reasons.Add("far from its own position - the pull is small and it may refuse"); }
                    }
                    else
                    {
                        float mine = Distance(party, loaded, opennessAvailable);
                        float governments = 0f;
                        for (int i = 0; i < loaded.Count; i++) { governments += loaded[i].Weight * Mathf.Abs(cabinetPosition[i] - loaded[i].End) / 10f; }
                        if (mine < governments)
                        {
                            reasons.Add(string.Format(CultureInfo.InvariantCulture, "opposition, but the bill is nearer this party ({0:0.00}) than the government ({1:0.00}): votes its position", mine, governments));
                        }
                        else
                        {
                            float before = alignment;
                            alignment -= OppositionLine * (1f + alignment);
                            reasons.Add(string.Format(CultureInfo.InvariantCulture, "opposition line: the bill is nearer the government ({0:0.00}) than this party ({1:0.00}) → {2:+0.00;-0.00}", governments, mine, alignment - before));
                        }
                    }
                }

                // Term 3: the public-opinion cost of the lines the bill cuts. Each cut prints its arithmetic when it
                // moves the alignment by a hundredth or more; the rest are named once with their total, so a screen
                // is not eight lines of "+0.00" (P3-A3's first film).
                if (concern.Cuts.Count > 0)
                {
                    float[] voters = VoterProfile(country.Id, party.Abbrev, out bool ecological);
                    var quiet = new List<string>();
                    float quietCost = 0f;
                    foreach ((SpendingCategory? category, WelfareProgramType? program, float cutShare) in concern.Cuts)
                    {
                        if (cutShare <= 0f) { continue; }
                        (int From, int To, IssueId Issue) band;
                        string line;
                        if (category.HasValue && LineBands.TryGetValue(category.Value, out band)) { line = category.Value.ToString(); }
                        else if (program.HasValue && ProgramBands.TryGetValue(program.Value, out band)) { line = program.Value.ToString(); }
                        else { continue; }
                        float salience = SalienceOf(country.Id, band.Issue, out bool salienceMeasured);
                        float dependence = voters == null ? 1f : BandShare(voters, band.From, band.To);
                        float cost = OpinionWeight * salience * dependence * Mathf.Clamp01(cutShare);
                        if (cost <= 0f) { continue; }
                        alignment -= cost;
                        if (cost < 0.005f) { quiet.Add(line); quietCost += cost; continue; }
                        string bands = band.From == 0 && band.To >= PopulationCohorts.OpenBandIndex ? "every band" : "bands " + PopulationCohorts.Label(band.From) + "–" + PopulationCohorts.Label(band.To);
                        reasons.Add(string.Format(CultureInfo.InvariantCulture, "cuts {0} by {1:P0}: its voters {2} in {3}, {4} salience {5:0.00}{6} → {7:+0.00;-0.00}",
                            line, cutShare, voters == null ? "(no voter profile for this chamber)" : string.Format(CultureInfo.InvariantCulture, "{0:P0}", dependence),
                            bands, band.Issue, salience, salienceMeasured ? " (EB105)" : " (floor: not in the survey's top five)", -cost));
                    }
                    if (quiet.Count > 0)
                    {
                        reasons.Add(string.Format(CultureInfo.InvariantCulture, "opinion cost of cutting {0}: {1:-0.000} at the authored weight - below a hundredth", string.Join(", ", quiet), -quietCost));
                    }
                    if (voters != null && ecological && concern.Cuts.Count > 0) { reasons.Add("voter profile: ecological, from 2022 valkrets returns over 2024 pyramids"); }
                }

                alignment = Mathf.Clamp(alignment, -1f, 1f);
                int side = Mathf.Abs(alignment) < UndecidedBand ? 0 : alignment > 0f ? 1 : -1;
                if (side == 0) { reasons.Add(string.Format(CultureInfo.InvariantCulture, "undecided: |{0:0.00}| inside the band {1:0.00}", alignment, UndecidedBand)); }
                result.Add(new PartyStance(party, seats, alignment, side, true, reasons));
            }
            return result;
        }

        /// <summary>
        /// P3-A3: the stance's reasons as one line for a screen - the position term first, then the term that moved
        /// it (cohesion, the opposition's line, an opinion cost), then the undecided note; the provenance notes
        /// ("voter profile: ecological …") are left to the record's full list.
        /// </summary>
        public static string ReasonLine(in PartyStance stance)
        {
            if (stance.Reasons == null || stance.Reasons.Count == 0) { return stance.Measured ? string.Empty : "no published position on this bill's axis"; }
            var parts = new List<string>();
            foreach (string reason in stance.Reasons)
            {
                if (reason.StartsWith("voter profile:", StringComparison.Ordinal)) { continue; }
                parts.Add(reason);
            }
            return string.Join(" · ", parts);
        }

        /// <summary>
        /// P3-A3: the reasons as a SHORT line for a plate - each term's contribution with a two-word name
        /// ("spendvtax 2.5 → +0.50 · opposition line −0.15 · cuts SocialSecurity −0.00") - the full line is
        /// <see cref="ReasonLine"/>. Built from the same reasons, so the two cannot disagree.
        /// </summary>
        public static string ReasonShort(in PartyStance stance)
        {
            if (stance.Reasons == null || stance.Reasons.Count == 0) { return stance.Measured ? string.Empty : "no published position on this axis"; }
            var parts = new List<string>();
            foreach (string reason in stance.Reasons)
            {
                int arrow = reason.LastIndexOf('→');
                string tail = arrow >= 0 ? reason.Substring(arrow + 1).Trim() : null;
                if (reason.StartsWith("voter profile:", StringComparison.Ordinal)) { continue; }
                if (reason.Contains(" on the ") && reason.Contains(" axis, the bill toward its "))
                {
                    // "S 2.5 on the spendvtax axis, the bill toward its 0 end: +0.50" → "spendvtax 2.5 → +0.50"
                    int on = reason.IndexOf(" on the ", StringComparison.Ordinal);
                    int axisEnd = reason.IndexOf(" axis", on, StringComparison.Ordinal);
                    int colon = reason.LastIndexOf(':');
                    string position = reason.Substring(0, on).Trim();
                    position = position.Substring(position.LastIndexOf(' ') + 1);
                    parts.Add(reason.Substring(on + 8, axisEnd - on - 8) + " " + position + " → " + (colon >= 0 ? reason.Substring(colon + 1).Trim() : string.Empty));
                }
                else if (reason.StartsWith("cabinet party", StringComparison.Ordinal) || reason.StartsWith("support party", StringComparison.Ordinal)) { parts.Add((reason.StartsWith("cabinet", StringComparison.Ordinal) ? "cohesion " : "support pull ") + tail); }
                else if (reason.StartsWith("opposition line", StringComparison.Ordinal)) { parts.Add("opposition line " + tail); }
                else if (reason.StartsWith("opposition, but", StringComparison.Ordinal)) { parts.Add("nearer than the government: own position"); }
                else if (reason.StartsWith("far from its own position", StringComparison.Ordinal)) { parts.Add("far - may refuse"); }
                else if (reason.StartsWith("cuts ", StringComparison.Ordinal)) { parts.Add(reason.Substring(0, reason.IndexOf(" by ", StringComparison.Ordinal)) + " " + tail); }
                else if (reason.StartsWith("opinion cost", StringComparison.Ordinal)) { parts.Add("opinion cost below 0.01"); }
                else if (reason.StartsWith("undecided", StringComparison.Ordinal)) { parts.Add("undecided: inside the band"); }
                else if (reason.StartsWith("no published", StringComparison.Ordinal)) { parts.Add(reason); }
            }
            return string.Join(" · ", parts);
        }

        /// <summary>The party's weighted distance from the ends the bill moves toward, 0 (at the end) to 1 (at the far end), over the measured axes.</summary>
        private static float Distance(in PoliticalParty party, List<(StanceAxis Axis, int End, float Weight)> loaded, bool opennessAvailable)
        {
            float distance = 0f, weight = 0f;
            for (int i = 0; i < loaded.Count; i++)
            {
                float p = Position(party, loaded[i].Axis, opennessAvailable);
                if (float.IsNaN(p)) { continue; }
                distance += loaded[i].Weight * Mathf.Abs(p - loaded[i].End) / 10f;
                weight += loaded[i].Weight;
            }
            return weight > 0f ? distance / weight : 1f;
        }

        /// <summary>The cabinet's seat-weighted position on each loaded axis (the opposition's comparison).</summary>
        private static float[] CabinetPositions(Country country, IReadOnlyList<PoliticalParty> parties, IReadOnlyList<string> cabinet,
            List<(StanceAxis Axis, int End, float Weight)> loaded, bool opennessAvailable)
        {
            var positions = new float[loaded.Count];
            for (int i = 0; i < loaded.Count; i++)
            {
                float sum = 0f, seats = 0f;
                foreach (PoliticalParty party in parties)
                {
                    if (!cabinet.Contains(party.Abbrev)) { continue; }
                    float p = Position(party, loaded[i].Axis, opennessAvailable);
                    if (float.IsNaN(p)) { continue; }
                    int held = country.ParliamentSeats.TryGetValue(party.Abbrev, out int s) ? s : 0;
                    sum += held * p;
                    seats += held;
                }
                positions[i] = seats > 0f ? sum / seats : 5f;
            }
            return positions;
        }

        private static bool VoterProfileAvailable(CountryId country) => country == CountryId.Sweden;

        private static readonly Dictionary<string, float[]> ProfileCache = new Dictionary<string, float[]>();

        /// <summary>
        /// A party's voters' age profile - the share of its voters' communities in each five-year band, 0–20 -
        /// the ecological estimate §246 names: the party's 2022 votes per valkrets (`SwedishValkretsReturns2022`,
        /// Valmyndigheten) weighting each valkrets's 2024 pyramid (`SwedishValkretsPopulation2024`, SCB), over
        /// the bands of voting age (18+, the 15–19 band pro rata). Null where no table exists (every chamber but
        /// Sweden), and the reason line says so.
        /// </summary>
        public static float[] VoterProfile(CountryId country, string abbrev, out bool ecological)
        {
            ecological = true;
            if (!VoterProfileAvailable(country)) { return null; }
            if (ProfileCache.TryGetValue(abbrev, out float[] cached)) { return cached; }
            int partyIndex = Array.IndexOf(SwedishValkretsReturns2022.Parties, abbrev);
            if (partyIndex < 0) { return null; }
            var profile = new float[PopulationCohorts.CohortCount];
            double total = 0.0;
            for (int v = 0; v < SwedishValkretsReturns2022.Votes.Length && v < SwedishValkretsPopulation2024.Bands.Length; v++)
            {
                long votes = SwedishValkretsReturns2022.Votes[v][partyIndex];
                long[] bands = SwedishValkretsPopulation2024.Bands[v];
                long population = 0;
                for (int b = 0; b < bands.Length; b++) { population += bands[b]; }
                if (population <= 0) { continue; }
                for (int b = 3; b < bands.Length && b < profile.Length; b++)
                {
                    double eligibleShare = b == 3 ? 2.0 / 5.0 : 1.0;   // 18 and 19 of the 15–19 band, pro rata
                    double weight = votes * (bands[b] * eligibleShare / (double)population);
                    profile[b] += (float)weight;
                    total += weight;
                }
            }
            if (total <= 0.0) { return null; }
            for (int b = 0; b < profile.Length; b++) { profile[b] = (float)(profile[b] / total); }
            ProfileCache[abbrev] = profile;
            return profile;
        }

        private static float BandShare(float[] profile, int from, int to)
        {
            float share = 0f;
            for (int b = Mathf.Max(0, from); b <= to && b < profile.Length; b++) { share += profile[b]; }
            return share;
        }
    }
}
