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
    ///   unified government .10, divided .06 - unified read as the president's party holding the House and, where a second chamber is seated, the Senate
    ///   (PS-6 US-15, DECLARED: the book's divided government "occurs when different parties control the executive and legislative branches of government"
    ///   [BK-D] p. 245, and its one US example of unified government, "the short period of unified Democratic government in the early 1990's", is the 103rd
    ///   Congress, whose House and Senate were both Democratic by the Historian's and the Senate's division tables on disk ([HH-DIV], [SEN-DIV]) - read as
    ///   control of both chambers; a Senate equally divided is the Vice President's party's, the president's own on one ticket,
    ///   <see cref="UsSenate.MajorityOf"/>); where no second chamber is seated (an AI USA, a world with no player) the House alone decides, as before;
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

        /// <summary>Each party's economic vote (the size of its loss per unit of deterioration) from the STORED government - empty where none is stored. PS-6 US-15:
        /// <paramref name="on"/> is the day the Vice President who decides an equal Senate is read on (<see cref="UsSenate.VicePresidentParty"/>); none given, the
        /// day the government in office formed - the same ticket, so the same party.</summary>
        public static Dictionary<string, double> Magnitudes(Country country, DateTime? on = null)
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
                // PS-6 US-15 (DECLARED, above): the second chamber too where one is seated - its majority the president's party's, an equal division going to the
                // Vice President's party (US-28's derivation, read on the day); none seated, the House alone, as before (an AI USA's, the trajectory dump's)
                bool senateHeld = country.SecondChamberSeats == null || country.SecondChamberSeats.Count == 0
                                  || UsSenate.MajorityOf(country.SecondChamberSeats, UsSenate.VicePresidentParty(country, on ?? g.FormedOn)) == g.PmParty;
                result[g.PmParty] = largest == g.PmParty && senateHeld ? UsPresidentUnified : UsPresidentDivided;
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

        /// <summary>
        /// §752 (Elias's ruling A1): the day the STORED government took office - the start of the term an election scores. A government formed in play
        /// took office on its own formation day. A government of record seated at a start (or the formation's stand-in where the record's head is of its
        /// party) took office when ITS HEAD did: the record's rows are walked back while the row before ends the day this one begins under the same
        /// person, so a partner's exit under one head (Scholz's minority from 2024-11-07) does not restart the term - Scholz's runs from 2021-12-08.
        /// **Never before the election that opened the term as §709 seeds it** (<see cref="Simulation.PublicationSystem.PreStartWindowOpens"/> - the
        /// latest election before the day that chose the head of government: the chamber's, the USA's presidential), so the term measured and the term
        /// seeded agree by construction (the reviews): the walk stops at it, a government of record whose row began before it is measured from it (the
        /// later of the two - Kristersson's caretaker from 2026-09-17 after the 2026-09-13 election, Scholz's minority in an epoch after 2025-02-23,
        /// Biden's lame weeks after 2024-11-05), and a stand-in seated after an election its record head took office before is that election's own
        /// government, its term from its own day. ⚠ A limit, stated (the third review's defect 2): §709 seeds from the election's own month, so an epoch
        /// between an election and the first print of that month's figures (Sweden from 2026-09-14) holds no figure for the term's first period - its
        /// reading stays at the neutral 50 until a government formed in play replaces the one of record; a ruling on §709's first month would close it.
        /// Null where no government is stored.
        /// </summary>
        public static DateTime? TookOffice(Country country)
        {
            GovernmentRecord g = country?.Government;
            if (g == null) { return null; }
            if ((g.Outcome == "of record" || g.OfRecord || g.Provisional) && WorldClock.TryGovernmentAt(country.Id, g.FormedOn, out WorldClock.GovernmentOfRecord row)
                && !string.IsNullOrEmpty(g.PmParty) && HeadParty(row.Head) == g.PmParty)
            {
                DateTime opened = Simulation.PublicationSystem.PreStartWindowOpens(country.Id, g.FormedOn);
                IReadOnlyList<WorldClock.GovernmentOfRecord> rows = WorldClock.Governments(country.Id);
                int at = -1;
                for (int i = 0; i < rows.Count; i++) { if (rows[i].From == row.From && rows[i].Head == row.Head) { at = i; break; } }
                if (at < 0) { return row.From > opened ? row.From : opened; }
                bool SameHead(int i) => rows[i - 1].Until == rows[i].From && HeadPerson(rows[i - 1].Head) == HeadPerson(rows[i].Head);
                int headFrom = at;
                while (headFrom > 0 && SameHead(headFrom)) { headFrom--; }
                // a stand-in seated after an election the record's head took office before (Sweden's caretaker row from 2026-09-17 continues the 2022
                // government) is the formation's government after that election - its term is its own
                if (g.Provisional && rows[headFrom].From < opened) { return g.FormedOn; }
                while (at > 0 && SameHead(at) && rows[at - 1].From >= opened) { at--; }
                // a government of record whose row began before the election that opened the term: measured from that election
                return rows[at].From > opened ? rows[at].From : opened;
            }
            return g.FormedOn;
        }

        /// <summary>The person a record's head names - "Olaf Scholz (SPD), minority" is Olaf Scholz.</summary>
        private static string HeadPerson(string head)
        {
            if (string.IsNullOrEmpty(head)) { return string.Empty; }
            int cut = head.IndexOf(" (", StringComparison.Ordinal);
            if (cut < 0) { cut = head.IndexOf(','); }
            return (cut < 0 ? head : head.Substring(0, cut)).Trim();
        }

        /// <summary>The party a record's head names in brackets - "Olaf Scholz (SPD), minority" is the SPD's; null where none is named.</summary>
        private static string HeadParty(string head)
        {
            if (string.IsNullOrEmpty(head)) { return null; }
            int open = head.IndexOf(" (", StringComparison.Ordinal), close = open < 0 ? -1 : head.IndexOf(')', open);
            return open < 0 || close < 0 ? null : head.Substring(open + 2, close - open - 2).Trim();
        }

        /// <summary>
        /// §752 (A1): the government's record judged over its term, as of a day - the term's reading (<see cref="PerceivedPerformance.OverTerm"/>)
        /// from the day it took office, then each party's shift at that index. Empty where no government is stored. Since A-LOOP q15 (2026-10-19) the
        /// RECORD the counts read is every component summed (<see cref="RecordPartsOverTerm"/>): the economic vote alone is <see cref="EconomyOverTerm"/>.
        /// </summary>
        public static Dictionary<string, double> RecordOverTerm(Country country, DateTime asOf, out PerceivedPerformance.TermReading reading)
        {
            RecordParts parts = RecordPartsOverTerm(country, asOf);
            reading = parts.Term;
            return parts.Total;
        }

        /// <summary>§752 (A1): the economic vote alone over the term - the record's first component.</summary>
        public static Dictionary<string, double> EconomyOverTerm(Country country, DateTime asOf, out PerceivedPerformance.TermReading reading)
        {
            DateTime? took = TookOffice(country);
            if (!took.HasValue) { reading = PerceivedPerformance.OverTerm(country, asOf, asOf); return new Dictionary<string, double>(); }
            reading = PerceivedPerformance.OverTerm(country, took.Value, asOf);
            return RecordShiftOf(country, reading.Index, asOf);
        }

        /// <summary>
        /// A-LOOP (`docs/specs/A_LOOP_PLAN.md` §0): <b>the government's record, by component</b> - one record, per party, for any day. Its components:
        /// the economic vote over the term (<see cref="Economy"/>, §638/§752) and q15's approval term (<see cref="Approval"/>, <see cref="ApprovalVote"/>);
        /// q13's fiscal term (Brender &amp; Drazen, the plan's §4) joins as a third, split by <see cref="SplitGovernmentShift"/> as the approval term is,
        /// and is summed into <see cref="Total"/> beside them. COAL-1 reads a party's staying and leaving off the components (the plan's §3 (2)).
        /// </summary>
        public sealed class RecordParts
        {
            /// <summary>The economic vote's term reading (the perceived index over the term).</summary>
            public PerceivedPerformance.TermReading Term;
            /// <summary>The approval term's reading (net approval then and now, b, shift_gov).</summary>
            public ApprovalVote.Reading ApprovalReading;
            /// <summary>Each component's shift per party, as a share fraction.</summary>
            public Dictionary<string, double> Economy = new Dictionary<string, double>(), Approval = new Dictionary<string, double>();
            /// <summary>The record the counts read: every component summed per party.</summary>
            public Dictionary<string, double> Total = new Dictionary<string, double>();
        }

        /// <summary>A-LOOP: the record over the term as of a day, by component, and the total every live count reads (<see cref="RecordOverTerm"/>).</summary>
        public static RecordParts RecordPartsOverTerm(Country country, DateTime asOf)
        {
            var parts = new RecordParts();
            parts.Economy = EconomyOverTerm(country, asOf, out parts.Term);
            parts.Approval = ApprovalVote.ShiftOverTerm(country, asOf, out parts.ApprovalReading);
            parts.Total = Sum(parts.Economy, parts.Approval);
            return parts;
        }

        /// <summary>The components summed per party key - a key in one only carries that one's shift.</summary>
        private static Dictionary<string, double> Sum(Dictionary<string, double> a, Dictionary<string, double> b)
        {
            var total = new Dictionary<string, double>(a);
            foreach (KeyValuePair<string, double> kv in b) { total[kv.Key] = total.TryGetValue(kv.Key, out double has) ? has + kv.Value : kv.Value; }
            return total;
        }

        /// <summary>A-LOOP (§0): <b>the responsibility vector</b> - each party's economic vote (<see cref="Magnitudes"/>) as its share of the
        /// government's, normalised to sum to one (DERIVED). Empty where no government is stored or every magnitude is zero.</summary>
        public static Dictionary<string, double> Responsibility(Country country, DateTime? on = null)
        {
            Dictionary<string, double> magnitudes = Magnitudes(country, on);
            double sum = 0.0;
            foreach (KeyValuePair<string, double> kv in magnitudes) { sum += kv.Value; }
            var shares = new Dictionary<string, double>();
            if (!(sum > 0.0)) { return shares; }
            foreach (KeyValuePair<string, double> kv in magnitudes) { shares[kv.Key] = kv.Value / sum; }
            return shares;
        }

        /// <summary>A-LOOP (§0): a government-wide shift (share fraction) split among the governing parties by <see cref="Responsibility"/> - the
        /// opposition gets none, so its share of a loss arrives by absorption (<see cref="ApplyRecordShift"/>). q15's approval term; q13's fiscal term.</summary>
        public static Dictionary<string, double> SplitGovernmentShift(Country country, double governmentShift, DateTime? on = null)
        {
            var split = new Dictionary<string, double>();
            foreach (KeyValuePair<string, double> kv in Responsibility(country, on)) { split[kv.Key] = governmentShift * kv.Value; }
            return split;
        }

        /// <summary>§752 (A1): the term's index alone - Campaign HQ's figure, what the election scores; the neutral 50 where no government is stored.</summary>
        public static double RecordTermIndex(Country country, DateTime asOf)
        {
            EconomyOverTerm(country, asOf, out PerceivedPerformance.TermReading reading);   // the index alone - the record's other components are not needed
            return reading.Index;
        }

        /// <summary>A term's reading in one line, for the logs: the index, the day it took office, each component then and now.</summary>
        public static string Describe(PerceivedPerformance.TermReading r)
        {
            string One(string name, PerceivedPerformance.TermValues v, string unit) => !v.Complete ? name + " not published at both ends"
                : string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} {1:0.0}{4} ({2:yyyy-MM}) -> {3:0.0}{4} ({5:yyyy-MM})", name, v.Then.Value, v.ThenPeriod, v.Now.Value, unit, v.NowPeriod);
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "term index {0:0.0} - took office {1:yyyy-MM-dd}, judged {2:yyyy-MM-dd}: {3}; {4}",
                r.Index, r.TookOffice, r.AsOf, One("unemployment", r.Unemployment, " %"), One("inflation", r.Inflation, " %"));
        }

        /// <summary>Each party's vote-share shift at a perceived index: minus its economic vote times the units of deterioration (<paramref name="on"/>, the day
        /// <see cref="Magnitudes"/> reads the Vice President on).</summary>
        public static Dictionary<string, double> RecordShiftOf(Country country, double perceivedIndex, DateTime? on = null)
        {
            double d = Deterioration(perceivedIndex);
            var shift = new Dictionary<string, double>();
            foreach (KeyValuePair<string, double> kv in Magnitudes(country, on)) { shift[kv.Key] = -kv.Value * d; }
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
