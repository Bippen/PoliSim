using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// A-LOOP q15 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §3, Elias's ruling J1): <b>approval reaches the vote, through a sourced popularity
    /// function.</b> The approval term is one more component of the government's record (`EconomicVote.RecordPartsOverTerm`), beside the economic
    /// vote, on the one path the record already takes into every live count - no second path into the vote is built (the plan's §0).
    /// <list type="bullet">
    /// <item><b>The term:</b> shift_gov = b × (A_E − A_0), in points of vote share. A_E is net approval on the day the record is judged (the eve for
    /// the count, as §752 judges the economy); A_0 is net approval on the day the government took office (`EconomicVote.TookOffice`), read off the
    /// marks the daily step takes (<see cref="NoteGovernment"/>), so a seeded government's reference is the seed's and a government formed in play
    /// starts at zero.</item>
    /// <item><b>Which approval (Q1 (a), ruled):</b> approval NET of its three economy terms - the growth gap and the unemployment and inflation
    /// misery terms - because the economy already votes through Duch &amp; Stevenson (<see cref="EconomicVote"/>). A second stock carries just those
    /// terms under approval's own reversion rate (`Country.ApprovalEconomyStock`, <see cref="AdvanceEconomyStock"/>); the vote reads approval minus it
    /// (<see cref="NetApproval"/>). Every other part of approval - the tax term and its fading (q37), spending, welfare, events, the reversion of
    /// those parts - stays in what the vote reads.</item>
    /// <item><b>The split:</b> shift_gov is divided among the governing parties by the responsibility vector the economic vote carries
    /// (`EconomicVote.SplitGovernmentShift`), so the opposition gets none and its share of a loss arrives by absorption.</item>
    /// </list>
    /// <para>⚠ <b>Declared readings</b> (the plan leaves them open):
    /// (1) the economy stock reverts at approval's own reversion rate, applied to the stock alone (the base the reversion reads - approval + T, q37 -
    /// is neutral + the economy's part + the rest, and a reversion linear in it takes the same fraction of each part);
    /// (2) where the base's [0, 100] clamp binds at a boundary, what it gives back or takes is shared between the economy stock and every other part
    /// of the base in proportion to each one's push past the bound (<see cref="ClampShare"/>; review 879's M1, on q37's contract that the base is the
    /// run without the tax term, kept inside [0, 100]), and base = neutral + the stock + the rest stays exact; where approval meets a bound and the
    /// base does not (a tax term standing against the bound), approval's own clamp is shared the same way (review 879b's M-2), the stock then off the
    /// base by at most the standing tax term - so at the floor a worse economy never raises net approval and at the ceiling a better one never lowers
    /// it; net approval is also held in [0, 100] itself, so |shift_gov| ≤ b × 100 by construction (§3 (4) (d));
    /// (3) A_E is approval as it stands when the record is judged - the live game judges on the day itself; approval keeps no history to read an
    /// earlier day from;
    /// (4) the reference: a government formed on the day it took office (one formed in play) reads its OWN mark, the one keyed by its formation day
    /// and taken the next day (the daily step marks after the tax term's fading and before the day's other moves), never a predecessor's (review 879's
    /// L1); a government whose term the record walks back to an earlier day (a government of record, `EconomicVote.TookOffice`) reads the first mark on
    /// or after that day - the start's for one in office before the run; a government with no mark yet (formed today, or a world never stepped)
    /// carries no approval term;
    /// (5) the economy stock advances as each boundary's approval period closes (`ApprovalLedgerRecorder.CloseAtBoundary`), from the
    /// period's recorded terms; the policy preview closes nothing, so its clone carries the stock unadvanced - nothing on the preview votes;
    /// (6) a government formed in play inherits the rest of approval as it stands (review 879's L2): its predecessor's non-economy residue, still
    /// reverting toward neutral, and its standing tax term, still fading - so a successor to an unpopular government gains a little over its term
    /// for nothing it did (about b × 30 × (1 − 0.95⁴) ≈ 0.9 pp after a residue of −30), and one after a popular government loses as much. Q3 rules
    /// approval's persistence out of A-LOOP (MF-13), and this direction is its consequence;
    /// (7) only the formula's three economy terms are netted out (review 879's L3): an economic EVENT's approval effect (`EventSystem.ApplyEvent`'s
    /// approval shock) stays in net approval, as the ruling's "its three economy terms" reads.</para>
    /// <para>The US House coefficients the plan names (the midterm's 0.13, Tufte 1978 via [LBS00] Table 7 col. 1, and the on-year's carried 0.13)
    /// have no live count to reach until the game counts the House (US-16); R-US20's (c) needs a presidential approval series of record (Gallup,
    /// BILLED in the plan's §8). Neither is built here.</para>
    /// </summary>
    public static class ApprovalVote
    {
        /// <summary><b>b, the approval-to-vote coefficient</b>: points of the government's vote share per point of approval change over the term.
        /// SOURCED (secondary) for the USA: Lewis-Beck &amp; Tien 1996 as tabulated in [LBS00] - Lewis-Beck &amp; Stegmaier, "Economic Determinants of
        /// Electoral Outcomes", Annu. Rev. Polit. Sci. 3 (2000) 183-219 (`docs/reference/raw/aloop/almendron_mirror_economicdeterminants.pdf`, its digest
        /// in that folder's `SHA256SUMS.txt`), TABLE 4 "Examples of presidential vote function models (figures in parentheses are t-ratios)", column
        /// (2): "Presidential popularity ... 0.16 (2.11)", beside "GNP change ... 1.83 (3.33)" and "Peace/prosperity ... 0.14 (2.35)", constant 27.34,
        /// adjusted R-squared 0.88, SEE 2.26, N 11; its note: "Dependent variable = percentage of the popular two-party vote (1952-1992); presidential
        /// popularity = the Gallup Poll approval rating in July". Secondary because the 1996 original was not fetched (BILLED, the plan's §8). A units
        /// transfer even here (review 879's L6): the source's dependent variable is the incumbent's TWO-PARTY share; the game applies the figure to the
        /// party's share of the whole vote, minor candidates included.
        /// [AUTHORED-DRAFT] everywhere else (Q2 (a), ruled): the US figure TRANSFERRED to every country - the fetched bracket is 0.13 (the US midterm,
        /// [LBS00] Table 7 col. 1) to 0.38 (France's Assembly, [LBS00] Table 10 col. 2: "Popularity -0.38 (3.69)", the opposition's first-round share);
        /// a European estimate is BILLED to replace it (Nannestad &amp; Paldam 1994, Lewis-Beck &amp; Stegmaier 2013). On the play-calibration list: "q15's
        /// approval-to-vote coefficient outside the USA: 0.16 per point of approval change over the term, Lewis-Beck &amp; Tien's US presidential figure
        /// [LBS00] transferred; the fetched bracket is 0.13-0.38. Look for in play: does a government the player has driven to 20 % lose the count it
        /// would otherwise win?"</summary>
        public const double PresidentialVoteCoefficient = 0.16;

        /// <summary>The coefficient a country's count reads: <see cref="PresidentialVoteCoefficient"/> everywhere - sourced for the USA (its live count is
        /// the presidential one), transferred elsewhere (<see cref="IsTransferred"/>).</summary>
        public static double CoefficientFor(CountryId id) => PresidentialVoteCoefficient;

        /// <summary>True where the coefficient is the US figure carried to another country ([AUTHORED-DRAFT], Q2 (a)).</summary>
        public static bool IsTransferred(CountryId id) => id != CountryId.USA;

        /// <summary>DERIVED: the share of the economy stock standing after one boundary - one less approval's own reversion rate a turn
        /// (`MacroSystem.ApplyApprovalRating`'s reversion), the rate the stock is carried under.</summary>
        public static float EconomyStockPersistence => 1f - Simulation.MacroSystem.ApprovalReversionSpeed;

        /// <summary>Advances the economy stock by one closed approval period: the stock's reversion, the period's three economy terms as the
        /// ledger recorded them (the formula's own terms, signed as contributions), and the stock's share of the clamp where it bound
        /// (<see cref="ClampShare"/>). Called as the period closes.</summary>
        public static void AdvanceEconomyStock(Country country, ApprovalAttribution closed)
        {
            if (country == null || closed == null) { return; }
            float before = country.ApprovalEconomyStock;
            float economy = closed.GrowthEffect + closed.MiseryUnemployment + closed.MiseryInflation;
            country.ApprovalEconomyStock = before * EconomyStockPersistence + economy + ClampShare(closed, before);
        }

        /// <summary>
        /// Declared reading 2 (review 879's M1, on q37's contract of review 878b's M-5; review 879b's M-2): the economy stock's share of a clamp at the
        /// boundary formula. Two clamps can bind, and the stock shares one of them:
        /// <list type="bullet">
        /// <item><b>The base's</b> - approval + T, the approval of the run without the tax term, which q37 keeps inside [0, 100] (`TaxTerm.BookFormula`,
        /// `TaxTerm.Reclamp`) and which approval's reversion reads. The stock is a part of that base (base = neutral + the stock + the rest) and the tax
        /// impulse is not: the base moves by the formula's terms with the impulse aside (FormulaTermSum + impulse) and is clamped as a whole. The base
        /// before the formula is read back from the formula's own reversion term (<see cref="BaseBeforeFormula"/>); the clamp is Clamp(x, 0, 100) − x
        /// for x = that base + the formula's step without the impulse.</item>
        /// <item><b>Approval's own</b> (`ApprovalAttribution.ClampLoss`), where approval met a bound and the base did not - a tax term standing against
        /// the bound (T &gt; 0 at the floor, T &lt; 0 at the ceiling), which `TaxTerm.BookFormula` forgives by approval's truncation. The vote reads approval,
        /// so the stock shares approval's clamp here, over the formula's own step (the impulse included); in this window the stock is not strictly a
        /// part of the base, by at most the standing tax term.</item>
        /// </list>
        /// Either clamp is shared between the stock - whose own move is its reversion and the three economy terms - and the rest, in proportion to
        /// each part's move past the bound (only a part moving toward the bound pushed past it); 0 where neither binds. So at the floor a worse economy
        /// never raises net approval, and at the ceiling a better one never lowers it. ⚠ The read-back of the base is accurate to a few ulp of the
        /// neutral rating (about 1e-5, review 879b's L-2): a base landing within that of a bound may take a spurious share of at most about 1e-5.
        /// </summary>
        public static float ClampShare(ApprovalAttribution closed, float stockBefore)
        {
            if (closed == null) { return 0f; }
            float speed = Simulation.MacroSystem.ApprovalReversionSpeed;
            float own = -speed * stockBefore + closed.GrowthEffect + closed.MiseryUnemployment + closed.MiseryInflation;
            float step = closed.FormulaTermSum - closed.TaxHikePenalty;   // the formula's step with the tax impulse aside (TaxHikePenalty is -impulse)
            float x = BaseBeforeFormula(closed) + step;
            if (x < 0f || x > 100f) { return Shared(Math.Max(0f, Math.Min(100f, x)) - x, own, step - own); }   // the base's clamp
            bool approvalBound = closed.ApprovalAfterFormula <= 0f || closed.ApprovalAfterFormula >= 100f;
            return approvalBound && closed.ClampLoss != 0f ? Shared(closed.ClampLoss, own, closed.FormulaTermSum - own) : 0f;   // approval's own clamp (M-2)
        }

        /// <summary>A clamp (positive: given back at the floor; negative: taken at the ceiling) shared between two parts by their push past the bound.</summary>
        private static float Shared(float clamp, float own, float rest)
        {
            if (clamp == 0f) { return 0f; }
            float toward = clamp > 0f ? -1f : 1f;   // at the floor the parts that pushed past it moved down
            float ownPush = Math.Max(0f, toward * own), restPush = Math.Max(0f, toward * rest);
            float push = ownPush + restPush;
            return push > 0f ? clamp * ownPush / push : 0f;
        }

        /// <summary>The base (approval + T) the boundary formula reverted, read back from its own reversion term: reversion = speed × (neutral − base).</summary>
        public static float BaseBeforeFormula(ApprovalAttribution closed)
            => Simulation.MacroSystem.NeutralApprovalRating - closed.Reversion / Simulation.MacroSystem.ApprovalReversionSpeed;

        /// <summary>Net approval: approval less its economy stock, held in [0, 100] (declared reading 2), in approval points.</summary>
        public static double NetApproval(Country country)
        {
            double net = (double)country.State.ApprovalRating - country.ApprovalEconomyStock;
            return Math.Max(0.0, Math.Min(100.0, net));
        }

        /// <summary>The daily step (`SimulationManager.AdvanceDay`, after the tax term's fading and the day's publications, before the day's other moves): a mark of net approval the first day a stored government
        /// is seen - a government whose formation day differs from the last mark's. Nothing else is written.</summary>
        public static void NoteGovernment(Country country, DateTime date)
        {
            GovernmentRecord g = country?.Government;
            if (g == null) { return; }
            List<ApprovalMark> marks = country.VoteApprovalMarks ?? (country.VoteApprovalMarks = new List<ApprovalMark>());
            if (marks.Count > 0 && marks[marks.Count - 1].FormedOn == g.FormedOn) { return; }
            marks.Add(new ApprovalMark { FormedOn = g.FormedOn, On = date, Net = NetApproval(country) });
        }

        /// <summary>The reference for the stored government, which took office on <paramref name="tookOffice"/> (declared reading 4): its own mark
        /// where it was formed that day; else the first mark taken on or after that day. False where none is.</summary>
        public static bool TryReference(Country country, DateTime tookOffice, out ApprovalMark mark)
        {
            mark = null;
            if (country?.VoteApprovalMarks == null) { return false; }
            GovernmentRecord g = country.Government;
            if (g != null && g.FormedOn.Date == tookOffice.Date)
            {
                foreach (ApprovalMark m in country.VoteApprovalMarks) { if (m.FormedOn == g.FormedOn) { mark = m; return true; } }
                return false;   // formed that day and not yet marked: no reference, never a predecessor's mark (review 879's L1)
            }
            foreach (ApprovalMark m in country.VoteApprovalMarks)
            {
                if (m.On.Date >= tookOffice.Date) { mark = m; return true; }
            }
            return false;
        }

        /// <summary>One judgement of the approval term, for the logs and the diagnostics.</summary>
        public struct Reading
        {
            /// <summary>Approval and its economy stock as they stood, and net approval (A_E).</summary>
            public float Approval, EconomyStock;
            public double Net;
            /// <summary>Whether a reference was found, its net approval (A_0) and the day it was taken; the day the government took office.</summary>
            public bool HasReference;
            public double Reference;
            public DateTime ReferenceOn;
            public DateTime? TookOffice;
            /// <summary>b, whether it is the transferred figure, and shift_gov as a share fraction (0 where no reference).</summary>
            public double Coefficient;
            public bool Transferred;
            public double GovernmentShift;
            /// <summary>Whether any governing party carries the term (the responsibility vector is not empty).</summary>
            public bool Carried;
        }

        /// <summary>The approval term of the record over the term, per governing party, as of <paramref name="asOf"/> (the day the responsibility vector
        /// reads the Vice President on, as the economic vote's does). Empty where no government is stored or no reference is marked.</summary>
        public static Dictionary<string, double> ShiftOverTerm(Country country, DateTime asOf, out Reading reading)
        {
            reading = default(Reading);
            if (country?.State == null) { return new Dictionary<string, double>(); }
            reading.Approval = country.State.ApprovalRating;
            reading.EconomyStock = country.ApprovalEconomyStock;
            reading.Net = NetApproval(country);
            reading.Coefficient = CoefficientFor(country.Id);
            reading.Transferred = IsTransferred(country.Id);
            reading.TookOffice = EconomicVote.TookOffice(country);
            if (!reading.TookOffice.HasValue || !TryReference(country, reading.TookOffice.Value, out ApprovalMark mark)) { return new Dictionary<string, double>(); }
            reading.HasReference = true;
            reading.Reference = mark.Net;
            reading.ReferenceOn = mark.On;
            reading.GovernmentShift = reading.Coefficient * (reading.Net - reading.Reference) / 100.0;
            Dictionary<string, double> split = EconomicVote.SplitGovernmentShift(country, reading.GovernmentShift, asOf);
            reading.Carried = split.Count > 0;
            return split;
        }

        /// <summary>A judgement in one line, for the logs.</summary>
        public static string Describe(Reading r)
        {
            if (!r.TookOffice.HasValue) { return "approval: no government stored"; }
            if (!r.HasReference) { return string.Format(CultureInfo.InvariantCulture, "approval: no reference marked for the government in office from {0:yyyy-MM-dd} - no approval term", r.TookOffice.Value); }
            string delivered = r.Carried
                ? string.Format(CultureInfo.InvariantCulture, "the government side {0:+0.00;-0.00} pp", 100.0 * r.GovernmentShift)
                : "no governing party carries it, so nothing is applied";   // review 879's L5: no figure where the split is empty
            return string.Format(CultureInfo.InvariantCulture,
                "approval net of its economy terms {0:0.00} (approval {1:0.00}, its economy stock {2:+0.00;-0.00}) against {3:0.00} when the government took office ({4:yyyy-MM-dd}, marked {5:yyyy-MM-dd}): b {6:0.00}{7} -> {8}",
                r.Net, r.Approval, r.EconomyStock, r.Reference, r.TookOffice.Value, r.ReferenceOn, r.Coefficient, r.Transferred ? " (transferred)" : "", delivered);
        }
    }
}
