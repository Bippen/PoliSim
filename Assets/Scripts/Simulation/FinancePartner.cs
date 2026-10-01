using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// §716 (Elias's ruling of 2026-10-01, item 7: <i>"A partner holding Finance holds its levers in fact, run from its own positions, under a player
    /// chancellor too."</i>) THE FINANCE PARTNER. Where a party other than the head of government's - and outside the head's own parliamentary group
    /// (the CSU is the Union's) - holds FinanceTreasury, Finance's levers are that party's, whoever leads:
    /// <list type="bullet">
    /// <item><description><b>the household rates</b> - the income tax and VAT, the pair the AI finance ministry's rule raises (§388) - move once a year
    /// toward the rate the partner's position asks, read on the axis the stance model scores a rate by (`redistribution`, ParliamentSystem's
    /// "taxation → redistribution", lrecon where unpublished): the rate the line was seeded at, less <see cref="PointsPerChesPoint"/> for every CHES
    /// point the partner stands right of the head (a partner more redistributive than the head raises them, one less redistributive cuts them),
    /// at most <see cref="StepPointsPerYear"/> a year. The head's own position is the reference because the status quo is the government's: a head
    /// holding Finance itself moves nothing here.</description></item>
    /// <item><description><b>§634's Finance levers under a player head</b> (the tax programme, the fiscal, monetary and electricity-tax laws, the
    /// Finance cabinet decision) are refused to the player (<c>SimulationManager.PlayerMayIntroduce</c>); the partner resolves the cabinet decision by
    /// <see cref="Choose"/>.</description></item>
    /// </list>
    /// A player's own party holding Finance as a partner introduces its levers itself (§634's gate) and is never run from here. The treaty's rule has
    /// first claim: in an AI government the ministry (<see cref="AiFinanceMinistry"/>) writes first and a rate it wrote is not moved here that year.
    /// </summary>
    public static class FinancePartner
    {
        /// <summary>[AUTHORED-DRAFT] (play-calibration list, 26th): the points of a household rate the partner asks per CHES point of `redistribution`
        /// between it and the head - the SPD under a CDU chancellor (CHES 2024 2.89 against 6.39, 3.50 points apart) asks 0.875 points more on each.</summary>
        public const float PointsPerChesPoint = 0.25f;
        /// <summary>[AUTHORED-DRAFT] (play-calibration list, 26th): the most a household rate moves in a year - a quarter of a point.</summary>
        public const float StepPointsPerYear = 0.25f;
        /// <summary>CONVENTION - a move below this many points is not written (the ministry's noise floor).</summary>
        private const float MinPoints = 0.01f;

        /// <summary>CONVENTION - a year, the boundary's own interval (SimulationManager.DaysPerTurn). §716 (found by the check's own day tick): the partner steps AT MOST ONCE IN ANY YEAR. The boundary is a year apart, but the
        /// government's own budget bill can be tabled more than once in one (the incoming government's arrival window, then the fiscal-year date),
        /// and each carried a step - three bills adopted moved the rates three quarter-points in a year.</summary>
        public const int StepIntervalDays = 365;

        /// <summary>The partner's step is due on <paramref name="asOf"/>: it has never stepped, or a year has passed since it last did.</summary>
        public static bool StepDue(Country country, System.DateTime asOf) =>
            country.FinancePartnerSteppedOn == System.DateTime.MinValue || (asOf - country.FinancePartnerSteppedOn).TotalDays >= StepIntervalDays;

        /// <summary>Records a step that was written for real (the boundary's decision, the government's bill) - never a preview's.</summary>
        public static void Record(Country country, Written written, System.DateTime on)
        {
            if (written != null && written.Taxes.Count > 0) { country.FinancePartnerSteppedOn = on; }
        }

        /// <summary>The household rates - the two the partner moves.</summary>
        public static bool IsHouseholdRate(TaxType type) => type == TaxType.IncomeTax || type == TaxType.VAT;

        /// <summary>What the partner wrote into a decision this turn, so the caller can withdraw it after the turn (the ministry's discipline, §388).</summary>
        public sealed class Written
        {
            public readonly List<TaxType> Taxes = new List<TaxType>();
            public readonly List<string> Moves = new List<string>();
        }

        /// <summary>The party holding Finance where it is not the head's own nor in the head's parliamentary group; null otherwise - and null on a
        /// PROVISIONAL government (the review's decision A, taken): a stand-in the formation seats where the record names no cabinet (France's start)
        /// is the model's guess at a government, and a lever "run from its own positions" by a party in no government of record is not the record's.</summary>
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

        /// <summary>The partner whose positions run Finance: the holder where it is not the player's party (a player partner introduces its levers itself).</summary>
        public static string AiHolder(Country country)
        {
            string holder = Holder(country);
            return holder != null && holder != country.PlayerPartyAbbrev ? holder : null;
        }

        private static bool SameGroup(CountryId id, string a, string b)
        {
            foreach ((string x, string y) in ChamberRules.JointGroups(id)) { if ((x == a && y == b) || (x == b && y == a)) { return true; } }
            return false;
        }

        /// <summary>A party's position on the rates' axis (`redistribution`, lrecon where unpublished); false where it has none.</summary>
        public static bool TryPosition(CountryId id, string abbrev, out float position)
        {
            position = float.NaN;
            if (string.IsNullOrEmpty(abbrev)) { return false; }
            foreach (PoliticalParty party in PartySystems.For(id))
            {
                if (party.Abbrev != abbrev) { continue; }
                position = StanceModel.Position(party, StanceAxis.Redistribution, false);
                return !float.IsNaN(position);
            }
            return false;
        }

        /// <summary>The rate the partner asks of a household line: the seeded rate less <see cref="PointsPerChesPoint"/> per point the partner stands
        /// right of the head, within the line's range. False where either party has no position. The seed is <see cref="TaxSchedule.RateSeedOf"/>'s:
        /// the income tax's is captured at the seed (F4-2); a line with none captured (VAT) takes its rate on the first read and keeps it, saved with
        /// the line - so the anchor never follows the rate (a target re-read off the moving rate would step without end).</summary>
        public static bool Target(Country country, TaxLine line, string partner, out float target)
        {
            target = line.Rate;
            if (!TryPosition(country.Id, partner, out float partnerAt) || !TryPosition(country.Id, country.Government.PmParty, out float headAt)) { return false; }
            float anchor = TaxSchedule.RateSeedOf(line);
            target = Mathf.Clamp(anchor - PointsPerChesPoint * (partnerAt - headAt), line.MinRate, line.MaxRate);
            return true;
        }

        /// <summary>Writes the partner's yearly step on each household rate into <paramref name="decision"/> where it carries none of its own for that
        /// tax, and returns what it wrote so the caller can Withdraw it after the turn. It changes nothing in the country but a line's first-read seed.
        /// <paramref name="claimed"/>: rates another writer claims this year though they are not in this decision - the preview's (it runs no
        /// ministry) way to skip what the ministry writes at the boundary.</summary>
        public static Written Apply(Country country, PolicyDecision decision, System.DateTime asOf, ICollection<TaxType> claimed = null)
        {
            var written = new Written();
            string partner = decision != null ? AiHolder(country) : null;
            if (partner == null || !StepDue(country, asOf)) { return written; }
            foreach (TaxLine line in country.TaxLines)
            {
                if (!line.IsImplemented || !IsHouseholdRate(line.Type) || decision.TaxRateOverrides.ContainsKey(line.Type) || (claimed != null && claimed.Contains(line.Type))) { continue; }
                if (!Target(country, line, partner, out float target)) { return written; }
                float step = Mathf.Clamp(target - line.Rate, -StepPointsPerYear, StepPointsPerYear);
                if (Mathf.Abs(step) < MinPoints) { continue; }
                decision.TaxRateOverrides[line.Type] = line.Rate + step;
                written.Taxes.Add(line.Type);
                written.Moves.Add($"{line.Type} {line.Rate.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} -> {(line.Rate + step).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} (asks {target.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)})");
            }
            return written;
        }

        /// <summary>Removes what Apply wrote, so a decision object a caller hands in again next turn carries nothing of this turn's partner.</summary>
        public static void Withdraw(PolicyDecision decision, Written written)
        {
            if (decision == null || written == null) { return; }
            foreach (TaxType type in written.Taxes) { decision.TaxRateOverrides.Remove(type); }
        }

        /// <summary>[AUTHORED-DRAFT] (play-calibration list, 26th): the Finance cabinet decision, resolved by the partner on `spendvtax` ("0 = improving
        /// public services ... 10 = reducing taxes", lrecon where unpublished) against the axis's middle: a party for services takes the option on the
        /// more-state side (<see cref="CabinetDecisionOption.StateLean"/> +1 - raising revenue or spending on the service), a party for lower taxes the
        /// less-state side (−1 - forgoing it or saving); at the middle, or where no option takes that side (the windfall's bank-or-announce), the first.
        /// (The review's defect 3: one signed budget figure mixes revenue and spending - the maintenance funded is a cost a services party pays.)</summary>
        public static CabinetDecisionOption Choose(Country country, string partner, CabinetDecision decision)
        {
            CabinetDecisionOption chosen = decision.Options[0];
            float at = float.NaN;
            foreach (PoliticalParty party in PartySystems.For(country.Id)) { if (party.Abbrev == partner) { at = StanceModel.Position(party, StanceAxis.SpendVsTax, false); break; } }
            if (float.IsNaN(at) || Mathf.Approximately(at, 5f)) { return chosen; }
            int side = at < 5f ? 1 : -1;
            foreach (CabinetDecisionOption option in decision.Options) { if (option.StateLean == side) { return option; } }
            return chosen;
        }
    }
}
