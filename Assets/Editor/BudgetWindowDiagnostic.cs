using System;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-C2 (Playtest-1 finding 4) — **how long a new government actually waits before it can lay a
    /// budget**, measured per country from the epoch, and **whether the window's existence moves any
    /// simulated quantity.**
    ///
    /// <para>The finding: *"Entering office should open the budget process immediately for the first
    /// fiscal year — the player lays a budget on arrival instead of waiting for the calendar's next
    /// cycle."* Before building a window, this measures the wait it is supposed to remove, so the
    /// item's own premise is a number rather than an impression.</para>
    ///
    /// <para>⚠ <b>THE SECOND MEASUREMENT IS THE ITEM'S REAL BAR, AND A TRAJECTORY DIFF CANNOT PROVIDE
    /// IT.</b> `TryOpenBudgetProcess` is reached only through `AdvanceCountryDayTick`, whose callers
    /// are `GameController.Update` and `UiScreenshotDriver` — **never `SimulationManager.AdvanceDay`**.
    /// `TrajectoryBaselineDump` drives `AdvanceDay`/`AdvanceTurn` with no controller, so it never calls
    /// the method this item changes: a byte-identical trajectory diff here would pass whether or not
    /// the design were right, which is the "0 anomalies" fallacy this repo's own front page warns
    /// about. So the identity is asserted where the change actually lives — **two worlds advanced day
    /// tick by day tick, one with the window opening early and one without, neither introducing a
    /// bill, compared on the full economic state.**</para>
    /// </summary>
    public static class BudgetWindowDiagnostic
    {
        private const int MaxDaysSearched = 800;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-C2: the incoming government's budget window ===\n");

            sb.Append("\n--- 1. How long a new government waits today, per country ---\n");
            sb.Append("    (day ticks from the epoch, exactly as GameController.Update drives them)\n");
            foreach (CountryId id in Enum.GetValues(typeof(CountryId)))
            {
                (int month, int day) = FiscalYearData.GetFiscalYearStart(id);
                int wait = DaysUntilWindowOpens(id, out DateTime openedOn);
                sb.Append(F("    {0,-8} fiscal year starts {1:00}-{2:00}   window opens after {3,3} day tick(s), on {4:yyyy-MM-dd}\n",
                    id, month, day, wait, openedOn));
            }

            sb.Append("\n--- 2. Does the window's existence move a simulated quantity? ---\n");
            int failures = CompareStates(sb);

            sb.Append("\n--- 3. The arrival window opens on day one, and ONCE ---\n");
            foreach (CountryId id in Enum.GetValues(typeof(CountryId)))
            {
                int wait = DaysUntilWindowOpens(id, out DateTime openedOn);
                if (wait != 1)
                {
                    failures++;
                    Debug.LogError(F("C-C2: {0}'s incoming government waits {1} day tick(s) for its budget window - the finding asks for arrival, which is 1.", id, wait));
                }

                int reopened = DaysUntilWindowReopens(id, out DateTime reopenedOn);
                sb.Append(F("    {0,-8} opens after {1} tick on {2:yyyy-MM-dd}; after it is spent the next opening is tick {3} on {4:yyyy-MM-dd}\n",
                    id, wait, openedOn, reopened, reopenedOn));

                // Once spent, the next opening must be the country's own fiscal-year start - the
                // arrival window is a one-off, not a process left permanently ajar.
                (int m, int d2) = FiscalYearData.GetFiscalYearStart(id);
                if (reopened < 0 || reopenedOn.Month != m || reopenedOn.Day != d2)
                {
                    failures++;
                    Debug.LogError(F("C-C2: after {0}'s arrival window is spent, the next opening is {1:yyyy-MM-dd}, which is not its fiscal-year start {2:00}-{3:00}. The arrival window must be a one-off.",
                        id, reopenedOn, m, d2));
                }
            }

            sb.Append(F("\n=== BudgetWindowDiagnostic: {0} ===\n",
                failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILURE(S)"));

            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        /// <summary>Day ticks from the epoch until this country's budget process opens, driven exactly
        /// as the controller drives it: advance the day, then run the country's day tick.</summary>
        private static int DaysUntilWindowOpens(CountryId id, out DateTime openedOn)
        {
            SimulationRandom.Seed(777);
            var go = new GameObject("C-C2 WAIT");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(WorldFactory.CreateDefault());

                for (int d = 0; d < MaxDaysSearched; d++)
                {
                    sim.AdvanceDay();
                    sim.AdvanceCountryDayTick(id);
                    if (sim.GetPendingBudgetProcess(id))
                    {
                        openedOn = sim.CurrentDate;
                        return d + 1;
                    }
                }

                openedOn = sim.CurrentDate;
                return -1;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>Day ticks until the window opens a SECOND time, after the arrival window has been
        /// spent and closed — which must be the country's own fiscal-year start.</summary>
        private static int DaysUntilWindowReopens(CountryId id, out DateTime reopenedOn)
        {
            SimulationRandom.Seed(777);
            var go = new GameObject("C-C2 REOPEN");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(WorldFactory.CreateDefault());

                bool spent = false;
                for (int d = 0; d < MaxDaysSearched; d++)
                {
                    sim.AdvanceDay();
                    sim.AdvanceCountryDayTick(id);

                    if (!sim.GetPendingBudgetProcess(id)) { continue; }

                    if (!spent)
                    {
                        // Close it exactly the way the game does, with no test-only hook: introducing
                        // a bill IS what resolves the process (`IntroduceBudgetBill` removes the
                        // country from the pending set - "introducing IS the resolution").
                        sim.IntroduceBudgetBill(id, new BudgetBill());
                        spent = true;
                        continue;
                    }

                    reopenedOn = sim.CurrentDate;
                    return d + 1;
                }

                reopenedOn = sim.CurrentDate;
                return -1;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Two worlds, one year of day ticks each, NEITHER introducing a bill — one with the window
        /// forced open on day one, one left alone. If the window is a player affordance and not a
        /// simulation change, every public `EconomyState` field of every country must match exactly.
        /// </summary>
        private static int CompareStates(StringBuilder sb)
        {
            string plain = RunYear(forceWindowOpen: false);
            string opened = RunYear(forceWindowOpen: true);

            if (string.Equals(plain, opened, StringComparison.Ordinal))
            {
                sb.Append("    IDENTICAL. One year of day ticks over all six countries, every public\n");
                sb.Append("    EconomyState field, with the window forced open on day one against not opened\n");
                sb.Append("    at all - and no bill introduced in either. The window is a PERMISSION, and\n");
                sb.Append("    opening it earlier changes nothing the simulation computes.\n");
                return 0;
            }

            Debug.LogError("C-C2: opening the budget window early CHANGED the simulated state with no bill introduced. "
                           + "The window is not a pure affordance, so P-B2 is a BASELINE item: stop, capture the new "
                           + "trajectory family and explain the change per country before committing.");
            sb.Append("    ⚠ DIFFERENT - see the error above. This is the item's stop condition.\n");
            return 1;
        }

        /// <summary>One year of day ticks over every country, returning the full state as a string.</summary>
        private static string RunYear(bool forceWindowOpen)
        {
            SimulationRandom.Seed(777);
            var go = new GameObject("C-C2 RUN");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                World world = WorldFactory.CreateDefault();
                sim.SetWorld(world);

                for (int d = 0; d < SimulationManager.DaysPerTurn * 3; d++)
                {
                    sim.AdvanceDay();
                    foreach (Country c in world.Countries)
                    {
                        sim.AdvanceCountryDayTick(c.Id);
                        if (forceWindowOpen) { sim.TryOpenBudgetProcess(c.Id, FiscalYearStartFor(c.Id, sim.CurrentDate)); }
                    }
                }

                return Fingerprint(world);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>A date this country's own fiscal-year check will accept, so the window opens on
        /// every tick of the forced run — the strongest form of "opened as early and as often as
        /// possible" without touching the method under test.</summary>
        private static DateTime FiscalYearStartFor(CountryId id, DateTime near)
        {
            (int month, int day) = FiscalYearData.GetFiscalYearStart(id);
            return new DateTime(near.Year, month, day);
        }

        private static string Fingerprint(World world)
        {
            var sb = new StringBuilder();
            foreach (Country c in world.Countries)
            {
                sb.Append(c.Id).Append(':');
                foreach (System.Reflection.FieldInfo f in typeof(EconomyState).GetFields())
                {
                    if (f.FieldType != typeof(float)) { continue; }
                    sb.Append(f.Name).Append('=')
                      .Append(((float)f.GetValue(c.State)).ToString("R", CultureInfo.InvariantCulture)).Append('|');
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
