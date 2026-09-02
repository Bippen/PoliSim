using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **S-29: the party-ink constraint is a DRAW-SITE rule, and nothing asserted it.**
    ///
    /// <para><b>Why a hue harness could not do this.</b> D9 row 5 ruled the derived 8.7° hue floor **the
    /// wrong constraint**: it keeps two AREA accents apart, and party inks never sit in that company. So
    /// `PartyInkHarness`'s six PENDs were *answered, not fixed* — their measurements are still true and
    /// still printed, and they were never evidence for the thing anyone cared about. ⚠ **What binds is
    /// structural: WHERE a colour is drawn, not what it is** — and no measurement of a colour can see
    /// that.</para>
    ///
    /// <para><b>The two clauses, in the only decidable form they have.</b></para>
    /// <list type="number">
    /// <item><b>Party ink is drawn only where party identity is the subject.</b> An allow-list of FILES,
    /// each with the argument for why it is on it. Anything else drawing `PoliSimTheme.Party(` fails.</item>
    /// <item><b>No file draws both a party ink and an area accent.</b> ⚠ *Adjacent* is not decidable from
    /// source — two draws in one method may be a metre apart on screen — so this takes the containing FILE
    /// as the unit and says so. It is a coarser rule than the ruling and **strictly stronger**: a file that
    /// never draws both cannot draw them adjacent.</item>
    /// </list>
    ///
    /// <para>⚠ <b>Clause 3 needs no clause of its own.</b> *"A party swatch forced into chrome draws in the
    /// neutral status ink"* is subsumed: chrome lives in `GameController`, which is not on the allow-list,
    /// so a party ink appearing in a status dot fails clause 1 by construction. Measured today, the one
    /// rail status dot draws `PoliSimTheme.Good`. **A second clause restating the first would be a second
    /// thing to keep true.**</para>
    ///
    /// <para>⚠ <b>Scope: runtime only</b> (`Assets/Scripts`). `PartyInkHarness` reads the ink to MEASURE it
    /// and draws nothing; including the Editor tree would make the check fire on its own instrument, which
    /// is the shape of a guard that gets widened until it stops meaning anything.</para>
    /// </summary>
    public static class PartyInkDrawSiteCheck
    {
        private const string PartyInkCall = "PoliSimTheme.Party(";
        /// <summary>Board 5e (D11 row 5, 2026-09-02): the laddered accessor is the same channel - a party ink stepped in lightness - and a site drawing it is a party-ink draw site.</summary>
        private const string PartyInkLadderedCall = "PoliSimTheme.PartyLaddered(";

        /// <summary>Area accents, both weights. ⚠ Both are listed because a file drawing the desk-weight
        /// variant beside a party ink is the same finding as one drawing the paper-weight variant, and a
        /// check that knew only one would report clean on half the cases.</summary>
        private static readonly string[] AreaAccentCalls =
        {
            "PoliSimTheme.Accent(",
            "PoliSimTheme.AccentOnDesk(",
        };

        /// <summary>The files allowed to draw a party ink, each with the argument for it. ⚠ This is an
        /// allow-list rather than a deny-list on purpose: a deny-list is silent about the file nobody
        /// thought of, and the whole finding is that party ink turned up somewhere nobody had considered.</summary>
        private static readonly (string File, string Reason)[] MayDrawPartyInk =
        {
            ("HemicycleRenderer.cs",
             "the chamber and its legend are the one surface where a party's IDENTITY is the subject "
             + "rather than the decoration. Behaviour 9 requires the arc and its legend swatch to come "
             + "from the SAME call, which is why both live here rather than agreeing across two files."),
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var offSite = new List<string>();
            var mixed = new List<string>();
            var drew = new List<string>();
            var unusedPermission = new List<string>(MayDrawPartyInk.Length);
            foreach (var a in MayDrawPartyInk) { unusedPermission.Add(a.File); }

            int scanned = 0;
            string scripts = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts");
            foreach (string path in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                // A commented-out draw is not a draw, and a comment DISCUSSING the rule would otherwise
                // read as a violation of it - the defect this repo has catalogued five times.
                string text = SourceText.WithoutComments(File.ReadAllText(path));
                string file = Path.GetFileName(path);
                scanned++;

                if (!text.Contains(PartyInkCall) && !text.Contains(PartyInkLadderedCall)) { continue; }

                drew.Add(file);
                unusedPermission.Remove(file);

                bool allowed = false;
                foreach (var a in MayDrawPartyInk) { if (a.File == file) { allowed = true; break; } }
                if (!allowed) { offSite.Add(file); }

                foreach (string accent in AreaAccentCalls)
                {
                    if (text.Contains(accent)) { mixed.Add(file + " also calls " + accent + ")"); break; }
                }
            }

            var sb = new StringBuilder();
            sb.Append("=== S-29: party ink is a DRAW-SITE rule ===\n");
            sb.Append("    THE ENUMERATION: ").Append(scanned).Append(" runtime source file(s) under Assets/Scripts; ")
              .Append(drew.Count).Append(" draw a party ink.\n");
            foreach (string d in drew) { sb.Append("      draws  ").Append(d).Append('\n'); }
            sb.Append("\n    ALLOWED TO, and why:\n");
            foreach (var a in MayDrawPartyInk) { sb.Append("      ").Append(a.File).Append(" - ").Append(a.Reason).Append('\n'); }
            foreach (string o in offSite) { sb.Append("    ⚠ OFF-SITE   ").Append(o).Append('\n'); }
            foreach (string m in mixed) { sb.Append("    ⚠ MIXED      ").Append(m).Append('\n'); }
            foreach (string u in unusedPermission) { sb.Append("    ⚠ UNUSED PERMISSION  ").Append(u).Append('\n'); }
            sb.Append("\n    ⚠ Clause 3 - a party swatch forced into chrome draws the NEUTRAL status ink - needs no\n");
            sb.Append("    clause of its own: chrome lives in GameController, which is not on the list, so a party\n");
            sb.Append("    ink in a status dot fails clause 1 by construction. A second clause restating the first\n");
            sb.Append("    would be a second thing to keep true.\n");

            // The enumeration rule. If NOTHING draws a party ink, either the accessor was renamed or this
            // check is reading the wrong tree - and a clean report would be indistinguishable from a repo
            // that obeys the rule.
            if (drew.Count == 0)
            {
                Debug.LogError("PARTYINK: not one runtime file draws " + PartyInkCall + ". Either the accessor was "
                               + "renamed or this check is scanning the wrong tree - either way this run verified "
                               + "NOTHING, which is not the same as finding nothing.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            // ⚠ An allow-list entry that matches nothing is not harmless: it reads as coverage, it outlives
            // the file it named, and the next reader takes it as evidence the case was considered.
            if (unusedPermission.Count > 0)
            {
                Debug.LogError("PARTYINK: " + unusedPermission.Count + " file(s) are permitted to draw a party ink and "
                               + "do not - " + string.Join(", ", unusedPermission.ToArray())
                               + ". Delete the permission or fix what it names; a permission nobody uses is a hole "
                               + "kept open for a file that no longer exists.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            if (offSite.Count > 0 || mixed.Count > 0)
            {
                Debug.LogError("PARTYINK: " + offSite.Count + " off-site draw(s) and " + mixed.Count + " file(s) drawing "
                               + "both a party ink and an area accent. ⚠ D9 row 5 ruled this a DRAW-SITE constraint - "
                               + "party ink never sits beside an area accent, and a party swatch forced into chrome "
                               + "draws the neutral status ink. A hue measurement cannot see either, which is why "
                               + "PartyInkHarness's PENDs were answered rather than fixed.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
