using System.Globalization;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// W-E7 — **the results and attribution screen**, the sixth of the Track E class and the last
    /// IMGUI one. Same 1156×680 board, same 440 / 250 / 440 columns and the same primitives as
    /// Campaign HQ, the action screen, the polling screen, the map and the debate.
    ///
    /// ⚠ **HARNESS ONLY — R-N2 holds until W-G1**, by the same mechanism as its five siblings.
    ///
    /// **§30's breakdown, and the one part of it this screen refuses to draw.** The count, the
    /// seats, the turnout, the regional table and the gains and losses are all here and all trace to
    /// the model or to a sourced file. §30 also asks for young / older / urban / rural / income
    /// voters — and the electorate is ONE GROUP until W-F4. Five plausible rows split out of one
    /// group would be invented demographics, which §0.4 forbids outright, so the block is drawn as a
    /// stated absence with the item that fills it named. The Desk's Year-0 convention (1m-r2) and
    /// W-E2's §36 gate are the same idea: absence is a fact, and a zero is a different claim.
    ///
    /// **Two figures are the PUBLISHED ones and say so.** Turnout and the electorate come from
    /// Valmyndigheten, not from this model's own arithmetic: the eight parties' votes over a
    /// derived electorate would read 85.88 %, and Sweden's turnout was 84.21 %. For the same
    /// reason the vote total is labelled as what it is - the votes THESE parties took - because
    /// the official valid total (6 477 970) also carries minor parties this model does not.
    /// A results screen is precisely where a derived figure gets mistaken for a published one.
    ///
    /// **The "why" column is W-D4's ledger, unaltered.** Every line is a mechanism the model
    /// applies, the labels are enum names rather than prose, and the lines sum to the movement they
    /// explain as an identity (residual ~1e-16). The screen adds no interpretation of its own — if
    /// it did, the sum would stop being a proof and become a story.
    /// </summary>
    public partial class GameController
    {
        private ResultsScreenSnapshot? _campaignResultsScreen;
        private Rect _campaignResultsInnerRect;

        internal void SetCampaignResultsScreen(ResultsScreenSnapshot? snapshot)
        {
            _campaignResultsScreen = snapshot;
        }

        private void DrawCampaignResultsStage(float availableHeight, float availableWidth, ResultsScreenSnapshot snapshot)
        {
            float innerWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle);

            GUILayout.BeginVertical(_boxStyle, GUILayout.Width(availableWidth),
                GUILayout.ExpandHeight(true));   // P2-1.1: the sheet fills the frame-height column
            Rect inner = GUILayoutUtility.GetRect(innerWidth, availableHeight,
                GUILayout.Width(innerWidth), GUILayout.Height(availableHeight));
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint) { _campaignResultsInnerRect = inner; }
            else if (_campaignResultsInnerRect.width > 1f) { inner = _campaignResultsInnerRect; }

            float ux = inner.width / DeskBoardInnerWidth;
            float uy = inner.height / DeskBoardInnerHeight;
            Rect Board(float x, float y, float w, float h) =>
                new Rect(inner.x + x * ux, inner.y + y * uy, w * ux, h * uy);

            DrawResultsMasthead(Board(0f, 0f, 1156f, 28f), snapshot);

            DrawResultsCount(Board(0f, 36f, 440f, 576f), snapshot);
            DrawResultsRegions(Board(453f, 36f, 250f, 576f), snapshot);
            DrawResultsWhy(Board(716f, 36f, 440f, 576f), snapshot);

            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(Board(0f, 620f, 1156f, 1f), PoliSimTheme.HairlineStrong);
            }

            DrawResultsFooter(Board(0f, 627f, 1156f, 53f), snapshot);
        }

        private void DrawResultsMasthead(Rect r, ResultsScreenSnapshot s)
        {
            GUIStyle title = DeskCaption(10.5f, PoliSimTheme.TextPrimary, bold: true);
            GUIStyle right = DeskCaption(9.5f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, r.width * 0.6f, r.height),
                "ELECTION RESULTS · " + s.CountryName.ToUpperInvariant(), title);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x + r.width * 0.6f, r.y, r.width * 0.4f, r.height),
                s.ElectionDate.ToString("d MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()
                + "   ·   AGAINST " + s.PreviousLabel.ToUpperInvariant(), right);
        }

        // ------------------------------------------------------------------------------------------
        // Left: the count. Votes, share, seats, and the movement against the named prior election.
        // ------------------------------------------------------------------------------------------
        private void DrawResultsCount(Rect r, ResultsScreenSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "THE COUNT — VOTES, SEATS, AND THE MOVEMENT", ux, uy);

            GUIStyle head = DeskCaption(8.5f, PoliSimTheme.TextMuted);
            GUIStyle name = DeskBody(12f, PoliSimTheme.TextPrimary);
            GUIStyle figure = DeskCaption(11f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            float rowHeight = Mathf.Max(Mathf.Round(20f * uy), name.CalcSize(new GUIContent("Ag")).y);

            float wName = Mathf.Round(58f * ux);
            float wVotes = Mathf.Round(112f * ux);
            float wShare = Mathf.Round(88f * ux);
            float wSeats = Mathf.Round(78f * ux);
            float wSwing = r.width - wName - wVotes - wShare - wSeats;

            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, wName, rowHeight), "PARTY", head);
            Cell(r.x + wName, y, wVotes, rowHeight, "VOTES", head);
            Cell(r.x + wName + wVotes, y, wShare, rowHeight, "SHARE", head);
            Cell(r.x + wName + wVotes + wShare, y, wSeats, rowHeight, "SEATS", head);
            Cell(r.x + wName + wVotes + wShare + wSeats, y, wSwing, rowHeight, "CHANGE", head);
            y += rowHeight;

            var order = new System.Collections.Generic.List<int>();
            for (int p = 0; p < s.PartyNames.Length; p++) { order.Add(p); }
            order.Sort((a, b) => s.Votes[b].CompareTo(s.Votes[a]));

            foreach (int p in order)
            {
                bool player = p == s.PlayerPartyIndex;
                GUIStyle rowName = player ? DeskBody(12f, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft) : name;
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, wName, rowHeight), s.PartyNames[p], rowName);
                Cell(r.x + wName, y, wVotes, rowHeight, s.Votes[p].ToString("N0", CultureInfo.InvariantCulture), figure);
                Cell(r.x + wName + wVotes, y, wShare, rowHeight, s.Share(p).ToString("P2", CultureInfo.InvariantCulture), figure);
                Cell(r.x + wName + wVotes + wShare, y, wSeats, rowHeight, s.Seats[p].ToString(CultureInfo.InvariantCulture), figure);
                Cell(r.x + wName + wVotes + wShare + wSeats, y, wSwing, rowHeight,
                    string.Format(CultureInfo.InvariantCulture, "{0:+0.00;-0.00} pp  {1:+0;-0;0}", s.SwingPp(p), s.SeatChange(p)), figure);
                y += rowHeight;
            }

            y += Mathf.Round(6f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }

            y += Mathf.Round(6f * uy);
            DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "COUNTED FOR THESE PARTIES",
                string.Format(CultureInfo.InvariantCulture, "{0:N0} votes    {1} seats", s.ValidVotes, s.TotalSeats),
                DeskBody(11f, PoliSimTheme.TextSecondary), figure);
        }

        private static void Cell(float x, float y, float w, float h, string text, GUIStyle style)
        {
            PoliSimWidgets.MeasuredLabel(new Rect(x, y, w, h), text, style);
        }

        // ------------------------------------------------------------------------------------------
        // Middle: section 30's regional results - every constituency this count actually holds.
        // ------------------------------------------------------------------------------------------
        private void DrawResultsRegions(Rect r, ResultsScreenSnapshot s)
        {
            float ux = r.width / 250f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "THE CONSTITUENCIES", ux, uy);

            GUIStyle name = DeskBody(10f, PoliSimTheme.TextSecondary);
            GUIStyle figure = DeskCaption(10f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            float rowHeight = Mathf.Max(Mathf.Round(16f * uy), name.CalcSize(new GUIContent("Ag")).y);

            if (s.RegionNames == null || s.RegionNames.Length == 0)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight), "NO REGIONAL COUNT", name);
                return;
            }

            // Largest first: a reader looks for the big ones, and the order is the count's own.
            var order = new System.Collections.Generic.List<int>();
            for (int i = 0; i < s.RegionNames.Length; i++) { order.Add(i); }
            order.Sort((a, b) => s.RegionValid[b].CompareTo(s.RegionValid[a]));

            int room = Mathf.Max(1, Mathf.FloorToInt((r.yMax - y) / rowHeight) - 2);
            int shown = 0;
            foreach (int i in order)
            {
                if (shown >= room) { break; }
                int leader = s.RegionLeader(i);
                DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), s.RegionNames[i],
                    leader < 0 ? "—" : string.Format(CultureInfo.InvariantCulture, "{0}  {1:N0}", s.PartyNames[leader], s.RegionValid[i]),
                    name, figure);
                y += rowHeight;
                shown++;
            }

            if (shown < s.RegionNames.Length)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight),
                    string.Format(CultureInfo.InvariantCulture, "AND {0} MORE", s.RegionNames.Length - shown), name);
            }
        }

        // ------------------------------------------------------------------------------------------
        // Right: WHY - W-D4's ledger, and the part of section 30 this screen will not invent.
        // ------------------------------------------------------------------------------------------
        private void DrawResultsWhy(Rect r, ResultsScreenSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / 576f;
            string who = s.PlayerPartyIndex >= 0 ? s.PartyNames[s.PlayerPartyIndex].ToUpperInvariant() : "THE RESULT";
            float y = DrawCampaignLedgerHead(r, "WHY — " + who + ", LINE BY LINE", ux, uy);

            GUIStyle name = DeskBody(11f, PoliSimTheme.TextSecondary);
            GUIStyle figure = DeskCaption(11f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            float rowHeight = Mathf.Max(Mathf.Round(18f * uy), name.CalcSize(new GUIContent("Ag")).y);

            VoteAttribution.Ledger ledger = s.Attribution;
            if (ledger == null || ledger.Lines.Count == 0)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight), "NO ATTRIBUTION FOR THIS PARTY", name);
                y += rowHeight;
            }
            else
            {
                var ordered = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<VoteAttributionSource, double>>(ledger.Lines);
                ordered.Sort((a, b) => System.Math.Abs(b.Value).CompareTo(System.Math.Abs(a.Value)));
                foreach (var line in ordered)
                {
                    if (System.Math.Abs(line.Value) < 5e-7) { continue; }   // below a hundredth of a pp
                    DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), SpacedIdentifier(line.Key.ToString()),
                        string.Format(CultureInfo.InvariantCulture, "{0:+0.000;-0.000} pp", line.Value * 100.0), name, figure);
                    y += rowHeight;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimTheme.Rule(new Rect(r.x, y + Mathf.Round(3f * uy), r.width, 1f), PoliSimTheme.Hairline);
                }

                y += Mathf.Round(8f * uy);
                DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "TOTAL, AND IT SUMS",
                    string.Format(CultureInfo.InvariantCulture, "{0:+0.000;-0.000} pp", ledger.LineSum * 100.0),
                    DeskBody(11f, PoliSimTheme.TextPrimary), DeskCaption(11f, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleRight));
                y += rowHeight + Mathf.Round(10f * uy);
            }

            // Section 30 asks for a demographic breakdown. The electorate is ONE GROUP until W-F4,
            // and rule 0.4 forbids inventing demographics - so this is absence, stated, not five rows.
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }

            y += Mathf.Round(8f * uy);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, rowHeight),
                "BY VOTER GROUP", DeskCaption(8.5f, PoliSimTheme.TextSecondary));
            y += rowHeight;

            if (!s.DemographicsAvailable)
            {
                foreach (string group in new[] { "Younger voters", "Older voters", "Urban", "Rural", "By income" })
                {
                    DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), group, "—", MutedBody(name), MutedBody(figure));
                    y += rowHeight;
                }

                DrawResultsNote(new Rect(r.x, y + Mathf.Round(4f * uy), r.width, r.yMax - y - Mathf.Round(4f * uy)), r,
                    "THE ELECTORATE IS COUNTED AS ONE GROUP, SO THESE ROWS HAVE NO ANSWER YET — NOT A ZERO, AND NOT A GUESS.",
                    "Results voter-group note");
            }
        }

        private static GUIStyle MutedBody(GUIStyle from)
        {
            var style = new GUIStyle(from);
            style.normal.textColor = PoliSimTheme.TextMuted;
            style.hover.textColor = PoliSimTheme.TextMuted;
            style.active.textColor = PoliSimTheme.TextMuted;
            style.focused.textColor = PoliSimTheme.TextMuted;
            return style;
        }

        /// <summary>A wrapped footnote in the campaign screens' idiom: `GUI.Label` with the height MEASURED for the width, never the single-line `MeasuredLabel` (W-E5's overflow class).</summary>
        private void DrawResultsNote(Rect r, Rect container, string text, string label)
        {
            GUIStyle note = DeskCaptionWrapped(8.5f, PoliSimTheme.TextMuted);
            float height = Mathf.Ceil(note.CalcHeight(new GUIContent(text), r.width));
            var rect = new Rect(r.x, r.y, r.width, Mathf.Min(height, Mathf.Max(0f, r.height)));
            if (Event.current.type == EventType.Repaint)
            {
                UiContainmentGuard.Check(label, rect, container);
            }

            GUI.Label(rect, text, note);
        }

        private void DrawResultsFooter(Rect r, ResultsScreenSnapshot s)
        {
            GUIStyle caption = DeskCaption(9f, PoliSimTheme.TextSecondary);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, r.width, r.height),
                string.Format(CultureInfo.InvariantCulture,
                    "TURNOUT {0:P2} OF {2:N0} ELIGIBLE, AS PUBLISHED   ·   SHARES ARE OF THE {1:N0} VOTES THESE PARTIES TOOK, NOT OF ALL BALLOTS CAST   ·   THE COMPARISON IS {3}",
                    s.Turnout, s.ValidVotes, s.Eligible, s.PreviousLabel.ToUpperInvariant()),
                caption);
        }
    }
}
