using System.Globalization;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// W-E5 — **the debate screen**, the fifth of the Track E class. Same board, same primitives and
    /// the same 440 / 250 / 440 columns as Campaign HQ, the action screen and the polling screen.
    ///
    /// ⚠ **HARNESS ONLY — R-N2 holds until W-G1**, by the same mechanism as its siblings.
    ///
    /// **The screen's ceiling is the model's ceiling.** `DebateResult` carries performance indices,
    /// a margin, a coverage shock and a momentum shock — and no share, no preference, no party
    /// standing (asserted by reflection since W-B7). So the verdict column reports exactly those and
    /// stops. A debate screen that ended with "+1.2 % in the polls" would be inventing the single
    /// number §15 refuses to produce, and the player would learn to read a debate as votes rather
    /// than as coverage and momentum, which is the opposite of what the chain says.
    ///
    /// **An exchange that has not happened is drawn as ABSENT, not as zero** (the Desk's Year-0
    /// convention, 1m-r2). Mid-debate, the remaining exchanges are em-dash rows: a zero would claim
    /// the candidates scored nothing, which is a different statement from "this has not run yet",
    /// and the player can only tell them apart if the screen does.
    ///
    /// **The running figure mid-debate is the running MEAN, and is labelled as such.** The
    /// performance index is a mean over ALL exchanges, so quoting it before the end would be quoting
    /// a number the debate has not produced; the screen quotes the mean of what HAS run and says
    /// how many that is.
    /// </summary>
    public partial class GameController
    {
        private DebateScreenSnapshot? _campaignDebateScreen;
        private Rect _campaignDebateInnerRect;

        internal void SetCampaignDebateScreen(DebateScreenSnapshot? snapshot)
        {
            _campaignDebateScreen = snapshot;
        }

        private void DrawCampaignDebateStage(float availableHeight, float availableWidth, DebateScreenSnapshot snapshot)
        {
            float innerWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle);

            GUILayout.BeginVertical(_boxStyle, GUILayout.Width(availableWidth),
                GUILayout.ExpandHeight(true));   // P2-1.1: the sheet fills the frame-height column
            Rect inner = GUILayoutUtility.GetRect(innerWidth, availableHeight,
                GUILayout.Width(innerWidth), GUILayout.Height(availableHeight));
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint) { _campaignDebateInnerRect = inner; }
            else if (_campaignDebateInnerRect.width > 1f) { inner = _campaignDebateInnerRect; }

            float ux = inner.width / DeskBoardInnerWidth;
            float uy = inner.height / DeskBoardInnerHeight;
            Rect Board(float x, float y, float w, float h) =>
                new Rect(inner.x + x * ux, inner.y + y * uy, w * ux, h * uy);

            DrawCampaignMasthead(Board(0f, 0f, 1156f, 28f), snapshot.Campaign, StageTitle(snapshot.Stage));

            DrawDebatePodiums(Board(0f, 36f, 440f, 576f), snapshot);
            DrawDebatePreparation(Board(453f, 36f, 250f, 576f), snapshot);
            DrawDebateFloor(Board(716f, 36f, 440f, 576f), snapshot);

            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(Board(0f, 620f, 1156f, 1f), PoliSimTheme.HairlineStrong);
            }

            DrawCampaignStrip(Board(0f, 627f, 1156f, 53f), snapshot.Campaign);
        }

        private static string StageTitle(DebateStage stage)
        {
            switch (stage)
            {
                case DebateStage.Preparation: return "CAMPAIGN · DEBATE — PREPARATION";
                case DebateStage.InProgress: return "CAMPAIGN · DEBATE — ON THE FLOOR";
                default: return "CAMPAIGN · DEBATE — THE VERDICT";
            }
        }

        // ------------------------------------------------------------------------------------------
        // Left: who is on the podiums, and the §16 attributes a move actually draws on.
        // ------------------------------------------------------------------------------------------
        private void DrawDebatePodiums(Rect r, DebateScreenSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "THE PODIUMS — WHAT EACH ONE BRINGS TO IT", ux, uy);

            GUIStyle nameStyle = DeskBody(13f, PoliSimTheme.TextPrimary);
            GUIStyle rowStyle = DeskBody(11f, PoliSimTheme.TextSecondary);
            GUIStyle figureStyle = DeskCaption(11f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            float rowHeight = Mathf.Max(Mathf.Round(18f * uy), rowStyle.CalcSize(new GUIContent("Ag")).y);
            float half = Mathf.Round((r.width - Mathf.Round(16f * ux)) / 2f);

            var names = new[] { s.NameA, s.NameB };
            var profiles = new[] { s.CandidateA, s.CandidateB };
            for (int side = 0; side < 2; side++)
            {
                float x = r.x + side * (half + Mathf.Round(16f * ux));
                float cy = y;
                PoliSimWidgets.MeasuredLabel(new Rect(x, cy, half, rowHeight), names[side], nameStyle);
                cy += rowHeight + Mathf.Round(2f * uy);

                CandidateProfile p = profiles[side];
                DrawCampaignRow(new Rect(x, cy, half, rowHeight), "Debate skill", Index(p.DebateSkill), rowStyle, figureStyle); cy += rowHeight;
                DrawCampaignRow(new Rect(x, cy, half, rowHeight), "Charisma", Index(p.Charisma), rowStyle, figureStyle); cy += rowHeight;
                DrawCampaignRow(new Rect(x, cy, half, rowHeight), "Policy knowledge", Index(p.PolicyKnowledge), rowStyle, figureStyle); cy += rowHeight;
                DrawCampaignRow(new Rect(x, cy, half, rowHeight), "Communication", Index(p.Communication), rowStyle, figureStyle); cy += rowHeight;
                DrawCampaignRow(new Rect(x, cy, half, rowHeight), "Popularity", Index(p.Popularity), rowStyle, figureStyle); cy += rowHeight;
            }

            float noteY = y + rowHeight * 7f + Mathf.Round(8f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, noteY, r.width, 1f), PoliSimTheme.Hairline);
            }

            DrawDebateNote(new Rect(r.x, noteY + Mathf.Round(6f * uy), r.width, r.yMax - noteY - Mathf.Round(6f * uy)), r,
                "THESE RATINGS ARE THE GAME'S OWN INVENTION, NOT A MEASUREMENT OF ANYONE — AND WHAT A MOVE DRAWS ON IS NOT A PLAIN AVERAGE OF THE FIVE.",
                "Debate podiums note");
        }

        private static string Index(double value) => value.ToString("F0", CultureInfo.InvariantCulture);

        // ------------------------------------------------------------------------------------------
        // Middle: what each side bought before the cameras came on. §35's curve, drawn as a figure.
        // ------------------------------------------------------------------------------------------
        private void DrawDebatePreparation(Rect r, DebateScreenSnapshot s)
        {
            float ux = r.width / 250f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "PREPARATION", ux, uy);

            GUIStyle nameStyle = DeskBody(11f, PoliSimTheme.TextPrimary);
            GUIStyle rowStyle = DeskBody(10f, PoliSimTheme.TextSecondary);
            GUIStyle figureStyle = DeskCaption(10f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            float rowHeight = Mathf.Max(Mathf.Round(17f * uy), rowStyle.CalcSize(new GUIContent("Ag")).y);

            var names = new[] { s.NameA, s.NameB };
            var preps = new[] { s.PreparationA, s.PreparationB };
            for (int side = 0; side < 2; side++)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, rowHeight), names[side], nameStyle);
                y += rowHeight;

                DebatePreparation p = preps[side];
                DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "Hours",
                    p.Hours.ToString("F1", CultureInfo.InvariantCulture), rowStyle, figureStyle);
                y += rowHeight;

                // §35's curve, quoted as the multiplier it actually is - a player who buys hours
                // should see what the hours bought, not a bar with no units.
                double multiplier = Debates.PreparationFloor
                    + (1.0 - Debates.PreparationFloor) * (1.0 - System.Math.Exp(-p.Hours / Debates.PreparationScale));
                DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "Skill multiplier",
                    "x" + multiplier.ToString("F2", CultureInfo.InvariantCulture), rowStyle, figureStyle);
                y += rowHeight;

                if (p.Topics == null || p.Topics.Length == 0)
                {
                    DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight), "NO TOPIC EMPHASISED", rowStyle);
                    y += rowHeight;
                }
                else
                {
                    for (int i = 0; i < p.Topics.Length; i++)
                    {
                        DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight),
                            i == 0 ? "Topic" : "", SpacedIdentifier(p.Topics[i].ToString()), rowStyle, figureStyle);
                        y += rowHeight;
                    }
                }

                y += Mathf.Round(10f * uy);
            }

            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }

            DrawDebateNote(new Rect(r.x, y + Mathf.Round(6f * uy), r.width, r.yMax - y - Mathf.Round(6f * uy)), r,
                "PREPARATION IS A MULTIPLIER ON SKILL, NEVER A SUBSTITUTE FOR IT — AN UNPREPARED CANDIDATE STILL DEBATES, AT "
                + Debates.PreparationFloor.ToString("P0", CultureInfo.InvariantCulture) + " OF SKILL.",
                "Debate preparation note");
        }

        // ------------------------------------------------------------------------------------------
        // Right: the floor itself - the exchanges as they resolve, then the verdict and its two
        // shocks. An exchange that has not run is an em dash, never a zero.
        // ------------------------------------------------------------------------------------------
        private void DrawDebateFloor(Rect r, DebateScreenSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "THE FLOOR — EXCHANGE BY EXCHANGE", ux, uy);

            GUIStyle rowStyle = DeskBody(11f, PoliSimTheme.TextSecondary);
            GUIStyle figureStyle = DeskCaption(11f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            GUIStyle headStyle = DeskCaption(8.5f, PoliSimTheme.TextMuted);
            float rowHeight = Mathf.Max(Mathf.Round(18f * uy), rowStyle.CalcSize(new GUIContent("Ag")).y);
            float topicWidth = Mathf.Round(120f * ux);
            float moveWidth = Mathf.Round(140f * ux);

            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, topicWidth, rowHeight), "TOPIC", headStyle);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x + topicWidth, y, moveWidth, rowHeight), "MOVES", headStyle);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x + topicWidth + moveWidth, y, r.width - topicWidth - moveWidth, rowHeight),
                "POINTS", DeskCaption(8.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleRight));
            y += rowHeight;

            if (s.Stage == DebateStage.Preparation)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight),
                    "THE DEBATE HAS NOT BEGUN — NOTHING HAS BEEN SAID", rowStyle);
                y += rowHeight;
            }

            for (int i = 0; i < s.TotalExchanges && s.Stage != DebateStage.Preparation; i++)
            {
                bool resolved = i < s.Resolved.Length;
                var row = new Rect(r.x, y, r.width, rowHeight);
                if (!resolved)
                {
                    // NOT a zero: an exchange that has not happened has no points, and the em dash
                    // is the Desk's own word for that (1m-r2).
                    DrawCampaignRow(row, "—", "—", MutedStyle(rowStyle), MutedStyle(figureStyle));
                    y += rowHeight;
                    continue;
                }

                DebateExchange e = s.Resolved[i];
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, topicWidth, rowHeight), SpacedIdentifier(e.Topic.ToString()), rowStyle);
                PoliSimWidgets.MeasuredLabel(new Rect(r.x + topicWidth, y, moveWidth, rowHeight),
                    Abbreviate(e.MoveA) + " / " + Abbreviate(e.MoveB), rowStyle);
                PoliSimWidgets.MeasuredLabel(new Rect(r.x + topicWidth + moveWidth, y, r.width - topicWidth - moveWidth, rowHeight),
                    string.Format(CultureInfo.InvariantCulture, "{0:F1} / {1:F1}", e.PointsA, e.PointsB), figureStyle);
                y += rowHeight;
            }

            y += Mathf.Round(8f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }

            y += Mathf.Round(8f * uy);
            DrawDebateVerdict(new Rect(r.x, y, r.width, r.yMax - y), s, rowStyle, figureStyle, rowHeight, uy);
        }

        private static GUIStyle MutedStyle(GUIStyle from)
        {
            var style = new GUIStyle(from);
            style.normal.textColor = PoliSimTheme.TextMuted;
            style.hover.textColor = PoliSimTheme.TextMuted;
            style.active.textColor = PoliSimTheme.TextMuted;
            style.focused.textColor = PoliSimTheme.TextMuted;
            return style;
        }

        /// <summary>"AttackOpponent" -> "Attack" - the floor table has room for a verb, not a sentence.</summary>
        private static string Abbreviate(DebateMove move)
        {
            switch (move)
            {
                case DebateMove.AttackOpponent: return "Attack";
                case DebateMove.DefendPolicy: return "Defend";
                case DebateMove.ChangeSubject: return "Change subject";
                case DebateMove.AppealEmotionally: return "Appeal";
                case DebateMove.PresentStatistics: return "Statistics";
                case DebateMove.IgnoreAttack: return "Ignore";
                default: return "Counter";
            }
        }

        /// <summary>
        /// The verdict, and the ceiling. Performance indices, the margin, and the TWO SHOCKS - and
        /// nothing that looks like a vote share, because the model produces none and a screen that
        /// invented one would teach the player to read a debate as votes.
        /// </summary>
        private void DrawDebateVerdict(Rect r, DebateScreenSnapshot s, GUIStyle rowStyle, GUIStyle figureStyle, float rowHeight, float uy)
        {
            GUIStyle head = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, r.width, rowHeight), "THE VERDICT", head);
            float y = r.y + rowHeight;

            if (!s.HasVerdict)
            {
                s.RunningPoints(out double runningA, out double runningB);
                if (s.Resolved.Length == 0)
                {
                    DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight),
                        "NO VERDICT — THE DEBATE HAS NOT BEEN HELD", rowStyle);
                    y += rowHeight;
                }
                else
                {
                    // The running MEAN, labelled as running: the performance index is a mean over
                    // ALL exchanges, so quoting it now would quote a number that does not exist yet.
                    DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight),
                        string.Format(CultureInfo.InvariantCulture, "Running mean, {0} of {1} exchanges", s.Resolved.Length, s.TotalExchanges),
                        string.Format(CultureInfo.InvariantCulture, "{0:F1} / {1:F1}", runningA, runningB), rowStyle, figureStyle);
                    y += rowHeight;
                }

                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight), "MARGIN — NOT YET", rowStyle); y += rowHeight;
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight), "COVERAGE SHOCK — NOT YET", rowStyle); y += rowHeight;
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight), "MOMENTUM SHOCK — NOT YET", rowStyle); y += rowHeight;
            }
            else
            {
                DebateResult result = s.Result;
                DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "Performance index",
                    string.Format(CultureInfo.InvariantCulture, "{0:F1} / {1:F1}", result.PerformanceA, result.PerformanceB), rowStyle, figureStyle);
                y += rowHeight;

                string winner = result.Winner < 0 ? "A draw" : (result.Winner == 0 ? s.NameA : s.NameB);
                DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "Margin, to " + winner,
                    System.Math.Abs(result.Margin).ToString("F1", CultureInfo.InvariantCulture) + " pts", rowStyle, figureStyle);
                y += rowHeight;

                DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "Coverage shock",
                    result.CoverageShock.ToString("F2", CultureInfo.InvariantCulture), rowStyle, figureStyle);
                y += rowHeight;

                DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), "Momentum shock, winner",
                    "+" + result.MomentumShockPp.ToString("F2", CultureInfo.InvariantCulture) + " pp", rowStyle, figureStyle);
                y += rowHeight;
            }

            DrawDebateNote(new Rect(r.x, y + Mathf.Round(6f * uy), r.width, r.yMax - y - Mathf.Round(6f * uy)), r,
                "A DEBATE MOVES COVERAGE AND MOMENTUM. IT MOVES NO VOTES DIRECTLY — WHATEVER THE POLLS DO IN THE DAYS AFTER IS THE MOMENTUM'S DOING.",
                "Debate verdict note");
        }

        /// <summary>
        /// A wrapped footnote in the campaign screens' own idiom: `GUI.Label` with the height
        /// MEASURED for the width, not `MeasuredLabel` — which is a single-line widget and shrinks
        /// to fit, so a sentence handed to it either shrinks below the 8 px floor or trips the
        /// overflow guard. (It tripped it twelve times on this screen's first film; that is the
        /// guard working.) The containment check is the siblings' too.
        /// </summary>
        private void DrawDebateNote(Rect r, Rect container, string text, string label)
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
    }
}
