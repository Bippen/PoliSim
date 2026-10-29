using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// D-NIGHT (Design's answer of 2026-10-28, boards 31a-31c, `PoliSim-captures/design/RETURN_2026-10-28/`): <b>WHO GOVERNS, REDRAWN IN V3.5</b> - the right
    /// column of the Swedish, German and Italian nights, in the built column's rect, at Design's sizes (a board pixel is 1.5 canvas units, so 14 board px is
    /// 14 px at 1280 and 28 at 2560: nothing reflows, 31c). Instruments and short words at rest; every longer line kept WORD FOR WORD in the slips and in the
    /// night-wide † view, which swaps the column for the full lines and scrolls inside the same rect. Block by block, as 31b's table sets them:
    /// <list type="bullet">
    /// <item><description><b>Head</b> - "Who governs" and the PROJECTED stamp (its slip: office changes only when the chamber votes); "owed" while counting.</description></item>
    /// <item><description><b>The round</b> - the procedure's steps as cells (the Speaker's four proposals, RF 6 kap. 5 §; Art. 63 GG's three ballots; Italy's three
    /// mandates): the step the projection comes from outlined with the asked party's mark, a refused one struck in BAD, one not reached dashed. The cells are the
    /// projection's own (`GovernmentFormation.View.Round`), never re-derived here.</description></item>
    /// <item><description><b>Outcome</b> - the cabinet's marks, the government's kind, its seats; <b>Support</b> where any.</description></item>
    /// <item><description><b>The vote</b> in the desk's division grammar - for in GOOD (support hatched), abstain in tan, against in BAD - with the tick WHERE THE RULE
    /// COUNTS: under the negative rule a majority against refuses (175 from the against end, RF 6 kap. 4 §), elsewhere a majority for elects (the Bundestag's
    /// 316 from the for end; art. 94 once per Italian chamber). The built bar put Sweden's tick 175 from the left and called it the majority: Design's
    /// correction, taken. Each party's side and reason are the procedure's (`View.Votes`), named in the slip and the † view.</description></item>
    /// <item><description><b>You</b> - the player's mark and the stamp (EXPECTED IN OFFICE / EXPECTED OUT OF OFFICE / CARETAKER SERVES ON), and where the projection
    /// read the player's party's answer by the AI's rule (K119), one line under it.</description></item>
    /// <item><description><b>Why</b> - the move once, the bridge (13b: 1 pp = 20 units, a lane of 36 to 60 units, the scale shrunk only above the cap), the eleven acts
    /// as a strip with a zero struck, and the largest fall and the largest rise alone, printed in the ledger's fixed order.</description></item>
    /// <item><description><b>If the budget stands</b> - the six outcomes as ▲/▼ with figures (v3.5), in their direction's ink; the plate's title, citation and scope line in
    /// the head's slip. Sweden's and Italy's nights only: no German budget act carries one.</description></item>
    /// </list>
    /// ⚠ Declared deviations: the night's faces have no bold Courier (§648: a bold STYLE on the night is a synthetic weight the guard fails), so Design's "mono bold"
    /// figures are the Document face at its weight; numbers read with a point, the surface's locale (§568), where the board wrote a comma.
    /// </summary>
    public partial class ElectionNightScreen
    {
        /// <summary>[AUTHORED] layout: Design's column width at 1280 (330 px, 31a) in canvas units - the width a block's word is measured against.</summary>
        private static float GovernsWidth => 330f * BoardPx;

        /// <summary>The night's column at rest and its † view (null where the night has none: the Polish and US nights).</summary>
        private GameObject _governsAtRest, _governsDense;
        private Image _daggerFace;
        private Text _daggerText;

        /// <summary>D-NIGHT ("For Code" 6): the † view is showing in the column's place.</summary>
        public bool DenseShown { get; private set; }

        /// <summary>The † cell at the masthead's right: the column's full lines, and back.</summary>
        public void ToggleDense() { SetDense(!DenseShown); }

        public void SetDense(bool on)
        {
            if (_governsAtRest == null || _governsDense == null) { return; }
            DenseShown = on;
            _governsAtRest.SetActive(!on);
            _governsDense.SetActive(on);
            if (_daggerFace != null) { _daggerFace.color = on ? PoliSimTheme.TextPrimary : PoliSimTheme.ModalBackground; }
            if (_daggerText != null) { _daggerText.color = on ? PoliSimTheme.ModalBackground : PoliSimTheme.TextPrimary; }
            if (on)
            {
                var content = (RectTransform)_governsDense.GetComponent<ScrollRect>().content;
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            }
            ApplyItalyViews();   // D-IT: Italy's † cell swaps the count and the map for their full wording too (ElectionNightScreen.Italy.cs); none on any other night
        }

        /// <summary>Everything the column reads, handed in by the night - none of it from the board.</summary>
        private sealed class GovernsNight
        {
            public NightState State;
            public string[] PartyNames;
            public GovernmentFormation.View Government;
            public CountryId Country;
            public int TotalSeats;
            public VoteAttribution.Ledger Ledger;
            public string LedgerParty;
            public bool BudgetBlock;
            public IReadOnlyList<DivisionEffect> StandingBudget;
            public string StandingBudgetCitation;
            public Transform Overlay;
        }

        /// <summary>One line of the † view: a head or a body line, kept whole.</summary>
        private readonly struct DenseLine
        {
            public readonly string Text;
            public readonly bool Head;

            public DenseLine(string text, bool head)
            {
                Text = text;
                Head = head;
            }
        }

        /// <summary>The † cell at the masthead's right (28a's, night-wide): paper with a rule, inked while the † view shows.</summary>
        private static void DaggerCell(Transform parent, ElectionNightScreen screen)
        {
            var go = new GameObject("Dagger");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            Image face = go.AddComponent<Image>();
            face.color = PoliSimTheme.ModalBackground;
            LayoutElement size = go.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = Px(22f);
            size.minHeight = size.preferredHeight = Px(20f);
            size.flexibleWidth = 0f;
            Text glyph = CanvasChrome.MakeTextRealWeight(go.transform, "Glyph", "†", PoliSimTheme.Document, Px(14f), PoliSimTheme.TextPrimary, TextAnchor.MiddleCenter);
            glyph.raycastTarget = false;
            var r = glyph.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            CanvasRows.Edges(go.transform, PoliSimTheme.TextPrimary, 1.5f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(screen.ToggleDense);
            CanvasRows.Slip(go, screen.Root.transform, "†", new[] { "Who governs, every line in full", "The column's place, scrolled inside it; again for the column" });
            screen._daggerFace = face;
            screen._daggerText = glyph;
        }

        /// <summary>D-NIGHT: the column - at rest, and its † view in the same rect.</summary>
        private static void BuildGoverns(ElectionNightScreen screen, Transform column, GovernsNight n)
        {
            column.GetComponent<VerticalLayoutGroup>().spacing = 0f;
            var atRest = new GameObject("AtRest");
            atRest.transform.SetParent(column, false);
            atRest.AddComponent<RectTransform>();
            var v = atRest.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.spacing = 0f;
            Transform rest = atRest.transform;

            var dense = new List<DenseLine>();
            bool counting = n.State != null && !n.State.Complete;
            GovernmentFormation.View g = counting ? null : n.Government;

            // the review's H1: the column is planned before it is drawn - its height summed block by block and the shrink order run until it fits its rect
            ColumnPlan plan = NightColumnPlan.PlanColumn(CaseOf(n, g, counting));
            if (plan.Shrunk.Count > 0) { Debug.Log("CANVAS: the night's column shrinks to fit " + ColumnPx.ToString(CultureInfo.InvariantCulture) + " px (" + plan.Natural.ToString(CultureInfo.InvariantCulture) + " natural, " + plan.Final.ToString(CultureInfo.InvariantCulture) + " drawn): " + string.Join("; ", plan.Shrunk)); }

            GovernsHead(rest, n, counting, dense);
            GovernsRound(rest, n, g, counting, dense);
            GovernsOutcome(rest, n, g, counting, dense);
            GovernsVotes(rest, n, g, counting, plan, dense);
            GovernsYou(rest, n, g, counting, plan, dense);
            GovernsWhy(rest, n, plan, dense);
            if (n.BudgetBlock) { GovernsBudget(rest, n, plan, dense); }

            // review 2's N5: the plan and the drawing cannot part silently - the built column's own preferred height against the plan's sum (a unit and a
            // half of slack: the bridge's natural height is rounded to a whole board pixel in the plan)
            var restRect = (RectTransform)rest;
            LayoutRebuilder.ForceRebuildLayoutImmediate(restRect);
            float built = LayoutUtility.GetPreferredHeight(restRect);
            if (Mathf.Abs(built - plan.Final * BoardPx) > 1.5f)
            {
                Debug.LogError(string.Format(CultureInfo.InvariantCulture, "CANVAS: the night's column was planned at {0} px ({1:F1} units) and built at {2:F1} units - the plan and the drawing have parted ({3})",
                    plan.Final, plan.Final * BoardPx, built, CaseOf(n, g, counting)));
            }

            screen._governsAtRest = atRest;
            screen._governsDense = DenseView(column, dense);
            screen._governsDense.SetActive(false);
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE BLOCKS
        // ---------------------------------------------------------------------------------------------------------------------------------------

        private static void GovernsHead(Transform parent, GovernsNight n, bool counting, List<DenseLine> dense)
        {
            Transform head = CanvasRows.HRow(parent, "GovernsHead", Px(28f), Px(8f));
            Hold(PlText(head, "Heading", "Who governs", Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            CanvasRows.Spacer(head);
            string vote = VoteSentence(n);
            if (counting)
            {
                Hold(PlText(head, "Owed", "owed", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                dense.Add(new DenseLine(n.Country == CountryId.Germany ? "WHO GOVERNS - WHEN THE LAST LAND DECLARES" : "WHO GOVERNS - WHEN THE COUNT IS COMPLETE", true));
                dense.Add(new DenseLine(n.Country == CountryId.Germany ? "the chamber waits with the seats, owed until the last Land declares" : "the chamber is not final until the last valkrets declares", false));
                dense.Add(new DenseLine("Nothing is projected on a partial chamber", false));
                return;
            }
            GameObject stamp = CanvasRows.Stamp(head, "PROJECTED", Px(14f), PoliSimTheme.TextPrimary);
            CanvasRows.Slip(stamp, n.Overlay, "PROJECTED", SlipWrap("What " + ProcedureWords(n.Country) + " is expected to form", "Nothing is installed tonight", "Office changes only when " + vote, Foot(n)));
            dense.Add(new DenseLine("WHO GOVERNS - EXPECTED TO FORM", true));
            dense.Add(new DenseLine(Foot(n), false));
            if (n.Country == CountryId.Germany)
            {
                // the German column's lines as first built (§707), kept whole
                int majority = n.TotalSeats / 2 + 1;
                dense.Add(new DenseLine(string.Format(CultureInfo.InvariantCulture, "THE BUNDESTAG ELECTS THE CHANCELLOR · {0} of {1}", majority, n.TotalSeats), false));
                dense.Add(new DenseLine("the Bundespräsident proposes, the Bundestag elects once it convenes (Art. 63 GG)", false));
                string fraktionen = FraktionLine(n);
                if (fraktionen != null)
                {
                    // the review's L5: the Fraktion line itself (the Union one block, § 10 Abs. 1 GO-BT), with its legend as first built
                    dense.Add(new DenseLine(fraktionen, false));
                    dense.Add(new DenseLine(string.Format(CultureInfo.InvariantCulture, "the Fraktionen, largest first - the Union's two lists as one"), false));
                }
            }
        }

        /// <summary>The German chamber by Fraktion as the night first drew it - the Union's two lists one block, every other seated list its own, largest first.</summary>
        private static string FraktionLine(GovernsNight n)
        {
            if (n.State == null || !n.State.Complete || n.PartyNames == null || n.State.SeatsOnCounted == null) { return null; }
            var blocks = new List<(string Name, int Seats)>();
            int union = 0;
            for (int p = 0; p < n.PartyNames.Length && p < n.State.SeatsOnCounted.Length; p++)
            {
                int seats = n.State.SeatsOnCounted[p];
                if (seats <= 0) { continue; }
                if (n.PartyNames[p] == "CDU" || n.PartyNames[p] == "CSU") { union += seats; continue; }
                blocks.Add((PartySystems.ShortName(CountryId.Germany, n.PartyNames[p]).ToUpperInvariant(), seats));
            }
            if (union > 0) { blocks.Add(("UNION", union)); }
            blocks.Sort((a, b) => b.Seats.CompareTo(a.Seats));
            var parts = new List<string>(blocks.Count);
            foreach ((string name, int seats) in blocks) { parts.Add(name + " " + seats.ToString(CultureInfo.InvariantCulture)); }
            return string.Join(" · ", parts);
        }

        private static void GovernsRound(Transform parent, GovernsNight n, GovernmentFormation.View g, bool counting, List<DenseLine> dense)
        {
            int limit = counting ? DefaultCells(n.Country) : g?.RoundLimit ?? 0;
            if (!counting && (g == null || g.Round.Count == 0)) { return; }   // a staged fixture's view carries no round: nothing is drawn in its place
            Gap(parent, 6f);
            Transform row = CanvasRows.HRow(parent, "Round", Px(24f), Px(10f));
            Hold(PlText(row, "Label", RoundLabel(n.Country), Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
            var cells = new GameObject("Cells");
            cells.transform.SetParent(row, false);
            cells.AddComponent<RectTransform>();
            var h = cells.AddComponent<HorizontalLayoutGroup>();
            h.spacing = Px(4f);
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            int at = 0;
            for (int i = 0; i < limit; i++)
            {
                GovernmentFormation.RoundCell c = counting || i >= g.Round.Count ? new GovernmentFormation.RoundCell(null, GovernmentFormation.RoundCellState.NotReached) : g.Round[i];
                RoundCellView(cells.transform, n.Country, c);
                if (c.State != GovernmentFormation.RoundCellState.NotReached) { at = i + 1; }
            }
            string fig = counting ? "owed" : at.ToString(CultureInfo.InvariantCulture) + " of " + limit.ToString(CultureInfo.InvariantCulture);
            HoldRight(PlText(row, "Figure", fig, Px(14f), counting ? PoliSimTheme.TextMuted : PoliSimTheme.TextPrimary, PoliSimTheme.Document));   // no spacer: one gap fewer in a row the label nearly fills

            var lines = new List<string>();
            if (counting) { lines.Add("Projected once the count is complete"); }
            else
            {
                if (!string.IsNullOrEmpty(g.ProjectionStep)) { lines.Add(g.ProjectionStep); }
                for (int i = 0; i < g.Round.Count; i++)
                {
                    GovernmentFormation.RoundCell c = g.Round[i];
                    if (c.State == GovernmentFormation.RoundCellState.NotReached) { continue; }
                    lines.Add(StepWord(n.Country) + " " + (i + 1).ToString(CultureInfo.InvariantCulture) + " · " + (c.Asked != null ? Short(n.Country, c.Asked) : "no party")
                        + (c.State == GovernmentFormation.RoundCellState.Current ? " · the step the projection comes from" : " · refused"));
                }
            }
            lines.Add(RoundRule(n.Country));
            CanvasRows.Slip(row.gameObject, n.Overlay, RoundLabel(n.Country), SlipWrap(lines.ToArray()));
            if (!counting) { foreach (string l in lines) { dense.Add(new DenseLine(l, false)); } }
        }

        /// <summary>One 22 px cell: the current step ruled in ink with the asked party's mark, a refused step struck in BAD (its mark faded), the last refusal ruled in BAD,
        /// a step not reached dashed.</summary>
        private static void RoundCellView(Transform parent, CountryId country, GovernmentFormation.RoundCell c)
        {
            var go = new GameObject("Cell " + c.State);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = Px(22f);
            le.flexibleWidth = 0f;
            switch (c.State)
            {
                case GovernmentFormation.RoundCellState.Current: CanvasRows.Edges(go.transform, PoliSimTheme.TextPrimary, 1.5f); break;
                case GovernmentFormation.RoundCellState.LastRefused: CanvasRows.Edges(go.transform, PoliSimTheme.Bad, 1.5f); break;
                case GovernmentFormation.RoundCellState.Refused: CanvasRows.Edges(go.transform, PoliSimTheme.HairlineStrong, 1.5f); break;
                default: DashedOutline(go.transform, PoliSimTheme.Hairline, 4, Px(22f)); break;
            }
            if (c.Asked != null && c.State != GovernmentFormation.RoundCellState.NotReached)
            {
                Texture2D mark = MarkOf(country, c.Asked);
                if (mark != null)
                {
                    var m = new GameObject("Mark " + c.Asked);
                    m.transform.SetParent(go.transform, false);
                    RawImage image = m.AddComponent<RawImage>();
                    image.texture = mark;
                    image.raycastTarget = false;
                    image.color = new Color(1f, 1f, 1f, c.State == GovernmentFormation.RoundCellState.Current ? 1f : 0.45f);
                    m.AddComponent<LayoutElement>().ignoreLayout = true;
                    RectTransform r = image.rectTransform;
                    r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                    r.sizeDelta = new Vector2(Px(16f), Px(16f));
                }
            }
            if (c.State == GovernmentFormation.RoundCellState.Refused || c.State == GovernmentFormation.RoundCellState.LastRefused)
            {
                var s = new GameObject("Struck");
                s.transform.SetParent(go.transform, false);
                Image bar = s.AddComponent<Image>();
                bar.color = PoliSimTheme.Bad;
                bar.raycastTarget = false;
                s.AddComponent<LayoutElement>().ignoreLayout = true;
                RectTransform r = bar.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(Px(28f), Px(2f));
                r.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
        }

        private static void GovernsOutcome(Transform parent, GovernsNight n, GovernmentFormation.View g, bool counting, List<DenseLine> dense)
        {
            Gap(parent, 8f);
            Rule(parent, PoliSimTheme.RuleRow);
            Transform row = CanvasRows.HRow(parent, "Outcome", Px(30f), Px(8f));
            if (counting)
            {
                Hold(PlText(row, "Waits", "When the count is complete", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                string declared = n.State.DeclaredCount.ToString(CultureInfo.InvariantCulture) + " of " + n.State.TotalConstituencies.ToString(CultureInfo.InvariantCulture);
                HoldRight(PlText(row, "Declared", declared, Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                CanvasRows.Slip(row.gameObject, n.Overlay, "When the count is complete", SlipWrap(declared + " declared", "Nothing is projected on a partial chamber"));
                return;
            }
            if (g == null)
            {
                Hold(PlText(row, "None", "No formation was run for this night", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                dense.Add(new DenseLine("no formation was run for this night", false));
                return;
            }
            if (!g.HasGovernment)
            {
                Hold(PlText(row, "Name", "No government expected", Px(16f), PoliSimTheme.Bad, PoliSimTheme.Body));
                foreach (string l in NoneLines(n.Country, g))
                {
                    Transform reason = CanvasRows.HRow(parent, "Reason", Px(20f), 0f);
                    Hold(PlText(reason, "Line", l, Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                }
                string none = "NO GOVERNMENT EXPECTED TO FORM · " + (g.NoneWords ?? (g.Partial ? "THE BALLOTS GO ON" : "THE ROUND BREAKS OFF"));
                var noneLines = new List<string> { none, g.Reason ?? string.Empty };
                if (!g.DeclarationsSourced) { noneLines.Add(DerivedNote); }
                CanvasRows.Slip(row.gameObject, n.Overlay, "No government expected", SlipWrap(noneLines.ToArray()));
                foreach (string l in noneLines) { if (!string.IsNullOrEmpty(l)) { dense.Add(new DenseLine(l, false)); } }
                return;
            }
            string name = OutcomeShort(g.Outcome);
            string seatsText = g.CabinetSeats.ToString(CultureInfo.InvariantCulture), ofText = "of " + g.TotalSeats.ToString(CultureInfo.InvariantCulture);
            // the review's L2: the marks fold to "+N" where the row cannot hold them beside the name and the seats
            float room = GovernsWidth - Measure(name, Px(16f), PoliSimTheme.Body) - Measure(seatsText, Px(16f), PoliSimTheme.Document) - Measure(ofText, Px(14f), PoliSimTheme.Document) - 3 * Px(8f);
            MarkRow(row, n.Country, g.Cabinet, room);
            Hold(PlText(row, "Name", name, Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            HoldRight(PlText(row, "Seats", seatsText, Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
            Hold(PlText(row, "Of", ofText, Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
            string kind = string.Format(CultureInfo.InvariantCulture, "{0} · {1} of {2} seats in cabinet", OutcomeName(g.Outcome), g.CabinetSeats, g.TotalSeats);
            string cabinet = "IN CABINET  " + ShortParties(n.Country, g.Cabinet);
            string support = g.Support.Count > 0 ? string.Format(CultureInfo.InvariantCulture, "SUPPORT FROM OUTSIDE  {0} · {1} with support", ShortParties(n.Country, g.Support), g.SupportedSeats) : "NO DECLARED SUPPORT FROM OUTSIDE";
            var lines = new List<string> { kind, cabinet, support };
            if (!g.DeclarationsSourced) { lines.Add(DerivedNote); }   // the review's H1 (a): the honesty sentence is the outcome's slip and the † view's - Design budgeted no row for it
            CanvasRows.Slip(row.gameObject, n.Overlay, name, SlipWrap(lines.ToArray()));
            foreach (string l in lines) { dense.Add(new DenseLine(l, false)); }

            if (g.Support.Count > 0)
            {
                Transform sup = CanvasRows.HRow(parent, "Support", Px(26f), Px(8f));
                Edge(sup, PoliSimTheme.RuleRow, top: true);
                Hold(PlText(sup, "Label", "Support", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                string seats = ShortParties(n.Country, g.Support);
                float seatsRoom = GovernsWidth - Measure("Support", Px(14f), PoliSimTheme.Document) - 3 * Px(8f) - Px(16f) - Px(28f);
                if (Measure(seats, Px(14f), PoliSimTheme.Document) > seatsRoom) { seats = (g.SupportedSeats - g.CabinetSeats).ToString(CultureInfo.InvariantCulture) + " seats"; }
                MarkRow(sup, n.Country, g.Support, GovernsWidth - Measure("Support", Px(14f), PoliSimTheme.Document) - Measure(seats, Px(14f), PoliSimTheme.Document) - 3 * Px(8f));
                HoldRight(PlText(sup, "Seats", seats, Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                CanvasRows.Slip(sup.gameObject, n.Overlay, "Support from outside", SlipWrap(ShortParties(n.Country, g.Support), "Votes for the cabinet without a seat in it", "Hatched in the bar"));
            }
        }

        /// <summary>The parties' marks in a row, as many as <paramref name="room"/> holds; the rest folded to "+N" (the slip names every party).</summary>
        private static void MarkRow(Transform row, CountryId country, List<(string Abbrev, int Seats)> parties, float room)
        {
            var marks = new GameObject("Marks");
            marks.transform.SetParent(row, false);
            marks.AddComponent<RectTransform>();
            var mh = marks.AddComponent<HorizontalLayoutGroup>();
            mh.spacing = Px(3f); mh.childControlWidth = true; mh.childControlHeight = true; mh.childForceExpandWidth = false; mh.childForceExpandHeight = false; mh.childAlignment = TextAnchor.MiddleLeft;
            float pitch = Px(16f) + Px(3f);
            int fit = Mathf.Max(0, Mathf.FloorToInt((room + Px(3f)) / pitch));
            int shown = parties.Count;
            if (shown > fit)
            {
                float fold = Measure("+" + parties.Count.ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.Document) + Px(3f);
                shown = Mathf.Max(1, Mathf.FloorToInt((room - fold + Px(3f)) / pitch));
            }
            for (int i = 0; i < shown && i < parties.Count; i++) { CanvasRows.Mark(marks.transform, country, parties[i].Abbrev, Px(16f)); }
            if (shown < parties.Count) { Hold(PlText(marks.transform, "Fold", "+" + (parties.Count - shown).ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document)); }
        }

        private const string DerivedNote = "projected on DERIVED red lines only - this country's declared refusals are not sourced";

        /// <summary>The votes the column draws: one block, one lane per chamber (Italy's two, art. 94; the review's H1 (b)), or the empty lane while counting.</summary>
        private static List<GovernmentFormation.NightVote> VotesOf(GovernsNight n, GovernmentFormation.View g, bool counting)
        {
            var votes = new List<GovernmentFormation.NightVote>();
            if (counting || g == null) { return votes; }
            if (g.Votes.Count > 0) { votes.AddRange(g.Votes); return votes; }
            if (!g.HasGovernment) { return votes; }
            // a staged fixture's view: the aggregate counts, no party named on a side
            votes.Add(new GovernmentFormation.NightVote
            {
                Members = g.TotalSeats, Needed = g.Majority, CountsAgainst = g.NegativeRule, For = g.SupportedSeats, Against = g.OpposedSeats,
                Abstain = Math.Max(0, g.TotalSeats - g.SupportedSeats - g.OpposedSeats), Carried = g.HasGovernment,
            });
            return votes;
        }

        /// <summary>One block of a lane: its seats, ink, hatch, the word drawn inside it (null where none fits) and the key's words where none does.</summary>
        private struct LaneBlock
        {
            public string Name;
            public int Seats;
            public Color Ink, Word;
            public bool Hatched;
            public string Fits, Key;
            /// <summary>The chamber the lane is (Italy's Camera or Senato), null for the night's one chamber - the key and the slip label a folded entry by it (review 2's N4).</summary>
            public string Chamber;
        }

        /// <summary>A lane's blocks in the division grammar - the cabinet in GOOD, support hatched, abstain in tan, against in BAD - each block's word chosen to
        /// fit <paramref name="laneWidth"/> (the board's width less the lane's prefix), a key entry where none does.</summary>
        private static List<LaneBlock> LaneBlocks(GovernmentFormation.NightVote v, GovernmentFormation.View g, float laneWidth)
        {
            int cabinet = 0, support = 0;
            if (v.Sides.Count > 0)
            {
                foreach (GovernmentFormation.VoteSide s in v.Sides)
                {
                    if (s.Side == CoalitionFormation.InvestitureSide.InCabinet) { cabinet += s.Seats; }
                    else if (s.Side == CoalitionFormation.InvestitureSide.Supports) { support += s.Seats; }
                }
            }
            else { cabinet = g != null ? Math.Min(g.CabinetSeats, v.For) : v.For; support = v.For - cabinet; }
            string forWords = "for " + v.For.ToString(CultureInfo.InvariantCulture);
            bool forOnCabinet = cabinet > 0;
            var blocks = new List<LaneBlock>();
            void Add(string name, int seats, Color ink, bool hatched, Color word, string[] words)
            {
                if (seats <= 0 || v.Members <= 0) { return; }
                string fits = null;
                if (words != null) { foreach (string w in words) { if (Measure(w, Px(14f), PoliSimTheme.Document) + Px(12f) <= laneWidth * seats / v.Members) { fits = w; break; } } }
                blocks.Add(new LaneBlock { Name = name, Seats = seats, Ink = ink, Hatched = hatched, Word = word, Fits = fits, Key = words != null && fits == null ? words[0] : null });
            }
            Color paper = PoliSimTheme.ModalBackground;
            Add("Cabinet", cabinet, PoliSimTheme.Good, false, paper, forOnCabinet ? new[] { forWords, "for" } : null);
            Add("Support", support, PoliSimTheme.Good, true, paper, forOnCabinet ? null : new[] { forWords, "for" });
            Add("Abstain", v.Abstain, PoliSimTheme.Hairline, false, PoliSimTheme.TextPrimary, new[] { "abstain " + v.Abstain.ToString(CultureInfo.InvariantCulture), "abstain" });
            Add("Against", v.Against, PoliSimTheme.Bad, false, paper, new[] { "against " + v.Against.ToString(CultureInfo.InvariantCulture), "against" });
            return blocks;
        }

        /// <summary>The width a lane has: the whole board's for one chamber, less the chamber's name, its glyph and its count where the block holds two.</summary>
        private static float LaneWidth(List<GovernmentFormation.NightVote> votes)
        {
            if (votes.Count <= 1) { return GovernsWidth; }
            float prefix = 0f;
            foreach (GovernmentFormation.NightVote v in votes)
            {
                prefix = Mathf.Max(prefix, Measure(v.Chamber ?? string.Empty, Px(14f), PoliSimTheme.Document) + Px(16f) + Measure(v.For.ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.Document) + 3 * Px(6f));
            }
            return GovernsWidth - prefix;
        }

        /// <summary>Whether the block owes a key line - a block too narrow to hold its name in any lane.</summary>
        private static bool NeedsKey(List<GovernmentFormation.NightVote> votes, GovernmentFormation.View g)
        {
            float w = LaneWidth(votes);
            foreach (GovernmentFormation.NightVote v in votes) { foreach (LaneBlock b in LaneBlocks(v, g, w)) { if (b.Key != null) { return true; } } }
            return false;
        }

        /// <summary>
        /// The vote: ✓/✗, the count where the rule counts and the words, then the bar - one lane, or one lane per chamber in ONE block (Italy, the review's
        /// H1 (b)), each lane with its own ✓/✗, count and tick; a key line for a block too narrow to hold its name, where the column's plan keeps it.
        /// The tick sits where the rule counts: the Riksdag's refusal line from the against end (RF 6 kap. 4 §), the Bundestag's majority of the members from
        /// the for end (Art. 63 Abs. 2), each Italian chamber's "more for than against" (art. 64: the tick at against + 1, from the for end).
        /// </summary>
        private static void GovernsVotes(Transform parent, GovernsNight n, GovernmentFormation.View g, bool counting, ColumnPlan plan, List<DenseLine> dense)
        {
            List<GovernmentFormation.NightVote> votes = VotesOf(n, g, counting);
            if (!counting && votes.Count == 0) { return; }
            bool italy = n.Country == CountryId.Italy;
            Gap(parent, 8f);
            var block = new GameObject("Vote");
            block.transform.SetParent(parent, false);
            block.AddComponent<RectTransform>();
            var bv = block.AddComponent<VerticalLayoutGroup>();
            bv.childControlWidth = true; bv.childControlHeight = true; bv.childForceExpandWidth = true; bv.childForceExpandHeight = false; bv.spacing = 0f;
            var lines = new List<string>();
            string title;

            if (counting)
            {
                // the empty lane: the Riksdag's and the Bundestag's line is known before the count (a majority of the members); Italy's is set by the votes
                // cast (art. 64), so its lane draws no tick and says so (the review's L6)
                int members = n.TotalSeats, needed = members / 2 + 1;
                bool against = ChamberCountsAgainst(n.Country);
                Transform line = CanvasRows.HRow(block.transform, "Count", Px(26f), Px(7f));
                Hold(PlText(line, "Figure", "—", Px(20f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                Hold(PlText(line, "Words", italy ? "for · more for than against" : (against ? "against · " : "for · ") + NeededWords(n.Country, needed, against), Px(15f), PoliSimTheme.TextMuted, PoliSimTheme.Body));
                Gap(block.transform, 4f);
                Transform lane = Lane(block.transform, Px(20f));
                DashedOutline(lane, PoliSimTheme.Hairline, 40, Px(20f));
                if (!italy) { LaneTick(lane, against ? (members - needed) / (double)members : needed / (double)members, PoliSimTheme.HairlineStrong); }
                title = "The vote";
                lines.Add("Owed until the count is complete");
                lines.Add(italy ? "Each chamber's line is more for than against (art. 64), set by the votes cast - no tick until they are"
                    : against ? "The tick is the refusal line: " + needed.ToString(CultureInfo.InvariantCulture) + " against"
                    : "The tick is " + needed.ToString(CultureInfo.InvariantCulture) + " for, a majority of the " + members.ToString(CultureInfo.InvariantCulture) + " members");
                CanvasRows.Slip(block, n.Overlay, title, SlipWrap(lines.ToArray()));
                return;
            }

            float laneWidth = LaneWidth(votes);
            var keys = new List<LaneBlock>();
            var foldedKeys = new HashSet<string>();
            if (votes.Count == 1)
            {
                GovernmentFormation.NightVote v = votes[0];
                Transform line = CanvasRows.HRow(block.transform, "Count", Px(26f), Px(7f));
                Verdict(line, v.Carried, Px(18f));
                int count = v.CountsAgainst ? v.Against : v.For;
                if (v.Decliners.Count > 0 || v.Withdrew.Count > 0)
                {
                    // the review's M2: the proposal fell on a drafted partner's answer, not on the vote - the row says so
                    Hold(PlText(line, "Fell", FellWords(n.Country, v) + " ·", Px(15f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
                    Hold(PlText(line, "Figure", count.ToString(CultureInfo.InvariantCulture), Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                    Hold(PlText(line, "Words", v.CountsAgainst ? "against" : "for", Px(15f), PoliSimTheme.TextMuted, PoliSimTheme.Body));
                }
                else
                {
                    Hold(PlText(line, "Figure", count.ToString(CultureInfo.InvariantCulture), Px(20f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                    Hold(PlText(line, "Words", (v.CountsAgainst ? "against · " : "for · ") + NeededWords(n.Country, v.Needed, v.CountsAgainst), Px(15f), PoliSimTheme.TextMuted, PoliSimTheme.Body));
                }
                Gap(block.transform, 4f);
                Transform lane = Lane(block.transform, Px(20f));
                DrawLane(lane, v, g, laneWidth, keys);
            }
            else
            {
                // Italy: one words line both chambers share (the same rule in each), then a lane per chamber with its own ✓/✗, count and tick
                Transform line = CanvasRows.HRow(block.transform, "Count", Px(26f), Px(7f));
                Hold(PlText(line, "Words", "Confidence · for, more for than against", Px(15f), PoliSimTheme.TextMuted, PoliSimTheme.Body));
                float prefixLabel = 0f;
                foreach (GovernmentFormation.NightVote v in votes) { prefixLabel = Mathf.Max(prefixLabel, Measure(v.Chamber ?? string.Empty, Px(14f), PoliSimTheme.Document)); }
                float prefixCount = 0f;
                foreach (GovernmentFormation.NightVote v in votes) { prefixCount = Mathf.Max(prefixCount, Measure(v.For.ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.Document)); }
                foreach (GovernmentFormation.NightVote v in votes)
                {
                    Gap(block.transform, 4f);
                    Transform row = CanvasRows.HRow(block.transform, "Lane " + v.Chamber, Px(20f), Px(6f));
                    CanvasRows.FixedCell(row, v.Chamber ?? string.Empty, prefixLabel + 2f, Px(14f), PoliSimTheme.TextMuted).horizontalOverflow = HorizontalWrapMode.Overflow;
                    Verdict(row, v.Carried, Px(16f));
                    CanvasRows.FixedCell(row, v.For.ToString(CultureInfo.InvariantCulture), prefixCount + 2f, Px(14f), PoliSimTheme.TextPrimary, TextAnchor.MiddleRight).horizontalOverflow = HorizontalWrapMode.Overflow;
                    Transform lane = Lane(row, Px(20f));
                    lane.GetComponent<LayoutElement>().flexibleWidth = 1f;
                    DrawLane(lane, v, g, laneWidth, keys);
                }
            }
            if (keys.Count > 0 && plan.Key)
            {
                Gap(block.transform, 6f);
                Transform key = CanvasRows.HRow(block.transform, "Key", Px(18f), Px(14f));
                var seen = new HashSet<string>();
                var entries = new List<LaneBlock>();
                foreach (LaneBlock b in keys) { if (seen.Add(b.Chamber + b.Name + b.Key)) { entries.Add(b); } }
                // review 3's N11: the key row is measured as the other rows are - an entry that would run past the board's width folds into "+N", its words to the slip
                float Width(LaneBlock b) => Px(12f) + Px(6f) + Measure(KeyWords(b), Px(14f), PoliSimTheme.Document);
                float used = 0f;
                int shown = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    float w = (i > 0 ? Px(14f) : 0f) + Width(entries[i]);
                    int left = entries.Count - i - 1;
                    float fold = left > 0 ? Px(14f) + Measure("+" + left.ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.Document) : 0f;
                    if (used + w + fold > GovernsWidth) { break; }
                    used += w;
                    shown++;
                }
                for (int i = 0; i < entries.Count; i++)
                {
                    if (i >= shown) { foldedKeys.Add(entries[i].Chamber + entries[i].Name + entries[i].Key); continue; }
                    Transform cell = CanvasRows.HRow(key, "KeyCell", Px(18f), Px(6f));
                    Swatch(cell, entries[i].Ink, entries[i].Hatched);
                    Hold(PlText(cell, "Words", KeyWords(entries[i]), Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                }
                if (shown < entries.Count) { Hold(PlText(key, "Fold", "+" + (entries.Count - shown).ToString(CultureInfo.InvariantCulture), Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document)); }
            }

            // the long wording, kept whole: the slip and the † view
            foreach (GovernmentFormation.NightVote v in votes)
            {
                title = v.CountsAgainst ? "Investiture by the negative rule" : v.Chamber != null ? v.Chamber + " · confidence (art. 94)" : n.Country == CountryId.Germany ? "The first ballot (Art. 63 GG)" : "Investiture by a majority for";
                var vl = new List<string>();
                if (v.Decliners.Count > 0 || v.Withdrew.Count > 0) { vl.Add("The proposal falls before the vote: " + FellWords(n.Country, v) + (string.IsNullOrEmpty(v.Fell) ? string.Empty : " (" + v.Fell + ")")); }
                vl.Add(v.CountsAgainst
                    ? string.Format(CultureInfo.InvariantCulture, "INVESTITURE BY THE NEGATIVE RULE: {0} seats vote against it, and it takes {1} against to refuse it", v.Against, v.Needed)
                    : v.Chamber != null ? string.Format(CultureInfo.InvariantCulture, "CONFIDENCE IN THE {0}: {1} for, {2} against - it carries with more for than against (art. 64)", v.Chamber.ToUpperInvariant(), v.For, v.Against)
                    : string.Format(CultureInfo.InvariantCulture, "INVESTITURE BY A MAJORITY FOR: {0} seats for it, {1} needed", v.For, v.Needed));
                if (v.Sides.Count > 0)
                {
                    vl.Add("For · " + SideList(n.Country, v, true, false));
                    if (v.Abstain > 0) { vl.Add("Abstain · " + SideList(n.Country, v, false, false)); }
                    if (v.Against > 0) { vl.Add("Against · " + SideList(n.Country, v, false, true)); }
                }
                else { vl.Add(string.Format(CultureInfo.InvariantCulture, "{0} for · {1} abstaining · {2} against - the parties on each side are not carried on this view", v.For, v.Abstain, v.Against)); }
                if (n.Country == CountryId.Germany && v.Chamber == null) { vl.Add("The ballot records no \"no\": a party that votes for no candidate abstains"); }   // K-c
                vl.Add(v.CountsAgainst
                    ? "Cabinet solid · support hatched · the tick is " + v.Needed.ToString(CultureInfo.InvariantCulture) + " against, the refusal line"
                    : v.Chamber != null ? "Cabinet solid · support hatched · the tick is " + v.Needed.ToString(CultureInfo.InvariantCulture) + " for, more for than against (art. 64)"
                    : "Cabinet solid · support hatched · the tick is " + v.Needed.ToString(CultureInfo.InvariantCulture) + " for, a majority of the " + v.Members.ToString(CultureInfo.InvariantCulture) + " members");
                var listed = new HashSet<string>(); foreach (LaneBlock b in keys) { if (b.Chamber == v.Chamber && (!plan.Key || foldedKeys.Contains(b.Chamber + b.Name + b.Key)) && listed.Add(b.Name + b.Key)) { vl.Add("Not drawn in its block" + (b.Chamber != null ? " (" + b.Chamber + ")" : string.Empty) + ": " + b.Key); } }
                lines.AddRange(vl);
                dense.Add(new DenseLine((v.Chamber != null ? v.Chamber.ToUpperInvariant() + " - " : string.Empty) + title.ToUpperInvariant(), true));
                foreach (string l in vl) { dense.Add(new DenseLine(l, false)); }
                foreach (GovernmentFormation.VoteSide s in v.Sides)
                {
                    dense.Add(new DenseLine(Short(n.Country, s.Abbrev) + " " + s.Seats.ToString(CultureInfo.InvariantCulture) + " · " + SideWord(s.Side) + (string.IsNullOrEmpty(s.Reason) ? string.Empty : " · " + s.Reason), false));
                }
            }
            title = votes.Count > 1 ? "Confidence of both chambers (art. 94)" : votes[0].CountsAgainst ? "Investiture by the negative rule" : n.Country == CountryId.Germany ? "The first ballot (Art. 63 GG)" : "Investiture by a majority for";
            CanvasRows.Slip(block, n.Overlay, title, SlipWrap(lines.ToArray()));
        }

        private static string NeededWords(CountryId c, int needed, bool against) =>
            needed.ToString(CultureInfo.InvariantCulture) + (against ? " needed to refuse" : c == CountryId.Germany ? " needed to elect" : " needed");

        /// <summary>A lane rect, its height fixed, its width the row's.</summary>
        private static Transform Lane(Transform parent, float height)
        {
            var go = new GameObject("VoteBar");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            LayoutElement size = go.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = height;
            size.minWidth = 0f;
            return go.transform;
        }

        /// <summary>One lane's blocks and its tick where the rule counts; the key entries it owes added to <paramref name="keys"/>.</summary>
        private static void DrawLane(Transform lane, GovernmentFormation.NightVote v, GovernmentFormation.View g, float laneWidth, List<LaneBlock> keys)
        {
            List<LaneBlock> blocks = LaneBlocks(v, g, laneWidth);
            double x = 0.0;
            for (int i = 0; i < blocks.Count; i++)
            {
                LaneBlock b = blocks[i];
                double from = x / v.Members, to = (x + b.Seats) / (double)v.Members;
                x += b.Seats;
                BarSegment(lane, b.Name, from, to, b.Ink, b.Hatched, b.Word, b.Fits, i == blocks.Count - 1);
                if (b.Key != null) { b.Chamber = v.Chamber; keys.Add(b); }
            }
            if (v.Members > 0) { LaneTick(lane, v.CountsAgainst ? (v.Members - v.Needed) / (double)v.Members : v.Needed / (double)v.Members, PoliSimTheme.TextPrimary); }
        }

        /// <summary>The tick: 2 px by 28 across a 20 px lane, at a fraction of its width.</summary>
        private static void LaneTick(Transform lane, double at, Color ink)
        {
            var tick = new GameObject("Tick");
            tick.transform.SetParent(lane, false);
            Image img = tick.AddComponent<Image>();
            img.color = ink;
            img.raycastTarget = false;
            RectTransform r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2((float)Math.Min(1.0, Math.Max(0.0, at)), 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.sizeDelta = new Vector2(3f, Px(28f));
            r.anchoredPosition = new Vector2(0f, Px(5f));
        }

        /// <summary>A key entry's words - the chamber's name first where the block holds two (review 2's N4).</summary>
        private static string KeyWords(LaneBlock b) => (b.Chamber != null ? b.Chamber + " " : string.Empty) + b.Key;

        /// <summary>Why a proposal fell before its vote, in the row's short words: "C declined to join", "SD withdrew support" (review 3's N9).</summary>
        private static string FellWords(CountryId c, GovernmentFormation.NightVote v)
        {
            var parts = new List<string>();
            if (v.Decliners.Count > 0) { parts.Add(ShortList(c, v.Decliners) + " declined to join"); }
            if (v.Withdrew.Count > 0) { parts.Add(ShortList(c, v.Withdrew) + " withdrew support"); }
            return string.Join(" · ", parts);
        }

        private static string ShortList(CountryId c, List<string> keys)
        {
            var names = new List<string>(keys.Count);
            foreach (string k in keys) { names.Add(Short(c, k)); }
            return string.Join(", ", names);
        }

        private static void GovernsYou(Transform parent, GovernsNight n, GovernmentFormation.View g, bool counting, ColumnPlan plan, List<DenseLine> dense)
        {
            if (counting || g == null) { return; }
            Gap(parent, 8f);
            Rule(parent, PoliSimTheme.RuleRow);
            Transform row = CanvasRows.HRow(parent, "You", Px(30f), Px(8f));
            Hold(PlText(row, "Label", "You", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
            if (g.PlayerParty == null)
            {
                Hold(PlText(row, "None", "No player's party on this ballot", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                dense.Add(new DenseLine("NO PLAYER'S PARTY ON THIS BALLOT", false));
                return;
            }
            CanvasRows.Mark(row, n.Country, g.PlayerParty, Px(16f));
            Hold(PlText(row, "Party", Short(n.Country, g.PlayerParty), Px(16f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
            CanvasRows.Spacer(row);
            string stamp, built;
            Color ink;
            var lines = new List<string>();
            if (!g.HasGovernment)
            {
                stamp = "CARETAKER SERVES ON"; ink = PoliSimTheme.Caution;
                built = "YOU - " + g.PlayerParty + " · THE OUTGOING GOVERNMENT SERVES ON";
                lines.Add("No government is expected; the outgoing government serves on as caretaker");
            }
            else if (g.PlayerInCabinet)
            {
                stamp = "EXPECTED IN OFFICE"; ink = PoliSimTheme.Good;
                built = "YOU - " + g.PlayerParty + " IN THE CABINET · EXPECTED IN OFFICE";
                lines.Add("In the cabinet: expected in office");
            }
            else
            {
                stamp = "EXPECTED OUT OF OFFICE"; ink = PoliSimTheme.Bad;
                built = "YOU - " + g.PlayerParty + (g.PlayerSupports ? " SUPPORTS IT FROM OUTSIDE" : " IN OPPOSITION") + " · EXPECTED OUT OF OFFICE";
                lines.Add(g.PlayerSupports ? "Supports it from outside: expected out of office" : "In opposition: expected out of office");
            }
            lines.Add("support is not office - the rule (C-R4, D-5 (a)) counts cabinet seats only");
            const string whole = "IT READS YOUR PARTY'S ANSWER AS THE MODEL WOULD GIVE IT - YOURS CAN DIFFER";
            string answerLine = "The projection reads " + UiFormat.Possessive(Short(n.Country, g.PlayerParty), false, true) + " answer as the model would give it";
            if (g.ReadsPlayersAnswer && !plan.K119) { lines.Add(answerLine); lines.Add(whole); }   // the plan's last fold: the K119 line to the slip
            CanvasRows.Stamp(row, stamp, Px(14f), ink);
            CanvasRows.Slip(row.gameObject, n.Overlay, "You · " + Short(n.Country, g.PlayerParty), SlipWrap(lines.ToArray()));
            dense.Add(new DenseLine(built, false));
            dense.Add(new DenseLine("support is not office - the rule (C-R4, D-5 (a)) counts cabinet seats only", false));

            if (g.ReadsPlayersAnswer)
            {
                dense.Add(new DenseLine(whole, false));
                if (!plan.K119) { return; }
                // K119: only where the projection answered the player's party by the AI's rule
                Transform answer = CanvasRows.HRow(parent, "YourAnswer", Px(20f), 0f);
                Hold(PlText(answer, "Line", "If you answer as the model would", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                CanvasRows.Slip(answer.gameObject, n.Overlay, "Your answer", SlipWrap(answerLine, "Yours can differ, and then so can this", whole));
            }
        }

        /// <summary>The bridge's geometry for the column's plan: its natural height in board px (13b: 1 pp = 20 units; the lane 24 to 40 px) and its movers.</summary>
        private static bool TryBridge(VoteAttribution.Ledger ledger, out AttributionBridge.Geometry g, out string fault)
        {
            g = null; fault = null;
            if (ledger == null || ledger.Lines.Count == 0) { return false; }
            try { g = AttributionBridge.Build(ledger); return true; }
            catch (InvalidOperationException e) { fault = e.Message; return false; }
        }

        private static int BridgeNaturalPx(AttributionBridge.Geometry g)
        {
            double range = Math.Max(0.0, g.HighPoints - g.LowPoints);
            float units = Mathf.Clamp((float)range * AttributionBridge.PixelsPerPoint + Px(8f), Px(24f), Px(40f));
            return Mathf.RoundToInt(units / BoardPx);
        }

        /// <summary>The largest fall and the largest rise (indices into the steps, -1 where none), chosen by size.</summary>
        private static void Movers(AttributionBridge.Geometry g, out int fall, out int rise)
        {
            fall = -1; rise = -1;
            for (int i = 0; i < g.Steps.Count; i++)
            {
                AttributionBridge.Step s = g.Steps[i];
                if (s.Zero) { continue; }
                if (s.Points < 0 && (fall < 0 || s.Points < g.Steps[fall].Points)) { fall = i; }
                if (s.Points > 0 && (rise < 0 || s.Points > g.Steps[rise].Points)) { rise = i; }
            }
        }

        private static void GovernsWhy(Transform parent, GovernsNight n, ColumnPlan plan, List<DenseLine> dense)
        {
            Gap(parent, 12f);
            VoteAttribution.Ledger ledger = n.Ledger;
            string party = Short(n.Country, n.LedgerParty ?? string.Empty);
            bool ok = TryBridge(ledger, out AttributionBridge.Geometry g, out string fault);
            if (!ok && fault != null)
            {
                Transform h1 = CanvasRows.HRow(parent, "WhyHead", Px(26f), Px(10f));
                Hold(PlText(h1, "Heading", "Why · the lines do not sum to the move", Px(18f), PoliSimTheme.Bad, PoliSimTheme.Body));
                CanvasRows.Slip(h1.gameObject, n.Overlay, "Why", SlipWrap(fault));
                dense.Add(new DenseLine($"WHY — {party.ToUpperInvariant()}: THE LINES DO NOT SUM TO THE MOVE", true));
                dense.Add(new DenseLine(fault, false));
                return;
            }
            if (!ok)
            {
                string head = ledger == null ? "Why · no campaign ledger" : "Why · no attribution";
                Transform h0 = CanvasRows.HRow(parent, "WhyHead", Px(26f), Px(10f));
                Hold(PlText(h0, "Heading", head, Px(18f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
                Gap(parent, 4f);
                var lane = new GameObject("BridgeEmpty");
                lane.transform.SetParent(parent, false);
                lane.AddComponent<RectTransform>();
                LayoutElement le = lane.AddComponent<LayoutElement>();
                le.minHeight = le.preferredHeight = Px(24f);
                Absolute(lane.transform, "Baseline", 0.0, 1.0, Px(12f) - 0.75f, 1.5f, PoliSimTheme.Hairline);
                string why = ledger == null ? "no live campaign was run to this election" : "the ledger carries no lines";
                CanvasRows.Slip(h0.gameObject, n.Overlay, head, SlipWrap(why));
                dense.Add(new DenseLine(ledger == null ? "WHY — NO CAMPAIGN LEDGER FOR THIS NIGHT" : "WHY — NO ATTRIBUTION FOR THIS PARTY", true));
                dense.Add(new DenseLine(why, false));
                return;
            }

            Transform head1 = CanvasRows.HRow(parent, "WhyHead", Px(26f), Px(10f));
            Hold(PlText(head1, "Heading", "Why " + party + " moved", Px(18f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            HoldRight(PlText(head1, "Levels", string.Format(CultureInfo.InvariantCulture, "{0:F1} → {1:F1}", g.BaselinePoints, g.ClosePoints), Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
            double move = Math.Round(g.MovePoints, 1);
            Color moveInk = move > 0 ? PoliSimTheme.Good : move < 0 ? PoliSimTheme.Bad : PoliSimTheme.TextPrimary;
            string moveText = (move > 0 ? "▲ " : move < 0 ? "▼ " : string.Empty) + Math.Abs(move).ToString("F1", CultureInfo.InvariantCulture) + " pp";
            Hold(PlText(head1, "Move", moveText, Px(16f), moveInk, PoliSimTheme.Document));
            string baseline = string.Format(CultureInfo.InvariantCulture, "WHY — {0} · BASELINE (AT THE CLOSE'S AWARENESS) {1:F1} → CLOSE {2:F1} · THE MOVE {3:+0.0;-0.0;0.0} pp", party.ToUpperInvariant(), g.BaselinePoints, g.ClosePoints, g.MovePoints);
            CanvasRows.Slip(head1.gameObject, n.Overlay, "Why " + party + " moved · " + moveText, SlipWrap(baseline, "Eleven sources, in the ledger's fixed order"));

            // the bridge: 1 pp = 20 units in a lane of 36 to 60; the scale shrinks to fit the lane the plan gives it (60 at most; 36 where the column's
            // shrink order took it to its floor)
            double range = Math.Max(0.0, g.HighPoints - g.LowPoints);
            float excursion = (float)range * AttributionBridge.PixelsPerPoint;
            float height = Mathf.Min(plan.BridgePx * BoardPx, Mathf.Round(Mathf.Clamp(excursion + Px(8f), Px(24f), Px(40f))));
            float perPoint = excursion + Px(8f) > height && range > 0.0 ? (height - Px(8f)) / (float)range : AttributionBridge.PixelsPerPoint;
            Gap(parent, 4f);
            var bridge = new GameObject("Bridge");
            bridge.transform.SetParent(parent, false);
            bridge.AddComponent<RectTransform>();
            LayoutElement size = bridge.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = height;
            float top = (height - (float)range * perPoint) * 0.5f;
            float Y(double points) => top + (float)(g.HighPoints - points) * perPoint;
            int slots = g.Steps.Count;
            AbsolutePx(bridge.transform, "BaselineTick", 0.0, 0f, 3f, Y(g.BaselinePoints) - Px(6f), Px(12f), PoliSimTheme.TextMuted);
            for (int i = 0; i < slots; i++)
            {
                AttributionBridge.Step s = g.Steps[i];
                Absolute(bridge.transform, "Level", i / (double)slots, (i + 1) / (double)slots, Y(s.From) - 0.75f, 1.5f, PoliSimTheme.Hairline);
                if (s.Zero) { continue; }
                float yTop = Y(Math.Max(s.From, s.To)), h = Mathf.Max(Px(2f), (float)Math.Abs(s.Points) * perPoint);
                double x0 = (i + 0.2) / slots, x1 = (i + 0.8) / slots;
                if (s.Points > 0) { Absolute(bridge.transform, "Up", x0, x1, yTop, h, PoliSimTheme.Good); }
                else if (h >= Px(5f))
                {
                    GameObject box = Absolute(bridge.transform, "Down", x0, x1, yTop, h, Color.clear);
                    DashedOutline(box.transform, PoliSimTheme.Bad, 3, h);
                }
                else { Absolute(bridge.transform, "Down", x0, x1, yTop, h, PoliSimTheme.Bad); }
            }
            AbsolutePx(bridge.transform, "CloseTick", 1.0, -4.5f, 4.5f, Y(g.ClosePoints) - Px(7f), Px(14f), PoliSimTheme.TextPrimary);
            CanvasRows.Slip(bridge, n.Overlay, "The bridge", SlipWrap("Solid up · dashed down · the level carried", "1 pp = " + (perPoint / BoardPx).ToString("F1", CultureInfo.InvariantCulture) + " px at 1280"
                + (perPoint < AttributionBridge.PixelsPerPoint ? " - the path is taller than its lane, so the scale shrinks to fit" : string.Empty), "The close tick heavier; a zero step draws nothing"));

            // the strip: the eleven acts, a zero struck and muted
            var strip = new GameObject("Strip");
            strip.transform.SetParent(parent, false);
            strip.AddComponent<RectTransform>();
            LayoutElement stripSize = strip.AddComponent<LayoutElement>();
            stripSize.minHeight = stripSize.preferredHeight = Px(18f);
            stripSize.flexibleHeight = 0f;   // the film of 396a6102: the strip's force-expanded cells made it report a flexible height of 1, and it took the column's spare 88 px
            var cells = strip.AddComponent<HorizontalLayoutGroup>();
            cells.childControlWidth = true; cells.childForceExpandWidth = true; cells.childControlHeight = true; cells.childForceExpandHeight = true;
            var rows = new List<string>();
            foreach (AttributionBridge.Step s in g.Steps)
            {
                string name = ActName(s.Source);
                Text t = CanvasChrome.MakeTextRealWeight(strip.transform, "Act " + s.Abbreviation, s.Abbreviation, PoliSimTheme.Document, Px(14f), s.Zero ? PoliSimTheme.HairlineStrong : PoliSimTheme.TextPrimary, TextAnchor.MiddleCenter);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f; le.minWidth = 0f; le.preferredWidth = 0f;
                if (s.Zero) { Strike(t.transform, PoliSimTheme.HairlineStrong); }
                string figure = s.Zero ? "0.0" : string.Format(CultureInfo.InvariantCulture, "{0:+0.00;-0.00} pp", s.Points);
                CanvasRows.Slip(t.gameObject, n.Overlay, s.Abbreviation + " · " + name, SlipWrap(s.Zero ? "0.0 · it did not move " + party : figure + " for " + party));
                rows.Add(s.Abbreviation + "  " + Regex.Replace(s.Source.ToString(), "([a-z])([A-Z])", "$1 $2").ToUpperInvariant() + " · " + figure);
            }

            // the largest fall and the largest rise, chosen by size, printed in the ledger's fixed order; the plan may keep one (the larger) or none
            Movers(g, out int fall, out int rise);
            if (plan.Movers == 1 && fall >= 0 && rise >= 0)
            {
                if (Math.Abs(g.Steps[fall].Points) >= Math.Abs(g.Steps[rise].Points)) { rise = -1; } else { fall = -1; }
            }
            if (plan.Movers > 0 && (fall >= 0 || rise >= 0))
            {
                Gap(parent, 4f);
                for (int i = 0; i < g.Steps.Count; i++)
                {
                    if (i != fall && i != rise) { continue; }
                    AttributionBridge.Step s = g.Steps[i];
                    Transform mover = CanvasRows.HRow(parent, "Mover " + s.Abbreviation, Px(20f), Px(8f));
                    Hold(PlText(mover, "Glyph", s.Points > 0 ? "▲" : "▼", Px(14f), s.Points > 0 ? PoliSimTheme.Good : PoliSimTheme.Bad, PoliSimTheme.Document));
                    Hold(PlText(mover, "Name", ActName(s.Source), Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                    HoldRight(PlText(mover, "Figure", Math.Abs(s.Points).ToString("F2", CultureInfo.InvariantCulture) + " pp", Px(14f), PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                    CanvasRows.Slip(mover.gameObject, n.Overlay, ActName(s.Source) + " · " + string.Format(CultureInfo.InvariantCulture, "{0:+0.00;-0.00} pp", s.Points),
                        SlipWrap(s.Points > 0 ? "The largest rise in the ledger" : "The largest fall in the ledger", "All eleven are on the strip and in †"));
                }
            }
            dense.Add(new DenseLine(string.Format(CultureInfo.InvariantCulture, "WHY — {0}", party.ToUpperInvariant()), true));
            dense.Add(new DenseLine(baseline, false));
            foreach (string r in rows) { dense.Add(new DenseLine(r, false)); }
        }

        private static void GovernsBudget(Transform parent, GovernsNight n, ColumnPlan plan, List<DenseLine> dense)
        {
            Gap(parent, 12f);
            Transform head = CanvasRows.HRow(parent, "BudgetHead", Px(26f), Px(10f));
            Hold(PlText(head, "Heading", "If the budget stands", Px(18f), PoliSimTheme.TextPrimary, PoliSimTheme.Body));
            CanvasRows.Spacer(head);
            dense.Add(new DenseLine(EffectArrowsRenderer.PlateTitleInherited, true));
            if (n.StandingBudget == null || n.StandingBudget.Count == 0)
            {
                Transform none = CanvasRows.HRow(parent, "NoBudget", Px(20f), 0f);
                Hold(PlText(none, "Line", "No enacted budget carries an estimate", Px(14f), PoliSimTheme.TextMuted, PoliSimTheme.Document));
                const string whole = "NO ENACTED BUDGET CARRIES AN ESTIMATE - NOTHING IS DRAWN IN ITS PLACE";
                CanvasRows.Slip(head.gameObject, n.Overlay, "If the budget stands", SlipWrap(whole));
                dense.Add(new DenseLine(whole, false));
                return;
            }
            CanvasRows.Stamp(head, "ESTIMATE", Px(14f), PoliSimTheme.TextPrimary);
            var arrows = new List<EffectArrow>(n.StandingBudget.Count);
            foreach (DivisionEffect e in n.StandingBudget) { arrows.Add(new EffectArrow(e.Name, e.Value, e.Neutral ? (bool?)null : e.HigherIsBetter, e.Figure)); }
            var headLines = new List<string> { EffectArrowsRenderer.PlateTitleInherited };
            if (!string.IsNullOrEmpty(n.StandingBudgetCitation)) { headLines.Add("AS ENACTED - " + n.StandingBudgetCitation); }
            headLines.Add(EffectArrowsRenderer.ScopeLine);
            var figures = new List<string>();
            foreach (EffectArrow a in arrows) { figures.Add(a.Name.ToUpperInvariant() + " " + a.Figure); }
            int shown = Math.Min(arrows.Count, plan.EffectRows * 3);
            if (shown < arrows.Count) { headLines.Add("Not drawn here, for want of room: " + string.Join(" · ", figures.GetRange(shown, figures.Count - shown))); }
            CanvasRows.Slip(head.gameObject, n.Overlay, "Estimated impact", SlipWrap(headLines.ToArray()));

            Gap(parent, 4f);
            Transform row = null;
            for (int i = 0; i < shown; i++)
            {
                if (i % 3 == 0)
                {
                    row = CanvasRows.HRow(parent, "Outcomes", Px(22f), 0f);
                    row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
                }
                EffectArrow a = arrows[i];
                Transform cell = CanvasRows.HRow(row, "Outcome " + a.Name, Px(22f), Px(5f));
                LayoutElement cellSize = cell.GetComponent<LayoutElement>();
                cellSize.flexibleWidth = 1f; cellSize.minWidth = 0f; cellSize.preferredWidth = 0f;
                Texture2D icon = IconLibrary.V35(EffectArrowsRenderer.V35Icon(a.Name));
                if (icon != null)
                {
                    var ig = new GameObject("Icon");
                    ig.transform.SetParent(cell, false);
                    ig.AddComponent<RectTransform>();
                    RawImage image = ig.AddComponent<RawImage>();
                    image.texture = icon;
                    image.color = PoliSimTheme.TextPrimary;
                    image.raycastTarget = false;
                    LayoutElement le = ig.AddComponent<LayoutElement>();
                    le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = Px(18f);
                    le.flexibleWidth = 0f;
                }
                // the review's L3: a zero estimate draws no arrow - nothing moves
                bool zero = Mathf.Abs(a.Value) < 0.005f;
                if (!zero) { Hold(PlText(cell, "Glyph", a.Value > 0f ? "▲" : "▼", Px(14f), EffectArrowsRenderer.V35Ink(a.Value, a.HigherIsBetter), PoliSimTheme.Body)); }
                string figure = UiFormat.Number(Mathf.Abs(a.Value), 2) + (a.Name == "GDP growth" ? "%" : " pts");
                Hold(PlText(cell, "Figure", figure, Px(14f), zero ? PoliSimTheme.TextMuted : PoliSimTheme.TextPrimary, PoliSimTheme.Document));
                CanvasRows.Slip(cell.gameObject, n.Overlay, a.Name, SlipWrap(a.Figure + " next year, if the budget stands", "With against without this draft"));
            }
            dense.Add(new DenseLine(string.Join(" · ", figures), false));
            for (int i = 1; i < headLines.Count; i++) { dense.Add(new DenseLine(headLines[i], false)); }
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE COLUMN'S PLAN (the review's H1)
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// [AUTHORED] layout: the column's rect at 1280 - 330 x 516 px above the CONTINUE row (Design measured it on now_sweden_1280, 1280 x 699, the
        /// tightest frame filmed: at 1280 x 720 and 2560 x 1440 the canvas is 1080 units tall, at 1280 x 699 it is 1064). The column never overflows it.
        /// <para><b>THE ARITHMETIC</b> (board px at 1280; x 2 at 2560), the blocks as built here: head 28; round 6+24; outcome 8+1+30 (+20 a reason row where none is
        /// expected, +26 the support row); the vote 8+26+4+20, Italy's two lanes 8+26+2x(4+20) (+6+18 a key line); You 8+1+30 (+20 the K119 line); Why 12+26
        /// +4+bridge(24..40)+18 +4+20 a mover (two at most) - without a ledger 12+26+4+24; the budget 12+26+4+22 a row of three (12+26+20 with none).</para>
        /// <para>The tallest cases (every optional row, the tallest bridge, six outcomes): Sweden with support 28+30+65+82+59+144+86 = 494, none expected
        /// 28+30+79+82+59+144+86 = 508; Germany (no budget) 408 and 422; Italy with support 28+30+65+106+59+144+86 = 518, none expected 532. Over 516 the
        /// SHRINK ORDER runs until it fits: (1) the bridge lane to its 36-unit floor (24 px), (2) the fall and rise to one row, the larger, (3) to none (the strip
        /// and the slips carry all eleven), (4) the key line folds into the vote's slip, (5) the outcomes to two rows (the rest in the slip and †), (6) the K119
        /// line into the You slip. Italy with support fits at (1), 502; none expected at (1), 516; seven outcomes none expected at (3), 494.
        /// `FormationProjectionDiagnostic` sums the same blocks through <see cref="NightColumnPlan.PlanColumn"/> for each country's worst case and fails above 516.</para>
        /// </summary>
        public const int ColumnPx = 516;

        /// <summary>What decides the column's height on one night - each optional row, and the sizes the shrink order may take back.</summary>
        public sealed class ColumnCase
        {
            public CountryId Country;
            public bool Counting, Round = true, NoFormation, NoneExpected, SupportRow, You = true, K119, Key, Ledger = true, LedgerFault, Budget;
            public int ReasonRows, Lanes = 1, BridgePx = 40, Movers = 2, Effects = 6;

            public override string ToString() => string.Format(CultureInfo.InvariantCulture,
                "{0}{1}{2}{3}, lanes {4}{5}{6}, bridge {7} px, movers {8}, {9}", Country, Counting ? " counting" : NoneExpected ? " none expected" : NoFormation ? " no formation" : " expected",
                SupportRow ? " with support" : string.Empty, ReasonRows > 0 ? " (" + ReasonRows.ToString(CultureInfo.InvariantCulture) + " reason rows)" : string.Empty, Lanes, Key ? ", a key line" : string.Empty,
                K119 ? ", the K119 line" : string.Empty, BridgePx, Movers, Budget ? Effects.ToString(CultureInfo.InvariantCulture) + " outcomes" : "no budget block");
        }

        /// <summary>The column as it will be drawn: the sizes after the shrink order, the natural and the final heights, and the steps taken.</summary>
        public sealed class ColumnPlan
        {
            public int BridgePx, Movers, EffectRows, Natural, Final;
            public bool Key, K119;
            public readonly List<string> Shrunk = new List<string>();
        }

        /// <summary>The case the night draws, measured from what it carries (the key line, the bridge, the movers, the outcomes).</summary>
        private static ColumnCase CaseOf(GovernsNight n, GovernmentFormation.View g, bool counting)
        {
            var k = new ColumnCase { Country = n.Country, Counting = counting, Budget = n.BudgetBlock, Effects = n.StandingBudget?.Count ?? 0 };
            if (!counting)
            {
                k.NoFormation = g == null;
                k.Round = g != null && g.Round.Count > 0;
                k.NoneExpected = g != null && !g.HasGovernment;
                k.ReasonRows = k.NoneExpected ? NoneLines(n.Country, g).Length : 0;
                k.SupportRow = g != null && g.HasGovernment && g.Support.Count > 0;
                List<GovernmentFormation.NightVote> votes = VotesOf(n, g, false);
                k.Lanes = votes.Count;
                k.Key = votes.Count > 0 && NeedsKey(votes, g);
                k.You = g != null;
                k.K119 = g != null && g.PlayerParty != null && g.ReadsPlayersAnswer;
            }
            bool ok = TryBridge(n.Ledger, out AttributionBridge.Geometry geometry, out string fault);
            k.Ledger = ok;
            k.LedgerFault = fault != null;
            if (ok)
            {
                k.BridgePx = BridgeNaturalPx(geometry);
                Movers(geometry, out int fall, out int rise);
                k.Movers = (fall >= 0 ? 1 : 0) + (rise >= 0 ? 1 : 0);
            }
            return k;
        }


        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE † VIEW
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>"For Code" 6: every line in full, in the column's own rect, scrolled inside it.</summary>
        private static GameObject DenseView(Transform column, List<DenseLine> lines)
        {
            var go = new GameObject("Dense");
            go.transform.SetParent(column, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            Image hit = go.AddComponent<Image>();
            hit.color = Color.clear;   // the wheel finds the view anywhere in its rect

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(go.transform, false);
            var vr = viewport.AddComponent<RectTransform>();
            vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one; vr.offsetMin = Vector2.zero; vr.offsetMax = new Vector2(-Px(6f), 0f);
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cr = content.AddComponent<RectTransform>();
            cr.anchorMin = new Vector2(0f, 1f); cr.anchorMax = new Vector2(1f, 1f); cr.pivot = new Vector2(0.5f, 1f);
            cr.offsetMin = Vector2.zero; cr.offsetMax = Vector2.zero;
            var v = content.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false; v.spacing = Px(3f);
            ContentSizeFitter fit = content.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            bool first = true;
            foreach (DenseLine l in lines)
            {
                if (l.Head)
                {
                    if (!first) { Gap(content.transform, 8f); }
                    Text t = CanvasChrome.MakeTextRealWeight(content.transform, "Head", l.Text, PoliSimTheme.Body, Px(16f), PoliSimTheme.TextPrimary, TextAnchor.UpperLeft);
                    t.horizontalOverflow = HorizontalWrapMode.Wrap;
                    t.raycastTarget = false;
                    t.gameObject.AddComponent<LayoutElement>().minHeight = Px(22f);
                    Rule(content.transform, PoliSimTheme.RuleRow);
                }
                else
                {
                    Text t = CanvasChrome.MakeTextRealWeight(content.transform, "Line", l.Text, PoliSimTheme.Document, Px(14f), PoliSimTheme.TextPrimary, TextAnchor.UpperLeft);
                    t.horizontalOverflow = HorizontalWrapMode.Wrap;
                    t.raycastTarget = false;
                    t.gameObject.AddComponent<LayoutElement>().minHeight = Px(19f);
                }
                first = false;
            }

            ScrollRect scroll = go.AddComponent<ScrollRect>();
            scroll.viewport = vr;
            scroll.content = cr;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = Px(19f);
            return go;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // WORDS
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>The vote office changes on, in a sentence: the Riksdag votes, the Bundestag elects, Italy's both chambers vote confidence.</summary>
        private static string VoteSentence(GovernsNight n) => n.Country == CountryId.Germany ? "the Bundestag elects"
            : n.Government?.ProjectionVote != null ? n.Government.ProjectionVote.ToLowerInvariant() : "the Riksdag votes";

        /// <summary>The foot as first built (q20, §918), kept whole - the stamp's slip and the † view.</summary>
        private static string Foot(GovernsNight n)
        {
            string vote = n.Country == CountryId.Germany ? "THE BUNDESTAG ELECTS" : n.Government?.ProjectionVote ?? "THE RIKSDAG VOTES";
            return "A PROJECTION - OFFICE CHANGES ONLY WHEN " + vote + (n.Government != null && n.Government.ReadsPlayersAnswer ? " · IT READS YOUR PARTY'S ANSWER AS THE MODEL WOULD GIVE IT - YOURS CAN DIFFER" : string.Empty);
        }

        private static string ProcedureWords(CountryId c) => c == CountryId.Germany ? "the chancellor's election" : c == CountryId.Italy ? "the President's mandates" : "the Speaker's round";

        private static string RoundLabel(CountryId c) => c == CountryId.Germany ? "Bundestag's ballot" : c == CountryId.Italy ? "President's mandate" : "Speaker's proposal";

        private static string StepWord(CountryId c) => c == CountryId.Germany ? "Ballot" : c == CountryId.Italy ? "Mandate" : "Proposal";

        private static string RoundRule(CountryId c) => c == CountryId.Germany
            ? "Art. 63 GG: the first ballot (a majority of the members), the fourteen days, the last ballot (the most votes)"
            : c == CountryId.Italy ? "Three mandates fail and the chambers are dissolved (art. 88)"
            : "Four refusals end the round and an extra election follows within three months (RF 6:5)";

        private static int DefaultCells(CountryId c) => c == CountryId.Germany || c == CountryId.Italy ? 3 : 4;

        /// <summary>The line the night's chamber counts - against under the negative rule, for elsewhere.</summary>
        private static bool ChamberCountsAgainst(CountryId c) => ChamberRules.UsesNegativeParliamentarism(c);

        /// <summary>Two short lines where none is expected, by the procedure's case; the whole reason is the slip's and the † view's.</summary>
        private static string[] NoneLines(CountryId c, GovernmentFormation.View g)
        {
            if (c == CountryId.Germany)
            {
                // the review's L1: elected at the first ballot with no government drawn on its party is said as such - the ✓ and the current cell stand
                bool elected = g.Votes.Count > 0 && g.Votes[0].ElectedWithoutGovernment;
                return elected ? new[] { "Elected at the first ballot", "No government drawn on its party" } : new[] { "Not elected at the first ballot", "The fourteen days follow" };
            }
            if (c == CountryId.Italy) { return new[] { "Three mandates fail", "The chambers are dissolved" }; }
            bool refused = false;
            foreach (GovernmentFormation.RoundCell cell in g.Round) { if (cell.State == GovernmentFormation.RoundCellState.LastRefused) { refused = true; } }
            return refused ? new[] { "Four proposals refused", "An extra election within three months" } : new[] { "No party asked can table a proposal" };
        }

        // M4: the outcome's words are the model's one lookup (EnumNames.Of) - the night's own words moved there, so no outcome prints its identifier
        private static string OutcomeShort(CoalitionOutcomeKind kind) => EnumNames.Of(kind);

        private static string OutcomeName(CoalitionOutcomeKind kind)
        {
            switch (kind)
            {
                case CoalitionOutcomeKind.MajorityCoalition: return "A MAJORITY GOVERNMENT";
                case CoalitionOutcomeKind.ConfidenceAndSupply: return "A MINORITY GOVERNMENT WITH SUPPORT";
                case CoalitionOutcomeKind.MinorityGovernment: return "A MINORITY GOVERNMENT";
                default: return EnumNames.Of(kind).ToUpperInvariant();   // M4: never the identifier
            }
        }

        private static string ShortParties(CountryId c, List<(string Abbrev, int Seats)> list)
        {
            var parts = new List<string>(list.Count);
            foreach ((string abbrev, int seats) in list) { parts.Add(Short(c, abbrev) + " " + seats.ToString(CultureInfo.InvariantCulture)); }
            return string.Join(" · ", parts);
        }

        private static string Short(CountryId c, string key) => string.IsNullOrEmpty(key) ? string.Empty : PartySystems.ShortName(c, key);

        private static string SideList(CountryId c, GovernmentFormation.NightVote v, bool forSide, bool against)
        {
            var parts = new List<string>();
            foreach (GovernmentFormation.VoteSide s in v.Sides)
            {
                bool isFor = s.Side == CoalitionFormation.InvestitureSide.InCabinet || s.Side == CoalitionFormation.InvestitureSide.Supports;
                bool isAgainst = s.Side == CoalitionFormation.InvestitureSide.Against;
                if (forSide ? isFor : against ? isAgainst : !isFor && !isAgainst) { parts.Add(Short(c, s.Abbrev) + " " + s.Seats.ToString(CultureInfo.InvariantCulture)); }
            }
            return parts.Count == 0 ? "none" : string.Join(" · ", parts);
        }

        private static string SideWord(CoalitionFormation.InvestitureSide side)
        {
            switch (side)
            {
                case CoalitionFormation.InvestitureSide.InCabinet: return "for, in the cabinet";
                case CoalitionFormation.InvestitureSide.Supports: return "for, from outside";
                case CoalitionFormation.InvestitureSide.Against: return "against";
                default: return "abstains";
            }
        }

        /// <summary>The act's name in sentence case - "Town hall", "Opponent campaigns".</summary>
        private static string ActName(VoteAttributionSource source)
        {
            string words = Regex.Replace(source.ToString(), "([a-z])([A-Z])", "$1 $2");
            return words.Length > 1 ? words.Substring(0, 1) + words.Substring(1).ToLowerInvariant() : words;
        }

        /// <summary>Slip lines: each sentence whole, broken at word boundaries into lines of at most 56 characters, its case kept (the long wording is word for word).</summary>
        private static List<string> SlipWrap(params string[] sentences)
        {
            var lines = new List<string>();
            foreach (string sentence in sentences)
            {
                if (string.IsNullOrEmpty(sentence)) { continue; }
                var line = new System.Text.StringBuilder();
                foreach (string word in sentence.Split(' '))
                {
                    if (line.Length > 0 && line.Length + 1 + word.Length > 56) { lines.Add(line.ToString()); line.Clear(); }
                    if (line.Length > 0) { line.Append(' '); }
                    line.Append(word);
                }
                if (line.Length > 0) { lines.Add(line.ToString()); }
            }
            return lines;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // PIECES
        // ---------------------------------------------------------------------------------------------------------------------------------------

        private static void Hold(Text t) => HoldWidth(t);

        /// <summary>A text held at its width that also takes the row's spare width, set to the right - the row's spacer and one gap fewer.</summary>
        private static void HoldRight(Text t)
        {
            HoldWidth(t);
            t.GetComponent<LayoutElement>().flexibleWidth = 1f;
            t.alignment = TextAnchor.MiddleRight;
        }

        /// <summary>A gap of <paramref name="boardPixels"/> in a column.</summary>
        private static void Gap(Transform parent, float boardPixels)
        {
            var go = new GameObject("Gap");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = boardPixels * BoardPx;
        }

        /// <summary>A 1 px rule across a column.</summary>
        private static void Rule(Transform parent, Color ink)
        {
            var go = new GameObject("Rule");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            Image img = go.AddComponent<Image>();
            img.color = ink;
            img.raycastTarget = false;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = BoardPx;
        }

        /// <summary>✓ or ✗ - the registry's verdict glyph in its ink, its word where the file is not held.</summary>
        private static void Verdict(Transform row, bool carried, float side)
        {
            Symbol s = carried ? Symbol.Good : Symbol.Bad;
            Color ink = carried ? PoliSimTheme.Good : PoliSimTheme.Bad;
            Texture2D glyph = SymbolRegistry.TextureOf(s);
            if (glyph == null) { Hold(PlText(row, "Verdict", SymbolRegistry.Word(s), Px(14f), ink, PoliSimTheme.Document)); return; }
            var go = new GameObject("Verdict");
            go.transform.SetParent(row, false);
            go.AddComponent<RectTransform>();
            RawImage image = go.AddComponent<RawImage>();
            image.texture = glyph;
            image.color = ink;
            image.raycastTarget = false;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = side;
            le.flexibleWidth = 0f;
        }

        /// <summary>One block of the vote bar by fraction of its width; a 1 px paper gap after it unless it is the last; its word inside where it fits.</summary>
        private static void BarSegment(Transform bar, string name, double from, double to, Color ink, bool hatched, Color wordInk, string word, bool last)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bar, false);
            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            if (hatched)
            {
                img.sprite = HatchSprite(ink, PoliSimTheme.ModalBackground);
                img.type = Image.Type.Tiled;
                img.color = Color.white;
            }
            else { img.color = ink; }
            RectTransform r = img.rectTransform;
            r.anchorMin = new Vector2((float)from, 0f);
            r.anchorMax = new Vector2((float)to, 1f);
            r.pivot = new Vector2(0f, 0.5f);
            r.offsetMin = Vector2.zero;
            r.offsetMax = new Vector2(last ? 0f : -BoardPx, 0f);
            if (string.IsNullOrEmpty(word)) { return; }
            Text t = CanvasChrome.MakeTextRealWeight(go.transform, "Word", word, PoliSimTheme.Document, Px(14f), wordInk, TextAnchor.MiddleLeft);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            RectTransform tr = t.rectTransform;
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(Px(6f), 0f); tr.offsetMax = Vector2.zero;
            t.gameObject.AddComponent<WordFit>().Word = word;   // measured against the board's width; the block as laid decides
        }

        /// <summary>A key line's 12 px swatch, hatched where the block is.</summary>
        private static void Swatch(Transform row, Color ink, bool hatched)
        {
            var go = new GameObject("Swatch");
            go.transform.SetParent(row, false);
            go.AddComponent<RectTransform>();
            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            if (hatched) { img.sprite = HatchSprite(ink, PoliSimTheme.ModalBackground); img.type = Image.Type.Tiled; img.color = Color.white; }
            else { img.color = ink; }
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = Px(12f);
            le.flexibleWidth = 0f;
        }

        private static readonly Dictionary<(Color32 Ink, Color32 Paper), Sprite> Hatches = new Dictionary<(Color32, Color32), Sprite>();

        /// <summary>Support hatched: the ink and paper in 135° stripes, 2 px of each at 1280 (3 canvas units), a tile that repeats seamlessly - one sprite per pair of
        /// colours (the review's L7: a theme's later inks get their own, never the first one's).</summary>
        private static Sprite HatchSprite(Color inkColor, Color paperColor)
        {
            Color32 ink = inkColor, paper = paperColor;
            if (Hatches.TryGetValue((ink, paper), out Sprite cached) && cached != null) { return cached; }
            const int n = 6;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++) { for (int x = 0; x < n; x++) { pixels[y * n + x] = (x + y) % n < n / 2 ? ink : paper; } }
            texture.SetPixels32(pixels);
            texture.Apply(false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Hatches[(ink, paper)] = sprite;
            return sprite;
        }

        /// <summary>A dashed rule round a rect: <paramref name="across"/> dashes along each horizontal edge (by fraction), dashes of 3 units every 6 down each vertical one.</summary>
        private static void DashedOutline(Transform rect, Color ink, int across, float height)
        {
            for (int k = 0; k < across; k++)
            {
                double x0 = k / (double)across, x1 = (k + 0.55) / across;
                Absolute(rect, "DashTop", x0, x1, 0f, 1.5f, ink);
                Absolute(rect, "DashBottom", x0, x1, height - 1.5f, 1.5f, ink);
            }
            for (float y = 0f; y < height; y += 6f)
            {
                float d = Mathf.Min(3f, height - y);
                AbsolutePx(rect, "DashLeft", 0.0, 0f, 1.5f, y, d, ink);
                AbsolutePx(rect, "DashRight", 1.0, -1.5f, 1.5f, y, d, ink);
            }
        }

        /// <summary>A rect from fraction <paramref name="x0"/> to <paramref name="x1"/> of its parent's width, <paramref name="top"/> units down from its top, <paramref name="height"/> tall.</summary>
        private static GameObject Absolute(Transform parent, string name, double x0, double x1, float top, float height, Color ink)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            if (ink.a > 0f)
            {
                Image img = go.AddComponent<Image>();
                img.color = ink;
                img.raycastTarget = false;
            }
            var r = (RectTransform)go.transform;
            r.anchorMin = new Vector2((float)x0, 1f);
            r.anchorMax = new Vector2((float)x1, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = new Vector2(0f, height);
            r.anchoredPosition = new Vector2(0f, -top);
            return go;
        }

        /// <summary>A rect of a fixed width at fraction <paramref name="at"/> of its parent's width, offset by <paramref name="dx"/> units.</summary>
        private static void AbsolutePx(Transform parent, string name, double at, float dx, float width, float top, float height, Color ink)
        {
            GameObject go = Absolute(parent, name, at, at, top, height, ink);
            var r = (RectTransform)go.transform;
            r.sizeDelta = new Vector2(width, height);
            r.anchoredPosition = new Vector2(dx, -top);
        }

        /// <summary>A zero act struck through at its text's middle.</summary>
        private static void Strike(Transform text, Color ink)
        {
            var line = new GameObject("Strike");
            line.transform.SetParent(text, false);
            Image bar = line.AddComponent<Image>();
            bar.color = ink;
            bar.raycastTarget = false;
            RectTransform rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0.2f, 0.5f); rt.anchorMax = new Vector2(0.8f, 0.5f); rt.sizeDelta = new Vector2(0f, 1.5f);
        }
    }
}
