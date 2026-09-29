using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §674 (ruled 2026-09-29): **THE TWO EXPERIMENTS ON SP-3's FAILED ASSERTION** - measured, asserted neither way, the model untouched. The vote model's
    /// own steps, called as `NationalElection.TryCompatibility` calls them (`VoteModel.PredictShares` on the placed parties, the compatibility scale,
    /// `LoyaltyModel.PartyLoyalties`, `PreferenceModel.Preference`), with the inputs each experiment names:
    /// (a) a party on M's positions with M's prior - "M's equivalent stats" in the only terms the vote model reads - and no loyal base;
    /// (b) the control: a real small party (L) moved to M's positions, its loyalty zeroed; recognition has no term in the vote model, so there is nothing to
    /// set to the newcomer's level. Each against the real roster, every party's change printed - the decomposition by source party.
    /// Run: `-executeMethod PoliSim.EditorTools.CreatedPartyExperiments.Run`. Not in a bar: an experiment, recorded (§674).
    /// </summary>
    public static class CreatedPartyExperiments
    {
        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        public static void Run()
        {
            var sb = new StringBuilder("=== CreatedPartyExperiments (§674): SP-3's (i), decomposed ===\n");
            int code = 0;
            using IDisposable epoch = SimulationManager.EpochScope();
            using IDisposable created = CreatedParties.Scope();
            try
            {
                WorldClock.ApplyStart(CountryId.Sweden);
                CreatedParties.Clear();
                MethodInfo toCompat = typeof(NationalElection).GetMethod("ToCompatScale", BindingFlags.Static | BindingFlags.NonPublic);
                if (toCompat == null) { throw new InvalidOperationException("NationalElection.ToCompatScale is not reachable"); }
                PartySystems.TryElectorate(CountryId.Sweden, out VoteModel.Electorate electorate, out double econ);
                PartySystems.TryHistory(CountryId.Sweden, out double[] latest, out double[] previous);
                PoliticalParty[] real = PartySystems.RealRoster(CountryId.Sweden);
                double[] loyalty = LoyaltyModel.PartyLoyalties(latest, previous);
                int m = Array.FindIndex(real, p => p.Abbrev == "M"), l = Array.FindIndex(real, p => p.Abbrev == "L");

                // The model's own steps over given inputs; returns the shares, the persuaded shares and the pre-normalisation total.
                (double[] Shares, double[] Persuaded) Predict(IList<(string Key, float Econ, float Gal)> pts, double[] prior, double[] loyal)
                {
                    var points = new VoteModel.PartyPoint[pts.Count];
                    for (int i = 0; i < pts.Count; i++) { points[i] = new VoteModel.PartyPoint(pts[i].Key, pts[i].Econ, pts[i].Gal); }
                    double[] compat = (double[])toCompat.Invoke(null, new object[] { VoteModel.PredictShares(points, electorate, econ) });
                    return (PreferenceModel.Preference(compat, prior, loyal), PreferenceModel.PersuadedShares(compat));
                }
                var basePts = new List<(string, float, float)>();
                foreach (PoliticalParty p in real) { basePts.Add((p.Abbrev, p.LrEcon, p.Galtan)); }
                (double[] baseShares, double[] basePers) = Predict(basePts, latest, loyalty);
                string Row(string[] keys, double[] shares, double[] before)
                {
                    var parts = new List<string>();
                    for (int i = 0; i < keys.Length; i++) { parts.Add(F("{0} {1:0.00}{2}", keys[i], shares[i] * 100.0, i < before.Length ? F(" ({0:+0.00;-0.00})", (shares[i] - before[i]) * 100.0) : " (new)")); }
                    return string.Join(", ", parts);
                }
                string[] realKeys = Array.ConvertAll(real, p => p.Abbrev);
                sb.Append("    baseline  ").Append(Row(realKeys, baseShares, baseShares)).Append('\n');
                sb.Append(F("    baseline  M's loyalty {0:0.0}, its persuaded share {1:0.000} % (λ·prior + (1-λ)·persuaded, then the whole vector renormalised)\n", loyalty[m], basePers[m] * 100.0));

                // (a) M's equivalent, no loyal base: M's positions and M's prior, loyalty 0.
                foreach ((string label, double prior) in new[] { ("prior = M's", latest[m]), ("prior = 0 (§671's test)", 0.0) })
                {
                    var pts = new List<(string, float, float)>(basePts) { ("NM", real[m].LrEcon, real[m].Galtan) };
                    var pr = new double[latest.Length + 1]; Array.Copy(latest, pr, latest.Length); pr[latest.Length] = prior;
                    var lo = new double[loyalty.Length + 1]; Array.Copy(loyalty, lo, loyalty.Length); lo[loyalty.Length] = 0.0;
                    (double[] s, double[] pers) = Predict(pts, pr, lo);
                    string[] keys = new string[realKeys.Length + 1]; Array.Copy(realKeys, keys, realKeys.Length); keys[realKeys.Length] = "NM";
                    sb.Append(F("    (a) {0,-24}", label)).Append(Row(keys, s, baseShares)).Append('\n');
                    sb.Append(F("        NM's persuaded share {0:0.000} % = its share before renormalising; M's persuaded {1:0.000} % - the two split the spatial share they sit on\n", pers[latest.Length] * 100.0, pers[m] * 100.0));
                }

                // (b) the control: L moved to M's positions, L's loyalty zeroed.
                {
                    var pts = new List<(string, float, float)>(basePts); pts[l] = ("L", real[m].LrEcon, real[m].Galtan);
                    var lo = (double[])loyalty.Clone(); lo[l] = 0.0;
                    (double[] s, double[] pers) = Predict(pts, latest, lo);
                    sb.Append("    (b) L at M's place, λ 0 ").Append(Row(realKeys, s, baseShares)).Append('\n');
                    sb.Append(F("        L's persuaded share {0:0.000} %; recognition has no term in the vote model - nothing to set\n", pers[l] * 100.0));
                }
            }
            catch (Exception e) { code = 1; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n"); }
            finally { CreatedParties.Clear(); NationalElection.TryPredictShares(CountryId.Sweden, out _); }
            Debug.Log(sb.ToString());
            CheckExit.Finish(code);
        }
    }
}
