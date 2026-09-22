using System;
using System.Collections.Generic;
using System.Text;
using PoliSim.Data;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §566 (2026-09-22, the sitting pass's Track 3) — **the six COUNTRY inks are a registered channel, and the D16 fence
    /// binds inside it.**
    ///
    /// <para><b>The finding.</b> Design's whole-game reading, part B item 5, on the political compass: *"six country discs
    /// in six unregistered inks (mauve, orange, violet, olive ×2) that appear nowhere else on the desk … the six hues are
    /// D9 row 5's per-channel floor being ignored."* Two of the six WERE the same olive to a reader - Poland's bronze
    /// `#826239` and the USA's ochre `#81641F` sat 8.5° apart in hue and 0.053 apart in lightness, inside the fence on
    /// both axes - because the country ink was not a channel at all: it was a lookup through
    /// <c>UiPalette.GetCountryArea</c>, so six countries wore six AREA accents chosen for other reasons entirely.</para>
    ///
    /// <para><b>The rule, which is the party inks' rule one level up</b> (Elias's ruling, 2026-09-22): the inks are
    /// REGISTERED - <see cref="UiPalette.GetCountryColor"/> reads a table of six that exists to be the country channel -
    /// and the fence is run on them. D16's fence: two inks of one channel must differ by at least <b>8.7°</b> of hue OR
    /// <b>0.08</b> of lightness. D9 row 5's ruling that *"the floor binds within a channel, not across them"* is why this
    /// check measures the six against EACH OTHER and against nothing else: a country ink never stands beside an area
    /// accent, and where one would (the selector's card, the fallback country buttons) the area channel is drawn.</para>
    ///
    /// <para><b>What it decides.</b> Every pair of the six, both axes, against the fence; a breach is a FAILURE with the
    /// pair named and measured. The table is printed on every run, closest pair first, so the margin is a fact of the
    /// record rather than a claim in one. The arithmetic is HSL - hue in degrees on the shorter arc, lightness as
    /// (max + min) / 2 - the same pair of axes D16 states the fence in.</para>
    /// </summary>
    public static class CountryInkFenceCheck
    {
        /// <summary>D16's per-channel fence: two inks of one channel part by at least this much hue, in degrees...</summary>
        private const float HueFloorDegrees = 8.7f;

        /// <summary>...or by at least this much lightness, either one being enough.</summary>
        private const float LightnessFloor = 0.08f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var ids = new List<CountryId>();
            foreach (CountryId id in Enum.GetValues(typeof(CountryId))) { ids.Add(id); }

            var pairs = new List<(string Name, float Hue, float Lightness, float Margin, bool Ok)>();
            int breaches = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                for (int j = i + 1; j < ids.Count; j++)
                {
                    Hsl(UiPalette.GetCountryColor(ids[i]), out float h1, out float l1);
                    Hsl(UiPalette.GetCountryColor(ids[j]), out float h2, out float l2);
                    float hueGap = Mathf.Abs(h1 - h2);
                    if (hueGap > 180f) { hueGap = 360f - hueGap; }
                    float lightnessGap = Mathf.Abs(l1 - l2);
                    bool ok = hueGap >= HueFloorDegrees || lightnessGap >= LightnessFloor;
                    if (!ok) { breaches++; }
                    float margin = Mathf.Max(hueGap / HueFloorDegrees, lightnessGap / LightnessFloor);
                    pairs.Add(($"{DisplayName.Of(ids[i].ToString())} / {DisplayName.Of(ids[j].ToString())}", hueGap, lightnessGap, margin, ok));
                }
            }

            pairs.Sort((a, b) => a.Margin.CompareTo(b.Margin));

            var sb = new StringBuilder();
            sb.Append($"=== CountryInkFenceCheck: {ids.Count} registered country inks, {pairs.Count} pairs, fence {HueFloorDegrees:F1} deg hue OR {LightnessFloor:F2} lightness ===\n");
            foreach (CountryId id in ids)
            {
                Hsl(UiPalette.GetCountryColor(id), out float h, out float l);
                sb.Append($"  {DisplayName.Of(id.ToString()),-16} #{ColorUtility.ToHtmlStringRGB(UiPalette.GetCountryColor(id))}  hue {h,6:F1}  lightness {l:F3}\n");
            }

            sb.Append("\n  pairs, closest first (a pair clears the fence on EITHER axis):\n");
            foreach ((string name, float hue, float lightness, float margin, bool ok) in pairs)
            {
                sb.Append($"  {name,-34} dHue {hue,6:F1}  dLightness {lightness:F3}  {margin,5:F2}x  {(ok ? "ok" : "** TOO CLOSE **")}\n");
            }

            if (breaches > 0)
            {
                sb.Append($"\n  {breaches} pair(s) inside the fence: two countries wear one ink on the compass, the map's chips and every legend that keys to them.\n");
            }

            sb.Append($"\n=== CountryInkFenceCheck: {(breaches == 0 ? "CLEAN - every pair clears the fence" : breaches + " pair(s) inside the fence")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(breaches == 0 ? 0 : 1);
        }

        /// <summary>Hue in degrees and lightness as (max + min) / 2 - the two axes D16 states the fence in.</summary>
        private static void Hsl(Color c, out float hue, out float lightness)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            lightness = (max + min) * 0.5f;
            float d = max - min;
            if (d <= 0f) { hue = 0f; return; }

            float h;
            if (Mathf.Approximately(max, c.r)) { h = 60f * Mathf.Repeat((c.g - c.b) / d, 6f); }
            else if (Mathf.Approximately(max, c.g)) { h = 60f * ((c.b - c.r) / d + 2f); }
            else { h = 60f * ((c.r - c.g) / d + 4f); }

            hue = Mathf.Repeat(h, 360f);
        }
    }
}
