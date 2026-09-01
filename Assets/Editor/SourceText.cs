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
        private static readonly Regex BlockComment = new Regex(@"/\*.*?\*/", RegexOptions.Singleline);

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
    }
}
