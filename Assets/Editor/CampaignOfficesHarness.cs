using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-B4's harness — §10's campaign offices.
    ///
    /// The done-when, asserted:
    /// 1. **offices measurably change regional door-to-door reach** — a region with an office has
    ///    volunteer-hours a region without has not (the ceiling on doors a day, W-B11); the office's
    ///    own daily operation makes contacts in its region and in no other; a rally's local audience
    ///    in a region with a full office is four times a visit's;
    /// 2. **and GOTV** — after a campaign the office region's mobilisation is above the untouched
    ///    50 and every other region's is exactly 50; the party's turnout there is higher, and the
    ///    votes it draws with the office exceed the votes without;
    /// 3. **concentration in few regions beats spreading thin in a measured scenario** — the same
    ///    money, three offices in the three largest valkretsar against ten spread thin, the votes
    ///    mobilised over all 29 compared; the budget at which spreading starts to win is measured
    ///    and reported, not hidden;
    /// 4. the economics are what §10 lists: opening costs, maintenance is paid daily, an office the
    ///    party cannot pay for starves (no recruits, no operation, influence down) and nothing is
    ///    spent that the party does not have.
    ///
    /// Staging: the 29 valkretsar's 2018 valid votes as eligible (SOURCED, `valkrets_votes_2018.csv`),
    /// a uniform 30 % preference for the party under test, §26's four attributes at the neutral 50,
    /// a 60-day campaign, door-knocking as the operation. All office figures [AUTHORED-DRAFT].
    /// </summary>
    public static class CampaignOfficesHarness
    {
        private const int Days = 60;
        private const double Preference = 0.30;
        private const double BaseTurnout = 0.8718;
        private const int PartyCount = 2;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-B4: campaign offices (§10) - organisation as local reach, volunteers recruited, a daily operation into the ground game, maintenance paid or starved ===\n");

            double[] eligible = ReadEligible(out string[] names);
            int[] largest = Largest(eligible, 10);
            int big = largest[0];
            var pref = new double[PartyCount];
            pref[0] = Preference; pref[1] = 1.0 - Preference;

            // ---------- 1. reach ----------
            {
                var gotv = new RegionalMobilization(eligible, PartyCount);
                var net = new OfficeNetwork(eligible.Length);
                double money = 1_000_000.0;
                bool opened = net.Open(big, 0, 10_000.0, ref money);
                double contacts = 0.0;
                for (int d = 0; d < Days; d++) { net.Day(gotv, 0, GotvOperation.DoorKnocking, ref money, out double c); contacts += c; }

                failures += Assert(sb, "1a. the office region has volunteer-hours a region without has none of", opened && net.VolunteerHours(big) > 0 && net.VolunteerHours(largest[1]) == 0,
                    string.Format(CultureInfo.InvariantCulture, "{0}: {1} volunteers, {2:F0} h a day; {3}: 0 h", names[big], net.At(big).Volunteers, net.VolunteerHours(big), names[largest[1]]));

                GotvSpec spec = GotvModel.Spec(GotvOperation.DoorKnocking);
                double withOffice = GotvModel.Contacts(spec, 50_000.0, 600.0 + net.VolunteerHours(big), out _, out _);
                double without = GotvModel.Contacts(spec, 50_000.0, 600.0, out _, out _);
                failures += Assert(sb, "1b. a door-to-door action with the office's hours knocks more doors than the same action without", withOffice > without,
                    string.Format(CultureInfo.InvariantCulture, "50 000 kr and 200 headquarters volunteers: {0:F0} doors without, {1:F0} with the office's {2:F0} h", without, withOffice, net.VolunteerHours(big)));

                bool onlyThere = true;
                for (int r = 0; r < eligible.Length; r++) { onlyThere &= (gotv.WeightedContacts(r, 0) > 0) == (r == big); }
                failures += Assert(sb, "1c. the office's daily operation makes contacts in its region and in no other", onlyThere && contacts > 0,
                    string.Format(CultureInfo.InvariantCulture, "{0:N0} doors over {1} days in {2}; {3:N0} kr spent", contacts, Days, names[big], 1_000_000.0 - money));

                double full = CampaignOffices.LocalAudience(eligible[big], 1.0), visit = CampaignOffices.LocalAudience(eligible[big], 0.0);
                failures += Assert(sb, "1d. a rally's local audience with a full office is four times a visit's", Math.Abs(full / visit - 1.0 / CampaignOffices.VisitFraction) < 1e-9,
                    string.Format(CultureInfo.InvariantCulture, "{0:N0} against {1:N0} of {2:N0}", full, visit, eligible[big]));
                failures += Assert(sb, "1e. influence is recruited, not bought: half capacity after 15 days, full after 30",
                    InfluenceAfter(15) < 0.51 && InfluenceAfter(15) > 0.49 && InfluenceAfter(30) == 1.0,
                    string.Format(CultureInfo.InvariantCulture, "{0:F2} at 15 days, {1:F2} at 30", InfluenceAfter(15), InfluenceAfter(30)));
            }

            // ---------- 2. GOTV ----------
            {
                var withNet = new RegionalMobilization(eligible, PartyCount);
                var bare = new RegionalMobilization(eligible, PartyCount);
                var net = new OfficeNetwork(eligible.Length);
                double money = 1_000_000.0;
                net.Open(big, 0, 10_000.0, ref money);
                for (int d = 0; d < Days; d++) { net.Day(withNet, 0, GotvOperation.DoorKnocking, ref money, out _); }

                bool others50 = true;
                for (int r = 0; r < eligible.Length; r++) { if (r != big) { others50 &= withNet.Mobilization(r, 0) == 50.0; } }
                failures += Assert(sb, "2a. the office region's mobilisation is above the untouched 50; every other region's is exactly 50", withNet.Mobilization(big, 0) > 50.0 && others50,
                    string.Format(CultureInfo.InvariantCulture, "{0}: {1:F2}", names[big], withNet.Mobilization(big, 0)));

                double turnoutWith = withNet.PartyTurnout(big, 0, BaseTurnout, 50, 50, 50), turnoutBare = bare.PartyTurnout(big, 0, BaseTurnout, 50, 50, 50);
                double votesWith = withNet.RegionVotes(big, pref, BaseTurnout, 50, 50, 50)[0], votesBare = bare.RegionVotes(big, pref, BaseTurnout, 50, 50, 50)[0];
                failures += Assert(sb, "2b. the party's turnout there is higher, and it draws more votes with the office than without", turnoutWith > turnoutBare && votesWith > votesBare,
                    string.Format(CultureInfo.InvariantCulture, "turnout {0:P2} -> {1:P2}; votes {2:N0} -> {3:N0} (+{4:N0})", turnoutBare, turnoutWith, votesBare, votesWith, votesWith - votesBare));
            }

            // ---------- 3. concentration against spread ----------
            {
                sb.Append("\n  The same money on the ground: three offices in the three largest valkretsar against ten spread thin (opening + maintenance fixed, the rest as operations):\n");
                double? crossover = null;
                double prevA = 0, prevB = 0;
                foreach (double budget in new[] { 0.9e6, 1.5e6, 2.4e6, 4e6, 8e6, 16e6 })
                {
                    double a = VotesMobilised(eligible, pref, largest, 3, budget);
                    double b = VotesMobilised(eligible, pref, largest, 10, budget);
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,5:N1} M kr: three {1,9:N0} votes, ten {2,9:N0} votes -> {3}\n", budget / 1e6, a, b, a > b ? "concentration wins" : "spreading wins"));
                    if (crossover == null && b > a) { crossover = budget; }
                    prevA = a; prevB = b;
                }

                double atWarChest3 = VotesMobilised(eligible, pref, largest, 3, 1.5e6), atWarChest10 = VotesMobilised(eligible, pref, largest, 10, 1.5e6);
                failures += Assert(sb, "3a. at the prototype's ground budget (1.5 M kr of a 2.4 M war chest) three offices beat ten", atWarChest3 > atWarChest10,
                    string.Format(CultureInfo.InvariantCulture, "{0:N0} against {1:N0} votes mobilised", atWarChest3, atWarChest10));
                sb.Append(string.Format(CultureInfo.InvariantCulture, "  measured, not asserted: spreading first wins at {0} (fixed costs against section 35's concavity)\n",
                    crossover.HasValue ? crossover.Value / 1e6 + " M kr" : "no budget in the sweep"));
            }

            // ---------- 4. economics ----------
            {
                var gotv = new RegionalMobilization(eligible, PartyCount);
                var net = new OfficeNetwork(eligible.Length);
                double money = CampaignOffices.OpenCost + 3 * CampaignOffices.MaintenancePerDay + 5_000.0;
                double start = money;
                net.Open(big, 0, 10_000.0, ref money);
                bool second = net.Open(largest[1], 0, 10_000.0, ref money);
                double spent = 0.0;
                for (int d = 0; d < 10; d++) { spent += net.Day(gotv, 0, GotvOperation.DoorKnocking, ref money, out _); }
                CampaignOffice o = net.At(big);
                failures += Assert(sb, "4a. a second office the party cannot afford is not opened and nothing is paid", !second && net.Count == 1, $"{net.Count} office");
                failures += Assert(sb, "4b. maintenance is paid daily; when the money runs out the office starves - no recruits, no operation, influence down - and nothing is spent that the party does not have",
                    o.StarvedDays > 0 && money >= 0.0 && Math.Abs(start - money - CampaignOffices.OpenCost - spent) < 1e-6 && o.Influence < (double)o.Volunteers / CampaignOffices.VolunteerCapacity,
                    string.Format(CultureInfo.InvariantCulture, "{0} starved days of 10, {1} volunteers, influence {2:F2}, {3:N0} kr left of {4:N0}", o.StarvedDays, o.Volunteers, o.Influence, money, start));
                failures += Assert(sb, "4c. a network's daily cost is what section 10 lists: maintenance plus the operation, per office", Math.Abs(net.DailyCost - (CampaignOffices.MaintenancePerDay + 10_000.0)) < 1e-9,
                    string.Format(CultureInfo.InvariantCulture, "{0:N0} kr a day", net.DailyCost));
            }

            sb.Append($"\nOFFICES: {(failures == 0 ? "all assertions hold" : failures + " FAILED")}\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static double InfluenceAfter(int days)
        {
            var net = new OfficeNetwork(1);
            double money = 1e9;
            net.Open(0, 0, 0.0, ref money);
            for (int d = 0; d < days; d++) { net.Day(null, 0, GotvOperation.DoorKnocking, ref money, out _); }
            return net.Influence(0);
        }

        /// <summary>Votes the party draws over all 29 with a network of the first <paramref name="count"/> of <paramref name="order"/>, the budget's fixed costs paid and the remainder run as operations, less the votes with no network at all.</summary>
        private static double VotesMobilised(double[] eligible, double[] pref, int[] order, int count, double budget)
        {
            double fixedCost = count * (CampaignOffices.OpenCost + Days * CampaignOffices.MaintenancePerDay);
            double perOfficePerDay = Math.Max(0.0, budget - fixedCost) / (count * Days);
            var gotv = new RegionalMobilization(eligible, PartyCount);
            var bare = new RegionalMobilization(eligible, PartyCount);
            var net = new OfficeNetwork(eligible.Length);
            double money = budget;
            for (int i = 0; i < count; i++) { net.Open(order[i], 0, perOfficePerDay, ref money); }
            for (int d = 0; d < Days; d++) { net.Day(gotv, 0, GotvOperation.DoorKnocking, ref money, out _); }

            double votes = 0.0;
            for (int r = 0; r < eligible.Length; r++)
            {
                votes += gotv.RegionVotes(r, pref, BaseTurnout, 50, 50, 50)[0] - bare.RegionVotes(r, pref, BaseTurnout, 50, 50, 50)[0];
            }

            return votes;
        }

        private static int[] Largest(double[] eligible, int n)
        {
            var idx = new List<int>();
            for (int r = 0; r < eligible.Length; r++) { idx.Add(r); }
            idx.Sort((a, b) => eligible[b].CompareTo(eligible[a]));
            return idx.GetRange(0, n).ToArray();
        }

        private static double[] ReadEligible(out string[] names)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "sweden", "valkrets_votes_2018.csv"));
            var eligible = new List<double>();
            var nameList = new List<string>();
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("valkrets;")) { continue; }
                string[] cells = line.Split(';');
                nameList.Add(cells[0]);
                eligible.Add(double.Parse(cells[1], CultureInfo.InvariantCulture) / BaseTurnout);
            }

            names = nameList.ToArray();
            return eligible.ToArray();
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
