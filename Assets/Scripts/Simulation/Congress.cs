using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;

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
    /// The drawn crossers on the veto path are US-21's (R-US17 rule (c)); here every division is the blocs', K = 0.</description></item>
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
    /// (<see cref="RidesReconciliation(Country, BudgetBill)"/>): until US-22 splits the US budget into its acts on E2's machinery, a bill carrying the
    /// appropriation carries its cloture whole. THE ONCE-A-YEAR LIMIT IS NOT MODELLED (DECLARED): the statute ties reconciliation to "a concurrent resolution
    /// on the budget for any fiscal year" (641(a)), and the game models no concurrent resolution - so neither its directives nor any bound on how many
    /// reconciliation bills one resolution yields; every bill the reading names rides reconciliation whenever it is voted - a trifecta passes the tax and
    /// welfare programme bills and the rate-only budgets by its Senate majority as often as its bills come round, and the reconciliation slip says no yearly
    /// limit is modelled (<see cref="RuleWords"/>). ASKED with the table's placements (<see cref="IsReconciliationKind"/>). R-US17's BUDGET VOTE (play) is "a
    /// reconciliation bill (US-18)", so this table is that ruled block's input at US-21: every bill it sends by reconciliation is a budget vote that draws no
    /// crossers. The Byrd rule is not modelled (R-US9 (a), stated).</para>
    /// <para><b>THE GOVERNMENT'S OWN BUDGET STAYS IN THE HOUSE UNTIL US-22 (DECLARED).</b> The plan dates the AI president's budget meeting Congress at US-22
    /// (risk 19: "a US game's AI president's budget meets Congress (US-22)"; US-22: the AI's budget "goes through the same procedure"), so a concern the
    /// government authored (`BillConcern.GovernmentAuthored`, set from `BudgetBill.GovernmentBill` alone) is voted by its first chamber alone until then -
    /// one predicate, <see cref="VotesBothChambers"/>, which the gate (`ParliamentSystem.Divide`), the dated verdict and the page (`ChamberVerdicts`) all
    /// read, so the pending card and the gate cannot disagree. THE ASYMMETRY US-22 CLOSES: the player's own budget meets cloture or reconciliation, the AI
    /// president's the House alone.</para>
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
        /// <paramref name="country"/> - the country's bills do (<see cref="Applies"/>), and the bill is not the government's own budget, which stays in its
        /// first chamber until US-22 (the class note: DECLARED, the asymmetry US-22 closes). Read by `ParliamentSystem.Divide`, the dated
        /// `ParliamentSystem.WouldBillPass` and `ChamberVerdicts`, so the gate and the page give one answer.
        /// </summary>
        public static bool VotesBothChambers(Country country, BillConcern concern) => Applies(country) && !(concern != null && concern.GovernmentAuthored);

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
        /// spending, F6's reading of a benefit level as set by statute) and the fund's rules ride it.</summary>
        public static bool RidesReconciliation(Country country, BudgetBill bill)
            => country != null && country.Id == CountryId.USA && bill != null && !ChangesSpendingLines(country, bill) && !bill.ChangesPensionAge(country);

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
        /// Every other country bears every bill exactly as before, where only the government could table one. The budget's failed vote
        /// (`ParliamentSystem.ApplyBillResult`) is not routed here - it differs only for a budget pending across a change of presidency, and US-22 rebuilds that
        /// path (DECLARED; the review's second-pass N1). Who should bear a congressional majority's bill
        /// instead - its own party's standing, or no one - is Elias's to rule.
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
        {
            Verdict verdict = Project(country, concern, date, origin);
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
            if (!passed && GovernmentBears(country, concern)) { ParliamentSystem.RecordBillLost(country, date); }   // PS-6 US-19 (the review's M2): the government's loss only where the bill was the government's
            return last;
        }

        private static DivisionRecord Record(Country country, string title, BillConcern concern, Division d, Verdict verdict, DateTime date, BillAxis axis)
        {
            var sides = new List<DivisionSide>();
            foreach (PartyStance stance in d.Stances)
            {
                sides.Add(new DivisionSide { Abbrev = stance.Party.Abbrev, ShortName = stance.Party.ShortName, Seats = stance.Seats, Side = stance.Side, Alignment = stance.Alignment, Reason = StanceModel.ReasonLine(stance), ReasonShort = StanceModel.ReasonShort(stance) });
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
