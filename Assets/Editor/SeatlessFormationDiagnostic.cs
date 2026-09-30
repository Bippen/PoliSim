using System;
using System.Collections.Generic;
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
    /// §683 (ruled): **A PARTY WITH ZERO SEATS IS NEVER OFFERED A CABINET OR SUPPORT ROLE.** Asserted: §679's case re-formed - M with no seat (its
    /// mandate held by a created party) is in neither the cabinet nor its support; a proposal naming a seatless party is inadmissible and fails, the
    /// seatless party's refusal saying why; **the assertion (`CoalitionFormation.AssertNoSeatless`) throws on a planted result** that seats one, and
    /// **never on a formed one** - every live chamber the game seats, and a seeded sweep of seat tables with zeros through `CoalitionFormation.Form`.
    /// </summary>
    public static class SeatlessFormationDiagnostic
    {
        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== SeatlessFormationDiagnostic (§683): a party with no seat is never offered a role ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            using IDisposable created = CreatedParties.Scope();
            try
            {
                const CountryId se = CountryId.Sweden;
                WorldClock.ApplyStart(se);
                CreatedParties.Clear();
                PoliticalParty[] real = PartySystems.RealRoster(se);

                // §679's case: a created party holds M's mandate, M none
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                Country country = world.GetCountry(se);
                var twin = new CreatedParty { Country = se, Key = "NM", Name = "Nya Moderaterna", LeaderName = "N. Moderat" };
                twin.ApplyOrigin(PartyOrigin.Splinter); twin.ParentKey = "M"; twin.InheritedSlice = 0.1; twin.PlaceOn(real[Array.FindIndex(real, p => p.Abbrev == "M")]);
                CreatedParties.TryRegister(twin, real, out _);
                country.ParliamentSeats["NM"] = country.ParliamentSeats.TryGetValue("M", out int m) ? m : 0;
                country.ParliamentSeats["M"] = 0;
                GovernmentFormation.View view = GovernmentFormation.ViewOfSitting(country);
                var roles = view.Cabinet.Select(c => c.Abbrev).Concat(view.Support.Select(s => s.Abbrev)).ToList();
                sb.Append(F("    measured  §679's chamber, M holding no seat: {0} - cabinet {1}, support {2}\n", view.HasGovernment ? view.Outcome.ToString() : "NO GOVERNMENT",
                    string.Join("+", view.Cabinet.Select(c => c.Abbrev)), string.Join("+", view.Support.Select(s => s.Abbrev))));
                Check(view.HasGovernment && !roles.Contains("M") && view.Cabinet.Any(c => c.Abbrev == "NM"),
                    F("a party with no seat is in neither the cabinet nor its support: {0} (M, 0 seats, left out; §679 formed NM+KD+L+M)", string.Join("+", view.Cabinet.Select(c => c.Abbrev))));

                // a proposal naming the seatless party, through the evaluator the formateur's sheet uses
                IReadOnlyList<PoliticalParty> parties = PartySystems.For(se);
                var seats = parties.Select(p => country.ParliamentSeats.TryGetValue(p.Abbrev, out int s) ? s : 0).ToArray();
                ElectionVintage vintage = GovernmentFormation.SittingVintage(country);
                CoalitionFormation.Chamber chamber = CoalitionFormation.Prepare(seats, GovernmentFormation.Compatibility(parties),
                    DeclaredRedLines.For(se, parties, vintage), true, DeclaredRedLines.InOrAgainstFor(se, parties, vintage));
                int Mask(params string[] keys) { int mk = 0; for (int p = 0; p < parties.Count; p++) { if (keys.Contains(parties[p].Abbrev)) { mk |= 1 << p; } } return mk; }
                CoalitionFormation.CabinetEvaluation withM = CoalitionFormation.Evaluate(chamber, Mask("NM", "KD", "L", "M"));
                int mIndex = Array.FindIndex(parties.ToArray(), p => p.Abbrev == "M");
                string refusal = CoalitionFormation.SupportRefusal(chamber, mIndex, Mask("NM", "KD", "L"));
                Check(withM.SeatlessMember && !withM.Admissible && !withM.Wins && refusal != null && refusal.StartsWith("holds no seat", StringComparison.Ordinal),
                    F("a proposal naming the seatless party is inadmissible and fails; asked to support, it refuses - \"{0}\"", refusal));

                // the assertion throws on a planted result
                var planted = new CoalitionResult();
                planted.Viable.Add(new GovernmentOption(Mask("NM", "KD", "L", "M"), 0, CoalitionOutcomeKind.MinorityGovernment, 0, 0, 0, 0.0, 0.0));
                bool threw = false;
                try { CoalitionFormation.AssertNoSeatless(chamber, planted); } catch (InvalidOperationException ex) { threw = true; sb.Append("    measured  the planted result: \"").Append(ex.Message).Append("\"\n"); }
                Check(threw, "the assertion fails a planted result that seats a party with no seat in a cabinet");
                var plantedSupport = new CoalitionResult();
                plantedSupport.Viable.Add(new GovernmentOption(Mask("NM", "KD", "L"), Mask("M"), CoalitionOutcomeKind.ConfidenceAndSupply, 0, 0, 0, 0.0, 0.0));
                bool threwSupport = false;
                try { CoalitionFormation.AssertNoSeatless(chamber, plantedSupport); } catch (InvalidOperationException) { threwSupport = true; }
                Check(threwSupport, "the assertion fails a planted result that lists a party with no seat as a supporter");
                CreatedParties.Clear();

                // never on a formed one: every live chamber, and a seeded sweep of seat tables with zeros
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World fresh = WorldFactory.CreateDefault();
                int chambers = 0, seatlessSeated = 0;
                foreach (Country c in fresh.Countries)
                {
                    IReadOnlyList<PoliticalParty> ps = PartySystems.For(c.Id);
                    if (ps == null || ps.Count == 0 || c.ParliamentSeats == null) { continue; }
                    int zero = ps.Count(p => !c.ParliamentSeats.TryGetValue(p.Abbrev, out int s) || s <= 0);
                    GovernmentFormation.View v = GovernmentFormation.ViewOfSitting(c);
                    chambers++;
                    seatlessSeated += zero;
                    sb.Append(F("    measured  {0}: {1} parties, {2} with no seat - {3}\n", c.Id, ps.Count, zero, v.HasGovernment ? string.Join("+", v.Cabinet.Select(x => x.Abbrev)) + (v.Support.Count > 0 ? " supported by " + string.Join("+", v.Support.Select(x => x.Abbrev)) : string.Empty) : "no government (" + v.Reason + ")"));
                }
                var rng = new System.Random(683);
                int sweeps = 0;
                for (int t = 0; t < 400; t++)
                {
                    int n = 4 + rng.Next(5);
                    var st = new int[n];
                    for (int p = 0; p < n; p++) { st[p] = rng.NextDouble() < 0.3 ? 0 : 1 + rng.Next(120); }
                    if (st.Sum() == 0) { st[0] = 10; }
                    var compat = new double[n, n];
                    for (int a = 0; a < n; a++) { for (int b = 0; b < n; b++) { compat[a, b] = a == b ? 100.0 : 20.0 + rng.Next(70); } }
                    CoalitionFormation.Form(st, compat, new List<RedLine>());   // throws (and so fails the sweep) if a seatless party is seated
                    sweeps++;
                }
                Check(chambers > 0 && sweeps == 400, F("the assertion never fires on a formed result: {0} live chambers ({1} parties with no seat among them) and 400 seeded seat tables with zeros, formed", chambers, seatlessSeated));
            }
            catch (Exception ex) { failures++; sb.Append("    THREW: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace + "\n"); }
            finally { CreatedParties.Clear(); EnergyMarket.ResetTurnState(); }

            if (failures > 0) { Debug.LogError($"SEATLESS FORMATION: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
