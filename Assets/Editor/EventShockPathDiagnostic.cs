using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// FT-9, LANDED as ruled (2026-09-08, §404): event shocks enter the daily path over their duration. (1) Landing the catalogue's −2 % event on a fresh
    /// Sweden leaves GDP untouched and arms the daily path: the per-day share is −2 % of the GDP it hit over ShockDurationDays, the days left the duration.
    /// (2) Sweden the player against an untouched run (the ministry off), the −2 % event landed by hand at the top of turn 3: after the year's days the shock
    /// has entered in full (the sum landed equals −2 % of the GDP at arming to 1e-4 relative; no days left), GDP sits below the untouched run at mid-year,
    /// and unemployment sits ABOVE it at mid-year - Okun saw the fall - and the unemployment gap two years on is smaller than at mid-year - the recovery
    /// lowered it back. (3) The boundary's events checkpoint (SimulationManager.BoundaryLedger) moves GDP by nothing for six countries over eight
    /// no-player turns, whatever the dice rolled: the jump §400 measured is gone from the boundary by construction.
    /// </summary>
    public static class EventShockPathDiagnostic
    {
        private const int Turns = 8;
        private const int Landing = 3;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            EconomicEvent shock = null;
            var poolField = typeof(EventSystem).GetField("EventPool", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);   // the pool is private; EventPoolCheck reads it the same way
            var pool = poolField?.GetValue(null) as List<EconomicEvent>;
            if (pool != null) { foreach (EconomicEvent e in pool) { if (Mathf.Abs(e.GdpShockPercent + 2f) < 1e-6f) { shock = e; break; } } }
            if (shock == null) { Debug.LogError("EVENT PATH: the catalogue carries no −2 % event to land."); CheckExit.Finish(1); return; }

            // (1) arming leaves GDP untouched
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            Country s = world.GetCountry(CountryId.Sweden);
            float gdpBefore = s.State.GDP;
            EventSystem.ApplyEvent(s, shock);
            if (Mathf.Abs(s.State.GDP - gdpBefore) > 1e-6f * gdpBefore) { Debug.LogError($"EVENT PATH: landing the event moved GDP at the boundary ({gdpBefore:F3} → {s.State.GDP:F3}) - the shock is meant to enter over the days (§404)."); ok = false; }
            float expectedPerDay = gdpBefore * (-2f / 100f) / EventSystem.ShockDurationDays;
            if (Mathf.Abs(s.State.EventGdpShockPerDay - expectedPerDay) > 1e-4f * Mathf.Abs(expectedPerDay) || s.State.EventGdpShockDaysLeft != EventSystem.ShockDurationDays) { Debug.LogError($"EVENT PATH: the armed shock reads {s.State.EventGdpShockPerDay:R} a day for {s.State.EventGdpShockDaysLeft} days, expected {expectedPerDay:R} for {EventSystem.ShockDurationDays}."); ok = false; }

            // (2) the shock enters over the year and Okun sees it
            float[] untouched = RunSweden(null);
            float[] shocked = RunSweden(shock);
            // [0] GDP at arming, [1] applied after the year, [2] days left after the year, [3] GDP mid-year, [4] U mid-year, [5] U at the end of the landing year, [6] U two years on, [7] GDP two years on
            float expectedApplied = shocked[0] * (-2f / 100f);
            if (Mathf.Abs(shocked[1] - expectedApplied) > 1e-4f * Mathf.Abs(expectedApplied) || shocked[2] != 0f) { Debug.LogError($"EVENT PATH: after the year the shock had landed {shocked[1]:F4} of {expectedApplied:F4} with {shocked[2]} days left."); ok = false; }
            if (!(shocked[3] < untouched[3])) { Debug.LogError($"EVENT PATH: GDP mid-year is not below the untouched run ({shocked[3]:F2} vs {untouched[3]:F2})."); ok = false; }
            float gapMid = shocked[4] - untouched[4], gapTwoOn = shocked[6] - untouched[6];
            if (!(gapMid > 0f)) { Debug.LogError($"EVENT PATH: unemployment mid-year is not above the untouched run ({gapMid:+0.000}) - Okun did not see the fall."); ok = false; }
            if (!(gapTwoOn < gapMid)) { Debug.LogError($"EVENT PATH: the unemployment gap two years on ({gapTwoOn:+0.000}) is not below mid-year's ({gapMid:+0.000}) - the recovery did not lower it."); ok = false; }

            // (3) the boundary's events checkpoint moves GDP by nothing
            SimulationRandom.Seed(777);
            World w3 = WorldFactory.CreateDefault();
            var go = new GameObject("EVENTPATH3");
            var gdpAtClose = new Dictionary<CountryId, float>();
            int eventsChecked = 0, eventsMoved = 0; float worst = 0f;
            System.Action<Country, string> ledger = (c, step) =>
            {
                if (step == "close") { gdpAtClose[c.Id] = c.State.GDP; }
                if (step == "events" && gdpAtClose.TryGetValue(c.Id, out float before)) { eventsChecked++; float rel = Mathf.Abs(c.State.GDP - before) / Mathf.Max(1f, before); if (rel > 1e-6f) { eventsMoved++; } if (rel > worst) { worst = rel; } }
            };
            SimulationManager.BoundaryLedger += ledger;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(w3);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in w3.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= Turns; year++) { for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); } sim.AdvanceTurn(decisions); }
            }
            finally { SimulationManager.BoundaryLedger -= ledger; Object.DestroyImmediate(go); }
            if (eventsChecked != 6 * Turns || eventsMoved != 0) { Debug.LogError($"EVENT PATH: the events checkpoint moved GDP in {eventsMoved} of {eventsChecked} boundaries (worst {worst:E2} relative) - the boundary still jumps."); ok = false; }

            Debug.Log($"EVENT PATH (§404): the −2 % event '{shock.Name}' on Sweden - armed at {expectedPerDay:F4} a day for {EventSystem.ShockDurationDays} days, GDP untouched at the boundary; landed {shocked[1]:F3} of {expectedApplied:F3} over the year; GDP mid-year {untouched[3]:F1} → {shocked[3]:F1}, unemployment mid-year {untouched[4]:F3} → {shocked[4]:F3} (+{gapMid:F3}), at the year's end {untouched[5]:F3} → {shocked[5]:F3}, two years on +{gapTwoOn:F3}; GDP two years on {untouched[7]:F1} → {shocked[7]:F1}. The events checkpoint moved GDP in {eventsMoved} of {eventsChecked} boundaries.");
            Debug.Log(ok ? "EVENT PATH: PASS - the shock enters over the days, in full; Okun sees the fall and the recovery; the boundary no longer moves GDP." : "EVENT PATH: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] RunSweden(EconomicEvent shock)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("EVENTPATH");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.AiFinanceMinistryEnabled = false;
                sim.PlayerCountryId = CountryId.Sweden;
                Country c = world.GetCountry(CountryId.Sweden);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var r = new float[8];
                for (int year = 1; year <= Turns; year++)
                {
                    // the GDP shock is ARMED by hand here rather than through EventSystem.ApplyEvent: ApplyEvent also lands the approval and inflation shocks, and an approval
                    // move outside the manager's own event step is unexplained to the approval ledger's self-audit (ATTRIB) - the first bar caught exactly that
                    if (year == Landing && shock != null) { r[0] = c.State.GDP; c.State.EventGdpShockPerDay = c.State.GDP * (shock.GdpShockPercent / 100f) / EventSystem.ShockDurationDays; c.State.EventGdpShockDaysLeft = EventSystem.ShockDurationDays; c.State.EventGdpShockAppliedThisPeriod = 0f; }
                    else if (year == Landing) { r[0] = c.State.GDP; }
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++)
                    {
                        sim.AdvanceDay();
                        if (year == Landing && day == SimulationManager.DaysPerTurn / 2) { r[3] = c.State.GDP; r[4] = c.State.Unemployment; }
                    }
                    if (year == Landing) { r[1] = c.State.EventGdpShockAppliedThisPeriod; r[2] = c.State.EventGdpShockDaysLeft; r[5] = c.State.Unemployment; }
                    sim.AdvanceTurn(decisions);
                    if (year == Landing + 2) { r[6] = c.State.Unemployment; r[7] = c.State.GDP; }
                }
                return r;
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
