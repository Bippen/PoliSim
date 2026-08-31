using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// ANNEX G's counting half (the Policy Web micro-pass, 2026-08-28, R-W3: the annexes are
    /// measurements, not prose). Counts the web's nodes and edges from the same public API the
    /// screen itself draws from - never from a doc's cached figure (rule 12):
    /// - nodes per wedge (the nine WedgeDef groups: eight policy areas + Stats), plus any node
    ///   that draws no edge (the Tariffs enum-member case), stated by name;
    /// - policy → stat edges per country, split DERIVED / DECLARED (GetEdgesFor with the real
    ///   Country object, so IsLiveFor's per-country predicate is exercised, not restated);
    /// - the causal graph's stat → stat edges (all derived by construction), deduplicated across
    ///   the per-stat enumeration.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.PolicyWebCensus.Run -logFile &lt;path&gt;`. Rendering-free on purpose:
    /// no GUIStyle, no CalcSize - label pixel sizes are the film's to measure (Annex G's other
    /// half), and a census that needed a graphics device would confound the two.
    /// </summary>
    public static class PolicyWebCensus
    {
        public static void Run()
        {
            World world = WorldFactory.CreateDefault();

            // Nodes per policy area (the wedge grouping), and the no-edge nodes by name.
            var areaCounts = new Dictionary<UiPalette.SystemArea, int>();
            var edgeless = new List<string>();
            int policyNodes = 0;
            foreach (PolicyNodeId node in System.Enum.GetValues(typeof(PolicyNodeId)))
            {
                policyNodes++;
                UiPalette.SystemArea area = PolicyWebRenderer.GetPolicyArea(node);
                areaCounts.TryGetValue(area, out int c);
                areaCounts[area] = c + 1;
                if (PolicyWebRenderer.GetEdgesFor(node).Count == 0)
                {
                    edgeless.Add(PolicyWebRenderer.GetPolicyName(node));
                }
            }

            int statNodes = System.Enum.GetValues(typeof(StatNodeId)).Length;
            Debug.Log($"WEBCENSUS: nodes = {policyNodes} policy + {statNodes} stat = {policyNodes + statNodes}");
            foreach (KeyValuePair<UiPalette.SystemArea, int> kv in areaCounts)
            {
                Debug.Log($"WEBCENSUS: wedge {kv.Key} = {kv.Value} node(s)");
            }

            Debug.Log($"WEBCENSUS: wedge Stats = {statNodes} node(s)");
            Debug.Log($"WEBCENSUS: edge-less node(s): {(edgeless.Count == 0 ? "none" : string.Join("; ", edgeless))}");

            // Policy -> stat edges: the full set (country = null), then per country through the
            // same GetEdgesFor the detail panel calls, split by provenance.
            CountEdges(null, "full set (no country)");
            foreach (CountryId id in System.Enum.GetValues(typeof(CountryId)))
            {
                Country country = world.GetCountry(id);
                if (country == null)
                {
                    Debug.Log($"WEBCENSUS: {id} not in the default world - skipped, stated rather than silent");
                    continue;
                }

                CountEdges(country, id.ToString());
            }

            // Stat -> stat edges (the causal graph's own kind), deduplicated: GetStatEdgesFor
            // returns each edge from both of its ends.
            var seen = new HashSet<string>();
            int statEdges = 0;
            foreach (StatNodeId stat in System.Enum.GetValues(typeof(StatNodeId)))
            {
                foreach (StatWebEdge edge in PolicyWebRenderer.GetStatEdgesFor(stat))
                {
                    if (seen.Add($"{edge.Source}>{edge.Target}>{edge.LedgerTerm}"))
                    {
                        statEdges++;
                    }
                }
            }

            Debug.Log($"WEBCENSUS: stat->stat edges (causal graph, all derived) = {statEdges}");

            // C-C3 (P-F1): R-W2's fence, ASSERTED rather than described. Focus mode encodes weight as
            // line thickness, so the fence's demand that "every encoded weight traces to the coupling
            // table" becomes a testable property: every edge's RelativeStrength must be a real number
            // in [0,1]. A NaN, a negative or an out-of-range weight would still DRAW - Mathf.Clamp01
            // swallows it silently - and the line would then assert a magnitude the table never gave.
            int badWeights = 0;
            foreach (PolicyWebEdge edge in PolicyWebRenderer.GetAllEdges())
            {
                if (float.IsNaN(edge.RelativeStrength) || edge.RelativeStrength < 0f || edge.RelativeStrength > 1f)
                {
                    badWeights++;
                    Debug.LogError($"WEBCENSUS: {edge.Source} -> {edge.Target} carries RelativeStrength {edge.RelativeStrength}, not a coupling-table weight in [0,1]. Clamp01 would hide it and the line would draw a magnitude the model never stated.");
                }
            }

            foreach (StatWebEdge edge in PolicyWebRenderer.GetAllStatEdges())
            {
                if (float.IsNaN(edge.RelativeStrength) || edge.RelativeStrength < 0f || edge.RelativeStrength > 1f)
                {
                    badWeights++;
                    Debug.LogError($"WEBCENSUS: {edge.Source} -> {edge.Target} carries RelativeStrength {edge.RelativeStrength}, outside [0,1].");
                }
            }

            Debug.Log(badWeights == 0
                ? "WEBCENSUS: every edge weight is a real number in [0,1] - thickness encodes the coupling table's own magnitude and nothing else (R-W2)."
                : $"WEBCENSUS: {badWeights} edge weight(s) outside [0,1].");

            Debug.Log("=== PolicyWebCensus: done ===");
            CheckExit.Finish(badWeights == 0 ? 0 : 1);
        }

        private static void CountEdges(Country country, string label)
        {
            int derived = 0, declared = 0;
            foreach (PolicyNodeId node in System.Enum.GetValues(typeof(PolicyNodeId)))
            {
                foreach (PolicyWebEdge edge in PolicyWebRenderer.GetEdgesFor(node, country))
                {
                    if (edge.Provenance == EdgeProvenance.Derived) { derived++; } else { declared++; }
                }
            }

            Debug.Log($"WEBCENSUS: policy->stat edges [{label}] = {derived + declared} ({derived} derived + {declared} declared)");
        }
    }
}
