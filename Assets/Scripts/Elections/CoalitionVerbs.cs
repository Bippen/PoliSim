using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>COAL-2: the coalition verbs of Design's one grammar (`S9 Coalition Verbs`, RETURN of 2026-10-30) - who does what to whom.</summary>
    public enum CoalitionVerb
    {
        /// <summary>A junior partner threatens to leave unless its demand is met by a deadline (the spec's §5.2, "threaten to leave").</summary>
        Threaten,
        /// <summary>A partner leaves the cabinet - the player's verb (K197) or an AI partner's exit at a budget vote (K130).</summary>
        Leave,
        /// <summary>The head of government's party dismisses a partner (the spec's §5.2, "the prime minister's party dismisses the partner").</summary>
        Dismiss,
        /// <summary>A support party calls an owed item of its agreement due by a deadline (the spec's §5.2, "enforce the agreement").</summary>
        Enforce,
        /// <summary>A party re-declares its bloc after a count (q19, K137).</summary>
        Redeclare,
    }

    /// <summary>COAL-2: a motion's state - open (a threat or an enforcement before its day), or how it was settled.</summary>
    public enum MotionState
    {
        Open,
        /// <summary>The demand was delivered by the deadline: a threat struck (MET · STAYS), an enforcement met (support goes on).</summary>
        Met,
        /// <summary>Done: a threat carried out (the party left), a leaving, a dismissal, a re-declaration.</summary>
        Carried,
        /// <summary>A threat or an enforcement taken back by its actor, or lapsed with the government it was made to.</summary>
        Withdrawn,
        /// <summary>An enforcement the government did not deliver by its deadline: the item is broken that day.</summary>
        Refused,
    }

    /// <summary>K137: the side a party re-declares - for the government the count formed (in it or behind it), against it, or neither.</summary>
    public enum BlocSide { Neither, For, Against }

    /// <summary>
    /// COAL-2 (Design's "For Code" 1): ONE MOTION - the actor, the verb, the target, the demand, the deadline and the state. Kept on the country
    /// (<see cref="Country.CoalitionMotions"/>), oldest first, as its record of the coalition's verbs: the open ones settle by the day loop
    /// (`SimulationManager.AdvanceCoalitionVerbsDay`), the settled ones are what the Docket and the strip read. Saved with the country (additive: an older
    /// save holds none).
    /// </summary>
    public sealed class CoalitionMotion
    {
        public CoalitionVerb Verb;
        /// <summary>The party acting, by key.</summary>
        public string Actor;
        /// <summary>The party acted on, by key: the head of government's party for a threat and an enforcement, the dismissed partner, the government's head
        /// for a re-declaration (the government declared for or against); null for a leaving.</summary>
        public string Target;
        /// <summary>A threat's demand and an enforcement's item by their stable key (`SupportAgreement.KeyOf`: "law:&lt;id&gt;" or "dial:&lt;dial&gt;");
        /// a re-declaration's side ("FOR", "AGAINST", "NEITHER").</summary>
        public string Demand;
        /// <summary>The demand in the catalogue's words (a law's plain name, a dial and its target).</summary>
        public string DemandName;
        /// <summary>A dial demand's target level (the agreement item's own).</summary>
        public float DialTarget;
        public DateTime On;
        /// <summary>The day a threat or an enforcement falls due; MinValue for a verb with none.</summary>
        public DateTime Deadline = DateTime.MinValue;
        public MotionState State;
        public DateTime SettledOn = DateTime.MinValue;
        /// <summary>True where the player's verb made it; false where the AI's rule did (K130's exit, K137's re-declaration).</summary>
        public bool ByPlayer;
        /// <summary>A re-declaration's bloc in Sweden's terms (K53: 0 the side of S's candidacy, 1 the Tidö side, -1 neither) - what `NationalElection.BlocOf`
        /// reads from <see cref="On"/>; -1 for every other verb and every other country.</summary>
        public int Bloc = -1;
        /// <summary>How it settled, in one line for the record (the government's breaks carry the same day's words).</summary>
        public string Outcome;
        public string Basis;
        /// <summary>The review's M1: the government the motion was made to (<see cref="CoalitionVerbs.GovernmentKey"/>) - an open threat or demand lapses once
        /// another government governs, so it is never carried out against, nor breaks an item of, a government it was not made to.</summary>
        public string Government;

        public bool IsOpen => State == MotionState.Open;

        /// <summary>The review's L5: a copy of every field - the preview's clone holds its own motions, so nothing a preview did could settle the played world's.</summary>
        public CoalitionMotion Copy() => (CoalitionMotion)MemberwiseClone();
    }

    /// <summary>
    /// COAL-2 (RULINGS_2026-10-18 q1, q19; Elias's K136 and K137 of 2026-10-27; the political-system spec's §5.2): <b>THE COALITION VERBS' RULES</b>, as pure
    /// functions of the country and the day - the manager's verbs (`SimulationManager.CoalitionVerbs.cs`), the desk and the checks read the same ones.
    /// <list type="bullet">
    /// <item><description><b>A threat</b> is a demand with a deadline; nothing changes until the day it is met, carried out or withdrawn (Design's level 2). Its
    /// demand is one of the threatener's own demands as the spec's §5.3 chooses a supporter's - from its own positions, the lead's compatibility allowing it
    /// (<see cref="DemandsOf"/>, `SupportAgreement.Demand`) - and it is met when delivered by the agreement's own test (<see cref="Delivered"/>). DECLARED
    /// (Code's reading): unmet on its deadline it is carried out - the threatener leaves by the leaving's own path.</description></item>
    /// <item><description><b>A dismissal</b> is a partner's exit the head makes: q21's "a partner's exit opens the country's confidence procedure" reads it as
    /// it reads a leaving (K131, K132).</description></item>
    /// <item><description><b>An enforcement</b> calls an owed item of the supporter's agreement due by a deadline; DECLARED: delivered by then it is met, else
    /// it is refused and the item breaks that day - the supporter may then withdraw (§635's verb).</description></item>
    /// <item><description><b>A re-declaration</b> (K137 as K330 narrows it, Code's recommendation under the standing instruction, on the COAL-2 review's H2): after
    /// each count, toward the government the one formation code forms (K119, q20), a party in its cabinet or its recorded support declares FOR it; a party
    /// outside both declares AGAINST it only where its own red lines exclude it from that cabinet (`GovernmentFormation.RedLinedFrom`); every other party
    /// re-declares nothing and keeps its standing bloc (<see cref="SideAfterCount"/>) - being outside a cabinet is not a side. It stands until the next
    /// count's or a later dated side of record; Sweden's blocs read it as K331 maps it (<see cref="BlocOfSide"/>, <see cref="TryRedeclaredBloc"/>).</description></item>
    /// </list>
    /// </summary>
    public static class CoalitionVerbs
    {
        /// <summary>K133: the countries whose coalition verbs are modelled - COAL-1's three (Sweden, Germany, Poland); Italy's coalitions are fixed by K152.</summary>
        public static bool Applies(CountryId country) => country == CountryId.Sweden || country == CountryId.Germany || country == CountryId.Poland;

        /// <summary>The deadlines a threat or an enforcement may name, in days [AUTHORED-DRAFT] - the player's choice (Design: "Pick a demand and a
        /// deadline"); the spec gives no figure, so the choices are a premise, stated, on the play-calibration list.</summary>
        public static readonly int[] DeadlineDays = { 30, 60, 90 };

        /// <summary>The Docket lists a settled verb this many days, as K197 lists the Art. 158 votes (its month).</summary>
        public const int ListedDays = 31;

        /// <summary>The words a side prints.</summary>
        public static string SideWord(BlocSide side) => side == BlocSide.For ? "FOR" : side == BlocSide.Against ? "AGAINST" : "NEITHER";

        public static BlocSide ParseSide(string word) => word == "FOR" ? BlocSide.For : word == "AGAINST" ? BlocSide.Against : BlocSide.Neither;

        /// <summary>
        /// K330 (narrowing K137): the side a party re-declares toward <paramref name="g"/> after a count - FOR where it sits in the cabinet or its recorded
        /// support; AGAINST where it is outside both and its own red lines exclude it from that cabinet (<paramref name="redLined"/>, the cabinet's
        /// `GovernmentFormation.RedLinedFrom` on the day); null for every other party, which re-declares nothing and keeps its standing bloc.
        /// </summary>
        public static BlocSide? SideAfterCount(GovernmentRecord g, string party, ICollection<string> redLined)
        {
            if (g == null || string.IsNullOrEmpty(party)) { return null; }
            if (g.Cabinet.Contains(party) || g.Support.Contains(party)) { return BlocSide.For; }
            if (redLined != null && redLined.Contains(party)) { return BlocSide.Against; }
            return null;
        }

        /// <summary>
        /// K331 (Code's recommendation under Elias's standing instruction; the COAL-2 second review's N4): a K330 side in Sweden's terms (K53's blocs). FOR takes
        /// the bloc of the government head's party standing that day (<paramref name="governmentBloc"/>). AGAINST never moves a party onto the other bloc: a party
        /// declaring AGAINST whose standing bloc is the head's leaves it and stands in no bloc (-1); any other keeps <paramref name="standing"/>. Where the head
        /// stands in no bloc, every side keeps <paramref name="standing"/>. NEITHER, the player's alone, is -1. Being red-lined out of a cabinet takes a party
        /// out of that camp, not into the rival one (the second review's probe had SD on S's side under M+C+KD, through the two-way C-SD line).
        /// </summary>
        public static int BlocOfSide(BlocSide side, int governmentBloc, int standing)
        {
            if (side == BlocSide.Neither) { return -1; }
            if (governmentBloc < 0 || governmentBloc > 1) { return standing; }
            if (side == BlocSide.For) { return governmentBloc; }
            return standing == governmentBloc ? -1 : standing;
        }

        /// <summary>
        /// K137 (read by `NationalElection.BlocOf` on a country and its tactical layer): the bloc a party re-declared standing on <paramref name="on"/> - the
        /// latest re-declaration made on or before that day, in Sweden only (the one chamber whose blocs the model reads, K53, K79). The review's L1 (§621, q19:
        /// a declaration stands until a later dated one replaces it): a dated side of record standing that day whose own date is later than the re-declaration
        /// replaces it. False where none stands, or the record's is the later: the dated sides of record then answer.
        /// </summary>
        public static bool TryRedeclaredBloc(Country country, string party, DateTime on, out int bloc)
        {
            bloc = -1;
            if (country == null || country.Id != CountryId.Sweden || country.CoalitionMotions == null || country.CoalitionMotions.Count == 0 || string.IsNullOrEmpty(party)) { return false; }
            CoalitionMotion latest = null;
            foreach (CoalitionMotion m in country.CoalitionMotions)
            {
                if (m.Verb != CoalitionVerb.Redeclare || m.Actor != party || m.On.Date > on.Date) { continue; }
                if (latest == null || m.On >= latest.On) { latest = m; }
            }
            if (latest == null) { return false; }
            foreach (NationalElection.DatedSide side in NationalElection.SwedenSides)
            {
                if (side.Party == party && side.StandsOn(on) && side.From.Date > latest.On.Date) { return false; }
            }
            bloc = latest.Bloc;
            return true;
        }

        /// <summary>
        /// The review's M1: a government's identity as a motion keeps it, rebuilt from saved fields alone - the day the record was formed, its head's party, the
        /// day it was last appointed again (`GovernmentRecord.ReappointedOn`: a cabinet the President appoints again keeps its record and its formation day,
        /// `GovernmentRecord.Reappoint`) and whether it serves as a caretaker (a discharged government is not the one a threat was made to). A record a count
        /// re-forms, even of the same parties, is another. The second review's N2: it changes only when the government does - a resignation, an appointment -
        /// never on a polling night (Poland's Sejm procedure is written onto the outgoing record then, weeks before its cabinet resigns at the first sitting).
        /// </summary>
        public static string GovernmentKey(GovernmentRecord g) => g == null ? null : string.Format(CultureInfo.InvariantCulture, "{0:yyyyMMdd}|{1}|{2}|{3}",
            g.FormedOn, g.PmParty, g.ReappointedOn == DateTime.MinValue ? "-" : g.ReappointedOn.ToString("yyyyMMdd", CultureInfo.InvariantCulture), g.Caretaker ? "caretaker" : "governing");

        /// <summary>The open motion of <paramref name="verb"/> by <paramref name="actor"/>, or null.</summary>
        public static CoalitionMotion OpenOf(Country country, CoalitionVerb verb, string actor)
        {
            if (country?.CoalitionMotions == null) { return null; }
            foreach (CoalitionMotion m in country.CoalitionMotions) { if (m.IsOpen && m.Verb == verb && m.Actor == actor) { return m; } }
            return null;
        }

        /// <summary>The latest motion of <paramref name="verb"/> by <paramref name="actor"/>, open or settled, or null.</summary>
        public static CoalitionMotion LatestOf(Country country, CoalitionVerb verb, string actor)
        {
            if (country?.CoalitionMotions == null) { return null; }
            CoalitionMotion latest = null;
            foreach (CoalitionMotion m in country.CoalitionMotions) { if (m.Verb == verb && m.Actor == actor && (latest == null || m.On >= latest.On)) { latest = m; } }
            return latest;
        }

        /// <summary>What the Docket lists on <paramref name="today"/>: every open motion, and every motion settled (or made settled) within <see cref="ListedDays"/>,
        /// newest first.</summary>
        public static List<CoalitionMotion> Listed(Country country, DateTime today)
        {
            var listed = new List<CoalitionMotion>();
            if (country?.CoalitionMotions == null) { return listed; }
            foreach (CoalitionMotion m in country.CoalitionMotions)
            {
                DateTime last = m.IsOpen ? today : (m.SettledOn != DateTime.MinValue ? m.SettledOn : m.On);
                if (m.IsOpen || (last.Date <= today.Date && (today.Date - last.Date).TotalDays <= ListedDays)) { listed.Add(m); }
            }
            listed.Sort((a, b) => (b.SettledOn != DateTime.MinValue ? b.SettledOn : b.On).CompareTo(a.SettledOn != DateTime.MinValue ? a.SettledOn : a.On));
            return listed;
        }

        /// <summary>The motions settled (or made, for a verb that is done when made) on <paramref name="today"/> - the strip's place 3 for its one day.</summary>
        public static List<CoalitionMotion> SettledOn(Country country, DateTime today)
        {
            var on = new List<CoalitionMotion>();
            if (country?.CoalitionMotions == null) { return on; }
            foreach (CoalitionMotion m in country.CoalitionMotions)
            {
                if (m.IsOpen || m.SettledOn.Date != today.Date) { continue; }
                on.Add(m);
            }
            return on;
        }

        /// <summary>
        /// The threatener's demands (the spec's §5.3 as a supporter's are chosen): the laws within competence not yet enacted and one dial target it wants
        /// moved, scored on its own positions, the lead's compatibility allowing each (`SupportAgreement.Demand`, the stance model the chamber votes with).
        /// The template is keyed on the government's own formation day, so the list is scored again only when the chamber, the enacted laws or the dials move.
        /// </summary>
        public static List<AgreementItem> DemandsOf(Country country, string actor, string lead, DateTime formedOn, World world)
        {
            var demands = new List<AgreementItem>();
            if (country == null || string.IsNullOrEmpty(actor)) { return demands; }
            SupportAgreement scored = SupportAgreement.Demand(country, actor, lead, formedOn, world);
            if (scored != null) { demands.AddRange(scored.Items); }
            return demands;
        }

        /// <summary>COAL-2: the government's record line a dismissal writes (here, beside the verbs' other pure readers, since §966: as a public static
        /// of the manager's partial file it was that file's one entry point and the diagnostic its only outside caller - UnwiredSubsystemCheck's GAP).</summary>
        public static string DismissedLine(DateTime day, string head, string partner) => $"{day:yyyy-MM-dd}: {head} dismissed {partner} from the government (COAL-2)";

        /// <summary>The agreement's items still owed - an enforcement's choices.</summary>
        public static List<AgreementItem> OwedOf(SupportAgreement agreement)
        {
            var owed = new List<AgreementItem>();
            if (agreement == null) { return owed; }
            foreach (AgreementItem item in agreement.Items) { if (item.State == AgreementState.Owed) { owed.Add(item); } }
            return owed;
        }

        /// <summary>The agreement item held under <paramref name="key"/> (`SupportAgreement.KeyOf`), or null.</summary>
        public static AgreementItem ItemOf(SupportAgreement agreement, string key)
        {
            if (agreement == null || key == null) { return null; }
            foreach (AgreementItem item in agreement.Items) { if (SupportAgreement.KeyOf(item) == key) { return item; } }
            return null;
        }

        /// <summary>Whether a threat's or an enforcement's demand is delivered on the country as it stands - the agreement's own test (`SupportAgreement.Track`):
        /// a law enacted, a dial at its target.</summary>
        public static bool Delivered(Country country, CoalitionMotion m)
        {
            if (country == null || m == null || string.IsNullOrEmpty(m.Demand)) { return false; }
            if (m.Demand.StartsWith("law:", StringComparison.Ordinal))
            {
                string id = m.Demand.Substring(4);
                return country.EnactedLaws.Exists(e => e.LawId == id);
            }
            if (m.Demand.StartsWith("dial:", StringComparison.Ordinal) && Enum.TryParse(m.Demand.Substring(5), out AgreementDial dial))
            {
                return SupportAgreement.DialValue(country, dial) >= m.DialTarget;
            }
            return false;
        }

        /// <summary>The day of the latest parliamentary count the game held in the country on or before <paramref name="today"/>; MinValue for none.</summary>
        public static DateTime LatestCount(Country country, DateTime today)
        {
            DateTime latest = DateTime.MinValue;
            if (country?.ElectionHistory == null) { return latest; }
            foreach (ElectionRecord held in country.ElectionHistory)
            {
                if (!NationalElection.CountsParliament(held.Method) || held.Date.Date > today.Date) { continue; }
                if (held.Date > latest) { latest = held.Date; }
            }
            return latest;
        }

        /// <summary>K137: whether <paramref name="g"/> is the government a count formed - installed on or after the count's day, its procedure concluded, not a
        /// caretaker or the formation's stand-in.</summary>
        public static bool FormedAfter(GovernmentRecord g, DateTime count)
        {
            if (g == null || count == DateTime.MinValue || g.Cabinet.Count == 0) { return false; }
            if (g.Caretaker || g.Provisional) { return false; }
            if (g.Round != null && g.Round.Open) { return false; }
            if (g.Sejm != null && g.Sejm.Open) { return false; }
            // AF-8: a cabinet the President appoints again under Art. 154 ust. 1 keeps its record and its day (`GovernmentRecord.Reappoint`) - the Sejm's
            // procedure for this count, concluded, is what says it was formed after it
            if (g.Sejm != null && g.Sejm.PollingDay.Date == count.Date) { return true; }
            return g.FormedOn.Date >= count.Date;
        }

        /// <summary>The chamber as a verb would leave it (Design's "For Code" 2: the cabinet's seats with the actor or the target removed): the seats in the
        /// cabinet and behind it without <paramref name="without"/>, of the chamber, and the line that keeps a government - the majority of the size the
        /// seated chamber declares (Poland's 231 of the statutory 460).</summary>
        public static (int Governing, int Members, int Line) ChamberWithout(Country country, GovernmentRecord g, string without, DateTime date)
        {
            int governing = 0, members = 0;
            if (country?.ParliamentSeats == null || g == null) { return (0, 0, 0); }
            foreach (KeyValuePair<string, int> seat in country.ParliamentSeats)
            {
                if (seat.Value <= 0) { continue; }
                members += seat.Value;
                if (seat.Key != without && (g.Cabinet.Contains(seat.Key) || g.Support.Contains(seat.Key))) { governing += seat.Value; }
            }
            int declared = PartySystems.DeclaredSize(country, date);
            int size = declared > 0 ? declared : members;
            return (governing, members, size / 2 + 1);
        }

        /// <summary>The standing a verb left, at rest on the Docket's line: "KO · TD · 230 of 460 · a minority" (the cabinet in short names, the seats in or
        /// behind it of the chamber, and whether they hold a majority of it).</summary>
        public static string StandingLine(Country country)
        {
            GovernmentRecord g = country?.Government;
            if (g == null || country.ParliamentSeats == null) { return string.Empty; }
            int total = 0, held = 0;
            foreach (KeyValuePair<string, int> seat in country.ParliamentSeats)
            {
                total += seat.Value;
                if (g.Cabinet.Contains(seat.Key) || g.Support.Contains(seat.Key)) { held += seat.Value; }
            }
            var names = new List<string>();
            foreach (string k in g.Cabinet) { names.Add(PartySystems.ShortName(country.Id, k) ?? k); }
            return PartySystems.JoinShortNames(names) + string.Format(CultureInfo.InvariantCulture, " · {0} of {1} · ", held, total) + (held * 2 > total ? "a majority" : "a minority");
        }

        /// <summary>A day as the verbs print it - "12 Nov".</summary>
        public static string ShortDay(DateTime d) => d.ToString("d MMM", CultureInfo.InvariantCulture);
    }
}
