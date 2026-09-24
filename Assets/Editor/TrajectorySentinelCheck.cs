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
        /// <summary>PN-3b (2026-09-22, §577): the wage bill's base is PotentialOutput.LabourInput × the real wage, where it was the 20–64 cohort × the 15+ participation
        /// rate - the seam PN-3 retired from potential, now off the base every wage-taxed instrument is levied on. Every AI book moves: five states take more revenue and run
        /// smaller deficits at year 100 (France +33.5 % on the budget, Italy +20.2, Poland +10.7, Sweden +4.1, Germany +2.6; debt -19.8 to -2.3 %), and the USA's fiscal rule
        /// spends the room instead - its government consumption stops being cut (4 053 at t20 and 2 056 at t60 on the old path; 4 338 at t40 and rising on this one) and its
        /// deficit is 2.4 % wider. Before it 'p6pap2' - FT-10 P-A′ (§572).</summary>
        /// <summary>FT-16 (2026-09-23, §585): the euro zone's rate blends its members' Taylor readings by their HICP country weights - household consumption at current
        /// prices, Eurostat's HFMCE basis - where it weighted by REAL GDP. Germany's weight at year 100 is 0.626 against 0.619, France 0.271 against 0.276, Italy 0.103
        /// against 0.105; the zone's rate moves +0.0007 pp at t100, and the three euro states and, through trade and currency, the other three move in late digits.
        /// A control dump of the tree without the change was byte-identical to 'pn3b', so the whole diff is this change. Before it 'pn3b' - PN-3b (§577).</summary>
        /// <summary>PN-1's participation response (2026-09-23, §596): the structural participation gains the bands a pension age has crossed since the seed, at the
        /// country's sourced rate for the year before the lower age × the hazard at it (Atav, Jongen &amp; Rabaté 2021, Table B.1). Zero at the seed; the no-policy run
        /// moves where a statute steps after 2026, and the state's rate carries each step as a level shift the day it moves (the review's D1: moved on the anchor
        /// alone, the lag read as a negative supply shock and lowered unemployment); the rate before an age is read by single year between band midpoints (D2).
        /// The shift is synced at the top of both readers of the gap, the daily reversion and the boundary's split (the verification pass: a boundary on the day a year
        /// commits, or a bill passing, reached the split before the day's step). At year 100 on both seeds: France (62 y 9 m → 64 by 2033) +0.93 % labour force,
        /// +0.93 to +0.96 % potential, +0.95 to +0.99 % GDP; Germany +0.14 %, Italy +0.08 to +0.10 %, the USA +0.06 %; Sweden and Poland by spillover alone. France's
        /// debt ratio 24.20 → 24.19 on seed 777 and 33.21 → 33.63 on 424242 - the AI ministry's threshold rule, whose net-expenditure cap reads the labour step as
        /// potential growth and cuts less (`PensionParticipationProbe`), and whose binding years move under a small perturbation. Before it 'ft16' - FT-16 (§585);
        /// 'ft14' and 'pn1d2' were byte-identical to it.</summary>
        /// <summary>§599 (2026-09-23, ruled): WHERE A COUNTRY'S EFFECT WAS MEASURED, THE MEASUREMENT IS THE STEP - France reads Rabaté &amp; Rochut's +20.9 pp and
        /// Germany Geyer &amp; Welteke's +13.5 pp directly (the formula gave France 25.3); the four unmeasured countries keep the sourced rate × the median hazard
        /// and move against 'pp3' only by spillover (Italy through the zone rate, the others through their currencies; 1.2e-4 at most). At year 100: France +0.77 % labour force, +0.78 to +0.83 % GDP; Germany +0.24 %, +0.25 to +0.26 %. France's century
        /// debt ratio moves −0.08 points on one seed and +0.70 on the other: recorded, by the same ruling, as NO MEASURABLE FISCAL EFFECT, not a finding.
        /// Before it 'pp3' - §596.</summary>
        /// <summary>§604 (2026-09-24, K-1 part (3)): THE GAME STARTS ON 1 OCTOBER 2026, after the Riksdag elected on 2026-09-13 convened. The move reaches the
        /// no-policy economy through `Country.CalendarYear` ALONE - with the year computed on the old start's day count the century is byte-identical to 'pp4'
        /// ('ep1old'). Germany, Italy and the USA first move at turn 1 (their 2027 statute steps land inside turn 0), France at turn 2, Sweden and Poland at
        /// turn 4 through their currencies alone. Debt as % of nominal GDP at turn 100: Germany 31.43 -> 33.04 on 777 and 33.61 -> 33.71 on 424242,
        /// France 24.12 -> 23.99 and 33.91 -> 31.13; the sign holds on both seeds (France lower on 99 of 100 turns, Germany higher on 94 and 95) and turns
        /// 1-5 are identical across them - a SYSTEMATIC effect of the statutes arriving about nine months earlier in game time, not seed noise, and
        /// §599's ruling (a sign that flips) does not cover it; its size at turn 100 depends on the seed. Before it 'pp4'; K-1's
        /// parts (1) and (2) ('k1ch', 'k1dl2') were byte-identical to it.</summary>
        /// <summary>§618 (2026-09-25, PS-1): THE WORLD CLOCK - the dump's world opens on Sweden's own start, 18 January 2026 (the standard run-up before 13 September),
        /// instead of K-1's 1 October 2026, and every country is seated in its chamber of record on that date (Sweden's 2022 Riksdag; the others as before, their
        /// latest elections). The move reaches the no-policy economy through the calendar year, as §604's did in the other direction - the statutes now arrive
        /// eight and a half months LATER in game time - and, where a chamber changed, through the parliament's votes; the probe `ps1probe` (this tree with the
        /// roster's own seats in every chamber) measures the second channel, recorded in §618. At turn 100 the largest movers are France's real household
        /// energy price change (−3.6 %), budget (−1.1 %) and population growth (−1.0 %) on seed 777; Germany's population growth rate moves ±10–14 % on a
        /// figure of 0.002; Sweden, Poland and the USA move in late digits. Before it 'k1ep' - K-1 part (3) (§604).</summary>
        public const string BaselineLabel = "ps2cal";   // PS-2 / CL-4 (§619): the family `ps2cal` is BYTE-IDENTICAL to `ps1wc` on both seeds - the dump holds no election (it drives the manager, not the controller) and the campaign draws on its own streams - so the digests stand

        /// <summary>CONVENTION: twenty turns - §490's divergence showed on turn 2, and the pair of runs costs seconds.</summary>
        public const int Turns = 20;

        /// <summary>SHA-256 of the dump's text through turn 20 (the header and every row of turns 1-20), read off `traj_ps1wc_s{seed}_t100.csv`.</summary>
        private static readonly (int Seed, string Sha256)[] Expected =
        {
            (777, "438e1148d46691f5086116373a6ab3d5cb08d92087b807931f2c1a1d83569446"),
            (424242, "fc163628c6dd184b47c6d472d6923343ba3345913ff511118ee372b5dc6ee905"),
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
            ("France", "household", "LEVEL", 20.0, RuleAnswer),   // 19.454 at t48 on PN-3b (20.728 at t56 before it) (27.176 before P-A′, 35.628 before P-A): K 13.4 x the share 0.763
            ("France", "industry", "LEVEL", 6.0, RuleAnswer),     // 5.064 at t56 on PN-3b (5.396 at t62 before it) (7.075; 9.275)
            ("Poland", "household", "LEVEL", 3.0, RuleAnswer),    // 2.414 at t65 on PN-3b (2.740 before it) (2.797 before P-A′): the coal chapter is nearly the whole line
            ("Poland", "industry", "LEVEL", 3.0, RuleAnswer),     // 2.461 at t65 on PN-3b (2.793 before it; 2.851)
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
