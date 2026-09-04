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
            { "    Override rate", new Dial("    Override rate", "tariff take on this partner", +1, 0f, new[]
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
            { "Discretionary line", new Dial("Discretionary line", "the line's provision", +1, 0.5f, new[]
            {
                B("Gutted", "A third of the line gone in a year. The ministry issues a statement; the statement is short.", -1),
                B("Slashed", "A quarter off. Programmes close; the closures make the news.", -1),
                B("Cut", "A real cut. The department finds economies, and then finds it cannot.", -1),
                B("Trimmed", "A little less than the index carried here. Nobody outside the building notices.", -1),
                B("As it stands", "The figure the index carried here. The driver did this; the player did nothing.", 0),
                B("Topped up", "A little more than the index gave. The minister mentions it once.", +1),
                B("Raised", "A real increase. Something new opens; the opposition asks what it costs.", +1),
                B("Expanded", "A fifth more than last year. Provision widens; the deficit notices.", +1),
                B("Surged", "A quarter on the line in one year. The ministry hires; the treasury frowns.", +1),
                B("Doubled down", "Nearly a third more. A signature commitment, funded on the day it is announced.", +1),
            }) },
            { "Mandatory line", new Dial("Mandatory line", "the line's provision", +1, 0.5f, new[]
            {
                B("Clawed back", "A seventh off an entitlement. Cheques shrink; the letters arrive in bulk.", -1),
                B("Cut", "A real cut to a promise made. Recipients organise; their organisation has a mailing list.", -1),
                B("Tightened", "Eligibility narrows or the rate slips. Felt at the margin, litigated at the centre.", -1),
                B("Trimmed", "A shade below what the index carried. Most recipients will not see it; some will.", -1),
                B("As it stands", "The entitlement the index carried here. Its cohort did this, not the player.", 0),
                B("Uprated", "A shade above the index. An honest uprating, and the cheapest kind of gratitude.", +1),
                B("Raised", "A real rise in the entitlement. Recipients notice; so does the projection.", +1),
                B("Widened", "A tenth more than last year: broader eligibility or a fatter rate. A promise that persists.", +1),
                B("Enlarged", "A big rise on a big line. The deficit carries the weight for every year after.", +1),
                B("Recast", "A seventh more in one year. A new social contract, priced at its opening.", +1),
            }) },
        };
    }
}
