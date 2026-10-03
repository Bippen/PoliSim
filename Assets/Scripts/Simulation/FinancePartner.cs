using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// THE FINANCE PARTNER. §716 (Elias's ruling of 2026-10-01, item 7) gave a party holding FinanceTreasury - other than the head of government's,
    /// and outside the head's parliamentary group (the CSU is the Union's) - Finance's levers "in fact": it stepped the household rates from its own
    /// positions and Finance's levers were refused to a player head. §755 (Elias's ruling A2, 2026-10-02) replaces that: <i>"Finance partner: nothing
    /// is frozen. A partner holding Finance acts through the fiscal stance only. The rule is symmetric: it is the same whether the player or the AI
    /// holds Finance."</i>
    /// <list type="bullet">
    /// <item><description><b>Nothing is frozen</b>: the head of government keeps every lever, Finance's included (the tax programme, the fiscal,
    /// monetary and electricity-tax laws, the Finance cabinet decision, the household rates on its budget) - no rate is held back from it and no
    /// decision resolved over its head.</description></item>
    /// <item><description><b>The partner acts through the fiscal stance only</b> - the balance, in points of GDP: it moves the book toward the
    /// stance it asks, at most <see cref="StepPointsPerYear"/> a year, through the AI finance ministry's own uniform instrument (one percentage on
    /// every line not otherwise written - restored for an expansion, cut for a tightening with what the lines' clamps leave on the income tax and
    /// VAT by the same points). The partner's other levers are none.</description></item>
    /// <item><description><b>Symmetric</b>: the stance an AI partner asks is read from its position against the head's (CHES `lrecon`,
    /// <see cref="StancePointsPerChesPoint"/> a point - a partner to the head's left asks an expansion, one to its right a tightening); a player
    /// partner asks the stance its own dial sets (<see cref="Country.FinanceStancePlayerTarget"/>). One step, one instrument, one pace, one count
    /// (<see cref="Country.FinanceStanceApplied"/>, kept per holder) for both.</description></item>
    /// </list>
    /// The treaty's rule has first claim: in an AI government the ministry (<see cref="AiFinanceMinistry"/>) writes first, and a line or rate it wrote is
    /// not moved here that year.
    /// </summary>
    public static class FinancePartner
    {
        /// <summary>[AUTHORED-DRAFT] (play-calibration list, 26th, re-cut §755): the stance an AI partner asks, points of GDP per CHES `lrecon` point
        /// between the head and it (+ = an expansion: a partner to the head's left) - the SPD under Merz's CDU asks +0.78 (about 3.1 points). §716's ask was
        /// read on `redistribution` (0.875 points of the household rates); the stance reads `lrecon`, the economic left-right CHES scores a fiscal position on.</summary>
        public const float StancePointsPerChesPoint = 0.25f;
        /// <summary>[AUTHORED-DRAFT] (play-calibration list, 26th): the most the stance moves in a year, points of GDP - a quarter of a point.</summary>
        public const float StepPointsPerYear = 0.25f;
        /// <summary>[AUTHORED-DRAFT] (§755): the reach of a player partner's dial either way, points of GDP - the widest an AI partner can ask (ten CHES
        /// points at <see cref="StancePointsPerChesPoint"/>), so the player's dial reaches no further than the AI's rule.</summary>
        public const float PlayerTargetLimit = 2.5f;
        /// <summary>CONVENTION - a step below this many points of GDP is not written (the ministry's noise floor).</summary>
        private const float MinPoints = 0.01f;

        /// <summary>CONVENTION - a year, the boundary's own interval (SimulationManager.DaysPerTurn). §716 (found by the check's own day tick): the partner steps AT MOST ONCE IN ANY YEAR. The boundary is a year apart, but the
        /// government's own budget bill can be tabled more than once in one (the incoming government's arrival window, then the fiscal-year date),
        /// and each carried a step - three bills adopted moved the rates three quarter-points in a year.</summary>
        public const int StepIntervalDays = 365;

        /// <summary>The partner's step is due on <paramref name="asOf"/>: it has never stepped, or a year has passed since it last did.</summary>
        public static bool StepDue(Country country, System.DateTime asOf) =>
            country.FinancePartnerSteppedOn == System.DateTime.MinValue || (asOf - country.FinancePartnerSteppedOn).TotalDays >= StepIntervalDays;

        /// <summary>Records a step the BOUNDARY wrote - never a preview's: the date (the year's step spent) and the count, which lands with the decision.</summary>
        public static void Record(Country country, Written written, System.DateTime on)
        {
            if (written == null || !written.Any) { return; }
            country.FinancePartnerSteppedOn = on;
            Credit(country, written.Holder, written.AppliedBefore, written.StancePoints);
        }

        /// <summary>§755 (the review's defect 1): the GOVERNMENT'S BILL path - tabling spends the year's step (§716's premise: a rejected bill spends it) and
        /// the step rides the bill (<see cref="BudgetBill.FinanceStanceHolder"/> and its fields); the count moves only if the chamber adopts it
        /// (<see cref="CreditAdopted"/>) - a bill rejected, or the alternative adopted, moved nothing, and nothing is counted.</summary>
        public static void Table(Country country, Written written, BudgetBill bill, System.DateTime on)
        {
            if (written == null || !written.Any || bill == null) { return; }
            country.FinancePartnerSteppedOn = on;
            bill.FinanceStanceHolder = written.Holder;
            bill.FinanceStancePoints = written.StancePoints;
            bill.FinanceStanceRatePoints = written.RatePoints;   // §768
            bill.FinanceStanceAppliedBefore = written.AppliedBefore;
            bill.FinanceStanceGovernment = country.Government?.FormedOn ?? System.DateTime.MinValue;
        }

        /// <summary>§755: the government's bill ADOPTED - its step counted, where the government that tabled it still sits (another by the vote's day: the
        /// step is not its partner's). §768 (Elias's ruling D4; the review's defect 3): where the bill's rates were withheld - Poland's tax act fell or its
        /// veto stood - <paramref name="ratesLanded"/> is false and only the part of the step that landed is counted (the lines', not the rates').</summary>
        public static void CreditAdopted(Country country, BudgetBill bill, bool ratesLanded = true)
        {
            if (bill == null || string.IsNullOrEmpty(bill.FinanceStanceHolder) || country.Government == null || country.Government.FormedOn != bill.FinanceStanceGovernment) { return; }
            Credit(country, bill.FinanceStanceHolder, bill.FinanceStanceAppliedBefore, ratesLanded ? bill.FinanceStancePoints : bill.FinanceStancePoints - bill.FinanceStanceRatePoints);
        }

        private static void Credit(Country country, string holder, float appliedBefore, float points)
        {
            country.FinanceStanceHolder = holder;
            country.FinanceStanceGovernment = country.Government?.FormedOn ?? System.DateTime.MinValue;
            country.FinanceStanceApplied = appliedBefore + points;
        }

        /// <summary>The household rates - the two the instrument's tightening raises when the lines' clamps leave a remainder.</summary>
        public static bool IsHouseholdRate(TaxType type) => type == TaxType.IncomeTax || type == TaxType.VAT;

        /// <summary>What the partner wrote into a decision this turn, so the caller can withdraw it after the turn (the ministry's discipline, §388).</summary>
        public sealed class Written
        {
            public readonly List<SpendingCategory> Lines = new List<SpendingCategory>();
            public readonly List<TaxType> Taxes = new List<TaxType>();
            public readonly List<string> Moves = new List<string>();
            /// <summary>The party whose stance this is.</summary>
            public string Holder;
            /// <summary>The stance this step moved, points of GDP (+ = expansion) - what the lines and rates written move at today's book.</summary>
            public float StancePoints;
            /// <summary>The stance the holder had moved before this step.</summary>
            public float AppliedBefore;
            /// <summary>§768: the part of <see cref="StancePoints"/> the household rates carry (0 where the lines took it all) - what a Polish tax act that
            /// falls withholds from the count (<see cref="CreditAdopted"/>).</summary>
            public float RatePoints;
            /// <summary>§768: the amount the rates moved, in the book's money - the tightening's own tally, read for <see cref="RatePoints"/> alone.</summary>
            public float RateAmount;
            public bool Any => Lines.Count > 0 || Taxes.Count > 0;
        }

        /// <summary>The party holding Finance where it is not the head's own nor in the head's parliamentary group; null otherwise - and null on a
        /// PROVISIONAL government (§716's decision A): a stand-in the formation seats where the record names no cabinet (France's start) is the
        /// model's guess at a government, and a stance "run from its own positions" by a party in no government of record is not the record's.</summary>
        public static string Holder(Country country)
        {
            GovernmentRecord government = country?.Government;
            if (government == null || government.Provisional || string.IsNullOrEmpty(government.PmParty)) { return null; }
            foreach (KeyValuePair<string, List<CabinetPortfolio>> held in government.Portfolios)
            {
                if (held.Value == null || !held.Value.Contains(CabinetPortfolio.FinanceTreasury)) { continue; }
                return held.Key == government.PmParty || SameGroup(country.Id, held.Key, government.PmParty) ? null : held.Key;
            }
            return null;
        }

        /// <summary>The partner holding Finance where it is not the player's party - an AI partner (<c>SimulationManager.FinancePartnerOfPlayer</c>).</summary>
        public static string AiHolder(Country country)
        {
            string holder = Holder(country);
            return holder != null && holder != country.PlayerPartyAbbrev ? holder : null;
        }

        /// <summary>§755: the player's own party holds Finance as a partner - its dial sets the stance.</summary>
        public static bool PlayerHolds(Country country) =>
            country != null && !string.IsNullOrEmpty(country.PlayerPartyAbbrev) && Holder(country) == country.PlayerPartyAbbrev;

        private static bool SameGroup(CountryId id, string a, string b)
        {
            foreach ((string x, string y) in ChamberRules.JointGroups(id)) { if ((x == a && y == b) || (x == b && y == a)) { return true; } }
            return false;
        }

        /// <summary>A party's CHES `lrecon` (0 = most state, 10 = most market); false where it has no published position.</summary>
        public static bool TryEconomicPosition(CountryId id, string abbrev, out float lrecon)
        {
            lrecon = float.NaN;
            if (string.IsNullOrEmpty(abbrev)) { return false; }
            foreach (PoliticalParty party in PartySystems.For(id))
            {
                if (party.Abbrev != abbrev) { continue; }
                if (!party.HasPosition) { return false; }
                lrecon = party.LrEcon;
                return true;
            }
            return false;
        }

        /// <summary>The stance the holder asks, points of GDP (+ = expansion): a player partner's dial; an AI partner's
        /// <see cref="StancePointsPerChesPoint"/> for every `lrecon` point the head stands right of it. False where an AI partner or the head has no position.</summary>
        public static bool Target(Country country, string holder, out float target)
        {
            target = 0f;
            if (string.IsNullOrEmpty(holder)) { return false; }
            if (holder == country.PlayerPartyAbbrev) { target = Mathf.Clamp(country.FinanceStancePlayerTarget, -PlayerTargetLimit, PlayerTargetLimit); return true; }
            if (!TryEconomicPosition(country.Id, holder, out float holderAt) || !TryEconomicPosition(country.Id, country.Government.PmParty, out float headAt)) { return false; }
            target = StancePointsPerChesPoint * (headAt - holderAt);
            return true;
        }

        /// <summary>The stance <paramref name="holder"/> has moved so far IN THIS GOVERNMENT - its own count; a new holder, or the same one in a new government
        /// (§755, the review's defect 2: the target is read against the head that sits), starts from the book as it stands (zero).</summary>
        public static float Applied(Country country, string holder) =>
            !string.IsNullOrEmpty(holder) && country.FinanceStanceHolder == holder && country.Government != null && country.FinanceStanceGovernment == country.Government.FormedOn
                ? country.FinanceStanceApplied : 0f;

        /// <summary>
        /// Writes the holder's yearly step toward its stance into <paramref name="decision"/> - on the lines and rates the decision carries none of its own
        /// for, nor <paramref name="claimedLines"/>/<paramref name="claimedTaxes"/> (what the ministry writes at the boundary, which the preview reads
        /// without running it) - and returns what it wrote so the caller can Withdraw it after the turn. It changes nothing in the country.
        /// </summary>
        public static Written Apply(Country country, PolicyDecision decision, System.DateTime asOf,
            ICollection<SpendingCategory> claimedLines = null, ICollection<TaxType> claimedTaxes = null)
        {
            var written = new Written();
            string holder = decision != null ? Holder(country) : null;
            if (holder == null || !StepDue(country, asOf) || !Target(country, holder, out float target)) { return written; }
            float applied = Applied(country, holder);
            float step = Mathf.Clamp(target - applied, -StepPointsPerYear, StepPointsPerYear);
            float gdp = country.State.NominalGdp;
            if (Mathf.Abs(step) < MinPoints || gdp <= 0f || country.SpendingLines == null) { return written; }
            float amount = Mathf.Abs(step) / 100f * gdp;
            float moved = step > 0f ? Expand(country, amount, decision, claimedLines, written) : Tighten(country, amount, decision, claimedLines, claimedTaxes, written);
            if (!written.Any) { return written; }
            written.Holder = holder;
            written.AppliedBefore = applied;
            written.StancePoints = Mathf.Sign(step) * moved / gdp * 100f;
            written.RatePoints = Mathf.Sign(step) * written.RateAmount / gdp * 100f;   // §768: the rates' share, its own store - StancePoints's untouched
            written.Moves.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "the stance {0:+0.00;-0.00} pp of GDP ({1:+0.00;-0.00} moved before, {2:+0.00;-0.00} asked)",
                written.StancePoints, applied, target));
            return written;
        }

        /// <summary>Removes what Apply wrote, so a decision object a caller hands in again next turn carries nothing of this turn's partner.</summary>
        public static void Withdraw(PolicyDecision decision, Written written)
        {
            if (decision == null || written == null) { return; }
            foreach (SpendingCategory c in written.Lines) { decision.SpendingLineChanges.Remove(c); }
            foreach (TaxType type in written.Taxes) { decision.TaxRateOverrides.Remove(type); }
        }

        private static bool Free(SpendingLine line, PolicyDecision decision, ICollection<SpendingCategory> claimed) =>
            !line.Pinned && !decision.SpendingLineChanges.ContainsKey(line.Category) && (claimed == null || !claimed.Contains(line.Category));

        private static float RangeOf(SpendingLine line) =>
            line.IsMandatory ? SimulationManager.MandatoryPercentChangeRangeForRules : SimulationManager.DiscretionaryPercentChangeRangeForRules;

        /// <summary>The ministry's release side (AiFinanceMinistry.RestoreLinesUniformly): one uniform percentage on every free line, clamped as the
        /// applier clamps it. Returns the nominal amount given.</summary>
        private static float Expand(Country country, float toGive, PolicyDecision decision, ICollection<SpendingCategory> claimed, Written written)
        {
            float scope = 0f;
            foreach (SpendingLine line in country.SpendingLines) { if (Free(line, decision, claimed)) { scope += line.Amount; } }
            if (scope <= 0f) { return 0f; }
            float percent = toGive / scope * 100f, given = 0f;
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (!Free(line, decision, claimed)) { continue; }
                float applied = Mathf.Min(percent, RangeOf(line));
                if (applied <= 0f) { continue; }
                decision.SpendingLineChanges[line.Category] = applied;
                written.Lines.Add(line.Category);
                given += line.Amount * applied / 100f;
            }
            return given;
        }

        /// <summary>The ministry's tightening (AiFinanceMinistry.CutLinesUniformly, mandatory lines included, then RaiseHouseholdRates): one uniform cut on
        /// every free line, clamped as the applier clamps it, and what the clamps leave on the income tax and VAT by the same points. Returns the nominal
        /// amount taken.</summary>
        private static float Tighten(Country country, float toTake, PolicyDecision decision, ICollection<SpendingCategory> claimedLines, ICollection<TaxType> claimedTaxes, Written written)
        {
            float scope = 0f;
            foreach (SpendingLine line in country.SpendingLines) { if (Free(line, decision, claimedLines)) { scope += line.Amount; } }
            float taken = 0f;
            if (scope > 0f)
            {
                float percent = toTake / scope * 100f;
                foreach (SpendingLine line in country.SpendingLines)
                {
                    if (!Free(line, decision, claimedLines)) { continue; }
                    float applied = Mathf.Min(percent, RangeOf(line));
                    if (applied <= 0f) { continue; }
                    decision.SpendingLineChanges[line.Category] = -applied;
                    written.Lines.Add(line.Category);
                    taken += line.Amount * applied / 100f;
                }
            }
            float shortfall = toTake - taken;
            if (shortfall <= 0f) { return taken; }
            float baseTotal = 0f;
            foreach (TaxLine line in country.TaxLines)
            {
                if (line.IsImplemented && IsHouseholdRate(line.Type) && !decision.TaxRateOverrides.ContainsKey(line.Type) && (claimedTaxes == null || !claimedTaxes.Contains(line.Type))) { baseTotal += TaxBases.Base(country, line.Type); }
            }
            if (baseTotal <= 0f) { return taken; }
            float points = shortfall / baseTotal * 100f;
            if (points < MinPoints) { return taken; }
            foreach (TaxLine line in country.TaxLines)
            {
                if (!line.IsImplemented || !IsHouseholdRate(line.Type) || decision.TaxRateOverrides.ContainsKey(line.Type) || (claimedTaxes != null && claimedTaxes.Contains(line.Type))) { continue; }
                float to = Mathf.Min(line.MaxRate, line.Rate + points);
                if (to <= line.Rate) { continue; }
                decision.TaxRateOverrides[line.Type] = to;
                written.Taxes.Add(line.Type);
                taken += TaxBases.Base(country, line.Type) * (to - line.Rate) / 100f;
                written.RateAmount += TaxBases.Base(country, line.Type) * (to - line.Rate) / 100f;   // §768: the same term, tallied apart - `taken`'s sum untouched
            }
            return taken;
        }
    }
}
