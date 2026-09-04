using System.Collections.Generic;
using System.Reflection;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P4-D2 (2026-09-04): the event pool held to its own rules. Every event has a name, a line, a tag and an
    /// analogue that opens with one of the law catalog's citation classes; its GDP shock sits inside the pool's
    /// ceiling; its band is derived (so this check prints the band spread rather than asserting an authored one);
    /// the pool is at least the doubled size the row asked for; and the pool's first entry keeps its name, because
    /// the screenshot driver stages `EventPool[0]` for the Desk's live card and a reordered pool would silently
    /// change a film. Reads the private pool by reflection, the driver's own idiom.
    /// </summary>
    public static class EventPoolCheck
    {
        private const int MinimumPoolSize = 48;   // P4-D2: "the pool doubles at minimum" - 24 became 50
        private const string StagedFirstEvent = "Recession in a Trading Partner";
        private static readonly string[] CitationClasses = { "CONFIRMED:", "CONFIRMED-DIRECTION:", "DIRECTIONAL:", "GENRE-IDIOM:" };

        public static void Run()
        {
            FieldInfo poolField = typeof(EventSystem).GetField("EventPool", BindingFlags.Static | BindingFlags.NonPublic);
            var pool = poolField?.GetValue(null) as List<EconomicEvent>;
            if (pool == null)
            {
                Debug.LogError("EVENTPOOL: EventSystem.EventPool not found by reflection - the driver's staging is broken too.");
                CheckExit.Finish(1);
                return;
            }

            int failures = 0;
            var names = new HashSet<string>();
            var bandCount = new Dictionary<EventBand, int>();
            var tagCount = new Dictionary<EventTag, int>();
            foreach (EconomicEvent e in pool)
            {
                string id = string.IsNullOrEmpty(e.Name) ? "(unnamed)" : e.Name;
                if (string.IsNullOrWhiteSpace(e.Name) || string.IsNullOrWhiteSpace(e.Description)) { Debug.LogError($"EVENTPOOL: {id} lacks a name or a line."); failures++; }
                if (!names.Add(e.Name)) { Debug.LogError($"EVENTPOOL: {id} is named twice."); failures++; }
                bool cited = false;
                foreach (string c in CitationClasses) { if (e.Analogue != null && e.Analogue.StartsWith(c, System.StringComparison.Ordinal)) { cited = true; break; } }
                if (!cited) { Debug.LogError($"EVENTPOOL: {id} has no analogue, or one without a citation class (CONFIRMED / CONFIRMED-DIRECTION / DIRECTIONAL / GENRE-IDIOM)."); failures++; }
                if (Mathf.Abs(e.GdpShockPercent) > EventBands.Ceiling) { Debug.LogError($"EVENTPOOL: {id} carries a GDP shock of {e.GdpShockPercent:0.0} %, past the pool's ceiling of {EventBands.Ceiling:0.0}."); failures++; }
                if (Mathf.Approximately(e.GdpShockPercent, 0f) && Mathf.Approximately(e.InflationShockPoints, 0f) && Mathf.Approximately(e.ApprovalEffect, 0f)) { Debug.LogError($"EVENTPOOL: {id} moves nothing."); failures++; }
                bandCount[e.Band] = (bandCount.TryGetValue(e.Band, out int b) ? b : 0) + 1;
                tagCount[e.Tag] = (tagCount.TryGetValue(e.Tag, out int t) ? t : 0) + 1;
            }
            if (pool.Count < MinimumPoolSize) { Debug.LogError($"EVENTPOOL: {pool.Count} events, below the {MinimumPoolSize} the row set."); failures++; }
            if (pool.Count == 0 || pool[0].Name != StagedFirstEvent) { Debug.LogError($"EVENTPOOL: the first event is not '{StagedFirstEvent}' - the Desk film stages EventPool[0]; keep it first or re-stage the driver."); failures++; }

            var bands = new List<string>();
            foreach (EventBand band in System.Enum.GetValues(typeof(EventBand))) { bands.Add($"{band} {(bandCount.TryGetValue(band, out int n) ? n : 0)}"); }
            var tags = new List<string>();
            foreach (EventTag tag in System.Enum.GetValues(typeof(EventTag))) { tags.Add($"{tag} {(tagCount.TryGetValue(tag, out int n) ? n : 0)}"); }
            Debug.Log($"EVENTPOOL: {pool.Count} events · bands {string.Join(", ", bands)} · tags {string.Join(", ", tags)} · rate {EventSystem.EventChancePerTurn:0.00} per country per turn (one turn is one year).");
            Debug.Log(failures == 0 ? "=== EventPoolCheck: ALL ASSERTIONS PASS ===" : $"=== EventPoolCheck: {failures} failure(s) ===");
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }
    }
}
