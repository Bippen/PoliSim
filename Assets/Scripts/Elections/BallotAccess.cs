using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections.Generated;

namespace PoliSim.Elections
{
    /// <summary>
    /// SP-5 (S5, Design's return of 2026-10-30; K164, K165): **FOUNDING OUTSIDE SWEDEN - A NEW PARTY'S WAY ONTO THE BALLOT, BY THE COUNTRY'S OWN LAW.**
    /// Germany first (K164). The law as data, every figure sourced under `ElectionsData/germany/raw/ballot_access/` (SHA256SUMS.txt beside it):
    /// <list type="bullet">
    /// <item>BWahlG §18(2): a party not seated in the Bundestag or a Landtag since its last election with at least five members elected on its own lists
    /// may stand only if it notified the Federal Returning Officer of its participation by the 97th day before the vote, 18:00, and the Federal Electoral
    /// Committee recognised it as a party (§18(4): by the 79th day).</item>
    /// <item>BWahlG §27(1): each Land list of such a party signed by 1 per mille of the Land's electorate at the last Bundestag election, at most 2,000.</item>
    /// <item>BWahlG §19: lists filed by the 69th day before the vote, 18:00.</item>
    /// <item>BWahlG §20(2): 200 signatures for each constituency candidate, admitted only where the party's Land list is admitted.</item>
    /// <item>The snap election of 23 February 2025 ran on the deadlines BWahlG §52(3) lets the Interior Ministry shorten: the ordinance BGBl. 2024 I Nr. 436
    /// (the PDF on disk carries the publisher's MD5), as the Federal Returning Officer's own dates page prints them - the notice by the 47th day
    /// (7 Jan 2025), recognition by the 40th (14 Jan), the lists by the 34th (20 Jan). The signature counts were unchanged (her candidates' page: "bleibt
    /// die erforderliche Zahl der Unterstützungsunterschriften für Parteien unverändert"), and that page's table of the sixteen counts is what
    /// <see cref="LandListNeed"/> derives (`BallotAccessCheck` holds every one).</item>
    /// </list>
    /// <para><b>The task</b> (K165, "time plus money at a sourced price per signature, a run-up task"): GATHER puts a third, two thirds or all of the party's
    /// volunteers (SIZE x1, x2, x3) on one Land's list for a day; its signatures count from the next day; FILE files the notice and every Land list whose
    /// signatures are in. A deadline that passes short REFUSES: no notice by its day, no list anywhere; a list not filed by the lists' day, no list votes
    /// in that Land (<see cref="StandsIn"/>, which `GermanRegions.Regions` and the count read). ⚠ <b>The price per signature is BILLED</b>: no primary source
    /// prices a German support signature (the searches are in `ElectionsData/germany/raw/ballot_access/BILLED.md`), so GATHER spends volunteer days and
    /// no money - Design's € 2.40 was illustrative - and the money half of K165 waits on a sourced price.</para>
    /// <para>The task's state rides the <see cref="CreatedParty"/> (saved with it, `SaveGame.CreatedParties`): the polling day it is for, the notice's
    /// filing day, the gathers and the filings, each dated. Everything else is DERIVED from those and the date, so nothing advances it but the player's
    /// own verbs and the calendar. With no created party registered nothing here is read: every real party stands where its returns say it stood.</para>
    /// </summary>
    public static class BallotAccess
    {
        /// <summary>What the law asks of a created party in <paramref name="country"/>, as this build holds it.</summary>
        public enum Rules
        {
            /// <summary>No ballot-access task in this build: Sweden as built (its registration rule is not modelled - owed), the USA's own form, France with no created party.</summary>
            NotModelled,
            /// <summary>The country's rules are on disk and the task is built: Germany.</summary>
            Sourced,
            /// <summary>The country requires access but its rules are not yet on disk: the slip says "ABSENT · not yet sourced" (Poland next, Italy after PS-7).</summary>
            Absent,
        }

        public static Rules RulesOf(CountryId country) => country == CountryId.Germany ? Rules.Sourced
            : country == CountryId.Poland || country == CountryId.Italy ? Rules.Absent : Rules.NotModelled;

        // ── Germany's law (BWahlG, as on disk) ──────────────────────────────────────────────────────────────

        /// <summary>BWahlG §18(2): the notice by the 97th day before the vote.</summary>
        public const int NoticeDaysBefore = 97;
        /// <summary>BWahlG §18(4): the committee's recognition by the 79th day.</summary>
        public const int RecognitionDaysBefore = 79;
        /// <summary>BWahlG §19: the lists filed by the 69th day.</summary>
        public const int ListsDaysBefore = 69;
        /// <summary>BWahlG §27(1): a Land list's signatures - 1 per mille of the Land's electorate at the last Bundestag election ...</summary>
        public const int PerMille = 1;
        /// <summary>... and at most 2,000.</summary>
        public const int LandListCap = 2000;
        /// <summary>BWahlG §20(2): signatures for each constituency candidate.</summary>
        public const int PerConstituency = 200;

        /// <summary>The snap election the ordinance BGBl. 2024 I Nr. 436 shortened (the Federal Returning Officer's dates page).</summary>
        public static readonly DateTime Snap2025 = new DateTime(2025, 2, 23);
        public const int SnapNoticeDaysBefore = 47;
        public const int SnapRecognitionDaysBefore = 40;
        public const int SnapListsDaysBefore = 34;

        /// <summary>The three deadlines for the election on <paramref name="pollingDay"/>: the statute's days, or the ordinance's for the 2025 snap. Each is
        /// the day itself (18:00 on it, so a filing on the day counts).</summary>
        public static void Deadlines(DateTime pollingDay, out DateTime notice, out DateTime recognition, out DateTime lists)
        {
            bool snap = pollingDay.Date == Snap2025;
            notice = pollingDay.Date.AddDays(-(snap ? SnapNoticeDaysBefore : NoticeDaysBefore));
            recognition = pollingDay.Date.AddDays(-(snap ? SnapRecognitionDaysBefore : RecognitionDaysBefore));
            lists = pollingDay.Date.AddDays(-(snap ? SnapListsDaysBefore : ListsDaysBefore));
        }

        /// <summary>The day number before the vote that the deadline is (the law's own way of naming it: "day 34").</summary>
        public static int DayBefore(DateTime pollingDay, DateTime deadline) => (int)(pollingDay.Date - deadline.Date).TotalDays;

        /// <summary>BWahlG §27(1): the signatures a Land list needs for the election on <paramref name="pollingDay"/> - 1 per mille of the Land's electorate
        /// at the LAST Bundestag election (the register before this polling day: 2021's for 2025, 2025's after it, `GermanRegions.EligibleForElection`),
        /// rounded up ("von 1 vom Tausend ... unterzeichnet": at least that share; the Federal Returning Officer's 2025 table reads Bremen 460 for 459,749),
        /// at most 2,000.</summary>
        public static int LandListNeed(int region, DateTime pollingDay)
        {
            long electorate = GermanRegions.EligibleForElection(region, pollingDay.Date.AddDays(-1));
            long need = (electorate * PerMille + 999L) / 1000L;
            return (int)Math.Min(LandListCap, need);
        }

        public static int LandCount => GermanRegions.Count;

        /// <summary>The whole need across the sixteen lists.</summary>
        public static int TotalListNeed(DateTime pollingDay)
        {
            int total = 0;
            for (int r = 0; r < LandCount; r++) { total += LandListNeed(r, pollingDay); }
            return total;
        }

        // ── the task's yield ([AUTHORED-DRAFT], on the play-calibration list) ──────────────────────────────

        /// <summary>[AUTHORED-DRAFT] signatures one volunteer gathers in a day on the street. No source measures it; the figure is game design, on the
        /// play-calibration list. With it a Grassroots party (Activists 5) can fill Germany's sixteen lists in about six weeks of full effort, and a party
        /// with few volunteers cannot - which, while the price per signature is BILLED, no money can change.</summary>
        public const double SignaturesPerVolunteerDay = 1.0;

        /// <summary>SIZE: the thirds of the party's volunteers a GATHER puts on the street (x1, x2, x3); a day holds three thirds in all.</summary>
        public const int MaxSize = 3;

        /// <summary>The party's volunteers, as its day zero sets them (`LiveCampaignSetup.TryCreatedDayZero`: the real parties' volunteers times the Activists
        /// pip's share).</summary>
        public static int VolunteersOf(CreatedParty c) => c == null ? 0 : (int)Math.Round(LiveCampaignSetup.Volunteers * PartyOrigins.Pip(PartyOrigins.ActivistShare, c.Activists));

        /// <summary>The signatures one GATHER at <paramref name="size"/> brings in.</summary>
        public static int YieldOf(CreatedParty c, int size) => (int)Math.Round(Math.Max(1, Math.Min(MaxSize, size)) / (double)MaxSize * VolunteersOf(c) * SignaturesPerVolunteerDay);

        // ── the task's state, derived ───────────────────────────────────────────────────────────────────────

        public enum TaskState { NotRequired, Collecting, OnTheBallot, Refused }

        private static string Iso(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static bool TryDate(string iso, out DateTime d) => DateTime.TryParseExact(iso ?? string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d);

        /// <summary>The created party registered under <paramref name="key"/> in <paramref name="country"/>, or null.</summary>
        public static CreatedParty Created(CountryId country, string key)
        {
            foreach (CreatedParty c in CreatedParties.Of(country)) { if (c.Key == key) { return c; } }
            return null;
        }

        /// <summary>The polling day the task is for on <paramref name="today"/> - the country's next on or after it (`WorldClock.TryNextPollingDay`).</summary>
        public static bool TryElection(CountryId country, DateTime today, out DateTime pollingDay) => WorldClock.TryNextPollingDay(country, today.Date, out pollingDay);

        /// <summary>Points a party with no task yet at <paramref name="pollingDay"/> - its first election, the start's. A task already held is never cleared,
        /// so the record a count was taken on survives it (a night derived again after the polling day reads the same lists). ⚠ The task is built for that
        /// first election only: a LATER election's (the law asks it afresh, and exempts a party holding five seats of its own lists) is not modelled - owed -
        /// and <see cref="StandsIn"/> reads the party as standing there.</summary>
        public static void EnsureFor(CreatedParty c, DateTime pollingDay)
        {
            if (c == null || !string.IsNullOrEmpty(c.BallotFor)) { return; }
            c.BallotFor = Iso(pollingDay);
        }

        /// <summary>Whether the party's task is the one for <paramref name="pollingDay"/> (its first election's).</summary>
        public static bool TaskIsFor(CreatedParty c, DateTime pollingDay) => IsFor(c, pollingDay);

        private static bool IsFor(CreatedParty c, DateTime pollingDay) => c != null && c.BallotFor == Iso(pollingDay);

        /// <summary>The signatures in for <paramref name="region"/>'s list on <paramref name="today"/>: every gather dated before today (a gather counts from
        /// the day after it is queued), at most the list's need.</summary>
        public static int Signatures(CreatedParty c, int region, DateTime pollingDay, DateTime today)
        {
            if (!IsFor(c, pollingDay)) { return 0; }
            int sum = 0;
            foreach (BallotGather g in c.BallotGathers) { if (g.Region == region && TryDate(g.On, out DateTime on) && on < today.Date) { sum += g.Signatures; } }
            return Math.Min(sum, LandListNeed(region, pollingDay));
        }

        /// <summary>The signatures queued today (the day's DRAFT rows) for <paramref name="region"/>, or for every Land where <paramref name="region"/> is −1.</summary>
        public static int QueuedToday(CreatedParty c, int region, DateTime pollingDay, DateTime today, out int sizeUsed)
        {
            sizeUsed = 0;
            if (!IsFor(c, pollingDay)) { return 0; }
            int sum = 0;
            foreach (BallotGather g in c.BallotGathers)
            {
                if (!TryDate(g.On, out DateTime on) || on != today.Date) { continue; }
                sizeUsed += g.Size;
                if (region < 0 || g.Region == region) { sum += g.Signatures; }
            }
            return sum;
        }

        /// <summary>The day <paramref name="region"/>'s list was filed, or null.</summary>
        public static DateTime? FiledOn(CreatedParty c, int region, DateTime pollingDay)
        {
            if (!IsFor(c, pollingDay)) { return null; }
            foreach (BallotFiling f in c.BallotFilings) { if (f.Region == region && TryDate(f.On, out DateTime on)) { return on; } }
            return null;
        }

        public static DateTime? NoticeFiledOn(CreatedParty c, DateTime pollingDay) => IsFor(c, pollingDay) && TryDate(c.BallotNoticeFiledOn, out DateTime on) ? on : (DateTime?)null;

        /// <summary>Whether the notice was filed in time, as of <paramref name="today"/>: filed on or before its deadline. The committee's recognition follows a
        /// notice in time (DECLARED: a created party meets PartG §2's test; the committee's two-thirds refusal is not modelled).</summary>
        public static bool NoticeInTime(CreatedParty c, DateTime pollingDay, DateTime today)
        {
            Deadlines(pollingDay, out DateTime notice, out DateTime _, out DateTime _);
            DateTime? on = NoticeFiledOn(c, pollingDay);
            return on.HasValue && on.Value <= notice && on.Value <= today.Date;
        }

        /// <summary>Whether <paramref name="region"/>'s list stands, as of <paramref name="today"/>: the notice in time and the list filed by the lists' day.</summary>
        public static bool ListStands(CreatedParty c, int region, DateTime pollingDay, DateTime today)
        {
            Deadlines(pollingDay, out DateTime _, out DateTime _, out DateTime lists);
            DateTime? on = FiledOn(c, region, pollingDay);
            return NoticeInTime(c, pollingDay, today) && on.HasValue && on.Value <= lists && on.Value <= today.Date;
        }

        /// <summary>The Länder whose lists are refused as of <paramref name="today"/> - none before the deadline that refuses them.</summary>
        public static int RefusedCount(CreatedParty c, DateTime pollingDay, DateTime today)
        {
            Deadlines(pollingDay, out DateTime notice, out DateTime _, out DateTime lists);
            if (today.Date > notice && !NoticeInTime(c, pollingDay, today)) { return LandCount; }
            if (today.Date <= lists) { return 0; }
            int n = 0;
            for (int r = 0; r < LandCount; r++) { if (!ListStands(c, r, pollingDay, today)) { n++; } }
            return n;
        }

        public static int FiledCount(CreatedParty c, DateTime pollingDay)
        {
            int n = 0;
            for (int r = 0; r < LandCount; r++) { if (FiledOn(c, r, pollingDay).HasValue) { n++; } }
            return n;
        }

        /// <summary>The task's state on <paramref name="today"/>.</summary>
        public static TaskState StateOf(CountryId country, CreatedParty c, DateTime pollingDay, DateTime today)
        {
            if (c == null || RulesOf(country) != Rules.Sourced) { return TaskState.NotRequired; }
            if (RefusedCount(c, pollingDay, today) > 0) { return TaskState.Refused; }
            bool all = NoticeFiledOn(c, pollingDay).HasValue && FiledCount(c, pollingDay) == LandCount;
            return all ? TaskState.OnTheBallot : TaskState.Collecting;
        }

        // ── the verbs ────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>GATHER on <paramref name="region"/>'s list at <paramref name="size"/> on <paramref name="today"/>, or false with the reason: the lists' deadline
        /// passed, the list already filed or full, or the day's volunteers already out (three thirds a day).</summary>
        public static bool TryGather(CreatedParty c, int region, int size, DateTime pollingDay, DateTime today, out string refused)
        {
            refused = null;
            EnsureFor(c, pollingDay);
            Deadlines(pollingDay, out DateTime _, out DateTime _, out DateTime lists);
            size = Math.Max(1, Math.Min(MaxSize, size));
            if (region < 0 || region >= LandCount) { refused = "no Land chosen"; return false; }
            if (today.Date > lists) { refused = "the lists' deadline has passed"; return false; }
            if (FiledOn(c, region, pollingDay).HasValue) { refused = "that list is filed"; return false; }
            QueuedToday(c, -1, pollingDay, today, out int used);
            if (used + size > MaxSize) { refused = "the day's volunteers are all out"; return false; }
            int room = LandListNeed(region, pollingDay) - Signatures(c, region, pollingDay, today) - QueuedToday(c, region, pollingDay, today, out int _);
            if (room <= 0) { refused = "that list's signatures are in"; return false; }
            int yield = Math.Min(room, YieldOf(c, size));
            if (yield <= 0) { refused = "no volunteers to send"; return false; }
            c.BallotGathers.Add(new BallotGather { On = Iso(today), Region = region, Size = size, Signatures = yield });
            return true;
        }

        /// <summary>FILE on <paramref name="today"/>: the notice, while its day has not passed, and every Land list whose signatures are in, while the lists' day
        /// has not passed. Returns how many lists it filed (the notice counted apart in <paramref name="notice"/>).</summary>
        public static int File(CreatedParty c, DateTime pollingDay, DateTime today, out bool notice)
        {
            notice = false;
            EnsureFor(c, pollingDay);
            Deadlines(pollingDay, out DateTime noticeDay, out DateTime _, out DateTime lists);
            if (!NoticeFiledOn(c, pollingDay).HasValue && today.Date <= noticeDay) { c.BallotNoticeFiledOn = Iso(today); notice = true; }
            if (today.Date > lists) { return 0; }
            int filed = 0;
            for (int r = 0; r < LandCount; r++)
            {
                if (FiledOn(c, r, pollingDay).HasValue || Signatures(c, r, pollingDay, today) < LandListNeed(r, pollingDay)) { continue; }
                c.BallotFilings.Add(new BallotFiling { Region = r, On = Iso(today) });
                filed++;
            }
            return filed;
        }

        // ── what the count reads ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Whether party <paramref name="key"/> stands in <paramref name="region"/> of <paramref name="country"/> for the election the date belongs to - the hook
        /// `GermanRegions.Regions` reads for a party its returns do not carry. True for every party but a created one in a country whose rules are built;
        /// for a created one, true until the lists' deadline of the election on or after <paramref name="date"/> (DECLARED: a list still being gathered is
        /// read as standing - the campaign is staged before the deadline and every party on its ballot competes), and after it only where the notice came in
        /// time and the Land's list was filed by the deadline.
        /// </summary>
        public static bool StandsIn(CountryId country, string key, int region, DateTime date)
        {
            if (RulesOf(country) != Rules.Sourced || !CreatedParties.Any(country)) { return true; }
            CreatedParty c = Created(country, key);
            if (c == null || !TryElection(country, date, out DateTime pollingDay)) { return true; }
            // a task held for an EARLIER election: this later one's is not modelled (EnsureFor's note) - read as standing, DECLARED
            if (!string.IsNullOrEmpty(c.BallotFor) && !IsFor(c, pollingDay) && TryDate(c.BallotFor, out DateTime first) && first < pollingDay.Date) { return true; }
            Deadlines(pollingDay, out DateTime _, out DateTime _, out DateTime lists);
            if (date.Date <= lists) { return true; }
            return ListStands(c, region, pollingDay, date);   // no task held at all: nothing filed, so refused everywhere after the deadline
        }

        /// <summary>
        /// The count's rule for a created party refused somewhere (plan_prereg, `docs/specs/START_POINTS_AND_PARTY_CREATION_SPEC.md` §2.7's ballot-access line):
        /// its national share is its campaign share times the share of the national valid vote cast in the Länder where it stands (`GermanRegions.ValidAt`,
        /// the seated election's valid Zweitstimmen - DECLARED: a newcomer has no regional record, so its support is read as uniform), and the share it loses
        /// goes to the others in proportion (the shares renormalised to their sum). Returns <paramref name="shares"/> itself where nothing is refused, so the
        /// count of a game with no refused created party is the very array it was.
        /// </summary>
        public static double[] ApplyToCount(CountryId country, IReadOnlyList<string> keys, double[] shares, DateTime pollingDay)
        {
            if (RulesOf(country) != Rules.Sourced || !CreatedParties.Any(country) || keys == null || shares == null) { return shares; }
            double[] adjusted = null;
            for (int p = 0; p < keys.Count && p < shares.Length; p++)
            {
                if (Created(country, keys[p]) == null) { continue; }
                double standing = 0.0, total = 0.0;
                for (int r = 0; r < LandCount; r++)
                {
                    double w = GermanRegions.ValidAt(r, pollingDay);
                    total += w;
                    if (StandsIn(country, keys[p], r, pollingDay)) { standing += w; }
                }
                if (total <= 0.0 || standing >= total) { continue; }
                adjusted ??= (double[])shares.Clone();
                adjusted[p] = shares[p] * standing / total;
            }
            if (adjusted == null) { return shares; }
            double before = 0.0, after = 0.0;
            for (int p = 0; p < shares.Length; p++) { before += shares[p]; after += adjusted[p]; }
            if (after <= 0.0) { return adjusted; }
            for (int p = 0; p < adjusted.Length; p++) { adjusted[p] *= before / after; }
            return adjusted;
        }
    }

    /// <summary>SP-5 (S5): one GATHER as the save carries it - the day it was queued (ISO), the Land (its index in `GermanRegions`' order), its SIZE and the
    /// signatures it brings in (fixed when queued, so a load replays the same count).</summary>
    [Serializable]
    public class BallotGather
    {
        public string On;
        public int Region;
        public int Size;
        public int Signatures;
    }

    /// <summary>SP-5 (S5): one Land list filed - the Land and the day.</summary>
    [Serializable]
    public class BallotFiling
    {
        public int Region;
        public string On;
    }

    /// <summary>
    /// SP-5 (S5, Design's return: "the funding line on the Desk ... shows only where the law funds parties"): **STATE PARTY FUNDING, BY THE COUNTRY'S LAW.**
    /// Germany: PartG §18 (`ElectionsData/germany/raw/party_funding/`). §18(4): a party qualifies with 0.5 % of the valid list votes at the last Bundestag or
    /// European election, or 1.0 % at a Landtag election. §18(3): a sum per list vote - one rate for its first four million votes, a lower one beyond -
    /// indexed yearly (§18(3) Satz 3 with §18(2)); Drucksache 21/160 (Bundestag, the claim year 2025) sets them at 1.21 € and 0.99 €. ⚠ What is NOT here:
    /// the 0.45 € per euro of donations and dues (§18(3) Nr. 3 - the game models no donations), the relative ceiling (§18(5): no more than the party's
    /// own income) and the absolute ceiling's pro-rata cut (§18(2)) - so the year's sum is the votes' part only, DERIVED and labelled so; and the sum
    /// is shown, not paid into the campaign's chest (owed with the money half of K165).
    /// </summary>
    public static class PartyFunding
    {
        public static bool Funds(CountryId country) => country == CountryId.Germany;

        /// <summary>PartG §18(4): the share of valid list votes at a Bundestag (or European) election that qualifies.</summary>
        public const double BundestagLine = 0.005;
        /// <summary>PartG §18(4): the share at a Landtag election.</summary>
        public const double LandtagLine = 0.010;
        /// <summary>PartG §18(3) Satz 2: the votes paid at the higher rate.</summary>
        public const long HigherRateVotes = 4_000_000;
        /// <summary>Drucksache 21/160 (the claim year 2025): the rate for the first four million votes, and beyond.</summary>
        public const double PerVoteFirst = 1.21;
        public const double PerVoteBeyond = 0.99;
        /// <summary>The claim year the two rates are set for; a later year is not on disk (the index's later steps are BILLED until fetched).</summary>
        public const int RatesYear = 2025;

        /// <summary>The year's sum for <paramref name="listVotes"/> valid list votes at a share of <paramref name="share"/> - 0 below the line.</summary>
        public static double YearSum(double share, long listVotes)
        {
            if (share < BundestagLine || listVotes <= 0) { return 0.0; }
            return Math.Min(listVotes, HigherRateVotes) * PerVoteFirst + Math.Max(0L, listVotes - HigherRateVotes) * PerVoteBeyond;
        }

        /// <summary>The valid list (Zweitstimme) votes a German count's share stands for - the share of the 2025 election's valid Zweitstimmen on disk
        /// (`GermanLandReturns2025`, kerg2.csv). DECLARED: the game's count records shares, not votes, so the latest valid total on disk is the base.</summary>
        public static long ListVotes(double share)
        {
            long valid = 0;
            foreach (long v in GermanLandReturns2025.Valid) { valid += v; }
            return (long)Math.Round(share * valid);
        }
    }
}
