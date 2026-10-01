using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §707 (D-DE, boards 24a-24b): GERMANY'S ELECTION NIGHT, its arithmetic. (a) The night's own count - `ElectionNight.At` on the official
    /// 2025 Land returns with the Bundestag's rules (pure Sainte-Laguë, 630, 5 %, the SSW exempt) - reproduces the Bundestag exactly, as 24a's
    /// note measured it (CDU 164 · AfD 152 · SPD 120 · GRÜNE 85 · LINKE 64 · CSU 44 · SSW 1), and calls no line against the exempt list.
    /// (b) Board 24b's table as the view carries it: sixteen tiles in the game's Land order, each tile's area k × its 2025 register
    /// (k = 2,689.7 px² per million at 1280), inside the map's extent. (c) The Union's one ink (Q3) and a German ink for every seated list.
    /// </summary>
    public static class GermanNightDiagnostic
    {
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== GermanNightDiagnostic (§707): Germany's election night, its arithmetic ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                // (a) the night's count on the official returns
                string[] names = GermanLandReturns2025.Names;
                string[] parties = GermanLandReturns2025.Parties;
                int regions = names.Length;
                var eligible = new long[regions];
                var arrivals = new int[regions];
                for (int r = 0; r < regions; r++) { eligible[r] = GermanRegions.EligibleForElection(r, new DateTime(2025, 2, 23)); arrivals[r] = r + 1; }
                var exempt = new bool[parties.Length];
                for (int p = 0; p < parties.Length; p++) { exempt[p] = NationalElection.ExemptFromThreshold(CountryId.Germany, parties[p]); }
                NightState night = ElectionNight.At(regions, names, GermanLandReturns2025.Votes, GermanLandReturns2025.Valid, eligible, arrivals, 630, 0.05, parties,
                    SeatAllocation.SainteLagueDivisor, null, exempt);
                var want = new Dictionary<string, int> { { "CDU", 164 }, { "AfD", 152 }, { "SPD", 120 }, { "Grune", 85 }, { "Linke", 64 }, { "CSU", 44 }, { "SSW", 1 }, { "BSW", 0 }, { "FDP", 0 } };
                bool exact = night.Complete;
                var got = new List<string>();
                for (int p = 0; p < parties.Length; p++)
                {
                    got.Add(parties[p] + " " + night.SeatsOnCounted[p].ToString(CultureInfo.InvariantCulture));
                    if (!want.TryGetValue(parties[p], out int w) || w != night.SeatsOnCounted[p]) { exact = false; }
                }
                Check(exact, F("(a) the night's count on the official 2025 Land returns - Sainte-Laguë, 630, 5 %, the SSW exempt - reproduces the Bundestag: {0}", string.Join(" · ", got)));
                int ssw = Array.IndexOf(parties, "SSW");
                bool sswCalledOut = night.Calls.Exists(c => c.Party == ssw && (c.Kind == CallKind.ThresholdMissed || c.Kind == CallKind.ThresholdCleared));
                Check(ssw >= 0 && !sswCalledOut && night.SeatsOnCounted[ssw] == 1,
                    "(a) the SSW is called against no line (§ 4 Abs. 2 Satz 3 BWahlG) and holds its seat");

                // (b) the tiles: the game's Land order, area = k × register, inside the extent
                const double k = 2689.7;
                bool order = LaenderTileView.Table.Length == regions;
                double worst = 0.0;
                for (int r = 0; r < regions && order; r++)
                {
                    (string code, string label, float x, float y, float w, float h) = LaenderTileView.Table[r];
                    if (!string.Equals(label, names[r].ToUpperInvariant(), StringComparison.Ordinal)) { order = false; break; }
                    double area = (double)w * h;
                    double due = k * eligible[r] / 1e6;
                    worst = Math.Max(worst, Math.Abs(area - due) / due);
                    if (x < -0.01f || y < -0.01f || x + w > LaenderTileView.BoardWidth + 0.05f || y + h > LaenderTileView.BoardHeight + 0.05f) { order = false; }
                }
                Check(order && worst < 0.005, F("(b) board 24b's sixteen tiles in the game's Land order, inside the {0}×{1} extent, each tile's area k × its 2025 register within {2:P2} (k = 2,689.7 px² per million)",
                    LaenderTileView.BoardWidth, LaenderTileView.BoardHeight, worst));

                // (c) the inks
                Color cdu = PoliSimTheme.PartyLaddered(CountryId.Germany, "CDU"), csu = PoliSimTheme.PartyLaddered(CountryId.Germany, "CSU");
                bool allInked = true;
                foreach (string p in parties) { if (!PoliSimTheme.HasPartyInk(CountryId.Germany, p)) { allInked = false; } }
                Check(allInked && cdu == csu, F("(c) every German list inked (24a's legend, [AUTHORED-REFERENCE]); the Union one ink - CDU #{0}, CSU #{1}", ColorUtility.ToHtmlStringRGB(cdu), ColorUtility.ToHtmlStringRGB(csu)));
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            if (failures > 0) { Debug.LogError($"GERMAN NIGHT: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
