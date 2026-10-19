using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// A-LOOP q13 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §4, Elias's ruling J1: Q4 through sigma_v, Q5 through the deficits): <b>deficits and debt
    /// reach the vote</b> (Brender &amp; Drazen). The fiscal term is the record's third component (`EconomicVote.RecordPartsOverTerm`), beside the economic
    /// vote and q15's approval term, on the one path the record already takes into every live count.
    /// <list type="bullet">
    /// <item><b>The two variables</b>, as [BD06] Appendix I defines them on Balance (the surplus as a share of GDP; BAL0 the election year, BAL-i
    /// i years before): "BALCH_term = ½ * (BAL-1 + BAL-2) - ½ * ( BAL-3 + BAL-4)" and "BALCH_ey = BAL0 - BAL-1". In the game BAL-i is the i-th
    /// latest CLOSED fiscal year as the player reads it (AF-17: `FiscalTurnReport.BudgetBalance`, the flow and the year's one-offs) over nominal GDP
    /// at its close, and BAL0 is the open year - the one the count falls in (<see cref="OpenRatio"/>).</item>
    /// <item><b>The index:</b> delta-z = beta_term × BALCH_term + beta_ey × BALCH_ey, the ratios as fractions, each variable first held inside
    /// <see cref="SupportBound"/>. Growth is not added: [BD06] Table 2 col. 2 finds it insignificant in the developed countries (-3.077, p 0.615),
    /// and the economic vote stays the economy's channel. Debt reaches the vote through the deficits that build it (Q5).</item>
    /// <item><b>The share:</b> shift_gov = sigma_v × delta-z (Q4: re-election as the incumbent's swing clearing a threshold under a normal spread
    /// sigma_v, so one unit of the probit's index is sigma_v points of share), split among the governing parties by the responsibility vector
    /// (`EconomicVote.SplitGovernmentShift`), as the approval term is.</item>
    /// </list>
    /// <para>⚠ <b>Declared readings</b> (the plan leaves them open):
    /// (1) a fiscal year is the game's fiscal period - the year between two turn boundaries, which the player reads closed - not the calendar year;
    /// (2) BAL0, the election year "to date, annualised": the open year's accrued flow scaled to a full year (× the year's days ÷ the days accrued) plus
    /// the year's one-offs to date unscaled (a one-off is a level, not a rate; the debt ledger's dated events of the open period, as AF-17 counts them
    /// at the close), over nominal GDP as it stands; with no day accrued there is no BAL0 and no BALCH_ey;
    /// (3) fewer than four closed years: [BD06]'s own fallback, "Where there are no data on BAL-3 and BAL-4 then: BALCH_term = BAL-1 - BAL-2", read
    /// as applying where either is missing; fewer than two, no BALCH_term; none, no BALCH_ey - so the first count of a run (the start's own run-up,
    /// before any year has closed) carries no fiscal term;
    /// (4) the window is the four years before the count whoever governed them - [BD06]'s fixed window, as the paper measures it, not the government's
    /// own term; a government formed in play answers for its predecessor's years inside the window;
    /// (5) the book is read as it stands when the record is judged, as the approval term reads approval (q15's declared reading 3);
    /// (6) the USA: [BD06]'s leader there is the president, inside Table 2's developed column; sigma_v, fitted on the parliamentary frame ParlGov
    /// holds (no US row), is carried to the president's party;
    /// (7) B&amp;D measure the LEADER's re-election; the game applies the shift to the governing parties' shares.</para>
    /// </summary>
    public static class FiscalVote
    {
        /// <summary><b>beta_term, the term variable's probit coefficient</b> - SOURCED: [BD06] Brender &amp; Drazen, "How Do Budget Deficits and Economic Growth
        /// Affect Reelection Prospects? Evidence from a Large Cross-Section of Countries", Foerder Institute WP 13-2006
        /// (`docs/reference/raw/aloop/tau_foerder_13-2006_brender_drazen.pdf`, its digest in that folder's `SHA256SUMS.txt`), "Table 2: The Effects of Budget
        /// Balances and Growth on the Probability of Reelection in Developed and Less Developed Economies", Narrow Sample, column (2) Developed:
        /// "BALCH_term ... 11.339** [0.026]", Observations 164 (probit coefficients, p-values in brackets).</summary>
        public const double BetaTerm = 11.339;

        /// <summary><b>beta_ey, the election-year variable's probit coefficient</b> - SOURCED: [BD06] Table 2, column (2) Developed: "BALCH_ey ... 22.073***
        /// [0.001]" (the same column and table as <see cref="BetaTerm"/>).</summary>
        public const double BetaElectionYear = 22.073;

        /// <summary>SOURCED, printed beside and not read: [BD06] Table 2, Expanded Sample, column (5) Developed - "7.172 [0.108]" (BALCH_term) and
        /// "19.390*** [0.003]" (BALCH_ey), Observations 180 (the substitute candidates included).</summary>
        public const double ExpandedBetaTerm = 7.172, ExpandedBetaElectionYear = 19.390;

        /// <summary><b>The support bound</b>, as a fraction of GDP: each variable is held within ± this before the index is formed, so delta-z stays inside
        /// what the estimating sample spans - a probit index extrapolated linearly to a thirty-point deficit is not a measurement. [AUTHORED-DRAFT]: the
        /// sample's range of the two variables is in Brender &amp; Drazen's replication data (openICPSR 113276, behind a login: BILLED in the plan's §8);
        /// the stored working paper prints each country's AVERAGE balance and average BALCH_ey (Table A3), not the observations' range. On the play-calibration
        /// list: "q13's support bound: BALCH_term and BALCH_ey held within ±10 % of GDP until Brender &amp; Drazen's replication data gives the sample's range."</summary>
        public const double SupportBound = 0.10;

        /// <summary><b>sigma_v as a share fraction</b> - FITTED (`Generated.IncumbentVoteSwing`, written by `Tools/aloop_sigma_fit.pl` from the ParlGov election
        /// and cabinet tables; `FiscalVoteFitCheck` refits it): the standard deviation of the prime minister's party's vote-share change between consecutive
        /// elections over [BD06]'s developed countries in ParlGov, 1960-2003.</summary>
        public static double SwingSpread => Generated.IncumbentVoteSwing.SigmaPoints / 100.0;

        /// <summary>The daily step, after the day's fiscal accrual (`SimulationManager.AdvanceDay`): the open year's flow to date as the fiscal period holds
        /// it, and one more day. Nothing else is written.</summary>
        public static void NoteDay(Country country, float flowToDate)
        {
            if (country == null) { return; }
            FiscalVoteBook book = country.FiscalVoteRecord ?? (country.FiscalVoteRecord = new FiscalVoteBook());
            book.OpenFlow = flowToDate;
            book.OpenDays++;
        }

        /// <summary>The boundary, as the fiscal report closes the year (beside the annual series the sheet draws): the year as the player reads it and
        /// nominal GDP at the close join the closed years, the oldest beyond `FiscalVoteBook.Kept` dropped, and the open year is emptied.</summary>
        public static void CloseYear(Country country, DateTime closedOn, float balance, float nominalGdp)
        {
            if (country == null) { return; }
            FiscalVoteBook book = country.FiscalVoteRecord ?? (country.FiscalVoteRecord = new FiscalVoteBook());
            book.Closed.Add(new FiscalVoteYear { ClosedOn = closedOn, Balance = balance, NominalGdp = nominalGdp });
            while (book.Closed.Count > FiscalVoteBook.Kept) { book.Closed.RemoveAt(0); }
            book.OpenFlow = 0f;
            book.OpenDays = 0;
        }

        /// <summary>A closed year's balance over GDP, a fraction; null where its GDP is not positive.</summary>
        public static double? Ratio(FiscalVoteYear year) => year != null && year.NominalGdp > 0f ? (double)year.Balance / year.NominalGdp : (double?)null;

        /// <summary>Declared reading 2: BAL0 - the open year to date, its flow annualised and its one-offs to date added, over nominal GDP as it stands;
        /// null with no day accrued or no GDP.</summary>
        public static double? OpenRatio(Country country)
        {
            FiscalVoteBook book = country?.FiscalVoteRecord;
            if (book == null || book.OpenDays < 1 || country.State == null || !(country.State.NominalGdp > 0f)) { return null; }
            double oneOffs = 0.0;
            if (country.FiscalLedgerAccruing?.Events != null) { foreach (DebtEventRecord e in country.FiscalLedgerAccruing.Events) { oneOffs -= e.AppliedDelta; } }
            double annual = (double)book.OpenFlow * Simulation.SimulationManager.DaysPerTurn / book.OpenDays + oneOffs;
            return annual / country.State.NominalGdp;
        }

        /// <summary>A variable held inside <see cref="SupportBound"/>.</summary>
        public static double Held(double balch) => double.IsNaN(balch) || double.IsInfinity(balch) ? 0.0 : Math.Max(-SupportBound, Math.Min(SupportBound, balch));   // the s880 review's L3: a non-finite balance carries no term (Math.Min/Max pass NaN through)

        /// <summary>One judgement of the fiscal term, for the logs and the diagnostics.</summary>
        public struct Reading
        {
            /// <summary>The closed years read (0 to 4), BAL-1..BAL-4 (NaN where absent) and BAL0 (NaN where absent), fractions of GDP.</summary>
            public int ClosedYears;
            public double Bal1, Bal2, Bal3, Bal4, Bal0;
            /// <summary>Whether each variable is formed, whether the term variable took [BD06]'s two-year fallback, the variables as formed and as held.</summary>
            public bool HasTerm, HasElectionYear, TermFallback;
            public double BalchTerm, BalchEy, HeldTerm, HeldEy;
            /// <summary>delta-z, sigma_v (a share fraction) and shift_gov (a share fraction).</summary>
            public double Index, Spread, GovernmentShift;
            /// <summary>Whether any governing party carries the term (the responsibility vector is not empty).</summary>
            public bool Carried;
        }

        /// <summary>The fiscal term's two variables and its index as the book stands, with no government read.</summary>
        public static Reading Judge(Country country)
        {
            var r = new Reading { Bal1 = double.NaN, Bal2 = double.NaN, Bal3 = double.NaN, Bal4 = double.NaN, Bal0 = double.NaN, Spread = SwingSpread };
            List<FiscalVoteYear> closed = country?.FiscalVoteRecord?.Closed;
            var ratios = new List<double>();
            if (closed != null)
            {
                for (int i = closed.Count - 1; i >= 0 && ratios.Count < FiscalVoteBook.Kept; i--)
                {
                    double? ratio = Ratio(closed[i]);
                    if (!ratio.HasValue) { break; }   // a year with no GDP ends the run of years read
                    ratios.Add(ratio.Value);
                }
            }
            r.ClosedYears = ratios.Count;
            if (ratios.Count > 0) { r.Bal1 = ratios[0]; }
            if (ratios.Count > 1) { r.Bal2 = ratios[1]; }
            if (ratios.Count > 2) { r.Bal3 = ratios[2]; }
            if (ratios.Count > 3) { r.Bal4 = ratios[3]; }
            if (ratios.Count >= 4) { r.HasTerm = true; r.BalchTerm = 0.5 * (r.Bal1 + r.Bal2) - 0.5 * (r.Bal3 + r.Bal4); }
            else if (ratios.Count >= 2) { r.HasTerm = true; r.TermFallback = true; r.BalchTerm = r.Bal1 - r.Bal2; }
            double? open = OpenRatio(country);
            if (open.HasValue) { r.Bal0 = open.Value; }
            if (open.HasValue && ratios.Count >= 1) { r.HasElectionYear = true; r.BalchEy = open.Value - r.Bal1; }
            r.HeldTerm = r.HasTerm ? Held(r.BalchTerm) : 0.0;
            r.HeldEy = r.HasElectionYear ? Held(r.BalchEy) : 0.0;
            r.Index = BetaTerm * r.HeldTerm + BetaElectionYear * r.HeldEy;
            r.GovernmentShift = r.Spread * r.Index;
            return r;
        }

        /// <summary>The fiscal term of the record, per governing party, as of <paramref name="asOf"/> (the day the responsibility vector reads the Vice
        /// President on, as the economic vote's does). Empty where no government is stored or neither variable can be formed.</summary>
        public static Dictionary<string, double> ShiftOverTerm(Country country, DateTime asOf, out Reading reading)
        {
            reading = Judge(country);
            if (country?.Government == null || (!reading.HasTerm && !reading.HasElectionYear)) { return new Dictionary<string, double>(); }
            Dictionary<string, double> split = EconomicVote.SplitGovernmentShift(country, reading.GovernmentShift, asOf);
            reading.Carried = split.Count > 0;
            return split;
        }

        /// <summary>A judgement in one line, for the logs.</summary>
        public static string Describe(Reading r)
        {
            if (!r.HasTerm && !r.HasElectionYear) { return string.Format(CultureInfo.InvariantCulture, "{0} closed year(s) - no fiscal term", r.ClosedYears); }
            string term = r.HasTerm
                ? string.Format(CultureInfo.InvariantCulture, "term {0:+0.000;-0.000} pp of GDP{1}{2}", 100.0 * r.BalchTerm, r.TermFallback ? " (two years)" : "", r.HeldTerm != r.BalchTerm ? string.Format(CultureInfo.InvariantCulture, ", held at {0:+0.0;-0.0}", 100.0 * r.HeldTerm) : "")
                : "no term variable";
            string year = r.HasElectionYear
                ? string.Format(CultureInfo.InvariantCulture, "election year {0:+0.000;-0.000} pp of GDP{1}", 100.0 * r.BalchEy, r.HeldEy != r.BalchEy ? string.Format(CultureInfo.InvariantCulture, ", held at {0:+0.0;-0.0}", 100.0 * r.HeldEy) : "")
                : "no election-year variable";
            string delivered = r.Carried
                ? string.Format(CultureInfo.InvariantCulture, "the government side {0:+0.00;-0.00} pp", 100.0 * r.GovernmentShift)
                : "no governing party carries it, so nothing is applied";
            return string.Format(CultureInfo.InvariantCulture, "balance change over the {0}, {1}: index {2:+0.0000;-0.0000} x spread {3:0.00} pp -> {4}",
                term, year, r.Index, 100.0 * r.Spread, delivered);
        }
    }
}
