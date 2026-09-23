using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// P4-B1 (Playtest 4, 2026-09-04): THE RANGE-CAPTION CATALOG. For every dial the label check reads, ten bands over
    /// its range, each with a name and one dry line - the desk's own civil-service deadpan. ⚠ [AUTHORED] game fiction,
    /// every line: nothing here is a figure, a source or a claim about the world, and `MetaTextCheck` exempts this
    /// file by name so its satire is not read as meta-text.
    ///
    /// <para><b>Authored FROM the effect table, never against it.</b> Each dial names the stat its captions speak to
    /// and the sign that stat takes when the dial rises (`RiseSign`), read off the model's own couplings
    /// (`LaborCouplings.All`, MacroSystem's sector sensitivities, the tariff take, the drawdown) and asserted by
    /// `RangeCaptionCheck`: every band above the dial's neutral level carries the rising sign, every band below it
    /// the opposite, and a line contradicting what the model does at that range fails the bar. The neutral level is
    /// the dial's own zero-gap point - 50 for the 0–100 dials, the country's own baseline for the minimum wage, 0
    /// for a tariff or a drawdown - and the band holding it carries sign 0. Two dials carry "contested" edges in the
    /// coupling table (the minimum wage's employment effect, overtime's work-sharing) and their lines say
    /// "the literature disagrees" rather than pretend.</para>
    ///
    /// <para><b>The keys are the dial names as `DrawDialRow` prints them</b>, so the presentation finds a dial's bands by
    /// the same string the label check reads, and a renamed dial loses its captions loudly (the check counts them).</para>
    /// </summary>
    public static class RangeCaptions
    {
        /// <summary>One band: its name, its one line, and the sign it claims on the dial's stat relative to the neutral band (−1, 0, +1).</summary>
        public readonly struct Band
        {
            public readonly string Name;
            public readonly string Line;
            public readonly int Sign;
            public Band(string name, string line, int sign) { Name = name; Line = line; Sign = sign; }
        }

        /// <summary>One dial's catalog: the stat its lines speak to, the sign that stat takes when the dial rises, where its neutral level sits in the range (0–1), and its ten bands from the range's floor to its ceiling.</summary>
        public sealed class Dial
        {
            public readonly string Key;
            public readonly string Stat;
            public readonly int RiseSign;
            public readonly float NeutralFraction;
            public readonly Band[] Bands;
            public Dial(string key, string stat, int riseSign, float neutralFraction, Band[] bands) { Key = key; Stat = stat; RiseSign = riseSign; NeutralFraction = neutralFraction; Bands = bands; }
        }

        /// <summary>
        /// The band a value falls in: the range in ten equal parts, each band holding its UPPER edge - (0, 10] is band 0
        /// on a 0–100 dial, (40, 50] band 4 - so the neutral value of a 0–100 dial (50) sits in the fifth band, the
        /// catalog's *Customary*, and the floor sits in the first. The floor itself is band 0.
        /// </summary>
        public static int BandIndex(float value, float min, float max)
        {
            if (max <= min) { return 0; }
            float t = Mathf.Clamp01((value - min) / (max - min));
            return Mathf.Clamp(Mathf.CeilToInt(t * 10f) - 1, 0, 9);
        }

        /// <summary>The band the dial's neutral level falls in, by the same rule - the check's zero band.</summary>
        public static int NeutralBand(Dial dial) => BandIndex(dial.NeutralFraction, 0f, 1f);

        public static bool TryGet(string dialKey, out Dial dial) => Catalog.TryGetValue(dialKey, out dial);

        public static IEnumerable<Dial> All => Catalog.Values;

        /// <summary>PF-2 (§592): CONVENTION - the longest line a spending row's caption may carry, in characters: the narrowest lane its instruments leave at 1280, 171.9 px at 7.2 px a character (dry591).</summary>
        public const int SpendingLineCaptionMax = 23;

        private static Band B(string name, string line, int sign) => new Band(name, line, sign);

        // Sign convention per dial: +1 = the band claims the Stat is HIGHER than at the neutral level, −1 lower, 0 the
        // neutral band. RiseSign is the model's: the Stat rises (+1) or falls (−1) as the dial rises.
        private static readonly Dictionary<string, Dial> Catalog = new Dictionary<string, Dial>
        {
            // LaborCouplings: MinimumWage → UnemploymentRate (+, contested), PovertyRate (−), Gini (−). The stat the
            // captions speak to is the poverty rate, the effect the dial is set for; unemployment is named where it bites.
            // Neutral = the country's own baseline Kaitz (the caller passes the standing baseline as the neutral point).
            { "Minimum Wage", new Dial("Minimum Wage", "poverty rate", -1, 0.5f, new[]
            {
                B("Vestigial", "A floor nobody stands on. Poverty is left to its own devices.", +1),
                B("Nominal", "The statute exists. Employers have not noticed.", +1),
                B("Modest", "Some at the bottom notice the difference; the rest notice the price of bread.", +1),
                B("Restrained", "Below the country's own custom. Poverty edges up; nobody writes a letter.", +1),
                B("Customary", "Where the country has always kept it. Nothing moves, which is the point.", 0),
                B("Firm", "A little above custom. Poverty eases; the literature disagrees about the jobs.", -1),
                B("Generous", "The low-paid are less poor. Some of them are also less employed.", -1),
                B("Ambitious", "Poverty falls further. The employers' association requests a meeting.", -1),
                B("Bold", "A wage floor the median worker can see from below. Fewer poor; fewer posts.", -1),
                B("Heroic", "The floor meets the median. Poverty is abolished by decree; so are some jobs.", -1),
            }) },
            // LaborCouplings: PaidFamilyLeave → LaborForceParticipation (+), ApprovalRating (+). Neutral = 0 weeks.
            { "Paid Family Leave", new Dial("Paid Family Leave", "labour-force participation", +1, 0f, new[]
            {
                B("None", "Parents take their chances. The participation rate does likewise.", 0),
                B("Token", "A fortnight, on paper. Participation barely stirs.", +1),
                B("Brief", "A season's leave. A few parents come back who would not have.", +1),
                B("Adequate", "Long enough to matter; short enough to remember the office.", +1),
                B("Comfortable", "Participation rises. So does the approval of anyone with a pram.", +1),
                B("Ample", "Half a year. Employers keep the desk warm.", +1),
                B("Generous", "The rate climbs; the payroll office learns a new form.", +1),
                B("Lavish", "Most of a year. Participation is up; so is the cost of the form.", +1),
                B("Nordic", "A year, near enough. The participation rate approves.", +1),
                B("Sabbatical", "Two years. The child will walk before the parent returns; participation holds.", +1),
            }) },
            // LaborCouplings: OvertimeRegulation → UnemploymentRate (−, contested: work-sharing). Neutral 50.
            { "Overtime / Working-Hour Regulation", new Dial("Overtime / Working-Hour Regulation", "unemployment rate", -1, 0.5f, new[]
            {
                B("Unregulated", "Hours are a private matter. Unemployment is not, but sits a little higher.", +1),
                B("Loose", "A ceiling nobody reaches. The dole queue is a touch longer.", +1),
                B("Light", "Overtime is permitted, mostly. Fewer hires than hours.", +1),
                B("Mild", "Below the customary rule. Employers stretch the staff they have.", +1),
                B("Customary", "The hours the country already keeps. Nothing moves.", 0),
                B("Firm", "Overtime costs a premium. A few shifts become a few jobs; the literature disagrees.", -1),
                B("Strict", "Work is shared out. Unemployment eases; so does the overtime budget.", -1),
                B("Tight", "The working week shortens. More names on the payroll, fewer hours on each.", -1),
                B("Severe", "Hours are rationed. Unemployment falls; the foreman counts minutes.", -1),
                B("Absolute", "Nobody works late. Unemployment is lower; the lights go off at five.", -1),
            }) },
            // LaborCouplings: RetrainingProgram → UnemploymentRate (−), LaborForceParticipation (+). Neutral 50.
            { "Workforce Retraining Programs", new Dial("Workforce Retraining Programs", "unemployment rate", -1, 0.5f, new[]
            {
                B("None", "The unemployed retrain themselves, or do not. Unemployment sits higher.", +1),
                B("Token", "A pamphlet and a waiting list. Unemployment is not impressed.", +1),
                B("Sparse", "A course a year, somewhere. Unemployment a little above custom.", +1),
                B("Thin", "Below the country's usual effort. The dole queue notices.", +1),
                B("Customary", "The programmes the country already runs. Nothing moves.", 0),
                B("Active", "Courses fill. Unemployment eases; participation stirs.", -1),
                B("Energetic", "Retraining is a second job. Unemployment falls further.", -1),
                B("Ambitious", "Every idle hand is offered a syllabus. Unemployment down; classrooms full.", -1),
                B("Sweeping", "The workforce is perpetually in school. Unemployment low; tutors scarce.", -1),
                B("Total", "Nobody is unemployed for long, or unenrolled for long. Unemployment at its floor.", -1),
            }) },
            // LaborCouplings: FamilyPolicy → BirthRate (+). Neutral 50.
            { "Family Policy", new Dial("Family Policy", "birth rate", +1, 0.5f, new[]
            {
                B("Minimal", "Children are a private venture. The birth rate is lower than custom.", -1),
                B("Sparse", "A one-off grant. The birth rate declines to notice.", -1),
                B("Thin", "Some support, late. Births a little below custom.", -1),
                B("Restrained", "Below what the country usually offers. Cots stay in the shop.", -1),
                B("Customary", "The country's usual provision. Births as before.", 0),
                B("Supportive", "Nurseries and allowances. The birth rate lifts a little.", +1),
                B("Encouraging", "Parents are noticed. Births rise; so does the nursery bill.", +1),
                B("Generous", "The state is fond of children. Births up; the maternity ward books ahead.", +1),
                B("Lavish", "A pram with every ballot. Births rise further.", +1),
                B("Pro-natalist", "Population policy, openly. The birth rate is the highest the dial reaches.", +1),
            }) },
            // LaborCouplings: ImmigrationPolicy → NetMigrationRate (+, more open). Neutral 50.
            { "Immigration Policy", new Dial("Immigration Policy", "net migration", +1, 0.5f, new[]
            {
                B("Closed", "The border is a wall with a form. Net migration at its lowest.", -1),
                B("Restrictive", "Few are admitted; fewer stay. Net migration low.", -1),
                B("Guarded", "A points system with a long queue. Migration below custom.", -1),
                B("Cautious", "Below the country's usual openness. The queue grows abroad.", -1),
                B("Customary", "The country's usual door. Net migration as before.", 0),
                B("Receptive", "The door is ajar. Net migration rises.", +1),
                B("Welcoming", "Arrivals outnumber departures more clearly.", +1),
                B("Open", "The queue moves. Net migration well above custom.", +1),
                B("Liberal", "Most who apply arrive. Net migration high.", +1),
                B("Unrestricted", "The border is a line on a map. Net migration at the dial's ceiling.", +1),
            }) },
            // TradeSystem: the take is imports × rate; partners mirror an override's excess; the pass-through reaches prices for a year. Neutral = 0 %.
            { "General Base Tariff", new Dial("General Base Tariff", "tariff take", +1, 0f, new[]
            {
                B("Free", "No duty at the port. The treasury collects nothing there.", 0),
                B("Nominal", "A few per cent. The take is a rounding line.", +1),
                B("Light", "Duty is paid and mostly forgotten. A small take.", +1),
                B("Modest", "The take is visible in the accounts; prices, a little.", +1),
                B("Firm", "Importers write to the minister. The take grows; so do shelf prices.", +1),
                B("Protective", "Domestic producers send flowers. The take is real; the pass-through, too.", +1),
                B("Heavy", "Trade slows at the gate. The take rises; partners take note.", +1),
                B("Punitive", "Partners answer in kind. The take is large; the bilateral flows are not.", +1),
                B("Fortress", "Imports are a luxury. A great take on a shrinking base.", +1),
                B("Wall", "Half of everything at the border. The take peaks; the shelves thin.", +1),
            }) },
            // The per-partner override: the same take on one partner, the excess over the standing rate mirrored back. Neutral = 0 %.
            { "Override rate", new Dial("Override rate", "tariff take on this partner", +1, 0f, new[]
            {
                B("None", "This partner pays the standing rate. Nothing to mirror.", 0),
                B("Nominal", "A few points above the rate. The partner shrugs.", +1),
                B("Light", "A small excess. The partner's customs office mirrors it, small.", +1),
                B("Modest", "The take rises on this partner; so does theirs on us.", +1),
                B("Firm", "A dispute in the making. The take grows; the excess comes back.", +1),
                B("Protective", "This partner's exporters send a delegation.", +1),
                B("Heavy", "The take is large; the mirrored duty is too.", +1),
                B("Punitive", "Trade with this partner is a negotiation by other means.", +1),
                B("Fortress", "Little crosses this border either way. A great take on little.", +1),
                B("Wall", "Half of everything from this partner. The excess returns in full.", +1),
            }) },
            // SwfDrawdownBill: the withdrawal is revenue now, the fund smaller after. Neutral = 0 % of GDP.
            { "Fund drawdown", new Dial("Fund drawdown", "revenue this year", +1, 0f, new[]
            {
                B("None", "The fund is left to compound. The treasury draws nothing.", 0),
                B("Token", "A sliver of the fund. Revenue rises by a rounding line.", +1),
                B("Prudent", "A year's returns, roughly. The fund does not notice.", +1),
                B("Measured", "Revenue rises; the fund's managers write a memo.", +1),
                B("Firm", "A real draw. The deficit narrows this year and the fund next.", +1),
                B("Heavy", "The fund is a budget line now. Revenue up; posterity down.", +1),
                B("Deep", "A tenth of GDP from the fund. The memo becomes a letter.", +1),
                B("Emergency", "The kind of draw that names its year. Revenue high; the fund is thinner.", +1),
                B("Drastic", "The fund is spent on this year. The letter becomes a resignation.", +1),
                B("Liquidation", "A quarter of GDP in one draw. Revenue at its peak; the fund at its knees.", +1),
            }) },
            // MacroSystem: SectorSubsidySensitivity (+ output share). Neutral 50.
            { "Subsidy", new Dial("Subsidy", "the sector's output", +1, 0.5f, new[]
            {
                B("None", "The sector stands on its own. Output below the customary share.", -1),
                B("Token", "A grant the sector frames rather than spends.", -1),
                B("Sparse", "Below the usual support. Output a little under custom.", -1),
                B("Thin", "The sector notices what it no longer gets.", -1),
                B("Customary", "The country's usual support. Output as before.", 0),
                B("Supportive", "Output rises a little; the sector's lobby sends a card.", +1),
                B("Generous", "The sector grows on the treasury's account.", +1),
                B("Lavish", "Output well above custom; the treasury's, less so.", +1),
                B("Sponsoring", "The state is the sector's largest customer.", +1),
                B("Nationalised in all but name", "Output at the dial's ceiling; the sector is a department.", +1),
            }) },
            // MacroSystem: SectorRegulationSensitivity (− output share), the gap from the sector's OWN seeded anchor. Neutral = the anchor (50 at every seed).
            { "Regulation", new Dial("Regulation", "the sector's output", -1, 0.5f, new[]
            {
                B("Light", "Rules fit on a postcard. Output above custom; so is the risk.", +1),
                B("Loose", "Inspectors are rare. Output a little higher.", +1),
                B("Lenient", "Below the sector's usual rulebook. Output up; complaints, later.", +1),
                B("Relaxed", "A thinner code than the sector is used to. Output edges up.", +1),
                B("Customary", "The sector's own rulebook, as seeded. Output as before.", 0),
                B("Firm", "More forms; slightly less output.", -1),
                B("Strict", "The rulebook thickens. Output slips.", -1),
                B("Heavy", "Compliance is a career. Output well below custom.", -1),
                B("Onerous", "The sector spends its mornings on paperwork.", -1),
                B("Suffocating", "Every act licensed. Output at the dial's floor.", -1),
            }) },
            // MacroSystem: SectorTaxCreditSensitivity (+ output share). Neutral 50.
            { "Tax Credits", new Dial("Tax Credits", "the sector's output", +1, 0.5f, new[]
            {
                B("None", "The sector pays in full. Output below custom.", -1),
                B("Token", "A credit worth the accountant's fee.", -1),
                B("Sparse", "Below the usual relief. Output a little lower.", -1),
                B("Thin", "The sector's accountants notice the difference first.", -1),
                B("Customary", "The relief the sector already enjoys. Output as before.", 0),
                B("Helpful", "Output edges up; the credit is claimed on the first day.", +1),
                B("Generous", "The sector invests the relief. Output rises.", +1),
                B("Lavish", "Output well above custom; the revenue line, below.", +1),
                B("Extravagant", "The credit exceeds the tax. Output high.", +1),
                B("Untaxed in effect", "Output at the ceiling; the sector files for the pleasure of it.", +1),
            }) },
            // MacroSystem: SectorResearchGrantsSensitivity (+ output share). Neutral 50.
            { "Research Grants", new Dial("Research Grants", "the sector's output", +1, 0.5f, new[]
            {
                B("None", "The sector's laboratories are dark. Output below custom.", -1),
                B("Token", "A prize, annually. Output a touch lower.", -1),
                B("Sparse", "Below the usual funding. Fewer patents; less output.", -1),
                B("Thin", "The researchers write proposals instead of papers.", -1),
                B("Customary", "The grants the sector already receives. Output as before.", 0),
                B("Active", "Output edges up as the laboratories fill.", +1),
                B("Generous", "The sector publishes. Output rises.", +1),
                B("Ambitious", "Output well above custom; the results, some years out.", +1),
                B("Lavish", "Every idea funded. Output high; some ideas were bad.", +1),
                B("Moonshot", "Output at the ceiling; the sector names a laboratory after the minister.", +1),
            }) },
            // MacroSystem: SectorDeregulationSensitivity (+ output share, − employment share as the dial rises toward deregulated). Neutral 50.
            { "Nationalization / Deregulation", new Dial("Nationalization / Deregulation", "the sector's output", +1, 0.5f, new[]
            {
                B("Nationalised", "The state owns the sector. Output below custom; employment above it.", -1),
                B("Public", "Mostly state-run. Output a little lower; the payroll longer.", -1),
                B("Directed", "Private in name, public in practice. Output under custom.", -1),
                B("Guided", "A little more state than the sector is used to. Output edges down.", -1),
                B("Customary", "The sector's usual mix. Output as before.", 0),
                B("Liberalised", "Fewer licences. Output up; the payroll a little shorter.", +1),
                B("Open", "The market decides more. Output rises; employment slips.", +1),
                B("Deregulated", "Output well above custom; the sector employs fewer to make more.", +1),
                B("Unfettered", "The rulebook is a pamphlet. Output high; jobs fewer.", +1),
                B("Laissez-faire", "Output at the ceiling; the sector runs itself, with fewer hands.", +1),
            }) },
            // P5-B5 (2026-09-05): the spending rows carry the FIGURE (P5-B2's nominal line); the track is the year's allowed
            // change around the standing amount (±30 % Discretionary, ±15 % Mandatory), the standing tick at its centre, so
            // the neutral band is the fifth and the stat is the line's own provision - it rises with the dial by construction
            // (ApplySpendingLineChanges sets the amount to the target). Two catalogs because the same band is a different
            // deed on an entitlement than on a programme.
            // PF-2 (2026-09-23, §592): SHORT LINES. The lane between a spending row's instruments is a third of a dial's, and the first twenty lines (58-95 characters)
            // never drew at 1280 or 2560; cut to what the lane the instruments leave holds - SpendingLineCaptionMax, asserted by RangeCaptionCheck - same names, same signs. The first cut (25-37 characters) still overran lanes of 172-228 px on film591: the narrowest lane at 1280 holds 23 characters of the 12 px caption face, so 23 is the cap.
            { "Discretionary line", new Dial("Discretionary line", "the line's provision", +1, 0.5f, new[]
            {
                B("Gutted", "A third gone overnight.", -1),
                B("Slashed", "Quarter off. Closures.", -1),
                B("Cut", "Economies, then none.", -1),
                B("Trimmed", "A shade under. Unseen.", -1),
                B("As it stands", "The index did this.", 0),
                B("Topped up", "A shade over; noted.", +1),
                B("Raised", "Something new opens.", +1),
                B("Expanded", "A fifth more. Noticed.", +1),
                B("Surged", "A quarter more. Frowns.", +1),
                B("Doubled down", "A signature third more.", +1),
            }) },
            { "Mandatory line", new Dial("Mandatory line", "the line's provision", +1, 0.5f, new[]
            {
                B("Clawed back", "A seventh off. Letters.", -1),
                B("Cut", "A promise cut.", -1),
                B("Tightened", "Narrower. Litigated.", -1),
                B("Trimmed", "A shade under. Felt.", -1),
                B("As it stands", "Its cohort did this.", 0),
                B("Uprated", "An honest uprating.", +1),
                B("Raised", "A real rise. Noticed.", +1),
                B("Widened", "A tenth more. It stays.", +1),
                B("Enlarged", "Big rise, every year.", +1),
                B("Recast", "A new social contract.", +1),
            }) },
            // ---- P6-F2b (2026-09-21, §542): THE ENERGY TAB'S FOUR INSTRUMENTS - the Energy sector's own dials under the names the spec-let's S9 maps them to,
            // each speaking to what the instrument reaches in the energy layer. Same ranges, same drafts, same save keys as the sector's rows above.
            // Retail intervention = the Subsidy dial's money side: the budget's energy line rises with it and the levy is cut one for one (EnergyLedger.Compute's
            // LevyScale falls as the line rises) - the stat is the levy on the bill, and it FALLS as the dial rises. Drawn only where the stack carries a levy.
            // PF-4 (2026-09-21, §554): THE LINES PROMISE A DIRECTION, NEVER A FLOOR. The dial's whole cost is a fixed share of GDP and the four seeded levies differ by two orders of
            // magnitude: the full dial leaves most of Italy's and Poland's levy standing and takes Sweden's and France's whole levy a few points above neutral. One catalog serves
            // four stacks, so a line above neutral that can stand where the levy is already gone SAYS SO ("or is gone"), none names a floor, and each is short enough to DRAW between the end-names at 1280 (some sixty characters of the caption face; the band's NAME drops first, by P6-4's rule, and does on most of these) - RangeCaptionCheck holds both to
            // every levied country's book.
            { "Retail intervention", new Dial("Retail intervention", "the levy on the bill", -1, 0.5f, new[]
            {
                B("Hands off", "The least the dial gives. The levy carries more.", +1),
                B("Token", "A gesture on the line. More of it goes on the bill.", +1),
                B("Sparse", "Below custom. A little more of the scheme on the bill.", +1),
                B("Thin", "The line gives less. Households pay it per kilowatt-hour.", +1),
                B("Customary", "The seeded support. The levy stands as found.", 0),
                B("Cushioned", "The treasury takes a slice. The levy eases.", -1),
                B("Subsidised", "The budget takes it over. The levy falls, or is gone.", -1),
                B("Generous", "The taxpayer takes more. The levy thins, or is gone.", -1),
                B("Lavish", "Nearly all the dial gives. The levy falls, or is gone.", -1),
                B("On the house", "The whole dial. Past the levy, only taxpayers pay.", -1),
            }) },
            // Market liberalisation = the Regulation dial read from the other end (S9: "0 light - 100 heavy inverted"): below the seeded anchor the supply margin
            // moves from industry onto households (EnergyLedger.LiberalisationShiftPerKwh, Steiner's finding), above it the other way - the stat is industry's
            // price against households', and it RISES as the dial (regulation) rises.
            { "Market liberalisation", new Dial("Market liberalisation", "industry's price against households'", +1, 0.5f, new[]
            {
                B("Unbundled", "Industry shops around. Households carry the margin.", -1),
                B("Open", "Large users bargain and win. Industry's price eases.", -1),
                B("Liberal", "Below the seeded rulebook. Industry pays a little less.", -1),
                B("Loosened", "A little competition at the top. Industry notices first.", -1),
                B("Customary", "The seeded regulation. The margin splits as found.", 0),
                B("Supervised", "The regulator leans on the household tariff.", +1),
                B("Regulated", "Household prices are watched. Industry pays more.", +1),
                B("Administered", "Tariffs are set, not found. Industry pays the rest.", +1),
                B("Controlled", "The household price is political. Industry pays for it.", +1),
                B("Decreed", "One tariff for the voter, a larger one for the firm.", +1),
            }) },
            // Investment planning = the Tax Credits dial. The energy layer does not read it - the fleet moves by the player's orders alone (P6-F2) - so the lines
            // speak to the sector's output, which MacroSystem's SectorTaxCreditSensitivity does move, and say what the dial does not do.
            { "Investment planning", new Dial("Investment planning", "the sector's output", +1, 0.5f, new[]
            {
                B("None", "No credit for building. The sector's output below custom.", -1),
                B("Token", "A credit too small to move a board. Output a shade under.", -1),
                B("Sparse", "Below the usual incentive. The sector invests less.", -1),
                B("Thin", "Less than the sector was used to. Output edges down.", -1),
                B("Customary", "The seeded incentive. The fleet's orders are above.", 0),
                B("Encouraging", "A better credit. Output rises; no plant is ordered.", +1),
                B("Generous", "The sector invests more. The queue still waits for you.", +1),
                B("Lavish", "Output well above custom. The treasury funds the mood.", +1),
                B("Directed", "The state drafts the plan. The fleet still takes orders.", +1),
                B("Planned", "Every project has a credit. Output at the ceiling.", +1),
            }) },
            // State ownership = the Nationalization / Deregulation dial. No ledger reads a state share of the generators' receipts (the page prints the rent and
            // nothing reads it), so the lines speak to the sector's output (SectorDeregulationSensitivity) and say so.
            { "State ownership", new Dial("State ownership", "the sector's output", +1, 0.5f, new[]
            {
                B("Nationalised", "The state owns the turbines. Output at the floor.", -1),
                B("State-led", "A ministry in all but name. Output well below custom.", -1),
                B("Majority state", "The treasury holds the casting vote. Output lower.", -1),
                B("Golden share", "Private in daylight, public in a crisis.", -1),
                B("Customary", "The seeded ownership. No ledger reads a state share.", 0),
                B("Commercial", "State firms told to act like firms. Output edges up.", +1),
                B("Privatising", "Shares are sold; prospectuses printed. Output rises.", +1),
                B("Private", "The state keeps the regulator and little else.", +1),
                B("Deregulated", "Owners answer to markets. Output and lobbying high.", +1),
                B("Hands off", "The state has left the building. Output at the ceiling.", +1),
            }) },
            // PN-1's dial (§590, DS-3): the pension age on its 60–70 track. SpendingDrivers.StatutoryPensionAge: the pension line follows the people at or above the age in
            // force, so a higher age is fewer pensioners and a smaller line. Neutral = 65, the track's middle. The model reads no participation response (PN-1's deferred
            // BASELINE half), so no line claims the older worker stays at work.
            { "Pension age", new Dial("Pension age", "the pension line", -1, 0.5f, new[]
            {
                B("Early release", "Pensions from sixty. The line carries a generation.", +1),
                B("Generous", "An early exit for most. The line runs heavy.", +1),
                B("Relaxed", "More pensioners than custom. The line is larger.", +1),
                B("Accommodating", "A year under the custom. The line a shade over.", +1),
                B("Customary", "Retirement at sixty-five. The line as the pyramid has it.", 0),
                B("Deferred", "A year past custom. Fewer pensioners; a lighter line.", -1),
                B("Extended", "Two years on. The line eases; the pyramid does not.", -1),
                B("Late", "The line is smaller. Nobody is asked to keep working.", -1),
                B("Very late", "Few reach it young. The line is lean.", -1),
                B("Terminal", "Pensions from seventy. The line at its floor.", -1),
            }) },
        };
    }
}
