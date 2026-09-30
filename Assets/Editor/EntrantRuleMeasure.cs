using System;
using System.Collections.Generic;
using PoliSim.Elections;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §684 (Elias's ruling, 2026-09-30): **THE MEASURED ALTERNATIVE, AGAINST THE SAME GATES.** Sets the entrant layer's rule to similarity by
    /// position over the nine dimensions for every entrant (`EntrantLayer.Rule.SimilarityForAll`), runs the gate diagnostic, the created-party
    /// campaign re-test and every elections backtest in one launch, and puts the rule back. <c>-executeMethod PoliSim.EditorTools.EntrantRuleMeasure.RunSimilarity</c>.
    /// Compare the backtests' figures with `ElectionsBacktests.RunBatch` under the default rule - the gate is "every backtest no worse".
    /// </summary>
    public static class EntrantRuleMeasure
    {
        public static void RunSimilarity()
        {
            EntrantLayer.Rule was = EntrantLayer.Active;
            EntrantLayer.Active = EntrantLayer.Rule.SimilarityForAll;
            int worst;
            try
            {
                Debug.Log("ENTRANT RULE: measuring SimilarityForAll against the gates.");
                var table = new List<(string Name, Action Run)>
                {
                    ("EntrantLayerDiagnostic", EntrantLayerDiagnostic.Run),
                    ("CreatedPartyCampaignDiagnostic", CreatedPartyCampaignDiagnostic.Run),
                };
                table.AddRange(ElectionsBacktests.Set);
                worst = CheckSuite.RunTable(table.ToArray(), "entrant rule: similarity", announceClean: true);
            }
            finally { EntrantLayer.Active = was; }
            Debug.Log($"ENTRANT RULE: SimilarityForAll measured, exiting {worst} (OutOfSample2026Diagnostic refuses by design).");
            EditorApplication.Exit(worst);
        }
    }
}
