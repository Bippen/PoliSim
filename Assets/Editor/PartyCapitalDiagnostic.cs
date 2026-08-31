using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-D4 (§38, R-CL3) — **the long-term political capital, and an honest account of what it is worth
    /// today.**
    ///
    /// <para>Five assertions, and the second is the one that keeps the item honest rather than
    /// flattering.</para>
    ///
    /// <list type="number">
    /// <item><description><b>SEEDED FOR EVERY PARTY.</b> All 53 parties across six countries open with a
    /// record keyed to the party system, at the mandate their own seeded election gave
    /// them.</description></item>
    /// <item><description>⚠ <b>THE CARRY-OVER IS INERT IN PLAY, AND THIS PROVES IT RATHER THAN
    /// MENTIONING IT.</b> The electorate does not move with the simulation, so a second election returns
    /// the same chamber and every ratio is exactly 1.0. A reader who takes "§38 is built" to mean "a
    /// party's machine now grows and shrinks in play" would be wrong, and the assertion says
    /// so.</description></item>
    /// <item><description><b>THE RULE WORKS WHEN SEATS ACTUALLY MOVE.</b> Forced seat changes — doubled,
    /// halved — move organisational strength by exactly that ratio, and the seat baseline
    /// follows.</description></item>
    /// <item><description>⚠ <b>ZERO SEATS HOLDS STILL.</b> A party that falls below the threshold keeps
    /// its organisation and its old baseline; multiplying by zero would delete the machine of a party
    /// that missed by a tenth of a point, and nothing supports that.</description></item>
    /// <item><description><b>REPUTATION DOES NOT MOVE</b> — asserted, so the asymmetry stays deliberate.
    /// An election observes seats and shares; it observes nothing about a party's reputation, and a rule
    /// moving it would need a coefficient nothing on disk sources.</description></item>
    /// </list>
    /// </summary>
    public static class PartyCapitalDiagnostic
    {
        private const int Seed = 777;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-D4 (SS38): the long-term political capital ===\n");
            int failures = 0;

            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();

            // ---- 1. seeded for every party ----
            int parties = 0;
            int missing = 0;
            foreach (Country country in world.Countries)
            {
                foreach (PoliticalParty party in PartySystems.For(country.Id))
                {
                    parties++;
                    PartyCampaignCapital record = PartyCapital.For(country.PartyCapital, party.Abbrev);
                    if (record == null)
                    {
                        missing++;
                        Debug.LogError($"C-D4: {country.Id}/{party.Abbrev} has no capital record. A party the model knows about "
                                       + "with no place to carry its capital is a party the carry-over will silently skip.");
                        continue;
                    }

                    if (record.SeatsAtLastUpdate != party.SeedSeats)
                    {
                        missing++;
                        Debug.LogError($"C-D4: {country.Id}/{party.Abbrev} opens at {record.SeatsAtLastUpdate} seats, its seeded "
                                       + $"election gave it {party.SeedSeats}. The first carry-over would measure against the wrong "
                                       + "baseline.");
                    }
                }
            }

            failures += missing;
            sb.Append(missing == 0
                ? F("    1. seeded          OK - all {0} parties across six countries carry a record at their own seeded mandate.\n", parties)
                : F("    1. seeded          ⚠ {0} problem(s) - see the errors above.\n", missing));

            // ---- 2. ⚠ inert in play, proven ----
            Country sweden = world.GetCountry(CountryId.Sweden);
            PartyCampaignCapital before = Clone(PartyCapital.For(sweden.PartyCapital, "S"));

            // ⚠ THREE elections, not two, and the number matters. The first draft ran two and reported
            // that the capital MOVED - which was true and, read as "the electorate moves", would have been
            // the wrong conclusion. The chamber changes exactly ONCE: from the SEEDED mandate (the real
            // 2022 result) to the one the model's own predicted shares produce. After that the electorate
            // does not move, so every later election repeats that chamber and every ratio is 1.0. The
            // third run is what separates "moves once at the seam" from "moves in play".
            var seatsAfter = new List<double>();
            int runs = 0;
            for (int i = 0; i < 3; i++)
            {
                if (!NationalElection.TryPredictShares(CountryId.Sweden, out Dictionary<string, double> shares)) { break; }

                ElectionRecord record = NationalElection.Run(CountryId.Sweden, i + 1, shares);
                PartyCapital.CarryOver(sweden.PartyCapital, record.Seats);
                seatsAfter.Add(PartyCapital.For(sweden.PartyCapital, "S")?.OrganizationalStrength ?? -1);
                runs++;
            }

            if (runs != 3)
            {
                failures++;
                Debug.LogError("C-D4: Sweden's election did not run three times, so the inertness claim is UNTESTED rather than proven.");
            }

            bool movesOnce = runs == 3
                             && !Mathf.Approximately((float)seatsAfter[0], (float)before.OrganizationalStrength)
                             && Mathf.Approximately((float)seatsAfter[1], (float)seatsAfter[0])
                             && Mathf.Approximately((float)seatsAfter[2], (float)seatsAfter[1]);

            bool neverMoves = runs == 3
                              && Mathf.Approximately((float)seatsAfter[0], (float)before.OrganizationalStrength)
                              && Mathf.Approximately((float)seatsAfter[2], (float)seatsAfter[0]);

            if (!movesOnce && !neverMoves)
            {
                failures++;
                Debug.LogError("C-D4: S's organisation kept moving across three elections with a static electorate. Either the "
                               + "electorate now moves - in which case this assertion is stale and SS38 has become a live mechanic - "
                               + "or the carry-over is not idempotent on a repeating chamber, which is a bug.");
            }

            sb.Append(movesOnce
                ? F("    2. inert in play   ⚠ MOVES EXACTLY ONCE, AT THE SEAM. S's organisation went {0:F2} -> {1:F2} on the first\n"
                    + "                       election - the SEEDED 2022 mandate handing over to the one the model's own predicted\n"
                    + "                       shares produce - and then held at {2:F2} through two more. The electorate does not\n"
                    + "                       move, so the chamber repeats. SS38 is BUILT AND PERSISTED; it is NOT yet a mechanic\n"
                    + "                       a player can feel, and reading \"SS38 is built\" as \"a party's machine now grows and\n"
                    + "                       shrinks in play\" would be wrong.\n",
                    before.OrganizationalStrength, seatsAfter[0], seatsAfter[2])
                : neverMoves
                    ? F("    2. inert in play   ⚠ CONFIRMED - three Swedish elections left S's organisation at {0:F2}, unchanged.\n", seatsAfter[2])
                    : "    2. inert in play   ⚠ KEPT MOVING - see the error above.\n");

            // ---- 3. the rule works when seats move ----
            var forced = new List<PartyCampaignCapital>
            {
                new PartyCampaignCapital { PartyAbbrev = "A", Reputation = 50, OrganizationalStrength = 40, SeatsAtLastUpdate = 20 },
                new PartyCampaignCapital { PartyAbbrev = "B", Reputation = 50, OrganizationalStrength = 60, SeatsAtLastUpdate = 20 },
                new PartyCampaignCapital { PartyAbbrev = "C", Reputation = 50, OrganizationalStrength = 80, SeatsAtLastUpdate = 10 },
                new PartyCampaignCapital { PartyAbbrev = "D", Reputation = 50, OrganizationalStrength = 30, SeatsAtLastUpdate = 12 },
            };

            PartyCapital.CarryOver(forced, new Dictionary<string, int>
            {
                { "A", 40 },   // doubled  -> 40 -> 80
                { "B", 10 },   // halved   -> 60 -> 30
                { "C", 40 },   // x4       -> 80 -> 320, CLAMPED to 100
                { "D", 0 },    // wiped out -> holds still
            });

            failures += Expect(sb, "3a. doubled", forced[0], 80, 40);
            failures += Expect(sb, "3b. halved", forced[1], 30, 10);
            failures += Expect(sb, "3c. clamped", forced[2], 100, 40);
            failures += Expect(sb, "3d. zero seats holds", forced[3], 30, 12);

            // ---- 5. reputation does not move ----
            bool reputationHeld = true;
            foreach (PartyCampaignCapital r in forced) { reputationHeld &= Mathf.Approximately((float)r.Reputation, 50f); }

            if (!reputationHeld)
            {
                failures++;
                Debug.LogError("C-D4: an election moved a party's REPUTATION. Nothing is supposed to - an election observes seats "
                               + "and shares and observes nothing about reputation, and any rule moving it would need a coefficient "
                               + "nothing on disk sources. If a rule was added, it needs its source recorded, not this assertion "
                               + "relaxed.");
            }

            sb.Append(reputationHeld
                ? "    5. reputation      OK - unchanged by every case above, which is the recorded asymmetry, not an oversight.\n"
                : "    5. reputation      ⚠ MOVED - see the error above.\n");

            sb.Append(F("\n=== PartyCapitalDiagnostic: {0} ===\n", failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILURE(S)"));

            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        private static int Expect(StringBuilder sb, string label, PartyCampaignCapital record, double strength, int seats)
        {
            bool ok = Mathf.Approximately((float)record.OrganizationalStrength, (float)strength) && record.SeatsAtLastUpdate == seats;
            if (!ok)
            {
                Debug.LogError($"C-D4: {label} - {record.PartyAbbrev} expected organisation {strength} at {seats} seats, "
                               + $"got {record.OrganizationalStrength} at {record.SeatsAtLastUpdate}.");
            }

            sb.Append(F("    {0,-22} {1} (organisation {2:F2}, baseline {3} seats)\n",
                label, ok ? "OK" : "⚠ FAILED", record.OrganizationalStrength, record.SeatsAtLastUpdate));
            return ok ? 0 : 1;
        }

        private static PartyCampaignCapital Clone(PartyCampaignCapital source) => source == null
            ? null
            : new PartyCampaignCapital
            {
                PartyAbbrev = source.PartyAbbrev,
                Reputation = source.Reputation,
                OrganizationalStrength = source.OrganizationalStrength,
                SeatsAtLastUpdate = source.SeatsAtLastUpdate,
            };

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
