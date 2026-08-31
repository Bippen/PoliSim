using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-C10 (P-G2) — **the impact ledger's premise, measured before anything is built.**
    ///
    /// <para>P-G2's done-when is *"attribution lines sum to the actual divergence within stated
    /// tolerance"*, and Elias's pre-ruling anticipates the other outcome: **report the residual as a
    /// named finding rather than forcing the sum. An honest residual beats a false identity.** Which of
    /// those the item ships is not a matter of taste — it is a measurable property of this model, and
    /// this harness measures it before a line of ledger code exists.</para>
    ///
    /// <para><b>The method: LEAVE-ONE-OUT, which is the strongest attribution available.</b> For a game
    /// where the player moves k dials, run k + 2 worlds from one seed: the FULL game (all k dials), the
    /// NO-POLICY baseline (C-C9's shadow, by definition), and for each dial d one world running
    /// everything EXCEPT d. Then</para>
    ///
    /// <list type="bullet">
    /// <item><description>attribution(d) = FULL − EXCEPT(d) — what removing that one dial would have
    /// changed, with every other dial still in play;</description></item>
    /// <item><description>divergence = FULL − NONE — the whole gap the ledger has to explain;</description></item>
    /// <item><description><b>residual = divergence − Σ attribution(d)</b> — the interaction the dials
    /// have with each other, which belongs to no single one of them.</description></item>
    /// </list>
    ///
    /// <para>⚠ <b>The residual is not an error term and is not a bug.</b> A tax rise and a spending rise
    /// interact through the same GDP, so a decomposition into per-dial lines cannot be exact unless the
    /// model is linear in the dials, which it is not. What this harness answers is HOW BIG that
    /// unattributable share is — and therefore whether a screen may honestly print lines that nearly sum,
    /// or must print the residual as a line of its own with the model's non-additivity stated.</para>
    ///
    /// <para>Nothing here is tuned and nothing is fitted. It runs the model as it is and reports.</para>
    /// </summary>
    public static class ImpactLedgerFeasibilityDiagnostic
    {
        private const int Seed = 777;
        private const int Turns = 12;
        private static readonly CountryId Player = CountryId.USA;

        /// <summary>The six stats the Statistics screen actually graphs — the ledger's real audience.</summary>
        private static readonly string[] Headline =
        {
            "GDP", "Inflation", "Unemployment", "ApprovalRating", "Budget", "GovernmentDebt"
        };

        private enum Dial { Tax, Welfare, Spending, Crime }

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-C10 (P-G2): can the live-vs-shadow divergence be attributed to the dials that caused it? ===\n");
            sb.Append(F("    seed {0}, {1} turns, player {2}, four dials moved every turn.\n\n", Seed, Turns, Player));

            var all = new List<Dial> { Dial.Tax, Dial.Welfare, Dial.Spending, Dial.Crime };

            Dictionary<string, float> full = RunWorld(all);
            Dictionary<string, float> none = RunWorld(new List<Dial>());

            var attribution = new Dictionary<Dial, Dictionary<string, float>>();
            foreach (Dial d in all)
            {
                var without = new List<Dial>(all);
                without.Remove(d);
                Dictionary<string, float> except = RunWorld(without);

                var contribution = new Dictionary<string, float>();
                foreach (KeyValuePair<string, float> kv in full)
                {
                    contribution[kv.Key] = kv.Value - except[kv.Key];
                }

                attribution[d] = contribution;
            }

            // ---- the table ----
            sb.Append("    stat              divergence        tax      welfare     spending        crime  |   residual   (% of divergence)\n");
            sb.Append("    ---------------------------------------------------------------------------------------------------------------\n");

            float worstShare = 0f;
            string worstAt = "";
            int deadDials = 0;

            foreach (string stat in Headline)
            {
                float divergence = full[stat] - none[stat];
                float sum = 0f;
                var cells = new StringBuilder();
                foreach (Dial d in all)
                {
                    float c = attribution[d][stat];
                    sum += c;
                    cells.Append(N(c));
                }

                float residual = divergence - sum;
                float share = Mathf.Abs(divergence) > 1e-6f ? Mathf.Abs(residual / divergence) : 0f;
                if (Mathf.Abs(divergence) > 1e-6f && share > worstShare) { worstShare = share; worstAt = stat; }

                sb.Append(F("    {0,-15} {1}{2}  | {3}   {4,8:P1}\n", stat, N(divergence), cells, N(residual), share));
            }

            // ---- did every dial actually move something? An attribution table with a dead dial in it
            // ---- is measuring the wrong thing, and C-C9's assertion 4 is the precedent for saying so.
            sb.Append("\n");
            foreach (Dial d in all)
            {
                float biggest = 0f;
                foreach (string stat in Headline) { biggest = Mathf.Max(biggest, Mathf.Abs(attribution[d][stat])); }
                if (biggest <= 1e-6f)
                {
                    deadDials++;
                    Debug.LogError($"C-C10: dial {d} moved NOTHING across {Turns} turns. Its column is not an attribution of zero - "
                                   + "it is a lever this test failed to pull, and the table would be measuring three dials while claiming four.");
                    sb.Append(F("    ⚠ dial {0} is DEAD in this run - see the error above.\n", d));
                }
            }

            sb.Append(F("\n    WORST RESIDUAL: {0:P1} of the divergence, on {1}.\n", worstShare, worstAt));
            sb.Append(worstShare <= 0.05f
                ? "    VERDICT: the dials are near-additive here, so a ledger MAY print lines that sum to the divergence within a stated tolerance.\n"
                : "    ⚠ VERDICT: THE DIALS ARE NOT ADDITIVE. Lines that appear to sum would be a false identity. The ledger must carry the\n"
                  + "    interaction as its own named line and say what it is, on Elias's pre-ruling: an honest residual beats a false identity.\n");

            if (deadDials > 0)
            {
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>One whole game from the seed with exactly the named dials moved every turn, returned
        /// as the player country's headline stats at close.</summary>
        private static Dictionary<string, float> RunWorld(List<Dial> dials)
        {
            SimulationRandom.Seed(Seed);
            var go = new GameObject("C-C10 WORLD");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                World world = WorldFactory.CreateDefault();
                sim.SetWorld(world);

                Country player = world.GetCountry(Player);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                decisions[Player] = BuildDecision(player, dials);

                for (int t = 0; t < Turns; t++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                }

                var state = new Dictionary<string, float>();
                foreach (FieldInfo f in typeof(EconomyState).GetFields())
                {
                    if (f.FieldType == typeof(float)) { state[f.Name] = (float)f.GetValue(player.State); }
                }

                return state;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>⚠ Every dial is an ABSOLUTE target held constant across the run, not a delta repeated
        /// each turn - a repeated delta would compound and make the dials incomparable in size. The
        /// targets are read off the country's own seeded values so no figure here is invented.</summary>
        private static PolicyDecision BuildDecision(Country player, List<Dial> dials)
        {
            var decision = new PolicyDecision();

            if (dials.Contains(Dial.Tax))
            {
                foreach (TaxLine line in player.TaxLines)
                {
                    if (line.Type == TaxType.IncomeTax && line.IsImplemented)
                    {
                        decision.TaxRateOverrides[TaxType.IncomeTax] = line.Rate + 8f;
                    }
                }
            }

            if (dials.Contains(Dial.Welfare))
            {
                foreach (WelfareProgram program in player.WelfarePrograms)
                {
                    if (program.IsImplemented)
                    {
                        decision.WelfareGenerosityOverrides[program.Type] = Mathf.Min(100f, program.GenerosityLevel + 15f);
                        break;
                    }
                }
            }

            if (dials.Contains(Dial.Spending))
            {
                decision.SpendingLineChanges[SpendingCategory.Defense] = 6f;
            }

            if (dials.Contains(Dial.Crime))
            {
                decision.PoliceFundingOverride = Mathf.Min(100f, player.PoliceFundingLevel + 20f);
            }

            return decision;
        }

        private static string N(float value) => string.Format(CultureInfo.InvariantCulture, "{0,12:F4}", value);

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
