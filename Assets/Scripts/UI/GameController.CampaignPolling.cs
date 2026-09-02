using System.Globalization;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// W-E4 — **the polling screen**, the third of the Track E class. Same board, same idiom and the
    /// same primitives as Campaign HQ and the action screen.
    ///
    /// ⚠ **HARNESS ONLY — R-N2 holds until W-G1**, by the same mechanism as its two siblings.
    ///
    /// **§21 is one sentence and this screen exists to serve it:** *"The player should have to decide
    /// whether additional information is worth the cost."* A decision needs both sides priced, so
    /// the right column puts kronor against **percentage points of precision** — and every ± on it is
    /// DERIVED from the offer's sample size by `PollingSystem.MarginOfErrorPp`, the same function a
    /// conducted poll reports with. A price list cannot promise an accuracy the polls then fail to
    /// deliver, because it is not making a promise: it is quoting the same arithmetic.
    ///
    /// **What is deliberately NOT reduced to one number.** Regional breakdown, demographic
    /// segmentation and turnout modelling are *capabilities*, not accuracy, and the cost-per-point
    /// figure ignores them by design. Averaging a capability into a precision score would hide the
    /// trade rather than show it — the player is choosing between a narrower number and a different
    /// KIND of answer, and those are not commensurable.
    ///
    /// **The screen also states what money cannot buy.** §20 names late swings, turnout, undecided
    /// voters and tactical voting as reasons a poll will differ from the result; the margin of error
    /// describes SAMPLING error and nothing else (W-B10). A screen that sold precision without
    /// saying so would be selling a false promise.
    /// </summary>
    public partial class GameController
    {
        private PollingScreenSnapshot? _campaignPollingScreen;
        private Rect _campaignPollingInnerRect;

        internal void SetCampaignPollingScreen(PollingScreenSnapshot? snapshot)
        {
            _campaignPollingScreen = snapshot;
        }

        private void DrawCampaignPollingStage(float availableHeight, float availableWidth, PollingScreenSnapshot snapshot)
        {
            float innerWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle);

            GUILayout.BeginVertical(_boxStyle, GUILayout.Width(availableWidth),
                GUILayout.ExpandHeight(true));   // P2-1.1: the sheet fills the frame-height column
            Rect inner = GUILayoutUtility.GetRect(innerWidth, availableHeight,
                GUILayout.Width(innerWidth), GUILayout.Height(availableHeight));
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint) { _campaignPollingInnerRect = inner; }
            else if (_campaignPollingInnerRect.width > 1f) { inner = _campaignPollingInnerRect; }

            float ux = inner.width / DeskBoardInnerWidth;
            float uy = inner.height / DeskBoardInnerHeight;
            Rect Board(float x, float y, float w, float h) =>
                new Rect(inner.x + x * ux, inner.y + y * uy, w * ux, h * uy);

            DrawCampaignMasthead(Board(0f, 0f, 1156f, 28f), snapshot.Campaign, "CAMPAIGN · POLLING");

            DrawPollingLastPoll(Board(0f, 36f, 440f, 576f), snapshot);
            DrawPollingMomentum(Board(453f, 36f, 250f, 576f), snapshot);
            DrawPollingOffers(Board(716f, 36f, 440f, 576f), snapshot);

            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(Board(0f, 620f, 1156f, 1f), PoliSimTheme.HairlineStrong);
            }

            DrawCampaignStrip(Board(0f, 627f, 1156f, 53f), snapshot.Campaign);
        }

        // ------------------------------------------------------------------------------------------
        // Left: what you last bought, and what it can and cannot tell you.
        // ------------------------------------------------------------------------------------------
        private void DrawPollingLastPoll(Rect r, PollingScreenSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "YOUR LAST POLL", ux, uy);

            Poll poll = s.Campaign.LatestPoll;
            GUIStyle nameStyle = DeskBody(13f, PoliSimTheme.TextPrimary);
            GUIStyle mutedName = DeskBody(13f, PoliSimTheme.TextSecondary);
            GUIStyle figureStyle = DeskCaption(11f, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleRight);
            float rowHeight = Mathf.Max(Mathf.Round(19f * uy), nameStyle.CalcSize(new GUIContent("Ag")).y);

            int parties = Mathf.Min(s.Campaign.PartyNames.Length, poll.PartyCount);
            for (int i = 0; i < parties; i++)
            {
                bool isPlayer = i == s.Campaign.PlayerPartyIndex;
                DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), s.Campaign.PartyNames[i],
                    Invariant("{0:F1} % ± {1:F1}", poll.Share(i) * 100.0, poll.MarginOfErrorPp(i)),
                    isPlayer ? nameStyle : mutedName, figureStyle);
                y += rowHeight;
            }

            y += Mathf.Round(8f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }

            y += Mathf.Round(8f * uy);

            // §20's own list of what a poll will differ from the result BY. This is the honest
            // counterweight to a price list that sells precision: the margin describes sampling
            // error and nothing else (W-B10), and no sample size buys away any of these.
            GUIStyle note = DeskCaptionWrapped(8.5f, PoliSimTheme.TextMuted);
            string noteText = Invariant(
                "{0} · n = {1:N0} · FIELDED {2}. THE ± IS SAMPLING ERROR AND NOTHING ELSE. THE OTHER "
                + "SOURCES OF DIFFERENCE — LATE SWINGS, TURNOUT, UNDECIDED VOTERS, TACTICAL VOTING, AND "
                + "EACH HOUSE'S OWN LEAN — ARE NOT IN IT, AND NO SAMPLE SIZE BUYS THEM AWAY.",
                poll.House.ToUpperInvariant(), poll.SampleSize,
                poll.FieldDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant());
            float noteHeight = Mathf.Ceil(note.CalcHeight(new GUIContent(noteText), r.width));
            var noteRect = new Rect(r.x, y, r.width, noteHeight);
            if (Event.current.type == EventType.Repaint)
            {
                UiContainmentGuard.Check("Polling last-poll note", noteRect, r);
            }

            GUI.Label(noteRect, noteText, note);
        }

        // ------------------------------------------------------------------------------------------
        // Centre: §22's momentum — a decaying stock, not a permanent gain.
        // ------------------------------------------------------------------------------------------
        private void DrawPollingMomentum(Rect r, PollingScreenSnapshot s)
        {
            float ux = r.width / 250f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "MOMENTUM", ux, uy);

            GUIStyle nameStyle = DeskBody(12f, PoliSimTheme.TextPrimary);
            float rowHeight = Mathf.Max(Mathf.Round(19f * uy), nameStyle.CalcSize(new GUIContent("Ag")).y);

            double[] momentum = s.Campaign.MomentumPp;
            bool any = false;
            int parties = Mathf.Min(s.Campaign.PartyNames.Length, momentum?.Length ?? 0);

            for (int i = 0; i < parties; i++)
            {
                if (Mathf.Abs((float)momentum[i]) < 0.05f) { continue; }

                any = true;
                GUIStyle figure = DeskCaption(11f,
                    UiPalette.GetDeltaColor((float)momentum[i], higherIsBetter: true), false, TextAnchor.MiddleRight);
                DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), s.Campaign.PartyNames[i],
                    momentum[i].ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " pp",
                    nameStyle, figure);
                y += rowHeight;
            }

            if (!any)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight), "NO MOMENTUM IN PLAY", nameStyle);
                y += rowHeight;
            }

            y += Mathf.Round(10f * uy);

            // §22's shape, stated with the half-life the code actually uses — and with the spec's own
            // inconsistency named rather than hidden, because a screen quoting "the spec's decay"
            // would be quoting three mutually contradictory numbers (see MomentumHalfLifeDays).
            GUIStyle note = DeskCaptionWrapped(8.5f, PoliSimTheme.TextMuted);
            string noteText = Invariant(
                "MOMENTUM DECAYS ON A {0:F0}-DAY HALF-LIFE AND CANNOT BE MADE PERMANENT — A LASTING GAIN "
                + "IS A REPUTATION CHANGE, WHICH IS A DIFFERENT THING. IT SHIFTS WHERE THE RACE "
                + "APPEARS TO BE WITHOUT TOUCHING THE PREFERENCE UNDERNEATH, WHICH IS WHY A POLL CAN "
                + "MOVE BEFORE ANYTHING REAL HAS.", PollingSystem.MomentumHalfLifeDays);
            float noteHeight = Mathf.Ceil(note.CalcHeight(new GUIContent(noteText), r.width));
            var noteRect = new Rect(r.x, y, r.width, noteHeight);
            if (Event.current.type == EventType.Repaint)
            {
                UiContainmentGuard.Check("Polling momentum note", noteRect, r);
            }

            GUI.Label(noteRect, noteText, note);
        }

        // ------------------------------------------------------------------------------------------
        // Right: §21's decision, with both sides of it priced.
        // ------------------------------------------------------------------------------------------
        private void DrawPollingOffers(Rect r, PollingScreenSnapshot s)
        {
            float ux = r.width / 440f;
            float uy = r.height / 576f;
            float y = DrawCampaignLedgerHead(r, "WHAT BETTER INFORMATION COSTS", ux, uy);

            if (s.Offers == null || s.Offers.Length == 0)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, Mathf.Round(20f * uy)),
                    "NO POLLING IS ON OFFER", DeskBody(13f, PoliSimTheme.TextPrimary));
                return;
            }

            GUIStyle capStyle = DeskCaption(8.5f, PoliSimTheme.TextMuted);
            // ⚠ The head line carries a BODY name and a MONO figure, and the two faces do not share
            // a line box: at 1920 the 11 px mono caption measures 21.2 px tall against the 13 px
            // body's 20.1, and the guard caught "120,000 kr · ± 2.6" overflowing by 1.1 px. The row
            // takes the TALLER of the two, not the name's. Third instance of one class this session
            // (W-E1's momentum caption, W-E3's, this) — a rect sized from one style and then used
            // for a label in another is the shape to watch for.
            float nameHeight = Mathf.Max(
                DeskBody(13f, PoliSimTheme.TextPrimary).CalcSize(new GUIContent("Ag")).y,
                Mathf.Ceil(DeskCaptionHeight(DeskCaption(11f, PoliSimTheme.TextPrimary))));
            float capHeight = Mathf.Ceil(DeskCaptionHeight(capStyle));
            // TWO caption lines, not one: at 1280 the full programme's "n = 6,000 · REGIONAL ·
            // DEMOGRAPHIC · TURNOUT MODEL · 228,048 kr PER POINT OF PRECISION GAINED" measured 446 px
            // in a 425 px slot and the overflow guard caught it. Splitting the line keeps every word:
            // what the offer IS and what it COSTS PER POINT are two facts, and abbreviating either
            // to fit would have traded the item's own subject - a priceable decision - for a tidy row.
            float rowHeight = nameHeight + capHeight * 2f + Mathf.Round(14f * uy);
            float pad = Mathf.Round(6f * ux);

            // The cheapest offer is the baseline every gain is measured against, because "is more
            // information worth it" is a question about the MARGIN over what you would otherwise buy.
            PollOffer baseline = s.Offers[0];
            for (int i = 1; i < s.Offers.Length; i++)
            {
                if (s.Offers[i].Cost < baseline.Cost) { baseline = s.Offers[i]; }
            }

            for (int i = 0; i < s.Offers.Length; i++)
            {
                PollOffer offer = s.Offers[i];
                bool selected = i == s.SelectedIndex;
                var row = new Rect(r.x, y, r.width, rowHeight - Mathf.Round(3f * uy));

                if (Event.current.type == EventType.Repaint && selected)
                {
                    PoliSimTheme.RoundedCard(row, PoliSimTheme.Tile, PoliSimTheme.HairlineStrong, 0f);
                }

                Color ink = !offer.Affordable
                    ? PoliSimTheme.TextMuted
                    : selected ? PoliSimTheme.TextPrimary : PoliSimTheme.TextSecondary;

                double moe = offer.MarginOfErrorPp(s.QuotedShare);
                string headRight = Invariant("{0} kr · ± {1:F1}", Kronor(offer.Cost), moe);
                float headRightWidth = Mathf.Ceil(
                    DeskCaption(11f, ink, false, TextAnchor.MiddleRight).CalcSize(new GUIContent(headRight)).x) + pad;

                PoliSimWidgets.MeasuredLabel(
                    new Rect(row.x + pad, row.y + Mathf.Round(2f * uy),
                        Mathf.Max(1f, row.width - headRightWidth - pad * 2f), nameHeight),
                    offer.Name, DeskBody(13f, ink));
                PoliSimWidgets.MeasuredLabel(
                    new Rect(row.xMax - headRightWidth - pad, row.y + Mathf.Round(2f * uy), headRightWidth, nameHeight),
                    headRight, DeskCaption(11f, ink, false, TextAnchor.MiddleRight));

                // What the offer buys BESIDES precision, named rather than scored: these are
                // different kinds of answer, not more accurate ones.
                string extras = Extras(offer);
                double perPoint = offer.CostPerPointGained(baseline, s.QuotedShare);
                string value = ReferenceEquals(offer.Name, baseline.Name) || offer.Cost <= baseline.Cost
                    ? "THE BASELINE"
                    : double.IsPositiveInfinity(perPoint)
                        ? "NO PRECISION GAINED OVER THE BASELINE"
                        : Invariant("{0} kr PER POINT OF PRECISION GAINED", Kronor(perPoint));

                // Line 1: what the offer IS. Line 2: what the extra precision COSTS. Two facts, two
                // lines - see rowHeight's note for why this is not one line abbreviated to fit.
                PoliSimWidgets.MeasuredLabel(
                    new Rect(row.x + pad, row.y + Mathf.Round(2f * uy) + nameHeight,
                        Mathf.Max(1f, row.width - pad * 2f), capHeight),
                    Invariant("n = {0:N0} · {1}", offer.SampleSize, extras), capStyle);
                PoliSimWidgets.MeasuredLabel(
                    new Rect(row.x + pad, row.y + Mathf.Round(2f * uy) + nameHeight + capHeight,
                        Mathf.Max(1f, row.width - pad * 2f), capHeight),
                    value, capStyle);

                y += rowHeight;
            }

            y += Mathf.Round(6f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }

            y += Mathf.Round(7f * uy);

            GUIStyle note = DeskCaptionWrapped(8.5f, PoliSimTheme.TextMuted);
            string noteText = Invariant(
                "± QUOTED AT {0}'s {1:F1} % — A POLL'S MARGIN DEPENDS ON THE SHARE MEASURED, SO IT IS "
                + "NOT ONE NUMBER FOR THE WHOLE POLL. PRECISION IMPROVES WITH THE SQUARE ROOT OF THE "
                + "SAMPLE, SO EACH POINT COSTS MORE THAN THE LAST. REGIONAL, DEMOGRAPHIC AND TURNOUT "
                + "DEPTH ARE DIFFERENT KINDS OF ANSWER, NOT NARROWER ONES, AND ARE DELIBERATELY NOT "
                + "PRICED PER POINT. SAMPLE SIZES AND PRICES ARE ILLUSTRATIVE; THE ± FIGURES "
                + "FOLLOW FROM THE SAMPLE SIZES.", s.QuotedPartyName, s.QuotedShare * 100.0);
            float noteHeight = Mathf.Ceil(note.CalcHeight(new GUIContent(noteText), r.width));
            var noteRect = new Rect(r.x, y, r.width, noteHeight);
            if (Event.current.type == EventType.Repaint)
            {
                UiContainmentGuard.Check("Polling offers note", noteRect, r);
            }

            GUI.Label(noteRect, noteText, note);
        }

        /// <summary>§21's three depth capabilities, named in the order the spec lists them.</summary>
        private static string Extras(PollOffer offer)
        {
            var parts = new System.Collections.Generic.List<string>(3);
            if (offer.RegionalBreakdown) { parts.Add("REGIONAL"); }
            if (offer.DemographicSegmentation) { parts.Add("DEMOGRAPHIC"); }
            if (offer.TurnoutModelling) { parts.Add("TURNOUT MODEL"); }

            return parts.Count == 0 ? "HEADLINE ONLY" : string.Join(" · ", parts.ToArray());
        }
    }
}
