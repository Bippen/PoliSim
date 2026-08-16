using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Continuous Time Phase 5, step 2: the throwaway diagnostic that pins the paths feedback must
    /// NOT flow through, at runtime rather than by code inspection, BEFORE any conversion exists -
    /// so the conversion cannot silently create a coupling and then pass its own equivalence bar
    /// with the coupling inside it (Phase 4's verdict, item 3).
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.Phase5NoFeedbackDiagnostic.Run -logFile &lt;path&gt;`
    ///
    /// Three pinned properties:
    /// 1. POTENTIAL-GDP INDEPENDENCE: PotentialGDP's path is identical whether or not actual GDP is
    ///    shocked - the real-output-gap property the Taylor rule depends on.
    /// 2. PUBLISHED-VS-LIVE SEPARATION (Step A's invariant, asserted dynamically): corrupting every
    ///    published figure wildly, every day, changes NOTHING in any country's simulated state over
    ///    two full turns. Stronger than the standing static check (no Simulation file mentions
    ///    Published), because it also catches an indirect route.
    /// 3. THE INTEREST CHAIN'S INDIRECTION, both directions: a rate change must NOT move inflation
    ///    through the Phillips step directly (monetary policy reaches inflation only through
    ///    rate -&gt; C/I -&gt; GDP -&gt; Okun -&gt; Phillips - a recorded design decision, not a gap), and
    ///    MUST move C and I through the identity (the transmission exists at all - the negative
    ///    pin alone would also pass on a severed chain).
    /// </summary>
    public static class Phase5NoFeedbackDiagnostic
    {
        public static void Run()
        {
            int passed = 0, total = 0;

            // ---- 1. PotentialGDP independence of actual-GDP shocks.
            {
                World a = WorldFactory.CreateDefault();
                World b = WorldFactory.CreateDefault();
                Country ca = a.GetCountry(CountryId.Germany);
                Country cb = b.GetCountry(CountryId.Germany);
                cb.State.GDP *= 0.7f; // a 30% actual-output crash in B only

                bool identical = true;
                for (int i = 0; i < 20; i++)
                {
                    MacroSystem.ApplyPotentialGdpGrowth(ca);
                    MacroSystem.ApplyPotentialGdpGrowth(cb);
                    if (ca.State.PotentialGDP != cb.State.PotentialGDP)
                    {
                        identical = false;
                        break;
                    }
                }

                total++;
                passed += Check("PotentialGDP path identical under a 30% actual-GDP shock", identical,
                    $"a={ca.State.PotentialGDP:R} b={cb.State.PotentialGDP:R}") ? 1 : 0;
            }

            // ---- 2. Published-vs-live separation, dynamic form.
            {
                List<float[]> clean = RunTwoTurns(corruptPublished: false);
                List<float[]> corrupted = RunTwoTurns(corruptPublished: true);
                bool identical = clean.Count == corrupted.Count;
                for (int i = 0; identical && i < clean.Count; i++)
                {
                    for (int j = 0; j < clean[i].Length; j++)
                    {
                        if (clean[i][j] != corrupted[i][j])
                        {
                            identical = false;
                            break;
                        }
                    }
                }

                total++;
                passed += Check("two turns of daily published-data corruption move NO simulated state", identical,
                    $"{clean.Count} country-turn snapshots compared field-for-field") ? 1 : 0;
            }

            // ---- 3a. The Phillips step must not read the interest rate.
            {
                World a = WorldFactory.CreateDefault();
                World b = WorldFactory.CreateDefault();
                Country ca = a.GetCountry(CountryId.USA);
                Country cb = b.GetCountry(CountryId.USA);
                cb.CurrencyZone.InterestRate += 10f;

                MacroSystem.ApplyPhillipsCurveInflation(ca);
                MacroSystem.ApplyInflationExpectations(ca.State);
                MacroSystem.ApplyPhillipsCurveInflation(cb);
                MacroSystem.ApplyInflationExpectations(cb.State);

                total++;
                passed += Check("+10pt rate leaves the Phillips step untouched (indirection holds)",
                    ca.State.Inflation == cb.State.Inflation && ca.State.InflationExpectations == cb.State.InflationExpectations,
                    $"a={ca.State.Inflation:R} b={cb.State.Inflation:R}") ? 1 : 0;
            }

            // ---- 3b. The identity must feel the same rate change (the chain is attached).
            {
                World a = WorldFactory.CreateDefault();
                World b = WorldFactory.CreateDefault();
                Country ca = a.GetCountry(CountryId.USA);
                Country cb = b.GetCountry(CountryId.USA);
                float g = ca.State.GDP * (ca.GovernmentSpendingRate / 100f);

                MacroSystem.ApplyNationalAccounts(ca, g, ca.CurrencyZone.InterestRate);
                MacroSystem.ApplyNationalAccounts(cb, g, cb.CurrencyZone.InterestRate + 10f);

                total++;
                passed += Check("+10pt rate DOES dampen C and I through the identity (transmission exists)",
                    cb.State.Consumption < ca.State.Consumption && cb.State.Investment < ca.State.Investment
                    && cb.State.GDP < ca.State.GDP,
                    $"C {ca.State.Consumption:F1}->{cb.State.Consumption:F1}, I {ca.State.Investment:F1}->{cb.State.Investment:F1}") ? 1 : 0;
            }

            Debug.Log($"P5DIAG: {passed} of {total} no-feedback pins hold.");
            CheckExit.Finish(passed == total ? 0 : 1);
        }

        /// <summary>Two full turns via the real day loop, every public EconomyState float snapshotted
        /// per country per turn. With <paramref name="corruptPublished"/>, every country's every
        /// published entry and period closing is overwritten with garbage EVERY DAY - if any of it
        /// reaches simulation state, the trajectories diverge.</summary>
        private static List<float[]> RunTwoTurns(bool corruptPublished)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"P5DIAG_{corruptPublished}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country country in world.Countries)
                {
                    decisions[country.Id] = PolicyDecision.None();
                }

                System.Reflection.FieldInfo[] fields = typeof(EconomyState).GetFields(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var snapshots = new List<float[]>();
                for (int turn = 0; turn < 2; turn++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++)
                    {
                        sim.AdvanceDay();
                        if (corruptPublished)
                        {
                            foreach (Country country in world.Countries)
                            {
                                foreach (KeyValuePair<PublishedStat, PublishedSeries> pair in country.Published.Series)
                                {
                                    foreach (PublishedEntry entry in pair.Value.Entries)
                                    {
                                        entry.Value = 99999f;
                                        entry.Status = RevisionStatus.Preliminary;
                                    }
                                }

                                // The period closings feed the credit-rating review; the rating is
                                // documented as feeding nothing simulated. Corrupting these proves
                                // that documentation dynamically - a rating gone haywire must still
                                // move NO EconomyState field.
                                var keys = new List<(ClosingStat Stat, System.DateTime PeriodStart)>(country.Published.PeriodClosingValues.Keys);
                                foreach ((ClosingStat, System.DateTime) key in keys)
                                {
                                    country.Published.PeriodClosingValues[key] = 99999f;
                                }
                            }
                        }
                    }

                    sim.AdvanceTurn(decisions);
                    foreach (Country country in world.Countries)
                    {
                        var snap = new float[fields.Length];
                        for (int i = 0; i < fields.Length; i++)
                        {
                            object v = fields[i].GetValue(country.State);
                            snap[i] = v is float f ? f : System.Convert.ToSingle(v);
                        }

                        snapshots.Add(snap);
                    }
                }

                return snapshots;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static bool Check(string name, bool ok, string detail)
        {
            Debug.Log($"  {(ok ? "ok  " : "FAIL")} {name}  ({detail})");
            return ok;
        }
    }
}
