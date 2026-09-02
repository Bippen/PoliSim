using System.Globalization;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// W-E8 — **the coalition screen**, the seventh and last Track E screen. Same 1156×680 board and
    /// the same 440 / 250 / 440 columns as its siblings.
    ///
    /// ⚠ **HARNESS ONLY — R-N2 holds until W-G1.**
    ///
    /// **The screen's one real decision is how to show a red line, and §36 decides it.** A DECLARED
    /// line is public: a party said it, and the citation is on disk, so the screen states it flatly
    /// and names who holds it. A DERIVED line is the model's own reading of a distance between two
    /// parties — nobody in the world has uttered it — so showing it as a refusal would hand the
    /// player a certainty that does not exist. It is drawn as the distance it is, with the axis and
    /// the gap, and the reader draws their own conclusion.
    ///
    /// **The arithmetic that was refused is the point of the middle column.** A coalition screen
    /// that showed only what CAN form teaches the player that the arithmetic is the whole story.
    /// This one shows the majorities the seats would have carried and the line that stopped each —
    /// which is what makes a red line legible as politics rather than as a missing option.
    /// </summary>
    public partial class GameController
    {
        private CoalitionScreenSnapshot? _campaignCoalitionScreen;
        private Rect _campaignCoalitionInnerRect;

        internal void SetCampaignCoalitionScreen(CoalitionScreenSnapshot? snapshot)
        {
            _campaignCoalitionScreen = snapshot;
        }

        private void DrawCampaignCoalitionStage(float availableHeight, float availableWidth, CoalitionScreenSnapshot snapshot)
        {
            float innerWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle);

            GUILayout.BeginVertical(_boxStyle, GUILayout.Width(availableWidth),
                GUILayout.ExpandHeight(true));   // P2-1.1: the sheet fills the frame-height column
            Rect inner = GUILayoutUtility.GetRect(innerWidth, availableHeight,
                GUILayout.Width(innerWidth), GUILayout.Height(availableHeight));
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint) { _campaignCoalitionInnerRect = inner; }
            else if (_campaignCoalitionInnerRect.width > 1f) { inner = _campaignCoalitionInnerRect; }

            float ux = inner.width / DeskBoardInnerWidth;
            float uy = inner.height / DeskBoardInnerHeight;
            Rect Board(float x, float y, float w, float h) =>
                new Rect(inner.x + x * ux, inner.y + y * uy, w * ux, h * uy);

            DrawCoalitionMasthead(Board(0f, 0f, 1156f, 28f), snapshot);
            DrawCoalitionArithmetic(Board(0f, 36f, 440f, 576f), snapshot);
            DrawCoalitionRefused(Board(453f, 36f, 250f, 576f), snapshot);
            DrawCoalitionRedLines(Board(716f, 36f, 440f, 576f), snapshot);

            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(Board(0f, 620f, 1156f, 1f), PoliSimTheme.HairlineStrong);
            }

            DrawCoalitionFooter(Board(0f, 627f, 1156f, 53f), snapshot);
        }

        private void DrawCoalitionMasthead(Rect r, CoalitionScreenSnapshot s)
        {
            GUIStyle title = DeskCaption(10.5f, PoliSimTheme.TextPrimary, bold: true);
            GUIStyle right = DeskCaption(9.5f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, r.width * 0.55f, r.height),
                "GOVERNMENT FORMATION · " + s.CountryName.ToUpperInvariant(), title);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x + r.width * 0.55f, r.y, r.width * 0.45f, r.height),
                string.Format(CultureInfo.InvariantCulture, "{0} SEATS · {1} FOR A MAJORITY · {2}",
                    s.TotalSeats, s.Majority, Outcome(s.Result.Outcome)), right);
        }

        private static string Outcome(CoalitionOutcomeKind kind)
        {
            switch (kind)
            {
                case CoalitionOutcomeKind.MajorityCoalition: return "A MAJORITY COALITION";
                case CoalitionOutcomeKind.MinorityGovernment: return "A MINORITY GOVERNMENT";
                case CoalitionOutcomeKind.ConfidenceAndSupply: return "CONFIDENCE AND SUPPLY";
                case CoalitionOutcomeKind.NewElection: return "NO GOVERNMENT — A NEW ELECTION";
                default: return "THE GOVERNMENT FELL";
            }
        }

        // ------------------------------------------------------------------------------------------
        // Left: the arithmetic, and what actually formed out of it.
        // ------------------------------------------------------------------------------------------
        private void DrawCoalitionArithmetic(Rect r, CoalitionScreenSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "THE ARITHMETIC — SEATS AND NEGOTIATING POWER", ux, uy);

            GUIStyle head = DeskCaption(8.5f, PoliSimTheme.TextMuted);
            GUIStyle name = DeskBody(12f, PoliSimTheme.TextPrimary);
            GUIStyle figure = DeskCaption(11f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            float rowHeight = Mathf.Max(Mathf.Round(19f * uy), name.CalcSize(new GUIContent("Ag")).y);

            float wName = Mathf.Round(70f * ux);
            float wSeats = Mathf.Round(80f * ux);
            float wPower = Mathf.Round(120f * ux);
            float wRole = r.width - wName - wSeats - wPower;

            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, wName, rowHeight), "PARTY", head);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x + wName, y, wSeats, rowHeight), "SEATS",
                DeskCaption(8.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleRight));
            PoliSimWidgets.MeasuredLabel(new Rect(r.x + wName + wSeats, y, wPower, rowHeight), "PIVOTALITY",
                DeskCaption(8.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleRight));
            PoliSimWidgets.MeasuredLabel(new Rect(r.x + wName + wSeats + wPower, y, wRole, rowHeight), "IN THIS GOVERNMENT",
                DeskCaption(8.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleRight));
            y += rowHeight;

            var order = new System.Collections.Generic.List<int>();
            for (int p = 0; p < s.PartyNames.Length; p++) { order.Add(p); }
            order.Sort((a, b) => s.Seats[b].CompareTo(s.Seats[a]));

            GovernmentOption g = s.Result.Government;
            foreach (int p in order)
            {
                string role = (g.Cabinet & (1 << p)) != 0 ? "in cabinet"
                    : (g.Support & (1 << p)) != 0 ? "supporting"
                    : "—";
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, wName, rowHeight), s.PartyNames[p], name);
                PoliSimWidgets.MeasuredLabel(new Rect(r.x + wName, y, wSeats, rowHeight),
                    s.Seats[p].ToString(CultureInfo.InvariantCulture), figure);
                PoliSimWidgets.MeasuredLabel(new Rect(r.x + wName + wSeats, y, wPower, rowHeight),
                    s.Result.NegotiatingPower[p].ToString("P1", CultureInfo.InvariantCulture), figure);
                PoliSimWidgets.MeasuredLabel(new Rect(r.x + wName + wSeats + wPower, y, wRole, rowHeight), role, figure);
                y += rowHeight;
            }

            y += Mathf.Round(8f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }

            y += Mathf.Round(8f * uy);
            if (s.Result.Outcome == CoalitionOutcomeKind.NewElection)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight),
                    "NO GOVERNMENT COULD BE FORMED FROM THESE SEATS", name);
                return;
            }

            DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "CABINET",
                string.Format(CultureInfo.InvariantCulture, "{0}   {1} seats", s.Name(g.Cabinet), g.CabinetSeats),
                DeskBody(11f, PoliSimTheme.TextPrimary), figure);
            y += rowHeight;
            DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "CARRIED FROM OUTSIDE BY",
                g.Support == 0 ? "nobody" : string.Format(CultureInfo.InvariantCulture, "{0}   {1} seats together", s.Name(g.Support), g.SupportedSeats),
                DeskBody(11f, PoliSimTheme.TextSecondary), figure);
            y += rowHeight;
            DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "VOTING AGAINST",
                g.OpposedSeats.ToString(CultureInfo.InvariantCulture) + " seats",
                DeskBody(11f, PoliSimTheme.TextSecondary), figure);
        }

        // ------------------------------------------------------------------------------------------
        // Middle: the majorities the seats would have carried, and the line that refused each.
        // A screen showing only what CAN form teaches that arithmetic is the whole story.
        // ------------------------------------------------------------------------------------------
        private void DrawCoalitionRefused(Rect r, CoalitionScreenSnapshot s)
        {
            float ux = r.width / 250f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "MAJORITIES THE SEATS ALLOWED, AND WHAT REFUSED THEM", ux, uy);

            GUIStyle name = DeskBody(10f, PoliSimTheme.TextSecondary);
            GUIStyle figure = DeskCaption(9.5f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            float rowHeight = Mathf.Max(Mathf.Round(16f * uy), name.CalcSize(new GUIContent("Ag")).y);

            if (s.Result.BlockedByRedLine.Count == 0)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight), "NOTHING WAS REFUSED", name);
                return;
            }

            // Smallest first: the tightest refusals are the ones a reader can hold in mind.
            var blocked = new System.Collections.Generic.List<(int Cabinet, RedLine Line)>(s.Result.BlockedByRedLine);
            blocked.Sort((a, b) => CountBits(a.Cabinet).CompareTo(CountBits(b.Cabinet)));

            int room = Mathf.Max(1, Mathf.FloorToInt((r.yMax - y) / (rowHeight * 2f)) - 1);
            int shown = 0;
            foreach ((int cabinet, RedLine line) in blocked)
            {
                if (shown >= room) { break; }
                DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), s.Name(cabinet),
                    "refused", name, figure);
                y += rowHeight;
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, rowHeight),
                    "by " + s.PartyNames[line.A] + " / " + s.PartyNames[line.B]
                    + (line.Kind == RedLineKind.Declared ? ", declared" : ", on distance"),
                    DeskCaption(9f, PoliSimTheme.TextMuted));
                y += rowHeight;
                shown++;
            }

            if (shown < blocked.Count)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight),
                    string.Format(CultureInfo.InvariantCulture, "AND {0} MORE", blocked.Count - shown), name);
            }
        }

        private static int CountBits(int v)
        {
            int c = 0;
            while (v != 0) { c += v & 1; v >>= 1; }
            return c;
        }

        // ------------------------------------------------------------------------------------------
        // Right: the red lines themselves - DECLARED stated flatly, DERIVED shown as a distance.
        // ------------------------------------------------------------------------------------------
        private void DrawCoalitionRedLines(Rect r, CoalitionScreenSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "RED LINES — WHAT WAS SAID, AND WHAT IS ONLY MEASURED", ux, uy);

            GUIStyle name = DeskBody(11f, PoliSimTheme.TextPrimary);
            GUIStyle detail = DeskCaption(9f, PoliSimTheme.TextMuted);
            float rowHeight = Mathf.Max(Mathf.Round(17f * uy), name.CalcSize(new GUIContent("Ag")).y);

            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, rowHeight),
                "DECLARED — A PARTY SAID THIS, AND IT IS ON THE RECORD", DeskCaption(8.5f, PoliSimTheme.TextSecondary));
            y += rowHeight;

            var declared = s.Declared();
            if (declared.Count == 0)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight), "NONE ON RECORD", name);
                y += rowHeight;
            }
            else
            {
                foreach (RedLine line in declared)
                {
                    DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight),
                        s.PartyNames[line.A] + " will not " + (line.BlocksSupport ? "depend on " : "sit with ") + s.PartyNames[line.B],
                        line.BlocksSupport ? "nor support" : "would support", name,
                        DeskCaption(10f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight));
                    y += rowHeight;
                }
            }

            y += Mathf.Round(10f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }

            y += Mathf.Round(8f * uy);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, rowHeight),
                "MEASURED — A DISTANCE, NOT A REFUSAL ANYONE UTTERED", DeskCaption(8.5f, PoliSimTheme.TextSecondary));
            y += rowHeight;

            int shown = 0;
            foreach (RedLine line in s.RedLines)
            {
                if (line.Kind != RedLineKind.Derived || shown >= 6) { continue; }
                DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight),
                    s.PartyNames[line.A] + " and " + s.PartyNames[line.B], Distance(line.Basis),
                    MutedBody(name), MutedBody(DeskCaption(10f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight)));
                y += rowHeight;
                shown++;
            }

            DrawResultsNote(new Rect(r.x, y + Mathf.Round(6f * uy), r.width, r.yMax - y - Mathf.Round(6f * uy)), r,
                "A DECLARED LINE IS PUBLIC AND CAN BE QUOTED. A MEASURED ONE IS THIS MODEL READING A GAP BETWEEN TWO PARTIES — NOBODY HAS SAID IT, AND THE SCREEN WILL NOT PUT IT IN THEIR MOUTHS.",
                "Coalition red-line note");
        }

        /// <summary>The gap out of a derived line's basis string, without the word DERIVED - the column already says that.</summary>
        private static string Distance(string basis)
        {
            int colon = basis.IndexOf(':');
            return colon >= 0 && colon + 2 < basis.Length ? basis.Substring(colon + 2) : basis;
        }

        private void DrawCoalitionFooter(Rect r, CoalitionScreenSnapshot s)
        {
            GUIStyle caption = DeskCaption(9f, PoliSimTheme.TextSecondary);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, r.width, r.height),
                string.Format(CultureInfo.InvariantCulture,
                    "{0} VIABLE GOVERNMENT(S) FROM THESE SEATS   ·   {1} ARITHMETIC MAJORITIES REFUSED BY A RED LINE   ·   PIVOTALITY IS THE SEATS, NOT AN OPINION",
                    s.Result.Viable.Count, s.Result.BlockedByRedLine.Count),
                caption);
        }
    }
}
