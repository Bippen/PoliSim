using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-B2 — **the harness `PoliSimTheme` already said was checking this, and was not.**
    ///
    /// `PoliSimTheme.PartyHues`' own doc comment ends: *"the desk-seated hues below are checked
    /// against the area accents by `PartyInkHarness`."* ⚠ **No such file existed.** The constraint it
    /// names is real and inherited from the four archetype inks W-G1 replaced — those were cut
    /// deliberately in hue space the eleven area accents do not occupy, so that **a party can never
    /// print in an area's semantic colour** — but nothing was enforcing it on the eight sourced hues
    /// that replaced them. This is that enforcement, plus the export the R5 hex exchange needs.
    ///
    /// <para><b>The bar is DERIVED from the palette, not authored.</b> Inventing a "minimum
    /// separation" constant would be picking a number until the test passes. Instead the floor is
    /// <b>the closest the eleven area accents already sit to each other</b>: if two areas are
    /// mutually legible at that distance, a party ink at least that far from every area is at least
    /// as legible. The floor is measured at run time and printed, so it moves when the palette moves
    /// and can never be quietly relaxed.</para>
    ///
    /// <para>⚠ <b>Hue is the wrong axis for one accent and the harness says so rather than papering
    /// over it.</b> Neutral (<c>#6D7480</c>) sits at saturation ≈0.15 — nearly grey — where hue is
    /// numerically unstable and perceptually meaningless. A party ink is seated at saturation 0.52 by
    /// construction, so the two cannot be confused whatever their hues; Neutral is therefore compared
    /// on SATURATION and reported separately, never folded into a hue distance that would be
    /// arithmetic about nothing.</para>
    ///
    /// <para><b>It also prints the exchange.</b> D8-2 asks Design for a colour ruling on the five
    /// countries with no published table; the half that is ours is the eight Swedish inks as they
    /// actually render — published hue in, desk-seated hex out — and the count of parties carrying no
    /// ink at all. Both are printed here so the ask quotes a run rather than a hand-copied list.</para>
    /// </summary>
    public static class PartyInkHarness
    {
        /// <summary>Below this saturation a colour has no usable hue; it is compared on saturation instead.</summary>
        private const float GreyThreshold = 0.20f;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-B2: party inks against the area accents, and the R5 hex exchange ===\n");

            var areas = new List<(string Name, Color Ink, float H, float S, float V)>();
            foreach (UiPalette.SystemArea area in Enum.GetValues(typeof(UiPalette.SystemArea)))
            {
                Color ink = PoliSimTheme.Accent(area);
                Color.RGBToHSV(ink, out float h, out float s, out float v);
                areas.Add((area.ToString(), ink, h * 360f, s, v));
            }

            // The derived floor: the closest two CHROMATIC area accents already sit in hue.
            float floorDeg = float.MaxValue;
            string floorPair = "-";
            for (int i = 0; i < areas.Count; i++)
            {
                if (areas[i].S < GreyThreshold) { continue; }
                for (int j = i + 1; j < areas.Count; j++)
                {
                    if (areas[j].S < GreyThreshold) { continue; }
                    float d = HueGap(areas[i].H, areas[j].H);
                    if (d < floorDeg) { floorDeg = d; floorPair = areas[i].Name + " / " + areas[j].Name; }
                }
            }

            sb.Append(F("\n--- The derived floor ---\n    the closest two chromatic area accents sit {0:F1} deg apart in hue ({1}).\n",
                floorDeg, floorPair));
            sb.Append("    That is the bar: a party ink must be at least as far from every area accent\n");
            sb.Append("    as two areas already are from each other. Nothing here is an authored constant.\n");

            var greyAreas = new List<string>();
            foreach (var a in areas) { if (a.S < GreyThreshold) { greyAreas.Add(F("{0} (S {1:F2})", a.Name, a.S)); } }
            sb.Append(F("    Compared on SATURATION rather than hue, being near-grey: {0}\n",
                greyAreas.Count > 0 ? string.Join(", ", greyAreas) : "none"));

            int failures = 0;

            // ⚠ RECONCILED 2026-09-01. One counter called `pending` was adding up THREE unlike things —
            // hue-floor breaches, near-grey saturation proximity, and two parties rendering to one ink —
            // and the summary line then described the total as inks "inside the floor". It printed 7
            // while the per-row marks showed 6, and §138, the register and Design's own board all say
            // six. **The disagreement was the LABEL, not the measurement**, and the fix is to stop
            // conflating them: counted apart, the printed number and the printed rows check each other.
            int pending = 0;
            int floorBreaches = 0;
            int nearGrey = 0;
            var byInk = new Dictionary<string, List<string>>();
            int inked = 0, uninked = 0;
            var uninkedByCountry = new Dictionary<CountryId, int>();

            sb.Append("\n--- The eight sourced inks: published hue in, desk-seated hex out (the R5 exchange) ---\n");
            sb.Append("    party  published  desk-seated   hue    nearest area accent        gap\n");

            foreach (CountryId country in Enum.GetValues(typeof(CountryId)))
            {
                foreach (PoliticalParty p in PartySystems.For(country))
                {
                    if (!PoliSimTheme.HasPartyInk(country, p.Abbrev))
                    {
                        uninked++;
                        uninkedByCountry.TryGetValue(country, out int n);
                        uninkedByCountry[country] = n + 1;
                        continue;
                    }

                    inked++;
                    Color ink = PoliSimTheme.Party(country, p.Abbrev);
                    Color.RGBToHSV(ink, out float ph, out float ps, out float pv);
                    float pHue = ph * 360f;

                    float nearest = float.MaxValue;
                    string nearestName = "-";
                    foreach (var a in areas)
                    {
                        if (a.S < GreyThreshold) { continue; }
                        float d = HueGap(pHue, a.H);
                        if (d < nearest) { nearest = d; nearestName = a.Name; }
                    }

                    // ⚠ REPORTED, NOT ASSERTED (SeatAllocationBacktest's idiom). A party ink sitting
                    // too close to an area accent cannot be fixed here without either re-seating the
                    // published hue - which would stop being "the authority's colour" - or picking a
                    // replacement by eye, which is the invention D8-2 exists to avoid. So it is a
                    // PEND carrying its measurement, and the ruling is Design's. The harness stays
                    // able to go red for what IS ours: the export disagreeing with the theme.
                    //
                    // ⚠ D9 ROW 5, RULED BY DESIGN AND READ 2026-09-01: **the floor was the wrong
                    // constraint, so these PENDs are ANSWERED rather than fixed.** Design's words: the
                    // 8.7 deg floor "was derived to keep two AREA accents apart - chrome semantics that
                    // sit side by side in one rail, one masthead, one tab strip. Party inks never appear
                    // in that company", so "the floor binds within a channel, not across them". The
                    // measurement below stays printed because it is TRUE; what changes is that it is no
                    // longer a debt. What replaces it is structural and this harness cannot see it:
                    // (1) party ink is never drawn adjacent to an area accent, and (3) a party swatch
                    // forced into chrome draws in the neutral status ink. That is a DRAW-SITE assertion,
                    // sized as its own row rather than smuggled in here as a hue test.
                    bool ok = nearest >= floorDeg;
                    if (!ok) { floorBreaches++; }

                    // ⚠ TWO PARTIES, ONE INK. The seating keeps HUE and replaces saturation and value,
                    // so two published colours that differ only in how dark or vivid they are collapse
                    // onto the same desk ink. Recorded per rendered hex, because a hemicycle drawing
                    // two different parties in one colour is a defect a reader sees immediately.
                    string hex = ToHex(ink);
                    if (!byInk.TryGetValue(hex, out List<string> sharers)) { sharers = new List<string>(); byInk[hex] = sharers; }
                    sharers.Add(country + "/" + p.Abbrev);

                    // ⚠ The published value below is a SECOND copy of a fact `PoliSimTheme` owns, kept
                    // only so the export can print "published hue in, desk-seated hex out". A second
                    // copy that is merely trusted is how two sources of one fact drift apart - so it is
                    // re-seated through the same arithmetic and must reproduce the theme's own ink.
                    int published = PublishedOf(country, p.Abbrev);
                    Color reseated = DeskSeat(published);
                    if (published == 0 || !Same(reseated, ink))
                    {
                        failures++;
                        Debug.LogError(F("C-B2: the harness's published hue for {0}/{1} (#{2}) does not re-seat to the theme's own ink (#{3} vs #{4}) - the export would print a colour the game does not draw.",
                            country, p.Abbrev, ToHex(published), ToHex(reseated), ToHex(ink)));
                    }

                    sb.Append(F("    {0,-6} {1,-10} {2,-13} {3,5:F1}  {4,-24} {5,5:F1} {6}\n",
                        p.Abbrev, "#" + ToHex(PublishedOf(country, p.Abbrev)), "#" + ToHex(ink), pHue,
                        nearestName, nearest, ok ? "ok" : "** TOO CLOSE **"));

                    // The saturation argument against the near-grey accents, per party.
                    foreach (var a in areas)
                    {
                        if (a.S >= GreyThreshold) { continue; }
                        if (Math.Abs(ps - a.S) < 0.10f)
                        {
                            nearGrey++;
                            sb.Append(F("      ⚠ {0}'s ink sits at saturation {1:F2}, within 0.10 of near-grey accent {2} ({3:F2}).\n",
                                p.Abbrev, ps, a.Name, a.S));
                        }
                    }
                }
            }

            sb.Append(F("\n    Every desk-seated ink is at saturation {0:F2} and value {1:F2} by construction.\n",
                0.52f, 0.46f));

            sb.Append("\n--- ⚠ TWO PARTIES, ONE INK ---\n");
            int collisions = 0;
            foreach (var kv in byInk)
            {
                if (kv.Value.Count < 2) { continue; }
                collisions++;
                pending++;
                sb.Append(F("    #{0} is drawn for {1} DIFFERENT parties: {2}\n", kv.Key, kv.Value.Count, string.Join(", ", kv.Value)));
            }

            if (collisions == 0)
            {
                sb.Append("    none - every inked party renders to its own hex.\n");
            }
            else
            {
                sb.Append("    The seating keeps the published HUE and replaces saturation and value, so two\n");
                sb.Append("    published colours differing only in darkness collapse onto one desk ink. A\n");
                sb.Append("    hemicycle, a legend swatch and an election-night row would draw these parties\n");
                sb.Append("    identically. ⚠ NOT fixed here: any fix either stops using the authority's own\n");
                sb.Append("    hue or picks a replacement by eye, and both are D8-2's ruling to make.\n");
            }

            sb.Append("\n--- What has NO ink, and is not given one ---\n");
            sb.Append(F("    {0} of {1} seeded parties carry a published colour; {2} carry none.\n",
                inked, inked + uninked, uninked));
            foreach (var kv in uninkedByCountry)
            {
                sb.Append(F("      {0,-8} {1,2} parties, no published colour table on disk\n", kv.Key, kv.Value));
            }
            sb.Append("    ⚠ These are NOT given a colour by this project. Picking 30 colours by eye for\n");
            sb.Append("      real organisations would be invention, and would probably be wrong - these are\n");
            sb.Append("      real parties with real colours a player may already know. `HasPartyInk`\n");
            sb.Append("      returns false so a caller can say \"not yet coloured\" instead of asserting one.\n");
            sb.Append("      The ruling is Design's: asset request D8-2, register row D-8.2.\n");

            if (failures == 0)
            {
                sb.Append(F("\n=== PartyInkHarness: ALL ASSERTIONS PASS ({0} inked, {1} uninked by design) ===\n", inked, uninked));
                sb.Append(F("    THREE COUNTS, KEPT APART (reconciled 2026-09-01 - one number used to add all three together):\n"
                            + "      {0} of {1} inks sit inside the derived {2:F1} deg hue floor  -> matches the ** TOO CLOSE ** rows above\n"
                            + "      {3} ink(s) sit within 0.10 saturation of a near-grey accent\n"
                            + "      {4} hex(es) are drawn for more than one party\n",
                    floorBreaches, inked, floorDeg, nearGrey, pending));
                sb.Append("    ⚠ D9 row 5 (Design, read 2026-09-01): the floor keeps two AREA accents apart, and party inks\n");
                sb.Append("    never sit in that company - so it binds WITHIN a channel, not across them. The figures above\n");
                sb.Append("    are still true and are still printed; they are no longer a debt, and they were NEVER closed by\n");
                sb.Append("    moving the floor. What now binds is structural and this harness cannot see it: party ink is not\n");
                sb.Append("    drawn adjacent to an area accent, and a party swatch forced into chrome draws in the neutral\n");
                sb.Append("    status ink. That is a DRAW-SITE assertion and is sized as its own row.\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
            }
            else
            {
                sb.Append(F("\n=== PartyInkHarness: {0} FAILURE(S) - the exported hex does not reproduce the theme's own ink, so the Design ask would quote a colour the game does not draw ===\n", failures));
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
            }
        }

        /// <summary>The published Valmyndigheten `fargkod` a desk-seated ink was derived from, for the export.
        /// ⚠ Kept in step with <c>PoliSimTheme.PartyHues</c> BY ASSERTION, not by trust: the harness
        /// re-seats this value and fails if it does not reproduce the theme's own ink.</summary>
        private static int PublishedOf(CountryId country, string abbrev)
        {
            if (country != CountryId.Sweden) { return 0; }
            switch (abbrev)
            {
                case "S":  return 0xFF0000;
                case "SD": return 0x4E83A3;
                case "M":  return 0x66BEE6;
                case "V":  return 0xC40000;
                case "C":  return 0x63A91D;
                case "KD": return 0x1B5CB1;
                case "MP": return 0x008000;
                case "L":  return 0x3399FF;
                default:   return 0;
            }
        }

        /// <summary>`PoliSimTheme.DeskSeated`'s arithmetic, re-stated here ONLY so the export can be
        /// checked against the theme rather than trusted. It is asserted equal on every inked party
        /// every run; if the theme's seating ever changes, this fails loudly instead of exporting a
        /// colour the game does not draw.</summary>
        private static Color DeskSeat(int publishedHex)
        {
            Color.RGBToHSV(PoliSimTheme.Hex(publishedHex), out float h, out float s, out float _);
            if (s <= 0.001f) { return PoliSimTheme.Accent(UiPalette.SystemArea.Neutral); }
            return Color.HSVToRGB(h, 0.52f, 0.46f);
        }

        private static bool Same(Color a, Color b) =>
            Math.Abs(a.r - b.r) < 0.002f && Math.Abs(a.g - b.g) < 0.002f && Math.Abs(a.b - b.b) < 0.002f;

        private static float HueGap(float a, float b)
        {
            float d = Math.Abs(a - b) % 360f;
            return d > 180f ? 360f - d : d;
        }

        private static string ToHex(Color c) => ToHex(
            ((int)Math.Round(c.r * 255f) << 16) | ((int)Math.Round(c.g * 255f) << 8) | (int)Math.Round(c.b * 255f));

        private static string ToHex(int rgb) => rgb.ToString("X6", CultureInfo.InvariantCulture);

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
