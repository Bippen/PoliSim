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
    /// at its close - or, before the start, the sourced year (K62, `SeedPreRun`) - and BAL0 is the open year, the one the count falls in (`OpenRatio`).</item>
    /// <item><b>The index:</b> delta-z = beta_term × BALCH_term + beta_ey × BALCH_ey, the ratios as fractions, each variable first held inside
    /// `SupportBound`. Growth is not added: [BD06] Table 2 col. 2 finds it insignificant in the developed countries (-3.077, p 0.615),
    /// and the economic vote stays the economy's channel. Debt reaches the vote through the deficits that build it (Q5).</item>
    /// <item><b>The share:</b> shift_gov = sigma_v × delta-z (Q4: re-election as the incumbent's swing clearing a threshold under a spread sigma_v, so
    /// one unit of the probit's index is sigma_v points of share), sigma_v the ROBUST scale of the swings (K64), held within ± `ShareCap` (K64),
    /// split among the governing parties by the responsibility vector (`EconomicVote.SplitGovernmentShift`), as the approval term is.</item>
    /// <item><b>Tenure</b> (K63, 2026-10-23: "each fiscal year counts against a government only under its own tenure"): a year - closed or open - is
    /// read only where it opened on or after the day the governing PARTY's tenure began (K101, 2026-10-24: tenure follows the governing party - the
    /// leading party, the president's party - not each formation; `TenureFrom`); the years before it are not counted, so a government of another
    /// party answers for none of its predecessor's years, and a re-elected or re-formed government of the same party answers for its own.</item>
    /// </list>
    /// <para>⚠ <b>Declared readings</b> (the plan and the rulings leave them open):
    /// (1) a fiscal year is the game's fiscal period - the year between two turn boundaries, which the player reads closed - not the calendar year;
    /// (2) BAL0, the election year "to date, annualised": the open year's accrued flow scaled to a full year (× the year's days ÷ the days accrued) plus
    /// the year's one-offs to date unscaled (a one-off is a level, not a rate; the debt ledger's dated events of the open period, as AF-17 counts them
    /// at the close), over nominal GDP as it stands; with no day accrued there is no BAL0 and no BALCH_ey;
    /// (3) fewer than four closed years read: [BD06]'s own fallback, "Where there are no data on BAL-3 and BAL-4 then: BALCH_term = BAL-1 - BAL-2",
    /// read as applying where either is missing; fewer than two, no BALCH_term; none, no BALCH_ey;
    /// (4) K63's tenure, read STRICTLY (confirmed as K99): a year counts only where the WHOLE year falls under the governing party's tenure (it opened
    /// on or after the day the tenure began); a year the party entered part-way is its predecessor's. The years read are the newest run of such
    /// years, so a year not counted ends the run, as a year with no GDP does. K101: the tenure is the governing party's (`GovernmentRecord.PmParty`
    /// - the prime minister's party, the president's party), carried on the book (`FiscalVoteBook.TenureParty`, `TenureFrom`): it begins where
    /// another party takes the lead and runs on through re-elections, oaths and re-formations under the same one. At a start it is read off the
    /// governments of record (`WorldClock.Governments`) - the government of record at the start walked back while the row before it is the same
    /// party's and ends the day this one begins - no earlier than the record's first row (the record says nothing before it); a government of the
    /// start that is not the record's party begins its tenure the day `EconomicVote.TookOffice` reads;
    /// (5) the book is read as it stands when the record is judged, as the approval term reads approval (q15's declared reading 3);
    /// (6) the USA: [BD06]'s leader there is the president, inside Table 2's developed column; sigma_v, fitted on the parliamentary frame ParlGov
    /// holds (no US row), is carried to the president's party;
    /// (7) B&amp;D measure the LEADER's re-election; the game applies the shift to the governing parties' shares;
    /// (8) K62's seeded years: the book a world enters with holds the four newest source years that had closed by the start (the calendar year for
    /// the EU five; the federal fiscal year, 1 October to 30 September, for the USA), each with its own opening and closing day and its published
    /// ratio, carried at the start's nominal GDP; a missing year ends the run. They are read after the first game year closes, beside the game's own;
    /// (8b) the FIRST count (K98: "reads the SOURCE for both years"; K100: "the first count's current year is the ELECTION's calendar year"; K105:
    /// "a source year that closed before the COUNT may be read even if it was open at the start"): until the first game year closes, every year is
    /// read off the record by the election's calendar year E - BAL0 is E (the USA: the federal fiscal year numbered E), BAL-1..BAL-4 are E-1..E-4,
    /// only years older than BAL0's, so none is read twice (the s-fisc review 2's H1: the USA's eve world read FY2024 as both), each closed by the
    /// election's day - so Germany's 2025 count reads 2024 as BAL-1; a year still open on that day is never read, E itself excepted as K98 and
    /// K100 rule it: E's full-year figure closes, and is published, after polling day - a DECLARED hindsight (the review's L5). A campaign's opening
    /// is judged for its election's day, not its own (the review's M1). Where the source does not yet publish E, there is no BAL0 and no
    /// BALCH_ey; from the first game year that closes, the game's own open year (declared reading 2) takes over;
    /// (9) the source's balance is GENERAL government for the EU five (Eurostat's B9, S13) and the unified FEDERAL budget for the USA (OMB) - the
    /// perimeter each country's book models; [BD06]'s own Balance is the central government's (the s880 review's L8).</para>
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
        /// [0.001]" (the same column and table as `BetaTerm`).</summary>
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

        /// <summary><b>The share cap</b> (K64, ruled 2026-10-23: "the cap is ±10 pp interim"), a share fraction: shift_gov, the government side's fiscal
        /// term, is held within ± this. [AUTHORED-DRAFT] - interim: at the support bound the term would reach sigma_v × (beta_term + beta_ey) × the bound
        /// (the s880 review's M3). On the play-calibration list: "q13's share cap: the fiscal term held within ±10 pp
        /// of the government side's share, interim (K64), until the support bound is measured on Brender &amp; Drazen's replication data."</summary>
        public const double ShareCap = 0.10;

        /// <summary><b>sigma_v as a share fraction</b> - FITTED (`Generated.IncumbentVoteSwing`, written by `Tools/aloop_sigma_fit.pl` from the ParlGov election
        /// and cabinet tables; `FiscalVoteFitCheck` refits it): since K64 (2026-10-23, "sigma_v takes a robust scale") the MAD-based scale - 1.4826 ×
        /// the median absolute deviation from the median - of the prime minister's party's vote-share change between consecutive elections over
        /// [BD06]'s developed countries in ParlGov, 1960-2003; the standard deviation and the IQR-based scale are printed beside it.</summary>
        public static double SwingSpread => Generated.IncumbentVoteSwing.SigmaPoints / 100.0;

        /// <summary>K62 (ruled 2026-10-23: "the pre-run fiscal years are seeded from a sourced annual balance series"): <b>the book a world enters with</b>
        /// - the open year opening on <paramref name="start"/>, the four newest source years that had closed by the start (declared reading 8, K100's
        /// no-future rule), oldest first, each marked with its source year, and the governing party's tenure as the record has it (K101, declared
        /// reading 4). Idempotent: a country already holding a book (a save's, a run's) is left as it is. Returns the years seeded.</summary>
        public static int SeedPreRun(Country country, DateTime start)
        {
            if (country == null || country.FiscalVoteRecord != null) { return 0; }
            var book = new FiscalVoteBook { OpenedOn = start };
            country.FiscalVoteRecord = book;
            if (country.Government != null && !string.IsNullOrEmpty(country.Government.PmParty))
            {
                book.TenureParty = country.Government.PmParty;
                book.TenureFrom = PartyTenureOfRecord(country, start) ?? EconomicVote.TookOffice(country) ?? start;
            }
            float gdp = country.State != null ? country.State.NominalGdp : 0f;
            if (!(gdp > 0f)) { return 0; }
            var years = new List<FiscalVoteYear>();
            (int Year, int OpensYear, int OpensMonth, int OpensDay, double Percent)[] record = Generated.FiscalBalanceRecord.For(country.Id);
            int expected = 0;
            for (int i = record.Length - 1; i >= 0 && years.Count < FiscalVoteBook.Kept; i--)
            {
                var opens = new DateTime(record[i].OpensYear, record[i].OpensMonth, record[i].OpensDay);
                DateTime closes = opens.AddYears(1);
                if (closes > start) { continue; }   // K100: a year still open at the start is never read
                if (expected != 0 && record[i].Year != expected) { break; }   // a missing year ends the run: an older one would be read in its place
                years.Add(new FiscalVoteYear { OpenedOn = opens, ClosedOn = closes, Balance = (float)(record[i].Percent / 100.0 * gdp), NominalGdp = gdp, SourceYear = record[i].Year });
                expected = record[i].Year - 1;
            }
            for (int i = years.Count - 1; i >= 0; i--) { book.Closed.Add(years[i]); }
            return years.Count;
        }

        /// <summary>K101, declared reading 4: the day the governing party's tenure began as the governments of record show it on <paramref name="on"/> - the
        /// row in force walked back while the row before it is the same party's (the party a row's head names in brackets) and ends the day it begins;
        /// null where no row is in force or its party is not the stored government's.</summary>
        public static DateTime? PartyTenureOfRecord(Country country, DateTime on)
        {
            string party = country?.Government?.PmParty;
            if (string.IsNullOrEmpty(party)) { return null; }
            IReadOnlyList<WorldClock.GovernmentOfRecord> rows = WorldClock.Governments(country.Id);
            int at = -1;
            for (int i = 0; i < rows.Count; i++) { if (rows[i].From <= on && on < rows[i].Until) { at = i; } }
            if (at < 0 || EconomicVote.HeadParty(rows[at].Head) != party) { return null; }
            while (at > 0 && rows[at - 1].Until == rows[at].From && EconomicVote.HeadParty(rows[at - 1].Head) == party) { at--; }
            return rows[at].From;
        }

        /// <summary>K101: the day the governing party's tenure began - the book's, where the stored government is still that party's; where another party
        /// leads now and the book has not yet noted it (the day of the change), the day `EconomicVote.TookOffice` reads. Null with no government stored.</summary>
        public static DateTime? TenureFrom(Country country)
        {
            GovernmentRecord g = country?.Government;
            if (g == null || string.IsNullOrEmpty(g.PmParty)) { return null; }
            FiscalVoteBook book = country.FiscalVoteRecord;
            if (book != null && book.TenureParty == g.PmParty && book.TenureFrom != default(DateTime)) { return book.TenureFrom; }
            return EconomicVote.TookOffice(country);
        }

        /// <summary>The daily step, after the day's fiscal accrual (`SimulationManager.AdvanceDay`): the open year's flow to date as the fiscal period holds
        /// it, and one more day; K101: where another party now leads, its tenure begins (the day `EconomicVote.TookOffice` reads, else
        /// <paramref name="today"/>). A country that entered without a book is seeded from the world's own day first (K62, `SeedPreRun`; the s880-fisc
        /// review's L6). Nothing else is written.</summary>
        public static void NoteDay(Country country, float flowToDate, DateTime today)
        {
            if (country == null) { return; }
            if (country.FiscalVoteRecord == null) { SeedPreRun(country, today); }
            FiscalVoteBook book = country.FiscalVoteRecord;
            string party = country.Government?.PmParty;
            bool restarts = !string.IsNullOrEmpty(party) && party != book.TenureParty;
            if (restarts)
            {
                book.TenureParty = party;
                book.TenureFrom = EconomicVote.TookOffice(country) ?? today;
            }
            if (country.Government != null && country.Government.FormedOn != book.FormationSeen)
            {
                // the s-fisc review 2's L6: each formation printed with the governing party and what it does to the tenure, so K101's clause is read, not assumed
                book.FormationSeen = country.Government.FormedOn;
                UnityEngine.Debug.Log(string.Format(CultureInfo.InvariantCulture, "FISCAL TENURE: {0} - {1}'s government formed {2:yyyy-MM-dd}: the tenure {3} {4:yyyy-MM-dd}",
                    country.Id, string.IsNullOrEmpty(party) ? "(no party)" : party, country.Government.FormedOn, restarts ? "restarts, from" : "of the same party runs on, from", book.TenureFrom));
            }
            book.OpenFlow = flowToDate;
            book.OpenDays++;
        }

        /// <summary>The boundary, as the fiscal report closes the year (beside the annual series the sheet draws): the year as the player reads it, nominal
        /// GDP at the close and the day it opened join the closed years, the oldest beyond `FiscalVoteBook.Kept` dropped, and the open year is emptied,
        /// opening on the boundary.</summary>
        public static void CloseYear(Country country, DateTime closedOn, float balance, float nominalGdp)
        {
            if (country == null) { return; }
            FiscalVoteBook book = country.FiscalVoteRecord ?? (country.FiscalVoteRecord = new FiscalVoteBook { OpenedOn = closedOn.AddDays(-Simulation.SimulationManager.DaysPerTurn) });
            book.Closed.Add(new FiscalVoteYear { ClosedOn = closedOn, OpenedOn = book.OpenedOn, Balance = balance, NominalGdp = nominalGdp });
            while (book.Closed.Count > FiscalVoteBook.Kept) { book.Closed.RemoveAt(0); }
            book.OpenFlow = 0f;
            book.OpenDays = 0;
            book.OpenedOn = closedOn;
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

        /// <summary>K98: whether the book is still on its first count's footing - no game year has closed (every closed year it holds is sourced).</summary>
        public static bool BeforeFirstClose(FiscalVoteBook book) => book != null && book.Closed.TrueForAll(y => y.SourceYear != 0);

        /// <summary>A variable held inside `SupportBound`.</summary>
        public static double Held(double balch) => double.IsNaN(balch) || double.IsInfinity(balch) ? 0.0 : Math.Max(-SupportBound, Math.Min(SupportBound, balch));   // the s880 review's L3: a non-finite balance carries no term (Math.Min/Max pass NaN through)

        /// <summary>K64: the government side's term held within ± `ShareCap`.</summary>
        public static double Capped(double shift) => Math.Max(-ShareCap, Math.Min(ShareCap, shift));

        /// <summary>One judgement of the fiscal term, for the logs and the diagnostics.</summary>
        public struct Reading
        {
            /// <summary>The closed years read (0 to 4), BAL-1..BAL-4 (NaN where absent) and BAL0 (NaN where absent), fractions of GDP; how many of the
            /// years read were seeded from the record (K62).</summary>
            public int ClosedYears, SourcedYears;
            /// <summary>K98: the source year each of BAL-1..BAL-4 was read as (0: a year the game played, or none), and BAL0's (0: the game's own open year,
            /// or none).</summary>
            public int Source1, Source2, Source3, Source4, Source0;
            /// <summary>K100: the election's calendar year the first count is read for (0: a count after the first game year closed, read off the book).</summary>
            public int ElectionYear;
            public double Bal1, Bal2, Bal3, Bal4, Bal0;
            /// <summary>K63 / K101: the day the governing party's tenure began (null: no gate, no government read); the closed years the book holds that
            /// are not read because they opened before it; whether the open year is not read for the same reason.</summary>
            public DateTime? TenureFrom;
            public int OutsideTenure;
            public bool OpenOutsideTenure;
            /// <summary>Whether each variable is formed, whether the term variable took [BD06]'s two-year fallback, the variables as formed and as held.</summary>
            public bool HasTerm, HasElectionYear, TermFallback;
            public double BalchTerm, BalchEy, HeldTerm, HeldEy;
            /// <summary>delta-z, sigma_v (a share fraction), sigma_v × delta-z before the cap and shift_gov after it (share fractions), and whether the
            /// cap bit (K64).</summary>
            public double Index, Spread, Uncapped, GovernmentShift;
            public bool CapBinds;
            /// <summary>Whether any governing party carries the term (the responsibility vector is not empty).</summary>
            public bool Carried;
        }

        /// <summary>The fiscal term's two variables and its index as the book stands, gated on the governing party's tenure (K63, K101: `TenureFrom`; no
        /// government stored, no gate), for an election on the day the open year opened.</summary>
        public static Reading Judge(Country country) => Judge(country, TenureFrom(country));

        /// <summary>The same, gated on a tenure that opened on <paramref name="tenureFrom"/> (null: every year read, as before K63), for the election held on
        /// <paramref name="electionDay"/> (null: the day the open year opened).
        /// <para>Until the first game year closes (K98) every year is the source's, read off the record by the ELECTION's calendar year E (K100):
        /// BAL0 is E, and BAL-1..BAL-4 are E-1..E-4 - only years OLDER than BAL0's, so no year is read twice (the s-fisc review 2's H1), and only
        /// years that had closed by the election's day (K105, 2026-10-24: a year open at the start but closed before the count is read; a year
        /// still open on the count's day is not - E itself excepted, as K98 and K100 rule, a declared hindsight). From the first game year that closes,
        /// the book: its played years, then the sourced years it entered with.</para></summary>
        public static Reading Judge(Country country, DateTime? tenureFrom, DateTime? electionDay = null)
        {
            var r = new Reading { Bal1 = double.NaN, Bal2 = double.NaN, Bal3 = double.NaN, Bal4 = double.NaN, Bal0 = double.NaN, Spread = SwingSpread, TenureFrom = tenureFrom };
            FiscalVoteBook book = country?.FiscalVoteRecord;
            var ratios = new List<double>();
            var sources = new List<int>();
            double? open = null;
            int openSource = 0;
            bool firstCount = book != null && BeforeFirstClose(book);
            if (firstCount)
            {
                DateTime election = electionDay ?? book.OpenedOn;
                int e = election.Year;
                r.ElectionYear = e;
                (int Year, int OpensYear, int OpensMonth, int OpensDay, double Percent)[] record = Generated.FiscalBalanceRecord.For(country.Id);
                int At(int year) => Array.FindIndex(record, row => row.Year == year);
                DateTime OpensOf(int at) => new DateTime(record[at].OpensYear, record[at].OpensMonth, record[at].OpensDay);
                for (int y = e - 1; y >= e - FiscalVoteBook.Kept; y--)
                {
                    int at = At(y);
                    if (at < 0) { break; }   // a missing year ends the run
                    DateTime opens = OpensOf(at);
                    if (opens.AddYears(1) > election) { break; }   // K105: a year still open on the election's day is not read
                    if (tenureFrom.HasValue && opens < tenureFrom.Value) { r.OutsideTenure = y - (e - FiscalVoteBook.Kept) + 1; break; }   // K63 / K101: this year and every older one opened before the tenure
                    ratios.Add(record[at].Percent / 100.0);
                    sources.Add(y);
                }
                int bal0 = At(e);
                r.OpenOutsideTenure = bal0 >= 0 && tenureFrom.HasValue && OpensOf(bal0) < tenureFrom.Value;
                if (bal0 >= 0 && !r.OpenOutsideTenure) { open = record[bal0].Percent / 100.0; openSource = e; }
                r.SourcedYears = ratios.Count;
            }
            else if (book != null)
            {
                List<FiscalVoteYear> closed = book.Closed;
                for (int i = closed.Count - 1; i >= 0 && ratios.Count < FiscalVoteBook.Kept; i--)
                {
                    if (tenureFrom.HasValue && closed[i].OpenedOn < tenureFrom.Value) { r.OutsideTenure = i + 1; break; }   // K63: this year and every older one opened before the tenure
                    double? ratio = Ratio(closed[i]);
                    if (!ratio.HasValue) { break; }   // a year with no GDP ends the run of years read
                    ratios.Add(ratio.Value);
                    sources.Add(closed[i].SourceYear);
                    if (closed[i].SourceYear != 0) { r.SourcedYears++; }
                }
                r.OpenOutsideTenure = tenureFrom.HasValue && book.OpenedOn < tenureFrom.Value;
                if (!r.OpenOutsideTenure) { open = OpenRatio(country); }
            }
            r.ClosedYears = ratios.Count;
            if (ratios.Count > 0) { r.Bal1 = ratios[0]; r.Source1 = sources[0]; }
            if (ratios.Count > 1) { r.Bal2 = ratios[1]; r.Source2 = sources[1]; }
            if (ratios.Count > 2) { r.Bal3 = ratios[2]; r.Source3 = sources[2]; }
            if (ratios.Count > 3) { r.Bal4 = ratios[3]; r.Source4 = sources[3]; }
            if (ratios.Count >= 4) { r.HasTerm = true; r.BalchTerm = 0.5 * (r.Bal1 + r.Bal2) - 0.5 * (r.Bal3 + r.Bal4); }
            else if (ratios.Count >= 2) { r.HasTerm = true; r.TermFallback = true; r.BalchTerm = r.Bal1 - r.Bal2; }
            r.Source0 = open.HasValue ? openSource : 0;
            if (open.HasValue) { r.Bal0 = open.Value; }
            if (open.HasValue && ratios.Count >= 1) { r.HasElectionYear = true; r.BalchEy = open.Value - r.Bal1; }
            r.HeldTerm = r.HasTerm ? Held(r.BalchTerm) : 0.0;
            r.HeldEy = r.HasElectionYear ? Held(r.BalchEy) : 0.0;
            r.Index = BetaTerm * r.HeldTerm + BetaElectionYear * r.HeldEy;
            r.Uncapped = r.Spread * r.Index;
            r.GovernmentShift = Capped(r.Uncapped);
            r.CapBinds = r.GovernmentShift != r.Uncapped;
            return r;
        }

        /// <summary>The fiscal term of the record, per governing party, as of <paramref name="asOf"/> (the day the responsibility vector reads the Vice
        /// President on, as the economic vote's does), for the election held on <paramref name="electionDay"/> (null: <paramref name="asOf"/>'s own - a
        /// count's day; a campaign's opening passes its election's, the s-fisc review 2's M1). Empty where no government is stored or neither variable
        /// can be formed under its tenure.</summary>
        public static Dictionary<string, double> ShiftOverTerm(Country country, DateTime asOf, out Reading reading, DateTime? electionDay = null)
        {
            reading = Judge(country, TenureFrom(country), electionDay ?? asOf);
            if (country?.Government == null || (!reading.HasTerm && !reading.HasElectionYear)) { return new Dictionary<string, double>(); }
            Dictionary<string, double> split = EconomicVote.SplitGovernmentShift(country, reading.GovernmentShift, asOf);
            reading.Carried = split.Count > 0;
            return split;
        }

        /// <summary>A judgement in one line, for the logs - every figure the term is formed from, so the line can be recomputed from itself
        /// (`Tools/audit/aloop_record_check.pl`): the years read, the tenure, BAL-4..BAL0, the variables, the index, the spread, the cap.</summary>
        public static string Describe(Reading r)
        {
            string P(double v) => double.IsNaN(v) ? "n/a" : string.Format(CultureInfo.InvariantCulture, "{0:+0.0000;-0.0000}", 100.0 * v);
            string tenure = r.TenureFrom.HasValue
                ? string.Format(CultureInfo.InvariantCulture, "tenure from {0:yyyy-MM-dd}, {1} closed year(s){2} before it not counted", r.TenureFrom.Value, r.OutsideTenure, r.OpenOutsideTenure ? " and the open year" : "")
                : "no tenure read";
            string S(string name, int y) => y != 0 ? string.Format(CultureInfo.InvariantCulture, " {0} {1}", name, y) : "";
            string sources = S("BAL-4", r.Source4) + S("BAL-3", r.Source3) + S("BAL-2", r.Source2) + S("BAL-1", r.Source1) + S("BAL0", r.Source0);
            string years = string.Format(CultureInfo.InvariantCulture, "fiscal years read {0} ({1} from the source; source years:{8}{9}), {2}; BAL-4 {3} BAL-3 {4} BAL-2 {5} BAL-1 {6} BAL0 {7} pp of GDP",
                r.ClosedYears, r.SourcedYears, tenure, P(r.Bal4), P(r.Bal3), P(r.Bal2), P(r.Bal1), P(r.Bal0), sources.Length > 0 ? sources : " none", r.ElectionYear != 0 ? string.Format(CultureInfo.InvariantCulture, "; election year {0}", r.ElectionYear) : "");
            if (!r.HasTerm && !r.HasElectionYear) { return years + " - no fiscal term"; }
            string term = r.HasTerm
                ? string.Format(CultureInfo.InvariantCulture, "term {0:+0.0000;-0.0000} pp of GDP{1}{2}", 100.0 * r.BalchTerm, r.TermFallback ? " (two years)" : "", r.HeldTerm != r.BalchTerm ? string.Format(CultureInfo.InvariantCulture, ", held at {0:+0.0;-0.0}", 100.0 * r.HeldTerm) : "")
                : "no term variable";
            string year = r.HasElectionYear
                ? string.Format(CultureInfo.InvariantCulture, "election year {0:+0.0000;-0.0000} pp of GDP{1}", 100.0 * r.BalchEy, r.HeldEy != r.BalchEy ? string.Format(CultureInfo.InvariantCulture, ", held at {0:+0.0;-0.0}", 100.0 * r.HeldEy) : "")
                : "no election-year variable";
            string delivered = r.Carried
                ? string.Format(CultureInfo.InvariantCulture, "the government side {0:+0.0000;-0.0000} pp", 100.0 * r.GovernmentShift)
                : "no governing party carries it, so nothing is applied";
            return string.Format(CultureInfo.InvariantCulture, "{0}; balance change over the {1}, {2}: index {3:+0.000000;-0.000000} x spread {4:0.000000} pp = {5:+0.0000;-0.0000} pp{6} -> {7}",
                years, term, year, r.Index, 100.0 * r.Spread, 100.0 * r.Uncapped, r.CapBinds ? string.Format(CultureInfo.InvariantCulture, ", capped at +/-{0:0.00} pp", 100.0 * ShareCap) : "", delivered);
        }
    }
}
