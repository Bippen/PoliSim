using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **S-39: we cut a screen Design was drawing and did not tell them.**
    ///
    /// <para><b>The instance.</b> Row `E17` of `CLAUDE_DESIGN_ASSET_REQUEST.md` asked Design to draw the
    /// *"as published"* band — the date axis, release markers, the PRELIMINARY/FINAL badge, the dashed
    /// revision frame, the pager. **P-A2 removed that band from the game on 2026-08-29 as a display cut,
    /// and the row went on asking for three days.** ⚠ It surfaced only because the row happened to name a
    /// DELETED MEMBER and `DocumentClaimCheck` resolves identifiers — **an accident of how the row was
    /// written.** A row phrased in pure prose would have gone on asking indefinitely.</para>
    ///
    /// <para>⚠ <b>The finding is not the band, it is the missing CHANNEL.</b> This project has a great deal
    /// of machinery for keeping its own documents true to its own code, and **none at all** for keeping an
    /// OUTWARD-FACING ask true to a decision made after it was sent. A cut is a decision we make; the ask
    /// lives in a document somebody else reads; **nothing connected the two but somebody remembering** —
    /// the mechanism this repo has recorded as failing more often than any other.</para>
    ///
    /// <para><b>What this decides, and it is deliberately narrow.</b> A row tagged <c>[CUT]</c> in the
    /// design request must be NAMED in that document's *"TO DESIGN, WITH THE NEXT RETURN"* section.
    /// ⚠ It does not and cannot judge whether a row's PROSE still matches the build — that is S-22's
    /// undecidable class. **It makes the one thing that is decidable failing: you cannot mark something cut
    /// without telling the person drawing it, because the document will not pass.**</para>
    ///
    /// <para>⚠ <b>WHAT IT CANNOT SEE.</b> A screen cut and never tagged. The tag is the enrolment, and an
    /// enrolment is only as good as the discipline of applying it — which is why the tag goes on the STATUS
    /// column a reader of the row already has to write, rather than somewhere separate that could be
    /// forgotten on its own. Stated rather than discovered later.</para>
    /// </summary>
    public static class DesignNotificationCheck
    {
        private const string RequestFile = "CLAUDE_DESIGN_ASSET_REQUEST.md";

        /// <summary>The heading that opens the section Design is told to read first.</summary>
        private const string NotificationHeading = "TO DESIGN, WITH THE NEXT RETURN";

        /// <summary>A row marked as cut. ⚠ On the STATUS column, which whoever records a cut is already
        /// editing — a tag kept somewhere separate is a second thing to remember.</summary>
        private const string CutTag = "[CUT]";

        /// <summary>The row's id, from the first cell: `E17`, `B6`, `D8.2`.</summary>
        private static readonly Regex RowId = new Regex(@"^\|\s*\**([A-Z]+[0-9]+(?:\.[0-9]+)?)\**\s*\|");

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string path = Path.Combine(Directory.GetCurrentDirectory(), RequestFile);
            if (!File.Exists(path))
            {
                Debug.LogError("DESIGNNOTIFY: " + RequestFile + " is not on disk, so this run checked NOTHING "
                               + "rather than finding nothing.");
                CheckExit.Finish(1);
                return;
            }

            string[] lines = File.ReadAllLines(path);

            // The notification section runs from its heading to the next heading of the same level.
            int start = -1;
            int end = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                if (start < 0 && lines[i].Contains(NotificationHeading)) { start = i; continue; }
                if (start >= 0 && i > start && lines[i].StartsWith("## ", System.StringComparison.Ordinal)) { end = i; break; }
            }

            var notified = new StringBuilder();
            if (start >= 0)
            {
                for (int i = start; i < end; i++) { notified.Append(lines[i]).Append('\n'); }
            }

            string notifiedText = notified.ToString();

            var cutRows = new List<string>();
            var untold = new List<string>();
            int tableRows = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                if (start >= 0 && i >= start && i < end) { continue; }   // the notice itself is not a row

                Match id = RowId.Match(lines[i]);
                if (!id.Success) { continue; }

                tableRows++;
                if (!lines[i].Contains(CutTag)) { continue; }

                string rowId = id.Groups[1].Value;
                cutRows.Add(rowId);

                // ⚠ Backticked, so a row id cannot be "named" by coincidence - `E17` is a reference and
                // E17 inside a sentence about something else is not.
                if (notifiedText.IndexOf("`" + rowId + "`", System.StringComparison.Ordinal) < 0)
                {
                    untold.Add(rowId);
                }
            }

            var sb = new StringBuilder();
            sb.Append("=== S-39: a cut screen must be told to the person drawing it ===\n");
            sb.Append("    THE ENUMERATION: ").Append(tableRows).Append(" request row(s) in ").Append(RequestFile)
              .Append("; ").Append(cutRows.Count).Append(" tagged ").Append(CutTag).Append("; ")
              .Append(untold.Count).Append(" of those not named in the notice.\n");
            sb.Append("    The notice section is '").Append(NotificationHeading).Append("'")
              .Append(start < 0 ? " - ABSENT" : " - present").Append(".\n");
            foreach (string c in cutRows)
            {
                sb.Append(untold.Contains(c) ? "    ⚠ UNTOLD  " : "    told    ").Append(c).Append('\n');
            }

            sb.Append("\n    ⚠ It cannot judge whether a row's PROSE still matches the build - that is S-22's\n");
            sb.Append("    undecidable class. It makes the one decidable thing FAILING: a screen cannot be marked\n");
            sb.Append("    cut without the document also telling the person drawing it.\n");
            sb.Append("    ⚠ AND IT CANNOT SEE a screen cut and never tagged. The tag is the enrolment, which is why\n");
            sb.Append("    it sits on the STATUS column somebody recording a cut is already editing.\n");

            // The enumeration rule: no rows parsed means the document's shape moved and this verified nothing.
            if (tableRows == 0)
            {
                Debug.LogError("DESIGNNOTIFY: not one request row parsed out of " + RequestFile + ". Its shape has "
                               + "changed or this check is reading the wrong file - either way it verified NOTHING, "
                               + "which is not the same as finding nothing.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            if (untold.Count > 0)
            {
                Debug.LogError("DESIGNNOTIFY: " + untold.Count + " row(s) are marked " + CutTag + " and are not named "
                               + "in the '" + NotificationHeading + "' notice - " + string.Join(", ", untold.ToArray())
                               + ". ⚠ S-39: E17 asked Design to draw a band this project had cut, and went on asking "
                               + "for three days. It surfaced only because that row happened to name a deleted MEMBER "
                               + "and DocumentClaimCheck resolves identifiers - an accident of how it was written. "
                               + "Add the row to the notice; a decision we make is not a decision they heard.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
