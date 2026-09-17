using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.UI
{
    /// <summary>
    /// **PF-1 (ruled 2026-09-17, `COMPLETED.md` §525): the chamber's answer to a bill, asked once and not on every draw.**
    ///
    /// <para><b>The defect.</b> The screens asked the parliament how a bill would vote inside OnGUI - every tax line's and
    /// welfare program's verdict, the live estimate's count, seat map and breakdown, the laws page, the pending cards -
    /// and every one of those questions ran `StanceModel.Stances`, which forms the government
    /// (`GovernmentFormation.TryGovernment`, a coalition search over the chamber's parties) each time. Measured on the
    /// default world: 90 ms a question for France's fifteen parties, 35 ms for Italy's fourteen, 0.3 ms for Sweden's
    /// eight; France's Budget tab drew at 2-4 s a pass.</para>
    ///
    /// <para><b>The key is what the answer reads.</b> The stance model reads the country, the player's party, the
    /// chamber's seats and the bill's concern; everything else it reads (the party positions, the declared red lines,
    /// the voter profiles, the salience) is seeded data. So an entry lives until the country, the player's party or any
    /// party's seats change - the whole cache is dropped then - and it is keyed by the concern itself, which is how a
    /// DRAFT reaches it: a draft change is a different concern, so the answers a draft feeds are asked again while the
    /// per-line verdicts, which no draft reads, stay.</para>
    ///
    /// <para>⚠ <b>The cache must not drift from the model, and that is asserted, not assumed.</b> With
    /// <see cref="VerifyHits"/> on - every film and dry film turns it on for each captured frame, and
    /// `ChamberVerdictCacheCheck` drives it through seat, party and draft changes - every hit is recomputed uncached
    /// and compared party by party (seats, side, alignment to the bit, measured, the reasons) and verdict for verdict.
    /// A difference is logged as a drift, which fails the run, and the fresh answer replaces the stale one. A key that
    /// misses an input the model starts reading is caught the first time that input moves under a film.</para>
    /// </summary>
    public sealed class ChamberVerdicts
    {
        /// <summary>When true, every cache hit is recomputed uncached and compared (see the class note).</summary>
        public static bool VerifyHits;

        /// <summary>Drifts found since the process started - the check and the films read it.</summary>
        public static int Drifts { get; private set; }

        /// <summary>Milliseconds spent verifying hits since the process started, so a pass time can be read without it.</summary>
        public static double VerifyMilliseconds { get; private set; }

        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public int Invalidations { get; private set; }

        private readonly Dictionary<string, List<PartyStance>> _stances = new Dictionary<string, List<PartyStance>>();
        private readonly Dictionary<string, bool> _verdicts = new Dictionary<string, bool>();
        private string _chamberKey;

        /// <summary>Every party's stance on <paramref name="concern"/> - `StanceModel.Stances`, asked once per chamber and concern.</summary>
        public IReadOnlyList<PartyStance> Stances(Country country, BillConcern concern)
        {
            Bind(country);
            string key = ConcernKey(concern);
            if (_stances.TryGetValue(key, out List<PartyStance> cached))
            {
                Hits++;
                if (VerifyHits) { cached = VerifyStances(country, concern, key, cached); }
                return cached;
            }

            Misses++;
            List<PartyStance> fresh = StanceModel.Stances(country, concern);
            _stances[key] = fresh;
            return fresh;
        }

        /// <summary>`ParliamentSystem.SeatSides(country, concern)` over the cached stances - the same tuple, the same order.</summary>
        public IEnumerable<(PoliticalParty Party, int Seats, int Side, float Weight, bool Measured)> SeatSides(Country country, BillConcern concern)
        {
            foreach (PartyStance stance in Stances(country, concern))
            {
                yield return (stance.Party, stance.Seats, stance.Side, stance.Alignment, stance.Measured);
            }
        }

        /// <summary>`ParliamentSystem.WouldBillPass(country, concern)`, asked once per chamber and concern.</summary>
        public bool WouldPass(Country country, BillConcern concern)
        {
            if (concern == null || concern.IsEmpty) { return ParliamentSystem.WouldBillPass(country, concern); }   // uncontested: the model answers without asking the chamber
            Bind(country);
            string key = ConcernKey(concern);
            if (_verdicts.TryGetValue(key, out bool cached))
            {
                Hits++;
                if (VerifyHits) { cached = VerifyVerdict(country, concern, key, cached); }
                return cached;
            }

            Misses++;
            bool fresh = ParliamentSystem.WouldBillPass(country, concern);
            _verdicts[key] = fresh;
            return fresh;
        }

        /// <summary>`ParliamentSystem.WouldBillPass(country, direction, axis)` - a program bill's scalar direction, through the concern it stands for.</summary>
        public bool WouldPass(Country country, float direction, BillAxis axis = BillAxis.Fiscal)
        {
            if (Mathf.Approximately(direction, 0f)) { return ParliamentSystem.WouldBillPass(country, direction, axis); }   // a zero direction asks the chamber for nothing
            return WouldPass(country, BillConcern.FromLegacy(direction, axis));
        }

        /// <summary>Drops every entry when the chamber the answers were asked of is not this one.</summary>
        private void Bind(Country country)
        {
            string key = ChamberKey(country);
            if (key == _chamberKey) { return; }
            if (_chamberKey != null) { Invalidations++; }
            _chamberKey = key;
            _stances.Clear();
            _verdicts.Clear();
        }

        /// <summary>The country, the player's party and every party's seats, in ordinal order.</summary>
        private static string ChamberKey(Country country)
        {
            var sb = new StringBuilder(256);
            sb.Append(country.Id).Append('|').Append(country.PlayerPartyAbbrev).Append('|');
            var parties = new List<string>(country.ParliamentSeats.Keys);
            parties.Sort(System.StringComparer.Ordinal);
            foreach (string party in parties) { sb.Append(party).Append('=').Append(country.ParliamentSeats[party]).Append(';'); }
            return sb.ToString();
        }

        /// <summary>The concern as the stance model reads it: every move in its own order, every cut, the direction - floats round-tripped.</summary>
        private static string ConcernKey(BillConcern concern)
        {
            if (concern == null) { return "null"; }
            var sb = new StringBuilder(128);
            sb.Append(concern.Direction.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            foreach (KeyValuePair<StanceAxis, float> move in concern.Moves)
            {
                sb.Append(move.Key).Append(':').Append(move.Value.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            }

            sb.Append('|');
            foreach ((SpendingCategory? category, WelfareProgramType? program, float cutShare) in concern.Cuts)
            {
                sb.Append(category).Append('/').Append(program).Append(':').Append(cutShare.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            }

            return sb.ToString();
        }

        private List<PartyStance> VerifyStances(Country country, BillConcern concern, string key, List<PartyStance> cached)
        {
            var clock = Stopwatch.StartNew();
            List<PartyStance> fresh = StanceModel.Stances(country, concern);
            string difference = Difference(cached, fresh);
            VerifyMilliseconds += clock.Elapsed.TotalMilliseconds;
            if (difference == null) { return cached; }
            Drifts++;
            Debug.LogError($"CHAMBER CACHE DRIFT: {country.Id}'s stances on [{key}] - {difference}. The cache's key misses an input the stance model reads; the fresh answer replaces the cached one.");
            _stances[key] = fresh;
            return fresh;
        }

        private bool VerifyVerdict(Country country, BillConcern concern, string key, bool cached)
        {
            var clock = Stopwatch.StartNew();
            bool fresh = ParliamentSystem.WouldBillPass(country, concern);
            VerifyMilliseconds += clock.Elapsed.TotalMilliseconds;
            if (fresh == cached) { return cached; }
            Drifts++;
            Debug.LogError($"CHAMBER CACHE DRIFT: {country.Id}'s verdict on [{key}] - cached {(cached ? "PASS" : "FAIL")}, the model now {(fresh ? "PASS" : "FAIL")}. The cache's key misses an input the stance model reads; the fresh answer replaces the cached one.");
            _verdicts[key] = fresh;
            return fresh;
        }

        /// <summary>Null when the two lists are the same answer party by party; otherwise the first difference, named.</summary>
        private static string Difference(IReadOnlyList<PartyStance> a, IReadOnlyList<PartyStance> b)
        {
            if (a.Count != b.Count) { return $"{a.Count} stances cached against {b.Count}"; }
            for (int i = 0; i < a.Count; i++)
            {
                PartyStance x = a[i], y = b[i];
                if (x.Party.Abbrev != y.Party.Abbrev) { return $"party {i} is {x.Party.Abbrev} cached against {y.Party.Abbrev}"; }
                if (x.Seats != y.Seats) { return $"{x.Party.Abbrev} holds {x.Seats} seats cached against {y.Seats}"; }
                if (x.Side != y.Side) { return $"{x.Party.Abbrev} votes {x.Side} cached against {y.Side}"; }
                if (x.Measured != y.Measured) { return $"{x.Party.Abbrev} measured {x.Measured} cached against {y.Measured}"; }
                if (x.Alignment != y.Alignment) { return $"{x.Party.Abbrev}'s alignment {x.Alignment:R} cached against {y.Alignment:R}"; }
                int reasons = x.Reasons?.Count ?? 0;
                if (reasons != (y.Reasons?.Count ?? 0)) { return $"{x.Party.Abbrev} gives {reasons} reasons cached against {y.Reasons?.Count ?? 0}"; }
                for (int r = 0; r < reasons; r++)
                {
                    if (x.Reasons[r] != y.Reasons[r]) { return $"{x.Party.Abbrev}'s reason {r} \"{x.Reasons[r]}\" cached against \"{y.Reasons[r]}\""; }
                }
            }

            return null;
        }
    }
}
