using System;
using System.Collections.Generic;

namespace PoliSim.UI
{
    /// <summary>
    /// K322 S4 (K320's sweep of the Laws page, ruled 2026-10-30; UI v3.3 §1 and §7): **THE LAWS PAGE'S SLIPS FOR THE WORDS ITS DECLUTTER TOOK OFF THE
    /// PAGE AT REST.** At rest a label is at most three words, a caption one line never cut, and no sentence stands; every word the sweep took off the
    /// Laws page (the tabs' bill status lines, the statute book's empty card, its pane's action, lock, competence note, effect lines and the party
    /// reasons, the bottom bar) is on a slip at the anchor of the thing that lost it, in fragments of about eight words, at most four lines under the
    /// head, and the † view still prints the whole text where it stood. The words each anchor took are listed in `docs/declutter/moved_k322s4.tsv`.
    /// <para>A line breaks only at a clause mark the declutter judge also splits at (a middle dot, a spaced dash, a semicolon, a colon, a comma, a full
    /// stop), so each clause of the text lands whole in one line; a clause longer than the slip's line cap is cut at its words, and then (or where the
    /// text needs more than four lines) the last line is a marked term whose level-2 slip holds the whole text as one sentence - UI v3.3's two levels.</para>
    /// </summary>
    public static class LawsSlips
    {
        // ---- the anchors ----------------------------------------------------------------------------------------------------------------------
        public const string ListEmpty = "laws:list:empty";
        public const string Affordable = "laws:affordable";
        public const string PaneNone = "laws:pane:none";
        public const string PaneAction = "laws:pane:action";
        public const string PanePending = "laws:pane:pending";
        public const string PaneEffect = "laws:pane:effect:";
        public const string PaneStance = "laws:pane:stance:";
        public const string LabourStatus = "laws:labourbill:status";
        public const string SectorsStatus = "laws:sectorsbill:status";
        public const string TradeStatus = "laws:tradebill:status";
        public const string Trace = "laws:trace";

        // ---- the labels the declutter shortened (K320: a label is at most three words) -------------------------------------------------------
        public const string IntroduceLabel = "INTRODUCE BILL ›";
        public const string IntroduceWords = "INTRODUCE AS A BILL";
        public const string RepealLabel = "REPEAL THIS LAW ›";
        public const string RepealWords = "REPEAL THIS LAW";
        public const string NoneSelected = "No law selected";
        public const string NoneSelectedBefore = "Select a law from the list to see its detail.";

        /// <summary>CONVENTION (K320 (3)): about eight counted words a slip line - the line packer's aim.</summary>
        public const int LineWords = 8;

        /// <summary>K321 (f): a level-1 line over this many counted words is a SLIP breach - a clause past it is cut at its words.</summary>
        public const int LineCap = 10;

        /// <summary>K320 (3): a level-1 slip holds at most four lines under its head.</summary>
        public const int MaxLines = 4;

        /// <summary>The clause marks a line may break at - the declutter judge's own segment marks (`DeclutterRules.Unreachable`).</summary>
        private static readonly string[] Breaks = { " · ", " - ", "; ", ": ", ", ", ". " };

        /// <summary>A moved text: what stays at rest, the whole text the † view prints where it stood, and the slip (with the level-2 slips it marks) that
        /// holds what left.</summary>
        public sealed class Moved
        {
            public string Rest;
            public string Full;
            public SlipContent Slip;
            public readonly Dictionary<string, SlipContent> Terms = new Dictionary<string, SlipContent>();

            /// <summary>What the surface draws: the whole text behind the †, the fragment at rest.</summary>
            public string Drawn(bool provenance) => provenance || Rest == null ? Full : Rest;

            /// <summary>Writes the slip under <paramref name="anchor"/> and its marked terms into <paramref name="book"/>.</summary>
            public void Into(PeopleSlips.Book book, string anchor)
            {
                book.Anchors[anchor] = Slip;
                foreach (KeyValuePair<string, SlipContent> term in Terms) { book.Terms[term.Key] = term.Value; }
            }
        }

        // ---- the line packer ------------------------------------------------------------------------------------------------------------------

        /// <summary>The text cut after each clause mark, each piece keeping its mark - the pieces joined are the text.</summary>
        public static List<string> Pieces(string text)
        {
            var pieces = new List<string>();
            int start = 0;
            while (start < text.Length)
            {
                int at = -1, length = 0;
                foreach (string mark in Breaks)
                {
                    int i = text.IndexOf(mark, start, StringComparison.Ordinal);
                    if (i >= 0 && (at < 0 || i < at)) { at = i; length = mark.Length; }
                }
                if (at < 0) { pieces.Add(text.Substring(start)); break; }
                pieces.Add(text.Substring(start, at + length - start));
                start = at + length;
            }
            return pieces;
        }

        /// <summary>A line without the clause mark it was cut after.</summary>
        private static string Close(string line) => line.TrimEnd().TrimEnd('·', '-', ';', ':', ',').TrimEnd();

        /// <summary>
        /// The text as slip lines of about <see cref="LineWords"/> counted words: whole clauses gathered while they fit, a clause past
        /// <see cref="LineCap"/> cut at its words (eight a line, a tail of two or fewer kept on the line before). <paramref name="whole"/> is false where
        /// a clause was cut - its words then span two lines and only the whole text holds the clause.
        /// </summary>
        public static List<string> Pack(string text, out bool whole)
        {
            whole = true;
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text)) { return lines; }
            string body = text.Trim();
            if (body.EndsWith(".", StringComparison.Ordinal)) { body = body.Substring(0, body.Length - 1); }
            var gathered = new List<string>();
            string current = string.Empty;
            foreach (string piece in Pieces(body))
            {
                if (current.Length > 0 && RangeCaptions.CountedWords(current + piece) > LineWords) { gathered.Add(Close(current)); current = string.Empty; }
                current += piece;
            }
            if (Close(current).Length > 0) { gathered.Add(Close(current)); }
            foreach (string line in gathered)
            {
                if (RangeCaptions.CountedWords(line) <= LineCap) { lines.Add(line); continue; }
                whole = false;
                string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var chunk = new List<string>();
                int counted = 0;
                for (int i = 0; i < words.Length; i++)
                {
                    chunk.Add(words[i]);
                    if (RangeCaptions.CountedWords(words[i]) > 0) { counted++; }
                    int left = 0;
                    for (int j = i + 1; j < words.Length; j++) { if (RangeCaptions.CountedWords(words[j]) > 0) { left++; } }
                    if (counted >= LineWords && left > 2) { lines.Add(string.Join(" ", chunk)); chunk.Clear(); counted = 0; }
                }
                if (chunk.Count > 0) { lines.Add(string.Join(" ", chunk)); }
            }
            return lines;
        }

        /// <summary>
        /// A slip whose lines are <paramref name="text"/> packed (<see cref="Pack"/>), upper case. Where a clause was cut or the lines run past four,
        /// the first three stand and the fourth is <paramref name="term"/>, marked, whose level-2 slip holds the whole text as one sentence.
        /// </summary>
        public static Moved Fragments(string head, string text, string term, string rest = null)
        {
            var moved = new Moved { Rest = rest, Full = text, Slip = new SlipContent(head) };
            List<string> lines = Pack(text, out bool whole);
            if (whole && lines.Count <= MaxLines)
            {
                foreach (string line in lines) { moved.Slip.Add(line.ToUpperInvariant()); }
                return moved;
            }
            for (int i = 0; i < Math.Min(lines.Count, MaxLines - 1); i++) { moved.Slip.Add(lines[i].ToUpperInvariant()); }
            moved.Slip.Add("[[" + term + "]]");
            moved.Terms[term] = new SlipContent(term).Add(text.ToUpperInvariant());
            return moved;
        }

        // ---- the statute book -----------------------------------------------------------------------------------------------------------------

        /// <summary>The laws card with nothing in it: the two counts at rest; the sentence on its slip.</summary>
        public static Moved EmptyList(string legislature)
        {
            const string rest = "0 in force · 0 pending";
            var moved = new Moved
            {
                Rest = rest,
                Full = "No law is in force or before " + legislature + " - the statute book below enacts them.",
                Slip = new SlipContent(rest.ToUpperInvariant()).Add(("No law is in force or before " + legislature).ToUpperInvariant()).Add("THE STATUTE BOOK BELOW ENACTS THEM"),
            };
            return moved;
        }

        /// <summary>The statute book's bottom bar: the count at rest; what it counts on its slip.</summary>
        public static Moved AffordableCount(int affordable, int shown)
        {
            string counts = affordable + " of " + shown;
            return new Moved
            {
                Rest = "Affordable: " + counts,
                Full = "Affordable now: " + counts + " shown",
                Slip = new SlipContent("AFFORDABLE NOW · " + counts.ToUpperInvariant() + " SHOWN").Add("COST AT OR UNDER THE APPROVAL ON HAND").Add("OF THE LAWS THE FILTERS SHOW"),
            };
        }

        /// <summary>A law outside this House's competence: the reason's head at rest (its words before the dash); the action it replaces and every clause on
        /// the slip.</summary>
        public static Moved Competence(string reason, string actionWords)
        {
            int dash = reason.IndexOf(" - ", StringComparison.Ordinal);
            return Fragments(actionWords + " · NOT OFFERED", reason, "THE REASON IN FULL", dash > 0 ? reason.Substring(0, dash) : reason);
        }

        /// <summary>A pending law bill's line in the pane: what is before the legislature at rest; when it resolves on the slip.</summary>
        public static Moved PaneBill(bool repeal, string legislature, string resolves)
        {
            string rest = (repeal ? "Repeal" : "Enactment") + " before " + legislature;
            var slip = new SlipContent(rest.ToUpperInvariant());
            foreach (string part in resolves.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries)) { slip.Add(part.ToUpperInvariant()); }
            return new Moved { Rest = rest, Full = rest + " - " + resolves + ".", Slip = slip };
        }

        // ---- the tabs' bills --------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// A tier bill's status under its action. At rest: whether a bill stands before the legislature (and, while a draft carries a figure the page
        /// computes, that figure in <paramref name="restWithFigure"/>); on the status's slip, the sentence's clauses one a line - the draft's clause, the
        /// figure's line, the draft's estimate as one sentence behind the marked DRAFT (level 2), and where a Congress votes the bill, the rule on the
        /// tariff behind the legislature's marked name. <paramref name="full"/> is the sentence as it stood - the † view prints it.
        /// </summary>
        public static Moved BillStatus(string bill, string legislature, string full, string pendingResolves, string draftClause,
            string restWithFigure = null, string figureLine = null, string estimate = null, string congressRule = null)
        {
            string article = "AEIOU".IndexOf(char.ToUpperInvariant(bill[0])) >= 0 ? "An " : "A ";
            string legislatureCaps = legislature.ToUpperInvariant();
            string named = congressRule != null ? "[[" + legislatureCaps + "]]" : legislatureCaps;
            var moved = new Moved { Full = full, Slip = new SlipContent("THE " + bill.ToUpperInvariant() + " BILL") };
            if (pendingResolves != null)
            {
                moved.Rest = bill + " bill before " + legislature;
                moved.Slip.Add((article + bill + " bill is before ").ToUpperInvariant() + named);
                foreach (string part in pendingResolves.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries)) { moved.Slip.Add(part.ToUpperInvariant()); }
            }
            else
            {
                moved.Rest = restWithFigure ?? "No " + bill + " bill before " + legislature;
                moved.Slip.Add(("No " + bill + " bill before ").ToUpperInvariant() + named);
                string clause = draftClause.ToUpperInvariant();
                if (estimate != null && clause.EndsWith(" DRAFT", StringComparison.Ordinal)) { clause = clause.Substring(0, clause.Length - "DRAFT".Length) + "[[DRAFT]]"; }
                moved.Slip.Add(clause);
                if (!string.IsNullOrEmpty(figureLine)) { moved.Slip.Add(figureLine.ToUpperInvariant()); }
                if (estimate != null) { moved.Terms["DRAFT"] = new SlipContent("DRAFT").Add(estimate.ToUpperInvariant()); }
            }
            if (congressRule != null) { moved.Terms[legislatureCaps] = new SlipContent(legislatureCaps).Add(congressRule.ToUpperInvariant()); }
            return moved;
        }
    }
}
