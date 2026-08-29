using System.Globalization;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// W-E3 — **the action screen**, the second of the Track E class. Same idiom and same board as
    /// Campaign HQ (`GameController.Campaign.cs`), and it reuses that screen's masthead, strip and
    /// ledger primitives outright rather than restating them: the two campaign screens must not
    /// drift into two dialects.
    ///
    /// ⚠ **HARNESS ONLY — R-N2 holds until W-G1**, by exactly the mechanism W-E1 uses: the screen
    /// draws when `_campaignActionScreen` has a value, and only the screenshot driver sets it.
    ///
    /// **What this screen exists to make visible: §42's chain.** W-B3 proved architecturally that no
    /// action can write a vote share — every effect must travel reach → exposure → relevance →
    /// persuasion/enthusiasm. Until now that was a property of the code only a harness could see.
    /// Here it is the middle column: each stage, its value, and the fact that every one of them
    /// multiplies.
    ///
    /// **The estimate is a RANGE, and the range is earned.** Every option carries its own
    /// `CampaignActions.ChainBand`, whose width is the measured polling error propagated through the
    /// chain (§20–§21) — not an authored ±. `ChainBandHarness` proves by sweep that the span really
    /// bounds the whole uncertainty box. Three consequences the drawing code is built around:
    ///
    /// 1. **The bands share one scale** across the options, because the decision is a comparison and
    ///    a per-row scale would make every action look equally promising.
    /// 2. **A zero-width band is printed as a POINT with its reason**, never as `3,477 — 3,477`.
    ///    A range that is not a range is false precision wearing the costume of honesty.
    /// 3. **An unmeasured quantity prints no number at all** (§36) — an unbought fact is an absent
    ///    estimate, not a wide one, and the two must not look alike.
    /// </summary>
    public partial class GameController
    {
        /// <summary>
        /// Non-null ONLY while the screenshot harness has staged one. Mirrors `_campaignScreen`
        /// exactly, including that nothing serialises it and no player path writes it.
        /// </summary>
        private ActionScreenSnapshot? _campaignActionScreen;

        private Rect _campaignActionInnerRect;

        /// <summary>The harness's only door in. `internal`, like `SetCampaignScreen`, for the same reason.</summary>
        internal void SetCampaignActionScreen(ActionScreenSnapshot? snapshot)
        {
            _campaignActionScreen = snapshot;
        }

        private void DrawCampaignActionStage(float availableHeight, float availableWidth, ActionScreenSnapshot snapshot)
        {
            float innerWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle);

            GUILayout.BeginVertical(_boxStyle, GUILayout.Width(availableWidth),
                GUILayout.Height(availableHeight + _boxStyle.padding.vertical));
            Rect inner = GUILayoutUtility.GetRect(innerWidth, availableHeight,
                GUILayout.Width(innerWidth), GUILayout.Height(availableHeight));
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint) { _campaignActionInnerRect = inner; }
            else if (_campaignActionInnerRect.width > 1f) { inner = _campaignActionInnerRect; }

            float ux = inner.width / DeskBoardInnerWidth;
            float uy = inner.height / DeskBoardInnerHeight;
            Rect Board(float x, float y, float w, float h) =>
                new Rect(inner.x + x * ux, inner.y + y * uy, w * ux, h * uy);

            DrawCampaignMasthead(Board(0f, 0f, 1156f, 28f), snapshot.Campaign, "CAMPAIGN · ACTIONS");

            DrawActionOptions(Board(0f, 36f, 440f, 576f), snapshot);
            DrawActionChain(Board(453f, 36f, 250f, 576f), snapshot);
            DrawActionEstimate(Board(716f, 36f, 440f, 576f), snapshot);

            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(Board(0f, 620f, 1156f, 1f), PoliSimTheme.HairlineStrong);
            }

            DrawCampaignStrip(Board(0f, 627f, 1156f, 53f), snapshot.Campaign);
        }

        // ------------------------------------------------------------------------------------------
        // Left: what §12 lets you run today, what it costs, and what it would buy — SIDE BY SIDE.
        // ------------------------------------------------------------------------------------------
        private void DrawActionOptions(Rect r, ActionScreenSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "WHAT YOU COULD RUN TODAY", ux, uy);

            if (s.Options == null || s.Options.Length == 0)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, Mathf.Round(20f * uy)),
                    "NO CAMPAIGN ACTIONS ARE OPEN IN THIS PHASE", DeskBody(13f, PoliSimTheme.TextPrimary));
                return;
            }

            GUIStyle costStyle = DeskCaption(10.5f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            // Same class as the polling screen's head line: the row carries a BODY name and a MONO
            // cost, and the two faces do not share a line box. Take the taller, not the name's.
            float nameHeight = Mathf.Max(
                DeskBody(13f, PoliSimTheme.TextPrimary).CalcSize(new GUIContent("Ag")).y,
                Mathf.Ceil(DeskCaptionHeight(DeskCaption(10.5f, PoliSimTheme.TextPrimary))));
            float barHeight = Mathf.Round(7f * uy);
            float rowHeight = nameHeight + barHeight + Mathf.Round(11f * uy);
            float costWidth = Mathf.Ceil(costStyle.CalcSize(new GUIContent("000,000 kr · 0 h")).x) + Mathf.Round(8f * ux);
            float pad = Mathf.Round(6f * ux);

            // ONE scale across every row: this column is a comparison, and a per-row scale would
            // make the cheapest social post look exactly as promising as a half-million-krona TV buy.
            double scale = s.PersuasionScale;

            for (int i = 0; i < s.Options.Length; i++)
            {
                ActionOption option = s.Options[i];
                bool selected = i == s.SelectedIndex;
                var row = new Rect(r.x, y, r.width, rowHeight - Mathf.Round(3f * uy));

                // The selected row is a plate; an unaffordable one is muted THROUGHOUT rather than
                // badged, because `TrySpend` refuses it outright (W-B2) and a row that reads live
                // but cannot be run is the same lie as a live-looking dead chip.
                if (Event.current.type == EventType.Repaint && selected)
                {
                    PoliSimTheme.RoundedCard(row, PoliSimTheme.Tile, PoliSimTheme.HairlineStrong, 0f);
                }

                Color ink = !option.Affordable
                    ? PoliSimTheme.TextMuted
                    : selected ? PoliSimTheme.TextPrimary : PoliSimTheme.TextSecondary;

                PoliSimWidgets.MeasuredLabel(
                    new Rect(row.x + pad, row.y + Mathf.Round(2f * uy), Mathf.Max(1f, row.width - costWidth - pad * 2f), nameHeight),
                    SpacedIdentifier(option.Kind.ToString()), DeskBody(13f, ink));
                PoliSimWidgets.MeasuredLabel(
                    new Rect(row.xMax - costWidth - pad, row.y + Mathf.Round(2f * uy), costWidth, nameHeight),
                    string.Format(CultureInfo.InvariantCulture, "{0} kr · {1:F0} h", Kronor(option.MoneyCost), option.Hours),
                    DeskCaption(10.5f, ink, false, TextAnchor.MiddleRight));

                // The row's own estimate band, on the shared scale. Unmeasured draws an empty track
                // rather than a zero-length bar: nothing known is not the same as nothing gained.
                var track = new Rect(row.x + pad, row.y + Mathf.Round(2f * uy) + nameHeight + Mathf.Round(3f * uy),
                    Mathf.Max(1f, row.width - pad * 2f), barHeight);
                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimTheme.RoundedCard(track, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f);
                    if (option.Estimate.Measured)
                    {
                        float lo = Mathf.Clamp01((float)(option.Estimate.Low.Persuasion / scale));
                        float hi = Mathf.Clamp01((float)(option.Estimate.High.Persuasion / scale));
                        PoliSimTheme.RoundedBox(new Rect(track.x + track.width * lo, track.y,
                                Mathf.Max(1f, track.width * (hi - lo)), track.height),
                            option.Affordable
                                ? UiPalette.GetAreaColor(UiPalette.SystemArea.Political)
                                : PoliSimTheme.Tint(PoliSimTheme.TextSecondary, 0.45f), 0f);
                    }
                }

                y += rowHeight;
            }

            y += Mathf.Round(6f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }

            y += Mathf.Round(7f * uy);

            int affordable = 0;
            bool anyMeasured = false;
            foreach (ActionOption option in s.Options)
            {
                if (option.Affordable) { affordable++; }
                if (option.Estimate.Measured) { anyMeasured = true; }
            }

            // The scale sentence is printed ONLY when there is a scale. With nothing measured
            // `PersuasionScale` falls back to 1.0 so the bars divide safely, and the first film
            // duly printed "TOP OF SCALE 1" — a number describing nothing, on the one screen whose
            // subject is not printing numbers it cannot justify.
            GUIStyle note = DeskCaptionWrapped(8.5f, PoliSimTheme.TextMuted);
            string noteText = anyMeasured
                ? string.Format(CultureInfo.InvariantCulture,
                    "{0} OF {1} AFFORDABLE ON TODAY'S MONEY AND HOURS — THE REST WOULD BE REFUSED, NOT DISCOUNTED. "
                    + "BARS ARE PERSUASION ESTIMATES ON ONE SHARED SCALE (TOP OF SCALE {2:N0}), SO THE ROWS CAN BE COMPARED.",
                    affordable, s.Options.Length, scale)
                : string.Format(CultureInfo.InvariantCulture,
                    "{0} OF {1} AFFORDABLE ON TODAY'S MONEY AND HOURS — THE REST WOULD BE REFUSED, NOT DISCOUNTED. "
                    + "NOTHING IS POLLED, SO NO ROW CARRIES AN ESTIMATE AND THE TRACKS ARE EMPTY.",
                    affordable, s.Options.Length);
            float noteHeight = Mathf.Ceil(note.CalcHeight(new GUIContent(noteText), r.width));
            var noteRect = new Rect(r.x, y, r.width, noteHeight);
            if (Event.current.type == EventType.Repaint)
            {
                UiContainmentGuard.Check("Action options note", noteRect, r);
            }

            GUI.Label(noteRect, noteText, note);
        }

        // ------------------------------------------------------------------------------------------
        // Centre: §42's chain, stage by stage. The architectural rule, finally on screen.
        // ------------------------------------------------------------------------------------------
        private void DrawActionChain(Rect r, ActionScreenSnapshot s)
        {
            float ux = r.width / 250f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "HOW A MESSAGE BECOMES VOTES — EACH STAGE MULTIPLIES", ux, uy);

            GUIStyle nameStyle = DeskBody(12f, PoliSimTheme.TextPrimary);
            GUIStyle valueStyle = DeskCaption(11f, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleRight);
            float rowHeight = Mathf.Max(Mathf.Round(20f * uy), nameStyle.CalcSize(new GUIContent("Ag")).y);

            if (!s.HasSelection || !s.Estimate.Measured)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight),
                    s.HasSelection ? "NOT MEASURED — NO CHAIN TO SHOW" : "NO ACTION SELECTED", nameStyle);
                return;
            }

            CampaignActions.ChainTrace mid = s.Estimate.Mid;

            // The stages in the order §42 states them. Reach and exposure are people; salience,
            // relevance and credibility are fractions; the last two are the chain's ONLY outputs.
            // Every figure is InvariantCulture — this machine's sv-SE renders `94 613` with a
            // non-breaking space otherwise, which is B3's recorded defect class.
            DrawChainStage(new Rect(r.x, y, r.width, rowHeight), "Reach",
                Invariant("{0:N0}", mid.Reach), nameStyle, valueStyle);
            y += rowHeight;
            DrawChainStage(new Rect(r.x, y, r.width, rowHeight), "× attention",
                Invariant("{0:N0} exposed", mid.Exposure), nameStyle, valueStyle);
            y += rowHeight + Mathf.Round(4f * uy);

            DrawChainStage(new Rect(r.x, y, r.width, rowHeight), "Salience",
                Invariant("{0:P0}", mid.Salience), nameStyle, valueStyle);
            y += rowHeight;
            DrawChainStage(new Rect(r.x, y, r.width, rowHeight), "× issue match",
                Invariant("{0:P0} relevant", mid.Relevance), nameStyle, valueStyle);
            y += rowHeight + Mathf.Round(4f * uy);

            DrawChainStage(new Rect(r.x, y, r.width, rowHeight), "Credibility",
                Invariant("{0:P0}", mid.Credibility), nameStyle, valueStyle);
            y += rowHeight + Mathf.Round(6f * uy);

            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }

            y += Mathf.Round(6f * uy);
            DrawChainStage(new Rect(r.x, y, r.width, rowHeight), "Persuasion",
                Invariant("{0:N0}", mid.Persuasion), nameStyle, valueStyle);
            y += rowHeight;
            DrawChainStage(new Rect(r.x, y, r.width, rowHeight), "Enthusiasm",
                Invariant("{0:N0}", mid.Enthusiasm), nameStyle, valueStyle);
            y += rowHeight + Mathf.Round(8f * uy);

            // The sentence the whole architecture is for. Not decoration: it states the invariant a
            // player would otherwise have to infer, and the one W-B3 spent its whole harness proving.
            GUIStyle note = DeskCaptionWrapped(8.5f, PoliSimTheme.TextMuted);
            const string noteText = "PERSUASION AND ENTHUSIASM ARE PRESSURES, NOT VOTES. ZERO ANY STAGE AND THE "
                                    + "EFFECT IS ZERO — NO ACTION CAN MOVE A SHARE WITHOUT TRAVELLING THE WHOLE CHAIN.";
            float noteHeight = Mathf.Ceil(note.CalcHeight(new GUIContent(noteText), r.width));
            var noteRect = new Rect(r.x, y, r.width, noteHeight);
            if (Event.current.type == EventType.Repaint)
            {
                UiContainmentGuard.Check("Action chain note", noteRect, r);
            }

            GUI.Label(noteRect, noteText, note);
        }

        private static void DrawChainStage(Rect rect, string name, string value, GUIStyle nameStyle, GUIStyle valueStyle)
        {
            float valueWidth = Mathf.Ceil(valueStyle.CalcSize(new GUIContent(value)).x) + 6f;
            PoliSimWidgets.MeasuredLabel(
                new Rect(rect.x, rect.y, Mathf.Max(1f, rect.width - valueWidth - 6f), rect.height), name, nameStyle);
            PoliSimWidgets.MeasuredLabel(new Rect(rect.xMax - valueWidth, rect.y, valueWidth, rect.height),
                value, valueStyle);
        }

        // ------------------------------------------------------------------------------------------
        // Right: the live estimate, as a RANGE with its provenance. W-E3's whole point.
        // ------------------------------------------------------------------------------------------
        private void DrawActionEstimate(Rect r, ActionScreenSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "IF YOU RAN THIS TOMORROW", ux, uy);

            GUIStyle body = DeskBody(13f, PoliSimTheme.TextPrimary);

            if (!s.HasSelection)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, Mathf.Round(20f * uy)),
                    "SELECT AN ACTION TO SEE WHAT IT WOULD DO", body);
                return;
            }

            // §36's gate. An unbought fact is an ABSENT estimate, not a wide one, and the screen must
            // not let the two look alike — so this branch prints no number whatsoever.
            if (!s.Estimate.Measured)
            {
                GUIStyle absent = DeskCaptionWrapped(9f, PoliSimTheme.Caution);
                const string absentText = "NOT MEASURED — YOU HAVE NOT POLLED THIS AUDIENCE, SO THERE IS NO HONEST "
                                          + "ESTIMATE TO SHOW. COMMISSION POLLING AND THIS BECOMES A RANGE. "
                                          + "AN UNBOUGHT FACT IS AN ABSENT ESTIMATE, NOT A WIDE ONE.";
                float h = Mathf.Ceil(absent.CalcHeight(new GUIContent(absentText), r.width));
                var absentRect = new Rect(r.x, y, r.width, h);
                if (Event.current.type == EventType.Repaint)
                {
                    UiContainmentGuard.Check("Action estimate absent", absentRect, r);
                }

                GUI.Label(absentRect, absentText, absent);
                return;
            }

            y = DrawEstimateBand(r, y, ux, uy, "PERSUASION PRESSURE",
                s.Estimate.Low.Persuasion, s.Estimate.Mid.Persuasion, s.Estimate.High.Persuasion,
                "SALIENCE AND ISSUE MATCH ARE BOTH POLLED, SO THIS ESTIMATE CARRIES THEIR ERROR.");
            y += Mathf.Round(16f * uy);
            y = DrawEstimateBand(r, y, ux, uy, "ENTHUSIASM PRESSURE",
                s.Estimate.Low.Enthusiasm, s.Estimate.Mid.Enthusiasm, s.Estimate.High.Enthusiasm,
                "NOT SENSITIVE TO WHAT YOU POLLED — ENTHUSIASM FOLLOWS FROM EXPOSURE AND "
                + "CREDIBILITY ALONE, AND YOU KNOW BOTH EXACTLY. THE POINT IS THE WHOLE ANSWER.");
            y += Mathf.Round(18f * uy);

            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }

            y += Mathf.Round(8f * uy);

            // The provenance. A range with no stated source is indistinguishable from a decorative ±,
            // which is the thing this item exists to avoid.
            GUIStyle method = DeskCaptionWrapped(8.5f, PoliSimTheme.TextMuted);
            EstimateProvenance p = s.Provenance;
            string methodText = string.Format(CultureInfo.InvariantCulture,
                "THE SPAN IS YOUR OWN MEASUREMENT ERROR CARRIED THROUGH THE CHAIN — {0}, n = {1:N0}, "
                + "FIELDED {2}, ±{3:F0} POINTS ON SALIENCE AND ±{4:F0} ON ISSUE MATCH. BUY A BIGGER "
                + "SAMPLE AND THE RANGE NARROWS; THE MARGIN IS MEASURED, NOT INVENTED.{5}",
                p.PollHouse.ToUpperInvariant(), p.SampleSize,
                p.FieldDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant(),
                p.SalienceError * 100.0, p.MatchError * 100.0,
                p.RegionalDetailBought
                    ? " REGIONAL DETAIL IS BOUGHT, SO THIS IS THE REGION'S OWN READING."
                    : " REGIONAL DETAIL IS NOT BOUGHT, SO THIS IS THE NATIONAL READING.");
            float methodHeight = Mathf.Ceil(method.CalcHeight(new GUIContent(methodText), r.width));
            var methodRect = new Rect(r.x, y, r.width, methodHeight);
            if (Event.current.type == EventType.Repaint)
            {
                UiContainmentGuard.Check("Action estimate provenance", methodRect, r);
            }

            GUI.Label(methodRect, methodText, method);
        }

        /// <summary>
        /// One estimate. A genuine span is drawn as a BAND — the low and high figures at the head, the
        /// span shaded on a track, the mid a hairline tick inside it — so the eye takes the interval
        /// first and the point second. That ordering is the item's requirement, not a style choice: a
        /// bold mid with a small ± beside it is exactly the false precision W-E3 forbids.
        ///
        /// ⚠ **A zero-width span is printed as a POINT, with the reason it is a point.** The first
        /// W-E3 film rendered enthusiasm as `3,477 — 3,477`, which is false precision wearing the
        /// costume of honesty: it claims to be a range and is not. The model finding behind it is
        /// real and is NOT papered over — §42 derives enthusiasm from exposure and credibility, and
        /// neither is polled, so no polling error can reach it. If enthusiasm should depend on what
        /// the electorate cares about, that is a change to the MODEL with its own reason, never a
        /// width invented at the drawing layer to make the screen look consistent.
        /// </summary>
        private float DrawEstimateBand(Rect r, float y, float ux, float uy, string label,
            double low, double mid, double high, string insensitiveNote)
        {
            GUIStyle caption = DeskCaption(9f, PoliSimTheme.TextSecondary);
            float captionHeight = Mathf.Ceil(DeskCaptionHeight(caption));
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, captionHeight), label, caption);
            y += captionHeight + Mathf.Round(4f * uy);

            bool isPoint = high - low < 1e-9;

            GUIStyle figure = DeskNumeral(20f, PoliSimTheme.TextPrimary, TextAnchor.LowerLeft);
            string figureText = isPoint
                ? Invariant("{0:N0}", mid)
                : Invariant("{0:N0} — {1:N0}", low, high);
            Vector2 figureSize = figure.CalcSize(new GUIContent(figureText));
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, figureSize.x + 4f, figureSize.y), figureText, figure);
            y += figureSize.y + Mathf.Round(5f * uy);

            if (isPoint)
            {
                GUIStyle why = DeskCaptionWrapped(8.5f, PoliSimTheme.TextMuted);
                float whyHeight = Mathf.Ceil(why.CalcHeight(new GUIContent(insensitiveNote), r.width));
                var whyRect = new Rect(r.x, y, r.width, whyHeight);
                if (Event.current.type == EventType.Repaint)
                {
                    UiContainmentGuard.Check("Action estimate point reason", whyRect, r);
                }

                GUI.Label(whyRect, insensitiveNote, why);
                return y + whyHeight;
            }

            // The track spans zero to the high end, so the band's WIDTH reads as a fraction of what
            // the action could do at best — a span filling most of the track is a reading the player
            // should not act on confidently.
            var track = new Rect(r.x, y, r.width, Mathf.Round(10f * uy));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.RoundedCard(track, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f);

                double scale = high > 0 ? high : 1.0;
                float lo = Mathf.Clamp01((float)(low / scale));
                float hi = Mathf.Clamp01((float)(high / scale));
                PoliSimTheme.RoundedBox(new Rect(track.x + track.width * lo, track.y,
                        Mathf.Max(1f, track.width * (hi - lo)), track.height),
                    UiPalette.GetAreaColor(UiPalette.SystemArea.Political), 0f);

                float m = Mathf.Clamp01((float)(mid / scale));
                PoliSimTheme.Rule(new Rect(track.x + track.width * m - 1f, track.y, 2f, track.height),
                    PoliSimTheme.TextPrimary);
            }

            return y + track.height;
        }

        /// <summary>
        /// Formats invariantly. Every numeric string on these screens goes through here or through
        /// an explicit `string.Format(CultureInfo.InvariantCulture, ...)`: this machine's sv-SE
        /// culture renders `{0:N0}` of 94613 as `94 613` with a non-breaking space, which is B3's
        /// recorded defect class and shipped once on the first W-E3 film.
        /// </summary>
        private static string Invariant(string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }
    }
}
