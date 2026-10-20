using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;

namespace PoliSim.Simulation
{
    /// <summary>
    /// PS-6 US-18 (`docs/specs/USA_STAGE_PLAN.md`, "Bills through both chambers"; R-US9 for what rides reconciliation; R-US17's terms for the divisions'
    /// sides): A US BILL GOES THROUGH BOTH CHAMBERS OF CONGRESS - "Every Bill which shall have passed the House of Representatives and the Senate" (Art. I §7
    /// cl. 2, [CONST], `ElectionsData/usa/records_by_date.md` §4). The arithmetic is the plan's, in the R-US17 block's play terms (WHEN THE DRAW MATTERS):
    /// <list type="bullet">
    /// <item><description><b>The House</b> passes a bill by a majority of those voting - more FOR than AGAINST among the seated members, a tie failing; the
    /// undecided and a unit with no published position abstain, as the count has always had them (P3-A2).</description></item>
    /// <item><description><b>The Senate, at cloture</b> (every ordinary bill): three fifths of the senators duly chosen and sworn on the day - Standing Rule XXII
    /// ¶2, "three-fifths of the Senators duly chosen and sworn" ([SEN-RULES], records_by_date.md §4) - so the line is <see cref="ClotureLine"/> of the seats
    /// seated that day (<see cref="Sworn"/>; a vacancy is nobody's seat, `UsSenate`), never a fixed sixty; an abstaining senator is no yea. Passage then
    /// follows by a majority on the same sides: cloture implies it, and the Vice President has no cloture vote.</description></item>
    /// <item><description><b>The Senate, by reconciliation</b> (<see cref="BillConcern.Reconciliation"/>): a majority of those voting, the Vice President deciding
    /// an equal division (Art. I §3 cl. 4, read through <see cref="UsSenate.Carries"/>) by his party's stance in that chamber (<see cref="UsSenate.VicePresidentParty"/>,
    /// US-28's one derivation; the block's "with the PP's stance", the president's party, the same ticket). 2 U.S.C. 641(e)(2): "Debate in the Senate on any
    /// reconciliation bill ... shall be limited to not more than 20 hours" ([USC-641]) - so it needs no cloture, the record's DERIVED reading
    /// (records_by_date.md §4).</description></item>
    /// <item><description><b>One stance per party, the same in both chambers</b> - THE BLOC PREMISE, stated (R-US17's premise (B), "play's premise, US-18"): a
    /// party votes as one in each chamber, and each chamber's division is the stance model's sides over the bill's concern
    /// (<see cref="StanceModel.StancesOver(Country, IReadOnlyList{PoliticalParty}, IReadOnlyDictionary{string, int}, BillConcern)"/>, whose alignment reads no
    /// seat count) carried onto that chamber's seats: the House's from `Country.ParliamentSeats`, the Senate's from `Country.SecondChamberSeats`, where each
    /// independent sits in his conference's seats (R-US10 (a), US-15). Stance is never split by chamber (that would amend the R-US17 block - STOP S2's option R1).
    /// The drawn crossers on the veto path are US-21's (R-US17 rule (c), <see cref="Crossing"/>): the projection is the blocs', K = 0, and the gate enters a draw only where it changes a chamber's verdict.</description></item>
    /// <item><description><b>The order:</b> the chamber the bill began in votes first (<see cref="Verdict.Origin"/>), and the other only if it passed - a bill
    /// the first chamber rejects never reaches the second, and the chamber that stops a bill is the first in that order to reject it
    /// (<see cref="Verdict.StoppedIn"/>). THE ORIGIN FIELD, decided here for US-21 (the block's override order, "the ORIGIN chamber votes first"): every
    /// division carries the chamber it was held in and the chamber its bill began in (`DivisionRecord.Chamber`, `DivisionRecord.Origin`), the second
    /// chamber's record carrying the first's (`DivisionRecord.OtherChamber`); until US-19 builds who introduces where (R-US7 (a)), every bill the game tables
    /// begins in the House - "All Bills for raising Revenue shall originate in the House of Representatives" (Art. I §7 cl. 1, [CONST]) for the revenue
    /// kinds, and the House by default for the rest (DECLARED) - while <see cref="Divide"/> takes the origin as an argument, so US-19's introduction and the
    /// Congress diagnostic can seat a bill in the Senate first.</description></item>
    /// </list>
    /// <para><b>WHAT RIDES RECONCILIATION - a READING, DECLARED</b> (R-US9 (a), ruled F8: "the rates, and direct spending and the debt limit where they move, ride
    /// reconciliation at simple majorities (2 U.S.C. 641(a), (e))"; "the appropriation needs cloture - the game's spending lines DECLARED as one appropriation";
    /// "the pension age is its own statute at cloture"). R-US9 rules the budget's path; the standalone kinds are read here by 641(a)'s own heads - "new
    /// entitlement authority" (641(a)(1)(C)), "the total amount by which revenues are to be changed ... changes in the revenue laws" (641(a)(2)) - and
    /// <see cref="IsReconciliationKind"/> holds the table. A BUDGET bill rides it only where it moves no spending line and not the pension age
    /// (<see cref="RidesReconciliation(Country, BudgetBill)"/>); since US-22 the US budget is voted as its acts on E2's machinery (`UsBudgetProcedure`), each
    /// act's procedure its own - the appropriation at cloture, the reconciliation by a majority, the pension act at cloture - and a continuing resolution
    /// at cloture. THE ONCE-A-YEAR LIMIT IS NOT MODELLED (DECLARED): the statute ties reconciliation to "a concurrent resolution
    /// on the budget for any fiscal year" (641(a)), and the game models no concurrent resolution - so neither its directives nor any bound on how many
    /// reconciliation bills one resolution yields; every bill the reading names rides reconciliation whenever it is voted - a trifecta passes the tax and
    /// welfare programme bills and the rate-only budgets by its Senate majority as often as its bills come round, and the reconciliation slip says no yearly
    /// limit is modelled (<see cref="RuleWords"/>). ASKED with the table's placements (<see cref="IsReconciliationKind"/>). R-US17's BUDGET VOTE (play) is "a
    /// reconciliation bill (US-18)", so this table is that ruled block's input at US-21: every bill it sends by reconciliation is a budget vote that draws no
    /// crossers. The Byrd rule is not modelled (R-US9 (a), stated).</para>
    /// <para><b>THE GOVERNMENT'S OWN BUDGET MEETS CONGRESS (PS-6 US-22, closing US-18's DECLARED asymmetry).</b> Until US-22 a concern the government
    /// authored (`BillConcern.GovernmentAuthored`) was voted by its first chamber alone; the plan dates the AI president's budget meeting Congress at US-22
    /// (risk 19; US-22: the AI's budget "goes through the same procedure"), so every bill of a Congress now goes through both chambers - one predicate,
    /// <see cref="VotesBothChambers"/>, which the gate (`ParliamentSystem.Divide`), the dated verdict and the page (`ChamberVerdicts`) all read. The budget
    /// itself is voted as its acts (`UsBudgetProcedure`), each through <see cref="Divide"/>.</para>
    /// <para><b>Scope.</b> <see cref="Applies"/>: the USA with a Senate seated - the player's country, which `SimulationManager.SeatTheSecondChamberOfRecord`
    /// seats from its first day. Every other country's single chamber is untouched (`ParliamentSystem.Divide` keeps its old path byte for byte), and an AI
    /// USA votes no bill (plan risk 19). Deterministic: arithmetic over the state, no random stream drawn.</para>
    /// </summary>
    public static class Congress
    {
        /// <summary>The chamber numbers a division carries (`DivisionRecord.Chamber`, `DivisionRecord.Origin`): the first chamber - the only one in every country
        /// with one, the House where there are two - is 0, the default every division recorded before US-18 carries. CONVENTION: a chamber code, not a quantity.</summary>
        public const int House = 0;

        /// <summary>The second chamber - the USA's Senate. CONVENTION: a chamber code, not a quantity.</summary>
        public const int Senate = 1;

        /// <summary>The bill kinds the game votes, for the reconciliation reading (<see cref="IsReconciliationKind"/>) and the Congress diagnostic's sweep.</summary>
        public enum Kind { Budget, TaxProgram, WelfareProgram, Labor, CrimeJustice, Law, Sector, Trade, SwfDrawdown }

        /// <summary>Whether the country's bills go through both chambers here: the USA with a Senate seated (`Country.SecondChamberSeats` holding a seat).</summary>
        public static bool Applies(Country country) => country != null && country.Id == CountryId.USA && Sworn(country.SecondChamberSeats) > 0;

        /// <summary>
        /// THE ONE PREDICATE every reader of a bill's chambers asks: whether <paramref name="concern"/> goes through both chambers in
        /// <paramref name="country"/> - every bill of a country whose bills do (<see cref="Applies"/>). PS-6 US-22: the government's own budget no longer stays
        /// in its first chamber (US-18's DECLARED asymmetry, closed - the class note), so the concern no longer decides it; the parameter is kept for the
        /// readers' one call. Read by `ParliamentSystem.Divide`, the dated `ParliamentSystem.WouldBillPass` and `ChamberVerdicts`, so the gate and the page give
        /// one answer.
        /// </summary>
        public static bool VotesBothChambers(Country country, BillConcern concern) => Applies(country);

        /// <summary>The senators duly chosen and sworn on the day: every seat the chamber's seating holds (a vacancy is in no side and no base - the block's
        /// VACANCIES).</summary>
        public static int Sworn(IReadOnlyDictionary<string, int> seats)
        {
            int n = 0;
            if (seats == null) { return 0; }
            foreach (int held in seats.Values) { if (held > 0) { n += held; } }
            return n;
        }

        /// <summary>Rule XXII ¶2's line: three fifths of <paramref name="sworn"/>, rounded up - floor((3W + 4) / 5), the R-US17 block's form and `UsVeto`'s, in
        /// integers.</summary>
        public static int ClotureLine(int sworn) => sworn <= 0 ? 0 : (3 * sworn + 4) / 5;

        /// <summary>
        /// THE READING'S TABLE (DECLARED; the class note): a standalone bill kind that rides reconciliation where the country's Congress votes it. A tax
        /// levied or removed changes the revenue laws (641(a)(2)); a welfare programme implemented or removed is entitlement authority (641(a)(1)(C)) - both
        /// direct spending or revenue, R-US9 (a)'s matter. A budget is read by its content (<see cref="RidesReconciliation(Country, BudgetBill)"/>). The labour,
        /// crime-and-justice and law bills carry regulation, matter outside 641(a)'s heads: ordinary bills at cloture. THREE PLACEMENTS ARE DECLARED AND ASKED
        /// (Elias's to rule before US-21, whose BUDGET VOTE reads this table):
        /// <list type="bullet">
        /// <item>(a) THE TARIFF BILL AT CLOTURE - customs duties are revenue laws under 641(a)(2), and Art. I §7 cl. 1 treats them as revenue; the placement
        /// rests not on the statute but on R-US9 naming the budget's rates and not the tariff schedule, and on the tariff being the trade lever whose delegation
        /// to the president is R-US8's (read at US-19);</item>
        /// <item>(b) THE SECTOR BILL AT CLOTURE whatever it moves - one moving only tax credits is a change in the revenue laws, held at cloture because the
        /// kind carries regulation and deregulation in one bill;</item>
        /// <item>(c) THE SOVEREIGN FUND'S DRAWDOWN AT CLOTURE - read as the appropriation's kind, by analogy to R-US9 (a)'s DECLARED single appropriation
        /// (641(a)(1)(A)'s "new budget authority" covers appropriations too); the US seeds no fund.</item>
        /// </list>
        /// </summary>
        public static bool IsReconciliationKind(Kind kind)
        {
            switch (kind)
            {
                case Kind.TaxProgram:
                case Kind.WelfareProgram:
                    return true;
                case Kind.Budget:   // by its content
                case Kind.Labor:
                case Kind.CrimeJustice:
                case Kind.Law:
                case Kind.Sector:
                case Kind.Trade:
                case Kind.SwfDrawdown:
                default:
                    return false;
            }
        }

        /// <summary>Whether a standalone bill of <paramref name="kind"/> rides reconciliation in <paramref name="country"/> - the USA's reading
        /// (<see cref="IsReconciliationKind"/>); false for every other country, whose single chamber has no such path.</summary>
        public static bool RidesReconciliation(Country country, Kind kind) => country != null && country.Id == CountryId.USA && IsReconciliationKind(kind);

        /// <summary>Whether a BUDGET bill rides reconciliation in <paramref name="country"/>: the USA's, where it moves no spending line (the appropriation, R-US9
        /// (a): cloture) and not the pension age (641(g): "it shall not be in order ... to consider any reconciliation bill ... that contains recommendations with
        /// respect to the old-age, survivors, and disability insurance program", [USC-641] - its own act at cloture). The rates, the benefit levels (direct
        /// spending, F6's reading of a benefit level as set by statute) and the fund's rules ride it. PS-6 US-22: never a continuing resolution - it moves
        /// nothing, and on the record it needed cloture (H.R. 5371: "not having achieved 60 votes in the affirmative"; cloture invoked 60 - 40, BILLSTATUS).</summary>
        public static bool RidesReconciliation(Country country, BudgetBill bill)
            => country != null && country.Id == CountryId.USA && bill != null && !bill.ContinuingResolution && !ChangesSpendingLines(country, bill) && !bill.ChangesPensionAge(country);

        /// <summary>Whether a budget bill moves a spending line - a percentage change that is not nought, a figure set other than the line's standing amount, or a
        /// pin turned (a bill names only the lines its author moved, GA-8-2).</summary>
        public static bool ChangesSpendingLines(Country country, BudgetBill bill)
        {
            if (bill == null) { return false; }
            foreach (KeyValuePair<SpendingCategory, float> kv in bill.SpendingPercentChanges) { if (Math.Abs(kv.Value) > 1e-6f) { return true; } }
            foreach (KeyValuePair<SpendingCategory, float> kv in bill.SpendingNominalTargets)
            {
                SpendingLine line = country?.SpendingLines.Find(l => l.Category == kv.Key);
                if (line == null || Math.Abs(kv.Value - line.Amount) > 1e-6f) { return true; }
            }
            return bill.SpendingPinChanges.Count > 0;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // PS-6 US-19 (`docs/specs/USA_STAGE_PLAN.md`, "Roles in a presidential system"; R-US7 (a), RULED F8): WHO INTRODUCES WHAT. "The president's party
        // introduces the administration's bills and proposes the budget (31 U.S.C. 1105(a)); a party out of the White House introduces ordinary bills in any
        // chamber it controls, and revenue bills only where it holds the House (Art. I §7 cl. 1); a party holding neither the presidency nor a chamber votes
        // and introduces nothing." The gate that reads these is `SimulationManager.PlayerMayIntroduce(CountryId, Congress.Kind, CabinetPortfolio?, out string)`;
        // the role a party reads on a day is `GovernmentRecord.RoleOn`. Pure functions of the country's chambers as seated and the Vice President of the day.
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// A REVENUE BILL (Art. I §7 cl. 1, [CONST]: "All Bills for raising Revenue shall originate in the House of Representatives") - READ OFF THE
        /// RECONCILIATION TABLE above (DECLARED: one classification of the bill kinds' revenue, so origination and the Senate's procedure cannot disagree): of
        /// the kinds <see cref="IsReconciliationKind"/> sends by reconciliation, those it reads under 641(a)(2)'s "changes in the revenue laws" - the
        /// tax-programme bill, not the welfare programme, which it reads as entitlement authority under 641(a)(1)(C) - and the budget, which carries the rates
        /// (and is the president's party's alone, R-US7 (a)). The tariff bill and the sector bill are revenue or not as the table places them: today at cloture,
        /// so ordinary here. ⚠ For the tariff that ties origination to a placement whose own note says the opposite on this point ("customs duties are revenue
        /// laws under 641(a)(2), and Art. I §7 cl. 1 treats them as revenue" - the table above) and whose second ground, R-US8, is now built as (a): the tariff
        /// bill's ORIGINATION is ASKED of Elias apart from the table's ask (a). No record day reaches it before the game elects Congress - only a party holding the
        /// Senate alone begins a bill there.
        /// </summary>
        public static bool IsRevenueKind(Kind kind) => kind == Kind.Budget || (IsReconciliationKind(kind) && kind != Kind.WelfareProgram);

        /// <summary>Whether a bill of <paramref name="kind"/> is a revenue bill in <paramref name="country"/> - the USA's reading (<see cref="IsRevenueKind"/>);
        /// false for every other country, whose single chamber has no origination rule here.</summary>
        public static bool IsRevenue(Country country, Kind kind) => country != null && country.Id == CountryId.USA && IsRevenueKind(kind);

        /// <summary>Whether <paramref name="party"/> holds the House: more than half of the members seated (`Country.ParliamentSeats`) - the chamber's
        /// majority, which organises it. A tie holds it for no one.</summary>
        public static bool HoldsHouse(Country country, string party)
        {
            if (country?.ParliamentSeats == null || string.IsNullOrEmpty(party)) { return false; }
            int total = 0;
            foreach (int held in country.ParliamentSeats.Values) { if (held > 0) { total += held; } }
            return country.ParliamentSeats.TryGetValue(party, out int seats) && seats > 0 && 2 * seats > total;
        }

        /// <summary>Whether <paramref name="party"/> holds the Senate on <paramref name="on"/>: the majority of the seats as seated (`Country.SecondChamberSeats`,
        /// each independent with his conference - R-US10 (a)), an equal division the Vice President's party's (Art. I §3 cl. 4, <see cref="UsSenate.MajorityOf"/>).
        /// The Vice President is read only on an equal division - the one case his party decides.</summary>
        public static bool HoldsSenate(Country country, string party, DateTime on) => !string.IsNullOrEmpty(party) && SenateMajority(country, on) == party;

        /// <summary>
        /// THE CHAMBER A BILL BEGINS IN (the origin US-18 left to this item, `DivisionRecord.Origin`), on the vote's day: a revenue bill in the House (Art. I §7
        /// cl. 1); the government's own budget, and a bill with no author, in the House; the administration's bills - its author the president's party - in the
        /// House, US-18's default kept (DECLARED: R-US7 (a) names no chamber for them; ASKED of Elias - the House by default, the chamber the president's party
        /// holds, or the player's choice - since it governs the default start, where the DEM player holds the Senate alone); a party out of the White House's
        /// bill in a chamber it controls - the
        /// House where it holds the House, else the Senate where it holds the Senate (R-US7 (a): "in any chamber it controls"); otherwise the House. Read where
        /// the bill is voted (`ParliamentSystem.Divide`) and where the page projects it (`ChamberVerdicts.Chambers`), so the two name the same first chamber.
        /// Whether both chambers pass a bill does not depend on it - only the order, and so the chamber that stops it.
        /// </summary>
        public static int OriginOf(Country country, BillConcern concern, DateTime on)
        {
            if (!Applies(country) || concern == null || concern.Revenue || concern.GovernmentAuthored || string.IsNullOrEmpty(concern.Author)) { return House; }
            GovernmentRecord g = country.Government;
            if (g == null || g.RoleOf(concern.Author) == PlayerRole.PrimeMinister) { return House; }
            return !HoldsHouse(country, concern.Author) && HoldsSenate(country, concern.Author, on) ? Senate : House;
        }

        /// <summary>
        /// R-US7 (a) FOR A PARTY OUT OF THE WHITE HOUSE (the president's party introduces every bill and is never asked here): may <paramref name="party"/>
        /// introduce a bill of <paramref name="kind"/> on <paramref name="on"/>? The budget is the president's to propose (31 U.S.C. 1105(a)); a revenue bill
        /// (<see cref="IsRevenueKind"/>) needs the House; an ordinary bill needs either chamber; a party holding neither introduces nothing. False with
        /// <paramref name="why"/> - the reason after the role's word, in a page's words - where it may not; true with none where it may.
        /// </summary>
        public static bool MayIntroduce(Country country, string party, Kind kind, DateTime on, out string why)
        {
            why = null;
            bool house = HoldsHouse(country, party), senate = HoldsSenate(country, party, on);
            if (kind == Kind.Budget) { why = "THE PRESIDENT PROPOSES THE BUDGET · YOUR PARTY VOTES ON IT"; return false; }
            if (!house && !senate) { why = HoldsNothingWords; return false; }
            if (IsRevenueKind(kind) && !house) { why = "A REVENUE BILL BEGINS IN THE HOUSE · YOUR PARTY HOLDS THE SENATE ALONE"; return false; }
            return true;
        }

        /// <summary>
        /// WHO BEARS A BILL (the review's M2, DECLARED and ASKED): whether the government pays for <paramref name="concern"/> - the approval a failed vote costs
        /// (`ParliamentSystem.BillFailedApprovalCost`), a passed law's enactment cost (`LawDefinition.EnactmentApprovalCost`), and the day marked lost (Elias's
        /// ruling F5, `ParliamentSystem.RecordBillLost`, which the cabinet's pressure check reads). Each was written for a bill only the government could table;
        /// R-US7 (a) lets a congressional majority out of the White House table its own, and charging the president for the opposition's bills is no reading of
        /// the ruling (the review's M2, Reviews/2026-10-17_s852_us19_roles.md). The smallest change: in a Congress
        /// (<see cref="Applies"/>) the government bears a bill only where its author is in the government - the president's party, or a party in its cabinet
        /// (`GovernmentRecord.RoleOf` PrimeMinister or JuniorPartner) - and the government's own budget and a bill with no author (the tools' hand) as before.
        /// Every other country bears every bill exactly as before, where only the government could table one. PS-6 US-22 (the review's second-pass N1, closed):
        /// the budget's failed vote reads this too (`ParliamentSystem.ApplyBillResult`'s governmentBears) - each US budget act, each continuing resolution and
        /// the budget paths of a country without a Congress - so a budget pending across a change of presidency is charged as its lost day is marked. Who
        /// should bear a congressional majority's bill instead - its own party's standing, or no one - is Elias's to rule.
        /// </summary>
        public static bool GovernmentBears(Country country, BillConcern concern)
        {
            if (!Applies(country) || concern == null || concern.GovernmentAuthored || string.IsNullOrEmpty(concern.Author) || country.Government == null) { return true; }
            PlayerRole role = country.Government.RoleOf(concern.Author);
            return role == PlayerRole.PrimeMinister || role == PlayerRole.JuniorPartner;
        }

        /// <summary>What a party holding neither the presidency nor a chamber may do - the plan's risk 18, stated, not padded.</summary>
        public const string HoldsNothingWords = "YOUR PARTY HOLDS NEITHER THE PRESIDENCY NOR A CHAMBER · IT VOTES AND INTRODUCES NOTHING UNTIL AN ELECTION CHANGES THAT";

        /// <summary>The party holding the House (<see cref="HoldsHouse"/>), by key; null where none holds a majority.</summary>
        public static string HouseMajority(Country country)
        {
            if (country?.ParliamentSeats == null) { return null; }
            foreach (string party in country.ParliamentSeats.Keys) { if (HoldsHouse(country, party)) { return party; } }
            return null;
        }

        /// <summary>The party holding the Senate on <paramref name="on"/> (<see cref="HoldsSenate"/>), by key; null where none is seated or none holds it.</summary>
        public static string SenateMajority(Country country, DateTime on)
        {
            if (country?.SecondChamberSeats == null || Sworn(country.SecondChamberSeats) <= 0) { return null; }
            return UsSenate.MajorityOf(country.SecondChamberSeats, UsSenate.EquallyDivided(country.SecondChamberSeats) ? UsSenate.VicePresidentParty(country, on) : null);
        }

        /// <summary>What a party out of the White House may introduce, in a page's words - by the chambers it holds on the day (null where it holds neither).</summary>
        public static string IntroducesWords(Country country, string party, DateTime on)
            => HoldsHouse(country, party) ? "YOUR PARTY INTRODUCES ORDINARY AND REVENUE BILLS IN THE HOUSE"
                : HoldsSenate(country, party, on) ? "YOUR PARTY INTRODUCES ORDINARY BILLS IN THE SENATE · A REVENUE BILL BEGINS IN THE HOUSE"
                : null;

        /// <summary>The words of a congressional role (<see cref="GovernmentRecord.RoleOn"/>'s for a party out of the White House) - HOUSE MAJORITY, SENATE MAJORITY,
        /// MINORITY; null for any other role. Words only: D-US composes them (the plan's Design asks, part two, question 5).</summary>
        public static string RoleWord(PlayerRole role) =>
            role == PlayerRole.HouseMajority ? "HOUSE MAJORITY" : role == PlayerRole.SenateMajority ? "SENATE MAJORITY" : role == PlayerRole.Minority ? "MINORITY" : null;

        /// <summary>
        /// R-US8, RULED F8: "(b) where the delegating statutes and the litigation's state are read by date when US-19 is built, otherwise (a), stated." US-19 was
        /// built with neither read (19 U.S.C. 1862 and 2411, 50 U.S.C. 1701-1702 and the litigation's state - the plan's sources table, its R-US8 (b) row; US-19's
        /// record says what was on disk), so (a) is built: "only the veto and appointments (with the Senate's consent) are the president's alone. Every dial - the
        /// tariff included - is a bill through Congress, and the president's delegated tariff powers are named as not modelled." These are the words the Laws
        /// page's trade bill says it in (its status line, as a sentence; its slip upper-cases it); (b) is a later item's, once the statutes and the litigation are
        /// read by date.
        /// </summary>
        public const string TariffPowersWords = "A tariff moves only by a bill through Congress - the president's delegated tariff powers are not modelled.";

        /// <summary>One chamber's division on one bill: its sides (the stances carried onto its seats), the count, the line, and the verdict.</summary>
        public sealed class Division
        {
            /// <summary><see cref="House"/> or <see cref="Senate"/>.</summary>
            public int Chamber;
            /// <summary>The Senate under Rule XXII - <see cref="Required"/> is the cloture line; false for the House and for the Senate's majority path.</summary>
            public bool Cloture;
            /// <summary>Whether the bill asks the chamber anything - an empty concern passes every chamber unasked, as a zero direction always has.</summary>
            public bool Contested;
            /// <summary>Every seated party's stance, in the party system's order, its seats this chamber's.</summary>
            public List<PartyStance> Stances = new List<PartyStance>();
            public int For, Against, Abstaining;
            /// <summary>The senators sworn on the day (the Senate's), 0 for the House.</summary>
            public int Sworn;
            /// <summary>The cloture line (<see cref="ClotureLine"/> of <see cref="Sworn"/>) where <see cref="Cloture"/>; 0 where the count decides.</summary>
            public int Required;
            public bool ClotureInvoked;
            /// <summary>The Vice President's party where his vote decided an equal division (the Senate's majority path), else null.</summary>
            public string VicePresidentParty;
            /// <summary>His vote there: +1 FOR, -1 AGAINST, 0 none (his party undecided, unmeasured or seatless - the tie then fails).</summary>
            public int VicePresidentVote;
            /// <summary>The seat-weighted lean over this chamber's measured seats - the record's alignment, as `ParliamentSystem.GetSeatWeightedAlignment` reads the
            /// House.</summary>
            public float Alignment;
            public bool Passed;
            /// <summary>PS-6 US-21: the gate's draw of defectors in this chamber (<see cref="Crossing"/>) - null on every projection (a projection never draws) and
            /// wherever no draw was made. Where <see cref="Crossing.Matters"/>, <see cref="For"/>, <see cref="Against"/> and <see cref="Passed"/> carry the crossers.</summary>
            public Crossing Crossing;
        }

        /// <summary>
        /// PS-6 US-21: ONE CHAMBER'S DRAW ON ONE BILL, as the gate made it (R-US17's rule (c), THE MODEL items 2-6): the president's party standing AGAINST drew
        /// <see cref="Kp"/> crossers from its cell (HP or SP), and the other party standing FOR drew <see cref="Ko"/> from its own (HO or SO) - an index into the
        /// cell's list (<see cref="IndexPp"/>, <see cref="IndexOp"/>; -1 where that party drew nothing), the keyed unit over the play key. The crossers entered
        /// the chamber's division only where they changed its verdict (<see cref="Matters"/>).
        /// </summary>
        public sealed class Crossing
        {
            public string PresidentsParty, OtherParty;
            public int Kp, Ko;
            public int IndexPp = -1, IndexOp = -1;
            /// <summary>The play keys the two draws hashed (null where a party drew nothing) - the diagnostic's "one play key per chamber per bill".</summary>
            public string KeyPp, KeyOp;
            /// <summary>The verdict at the drawn pair differs from the blocs' (0, 0): the draw MATTERS, and the crossers are in the division.</summary>
            public bool Matters;
            /// <summary>PS-6 US-22b (I13 (2)): the party withholding a continuing resolution, its senators drawn at the resolution's cloture during a lapse
            /// (`UsLapseDraw`) - its K (crossing from against to for), the list index and the key; null where no such draw was made.</summary>
            public string Withholder;
            public int Kw, IndexW = -1;
            public string KeyW;

            /// <summary>The crossers a party sent to the other side in the division - its drawn K where the draw mattered, else none.</summary>
            public int CrossersOf(string party) => !Matters ? 0 : party == PresidentsParty ? Kp : party == Withholder ? Kw : party == OtherParty ? Ko : 0;   // US-22b (the review's L5): the withholder before the other party
        }

        /// <summary>A bill through both chambers: each chamber's division, the order, the procedure, the verdict and the chamber that stops it.</summary>
        public sealed class Verdict
        {
            public Division House, Senate;
            /// <summary>The chamber the bill began in - it votes first.</summary>
            public int Origin;
            /// <summary>The Senate takes it by reconciliation (a majority) rather than at cloture.</summary>
            public bool Reconciliation;
            /// <summary>Both chambers pass it.</summary>
            public bool Passed;
            /// <summary>The first chamber in the order to reject it - <see cref="Congress.House"/> or <see cref="Congress.Senate"/> - or -1 where none does.</summary>
            public int StoppedIn = -1;

            /// <summary>The chamber that votes first.</summary>
            public Division First => Origin == Congress.Senate ? Senate : House;

            /// <summary>The chamber that votes second, and only if the first passed the bill.</summary>
            public Division Second => Origin == Congress.Senate ? House : Senate;

            /// <summary>The division that stops the bill, or null where it passes.</summary>
            public Division Stopping => StoppedIn == Congress.House ? House : StoppedIn == Congress.Senate ? Senate : null;
        }

        /// <summary>
        /// THE ONE PROJECTION: both chambers' divisions on <paramref name="concern"/> on <paramref name="on"/> (the vote's day - the Senate's seats are the
        /// country's as seated, the Vice President the one in office that day), in the order the bill takes from <paramref name="origin"/>. Both divisions are
        /// computed whatever the order, so a page can say what each chamber would do; the verdict and the stopping chamber read the order. Reads only; the
        /// gate records through <see cref="Divide"/>, and `ParliamentSystem.WouldBillPass(Country, BillConcern, DateTime)` and `ChamberVerdicts` read this.
        /// </summary>
        public static Verdict Project(Country country, BillConcern concern, DateTime on, int origin = House)
        {
            bool contested = concern != null && !concern.IsEmpty;
            bool reconciliation = concern != null && concern.Reconciliation;
            var verdict = new Verdict { Origin = origin == Senate ? Senate : House, Reconciliation = reconciliation };
            verdict.House = HouseDivision(country, concern, contested);
            verdict.Senate = SenateDivision(country, concern, contested, reconciliation, on, verdict.House);
            Division first = verdict.First, second = verdict.Second;
            verdict.StoppedIn = !first.Passed ? first.Chamber : !second.Passed ? second.Chamber : -1;
            verdict.Passed = first.Passed && second.Passed;
            return verdict;
        }

        private static Division HouseDivision(Country country, BillConcern concern, bool contested)
        {
            var d = new Division { Chamber = House, Contested = contested };
            if (contested)
            {
                d.Stances = StanceModel.Stances(country, concern);
                Count(d);
            }
            d.Passed = !contested || d.For > d.Against;   // a majority of those voting; a tie fails
            return d;
        }

        private static Division SenateDivision(Country country, BillConcern concern, bool contested, bool reconciliation, DateTime on, Division house)
        {
            int sworn = Sworn(country.SecondChamberSeats);
            var d = new Division { Chamber = Senate, Contested = contested, Cloture = !reconciliation, Sworn = sworn, Required = reconciliation ? 0 : ClotureLine(sworn) };
            if (!contested)
            {
                d.ClotureInvoked = d.Cloture;   // nothing asked: no senator stands against it
                d.Passed = true;
                return d;
            }
            if (sworn <= 0) { d.Passed = false; return d; }   // a Senate with nobody sworn passes nothing asked of it (every caller asks Applies first)
            d.Stances = StanceModel.StancesOver(country, PartySystems.For(country.Id), country.SecondChamberSeats, concern);   // never null past the line above (a null seating would read the House's)
            Count(d);
            int vice = 0;
            if (!d.Cloture && d.For == d.Against)
            {
                // an equal division on the majority path: the Vice President votes his party's stance in this chamber (in the House's where his party holds no
                // seat here; none where it holds none in either, and the tie fails)
                d.VicePresidentParty = UsSenate.VicePresidentParty(country, on);
                vice = SideOf(d.Stances, d.VicePresidentParty) ?? SideOf(house.Stances, d.VicePresidentParty) ?? 0;
                d.VicePresidentVote = vice;
            }
            bool carries = UsSenate.Carries(d.For, d.Against, vice > 0);
            d.ClotureInvoked = d.Cloture && d.For >= d.Required;
            d.Passed = d.Cloture ? d.ClotureInvoked && carries : carries;
            return d;
        }

        /// <summary>The count over a division's stances - FOR, AGAINST, and the undecided and unmeasured abstaining - and its seat-weighted lean over the measured
        /// seats, summed in the order `ParliamentSystem.GetSeatWeightedAlignment` sums the House.</summary>
        private static void Count(Division d)
        {
            float weightedAlignment = 0f;
            float measuredSeats = 0f;
            foreach (PartyStance stance in d.Stances)
            {
                if (!stance.Measured) { d.Abstaining += stance.Seats; continue; }
                measuredSeats += stance.Seats;
                weightedAlignment += stance.Seats * stance.Alignment;
                if (stance.Side > 0) { d.For += stance.Seats; } else if (stance.Side < 0) { d.Against += stance.Seats; } else { d.Abstaining += stance.Seats; }
            }
            d.Alignment = measuredSeats > 0f ? weightedAlignment / measuredSeats : 0f;
        }

        private static int? SideOf(List<PartyStance> stances, string party)
        {
            if (string.IsNullOrEmpty(party) || stances == null) { return null; }
            foreach (PartyStance stance in stances) { if (stance.Party.Abbrev == party) { return stance.Measured ? stance.Side : 0; } }
            return null;
        }

        /// <summary>
        /// THE GATE: the bill voted through both chambers on <paramref name="date"/> and RECORDED - each chamber's division its own record, written as it is held:
        /// the chamber the bill began in first, then, only where it passed, the other. A first chamber's division the second follows is marked a motion - its
        /// ceremony is the second's, which carries it (`DivisionRecord.OtherChamber`; §761's precedent, the passage a veto follows) - so the plate shows both
        /// and a bill is signed once. A bill either chamber rejects is the day's loss (`ParliamentSystem.RecordBillLost`, Elias's ruling F5) where the government
        /// bears it (<see cref="GovernmentBears"/>, PS-6 US-19: not a congressional majority's bill from out of the government). Returns the
        /// division that carries the bill's ceremony - the second chamber's where it voted, the first's where it stopped there - and, in
        /// <paramref name="passed"/>, whether both chambers passed it. The log stays write-only: nothing in the simulation reads it back.
        /// </summary>
        public static DivisionRecord Divide(Country country, string title, BillConcern concern, DateTime date, out bool passed, BillAxis axis = BillAxis.Fiscal, int origin = House)
            => Divide(country, _ => title, concern, date, out passed, out _, axis, origin);

        /// <summary>The gate with the title read from the bill's own verdict (the government's budget names its fate) - <paramref name="titleOf"/> is handed both
        /// chambers' verdict, and both chambers' records carry the one title (the bill's identity: US-21's draw keys on it).</summary>
        public static DivisionRecord Divide(Country country, Func<bool, string> titleOf, BillConcern concern, DateTime date, out bool passed, out string title, BillAxis axis = BillAxis.Fiscal, int origin = House)
            => Divide(country, titleOf, concern, date, out passed, out title, out _, axis, origin);

        /// <summary>PS-6 US-21: the gate, handing back the verdict it voted - both chambers' divisions with each chamber's draw (<see cref="Division.Crossing"/>:
        /// its play keys, indices, K and whether it mattered) - for the veto diagnostic; the records it writes are the same.</summary>
        public static DivisionRecord Divide(Country country, Func<bool, string> titleOf, BillConcern concern, DateTime date, out bool passed, out string title, out Verdict verdict, BillAxis axis = BillAxis.Fiscal, int origin = House)
        {
            verdict = Project(country, concern, date, origin);
            DrawAtTheGate(country, concern, titleOf(verdict.Passed), date, verdict);   // PS-6 US-21: R-US17 rule (c)'s draw - the crossers enter a division only where they change it
            if (concern != null && concern.DrawsWithholders) { DrawWithholders(country, concern, titleOf(verdict.Passed), date, verdict); }   // PS-6 US-22b (I13 (2)): a lapse's resolution, the withholder's own senators at cloture
            title = titleOf(verdict.Passed);
            DivisionRecord first = Record(country, title, concern, verdict.First, verdict, date, axis);
            DivisionRecord last = first;
            if (verdict.First.Passed)
            {
                first.Motion = true;
                last = Record(country, title, concern, verdict.Second, verdict, date, axis);
                last.OtherChamber = first;
            }
            passed = verdict.Passed;
            if (!passed) { RecordLost(country, concern, date); }   // PS-6 US-19 (the review's M2), US-21: the day's loss where the government bears the bill - one writer, the US veto gate's too
            return last;
        }

        private static DivisionRecord Record(Country country, string title, BillConcern concern, Division d, Verdict verdict, DateTime date, BillAxis axis)
        {
            var sides = new List<DivisionSide>();
            foreach (PartyStance stance in d.Stances)
            {
                int crossers = d.Crossing != null ? d.Crossing.CrossersOf(stance.Party.Abbrev) : 0;   // PS-6 US-21: none unless the draw changed this chamber's verdict
                sides.Add(new DivisionSide { Abbrev = stance.Party.Abbrev, ShortName = stance.Party.ShortName, Seats = stance.Seats - crossers, Side = stance.Side, Alignment = stance.Alignment, Reason = StanceModel.ReasonLine(stance), ReasonShort = StanceModel.ReasonShort(stance) });
                if (crossers > 0)
                {
                    // the party's drawn defectors, on the other side of its stance, flagged (the block's item 6: "they enter as each party's crossers on the other side, flagged as defectors")
                    sides.Add(new DivisionSide { Abbrev = stance.Party.Abbrev, ShortName = stance.Party.ShortName, Seats = crossers, Side = -stance.Side, Alignment = stance.Alignment, Defectors = true,
                        Reason = DefectorWords(stance.Side, crossers), ReasonShort = "crossed its party" });
                }
            }
            DivisionRecord record = country.Divisions.Append(title, date, d.Contested ? d.Alignment : 0f, d.Passed, concern?.Direction ?? 0f, (int)axis, sides);
            record.Chamber = d.Chamber;
            record.Origin = verdict.Origin;
            record.Reconciliation = verdict.Reconciliation;
            record.Cloture = d.Cloture;
            record.Required = d.Required;
            record.Sworn = d.Sworn;
            record.VicePresidentVote = d.VicePresidentVote;
            record.VicePresidentParty = d.VicePresidentParty;   // set only where an equal division on the majority path consulted him
            return record;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // PS-6 US-21 (`docs/specs/USA_STAGE_PLAN.md`, "The US veto"; R-US17's rule (c), INSTALLED - the fenced block's THE MODEL, THE VETO, THE OVERRIDE and SEEDS
        // AND DETERMINISM): THE DRAW AT THE GATE, THE PRESIDENT'S ANSWER AND THE OVERRIDE. The arithmetic is `UsVeto`'s and the lists are the generated catalog's
        // (`UsVetoDefections`, indexed, never sorted or merged here); this is only what play reads into them - the president of the day, the veto path, each
        // chamber's division. No SimulationRandom stream is created, drawn or moved: the draw is keyed.
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// R-US17's BUDGET VOTE (play) for the bill kinds the game votes before US-22 (the block: "every act the US budget procedure tables ..., a reconciliation
        /// bill (US-18), and US-24's debt-limit act"; H1: "Budget votes ... stay as they are") - a READING, DECLARED: the budget, whatever it moves (it carries the
        /// appropriation - R-US9 (a)'s spending lines DECLARED as one appropriation - or rides reconciliation by its content: a budget vote either way); every
        /// kind the reconciliation table sends by reconciliation (<see cref="IsReconciliationKind"/> - a ruling on the table's asks moves this with it); and the
        /// sovereign fund's drawdown, which the table reads as the appropriation's kind (its ask (c); the US seeds no fund). Every other kind is ORDINARY
        /// LEGISLATION, the only bills a defector is drawn on.
        /// </summary>
        public static bool IsBudgetVoteKind(Kind kind) => kind == Kind.Budget || kind == Kind.SwfDrawdown || IsReconciliationKind(kind);

        /// <summary>Whether a bill of <paramref name="kind"/> is a budget vote in <paramref name="country"/> - the USA's reading (<see cref="IsBudgetVoteKind"/>);
        /// false for every other country, where no draw is made at all.</summary>
        public static bool IsBudgetVote(Country country, Kind kind) => country != null && country.Id == CountryId.USA && IsBudgetVoteKind(kind);

        /// <summary>
        /// PS-6 US-21 (the review of 54f10b8d, M3): an act NOT PRESENTED to the president - a US constitutional amendment, enacted or repealed. A READING, DECLARED:
        /// `LawDefinition.ConstitutionalAmendment`'s own contract ("never vetoed" where a president holds a veto the game runs) and Art. V, under which Congress
        /// proposes an amendment with no presentment (the game models no ratification by the states). The veto gate does not present it, and - so that the draw
        /// decides only whether a bill REACHES a president (H1, "on the USA's veto path only") - no defector is drawn on it: an amendment votes its blocs exactly as
        /// before US-21. False for every other country, where no draw is made at all.
        /// </summary>
        public static bool IsUnpresented(Country country, LawDefinition law) => country != null && country.Id == CountryId.USA && law != null && law.ConstitutionalAmendment;

        /// <summary>
        /// PS-6 US-21: THE DAY'S LOSS for a bill through Congress (Elias's ruling F5, `ParliamentSystem.RecordBillLost`) - written where a chamber rejects the
        /// bill (<see cref="Divide"/>) and where a veto of it stands (the US veto gate, `SimulationManager.UsVetoGate`): ONE writer, handed the bill's concern
        /// (its author), so the rule on whose loss a bill is - US-19's (the review's M2): the government's only where it bears the bill (<see cref="GovernmentBears"/>)
        /// - applies to a rejection and to a sustained veto alike, and is written once. A congressional majority's bill a president vetoes is not the
        /// government's loss; the government's own bill a veto kills is (a player-president's veto of his own party's bill).
        /// </summary>
        public static void RecordLost(Country country, BillConcern concern, DateTime date)
        {
            if (GovernmentBears(country, concern)) { ParliamentSystem.RecordBillLost(country, date); }
        }

        /// <summary>Whether the draw is installed: the generated catalog's outcome is the rule's INSTALL (the ruling record INSTALLED, which `UsVetoDrawCheck`
        /// holds). Any other outcome leaves every division the blocs', and the veto diagnostic fails while it is so.</summary>
        public static bool DrawInstalled => UsVetoDefections.Outcome == "INSTALL";

        /// <summary>A cell's drawn list as `UsVeto` indexes it - the catalog's entries in their order (already merged for a pooled cell), read-only; null for an
        /// unfitted cell, which draws nothing.</summary>
        public static IReadOnlyList<(int Defectors, int Voting)> ListOf(UsVetoDefections.Cell cell)
        {
            if (cell == null || !cell.Fitted) { return null; }
            var list = new (int Defectors, int Voting)[cell.List.Length];
            for (int i = 0; i < list.Length; i++) { list[i] = (cell.List[i].Defectors, cell.List[i].Voting); }
            return Array.AsReadOnly(list);
        }

        private static readonly IReadOnlyList<(int Defectors, int Voting)> HouseP = ListOf(UsVetoDefections.HP), HouseO = ListOf(UsVetoDefections.HO),
            SenateP = ListOf(UsVetoDefections.SP), SenateO = ListOf(UsVetoDefections.SO);

        /// <summary>The list a party draws from in <paramref name="chamber"/>: the president's party's cell (HP, SP), or the other party's (HO, SO).</summary>
        public static IReadOnlyList<(int Defectors, int Voting)> CellOf(int chamber, bool presidentsParty) =>
            chamber == Senate ? (presidentsParty ? SenateP : SenateO) : (presidentsParty ? HouseP : HouseO);

        /// <summary>
        /// THE PRESIDENT OF THE DAY a bill goes to (the block's PRESIDENT, PP, OP: "the game's seated president where ... a decided contest, otherwise the
        /// president of record from WorldClock.TryGovernmentAt (ExecutiveKind Presidency; PP its party, REP or DEM)"), READ through US-28's one derivation,
        /// `UsTermLimit.TryPresidentOn` (DECLARED): the winner of the game's latest contest sworn by the day (US-7's oath - the US contests, where the block's
        /// `PresidentialElection.PresidentAt` reads Poland's list and finds no US president), else the president of record by date, else the sitting presidency
        /// an undecided contest leaves. <paramref name="playerPresident"/>: the president is the game's own and of the player's party - the player is the
        /// president, and chooses (R-US7, F8). False where none is found - on a US vote day a DEFECT (the block).
        /// </summary>
        public static bool PresidentOn(Country country, DateTime on, out string shownAs, out string party, out bool playerPresident)
        {
            shownAs = null; party = null; playerPresident = false;
            if (country == null || !UsTermLimit.TryPresidentOn(country, on, out _, out party, out shownAs)) { return false; }
            playerPresident = UsPresidentialElection.TryInOffice(country, on, out UsPresidentialElection.Contest sworn) && !string.IsNullOrEmpty(sworn.WinnerParty)
                              && sworn.WinnerParty == country.PlayerPartyAbbrev;
            // PS-6 US-16 - R-US22 (DECLARED): a president ACTING after the game's own undecided contest - the Vice President the Senate chose, or the Speaker - of
            // the player's party is the player's too: the presidency is seated by the game's own contest, so the player signs and vetoes (R-US7)
            if (!playerPresident && UsPresidentialElection.TryActingOn(country, on, out UsPresidentialElection.Contest undecided) && country.Government != null
                && country.Government.Outcome == UsPresidentialElection.ActingOutcome && !string.IsNullOrEmpty(country.Government.PmParty)
                && country.Government.PmParty == undecided.ActingParty && country.Government.PmParty == country.PlayerPartyAbbrev)
            {
                playerPresident = true;
            }
            return true;
        }

        /// <summary>
        /// THE DRAW AT THE GATE (the block's THE MODEL, items 2 to 6, and THE PLAY KEY): on a US bill, in each chamber as it votes - the bill's first chamber, then
        /// the second only where the first passed it - a chamber where the president's party stands AGAINST draws its crossers, and where the other party stands
        /// FOR, so does it: once per party per chamber per bill, keyed on the master seed, the day of that chamber's division, the title, the chamber and the
        /// party (`UsVeto.Key`), so a reload, the shadow and a fork that vote the bill on the same day draw alike. The crossers enter the chamber's division only
        /// where they change its verdict (WHEN THE DRAW MATTERS); everywhere else the division is the blocs', untouched. Nothing is drawn on a budget vote (the
        /// draw is asked and `UsVeto.Draw` refuses it, uncounted), outside the USA, where the president's party is not REP or DEM (a president with no party, the
        /// block's DECLARED default), where it holds no seat in the chamber or does not stand against, or where both blocs stand against (UNCONTESTED). The game
        /// votes each chamber once on a bill, so that division's day is the bill's day there, and its one draw serves the override too (no fresh draw: SIDES).
        /// </summary>
        private static void DrawAtTheGate(Country country, BillConcern concern, string title, DateTime date, Verdict verdict)
        {
            if (country == null || !UsVeto.Applies(country.Id) || concern == null || concern.IsEmpty || concern.Unpresented || !DrawInstalled) { return; }   // an act no president sees is drawn on by no one (the review of 54f10b8d, M3)
            if (!PresidentOn(country, date, out _, out string pp, out _))
            {
                UnityEngine.Debug.LogError($"VETO: {country.Id} - no president found on {date:yyyy-MM-dd} for '{title}': the bill goes undrawn and unsigned by any rule (a defect)");
                return;
            }
            if (pp != "REP" && pp != "DEM") { return; }
            Division first = verdict.First, second = verdict.Second;
            DrawIn(country, concern, first, verdict, title, date, pp);
            if (first.Passed) { DrawIn(country, concern, second, verdict, title, date, pp); }
            verdict.StoppedIn = !first.Passed ? first.Chamber : !second.Passed ? second.Chamber : -1;
            verdict.Passed = first.Passed && second.Passed;
        }

        /// <summary>One chamber's draw at the gate: asked of `UsVeto` wherever the chamber is on the veto path's terms (a budget vote refused there), the crossers
        /// entered where the draw matters, the division's count, verdict and cloture moved with them.</summary>
        private static void DrawIn(Country country, BillConcern concern, Division d, Verdict verdict, string title, DateTime date, string pp)
        {
            UsVeto.ChamberVote v = DrawableVote(country, d, verdict, date, pp);
            if (v == null) { return; }
            string op = pp == "REP" ? "DEM" : "REP";
            if (UsVeto.ChamberOutcome(v, 0, 0) != d.Passed)
            {
                UnityEngine.Debug.LogError($"VETO: {country.Id} - '{title}' in the {ChamberName(country.Id, d.Chamber)}: the blocs' verdict and the rule's K = 0 verdict differ - no draw entered (a defect)");
                return;
            }
            Crossing c = DrawCrossers(v, country.Id, concern.BudgetVote, SimulationRandom.MasterSeed, date, title, d.Chamber, pp, op);
            if (concern.BudgetVote) { return; }   // refused by UsVeto: K = 0, nothing counted, the division the blocs'
            d.Crossing = c;
            UnityEngine.Debug.Log($"DRAW: {country.Id} - '{title}' in the {ChamberName(country.Id, d.Chamber)}: {pp} draws {c.Kp} crosser(s) (list index {c.IndexPp}){(c.IndexOp >= 0 ? $", {op} {c.Ko} (list index {c.IndexOp})" : string.Empty)} - {(c.Matters ? "the draw changes the verdict, and the crossers enter the count" : "the verdict stands as the blocs give it")}");
            if (!c.Matters) { return; }
            int moved = c.Kp - c.Ko;   // the president's party's crossers nay to yea, the other party's yea to nay
            d.For += moved;
            d.Against -= moved;
            d.Passed = UsVeto.ChamberOutcome(v, c.Kp, c.Ko);
            d.ClotureInvoked = d.Cloture && d.For >= d.Required;
            if (d.Chamber == Senate && !d.Cloture)
            {
                bool tie = d.For == d.Against;   // the majority path: the Vice President named only where his vote decides an equal division
                d.VicePresidentParty = tie ? UsSenate.VicePresidentParty(country, date) : null;
                d.VicePresidentVote = tie ? v.VicePresident : 0;
            }
        }

        /// <summary>
        /// PS-6 US-22b (Elias's ruling I13 (2): "(P) stands. Add R-US17's draw (H1) for the withholder's own senators on each resolution's cloture vote during a
        /// lapse"): THE WITHHOLDER'S DRAW. In the Senate alone, where it votes the resolution at cloture, the party withholding it (REP or DEM, standing against
        /// with a seat there) draws its crossers from the record's lapse-period cloture votes (`UsLapseDraw`, keyed on the master seed, the day, the title and
        /// the party); they move from against to for and enter the division only where they change its verdict - R-US17's rule, so a lapse can end through
        /// its defectors. The veto-path draw is untouched (a continuing resolution is a budget vote, which it refuses). Nothing where the chamber's blocs and
        /// the rule's K = 0 verdict differ (a defect, logged).
        /// </summary>
        private static void DrawWithholders(Country country, BillConcern concern, string title, DateTime date, Verdict verdict)
        {
            string w = concern.WithheldBy;
            if (country == null || !UsVeto.Applies(country.Id) || (w != "REP" && w != "DEM")) { return; }
            Division d = verdict.Senate;
            if (d == null || !d.Cloture || (verdict.Origin != Senate && !verdict.First.Passed)) { return; }
            PartyStance? s = StanceOf(d.Stances, w);
            if (s == null || !s.Value.Measured || s.Value.Side >= 0 || s.Value.Seats <= 0) { return; }
            var v = new UsVeto.ChamberVote { Procedure = UsVeto.Procedure.SenateCloture, Yea0 = d.For, Nay0 = d.Against, Sworn = d.Sworn, PpSeats = s.Value.Seats, PpList = UsLapseDraw.List };
            if (UsVeto.ChamberOutcome(v, 0, 0) != d.Passed)
            {
                UnityEngine.Debug.LogError($"DRAW: {country.Id} - '{title}' in the Senate: the blocs' verdict and the rule's K = 0 verdict differ - no withholder's draw entered (a defect)");
                return;
            }
            int k = UsLapseDraw.Draw(SimulationRandom.MasterSeed, date, title, w, s.Value.Seats, out int j);
            var c = d.Crossing ?? new Crossing();
            c.Withholder = w; c.Kw = k; c.IndexW = j; c.KeyW = UsLapseDraw.Key(SimulationRandom.MasterSeed, date, title, w);
            c.Matters = UsVeto.ChamberOutcome(v, k, 0) != UsVeto.ChamberOutcome(v, 0, 0);
            d.Crossing = c;
            UnityEngine.Debug.Log($"DRAW: {country.Id} - '{title}' in the Senate: {w}, withholding it, draws {k} crosser(s) at cloture (list index {j}, {UsLapseDraw.FittedLabel}) - {(c.Matters ? "the draw changes the verdict, and the crossers enter the count" : "the verdict stands as the blocs give it")}");
            if (!c.Matters) { return; }
            d.For += k;
            d.Against -= k;
            d.Passed = UsVeto.ChamberOutcome(v, k, 0);
            d.ClotureInvoked = d.Cloture && d.For >= d.Required;
            Division first = verdict.First, second = verdict.Second;
            verdict.StoppedIn = !first.Passed ? first.Chamber : !second.Passed ? second.Chamber : -1;
            verdict.Passed = first.Passed && second.Passed;
        }

        /// <summary>
        /// The chamber as `UsVeto` reads it where a draw would be made on it (the block's item 2), null where none would: the president's party standing AGAINST
        /// with a seat there; the other party drawing only where it stands FOR; nothing where both blocs stand against (UNCONTESTED), where nothing is asked, or
        /// where the cell is unfitted. Yea0 and Nay0 are the blocs' FOR and AGAINST, the undecided abstaining as the count has them.
        /// </summary>
        private static UsVeto.ChamberVote DrawableVote(Country country, Division d, Verdict verdict, DateTime on, string pp)
        {
            if (d == null || !d.Contested || (pp != "REP" && pp != "DEM")) { return null; }
            string op = pp == "REP" ? "DEM" : "REP";
            PartyStance? p = StanceOf(d.Stances, pp), o = StanceOf(d.Stances, op);
            if (p == null || !p.Value.Measured || p.Value.Side >= 0 || p.Value.Seats <= 0) { return null; }
            if (o != null && o.Value.Measured && o.Value.Side < 0) { return null; }
            bool opDraws = o != null && o.Value.Measured && o.Value.Side > 0 && o.Value.Seats > 0;
            var v = new UsVeto.ChamberVote
            {
                Procedure = d.Chamber == House ? UsVeto.Procedure.House : d.Cloture ? UsVeto.Procedure.SenateCloture : UsVeto.Procedure.SenateMajority,
                Yea0 = d.For, Nay0 = d.Against, Sworn = d.Sworn,
                VicePresident = d.Chamber == Senate && !d.Cloture ? VicePresidentSide(country, verdict, on) : 0,
                PpSeats = p.Value.Seats, PpList = CellOf(d.Chamber, true),
                OpSeats = opDraws ? o.Value.Seats : 0, OpList = opDraws ? CellOf(d.Chamber, false) : null,
            };
            return v.PpList == null ? null : v;
        }

        /// <summary>
        /// THE DRAW IN ONE CHAMBER - the gate's, and the one the veto diagnostic's planted-index enumeration runs (`UsVeto.PlantIndex`): the president's party's
        /// K from its list, the other party's from its own where it draws (<see cref="UsVeto.ChamberVote.OpList"/> not null), each through `UsVeto.Draw` on the
        /// play key (every draw not refused counted in `UsVeto.DrawCalls`; a budget vote refused), and whether the verdict at the drawn pair differs from the
        /// blocs' at (0, 0) - WHEN THE DRAW MATTERS.
        /// </summary>
        public static Crossing DrawCrossers(UsVeto.ChamberVote v, CountryId country, bool budgetVote, int masterSeed, DateTime firstDivision, string title, int chamber, string pp, string op)
        {
            UsVeto.Chamber which = chamber == Senate ? UsVeto.Chamber.Senate : UsVeto.Chamber.House;
            var c = new Crossing { PresidentsParty = pp, OtherParty = op };
            c.Kp = UsVeto.Draw(country, budgetVote, masterSeed, firstDivision, title, which, pp, v.PpSeats, v.PpList, out int jp);
            if (v.PpList != null && v.PpList.Count > 0 && !budgetVote) { c.IndexPp = jp; c.KeyPp = UsVeto.Key(masterSeed, firstDivision, title, which, pp); }
            if (v.OpList != null)
            {
                c.Ko = UsVeto.Draw(country, budgetVote, masterSeed, firstDivision, title, which, op, v.OpSeats, v.OpList, out int jo);
                if (v.OpList.Count > 0 && !budgetVote) { c.IndexOp = jo; c.KeyOp = UsVeto.Key(masterSeed, firstDivision, title, which, op); }
            }
            c.Matters = UsVeto.ChamberOutcome(v, c.Kp, c.Ko) != UsVeto.ChamberOutcome(v, 0, 0);
            return c;
        }

        private static PartyStance? StanceOf(List<PartyStance> stances, string party)
        {
            if (stances == null) { return null; }
            foreach (PartyStance s in stances) { if (s.Party.Abbrev == party) { return s; } }
            return null;
        }

        /// <summary>The Vice President's vote on an equal Senate division of the majority path - his party's stance in the Senate, else in the House, else none
        /// (`SenateDivision`'s own reading, the vote's day).</summary>
        private static int VicePresidentSide(Country country, Verdict verdict, DateTime on)
        {
            string vice = UsSenate.VicePresidentParty(country, on);
            return SideOf(verdict.Senate?.Stances, vice) ?? SideOf(verdict.House?.Stances, vice) ?? 0;
        }

        /// <summary>A defector side's reason, in the plate's words.</summary>
        private static string DefectorWords(int stance, int crossers) =>
            (crossers == 1 ? "one member" : crossers.ToString(CultureInfo.InvariantCulture) + " members") + " crossed the party's stance to vote "
            + (stance < 0 ? "for" : "against") + " - drawn defectors, counted because they decided the chamber's verdict";

        /// <summary>One chamber's vote on the president's veto (the block's THE OVERRIDE): on the passage's recorded sides in that chamber - YEA its FOR side, the
        /// crossers among them where they entered - against every other member of the division, the undecided and the unmeasured counting against (premise
        /// (B)); "two-thirds of the Members voting, a quorum being present" (CRS RS22654), so it carries iff YEA &gt;= 2 (Members - YEA) - `UsVeto.OverrideCarries`.</summary>
        public sealed class OverrideVote
        {
            public int Chamber;
            /// <summary>The chamber's passage the vote is held on (null on a projection).</summary>
            public DivisionRecord Passage;
            public int Yea, Members;
            public int Nay => Members - Yea;
            /// <summary>The votes it needs: the least YEA with YEA &gt;= 2 (Members - YEA), ceil(2 Members / 3).</summary>
            public int Required => (2 * Members + 2) / 3;
            public bool Carries;
        }

        /// <summary>THE PRESIDENT'S ANSWER to a bill both chambers sent, and the override (<see cref="Present"/>).</summary>
        public sealed class Veto
        {
            public string President, PresidentsParty;
            public bool PlayerPresident;
            public bool Vetoed;
            /// <summary>Why, in a log's words.</summary>
            public string Why;
            /// <summary>Rule (a)'s inputs: the president's party's House seats in the division, and its nays there.</summary>
            public int PpHouseSeats, PpHouseNays;
            /// <summary>The origin chamber's vote on the veto, and the other's - null where the origin sustained it (the other does not vote), and both null where
            /// nothing was vetoed.</summary>
            public OverrideVote First, Second;
            public bool Overridden => Vetoed && First != null && First.Carries && Second != null && Second.Carries;
            public bool Stands => !Vetoed || Overridden;
        }

        /// <summary>
        /// THE PRESIDENT'S ANSWER (the block's THE VETO) to a bill both chambers sent - <paramref name="passage"/>, the second chamber's division carrying the
        /// first's (`DivisionRecord.OtherChamber`), both passed - read off the recorded divisions, never re-voted. An AI president vetoes by rule (a): his party
        /// holds a House seat and more than half of its House members in the division voted nay (`UsVeto.NamedByA` - DETERMINISTIC, every act presented, budget
        /// acts included). A player-president's answer is the player's (<paramref name="playerVetoes"/>; R-US7, F8: "a player-president chooses"), and rule (a)
        /// is not read for him; where no answer is given he signs (since PS-6 US-21b the gate never presents a bill to him without one: with none on the day the
        /// bill is held on his docket, `SimulationManager.UsVetoGate`, `PresidentialDocket`). Where vetoed, the override: the origin chamber first, the other
        /// only where the origin overrode, the veto falling iff both do, both on the day of the answer - an AI president's the day both chambers sent it, a
        /// player-president's the day his answer is acted on (Poland's premise; the pocket veto is not modelled, stated), no cloture on an override. Reads only - the gate records
        /// (<see cref="RecordVeto"/>). Null where the passage is not a bill both chambers sent, or no president is found on the day.
        /// </summary>
        public static Veto Present(Country country, DivisionRecord passage, DateTime date, Func<DivisionRecord, bool> playerVetoes)
        {
            if (country == null || !UsVeto.Applies(country.Id) || passage?.OtherChamber == null || !passage.Passed || !passage.OtherChamber.Passed) { return null; }
            if (!PresidentOn(country, date, out string president, out string pp, out bool player)) { return null; }
            DivisionRecord house = passage.Chamber == House ? passage : passage.OtherChamber;
            var veto = new Veto { President = president, PresidentsParty = pp, PlayerPresident = player };
            if (!string.IsNullOrEmpty(pp))
            {
                foreach (DivisionSide s in house.Sides)
                {
                    if (s.Abbrev != pp) { continue; }
                    veto.PpHouseSeats += s.Seats;
                    if (s.Side < 0) { veto.PpHouseNays += s.Seats; }
                }
            }
            if (player)
            {
                veto.Vetoed = playerVetoes != null && playerVetoes(passage);
                veto.Why = veto.Vetoed ? "the player-president's veto" : "the player-president signs";
            }
            else
            {
                veto.Vetoed = UsVeto.NamedByA(veto.PpHouseSeats, veto.PpHouseNays);
                string party = string.IsNullOrEmpty(pp) ? "no party" : pp;
                veto.Why = string.IsNullOrEmpty(pp) ? "a president with no party vetoes nothing"
                    : house.Sides.Count == 0 ? "nothing was asked; the president's party (" + party + ") did not vote against it"   // an uncontested passage records no sides
                    : veto.PpHouseSeats < 1 ? "the president's party (" + party + ") holds no House seat"
                    : string.Format(CultureInfo.InvariantCulture, "the president's party ({0}): {1} of its {2} House members voted against it - {3}", party, veto.PpHouseNays, veto.PpHouseSeats,
                        veto.Vetoed ? "more than half, so rule (a) vetoes it" : "not more than half, so rule (a) signs it");
            }
            if (!veto.Vetoed) { return veto; }
            veto.First = OverrideOf(country, passage.OtherChamber);   // the origin voted first: the second chamber's record carries it
            if (veto.First.Carries) { veto.Second = OverrideOf(country, passage); }
            return veto;
        }

        /// <summary>A chamber's vote on the veto over its passage's record (the gate's): contested where the passage recorded sides.</summary>
        private static OverrideVote OverrideOf(Country country, DivisionRecord passage)
        {
            int yea = 0, sides = 0;
            foreach (DivisionSide s in passage.Sides)
            {
                sides += s.Seats;
                if (s.Side > 0) { yea += s.Seats; }
            }
            return OverrideCount(country, passage.Chamber, passage.Sides.Count > 0, yea, sides, passage.Sworn, passage);
        }

        /// <summary>A chamber's vote on the veto over a projected division (the page's): the same count on the blocs' sides - contested, by the same test as the
        /// record's, where it carries sides (its stances, which <see cref="Record"/> writes one side each).</summary>
        private static OverrideVote OverrideOf(Country country, Division d)
        {
            int sides = 0;
            foreach (PartyStance s in d.Stances) { sides += s.Seats; }
            return OverrideCount(country, d.Chamber, d.Stances.Count > 0, d.For, sides, d.Sworn, null);
        }

        /// <summary>
        /// THE OVERRIDE'S COUNT IN ONE CHAMBER - ONE RULE, which the gate (<see cref="Present"/>, <see cref="RecordVeto"/>) and the projection
        /// (<see cref="ProjectVeto"/>) both read, so the two cannot decide one bill two ways. "Two-thirds of the Members voting, a quorum being present" (CRS
        /// RS22654): it carries iff YEA &gt;= 2 (the base - YEA), every member of the base voting (the block's premise (B)).
        /// <list type="bullet">
        /// <item>A CONTESTED passage: YEA is its recorded FOR side - the drawn crossers among it where they entered its division, no fresh draw (the block's
        /// SIDES) - and the base is its recorded sides, the undecided and the unmeasured counting against; the Senate's base the senators sworn where they are
        /// more (the caucus within).</item>
        /// <item>AN UNCONTESTED passage - one that passed unasked, its concern moving nothing, so no member stood against it (US-18: "an empty concern passes every
        /// chamber unasked") - overrides: YEA is the whole base and no member is against. A READING, DECLARED (the review of 54f10b8d, M1): the passage records
        /// no sides, so the base is the chamber's seated members on the day - the House's seats as seated (<see cref="Sworn"/> of `Country.ParliamentSeats`),
        /// the Senate's senators sworn. US-22 tables continuing resolutions that may pass unasked and be vetoed; this is the reading they meet.</item>
        /// </list>
        /// <para>CONTESTED IS ONE TEST (the review of dc32c5ff, L-b): the passage carries SIDES - a record's `DivisionRecord.Sides`, a projected division's
        /// <see cref="Division.Stances"/>, the same list (<see cref="Record"/> writes one side per stance, the drawn defectors beside their party's where they
        /// entered). A division asked something carries one stance per seated party the party system knows (`StanceModel.Stances`, `StanceModel.StancesOver`);
        /// one asked nothing carries none (<see cref="Division.Contested"/> false). The two coincide but for a division asked something in a chamber where no
        /// seated party is known to the party system - it carries no sides, and is counted here as passed unasked, by the gate and the projection alike.</para>
        /// </summary>
        private static OverrideVote OverrideCount(Country country, int chamber, bool recordedSides, int yea, int sides, int sworn, DivisionRecord passage)
        {
            int members = chamber == Senate ? Math.Max(sides, sworn) : sides > 0 ? sides : Sworn(country?.ParliamentSeats);
            if (!recordedSides) { yea = members; }   // passed unasked: no member against
            return new OverrideVote { Chamber = chamber, Passage = passage, Yea = yea, Members = members, Carries = UsVeto.OverrideCarries(yea, members - yea) };
        }

        /// <summary>
        /// THE VETO ON THE RECORD (the gate's writer, called only where <paramref name="veto"/> vetoed): the passage's ceremony moves to the vote on the veto - the
        /// passage marked a motion, as Poland's (§761's precedent) - and each chamber's vote on it is a division of its own, written in the order held: the
        /// origin's, then the other's only where the origin overrode, the second carrying the first (`DivisionRecord.OtherChamber`, so the plate shows both).
        /// Each copies its chamber's passage sides (the override is voted by the passage's recorded sides, the crossers where they entered, no fresh draw), its
        /// line the two thirds (`DivisionRecord.Required`, never at cloture), its base the members (`DivisionRecord.Sworn`), passed where it overrode. Returns the
        /// record that carries the ceremony - the last written.
        /// </summary>
        public static DivisionRecord RecordVeto(Country country, DivisionRecord passage, Veto veto, DateTime date)
        {
            passage.Motion = true;
            DivisionRecord first = RecordOverride(country, passage, veto, veto.First, null, date);
            if (veto.Second == null) { return first; }
            first.Motion = true;
            DivisionRecord second = RecordOverride(country, passage, veto, veto.Second, veto.First, date);
            second.OtherChamber = first;
            return second;
        }

        private static DivisionRecord RecordOverride(Country country, DivisionRecord passage, Veto veto, OverrideVote vote, OverrideVote before, DateTime date)
        {
            var sides = new List<DivisionSide>();
            foreach (DivisionSide s in vote.Passage.Sides)
            {
                bool presidents = s.Abbrev == veto.PresidentsParty;
                string reason = s.Defectors ? (s.Reason ?? string.Empty) + "; on the veto they vote as they voted on the bill"
                    : presidents ? (s.Side < 0 ? "the President's party - against the bill; against overriding the veto" : s.Side > 0 ? "the President's party - for the bill; for overriding the veto"
                        : "the President's party - undecided on the bill; not for the override, so counted against it")
                    : s.Side > 0 ? "voted for the bill; for overriding the veto" : s.Side < 0 ? "voted against the bill; against overriding the veto" : "undecided on the bill; not for the override, so counted against it";
                string shortReason = s.Defectors ? s.ReasonShort : s.Side > 0 ? "for the override" : "against the override";
                sides.Add(new DivisionSide { Abbrev = s.Abbrev, ShortName = s.ShortName, Seats = s.Seats, Side = s.Side, Alignment = s.Alignment, Defectors = s.Defectors, Reason = reason, ReasonShort = shortReason });
            }
            DivisionRecord record = country.Divisions.Append(OverrideTitle(country.Id, passage.Title, veto, before, vote), date, vote.Passage.Alignment, vote.Carries, vote.Passage.Direction, vote.Passage.Axis, sides);
            record.Chamber = vote.Chamber;
            record.Origin = passage.Origin;
            record.Reconciliation = passage.Reconciliation;
            record.Required = vote.Required;
            record.Sworn = vote.Members;
            return record;
        }

        /// <summary>The vote on the veto's title as a player reads it: who vetoed what, and each chamber's vote so far - its YEA against the two thirds it needed.</summary>
        private static string OverrideTitle(CountryId id, string bill, Veto veto, OverrideVote before, OverrideVote vote)
        {
            string Vote(OverrideVote v) => string.Format(CultureInfo.InvariantCulture, "the {0} {1}, {2} for, {3} needed of {4}", ChamberName(id, v.Chamber), v.Carries ? "overrides" : "sustains the veto", v.Yea, v.Required, v.Members);
            string who = veto.PlayerPresident ? "you, " + veto.President : veto.President;
            return "Vetoed by the President (" + who + "): " + bill + " - " + (before != null ? Vote(before) + "; " : string.Empty) + Vote(vote)
                + (before != null && before.Carries && vote.Carries ? " - two thirds in each chamber, so it becomes law" : !vote.Carries ? " - the veto stands" : string.Empty);
        }

        /// <summary>
        /// THE VETO AS A PAGE CAN KNOW IT (the block's item 8: "a projection reads exact probabilities and never draws; only the gate draws"): the president of
        /// the vote's day, whether the bill is on the veto path, each chamber's exact chance to pass it over the drawn index pairs (`UsVeto.PassProbability`, no
        /// `UsVeto.Draw`), whether rule (a) would veto it, and the override on the blocs' sides. NO SURFACE SHOWS A FITTED CHANCE (DECLARED, US-21): the
        /// chances are here for the veto diagnostic; a page reads the deterministic facts beside them.
        /// </summary>
        public sealed class VetoProjection
        {
            public string President, PresidentsParty;
            public bool PlayerPresident;
            /// <summary>Ordinary legislation the president's party stands AGAINST in a chamber - defectors are drawn at the vote.</summary>
            public bool OnVetoPath;
            public bool BudgetVote;
            /// <summary>An act not presented to the president (<see cref="IsUnpresented"/>): no draw, no veto - the page says so.</summary>
            public bool Unpresented;
            /// <summary>The exact chance the first and the second chamber pass it - over the drawn index pairs where a draw is made, 1/1 or 0/1 at the blocs' count
            /// where none is. The chance the bill is sent is their product (the draws independent, the block's item 5).</summary>
            public UsVeto.Odds FirstPasses, SecondPasses;
            /// <summary>Both chambers pass it at the blocs' count (K = 0).</summary>
            public bool BlocsSend;
            /// <summary>Rule (a) on the blocs' House division: an AI president vetoes it if it reaches him (DETERMINISTIC); false for a player-president.</summary>
            public bool NamedByA;
            /// <summary>The override on the blocs' sides, the origin first (where the draw is what sends a bill, the crossers that send it would enter its sides).</summary>
            public OverrideVote First, Second;
            public bool OverrideCarries => First != null && First.Carries && Second != null && Second.Carries;
        }

        /// <summary>The projection (<see cref="VetoProjection"/>) over <paramref name="verdict"/>, the blocs' two divisions on the vote's day - null outside the USA
        /// or where no president is found. Never draws: `UsVeto.DrawCalls` does not move.</summary>
        public static VetoProjection ProjectVeto(Country country, BillConcern concern, DateTime on, Verdict verdict)
        {
            if (country == null || !UsVeto.Applies(country.Id) || concern == null || verdict == null) { return null; }
            if (!PresidentOn(country, on, out string president, out string pp, out bool player)) { return null; }
            var p = new VetoProjection { President = president, PresidentsParty = pp, PlayerPresident = player, BudgetVote = concern.BudgetVote, Unpresented = concern.Unpresented, BlocsSend = verdict.House.Passed && verdict.Senate.Passed };
            UsVeto.Odds Chance(Division d)
            {
                UsVeto.ChamberVote v = concern.BudgetVote || concern.Unpresented || !DrawInstalled ? null : DrawableVote(country, d, verdict, on, pp);
                if (v != null) { p.OnVetoPath = true; return UsVeto.PassProbability(v); }
                return new UsVeto.Odds(d.Passed ? 1 : 0, 1);
            }
            p.FirstPasses = Chance(verdict.First);
            p.SecondPasses = Chance(verdict.Second);
            PartyStance? ppHouse = string.IsNullOrEmpty(pp) ? null : StanceOf(verdict.House.Stances, pp);
            int seats = ppHouse?.Seats ?? 0;
            p.NamedByA = !player && !concern.Unpresented && UsVeto.NamedByA(seats, ppHouse != null && ppHouse.Value.Measured && ppHouse.Value.Side < 0 ? seats : 0);
            p.First = OverrideOf(country, verdict.First);
            p.Second = OverrideOf(country, verdict.Second);
            return p;
        }

        /// <summary>A chamber's name as a page says it - the House, the Senate (the country's own names, `StartBrief`).</summary>
        public static string ChamberName(CountryId id, int chamber) => chamber == Senate ? StartBrief.SecondChamberOf(id) ?? "Senate" : StartBrief.ChamberOf(id);

        /// <summary>
        /// The verdict in a page's words, upper case: where it stops - "STOPPED IN THE SENATE AT CLOTURE - n FOR, m NEEDED OF w SWORN" - or that both
        /// chambers pass it; the figures the projection's own. Null for a verdict a single chamber gave.
        /// </summary>
        public static string StopWords(CountryId id, Verdict verdict)
        {
            if (verdict == null) { return null; }
            Division stop = verdict.Stopping;
            if (stop == null) { return "BOTH CHAMBERS PASS IT"; }
            return "STOPPED IN THE " + ChamberName(id, stop.Chamber).ToUpperInvariant() + (stop.Cloture ? " AT CLOTURE" : string.Empty) + " - " + Figures(id, stop);
        }

        /// <summary>The verdict in a count's room, upper case - the chamber that stops the bill and its count there (the votes FOR against the line at cloture),
        /// or, where both pass it, each chamber's count in the order they vote. Null for a verdict a single chamber gave.</summary>
        public static string CountWords(CountryId id, Verdict verdict)
        {
            if (verdict == null) { return null; }
            string F(int n) => n.ToString(CultureInfo.InvariantCulture);
            string Count(Division d) => !d.Contested ? "UNCONTESTED" : d.Cloture ? F(d.For) + " OF " + F(d.Required) + " FOR CLOTURE" : F(d.For) + " ⁄ " + F(d.Against);
            string Name(Division d) => ChamberName(id, d.Chamber).ToUpperInvariant();
            Division stop = verdict.Stopping;
            if (stop != null) { return "STOPPED IN THE " + Name(stop) + " · " + Count(stop); }
            return Name(verdict.First) + " " + Count(verdict.First) + " · " + Name(verdict.Second) + " " + Count(verdict.Second);
        }

        /// <summary>Both chambers' divisions in a slip's words, in the order the bill takes them - the second said as never reached where the first rejects the
        /// bill (the projection computes it all the same, so the page can say what it would do).</summary>
        public static IEnumerable<string> OrderWords(CountryId id, Verdict verdict)
        {
            if (verdict == null) { yield break; }
            yield return ChamberLine(id, verdict.First);
            yield return verdict.First.Passed ? ChamberLine(id, verdict.Second)
                : "IT NEVER REACHES THE " + ChamberName(id, verdict.Second.Chamber).ToUpperInvariant() + ", WHICH WOULD " + (verdict.Second.Passed ? "PASS" : "REJECT") + " IT - " + Figures(id, verdict.Second);
        }

        /// <summary>The rule each chamber counts by, in a slip's words - the House's, and the Senate's for this bill's procedure.</summary>
        public static string RuleWords(Verdict verdict) => verdict == null ? null
            : "EACH CHAMBER COUNTS: THE HOUSE A MAJORITY OF THOSE VOTING, A TIE FAILING; THE SENATE " + (verdict.Reconciliation
                ? "BY RECONCILIATION - A MAJORITY, THE VICE PRESIDENT BREAKING A TIE, NO CLOTURE; NO YEARLY LIMIT ON RECONCILIATION IS MODELLED"
                : "AT CLOTURE - THREE FIFTHS OF THE SENATORS SWORN, THEN A MAJORITY");

        /// <summary>One chamber's division in a page's words: its name, its procedure, its figures and whether it passes.</summary>
        public static string ChamberLine(CountryId id, Division d)
        {
            if (d == null) { return null; }
            string name = "THE " + ChamberName(id, d.Chamber).ToUpperInvariant();
            string procedure = d.Chamber == House ? " (A MAJORITY OF THOSE VOTING)" : d.Cloture ? " AT CLOTURE (THREE FIFTHS OF THE SWORN)" : " BY RECONCILIATION (A MAJORITY, NO CLOTURE)";
            return name + procedure + " " + (d.Passed ? "PASSES" : "REJECTS") + " IT - " + Figures(id, d);
        }

        private static string Figures(CountryId id, Division d)
        {
            if (!d.Contested) { return "NOTHING ASKED, UNCONTESTED"; }
            string F(int n) => n.ToString(CultureInfo.InvariantCulture);
            if (d.Cloture) { return F(d.For) + " FOR, " + F(d.Required) + " NEEDED OF " + F(d.Sworn) + " SWORN"; }
            string tie = d.VicePresidentParty == null ? string.Empty
                : ", EQUALLY DIVIDED - THE VICE PRESIDENT (" + PartySystems.ShortName(id, d.VicePresidentParty) + ") " + (d.VicePresidentVote > 0 ? "VOTING FOR" : d.VicePresidentVote < 0 ? "VOTING AGAINST" : "CASTING NO VOTE, SO THE TIE FAILS");
            return F(d.For) + " FOR, " + F(d.Against) + " AGAINST" + tie;
        }
    }
}
