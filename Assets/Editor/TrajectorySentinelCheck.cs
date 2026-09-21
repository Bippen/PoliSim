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
    /// </summary>
    public static class TrajectorySentinelCheck
    {
        /// <summary>The baseline these digests are the first turns of: `traj_p6e1` - P6-E1's yield (2026-09-18, §538): jobbskatteavdraget in Sweden's income-tax yield, the lever seeded at the credited average on taxed income; before it `traj_p6d1` - P6-D1 (2026-09-17, §533): every pair of the six carries a sourced trade link (Eurostat 2023, goods and services, the exporter's report), replacing ten authored pairs and five absences; all six move through the trade balance and tariff revenue (was `traj_pn3`).</summary>
        public const string BaselineLabel = "p6e1";

        /// <summary>CONVENTION: twenty turns - §490's divergence showed on turn 2, and the pair of runs costs seconds.</summary>
        public const int Turns = 20;

        /// <summary>SHA-256 of the dump's text through turn 20 (the header and every row of turns 1-20), read off `traj_p6d1_s{seed}_t100.csv`.</summary>
        private static readonly (int Seed, string Sha256)[] Expected =
        {
            (777, "59c67cd49946729fa49b538732c2505f4e151209ca97cf0f57a1cd5fe8b07049"),
            (424242, "1557a604fafef42a3623bd042d5c1574e88a9a3e7123096139ab4e10783e2ceb"),
        };

        // ---- FT-10: the bounds pass ------------------------------------------------------------------------------------------------

        /// <summary>CONVENTION: the seed the century runs on - the first of <see cref="Expected"/>, so its first twenty turns are the digest's and one run serves both.</summary>
        public const int CenturySeed = 777;

        /// <summary>CONVENTION: a hundred turns - the no-policy century every long-run diagnostic reads; FT-10's runaway stood at +31 % there and needed a millennium to be noticed without a bound.</summary>
        public const int CenturyTurns = 100;

        /// <summary>CONVENTION: TAIL is measured from this turn to the century's end - twenty years, the digest's own span.</summary>
        public const int TailFromTurn = 80;

        /// <summary>CONVENTION: the band a clean country's real energy prices are held to, per cent, on both measures. Measured 2026-09-21 on `traj_p6e1`: the USA and Germany 0.000,
        /// Italy 0.77 at most (its levy scale breathes with the ministry's cut of the line) - one per cent holds all three with nothing to spare for a compounding term.</summary>
        public const double CleanBandPercent = 1.0;

        /// <summary>CONVENTION: a recorded cell's ceiling may stand this far above its measurement and no further, per cent - the ceilings are the measurements rounded up to a whole point.</summary>
        public const double CellSlackPercent = 1.0;

        /// <summary>
        /// FT-10's recorded breaches - the cells over the clean band on `traj_p6e1`, 2026-09-21. LOWER IT AS THEY CLOSE, NEVER RAISE IT: thirteen cells, three countries, two mechanisms
        /// (`EnergyRunawayDiagnostic` decomposes them): the levy scale divides the support line's shortfall, which rides the spending index's REAL GROWTH, by a levy path that rides the
        /// price level alone (Poland, France, Sweden); and Sweden's water value reads Poland's and Germany's prices at THEIR price levels against a seed carried by Sweden's.
        /// </summary>
        public const int EnergyBreachCeiling = 13;

        /// <summary>The recorded cells: country, series, measure, the ceiling in per cent (the 2026-09-21 measurement rounded up to a whole point).</summary>
        private static readonly (string Country, string Series, string Measure, double CeilingPercent)[] KnownBreaches =
        {
            ("France", "household", "LEVEL", 36.0),   // 35.628 at t100 - of which the rule's own level shift is the larger part (the levy is a small share of what the line funds)
            ("France", "industry", "LEVEL", 10.0),    // 9.275
            ("France", "household", "TAIL", 2.0),     // 1.878
            ("Poland", "household", "LEVEL", 31.0),   // 30.946
            ("Poland", "industry", "LEVEL", 32.0),    // 31.544
            ("Poland", "household", "TAIL", 12.0),    // 11.652 - the runaway's signature: still compounding at the century's end
            ("Poland", "industry", "TAIL", 12.0),     // 11.844
            ("Sweden", "household", "LEVEL", 8.0),    // 7.125
            ("Sweden", "industry", "LEVEL", 14.0),    // 13.986
            ("Sweden", "household", "TAIL", 2.0),     // 1.658
            ("Sweden", "industry", "TAIL", 4.0),      // 3.101
            ("Sweden", "rent", "LEVEL", 49.0),        // 48.427 - the congestion rent over the price level, downstream of the water value
            ("Sweden", "rent", "TAIL", 8.0),          // 7.649
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
                        double ceiling = CleanBandPercent; bool recorded = false;
                        foreach ((string c, string s, string m, double pct) in KnownBreaches) { if (c == country && s == key && m == measure) { ceiling = pct; recorded = true; seen.Add(c + "/" + s + "/" + m); } }
                        bool over = measured > ceiling;
                        bool slack = recorded && measured < ceiling - CellSlackPercent;
                        if (measured > CleanBandPercent) { breaches++; }
                        if (recorded || over)
                        {
                            sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0} {1,-8} {2,-9} {3,-5} {4,8:F3} % at {5} against {6:F1} %{7}\n",
                                over ? "⚠ OVER " : slack ? "⚠ SLACK" : "  OPEN ", country, key, measure, measured, span, ceiling,
                                over ? (recorded ? " - FT-10's recorded breach got WORSE" : " - a NEW breach of the clean band: a term is compounding against the price level")
                                     : slack ? " - the ceiling stands more than a point above its measurement: lower it" : " - FT-10, recorded, not excused"));
                        }
                        if (over || slack) { ok = false; }
                    }
                }
            }
            foreach ((string c, string s, string m, double pct) in KnownBreaches)
            {
                if (!seen.Contains(c + "/" + s + "/" + m)) { ok = false; sb.Append($"    ⚠ {c} {s} {m}: a recorded cell the century never produced - the table names a series the dump no longer carries.\n"); }
            }
            sb.Append($"    {cells} cell(s) held; {breaches} over the clean band against the recorded {EnergyBreachCeiling} (FT-10 OPEN - `EnergyRunawayDiagnostic` decomposes them; lower the ceiling as they close).\n");
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
