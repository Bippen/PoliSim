using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// SP-7's design (`docs/specs/POLAND_PRESIDENTIAL_START_PLAN.md`, its ask 4 - the start date): THE MEASUREMENT A PRESIDENTIAL START'S DATE RIDES ON,
    /// for Elias, not a gate. A measuring instrument run once by name - it is in no bar:
    /// <c>-executeMethod PoliSim.EditorTools.MultiRun.Run -methods=PoliSim.EditorTools.PresidentialStartMeasure.Run</c>.
    ///
    /// <para><b>What it measures.</b> A fresh Polish world opened on each candidate start - the standard run-up before 18 May 2025 (§618's rule for a
    /// scheduled election, the rule <see cref="WorldClock.StartDate"/> applies), the Marshal's order of 15 January 2025 (Dz.U. 2025 poz. 48; the
    /// statute's campaign opens on its publication, Kodeks wyborczy art. 104), the committees' deadline of 24 March and the candidates' of 4 April 2025
    /// (the order's calendar - `ElectionsData/poland/presidential_calendar_2025.md`) - built as a new game builds one (the epoch set first, the world
    /// created on it), the player's party seated as the backing party of a candidate of record and left idle, and stepped by the play loop's simulation
    /// calls (`SimulationManager.AdvanceDay`, then the player's `SimulationManager.AdvanceCountryDayTick`, as `PlayerSmoke` and the film driver step it)
    /// through both rounds, each at two seeds (the model's own releases over a run-up carry seeded revision noise). Printed per world: the role the seat
    /// gives at the start, the government and the president of record, the poll on the first vote's day and the term reading it carries, the first round
    /// against the PKW's per candidate, the run-off and the verdict - so the drift a run-up of each length puts between a passive game and the record is
    /// a figure, not a guess. Also printed: what the game's campaign machinery offers a presidential start today (the next polling day it keys on, and
    /// whether a campaign is staged).</para>
    ///
    /// <para><b>The eve worlds check the wiring, nothing more.</b> On the eve the first round is held inside the one `AdvanceDay` stepped, before any
    /// call the seat can reach, and no vote path reads the player's party or its approval - so the eve rows equal the control by construction. Only the
    /// worlds opened earlier can show what an idle seat does over a run-up. And an idle seat is not the whole AI government: no turn boundary falls in
    /// any window, so the AI finance ministry (which acts in `SimulationManager.AdvanceTurn`) never acts; what differs by seat is the day tick's - the
    /// budget the AI government lays as a bill where the player does not govern (`SimulationManager.TryOpenBudgetProcess`) and the foreign-meeting
    /// rolls, which go to the government.</para>
    ///
    /// <para><b>What it fails on:</b> the control - the eve, no party seated, exactly E1's reference world - not reproducing the PKW's first round within
    /// <see cref="ControlTolerance"/> at either seed, the two seeds' controls differing, or an eve row with a seat differing from the control (the
    /// instrument would not be measuring what it says); a world that does not seat the government of record, or does not hold exactly one contest with
    /// its first vote on the record's day and a called run-off on its day; any exception; and - the log fold is armed (`CheckExit.ArmLogFold`) - any error the simulation logs while it steps, which `CheckExit.Finish`
    /// turns into a non-zero exit naming the count and the first line (under `MultiRun`, a FAILED row). The figures print either way. Everything else
    /// is PRINTED for the ask, never judged.</para>
    ///
    /// <para><b>Stated:</b> the controller's holds (a budget window, a cabinet decision, a meeting) are not answered - nothing holds the clock here, so an
    /// idle player is the loop with no answers given; no Sejm election falls inside any window measured, so nothing the controller alone holds is
    /// missed (§772's note on stepped worlds); each world opens on its seed and the process's streams are put back afterwards, as is the epoch.</para>
    /// </summary>
    public static class PresidentialStartMeasure
    {
        /// <summary>The control's bound on the first round against the PKW's, per candidate, in points - the live diagnostic's own (§772).</summary>
        private const double ControlTolerance = 0.005;

        /// <summary>The two seeds every measured world opens on - the trajectory sentinel's pair, so a re-run prints the same figures and a figure that
        /// moves with the seed shows it.</summary>
        private static readonly int[] MeasureSeeds = { 777, 424242 };

        /// <summary>[SOURCED] the Marshal of the Sejm's order of the 2025 election - Dz.U. 2025 poz. 48, announced and in force 2025-01-15
        /// (`ElectionsData/poland/presidential_calendar_2025.md` §1); Kodeks wyborczy art. 104 opens the campaign on its publication.</summary>
        private static readonly DateTime MarshalsOrder = new DateTime(2025, 1, 15);

        /// <summary>[SOURCED] the order's calendar: the committees' last day to notify the PKW (art. 299 § 4, the 55th day) and the candidates' last day to be
        /// filed with their signatures (the 44th day) - `ElectionsData/poland/presidential_calendar_2025.md` §2.</summary>
        private static readonly DateTime CommitteesNotified = new DateTime(2025, 3, 24), CandidatesFiled = new DateTime(2025, 4, 4);

        private sealed class Measured
        {
            public string Option, Seat;
            public int Seed;
            public DateTime Opens;
            public PresidentialElection.Contest Contest;
            public double WorstMiss = double.NaN;
            public string WorstName;
            public double NawrockiOfTwo = double.NaN;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PresidentialStartMeasure (SP-7's design, ask 4): a passive Polish world from each candidate start, through both rounds of 2025 ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            SimulationRandom.LiveState streams = SimulationRandom.CaptureLive();
            var rows = new List<Measured>();
            try
            {
                TwoRoundElection.RoundOfRecord(CountryId.Poland, 2025, 1, out DateTime firstVote);
                DateTime standard = new CampaignCalendar(firstVote).PreCampaignStart;   // §618: a scheduled election's start, as WorldClock.StartDate cuts it
                sb.Append(F("    the first vote of record {0:yyyy-MM-dd}; the standard run-up's first day {1:yyyy-MM-dd} ({2} days before it); the Marshal's order {3:yyyy-MM-dd} ({4} days before it)\n",
                    firstVote, standard, (firstVote - standard).Days, MarshalsOrder, (firstVote - MarshalsOrder).Days));

                // ---- ask 3: what the game's campaign machinery offers a presidential start today ----
                foreach (DateTime opens in new[] { standard, MarshalsOrder })
                {
                    bool next = WorldClock.TryNextPollingDay(CountryId.Poland, opens, out DateTime polling);
                    string window = next ? F("the next polling day the run-up keys on {0:yyyy-MM-dd} (its run-up from {1:yyyy-MM-dd})", polling, CampaignCalendar.FromWorldStart(polling, opens).PreCampaignStart) : "no polling day modelled";
                    bool staged = LiveCampaignSetup.TryFor(CountryId.Poland, new (int, int, Scandal)[0], next ? (CampaignCalendar?)new CampaignCalendar(polling) : null, out CampaignRun.Setup _, out string note);
                    sb.Append(F("    CAMPAIGN from {0:yyyy-MM-dd}: {1}; a campaign staged: {2}{3}\n", opens, window, staged ? "yes" : "no", staged ? string.Empty : " - " + note));
                }

                // ---- the worlds: the control at both seeds first, the eve's wiring rows, then each option with two seats at both seeds ----
                var plan = new List<(string Option, DateTime Opens, string Seat, int Seed)>();
                foreach (int seed in MeasureSeeds) { plan.Add(("the eve (control)", firstVote.AddDays(-1), null, seed)); }
                plan.Add(("the eve (wiring)", firstVote.AddDays(-1), "KO", MeasureSeeds[0]));
                plan.Add(("the eve (wiring)", firstVote.AddDays(-1), "PiS", MeasureSeeds[0]));
                foreach ((string option, DateTime opens) in new[] { ("the standard run-up", standard), ("the Marshal's order", MarshalsOrder), ("committees notified", CommitteesNotified), ("candidates filed", CandidatesFiled) })
                {
                    foreach (int seed in MeasureSeeds) { foreach (string seat in new[] { "KO", "PiS" }) { plan.Add((option, opens, seat, seed)); } }
                }
                foreach ((string option, DateTime opens, string seat, int seed) in plan)
                {
                    Measured m = Hold(option, opens, seat, seed, firstVote, sb, Check);
                    rows.Add(m);
                    if (seat == null)
                    {
                        Check(m.Contest != null && m.WorstMiss < ControlTolerance,
                            F("the control at seed {0} - the eve, no party seated, E1's reference world - reproduces the PKW's first round: the largest miss {1:0.0000} pp ({2})", seed, m.WorstMiss, m.WorstName ?? "none"));
                    }
                }
                List<Measured> controls = rows.Where(r => r.Seat == null).ToList();
                bool sameControls = controls.Count == MeasureSeeds.Length && controls.All(c => c.Contest != null) && controls.Skip(1).All(c => c.Contest.Field.Count == controls[0].Contest.Field.Count
                    && c.Contest.Field.Zip(controls[0].Contest.Field, (x, y) => x.Name == y.Name && x.Share == y.Share).All(same => same) && c.Contest.RunOffShareA == controls[0].Contest.RunOffShareA);
                Check(sameControls, "the control does not move with the seed - both seeds' first rounds and run-offs are identical, so the eve's poll reads nothing seeded and the bar's unseeded reference world is the same world");
                List<Measured> wiring = rows.Where(r => r.Option == "the eve (wiring)").ToList();
                bool wired = controls.Count > 0 && controls[0].Contest != null && wiring.Count == 2 && wiring.All(w => w.Contest != null && w.Contest.Field.Count == controls[0].Contest.Field.Count
                    && w.Contest.Field.Zip(controls[0].Contest.Field, (x, y) => x.Name == y.Name && x.Share == y.Share).All(same => same));
                Check(wired, "the wiring: the eve seated KO and seated PiS hold the control's first round exactly - on the eve the round is held before anything a seat reaches, so these rows prove the stepping, not the seat");

                // ---- the table the ask reads ----
                sb.Append("    SUMMARY (option, seat, seed: opens, days run, first round's largest miss against the PKW, the verdict, Nawrocki's share of the two):\n");
                foreach (Measured m in rows)
                {
                    sb.Append(F("      {0,-20} {1,-5} {2,6} {3:yyyy-MM-dd} {4,4} d  miss {5,6:0.00} pp ({6})  {7}  Nawrocki {8:0.00} %\n", m.Option, m.Seat ?? "none", m.Seed, m.Opens, (firstVote - m.Opens).Days,
                        m.WorstMiss, m.WorstName ?? "-", m.Contest?.Elected == null ? "NOTHING DECIDED" : m.Contest.Elected.ToUpperInvariant() + " ELECTED", m.NawrockiOfTwo));
                }
                var runOff = PoliSim.Elections.Generated.PolishPresidentialReturns.Votes.Where(v => v.Year == 2025 && v.Round == 2).ToList();
                double ofTwo = 100.0 * runOff.Where(v => v.Candidate.StartsWith("NAWROCKI ", StringComparison.Ordinal)).Sum(v => (double)v.Votes) / runOff.Sum(v => (double)v.Votes);
                sb.Append(F("      the record: Nawrocki elected with {0:0.00} % of the two (the PKW's run-off notice, Dz.U. 2025 poz. 714, as generated)\n", ofTwo));
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }
            finally { SimulationRandom.Reinstate(streams); }

            sb.Append(failures == 0 ? "=== PresidentialStartMeasure: measured - the figures above are for the ask, not a verdict ===" : F("=== PresidentialStartMeasure: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>One world: opened on <paramref name="opens"/> at <paramref name="seed"/> as a new game opens one, the seat given, stepped through both rounds; printed into <paramref name="sb"/>.</summary>
        private static Measured Hold(string option, DateTime opens, string seat, int seed, DateTime firstVote, StringBuilder sb, Action<bool, string> check)
        {
            var m = new Measured { Option = option, Seat = seat, Seed = seed, Opens = opens };
            var host = new GameObject("PresidentialStartMeasure");
            var watch = Stopwatch.StartNew();
            try
            {
                using (SimulationManager.EpochScope())
                {
                    SimulationRandom.Seed(seed);
                    SimulationManager.SetEpoch(opens);   // before the world exists - SetEpoch's contract, and a new game's order
                    World world = WorldFactory.CreateDefault();
                    Country pl = world.GetCountry(CountryId.Poland);
                    var sim = host.AddComponent<SimulationManager>();
                    sim.SetWorld(world);
                    sim.PlayerCountryId = CountryId.Poland;
                    if (seat != null) { pl.PlayerPartyAbbrev = seat; pl.PartyApprovalRating = pl.State.ApprovalRating; }   // as the picker seats a party

                    string role = seat == null ? "none seated (the instrument's hand: PlayerGoverns reads an empty party as the tools')" : pl.Government.RoleOf(seat).ToString();
                    string president = PresidencyOfRecord.TryAt(CountryId.Poland, opens, out PresidencyOfRecord.President p) ? p.Name : "none of record";
                    bool recordGovernment = WorldClock.TryGovernmentAt(CountryId.Poland, opens, out WorldClock.GovernmentOfRecord g)
                        && pl.Government != null && pl.Government.PmParty == "KO" && g.Cabinet.All(k => pl.Government.Cabinet.Contains(k)) && pl.Government.Cabinet.Count == g.Cabinet.Length;
                    sb.Append(F("    MEASURE {0} · seat {1} · seed {9}: opens {2:yyyy-MM-dd}, {3} days to the first vote - role {4}; government {5} {6}; president of record {7}; chamber {8}\n",
                        option, seat ?? "none", opens, (firstVote - opens).Days, role, pl.Government?.PmParty ?? "none", string.Join("+", pl.Government?.Cabinet ?? new List<string>()),
                        president, WorldClock.SeatedVintage(CountryId.Poland, opens), seed));
                    check(recordGovernment, F("{0} · {1}: the world seats the government of record on {2:yyyy-MM-dd} ({3})", option, seat ?? "none", opens, g.Head ?? "none of record"));

                    var none = new Dictionary<CountryId, PolicyDecision>();
                    int boundaries = 0;
                    while (sim.CurrentDate < firstVote.AddDays(-1))
                    {
                        if (sim.AdvanceDay()) { boundaries++; sim.AdvanceTurn(none); }
                        sim.AdvanceCountryDayTick(CountryId.Poland);
                    }
                    if (sim.AdvanceDay()) { boundaries++; sim.AdvanceTurn(none); }   // the first vote's day - the hook holds the first round inside it
                    NationalElection.TryPredictShares(CountryId.Poland, out Dictionary<string, double> poll, EconomicVote.RecordOverTerm(pl, sim.CurrentDate, out PerceivedPerformance.TermReading term), on: sim.CurrentDate);
                    sim.AdvanceCountryDayTick(CountryId.Poland);
                    m.Contest = pl.PresidentialElections.Count == 1 ? pl.PresidentialElections[0] : null;
                    if (m.Contest != null && m.Contest.RunOffPending())
                    {
                        while (sim.CurrentDate < m.Contest.RunOffOn)
                        {
                            if (sim.AdvanceDay()) { boundaries++; sim.AdvanceTurn(none); }
                            sim.AdvanceCountryDayTick(CountryId.Poland);
                        }
                    }
                    watch.Stop();

                    sb.Append(F("      the poll on {0:yyyy-MM-dd}: {1}\n", firstVote, poll == null ? "none could be made"
                        : string.Join(", ", new[] { "PiS", "KO", "Konf", "TD", "NL" }.Select(k => F("{0} {1:0.00} %", k, poll.TryGetValue(k, out double s) ? 100.0 * s : double.NaN)))));
                    sb.Append("      the term it carries: ").Append(EconomicVote.Describe(term)).Append('\n');
                    sb.Append(F("      the government on the first vote's day: {0} {1}; {2} turn boundar(ies) crossed; {3:0.0} s\n", pl.Government?.PmParty ?? "none",
                        string.Join("+", pl.Government?.Cabinet ?? new List<string>()), boundaries, watch.Elapsed.TotalSeconds));
                    bool once = m.Contest != null && m.Contest.FirstVote == firstVote && (m.Contest.Decided() || m.Contest.RunOffHeld) && (m.Contest.RunOffA == null || m.Contest.RunOffOn == firstVote.AddDays(14));
                    check(once, F("{0} · {1}: one contest held, its first vote on {2:yyyy-MM-dd}{3} - {4} contest(s)", option, seat ?? "none", firstVote,
                        m.Contest?.RunOffA != null ? F(" and its run-off on {0:yyyy-MM-dd}", m.Contest.RunOffOn) : string.Empty, pl.PresidentialElections.Count));
                    if (m.Contest == null) { return m; }

                    m.WorstMiss = 0.0;
                    foreach (PresidencyOfRecord.CandidateOfRecord c in PresidencyOfRecord.CandidatesOf(CountryId.Poland, 2025))
                    {
                        string name = PresidencyOfRecord.NameOfRecord(CountryId.Poland, 2025, c.Surname);
                        double record = 100.0 * PresidencyOfRecord.ShareOfRecord(CountryId.Poland, 2025, c.Surname);
                        PresidentialElection.Candidate f = m.Contest.Field.FirstOrDefault(x => x.Name == name);
                        double share = f?.Share ?? 0.0;
                        double miss = share - record;
                        if (Math.Abs(miss) > m.WorstMiss) { m.WorstMiss = Math.Abs(miss); m.WorstName = name; }
                        sb.Append(F("      {0,-22} {1,-5} {2,6:0.00} %   record {3,6:0.00} %   {4,6:+0.00;-0.00} pp\n", name, c.BackingParty ?? "-", share, record, miss));
                    }
                    foreach (string n in m.Contest.NotStanding) { sb.Append("      not standing: ").Append(n).Append('\n'); }
                    sb.Append("      ").Append(m.Contest.Line).Append('\n');
                    if (m.Contest.RunOffHeld && m.Contest.RunOffA != null)
                    {
                        string nawrocki = PresidencyOfRecord.NameOfRecord(CountryId.Poland, 2025, "NAWROCKI");
                        m.NawrockiOfTwo = m.Contest.RunOffA == nawrocki ? m.Contest.RunOffShareA : m.Contest.RunOffB == nawrocki ? 100.0 - m.Contest.RunOffShareA : double.NaN;
                    }
                }
            }
            catch (Exception ex) { check(false, F("{0} · {1}: threw - {2}", option, seat ?? "none", ex)); }
            finally { UnityEngine.Object.DestroyImmediate(host); }
            return m;
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
