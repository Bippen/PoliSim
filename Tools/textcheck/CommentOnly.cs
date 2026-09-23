// RL-1 (ruled 2026-09-23, COMPLETED.md s588): THE COMMENT-ONLY TEST ON ROSLYN. The review ledger's exemption asks one question of two
// versions of a C# file - does anything but comments and whitespace differ? - and it is answered here by the compiler's own parser
// (Microsoft.CodeAnalysis.CSharp), not by a regex stripper: both texts are parsed, the trivia a compiler ignores is dropped, and the rest
// is compared token for token. Only COMMENT and WHITESPACE trivia are dropped (line and block comments, documentation comments, spaces
// and every line break C# honours). A preprocessor directive, text in a disabled #if branch and skipped tokens are KEPT as code:
// a directive changes what compiles, and a branch disabled here is live under another define (UNITY_EDITOR and its kin), so an edit
// there owes its review. A text that does not parse cleanly is refused - a verdict on a broken file would be a guess.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class CommentOnly
{
    private static readonly CSharpParseOptions Options = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Regular);

    /// <summary>Null when the two texts differ in comments and whitespace only; otherwise the reason, naming the first difference.</summary>
    public static string Refusal(string before, string after)
    {
        List<string> a = Code(before, out string errorA);
        if (errorA != null) { return "the earlier text does not parse cleanly (" + errorA + ") - no verdict on a broken file"; }
        List<string> b = Code(after, out string errorB);
        if (errorB != null) { return "the later text does not parse cleanly (" + errorB + ") - no verdict on a broken file"; }
        int n = Math.Min(a.Count, b.Count);
        for (int i = 0; i < n; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) { return "the code differs at element " + i + ": '" + Clip(a[i]) + "' against '" + Clip(b[i]) + "'"; }
        }
        return a.Count == b.Count ? null : "the code differs in length (" + a.Count + " against " + b.Count + " elements)";
    }

    /// <summary>The code a text carries, as a sequence: every token's kind and text, and every trivia that is not a comment or whitespace, in order.</summary>
    private static List<string> Code(string text, out string error)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(text, Options);
        Diagnostic first = tree.GetDiagnostics().FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        error = first == null ? null : first.Id + " at " + first.Location.GetLineSpan().StartLinePosition;
        var code = new List<string>();
        foreach (SyntaxToken token in tree.GetRoot().DescendantTokens(descendIntoTrivia: false))
        {
            AddTrivia(code, token.LeadingTrivia);
            code.Add(token.RawKind + ":" + token.Text);
            AddTrivia(code, token.TrailingTrivia);
        }
        return code;
    }

    private static void AddTrivia(List<string> code, SyntaxTriviaList list)
    {
        foreach (SyntaxTrivia t in list)
        {
            switch ((SyntaxKind)t.RawKind)
            {
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                case SyntaxKind.WhitespaceTrivia:
                case SyntaxKind.EndOfLineTrivia:
                    continue;   // the compiler ignores these; so does the verdict
                default:
                    // a directive (its text, trimmed of its own line break), disabled text, skipped tokens: all kept as code
                    code.Add("trivia " + t.RawKind + ":" + t.ToFullString().Trim());
                    break;
            }
        }
    }

    private static string Clip(string s) => s.Length <= 60 ? s : s.Substring(0, 60) + "...";
}
