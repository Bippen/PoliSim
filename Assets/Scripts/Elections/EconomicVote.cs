using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-3k (2026-09-25, §638, ruled): ELECTIONS READ THE GOVERNMENT'S RECORD. Magnitudes from the economic-voting literature, fetched and verified
    /// (`docs/reference/ECONOMIC_VOTE.md`; Duch &amp; Stevenson, *The Economic Vote*, 2008, read from the authors' own draft of the book [BK-D], every
    /// figure marked [draft] there):
    /// - the UNIT is "the change in the Chief Executive party's vote probabilities associated with a unit deterioration in economic perceptions"
    ///   [JOP10], a unit being one category worse on the three-point retrospective scale [BK-D] p. 48 fn 41 - applied here as that many points of
    ///   vote share, delivered exactly (below);
    /// - the PRIME MINISTER'S PARTY by cabinet type [BK-D] Table 9.4 (the first model, "Negative of the Economic Vote", so positive is a loss):
    ///   single-party majority .067, single-party minority .053, coalition majority .044, coalition minority .030;
    /// - a CABINET PARTNER by its share of the cabinet's portfolios [BK-D] Figure 9.7 (axis "Percent of Cabinet Portfolios Held by Party"):
    ///   "Economic Vote for Cabinet Partners = 0.007 - 0.057 X % of Cabinet Seats Held", a loss of .057 x share - .007. The share is Gamson's law's
    ///   estimate of it - the partner's seats over the cabinet parties' seats (`docs/reference/GAMSON_PORTFOLIOS.md`, the rule §634 allocates by) -
    ///   not the game's six abstract portfolio slots, whose sixths are the game's quantisation, not the cabinet's. FLOORED AT ZERO, stated: below a
    ///   12.3 % share the line's intercept (t = 1.25, not significant) would have a small partner GAIN from a worse economy, which no page claims;
    /// - the US PRESIDENT'S PARTY, "coded as holding all administrative responsibility" [BK-D] p. 266 fn 233, by Table 9.1's presidential row:
    ///   unified government .10, divided .06 - unified read as the president's party holding the House of record;
    /// - the OPPOSITION none: the book pools every non-cabinet party (Figure 9.5), so no party of it is given one - stated.
    /// [AUTHORED-DRAFT], the SUPPORT PARTY (GAP 4: no page gives a party outside the cabinet an economic vote - the book counts it with the
    /// opposition; its opposition-influence weight w enters only a concentration measure that predicts the prime minister's vote, and "this impact is
    /// quite small compared to the impact of ... the role that parties play in cabinet" [BK-D] p. 271): its responsibility by the book's own share,
    /// w·s_i (the p. 269 formula with no cabinet seat; w = (opposition strength + majority status)/4, fn 237, Table 9.7), carried at the partner line's
    /// slope - CAPPED at the smallest cabinet partner's loss, the spec's §5.5 "less than sitting in the cabinet, but not nothing" (a single-party
    /// minority's supporter is capped at the prime minister's party's). The composition and the cap are the authored part, on the play-calibration list.
    /// [AUTHORED-DRAFT], the scale: the game's perceived-economy index (0-100, 50 neutral, <see cref="PerceivedPerformance"/>) read as the book's -
    /// one category worse at 0, one better at 100 - the play-calibration list's 22nd entry in place of §637's share.
    /// </summary>
    public static class EconomicVote
    {
        public const double SingleMajority = 0.067, SingleMinority = 0.053, CoalitionMajority = 0.044, CoalitionMinority = 0.030;
        public const double PartnerIntercept = 0.007, PartnerSlope = 0.057;
        public const double UsPresidentUnified = 0.10, UsPresidentDivided = 0.06;

        /// <summary>Table 9.7's institutional strength of the opposition, coded as fn 237 codes it; Poland is not in the book's sample - 0, stated.</summary>
        private static int OppositionStrength(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden: case CountryId.Germany: case CountryId.Italy: return 2;
                case CountryId.USA: return 1;
                default: return 0;
            }
        }

        /// <summary>The premise: the perceived index as units of deterioration on the book's scale - +1 at 0 (one category worse), 0 at 50, -1 at 100.</summary>
        private static double Deterioration(double perceivedIndex) => (50.0 - Math.Max(0.0, Math.Min(100.0, perceivedIndex))) / 50.0;

        /// <summary>Each party's economic vote (the size of its loss per unit of deterioration) from the STORED government - empty where none is stored.</summary>
        public static Dictionary<string, double> Magnitudes(Country country)
        {
            var result = new Dictionary<string, double>();
            GovernmentRecord g = country?.Government;
            if (g == null || g.Cabinet.Count == 0 || string.IsNullOrEmpty(g.PmParty) || country.ParliamentSeats == null) { return result; }
            int members = 0; foreach (KeyValuePair<string, int> kv in country.ParliamentSeats) { members += kv.Value; }
            if (members <= 0) { return result; }
            int Seats(string p) => country.ParliamentSeats.TryGetValue(p, out int n) ? n : 0;
            if (g.Kind == WorldClock.ExecutiveKind.Presidency)
            {
                string largest = null; int most = -1;
                foreach (KeyValuePair<string, int> kv in country.ParliamentSeats) { if (kv.Value > most) { most = kv.Value; largest = kv.Key; } }
                result[g.PmParty] = largest == g.PmParty ? UsPresidentUnified : UsPresidentDivided;
                return result;
            }
            int cabinetSeats = 0; foreach (string p in g.Cabinet) { cabinetSeats += Seats(p); }
            bool majority = cabinetSeats >= members / 2 + 1;
            bool single = g.Cabinet.Count == 1;
            double pm = single ? (majority ? SingleMajority : SingleMinority) : (majority ? CoalitionMajority : CoalitionMinority);
            result[g.PmParty] = pm;
            double smallestPartner = double.MaxValue;
            foreach (string p in g.Cabinet)
            {
                if (p == g.PmParty || cabinetSeats <= 0) { continue; }
                double partner = Math.Max(0.0, PartnerSlope * Seats(p) / cabinetSeats - PartnerIntercept);
                result[p] = partner;
                smallestPartner = Math.Min(smallestPartner, partner);
            }
            double cap = smallestPartner == double.MaxValue ? pm : smallestPartner;
            int majorityStatus = majority ? 0 : single ? 1 : 2;
            double w = (OppositionStrength(country.Id) + majorityStatus) / 4.0;
            foreach (string p in g.Support) { result[p] = Math.Min(cap, PartnerSlope * w * Seats(p) / members); }
            return result;
        }

        /// <summary>Each party's vote-share shift at a perceived index: minus its economic vote times the units of deterioration.</summary>
        public static Dictionary<string, double> RecordShiftOf(Country country, double perceivedIndex)
        {
            double d = Deterioration(perceivedIndex);
            var shift = new Dictionary<string, double>();
            foreach (KeyValuePair<string, double> kv in Magnitudes(country)) { shift[kv.Key] = -kv.Value * d; }
            return shift;
        }

        /// <summary>
        /// Applies a shift to a share vector by key, DELIVERED EXACTLY: each named party moves by its own shift (floored at zero), and the parties the
        /// shift does not name absorb the difference in proportion to their shares - so a governing party's loss is the literature's figure, not a
        /// renormalised fraction of it (the §638 review's finding). No party moved, or nothing to absorb it, and the shares come back as given.
        /// </summary>
        public static double[] ApplyRecordShift(IReadOnlyList<string> keys, double[] shares, IReadOnlyDictionary<string, double> shift)
        {
            if (shift == null || shift.Count == 0 || shares == null) { return shares; }
            var adjusted = (double[])shares.Clone();
            double moved = 0.0, others = 0.0;
            bool any = false;
            for (int i = 0; i < shares.Length; i++)
            {
                if (shift.TryGetValue(keys[i], out double by))
                {
                    if (by != 0.0) { any = true; }
                    adjusted[i] = Math.Max(0.0, shares[i] + by);
                    moved += adjusted[i] - shares[i];
                }
                else { others += shares[i]; }
            }
            if (!any || others <= 0.0 || others - moved <= 0.0) { return shares; }
            double scale = (others - moved) / others;
            for (int i = 0; i < shares.Length; i++) { if (!shift.ContainsKey(keys[i])) { adjusted[i] = shares[i] * scale; } }
            return adjusted;
        }

        /// <summary>
        /// The same, by position - the campaign's form: one shift per party, NaN for a party the record does not name. A party the government holds
        /// carries a number (zero included); every other party carries NaN and absorbs. (A zero for the opposition would name every party and leave
        /// no one to absorb, so the shift would silently do nothing - the first §638 film's finding.)
        /// </summary>
        public static double[] ApplyRecordShiftByIndex(double[] shares, double[] shiftPerParty)
        {
            if (shares == null || shiftPerParty == null || shiftPerParty.Length != shares.Length) { return shares; }
            var keys = new string[shares.Length];
            var shift = new Dictionary<string, double>();
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (!double.IsNaN(shiftPerParty[i])) { shift[keys[i]] = shiftPerParty[i]; }
            }
            return ApplyRecordShift(keys, shares, shift);
        }
    }
}
