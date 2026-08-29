using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-B11 / SPEC §26 — Get-Out-The-Vote: the ground operations that turn support into votes
    /// cast. PURE FUNCTIONS AND SMALL STATE, WIRED TO NOTHING (R-N2).
    ///
    /// §26's turnout formula is `TurnoutModel`'s (base × engagement × mobilization × enthusiasm
    /// × salience). What this file adds is the MOBILIZATION input: where it comes from, what it
    /// costs, and where it applies.
    ///
    /// **Mobilization is per REGION and per PARTY, and it is contacts.** A party's ground
    /// operation in a valkrets makes contacts — doors knocked, calls made, lifts to the polling
    /// station, reminders on the day — and the region's mobilization attribute for THAT party's
    /// supporters rises with contacts per eligible voter on a saturating curve:
    /// <code>
    /// mobilization = 50 + 50 × (1 − exp(−(weighted contacts / eligible) / MobilizationScale))
    /// </code>
    /// 50 is `TurnoutModel`'s neutral (multiplier 1.0), so **a region nobody works stays at
    /// exactly base turnout** — the done-when's "targeted regions only" holds by construction,
    /// not by a small number — and the curve is §35's shape, so the first thousand doors are
    /// worth more than the hundred-thousandth and no budget can push a region past 100.
    ///
    /// **Why per party.** `TurnoutModel` deliberately carries no party term: turnout is a
    /// property of a group in a context. GOTV is the one thing that IS party-specific about
    /// turnout — "the campaign must actually get SUPPORTERS to vote" — and it enters through the
    /// mobilization input, not through a new term: a party's supporters in a worked region turn
    /// out at `TurnoutModel.Turnout(base, engagement, mobilization[region][party], enthusiasm,
    /// salience)`; the others at the same call with mobilization 50. The region's turnout is the
    /// preference-weighted mean, and §31 can later say "you won on turnout" and mean it.
    ///
    /// **Contacts cost money AND volunteer-hours (W-B2), and volunteers bind.** An operation
    /// cannot make more contacts than its volunteer-hours allow, whatever it pays — the reason
    /// §10's offices (W-B4) and §9's volunteers matter.
    ///
    /// **[AUTHORED-DRAFT]** the four operations' cost, hours and weight per contact, and
    /// `MobilizationScale`. Base turnout is SOURCED (Sweden 2022: 84.21 %, Valmyndigheten) and
    /// applied uniformly across regions because per-valkrets eligible counts are not on disk —
    /// billed, and stated in the harness.
    /// </summary>
    public enum GotvOperation
    {
        PhoneBanking = 0,
        DoorKnocking = 1,
        Transport = 2,
        ElectionDayReminders = 3,
    }

    /// <summary>What one contact of an operation costs and is worth. [AUTHORED-DRAFT].</summary>
    public readonly struct GotvSpec
    {
        public readonly GotvOperation Operation;
        public readonly double MoneyPerContact;
        public readonly double VolunteerHoursPerContact;
        /// <summary>How much mobilization one contact is worth relative to a door knocked (1.0).</summary>
        public readonly double Weight;

        public GotvSpec(GotvOperation operation, double moneyPerContact, double volunteerHoursPerContact, double weight)
        {
            Operation = operation; MoneyPerContact = moneyPerContact; VolunteerHoursPerContact = volunteerHoursPerContact; Weight = weight;
        }
    }

    public static class GotvModel
    {
        /// <summary>[AUTHORED-DRAFT] weighted contacts per eligible voter at which ~63 % of the achievable mobilization is bought.</summary>
        public const double MobilizationScale = 0.5;

        public static readonly GotvOperation[] TheFour =
        {
            GotvOperation.PhoneBanking, GotvOperation.DoorKnocking, GotvOperation.Transport, GotvOperation.ElectionDayReminders,
        };

        /// <summary>[AUTHORED-DRAFT] per contact: a call is cheap and quick, a door slow, a lift dear and decisive, a reminder almost free.</summary>
        public static GotvSpec Spec(GotvOperation operation)
        {
            switch (operation)
            {
                //                                                         kr    hours  weight
                case GotvOperation.PhoneBanking: return new GotvSpec(operation, 3.0, 0.10, 0.5);
                case GotvOperation.DoorKnocking: return new GotvSpec(operation, 5.0, 0.25, 1.0);
                case GotvOperation.Transport: return new GotvSpec(operation, 60.0, 0.50, 3.0);
                case GotvOperation.ElectionDayReminders: return new GotvSpec(operation, 1.0, 0.02, 0.25);
                default: throw new ArgumentException($"{operation} is not one of §26's operations");
            }
        }

        /// <summary>
        /// The contacts an operation can make with what it has: bounded by money AND by
        /// volunteer-hours, whichever runs out first. Returns the contacts, and what they cost.
        /// </summary>
        public static double Contacts(GotvSpec spec, double money, double volunteerHours, out double moneySpent, out double hoursSpent)
        {
            double byMoney = spec.MoneyPerContact > 0 ? money / spec.MoneyPerContact : double.PositiveInfinity;
            double byHours = spec.VolunteerHoursPerContact > 0 ? volunteerHours / spec.VolunteerHoursPerContact : double.PositiveInfinity;
            double contacts = Math.Max(0.0, Math.Floor(Math.Min(byMoney, byHours)));
            moneySpent = contacts * spec.MoneyPerContact;
            hoursSpent = contacts * spec.VolunteerHoursPerContact;
            return contacts;
        }

        /// <summary>§26's mobilization attribute (0–100; 50 = untouched) from weighted contacts per eligible voter, on §35's curve.</summary>
        public static double Mobilization(double weightedContacts, double eligible)
        {
            if (eligible <= 0.0 || weightedContacts <= 0.0) { return 50.0; }
            double effort = weightedContacts / eligible;
            return 50.0 + 50.0 * (1.0 - Math.Exp(-effort / MobilizationScale));
        }
    }

    /// <summary>
    /// Every party's ground effort in every region: weighted contacts accumulated, and the
    /// mobilization and turnout they produce. The state a campaign builds up to election day.
    /// </summary>
    public sealed class RegionalMobilization
    {
        private readonly double[][] _weightedContacts;   // [region][party]
        private readonly double[] _eligible;

        public RegionalMobilization(double[] eligiblePerRegion, int partyCount)
        {
            if (eligiblePerRegion == null || eligiblePerRegion.Length == 0) { throw new ArgumentException("no regions"); }
            _eligible = eligiblePerRegion;
            _weightedContacts = new double[eligiblePerRegion.Length][];
            for (int r = 0; r < eligiblePerRegion.Length; r++) { _weightedContacts[r] = new double[partyCount]; }
        }

        public int RegionCount => _eligible.Length;
        public int PartyCount => _weightedContacts[0].Length;
        public double Eligible(int region) => _eligible[region];
        public double WeightedContacts(int region, int party) => _weightedContacts[region][party];

        /// <summary>Run an operation for a party in a region with a budget and volunteer-hours; returns contacts made.</summary>
        public double Operate(int region, int party, GotvOperation operation, double money, double volunteerHours,
            out double moneySpent, out double hoursSpent)
        {
            GotvSpec spec = GotvModel.Spec(operation);
            double contacts = GotvModel.Contacts(spec, money, volunteerHours, out moneySpent, out hoursSpent);
            _weightedContacts[region][party] += contacts * spec.Weight;
            return contacts;
        }

        public double Mobilization(int region, int party) => GotvModel.Mobilization(_weightedContacts[region][party], _eligible[region]);

        /// <summary>A party's supporters' turnout in a region: §26 through `TurnoutModel`, with this party's mobilization there.</summary>
        public double PartyTurnout(int region, int party, double baseTurnout, double engagement, double enthusiasm, double salience)
        {
            return TurnoutModel.Turnout(baseTurnout, engagement, Mobilization(region, party), enthusiasm, salience);
        }

        /// <summary>
        /// A region's turnout: the preference-weighted mean of each party's supporters' turnout —
        /// so a party that mobilises its 30 % lifts the region by 30 % of its lift, and a region
        /// nobody works is at exactly base × the shared multipliers.
        /// </summary>
        public double RegionTurnout(int region, double[] preference, double baseTurnout, double engagement, double enthusiasm, double salience)
        {
            double turnout = 0.0;
            double weight = 0.0;
            for (int p = 0; p < preference.Length; p++)
            {
                turnout += preference[p] * PartyTurnout(region, p, baseTurnout, engagement, enthusiasm, salience);
                weight += preference[p];
            }

            return weight > 0 ? turnout / weight : 0.0;
        }

        /// <summary>National turnout: eligible-weighted over regions (never a mean of regional rates).</summary>
        public double NationalTurnout(double[] preference, double baseTurnout, double engagement, double enthusiasm, double salience)
        {
            double cast = 0.0, eligible = 0.0;
            for (int r = 0; r < _eligible.Length; r++)
            {
                cast += _eligible[r] * RegionTurnout(r, preference, baseTurnout, engagement, enthusiasm, salience);
                eligible += _eligible[r];
            }

            return eligible > 0 ? cast / eligible : 0.0;
        }

        /// <summary>
        /// A region's votes per party with GOTV: eligible × preference × that party's supporters'
        /// turnout. The turnout advantage is the only thing GOTV moves — the preference is §8's,
        /// untouched.
        /// </summary>
        public double[] RegionVotes(int region, double[] preference, double baseTurnout, double engagement, double enthusiasm, double salience)
        {
            var votes = new double[preference.Length];
            for (int p = 0; p < preference.Length; p++)
            {
                votes[p] = _eligible[region] * preference[p] * PartyTurnout(region, p, baseTurnout, engagement, enthusiasm, salience);
            }

            return votes;
        }
    }
}
