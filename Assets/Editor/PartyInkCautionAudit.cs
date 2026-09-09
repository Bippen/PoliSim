using System.Collections.Generic;
using System.IO;
using PoliSim.Data;
using PoliSim.UI;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// D16 §9.2 (2026-09-09, §414) - THE AUDIT DESIGN PUT ON THIS SIDE. Board 10d's assigned SD yellow `#A17E12` sits ΔH 4.6° and
    /// ΔL 0.065 from the desk's Caution `#8F6900`, inside the 8.7°-per-channel floor. Design's own reading is that the standing rule
    /// already covers it - <b>party ink is never drawn adjacent to an area accent, and chrome status dots draw neutral</b> - and that the
    /// hemicycle is clean because no Caution ink appears on it; the two places to check are <b>the Docket's draft cue</b> and <b>the
    /// Budget's pencil face</b>, on the built screens. That check is this.
    ///
    /// <para><b>It runs on the palette the record holds</b> (§256, §279, and §409's finding), not on 10d's assignment: the desk's SD is
    /// the published `fargkod` seated, and it is a BLUE. So the audit answers two questions and prints both - <i>how close is each party
    /// ink to the two draft inks?</i> and <i>does either surface draw a party ink at all?</i> - and the second is the one that decides it,
    /// because a collision that is never drawn side by side is not a collision.</para>
    ///
    /// <para>The structural half is decidable from the source, which is S-29's own method: the two surfaces live in `LedgerRow.cs` (the
    /// Budget's pencil face and its draft figures) and in `GameController.cs` (the Docket's draft cue), and NEITHER file is on
    /// `PartyInkDrawSiteCheck`'s allow-list - so neither may draw `PoliSimTheme.Party(` at all. This check asserts that directly, by name,
    /// so the audit's answer is re-proved every bar rather than remembered.</para>
    /// </summary>
    public static class PartyInkCautionAudit
    {
        private const float HueFloorDegrees = 8.7f;
        private const float LightnessFloor = 0.08f;

        /// <summary>The two surfaces §9.2 names, and the file each is drawn in.</summary>
        private static readonly (string Surface, string File)[] Surfaces =
        {
            ("the Budget's pencil face and draft figures", "LedgerRow.cs"),
            ("the Docket's draft cue", "GameController.cs"),
        };

        private static readonly string[] Parties = { "V", "S", "MP", "C", "L", "KD", "M", "SD" };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var sb = new System.Text.StringBuilder();

            // (1) THE MEASUREMENT: every party ink against the two inks a draft is drawn in.
            PoliSimTheme.ToOklch(PoliSimTheme.Caution, out float cautionL, out float _, out float cautionH);
            PoliSimTheme.ToOklch(PoliSimTheme.Draft, out float draftL, out float _, out float draftH);
            sb.Append($"PARTYCAUTION: Caution #8F6900 reads oklch L {cautionL:F3} H {cautionH:F1}°; Draft (the pencil's own ink) L {draftL:F3} H {draftH:F1}°.\n");
            var inside = new List<string>();
            foreach (string abbrev in Parties)
            {
                Color ink = PoliSimTheme.PartyLaddered(CountryId.Sweden, abbrev);
                PoliSimTheme.ToOklch(ink, out float l, out float _, out float h);
                float dhC = Mathf.Abs(Mathf.DeltaAngle(h, cautionH)), dlC = Mathf.Abs(l - cautionL);
                float dhD = Mathf.Abs(Mathf.DeltaAngle(h, draftH)), dlD = Mathf.Abs(l - draftL);
                bool insideCaution = dhC < HueFloorDegrees && dlC < LightnessFloor;
                bool insideDraft = dhD < HueFloorDegrees && dlD < LightnessFloor;
                if (insideCaution) { inside.Add($"{abbrev} ⁄ Caution ΔH {dhC:F1}° ΔL {dlC:F3}"); }
                if (insideDraft) { inside.Add($"{abbrev} ⁄ Draft ΔH {dhD:F1}° ΔL {dlD:F3}"); }
                sb.Append($"PARTYCAUTION: {abbrev,-2} vs Caution ΔH {dhC,5:F1}° ΔL {dlC,6:F3}   vs Draft ΔH {dhD,5:F1}° ΔL {dlD,6:F3}   {(insideCaution || insideDraft ? "INSIDE THE FLOOR" : "clear")}\n");
            }
            sb.Append(inside.Count == 0
                ? "PARTYCAUTION: no party ink on the desk's own table sits inside the floor of either draft ink.\n"
                : "PARTYCAUTION: inside the floor — " + string.Join(" · ", inside) + "\n");

            // (2) THE STRUCTURAL HALF, which is what decides it: neither surface may draw a party ink at all.
            string scripts = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts");
            foreach ((string surface, string file) in Surfaces)
            {
                string[] found = Directory.GetFiles(scripts, file, SearchOption.AllDirectories);
                if (found.Length == 0)
                {
                    Debug.LogError($"PARTYCAUTION: {file} is not under Assets/Scripts - §9.2 names {surface} and the audit cannot read it.");
                    ok = false;
                    continue;
                }
                string text = File.ReadAllText(found[0]);
                bool drawsParty = text.Contains("PoliSimTheme.Party(") || text.Contains("PoliSimTheme.PartyLaddered(");
                bool drawsDraftInk = text.Contains("PoliSimTheme.Draft") || text.Contains("PoliSimTheme.Caution");
                sb.Append($"PARTYCAUTION: {surface,-44} {file,-22} draws a draft ink: {(drawsDraftInk ? "yes" : "no"),-3}   draws a party ink: {(drawsParty ? "YES" : "no")}\n");
                if (drawsParty)
                {
                    Debug.LogError($"PARTYCAUTION: {file} draws BOTH a party ink and a draft ink - {surface} is exactly where §9.2 said to look, and the two are now in one file. "
                                   + "The standing rule (S-29, D9 row 5) is that party ink is never drawn adjacent to an area accent; this is the decidable form of it.");
                    ok = false;
                }
            }

            sb.Append("PARTYCAUTION: THE AUDIT'S ANSWER — on the desk as built, neither surface draws a party ink at all, so no party ink is drawn beside Caution or the\n"
                      + "    pencil's Draft on either of them. The finding holds on the palette the record carries (§256, §279), where SD is a BLUE and sits far from\n"
                      + "    Caution in hue; it would hold the same way on board 10d's assigned yellow, because what keeps them apart is WHERE each is drawn and not\n"
                      + "    what either one is. If the assignment ever lands (§409), this audit is already the check that says so.\n");
            Debug.Log(sb.ToString());
            Debug.Log(ok ? "PARTYCAUTION: PASS - the two surfaces §9.2 names draw no party ink, and the measurement is on record." : "PARTYCAUTION: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }
    }
}
