using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **One comment stripper, shared by every check that counts a name in source text.**
    ///
    /// <para><b>Why this exists.</b> On 2026-09-01 a generated file's header comment named two subsystems
    /// while explaining why the file existed, and `UnwiredSubsystemCheck` **stopped reporting both as
    /// unreachable** — a prose mention counted as a reference. ⚠ **A check a COMMENT can silence has
    /// stopped discriminating**, which is the audit's own dominant class turned on its own tools.</para>
    ///
    /// <para>⚠ <b>The sweep that followed found it was not one check but four.</b> Every name-scanning
    /// check read raw text:</para>
    /// <list type="bullet">
    /// <item><see cref="UnwiredSubsystemCheck"/> — a prose mention made a subsystem look reachable
    /// (the instance).</item>
    /// <item><see cref="PlayerReachabilityCheck"/> — ⚠ **a comment in `GameController` naming a takeover
    /// would have made it "reachable"**, which is precisely what that check's own ratchet doc warns
    /// somebody not to do, and which the check could not tell apart from a real route.</item>
    /// <item><see cref="EvidenceDiscriminationCheck"/> — ⚠ **a COMMENTED-OUT `Debug.LogError` counted as a
    /// failure path**, so a check that cannot fail would have passed the clause built to catch exactly
    /// that. The sixth sweep, defeated by a comment.</item>
    /// <item><see cref="DocumentClaimCheck"/> — a member named only in a comment in its own type's file
    /// counted as present, masking a document claim about a member that is gone.</item>
    /// </list>
    ///
    /// <para><b>STRING LITERALS SURVIVE, deliberately.</b> A reflected call is built from a string and
    /// must register; a comment can never call anything. That difference is the whole rule.</para>
    ///
    /// <para>⚠ <b>It is an approximation and says so.</b> The line rule leaves a `//` alone when the line
    /// already contains a quote, so a URL inside a literal is not eaten — at the cost of keeping a real
    /// line comment that follows a literal on the same line. That residue can only ever cause the SAME
    /// class of miss, smaller and named, and closing it properly needs a C# lexer rather than a regex.
    /// <see cref="CommentImmunityCheck"/> asserts the behaviour this promises, in both directions.</para>
    /// </summary>
    public static class SourceText
    {
        /// <summary>The block-comment rule <see cref="WithoutComments"/> applies. Internal so `ReviewLedgerCheck`'s comment-only test can refuse the shapes this
        /// regex mis-strips - a block it would open inside a string literal or a line comment - with the stripper's own pattern, not a copy of it.</summary>
        internal static readonly Regex BlockComment = new Regex(@"/\*.*?\*/", RegexOptions.Singleline);

        /// <summary>
        /// P6-F2b (2026-09-21, §542): EVERY PARTIAL OF THE CONTROLLER, concatenated - `GameController.cs` and its `GameController.*.cs` siblings in the same
        /// folder, in name order. The two dial checks read `GameController.cs` alone from the day they were written; the Energy tab's dials are drawn in
        /// `GameController.Energy.cs`, and a dial a check cannot see is the hole the checks exist to close. Empty where the path's folder is missing.
        /// </summary>
        public static string ControllerPartials(string controllerPath)
        {
            string dir = System.IO.Path.GetDirectoryName(controllerPath);
            if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir)) { return string.Empty; }
            string[] files = System.IO.Directory.GetFiles(dir, "GameController*.cs");
            System.Array.Sort(files, string.CompareOrdinal);
            var sb = new System.Text.StringBuilder();
            foreach (string file in files) { sb.Append(System.IO.File.ReadAllText(file)).Append('\n'); }
            return sb.ToString();
        }

        /// <summary>Source with `/* … */` blocks and `//` line comments removed and string literals kept.</summary>
        public static string WithoutComments(string text)
        {
            if (string.IsNullOrEmpty(text)) { return text; }

            text = BlockComment.Replace(text, " ");

            var sb = new StringBuilder(text.Length);
            foreach (string line in text.Split('\n'))
            {
                int slash = line.IndexOf("//", System.StringComparison.Ordinal);
                if (slash >= 0 && line.IndexOf('"') < 0) { sb.Append(line, 0, slash).Append('\n'); }
                else { sb.Append(line).Append('\n'); }
            }

            return sb.ToString();
        }

        // ── The source cache (2026-09-17, `COMPLETED.md` §525) ────────────────────────────────────────────────────────
        // ⚠ ONE READ PER FILE PER BAR, NOT ONE PER CHECK. Thirteen cheap checks each read the source tree - six all of it,
        // four `Assets/Scripts`, three a subset - and several stripped the comments again after another had. Inside a
        // scope (the suite opens one around each group it runs) every form of a file is made once from ONE read of its
        // bytes and handed to every later reader; outside a scope nothing is cached and every call reads the disk, exactly
        // as the checks did before. Each access re-stats the file (length and last write), so a file changed mid-scope is
        // read again rather than served stale.

        private sealed class Entry
        {
            public long Length;
            public DateTime Written;
            public byte[] Bytes;
            public string Text;
            public string[] Lines;
            public string Stripped;
        }

        private static readonly Dictionary<string, Entry> Cache = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private static int _scopes;

        /// <summary>Opens a cache scope; nested scopes share one cache, and the last close empties it.</summary>
        public static void BeginScope() { _scopes++; }

        /// <summary>Closes a cache scope; the last close drops every entry.</summary>
        public static void EndScope()
        {
            if (_scopes > 0) { _scopes--; }
            if (_scopes == 0) { Cache.Clear(); }
        }

        /// <summary>Reads within a scope that were served from the cache, and reads that went to the disk - the bar's enumeration of the dedupe.</summary>
        public static int CacheHits { get; private set; }
        public static int DiskReads { get; private set; }

        private static Entry Get(string path)
        {
            string full = Path.GetFullPath(path);
            var info = new FileInfo(full);
            if (Cache.TryGetValue(full, out Entry entry) && entry.Length == info.Length && entry.Written == info.LastWriteTimeUtc)
            {
                CacheHits++;
                return entry;
            }

            DiskReads++;
            entry = new Entry { Length = info.Length, Written = info.LastWriteTimeUtc, Bytes = File.ReadAllBytes(full) };
            Cache[full] = entry;
            return entry;
        }

        /// <summary>`File.ReadAllBytes`, once per file inside a scope.</summary>
        public static byte[] ReadBytes(string path)
        {
            return _scopes == 0 ? File.ReadAllBytes(path) : Get(path).Bytes;
        }

        /// <summary>`File.ReadAllText` (UTF-8, a byte-order mark honoured), once per file inside a scope.</summary>
        public static string Read(string path)
        {
            if (_scopes == 0) { return File.ReadAllText(path); }
            Entry entry = Get(path);
            if (entry.Text == null)
            {
                using (var reader = new StreamReader(new MemoryStream(entry.Bytes, false), Encoding.UTF8, true))
                {
                    entry.Text = reader.ReadToEnd();
                }
            }

            return entry.Text;
        }

        /// <summary>`File.ReadAllLines`, once per file inside a scope.</summary>
        public static string[] ReadLines(string path)
        {
            if (_scopes == 0) { return File.ReadAllLines(path); }
            Entry entry = Get(path);
            if (entry.Lines == null)
            {
                var lines = new List<string>();
                using (var reader = new StreamReader(new MemoryStream(entry.Bytes, false), Encoding.UTF8, true))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null) { lines.Add(line); }
                }

                entry.Lines = lines.ToArray();
            }

            return entry.Lines;
        }

        /// <summary>`WithoutComments(File.ReadAllText(path))`, stripped once per file inside a scope.</summary>
        public static string ReadWithoutComments(string path)
        {
            if (_scopes == 0) { return WithoutComments(File.ReadAllText(path)); }
            Entry entry = Get(path);
            if (entry.Stripped == null) { entry.Stripped = WithoutComments(Read(path)); }
            return entry.Stripped;
        }
    }
}
