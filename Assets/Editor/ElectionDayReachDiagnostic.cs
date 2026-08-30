using System;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-G1's done-when, made MACHINE-PROVABLE: "the game reaches election day from a new game
    /// without a crash".
    ///
    /// That clause asks for something a harness normally cannot give, because reaching an election
    /// means playing. What it actually requires is narrower and entirely testable in batch: a NEW
    /// WORLD, seeded exactly as the game seeds it, advanced turn by turn past an election turn, with
    /// the election held on the real path and the chamber set from the result - and no exception
    /// thrown anywhere along the way.
    ///
    /// It runs ALL SIX COUNTRIES, not just the player's, because the seat seed, the chamber size and
    /// the election path all differ per country and four of the six deliberately hold no election.
    /// A crash in the four "not implemented" branches would be exactly the kind of failure that only
    /// shows up when a player picks that country.
    /// </summary>
    public static class ElectionDayReachDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            sb.Append("=== W-G1: a new game reaches election day, six countries, no exception ===\n");
            int failures = 0;

            foreach (CountryId id in Enum.GetValues(typeof(CountryId)))
            {
                try
                {
                    failures += ReachOne(sb, id);
                }
                catch (Exception ex)
                {
                    sb.Append($"  FAIL {id}: threw {ex.GetType().Name} - {ex.Message}\n");
                    failures++;
                }
            }

            sb.Append($"\n=== ElectionDayReachDiagnostic: {(failures == 0 ? "ALL PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static int ReachOne(StringBuilder sb, CountryId id)
        {
            int failures = 0;
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            Country country = world.Countries.Find(c => c.Id == id);
            if (country == null)
            {
                sb.Append($"  FAIL {id}: not in the seeded world\n");
                return 1;
            }

            // 1. The seed itself: a real chamber, summing to that country's real size.
            int chamber = PartySystems.ChamberSeats(id);
            int seeded = 0;
            foreach (var kvp in country.ParliamentSeats) { seeded += kvp.Value; }
            if (seeded != chamber)
            {
                sb.Append($"  FAIL {id}: seeded {seeded} seats, chamber is {chamber}\n");
                failures++;
            }

            // 2. Advance past an election turn, exactly as AdvanceTurn does for seats.
            int electionTurn = ElectionSystem.ElectionCycle;
            for (int turn = 1; turn <= electionTurn; turn++)
            {
                ParliamentSystem.UpdateSeats(country);
            }

            if (!ElectionSystem.IsElectionTurn(electionTurn))
            {
                sb.Append($"  FAIL {id}: turn {electionTurn} is not an election turn\n");
                failures++;
            }

            // 3. Hold the election on the real path.
            ElectionRecord record = NationalElection.TryPredictShares(id, out System.Collections.Generic.Dictionary<string, double> byParty)
                ? NationalElection.Run(id, electionTurn, byParty)
                : NationalElection.Run(id, electionTurn, null);

            country.ElectionHistory.Add(record);

            if (record.Method == ElectionMethod.NotImplemented)
            {
                // The chamber must be UNTOUCHED and the reason must be stated - an empty result
                // passed off as a real one is the failure this branch exists to prevent.
                int after = 0;
                foreach (var kvp in country.ParliamentSeats) { after += kvp.Value; }
                bool intact = after == chamber;
                bool explained = !string.IsNullOrEmpty(record.NotHeldReason);
                if (!intact || !explained)
                {
                    sb.Append($"  FAIL {id}: no live path, but chamber {after}/{chamber} and reason {(explained ? "given" : "MISSING")}\n");
                    failures++;
                }
                else
                {
                    sb.Append($"  ok   {id,-8} no live path, chamber untouched at {chamber}; reason stated\n");
                }

                return failures;
            }

            ParliamentSystem.SetSeatsFromElection(country, record.Seats);
            int elected = 0;
            foreach (var kvp in country.ParliamentSeats) { elected += kvp.Value; }
            if (elected != chamber)
            {
                sb.Append($"  FAIL {id}: the election returned {elected} seats, chamber is {chamber}\n");
                failures++;
            }

            var top = new StringBuilder();
            foreach (PoliticalParty p in PartySystems.For(id))
            {
                if (country.ParliamentSeats.TryGetValue(p.Abbrev, out int s) && s > 0) { top.Append($"{p.Abbrev} {s}  "); }
            }

            sb.Append($"  ok   {id,-8} {record.Method}, {elected} seats: {top}\n");
            return failures;
        }
    }
}