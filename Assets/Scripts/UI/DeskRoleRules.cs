using System;
using System.Collections.Generic;

namespace PoliSim.UI
{
    /// <summary>
    /// §694 (ruled 2026-09-30): **WHAT THE DESK'S EFFECTS CARD ESTIMATES, BY ROLE.** A governing player's card estimates the player's own
    /// draft; any other role's estimates what is BEFORE THE CHAMBER first - the player's alternative once tabled, else the government's budget -
    /// then, with neither, the player's alternative as drafted (where the role may table one), and last the book as it stands. The order was
    /// the other way round on the first film: an opposition player whose sheet held any draft - one stray drag - never saw the government's
    /// budget on the Desk while it stood before the chamber (93a showed YOUR ALTERNATIVE · AS DRAFTED); the draft's own effect stays on the
    /// budget sheet, beside the sliders that make it.
    /// </summary>
    public enum DeskEffectsSubject
    {
        /// <summary>Governing, a draft that moves the estimate (a budget-sheet draft, or the rate lever where the player holds it).</summary>
        YourDraft,
        /// <summary>Governing, nothing drafted that moves the estimate.</summary>
        NothingDrafted,
        /// <summary>Not governing: the player's alternative budget, tabled against the government's.</summary>
        YourAlternativeTabled,
        /// <summary>Not governing: the player's alternative as drafted on the budget sheet, not yet tabled.</summary>
        YourAlternativeDrafted,
        /// <summary>Not governing: the government's budget bill, before the chamber.</summary>
        GovernmentDraft,
        /// <summary>Not governing, nothing before the chamber: the book as it stands.</summary>
        StandingBook,
    }

    /// <summary>
    /// §694: **THE EFFECTS NOTE NAMES ONLY A LEVER THE PLAYER HOLDS.** Pure - the Desk draws what this returns and `DeskRoleCheck` proves
    /// it over every role: the rate lever is named only where the player holds it (it is the prime minister's, and a country whose rate a
    /// chair sets has none), the budget sheet only for a governing player, the player's alternative only where the role may table one.
    /// </summary>
    public static class DeskEffectsNote
    {
        /// <summary>The words that name a lever - the check reads a note for each and holds that the role holds it.</summary>
        public const string RateDial = "THE RATE DIAL", RatePush = "THE RATE PUSH", BudgetSheet = "THE BUDGET SHEET", Alternative = "YOUR ALTERNATIVE";

        /// <summary>The card's subject: <paramref name="governs"/> the player introduces the budget (the prime minister's party); <paramref name="draftMoves"/>
        /// the budget sheet's draft moves the estimate; <paramref name="rateDrafted"/> the rate lever is held and moved; <paramref name="mayTable"/> the
        /// role may table an alternative (not a junior partner, and the country's budget procedure sourced).</summary>
        public static DeskEffectsSubject SubjectOf(bool governs, bool draftMoves, bool rateDrafted, bool alternativeTabled, bool mayTable, bool governmentBillPending)
        {
            if (governs) { return draftMoves || rateDrafted ? DeskEffectsSubject.YourDraft : DeskEffectsSubject.NothingDrafted; }
            if (alternativeTabled) { return DeskEffectsSubject.YourAlternativeTabled; }
            if (governmentBillPending) { return DeskEffectsSubject.GovernmentDraft; }
            return mayTable && draftMoves ? DeskEffectsSubject.YourAlternativeDrafted : DeskEffectsSubject.StandingBook;
        }

        /// <summary>Whether the card draws its dashed empty state (nothing of the player's or before the chamber moves the estimate).</summary>
        public static bool IsEmptyState(DeskEffectsSubject subject) => subject == DeskEffectsSubject.NothingDrafted || subject == DeskEffectsSubject.StandingBook;

        /// <summary>§726 (v3.5): the card's NAME says whose budget the arrows estimate - §694's ruling kept at rest, where the scope line under the arrows,
        /// now the head's slip, used to say it.</summary>
        public static string Head(DeskEffectsSubject subject)
        {
            switch (subject)
            {
                case DeskEffectsSubject.YourDraft: return "Effects of your draft";
                case DeskEffectsSubject.YourAlternativeTabled: return "Effects of your alternative";
                case DeskEffectsSubject.YourAlternativeDrafted: return "Effects of your alternative, drafted";
                case DeskEffectsSubject.GovernmentDraft: return "Effects of the government's budget";
                default: return "Estimated effects";
            }
        }

        private const string Tail = "NO MARGIN: THE PROJECTION IS DETERMINISTIC · SCALED DISPLAY ESTIMATE, NOT A SIMULATED SUB-YEAR VALUE";

        /// <summary>
        /// The note under the arrows: the dashed empty state's whole caption, or the subject line above the methodology caption.
        /// <paramref name="rateLever"/> is <see cref="RateDial"/> or <see cref="RatePush"/> where the player holds a rate lever, else null;
        /// <paramref name="daysRemaining"/> the days until the chamber decides the budget before it.
        /// </summary>
        public static string Line(DeskEffectsSubject subject, string rateLever, bool mayTable, int daysRemaining)
        {
            string days = daysRemaining == 1 ? "1 DAY" : daysRemaining + " DAYS";
            switch (subject)
            {
                case DeskEffectsSubject.NothingDrafted:
                    return "NO DRAFT PENDING — ESTIMATES FOLLOW " + BudgetSheet + (rateLever != null ? " AND " + rateLever : string.Empty) + " AS DRAFTED AND EVERY BILL AS IT PASSES · " + Tail;
                case DeskEffectsSubject.StandingBook:
                    return "NO BUDGET BEFORE THE CHAMBER — THE BOOK AS IT STANDS, UNTIL THE GOVERNMENT TABLES ITS BUDGET"
                        + (mayTable ? " OR YOU DRAFT " + Alternative : string.Empty) + " · " + Tail;
                case DeskEffectsSubject.YourDraft:
                    return "NO MARGIN — THE PROJECTION IS DETERMINISTIC";   // C-C14's line, unchanged: a governing draft is the card's first subject
                case DeskEffectsSubject.YourAlternativeTabled:
                    return Alternative + " · TABLED, THE CHAMBER DECIDES IN " + days;
                case DeskEffectsSubject.YourAlternativeDrafted:
                    return Alternative + " · AS DRAFTED, NOT TABLED";
                case DeskEffectsSubject.GovernmentDraft:
                    return "THE GOVERNMENT'S BUDGET · THE CHAMBER DECIDES IN " + days;
                default:
                    throw new ArgumentOutOfRangeException(nameof(subject));
            }
        }

        /// <summary>The methodology caption under a subject line (a subject that is not the player's own draft carries the margin's clause here).</summary>
        public static string Method(DeskEffectsSubject subject, int daysPerTurn)
        {
            string method = $"SCALED DISPLAY ESTIMATE — FROM THE {daysPerTurn}-DAY PROJECTION, NOT A SIMULATED SUB-YEAR VALUE";
            return subject == DeskEffectsSubject.YourDraft ? method : "NO MARGIN · " + method;
        }

        /// <summary>The levers a note names that the role does not hold - empty when the note is true to the role. The check's own reading.</summary>
        public static List<string> UnheldLeversNamed(string note, bool governs, string rateLever, bool mayTable)
        {
            var unheld = new List<string>();
            if (note.Contains(RateDial) && rateLever != RateDial) { unheld.Add(RateDial); }
            if (note.Contains(RatePush) && rateLever != RatePush) { unheld.Add(RatePush); }
            if (note.Contains(BudgetSheet) && !governs) { unheld.Add(BudgetSheet); }
            if (note.Contains(Alternative) && (governs || !mayTable)) { unheld.Add(Alternative); }
            return unheld;
        }
    }

    /// <summary>
    /// §694 (ruled 2026-09-30): **THE RAIL LIGHTS EXACTLY ONE CELL.** One rule decides the lit cell and every rail cell reads it: the
    /// campaign's while its page is open over the stage and its cell is on the rail, the Desk's while the Desk is up, else the open
    /// document's. Before it each cell judged itself, and the campaign page - which overlays the stage without changing the tab - lit
    /// CAMPAIGN and the document beneath it at once (Design's sighting 21c, §687). `DeskRoleCheck` proves one lit cell in every state and
    /// that its count names the two the old per-cell rule lit; the draw counts what it lit and says so on a frame that lights any other number.
    /// </summary>
    public static class RailLit
    {
        public const string Desk = "DESK", Campaign = "CAMPAIGN";

        /// <summary>The seven documents' captions, in the rail's order - the cells between the Desk and the campaign.</summary>
        public static readonly string[] Documents = { "STATS", "DOCKET", "PEOPLE", "BUDGET", "LAWS", "POLITICS", "ENERGY" };

        /// <summary>The one lit cell.</summary>
        public static string Of(bool onDesk, bool campaignOpen, bool campaignCellPresent, string document)
            => campaignOpen && campaignCellPresent ? Campaign : onDesk ? Desk : document;

        /// <summary>The cells the rail draws: the Desk, the seven documents, the campaign's while it is present.</summary>
        public static List<string> Drawn(bool campaignCellPresent)
        {
            var cells = new List<string> { Desk };
            cells.AddRange(Documents);
            if (campaignCellPresent) { cells.Add(Campaign); }
            return cells;
        }

        /// <summary>The drawn cells a rule lights - the check counts with it, and the draw's own count is the same list.</summary>
        public static List<string> Lit(IEnumerable<string> drawn, Func<string, bool> isLit)
        {
            var lit = new List<string>();
            foreach (string cell in drawn) { if (isLit(cell)) { lit.Add(cell); } }
            return lit;
        }
    }
}
