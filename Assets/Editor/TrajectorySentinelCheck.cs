using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §496 (2026-09-14): THE NO-POLICY TRAJECTORY'S SENTINEL - the first <see cref="Turns"/> turns of the trajectory dump's own text
    /// (<see cref="TrajectoryBaselineDump.Build"/>, the same builder the dump writes from), at both seeds, hashed and held against the
    /// digests of the declared baseline. §490 moved the no-policy trajectory from its SECOND turn - Germany's revenue left § 32a's own
    /// arithmetic for the ramps derived from it - and no bar saw it: the pass was filmed, not dumped, and `ArtifactIdentityCheck` verifies
    /// the trajectory files already on disk, never a rerun. A change that moves this text is either a family - dump it, explain it per
    /// country, and set <see cref="BaselineLabel"/> and the digests here in the same commit - or a numeric-inertness break (§402).
    /// First in the simulation group, so no other check's static state can reach it before it runs.
    ///
    /// <para><b>FT-10 (2026-09-21): THE BOUNDS PASS - a digest certifies SAMENESS, not sanity.</b> The digests above already hash the energy
    /// layer's fields (they are public fields of the state, eight of the dump's seventy-seven), so a CHANGE in them was always seen; what no
    /// sentinel saw was GROWTH - Poland's `EnergyIndustryPrice` past 10⁶ at t361 and near 10²⁰ at t1000, in the baseline itself, found only by
    /// §538's diff. The bounds pass runs seed 777 for a century (its first twenty turns are the digest's, so the run is shared) and holds each
    /// country's energy prices OVER ITS OWN PRICE LEVEL - the household price at the seed's prices, the industry price deflated, the congestion
    /// rent deflated - to two measures: LEVEL, the largest move from turn 1 anywhere in the century, and TAIL, the move over the last twenty
    /// years, which is what separates a runaway from a level shift. A country with no recorded breach is held to <see cref="CleanBandPercent"/>
    /// on both. ⚠ FT-10's measured breaches are RECORDED, not excused: each is a named cell with its own ceiling (the measurement rounded up, so
    /// it cannot worsen by a point unseen, and cannot sit a point slack either), their count is a ratchet in the ledger, and every run prints
    /// them as OPEN. The fix lowers the cells to the band and the count to zero in its own commit - apply nothing until ruled.</para>
    ///
    /// <para><b>P-A landed (2026-09-21, §545) and lowered them in its own commit:</b> thirteen cells to eleven, Poland's two TAIL cells and France's closed (the runaway's signature is gone: Poland's
    /// tail reads 0.004 %), every LEVEL ceiling re-cut on the new measurement, and each cell now carries its CAUSE - what is left is the rule's own bounded answer to a ministry's cut (Poland, Italy),
    /// the same answer at a K the rule should not have (France, Sweden - P-A′, ruled third), and Sweden's water value (P-B, ruled second). Italy's industry LEVEL is a cell P-A OPENED, by three
    /// thousandths of a point: its path shrinks in real terms, so the old rule's growth index had been DAMPING its answer, and the share-of-path form reads the cut in full.</para>
    ///
    /// <para><b>P-B landed (2026-09-21, §552) and lowered them again:</b> eleven cells to six. Sweden's five water-value cells are DELETED - its congestion rent over the price level
    /// reads 0.000 % on both measures where it read 47.8 and 7.4, its tails 0.1 and 0.2 - and the one Swedish cell left, the industry price's LEVEL at 1.193 %, is the levy's
    /// part: K 40.2 on a levy of two hundredths of a cent, P-A′'s subject, re-cut with that cause.</para>
    ///
    /// <para><b>P-A′ landed (2026-09-21, §553) and lowered them a third time:</b> six cells to two. Sweden's, Italy's and Poland's four are DELETED - each reads 0.000 %: their
    /// lines carry none of the scheme the bill's levy funds (each sourced from the country's own 2026 budget document), so a ministry's cut of the line no longer reaches the
    /// levy. France's two stand, lower (18.394 and 4.789 where they read 27.176 and 7.075): two thirds of its line IS that support, so the rule still answers its ministry's
    /// cut - the rule's bounded answer, and the only cause left.</para>
    /// </summary>
    public static class TrajectorySentinelCheck
    {
        /// <summary>The baseline these digests are the first turns of: `traj_p6pap` - FT-10 · P-A′ (2026-09-21, §553): the levy rule reads the SUPPORT in the budget's energy line -
        /// the line's own path at its sourced support share (France's two thirds; Sweden, Italy and Poland none) and the subsidy dial's cost in full. France, Sweden, Italy and
        /// Poland move from turn 2 on their own levies - the three whose line carries no support stop answering their ministries' cuts, and their real energy prices hold the seed's
        /// figures for a thousand years; Germany moves from turn 3 through the euro zone's one rate and the USA from turn 3 through its currency, their REAL energy prices not at
        /// all. Only AI-governed books change. Before it `traj_p6pb` - FT-10 · P-B (2026-09-21, §552): Sweden's water value in the seed's prices - each neighbour's block
        /// price deflated by its own price level before the weighting, the zone price carried by Sweden's level once. Sweden moves from TURN 1 on its own clearing (the three price
        /// levels already differ at the first boundary); the USA and Poland from turn 2 through `CurrencyStrength` (a floating currency follows its rate against its partners'
        /// average), in the fifth to eighth digit; Germany, France and Italy DO NOT MOVE AT ALL - no channel reaches a shared-currency country. Before it `traj_p6pa` - FT-10 · P-A (2026-09-21, §545): the levy scale reads the support line's move as a SHARE of its own path
        /// times K, the seed's line over the seed's levy - Poland, France, Sweden and Italy move on their own levies from turn 2; the USA's (no levy) and Germany's (no line) levy scales and REAL
        /// energy prices cannot move and do not, and their other rows move from turn 3 in the fifth decimal place by propagation (Germany through the euro zone's one rate, the USA through its
        /// currency and trade). Only AI-governed books change: a player's path rides prices alone and P-A is the old arithmetic there. Before it `traj_p6e1` - P6-E1's yield (2026-09-18, §538): jobbskatteavdraget in Sweden's income-tax yield, the lever seeded at the credited average on taxed income; before it `traj_p6d1` - P6-D1 (2026-09-17, §533): every pair of the six carries a sourced trade link (Eurostat 2023, goods and services, the exporter's report), replacing ten authored pairs and five absences; all six move through the trade balance and tariff revenue (was `traj_pn3`).</summary>
        public const string BaselineLabel = "p6pap2";

        /// <summary>CONVENTION: twenty turns - §490's divergence showed on turn 2, and the pair of runs costs seconds.</summary>
        public const int Turns = 20;

        /// <summary>SHA-256 of the dump's text through turn 20 (the header and every row of turns 1-20), read off `traj_p6pap2_s{seed}_t100.csv`.</summary>
        private static readonly (int Seed, string Sha256)[] Expected =
        {
            (777, "ddc9d3fdf0b62a893ee08d514b984433a6a5820007e9f2e000fcb6a3fc3ff54b"),
            (424242, "ceae8afba8898f82ed36aa93ad19e7237bc0cb16272fd3a99d78edf44adf6424"),
        };

        // ---- FT-10: the bounds pass ------------------------------------------------------------------------------------------------

        /// <summary>CONVENTION: the seed the century runs on - the first of <see cref="Expected"/>, so its first twenty turns are the digest's and one run serves both.</summary>
        public const int CenturySeed = 777;

        /// <summary>CONVENTION: a hundred turns - the no-policy century every long-run diagnostic reads; FT-10's runaway stood at +31 % there and needed a millennium to be noticed without a bound.</summary>
        public const int CenturyTurns = 100;

        /// <summary>CONVENTION: TAIL is measured from this turn to the century's end - twenty years, the digest's own span.</summary>
        public const int TailFromTurn = 80;

        /// <summary>CONVENTION: the band a clean country's real energy prices are held to, per cent, on both measures. Measured 2026-09-21 on `traj_p6e1`: the USA and Germany 0.000,
        /// Italy 0.77 at most (its levy scale breathes with the ministry's cut of the line) - one per cent holds all three with nothing to spare for a compounding term. On `traj_p6pa` Italy's
        /// industry LEVEL reads 1.003 and is a recorded cell (P-A reads its cut in full); the band itself is not moved.</summary>
        public const double CleanBandPercent = 1.0;

        /// <summary>CONVENTION: a recorded cell's ceiling may stand this far above its measurement and no further, per cent - the ceilings are the measurements rounded up to a whole point.</summary>
        public const double CellSlackPercent = 1.0;

        /// <summary>
        /// FT-10's recorded cells - the cells over the clean band, measured on the landing of 2026-09-22. LOWER IT AS THEY CLOSE, NEVER RAISE IT: FOUR cells in two countries (six in four on
        /// `traj_p6pb` before P-A′, eleven on `traj_p6pa` before P-B, thirteen in three on `traj_p6e1` before P-A). The compounding term is gone (§545), so is the water value read
        /// across price levels (§552), and the rule reads the support in the line, not the line (§553); what stands is the rule's bounded answer to France's ministry, named in the
        /// table's CAUSE (`EnergyRunawayDiagnostic` decomposes it).
        /// </summary>
        public const int EnergyBreachCeiling = 4;

        /// <summary>CONVENTION: the cause a recorded cell carries, printed on its line (P-B's water-value cause retired with its cells, §552; P-A′'s whole-line cause with its, §553).</summary>
        private const string RuleAnswer = "EN-4's rule answering the ministry's cut of the line at its sourced support share - bounded at 1 + 0.8 K × share on the ministry's own path, no tail";

        /// <summary>The recorded cells: country, series, measure, the ceiling in per cent (the 2026-09-21 measurement on `traj_p6pap` rounded up to a whole point), and the cause the cell waits on.</summary>
        private static readonly (string Country, string Series, string Measure, double CeilingPercent, string Cause)[] KnownBreaches =
        {
            // §572: the cells under the RULED test (Annex II's categories for all four). France's two fall with the share it reads; Poland's two stay because its
            // share is nearly the whole line; Sweden's and Italy's CLOSE - their shares are small enough that the rule's answer is inside the clean band.
            ("France", "household", "LEVEL", 21.0, RuleAnswer),   // 20.728 at t56 (27.176 before P-A′, 35.628 before P-A): K 13.4 x the share 0.763
            ("France", "industry", "LEVEL", 6.0, RuleAnswer),     // 5.396 at t62 (7.075; 9.275)
            ("Poland", "household", "LEVEL", 3.0, RuleAnswer),    // 2.740 at t65 (2.797 before P-A′): the coal chapter is nearly the whole line
            ("Poland", "industry", "LEVEL", 3.0, RuleAnswer),     // 2.793 at t65 (2.851)
        };

        /// <summary>The bounds pass over the century's text. True when every cell holds: a clean cell inside the band, a recorded one under its ceiling and not slack, the count at its ratchet.</summary>
        private static bool EnergyBounds(string century, StringBuilder sb)
        {
            var level = new Dictionary<string, double[]>();   // country -> PriceLevel by turn
            var series = new Dictionary<string, Dictionary<string, double[]>>();   // country -> series -> value by turn (already over the price level where the field is nominal)
            var nominal = new Dictionary<string, Dictionary<string, double[]>>();
            foreach (string line in century.Split('\n'))
            {
                string[] p = line.Split(',');
                if (p.Length != 4 || !int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int turn) || turn < 1 || turn > CenturyTurns) { continue; }
                string key = p[2] == "PriceLevel" ? "P" : p[2] == "EnergyHouseholdPriceReal" ? "household" : p[2] == "EnergyIndustryPrice" ? "industry" : p[2] == "EnergyCongestionRent" ? "rent" : null;
                if (key == null || !double.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) { continue; }
                if (key == "P") { if (!level.TryGetValue(p[1], out double[] l)) { level[p[1]] = l = new double[CenturyTurns + 1]; } l[turn] = value; continue; }
                if (!nominal.TryGetValue(p[1], out Dictionary<string, double[]> byKey)) { nominal[p[1]] = byKey = new Dictionary<string, double[]>(); }
                if (!byKey.TryGetValue(key, out double[] values)) { byKey[key] = values = new double[CenturyTurns + 1]; }
                values[turn] = value;
            }
            foreach (KeyValuePair<string, Dictionary<string, double[]>> c in nominal)
            {
                if (!level.TryGetValue(c.Key, out double[] l)) { continue; }
                var real = new Dictionary<string, double[]>();
                foreach (KeyValuePair<string, double[]> k in c.Value)
                {
                    var r = new double[CenturyTurns + 1];
                    for (int t = 1; t <= CenturyTurns; t++) { r[t] = k.Key == "household" ? k.Value[t] : (l[t] > 0 ? k.Value[t] / l[t] : 0.0); }   // the household price is written at the seed's prices already
                    real[k.Key] = r;
                }
                series[c.Key] = real;
            }

            sb.Append($"    --- FT-10 · the bounds pass: seed {CenturySeed}, {CenturyTurns} turns, each energy price over its own country's price level · LEVEL = the largest move from turn 1, TAIL = turn {TailFromTurn} to {CenturyTurns} · clean band {CleanBandPercent.ToString("0.0", CultureInfo.InvariantCulture)} %\n");
            // The enumeration rule: a century that parsed no energy rows bounded nothing, and its silence would read as six clean countries.
            if (series.Count == 0) { sb.Append("    ⚠ NO ENERGY ROWS PARSED - the bounds pass verified nothing.\n"); return false; }
            bool ok = true; int breaches = 0, cells = 0;
            var seen = new HashSet<string>();
            var countries = new List<string>(series.Keys); countries.Sort(string.CompareOrdinal);
            foreach (string country in countries)
            {
                foreach (string key in new[] { "household", "industry", "rent" })
                {
                    if (!series[country].TryGetValue(key, out double[] x) || !(x[1] > 0.0)) { continue; }   // a series at zero from the seed (no zonal links, so no rent) has no ratio to hold
                    double levelDev = 0.0; int at = 1;
                    for (int t = 1; t <= CenturyTurns; t++) { double d = Math.Abs(x[t] / x[1] - 1.0) * 100.0; if (d > levelDev) { levelDev = d; at = t; } }
                    double tailDev = x[TailFromTurn] > 0.0 ? Math.Abs(x[CenturyTurns] / x[TailFromTurn] - 1.0) * 100.0 : 0.0;
                    foreach ((string measure, double measured, string span) in new[] { ("LEVEL", levelDev, "t" + at), ("TAIL", tailDev, "t" + TailFromTurn + "-t" + CenturyTurns) })
                    {
                        cells++;
                        double ceiling = CleanBandPercent; bool recorded = false; string cause = null;
                        foreach ((string c, string s, string m, double pct, string why) in KnownBreaches) { if (c == country && s == key && m == measure) { ceiling = pct; recorded = true; cause = why; seen.Add(c + "/" + s + "/" + m); } }
                        bool over = measured > ceiling;
                        bool slack = recorded && measured < ceiling - CellSlackPercent;
                        if (measured > CleanBandPercent) { breaches++; }
                        if (recorded || over)
                        {
                            sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0} {1,-8} {2,-9} {3,-5} {4,8:F3} % at {5} against {6:F1} %{7}\n",
                                over ? "⚠ OVER " : slack ? "⚠ SLACK" : "  OPEN ", country, key, measure, measured, span, ceiling,
                                over ? (recorded ? " - a recorded cell got WORSE" : " - a NEW breach of the clean band: a term is compounding against the price level")
                                     : slack ? " - the ceiling stands more than a point above its measurement: lower it" : " - recorded, not excused · " + cause));
                        }
                        if (over || slack) { ok = false; }
                    }
                }
            }
            foreach ((string c, string s, string m, double pct, string why) in KnownBreaches)
            {
                if (!seen.Contains(c + "/" + s + "/" + m)) { ok = false; sb.Append($"    ⚠ {c} {s} {m}: a recorded cell the century never produced - the table names a series the dump no longer carries.\n"); }
            }
            sb.Append($"    {cells} cell(s) held; {breaches} over the clean band against the recorded {EnergyBreachCeiling} (P-A, P-B and P-A′ landed - §545, §552, §553; what stands is EN-4's rule answering France's ministry at the line's support share - `EnergyRunawayDiagnostic` decomposes it; lower the ceiling if it closes).\n");
            RatchetLedger.Report("TrajectorySentinelCheck.ENERGY_BREACHES", breaches, EnergyBreachCeiling);
            if (breaches > EnergyBreachCeiling) { ok = false; }
            return ok;
        }

        /// <summary>The century's text through the last row of <paramref name="turns"/> - the same bytes <see cref="TrajectoryBaselineDump.Build"/> writes for that horizon, since the run is deterministic and the rows are in turn order.</summary>
        private static string Prefix(string century, int turns)
        {
            int cut = century.IndexOf("\n" + (turns + 1).ToString(CultureInfo.InvariantCulture) + ",", StringComparison.Ordinal);
            return cut < 0 ? century : century.Substring(0, cut + 1);
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            FieldInfo[] fields = TrajectoryBaselineDump.StateFields();
            bool ok = true;
            var sb = new StringBuilder();
            sb.Append($"=== TRAJECTORY SENTINEL: the first {Turns} turns of the no-policy dump against baseline '{BaselineLabel}' ===\n");
            string century = TrajectoryBaselineDump.Build(CenturySeed, CenturyTurns, fields);   // FT-10: one run - its first turns are the digest's, its whole length the bounds pass's
            foreach ((int seed, string expected) in Expected)
            {
                string got = Sha256(seed == CenturySeed ? Prefix(century, Turns) : TrajectoryBaselineDump.Build(seed, Turns, fields));
                bool same = got == expected;
                sb.Append($"    seed {seed}: {got} {(same ? "- the baseline's" : "- NOT the baseline's " + expected)}\n");
                if (!same)
                {
                    ok = false;
                    Debug.LogError($"TRAJECTORY SENTINEL: seed {seed}'s first {Turns} no-policy turns moved off baseline '{BaselineLabel}' - a family (dump it, explain it per country, set the label and the digests here in the same commit) or a numeric-inertness break.");
                }
            }
            bool bounded = EnergyBounds(century, sb);
            if (!bounded) { ok = false; Debug.LogError("TRAJECTORY SENTINEL: the energy layer's bounds pass failed - a real energy price left its band, a recorded FT-10 cell got worse or sits slack, or the breach count passed its ratchet. The lines above name the cell."); }
            Debug.Log(sb.ToString());
            Debug.Log(ok ? "=== TrajectorySentinelCheck: ALL ASSERTIONS PASS ===" : "=== TrajectorySentinelCheck: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static string Sha256(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(new UTF8Encoding(false).GetBytes(text));
                var hex = new StringBuilder(64);
                foreach (byte b in hash) { hex.Append(b.ToString("x2")); }
                return hex.ToString();
            }
        }
    }
}
