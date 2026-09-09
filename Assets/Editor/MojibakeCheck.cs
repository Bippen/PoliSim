using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **Text that was decoded once too few times: UTF-8 bytes read as Latin-1 and written back out as
    /// UTF-8.** The em dash becomes three characters, the warning sign becomes four, and nothing in the
    /// build notices - the file is still valid UTF-8, it just says something else.
    ///
    /// <para><b>Why it exists (2026-09-09, §422).</b> Twice now, a tool edit has done this to a whole
    /// file. `perl -0777 -i -pe` slurps a file as BYTES; the moment a replacement string carries a
    /// character above U+00FF, Perl upgrades the entire string to characters, and every byte that was
    /// part of a multi-byte sequence is re-encoded on the way out. One warning on stderr, no error, and
    /// every dash and warning glyph in the file is silently mangled. **`Country.cs` carried it from
    /// 2026-09-08 to 2026-09-09 with a green bar the whole time**, and the second instance put mangled
    /// text into `CLAUDE_DESIGN_ASSET_REQUEST.md`'s generated block - a document that is sent to another
    /// party.</para>
    ///
    /// <para><b>How it decides.</b> Decode the file as UTF-8, take every maximal run of characters in
    /// U+0080..U+00FF, and ask whether the BYTE VALUES of that run are themselves valid UTF-8. If they
    /// are, the run is almost certainly one round of this defect, because a run of Latin-1 supplement
    /// characters that happens to form a valid UTF-8 sequence is not how real prose is written.</para>
    ///
    /// <para>⚠ <b>ALMOST certainly, which is why this is a RATCHET and not a rule.</b> The measured
    /// backlog is one: `COMPLETED.md` contains a multiplication sign followed by a half (U+00D7,
    /// U+00BD) in a line about doubling and halving, and those two characters' byte values (D7 BD)
    /// happen to be a valid UTF-8 encoding of a Hebrew point. ⚠ Named by code point rather than
    /// written out, because writing it out here would put a second copy of the backlog in the file
    /// that measures it. **The text is correct and the check cannot know that**, so
    /// the count is ratcheted at what was measured and the check fails on GROWTH. A blanket repair that
    /// trusted this rule would have damaged that line, which is the argument for reporting rather than
    /// fixing.</para>
    ///
    /// <para><b>What it enumerates</b> (rule 14): every `*.md` at the project root, every `*.cs` under
    /// `Assets/`, and every file under `Tools/`. Not `Logs/` or `Library/` - generated and imported
    /// content is not authored text and would report on somebody else's encoding.</para>
    /// </summary>
    public static class MojibakeCheck
    {
        /// <summary>⚠ The measured backlog on 2026-09-09: one, and it is a FALSE POSITIVE that cannot be
        /// distinguished (see the class comment). Lower it if that line ever changes; never raise it.</summary>
        private const int SuspectCeiling = 1;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string root = Directory.GetCurrentDirectory();
            var files = new List<string>();
            files.AddRange(Directory.GetFiles(root, "*.md", SearchOption.TopDirectoryOnly));

            string assets = Path.Combine(root, "Assets");
            if (Directory.Exists(assets)) { files.AddRange(Directory.GetFiles(assets, "*.cs", SearchOption.AllDirectories)); }

            string tools = Path.Combine(root, "Tools");
            if (Directory.Exists(tools)) { files.AddRange(Directory.GetFiles(tools, "*", SearchOption.AllDirectories)); }

            if (files.Count == 0)
            {
                Debug.LogError("MOJIBAKE: no authored text file was found at all, so this run VERIFIED NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            var strict = new UTF8Encoding(false, true);
            var sb = new StringBuilder();
            sb.Append("=== Text decoded once too few times ===\n");

            int suspects = 0, scanned = 0, undecodable = 0;
            foreach (string file in files)
            {
                byte[] bytes;
                try { bytes = File.ReadAllBytes(file); }
                catch (IOException) { continue; }

                string text;
                try { text = strict.GetString(bytes); }
                catch (DecoderFallbackException)
                {
                    // Not UTF-8 at all. A different defect, and worth saying so rather than skipping in
                    // silence - but not this check's ratchet.
                    undecodable++;
                    sb.Append("    NOT UTF-8  ").Append(Relative(root, file)).Append('\n');
                    continue;
                }

                scanned++;
                int i = 0;
                while (i < text.Length)
                {
                    if (text[i] < 0x80 || text[i] > 0xFF) { i++; continue; }

                    int start = i;
                    while (i < text.Length && text[i] >= 0x80 && text[i] <= 0xFF) { i++; }

                    int length = i - start;
                    var raw = new byte[length];
                    for (int k = 0; k < length; k++) { raw[k] = (byte)text[start + k]; }

                    string decoded;
                    try { decoded = strict.GetString(raw); }
                    catch (DecoderFallbackException) { continue; }

                    if (decoded.Length == length) { continue; }   // nothing collapsed: not a re-encoding

                    suspects++;
                    sb.Append("    SUSPECT   ").Append(Relative(root, file))
                      .Append(" line ").Append(LineOf(text, start))
                      .Append(": [").Append(Escape(text.Substring(start, length)))
                      .Append("] reads as [").Append(Escape(decoded)).Append("]\n");
                }
            }

            sb.Append($"\n    {scanned} authored file(s) scanned, {undecodable} not UTF-8, {suspects} suspect run(s) "
                      + $"against a ceiling of {SuspectCeiling}.\n");
            sb.Append("    ⚠ A suspect run is REPORTED, never repaired here: the one in the backlog is correct text\n");
            sb.Append("    that this rule cannot tell from the defect, and a check that edited files on that basis\n");
            sb.Append("    would break a line to satisfy itself.\n");

            RatchetLedger.Report("MojibakeCheck.SUSPECT_RUNS", suspects, SuspectCeiling);

            if (undecodable > 0 || suspects > SuspectCeiling)
            {
                Debug.LogError(sb.ToString() + $"\nMOJIBAKE: {suspects} suspect run(s) above the ceiling {SuspectCeiling}"
                               + (undecodable > 0 ? $", and {undecodable} file(s) that are not UTF-8 at all" : string.Empty)
                               + ". Text was decoded once too few times somewhere - see the lines above.");
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            Debug.Log("=== MojibakeCheck: ALL ASSERTIONS PASS ===");
            CheckExit.Finish(0);
        }

        private static string Relative(string root, string file)
            => file.StartsWith(root, StringComparison.Ordinal)
                ? file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, '/')
                : file;

        private static int LineOf(string text, int index)
        {
            int line = 1;
            for (int i = 0; i < index && i < text.Length; i++) { if (text[i] == '\n') { line++; } }

            return line;
        }

        /// <summary>The run itself is unprintable in a log by definition - it is the wrong characters.
        /// Code points, so the line says what is actually there.</summary>
        private static string Escape(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s) { sb.Append("U+").Append(((int)c).ToString("X4")).Append(' '); }

            return sb.ToString().TrimEnd();
        }
    }
}
