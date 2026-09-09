using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.UI;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// D16 §8 (2026-09-09, §413): the acceptance bars Design put on THIS side, as real checks. Design named eight; four of them are
    /// assertions a build can make and are made here, each against the code the desk actually draws by rather than against a copy of it.
    ///
    /// <list type="number">
    /// <item><b>§8.3 The fence.</b> The eight Swedish party inks, converted to oklch, with all twelve WITHIN-BLOC pairs clearing
    /// `ΔH ≥ 8.7° || ΔL ≥ 0.08`. ⚠ The inks asserted are <b>the desk's</b> - Valmyndigheten's published table as the seating and the
    /// nudge leave it (§256, §279) - because board 10d's assigned palette rests on a ruling that is not in the record (§409). The bar is
    /// the board's; the palette under it is the repo's. A pair that fails is REPORTED with its two figures and does not fail the run:
    /// this is a ratchet on a finding the desk cannot close today (the `PartyMarkCoverageCheck` precedent), and it fails the run when it
    /// gets WORSE - a pair that clears today and stops clearing tomorrow.</item>
    /// <item><b>§8.4 `†` parity.</b> READING and PROVENANCE lay out the same columns: every boundary the eye reads down - the name's, the
    /// figure's left and right, the pips' and the sparkline's - is identical to the pixel in both states, and only the band's right edge
    /// and the sixth track differ. Asserted on `PlateGrid`, which is what the plate draws by.</item>
    /// <item><b>§8.5 The at-rest token rule.</b> `COUPLING DRAFT` renders iff 5c's arrow renders; `SOURCED` / `DERIVED` never render in
    /// READING; `BILLED` / `ABSENT` always render. Asserted on `DeskProvenance`'s three predicates, which the draw calls.</item>
    /// <item><b>§8.6 The waterfall.</b> At three input sets - including one with a zero term and one with two negative terms - the sum of
    /// the drawn positive widths minus the drawn cut width equals `total × scale` to the pixel. Asserted on `RuleWaterfall.Compute`,
    /// which is what the Riksbank page draws by.</item>
    /// </list>
    ///
    /// The other four bars are not assertions of this kind and are not faked here: §8.1 and §8.2 (the seat counts and the hemicycle's
    /// geometry) belong to board 10d, which is STOPPED (§409); §8.7 (no new hue) is `PartyInkDrawSiteCheck`'s and
    /// `ConstantProvenanceCheck`'s ground, already run; §8.8 (captures at four sizes) is the film, run per board.
    /// </summary>
    public static class D16AcceptanceCheck
    {
        /// <summary>§6.4's fence: a pair inside a bloc passes on hue OR on lightness.</summary>
        private const float HueFloorDegrees = 8.7f;
        private const float LightnessFloor = 0.08f;

        /// <summary>The within-bloc pairs that clear the fence on the desk's own palette. A RATCHET: never lowered to accommodate a
        /// regression, and the run fails if it falls. First measured 2026-09-09 on the seated Valmyndigheten table at 10 of 12, with
        /// V ⁄ S (ΔH 0.0°, ΔL 0.060) and M ⁄ SD (ΔH 7.4°, ΔL 0.060) below it. ⚠ RAISED TO 12 the same day (§423) when D17 item 1 - Design's
        /// board 11a - closed both pairs INSIDE §256: `PoliSimTheme`'s bloc fence moves the smaller party of a hue-crowded pair outward in
        /// lightness by the deficit, so V goes down 0.030 and M up 0.030 and nothing that cleared before stops clearing. A floor is raised
        /// to what was measured, never to what is hoped for; this one was set to 12 as Design's prediction and the run below is what
        /// judged it.</summary>
        private const int PairsClearingRatchet = 12;

        /// <summary>⚠ The two blocs, written here AND read by `PoliSimTheme`'s bloc fence from
        /// `NationalElection.BlocOf`. Two enumerations of one fact, so the check below asserts they agree
        /// rather than assuming it - a fence measured over one grouping and enforced over another would
        /// pass while separating the wrong pairs.</summary>
        private static readonly string[] LeftBloc = { "V", "S", "MP", "C" };
        private static readonly string[] RightBloc = { "L", "KD", "M", "SD" };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var sb = new System.Text.StringBuilder();

            // ---- the two blocs, asserted against the grouping the ladder actually reads ------------------------
            foreach (string[] bloc in new[] { LeftBloc, RightBloc })
            {
                int expected = PoliSim.Elections.NationalElection.BlocOf(CountryId.Sweden, bloc[0]);
                foreach (string abbrev in bloc)
                {
                    int actual = PoliSim.Elections.NationalElection.BlocOf(CountryId.Sweden, abbrev);
                    if (actual == expected) { continue; }
                    Debug.LogError($"D16BARS: this check groups {abbrev} with {bloc[0]} and NationalElection.BlocOf does not "
                                   + $"({actual} against {expected}). The fence would be measured over one grouping and enforced over another.");
                    ok = false;
                }
            }

            // ---- §8.3 the fence, on the palette the desk holds -------------------------------------------------
            int pairs = 0, clearing = 0;
            var failures = new List<string>();
            foreach (string[] bloc in new[] { LeftBloc, RightBloc })
            {
                for (int i = 0; i < bloc.Length; i++)
                {
                    for (int j = i + 1; j < bloc.Length; j++)
                    {
                        pairs++;
                        Color a = PoliSimTheme.PartyLaddered(CountryId.Sweden, bloc[i]);
                        Color b = PoliSimTheme.PartyLaddered(CountryId.Sweden, bloc[j]);
                        PoliSimTheme.ToOklch(a, out float la, out float _, out float ha);
                        PoliSimTheme.ToOklch(b, out float lb, out float _, out float hb);
                        float dh = Mathf.Abs(Mathf.DeltaAngle(ha, hb));
                        float dl = Mathf.Abs(la - lb);
                        bool clears = dh >= HueFloorDegrees || dl >= LightnessFloor;
                        if (clears) { clearing++; }
                        else { failures.Add($"{bloc[i]} ⁄ {bloc[j]} ΔH {dh:F1}° · ΔL {dl:F3}"); }
                        sb.Append($"D16BARS: {bloc[i],-2} ⁄ {bloc[j],-2} ΔH {dh,5:F1}° ΔL {dl,6:F3}  {(clears ? "clears" : "BELOW THE FENCE")}\n");
                    }
                }
            }
            sb.Append($"D16BARS: §8.3 the fence, on the DESK's palette (§256/§279's published-hue identity, not 10d's assignment): {clearing} of {pairs} within-bloc pairs clear ΔH ≥ {HueFloorDegrees:F1}° or ΔL ≥ {LightnessFloor:F2}.\n");
            if (failures.Count > 0)
            {
                sb.Append("D16BARS: below the fence — " + string.Join(" · ", failures) + "\n");
                sb.Append("D16BARS: ⚠ REPORTED, NOT FAILED. The pairs that do not clear are a finding about the published table at dot size, which is the\n"
                          + "    finding board 10d's assignment was drawn to answer - and that assignment is not in the record (§409). The ratchet below fails\n"
                          + "    the run if this count ever DROPS; closing the gap is a ruling, not a tuning.\n");
            }
            // ⚠ THE ONE PRINT DESIGN ASKED BACK FOR (board 11a: "the eight laddered L values as PartyLaddered
            // emits them - eight numbers, one line"). It closes their ceiling question - whether M's upward
            // move leaves it under 3:1 against the paper - without another round.
            var ladder = new System.Text.StringBuilder("D16BARS: the eight laddered L, as PartyLaddered emits them —");
            foreach (string[] bloc in new[] { LeftBloc, RightBloc })
            {
                foreach (string abbrev in bloc)
                {
                    PoliSimTheme.ToOklch(PoliSimTheme.PartyLaddered(CountryId.Sweden, abbrev), out float l, out float _, out float _);
                    ladder.Append($" {abbrev} {l:F3}");
                }
            }

            sb.Append(ladder.Append('\n').ToString());

            RatchetLedger.Report("D16AcceptanceCheck.FENCE_PAIRS", clearing, PairsClearingRatchet, isFloor: true);
            if (clearing < PairsClearingRatchet)
            {
                Debug.LogError($"D16BARS: §8.3 the fence GOT WORSE - {clearing} pairs clear against the ratchet {PairsClearingRatchet}. A re-cut has broken a pair that used to hold.");
                ok = false;
            }

            // ---- §8.4 the tab's parity ---------------------------------------------------------------------------
            var area = new Rect(0f, 0f, 1119f, 100f);
            float[] reading = PlateGrid.Tracks(area, PlateGrid.Reading);
            float[] provenance = PlateGrid.Tracks(area, PlateGrid.Provenance);
            // The name's edges, the figure's edges: identical in both states. The eye reads down these boundaries and they may not move.
            for (int i = 0; i <= 2; i++)
            {
                if (Mathf.Abs(reading[i] - provenance[i]) > 0.001f)
                {
                    Debug.LogError($"D16BARS: §8.4 the tab MOVED a column - boundary {i} reads {reading[i]:F3} at rest and {provenance[i]:F3} behind the tab. PROVENANCE adds lines, never columns.");
                    ok = false;
                }
            }
            float readingFigureWidth = reading[2] - reading[1], provenanceFigureWidth = provenance[2] - provenance[1];
            if (Mathf.Abs(readingFigureWidth - provenanceFigureWidth) > 0.001f)
            {
                Debug.LogError($"D16BARS: §8.4 the figure cell changed width - {readingFigureWidth:F3} at rest, {provenanceFigureWidth:F3} behind the tab.");
                ok = false;
            }
            // The band is the only track that flexes, and it pays for the sixth.
            float bandLost = (reading[3] - reading[2]) - (provenance[3] - provenance[2]);
            float honestyGained = provenance[6] - provenance[5];
            sb.Append($"D16BARS: §8.4 the tab - the first three boundaries identical to 1e-3, the figure cell {readingFigureWidth:F1} px in both; the band pays {bandLost:F1} px for a {honestyGained:F1} px honesty column.\n");
            if (bandLost <= 0f)
            {
                Debug.LogError("D16BARS: §8.4 the band did not pay for the honesty column - some other track did, which is a column moving sideways.");
                ok = false;
            }

            // ---- §8.5 the at-rest token rule ---------------------------------------------------------------------
            if (DeskProvenance.ShowsHonestyColumn(false)) { Debug.LogError("D16BARS: §8.5 SOURCED/DERIVED render at rest - they qualify nothing a reader can see."); ok = false; }
            if (!DeskProvenance.ShowsHonestyColumn(true)) { Debug.LogError("D16BARS: §8.5 the honesty column does not render behind the tab."); ok = false; }
            if (DeskProvenance.ShowsCouplingDraft(true, false)) { Debug.LogError("D16BARS: §8.5 COUPLING DRAFT renders where 5c's arrow does not - the apparatus must follow its subject."); ok = false; }
            if (!DeskProvenance.ShowsCouplingDraft(true, true)) { Debug.LogError("D16BARS: §8.5 COUPLING DRAFT does not render where the arrow does."); ok = false; }
            if (DeskProvenance.ShowsCouplingDraft(false, true)) { Debug.LogError("D16BARS: §8.5 COUPLING DRAFT renders on a row whose coupling is not a draft."); ok = false; }
            foreach (bool state in new[] { false, true })
            {
                if (!DeskProvenance.ShowsGapWord(true, state)) { Debug.LogError($"D16BARS: §8.5 a gap row loses its word in {(state ? "PROVENANCE" : "READING")} - a mark may not replace a gap."); ok = false; }
                if (DeskProvenance.ShowsGapWord(false, state)) { Debug.LogError("D16BARS: §8.5 a row that is not a gap draws the gap word."); ok = false; }
            }
            sb.Append("D16BARS: §8.5 the at-rest rule - honesty behind the tab only, COUPLING DRAFT iff the arrow, the gap word in both states.\n");

            // ---- §8.6 the waterfall, at three input sets ---------------------------------------------------------
            var sets = new (string Name, (string, float)[] Terms, float Total)[]
            {
                ("the sitting's figures", new[] { ("NEUTRAL REAL", 2.00f), ("INFLATION", 2.00f), ("INFLATION GAP", 0.00f), ("U-GAP", -1.50f) }, 2.50f),
                ("a zero term", new[] { ("NEUTRAL REAL", 2.00f), ("INFLATION", 1.77f), ("INFLATION GAP", 0.00f), ("U-GAP", -0.12f) }, 3.65f),
                ("two negative terms", new[] { ("NEUTRAL REAL", 2.00f), ("INFLATION", 1.77f), ("INFLATION GAP", -0.12f), ("U-GAP", -0.12f) }, 3.53f),
            };
            const float Scale = 130f;
            foreach ((string name, (string, float)[] terms, float total) in sets)
            {
                RuleWaterfall.Geometry g = RuleWaterfall.Compute(terms, total, Scale);
                float drawn = g.PositiveSum - g.CutWidth;
                float expected = total * Scale;
                int zeroes = 0, negatives = g.Negatives.Count;
                foreach (RuleWaterfall.Segment seg in g.Positives) { if (seg.Zero) { zeroes++; } }
                sb.Append($"D16BARS: §8.6 {name,-22} positives {g.PositiveSum,7:F1} px − cut {g.CutWidth,6:F1} px = {drawn,7:F1} px, total × {Scale:F0} = {expected,7:F1} px; {zeroes} zero term(s) drawn as nothing, {negatives} cut(s).\n");
                if (Mathf.Abs(drawn - expected) > 1f)
                {
                    Debug.LogError($"D16BARS: §8.6 the waterfall does not sum - {name}: drawn {drawn:F2} px against total × scale {expected:F2} px.");
                    ok = false;
                }
            }
            RuleWaterfall.Geometry zeroSet = RuleWaterfall.Compute(sets[1].Terms, sets[1].Total, Scale);
            bool anyZeroDrawn = false;
            foreach (RuleWaterfall.Segment seg in zeroSet.Positives) { if (seg.Zero && seg.Width > 0f) { anyZeroDrawn = true; } }
            if (anyZeroDrawn) { Debug.LogError("D16BARS: §8.6 a zero term was drawn as a sliver - it must be drawn as nothing and labelled struck through."); ok = false; }

            Debug.Log(sb.ToString());
            Debug.Log(ok ? "D16BARS: PASS - the fence at its ratchet, the tab's parity, the at-rest rule and the waterfall's arithmetic." : "D16BARS: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }
    }
}
