using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-3k (§638, ruled): ELECTIONS READ THE GOVERNMENT'S RECORD, ASSERTED against the literature's own figures (`docs/reference/ECONOMIC_VOTE.md`):
    /// at Sweden's start (M+KD+L, a coalition minority, with SD's support) the prime minister's party carries .030 per unit of deterioration (Table 9.4),
    /// each partner .057 x its portfolio share (Gamson's estimate) - .007, floored at zero (Figure 9.7), SD - the authored support rule - .057 x w x its
    /// legislative-seat share with w = (2 + 2)/4 (fn 237, p. 269) capped at the smallest partner's; no magnitude anywhere negative; the opposition none; the USA's unified government at the epoch .10 on the president's party and the divided branch .06 (Table 9.1); a neutral economy moves no one; the shift reaches the
    /// no-campaign prediction as a distribution summing to one and moves the governing parties the right way.
    /// </summary>
    public static class EconomicVoteDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== EconomicVoteDiagnostic (PS-3k, §638): the government's record in the vote ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            try
            {
                WorldClock.ApplyStart(CountryId.Sweden);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                Country sweden = world.GetCountry(CountryId.Sweden);
                int Seats(string p) => sweden.ParliamentSeats.TryGetValue(p, out int x) ? x : 0;
                int members = 0; foreach (KeyValuePair<string, int> kv in sweden.ParliamentSeats) { members += kv.Value; }
                int cabinet = Seats("M") + Seats("KD") + Seats("L");
                Dictionary<string, double> e = EconomicVote.Magnitudes(sweden);
                foreach (KeyValuePair<string, double> kv in e) { sb.Append(F("    e         {0} {1:0.0000}\n", kv.Key, kv.Value)); }
                Check(cabinet < members / 2 + 1 && Math.Abs(e["M"] - 0.030) < 1e-12, F("M, prime minister's party of a coalition minority: .030 (Table 9.4) - {0:0.000}", e["M"]));
                double kd = 0.057 * Seats("KD") / cabinet - 0.007, l = 0.057 * Seats("L") / cabinet - 0.007;
                Check(Math.Abs(e["KD"] - kd) < 1e-12 && Math.Abs(e["L"] - l) < 1e-12, F("the partners by the Figure 9.7 line: KD {0:0.0000}, L {1:0.0000}", e["KD"], e["L"]));
                double sd = Math.Min(Math.Min(kd, l), 0.057 * ((2 + 2) / 4.0) * Seats("SD") / members);
                Check(Math.Abs(e["SD"] - sd) < 1e-12 && e["SD"] <= e["KD"] && e["SD"] <= e["L"], F("SD, the support party (authored): .057 x w x its legislative-seat share, w = 1, capped at the smallest partner's - {0:0.0000}", e["SD"]));
                foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.USA })
                {
                    foreach (KeyValuePair<string, double> kv in EconomicVote.Magnitudes(world.GetCountry(id))) { if (kv.Value < 0.0) { Check(false, F("{0}'s {1} carries a NEGATIVE magnitude {2:0.0000} - a worse economy would pay it", id, kv.Key, kv.Value)); } }
                }
                Check(true, "no governing party in any country gains from a worse economy (the partner line floored at zero)");
                Check(!e.ContainsKey("S") && !e.ContainsKey("V") && !e.ContainsKey("C") && !e.ContainsKey("MP"), "the opposition carries none");
                Dictionary<string, double> worse = EconomicVote.RecordShiftOf(sweden, 25.0), neutral = EconomicVote.RecordShiftOf(sweden, 50.0);
                Check(Math.Abs(worse["M"] + 0.015) < 1e-12 && worse["SD"] < 0.0, F("half a category worse (index 25): M {0:+0.0000;-0.0000}, SD {1:+0.0000;-0.0000}", worse["M"], worse["SD"]));
                bool zero = true; foreach (KeyValuePair<string, double> kv in neutral) { if (kv.Value != 0.0) { zero = false; } }
                Check(zero, "a neutral economy moves no one");

                // The no-campaign prediction reads it: a distribution, the governing parties down on a bad record.
                bool ranPlain = NationalElection.TryPredictShares(CountryId.Sweden, out Dictionary<string, double> plain);
                bool ranJudged = NationalElection.TryPredictShares(CountryId.Sweden, out Dictionary<string, double> judged, worse);
                Check(ranPlain && ranJudged, "the prediction runs with and without the record");
                if (!ranPlain || !ranJudged) { throw new InvalidOperationException("no prediction for Sweden"); }
                double sum = 0.0; foreach (KeyValuePair<string, double> kv in judged) { sum += kv.Value; }
                Check(Math.Abs(sum - 1.0) < 1e-9 && judged["S"] > plain["S"], F("on a bad record S {0:P2} -> {1:P2}; the shares sum to one", plain["S"], judged["S"]));
                foreach (string p in new[] { "M", "KD", "L", "SD" })
                {
                    double delivered = judged[p] - plain[p];
                    Check(Math.Abs(delivered - worse[p]) < 1e-9, F("{0} {1:P2} -> {2:P2}: moved {3:+0.0000;-0.0000}, its shift {4:+0.0000;-0.0000} delivered exactly", p, plain[p], judged[p], delivered, worse[p]));
                }
                var keysNeutral = new List<string>(plain.Keys); var arr = new double[keysNeutral.Count];
                for (int i = 0; i < arr.Length; i++) { arr[i] = plain[keysNeutral[i]]; }
                Check(ReferenceEquals(EconomicVote.ApplyRecordShift(keysNeutral, arr, neutral), arr), "a neutral record leaves the shares untouched, to the bit");

                // The campaign's form, by position: governing parties carry their shift, the rest NaN and absorb - delivered exactly, as by key.
                var byIndex = new double[arr.Length];
                for (int i = 0; i < arr.Length; i++) { byIndex[i] = worse.TryGetValue(keysNeutral[i], out double by) ? by : double.NaN; }
                double[] campaignForm = EconomicVote.ApplyRecordShiftByIndex(arr, byIndex);
                int iM = keysNeutral.IndexOf("M"), iS = keysNeutral.IndexOf("S");
                double csum = 0.0; foreach (double v in campaignForm) { csum += v; }
                Check(Math.Abs(campaignForm[iM] - arr[iM] - worse["M"]) < 1e-12 && campaignForm[iS] > arr[iS] && Math.Abs(csum - 1.0) < 1e-9, F("the campaign's form moves M by exactly {0:+0.0000;-0.0000} and S up, summing to one", worse["M"]));
                var zeros = new double[arr.Length]; for (int i = 0; i < zeros.Length; i++) { zeros[i] = double.IsNaN(byIndex[i]) ? 0.0 : byIndex[i]; }
                Check(ReferenceEquals(EconomicVote.ApplyRecordShiftByIndex(arr, zeros), arr), "a shift naming every party (zeros for the opposition) has no one to absorb it and moves nothing - why the campaign's form carries NaN");
                Country other = world.GetCountry(CountryId.Sweden);
                Check(true, F("the start's perceived index {0:0.0}", PerceivedPerformance.Perceived(other, null).Index));

                // The USA at the epoch (1 Oct 2026): the president's party (REP, Trump) also holds the House of record - unified government, .10 (Table 9.1),
                // carried by the president's party alone (p. 266 fn 233). The divided branch: the same record with the House handed to the other party.
                Country usa = world.GetCountry(CountryId.USA);
                foreach (KeyValuePair<string, int> kv in usa.ParliamentSeats) { sb.Append(F("    House     {0} {1}\n", kv.Key, kv.Value)); }
                Dictionary<string, double> eu = EconomicVote.Magnitudes(usa);
                Check(usa.Government.PmParty == "REP" && eu.Count == 1 && eu.TryGetValue("REP", out double pres) && Math.Abs(pres - 0.10) < 1e-12, F("the USA: {0}, the president's party, holds the House - .10 under unified government", usa.Government.PmParty));
                var saved = new Dictionary<string, int>(usa.ParliamentSeats);
                try
                {
                    int rep = usa.ParliamentSeats["REP"], dem = usa.ParliamentSeats["DEM"];
                    usa.ParliamentSeats["REP"] = dem; usa.ParliamentSeats["DEM"] = rep;
                    Dictionary<string, double> divided = EconomicVote.Magnitudes(usa);
                    Check(divided.Count == 1 && Math.Abs(divided["REP"] - 0.06) < 1e-12, "the House handed to DEM: divided government, .06 on the president's party");
                }
                finally { foreach (KeyValuePair<string, int> kv in saved) { usa.ParliamentSeats[kv.Key] = kv.Value; } }
            }
            catch (Exception ex) { failures++; sb.Append("    THREW: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace + "\n"); }
            finally { EnergyMarket.ResetTurnState(); }
            if (failures > 0) { Debug.LogError($"ECONOMIC VOTE: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
