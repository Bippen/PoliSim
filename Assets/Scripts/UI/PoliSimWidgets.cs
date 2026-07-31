using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// The six reusable widgets the R2 GUI is assembled from — stat tile, threshold bar, legislative
    /// support bar, standing/draft pair, decision-card chrome and the geometric portrait. Every one
    /// is drawn from <see cref="PoliSimTheme"/> primitives (rounded rects, lines, arcs), so a screen
    /// is a layout of these calls rather than a hand-rolled block of GUILayout per tab.
    /// All sizes are unscaled px; pass <paramref name="scale"/> from GameController's existing UI scale.
    /// </summary>
    public static class PoliSimWidgets
    {
        // --- Styles -----------------------------------------------------------------------

        private static GUIStyle _label, _value, _body, _mono;
        private static float _builtAtScale = -1f;

        private static void EnsureStyles(float scale)
        {
            if (Mathf.Approximately(_builtAtScale, scale) && _label != null)
            {
                return;
            }

            _builtAtScale = scale;

            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(PoliSimTheme.FontLabel * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 0, 0)
            };
            _label.normal.textColor = PoliSimTheme.TextMuted;

            _value = new GUIStyle(_label)
            {
                fontSize = Mathf.RoundToInt(PoliSimTheme.FontStatHero * scale),
                fontStyle = FontStyle.Bold
            };
            _value.normal.textColor = PoliSimTheme.TextPrimary;

            _body = new GUIStyle(_label)
            {
                fontSize = Mathf.RoundToInt(PoliSimTheme.FontBodySmall * scale),
                fontStyle = FontStyle.Normal
            };
            _body.normal.textColor = PoliSimTheme.TextSecondary;

            _mono = new GUIStyle(_body)
            {
                fontSize = Mathf.RoundToInt(PoliSimTheme.FontMicro * scale + 2f)
            };
            _mono.normal.textColor = PoliSimTheme.TextMuted;
        }

        private static GUIStyle Sized(GUIStyle basis, int unscaledSize, Color color, float scale, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var s = new GUIStyle(basis)
            {
                fontSize = Mathf.RoundToInt(unscaledSize * scale),
                alignment = anchor
            };
            s.normal.textColor = color;
            return s;
        }

        // --- 1. Stat tile -----------------------------------------------------------------

        /// <summary>
        /// Headline figure with a small caps label, an optional signed delta pill and an optional
        /// capacity bar with a threshold tick. <paramref name="barFraction"/> below zero hides the bar.
        /// </summary>
        public static void StatTile(
            Rect rect,
            string label,
            string value,
            string suffix,
            string delta,
            bool deltaIsGood,
            string subLabel,
            UiPalette.SystemArea area,
            float scale,
            float barFraction = -1f,
            float thresholdFraction = -1f)
        {
            EnsureStyles(scale);

            PoliSimTheme.RoundedCard(rect, PoliSimTheme.Card, PoliSimTheme.Hairline, PoliSimTheme.RadiusCard * scale);
            PoliSimTheme.TopAccent(rect, area, 2f * scale);

            float padX = 17f * scale;
            float padY = 16f * scale;
            float x = rect.x + padX;
            float y = rect.y + padY;
            float innerWidth = rect.width - padX * 2f;

            GUI.Label(new Rect(x, y, innerWidth, 12f * scale), label.ToUpperInvariant(),
                Sized(_label, PoliSimTheme.FontLabel, PoliSimTheme.TextMuted, scale));
            y += 20f * scale;

            var valueStyle = Sized(_value, PoliSimTheme.FontStatHero, PoliSimTheme.TextPrimary, scale, TextAnchor.LowerLeft);
            float valueHeight = PoliSimTheme.FontStatHero * scale * 1.05f;
            Vector2 valueSize = valueStyle.CalcSize(new GUIContent(value));
            GUI.Label(new Rect(x, y, innerWidth, valueHeight), value, valueStyle);

            if (!string.IsNullOrEmpty(suffix))
            {
                GUI.Label(new Rect(x + valueSize.x + 5f * scale, y, innerWidth, valueHeight), suffix,
                    Sized(_body, PoliSimTheme.FontBodySmall, PoliSimTheme.Neutral, scale, TextAnchor.LowerLeft));
            }

            y += valueHeight + 9f * scale;

            if (!string.IsNullOrEmpty(delta))
            {
                Color tone = deltaIsGood ? PoliSimTheme.Good : PoliSimTheme.Bad;
                var deltaStyle = Sized(_mono, PoliSimTheme.FontBodySmall - 1, tone, scale, TextAnchor.MiddleCenter);
                float w = deltaStyle.CalcSize(new GUIContent(delta)).x + 14f * scale;
                var pill = new Rect(x, y, w, 18f * scale);
                PoliSimTheme.RoundedBox(pill, PoliSimTheme.Tint(tone, 0.14f), 7f * scale);
                GUI.Label(pill, delta, deltaStyle);

                if (!string.IsNullOrEmpty(subLabel))
                {
                    GUI.Label(new Rect(pill.xMax + 7f * scale, y, innerWidth, 18f * scale), subLabel,
                        Sized(_body, PoliSimTheme.FontBodySmall - 1, PoliSimTheme.TextMuted, scale));
                }

                y += 26f * scale;
            }

            if (barFraction >= 0f)
            {
                var bar = new Rect(x, y, innerWidth, PoliSimTheme.BarHeightSm * scale);
                ThresholdBar(bar, barFraction, thresholdFraction, PoliSimTheme.Accent(area));
            }
        }

        // --- 2. Threshold bar --------------------------------------------------------------

        /// <summary>
        /// Rounded track + fill + a white tick at the threshold. This is the game-wide convention
        /// for "value against a target": comfortable debt level, approval floor, capacity limits.
        /// Pass a negative threshold to draw a plain capacity bar.
        /// </summary>
        public static void ThresholdBar(Rect rect, float fraction, float thresholdFraction, Color fill)
        {
            float radius = rect.height * 0.5f;
            PoliSimTheme.RoundedBox(rect, PoliSimTheme.BarTrack, radius);

            fraction = Mathf.Clamp01(fraction);
            if (fraction > 0f)
            {
                float w = Mathf.Max(rect.height, rect.width * fraction);
                PoliSimTheme.RoundedBox(new Rect(rect.x, rect.y, w, rect.height), fill, radius);
            }

            if (thresholdFraction >= 0f)
            {
                float markerX = rect.x + rect.width * Mathf.Clamp01(thresholdFraction);
                PoliSimTheme.Rule(new Rect(markerX - 1f, rect.y - 2f, 2f, rect.height + 4f), PoliSimTheme.ThresholdMarker);
            }
        }

        // --- 3. Legislative support bar ----------------------------------------------------

        /// <summary>
        /// Seats-for versus the majority line, with the status word derived from the margin — the
        /// one control used by the budget summary, every standalone bill and any pending vote.
        /// </summary>
        public static void SupportBar(Rect rect, int seatsFor, int totalSeats, int majority, float scale, bool showScale = true)
        {
            EnsureStyles(scale);

            int margin = seatsFor - majority;
            Color tone = margin >= 5 ? PoliSimTheme.Good : margin >= -8 ? PoliSimTheme.Caution : PoliSimTheme.Bad;
            string status = margin >= 5 ? "PASSING" : margin >= 0 ? "RAZOR THIN" : Mathf.Abs(margin) + " SHORT";
            float percent = totalSeats > 0 ? seatsFor / (float)totalSeats : 0f;

            float y = rect.y;
            GUI.Label(new Rect(rect.x, y, rect.width, 14f * scale), "LEGISLATIVE SUPPORT",
                Sized(_label, PoliSimTheme.FontLabel, PoliSimTheme.TextMuted, scale));

            var statusStyle = Sized(_label, PoliSimTheme.FontLabel + 1, tone, scale, TextAnchor.MiddleCenter);
            float statusWidth = statusStyle.CalcSize(new GUIContent(status)).x + 18f * scale;
            var statusPill = new Rect(rect.xMax - statusWidth, y - 2f * scale, statusWidth, 18f * scale);
            PoliSimTheme.RoundedBox(statusPill, PoliSimTheme.Tint(tone, 0.14f), 9f * scale);
            GUI.Label(statusPill, status, statusStyle);

            y += 22f * scale;
            GUI.Label(new Rect(rect.x, y, rect.width, 36f * scale), Mathf.RoundToInt(percent * 100f) + "%",
                Sized(_value, PoliSimTheme.FontStatLarge, tone, scale, TextAnchor.LowerLeft));
            GUI.Label(new Rect(rect.x, y, rect.width, 36f * scale), seatsFor + " / " + majority + " SEATS",
                Sized(_mono, PoliSimTheme.FontBodySmall, PoliSimTheme.Neutral, scale, TextAnchor.LowerRight));

            y += 44f * scale;
            ThresholdBar(new Rect(rect.x, y, rect.width, PoliSimTheme.BarHeightLg * scale), percent,
                totalSeats > 0 ? majority / (float)totalSeats : 0.5f, tone);

            if (!showScale)
            {
                return;
            }

            y += 18f * scale;
            var scaleStyle = Sized(_mono, PoliSimTheme.FontMicro, PoliSimTheme.TextMuted, scale);
            GUI.Label(new Rect(rect.x, y, rect.width, 14f * scale), "0", scaleStyle);
            GUI.Label(new Rect(rect.x, y, rect.width, 14f * scale), "MAJORITY " + majority,
                Sized(_mono, PoliSimTheme.FontMicro, PoliSimTheme.TextMuted, scale, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(rect.x, y, rect.width, 14f * scale), totalSeats.ToString(),
                Sized(_mono, PoliSimTheme.FontMicro, PoliSimTheme.TextMuted, scale, TextAnchor.MiddleRight));
        }

        // --- 4. Standing / draft pair -------------------------------------------------------

        /// <summary>
        /// The budget line-item readout: the enacted value, an arrow, the draft value, and a signed
        /// delta pill when they differ. Draft text turns amber the moment it diverges from standing,
        /// which is the only cue that a change is pending a vote — so it is never optional.
        /// </summary>
        public static void StandingDraftPair(Rect rect, string standingText, string draftText, float delta, string unitSuffix, float scale)
        {
            EnsureStyles(scale);

            bool changed = Mathf.Abs(delta) > 0.0001f;
            Color draftColor = changed ? PoliSimTheme.Draft : PoliSimTheme.TextPrimary;

            float half = rect.height * 0.5f;
            GUI.Label(new Rect(rect.x, rect.y, 110f * scale, half), "STANDING",
                Sized(_label, PoliSimTheme.FontLabel, PoliSimTheme.TextMuted, scale, TextAnchor.LowerRight));
            GUI.Label(new Rect(rect.x, rect.y + half, 110f * scale, half), standingText,
                Sized(_mono, PoliSimTheme.FontSubtitle, PoliSimTheme.Neutral, scale, TextAnchor.UpperRight));

            float arrowX = rect.x + 120f * scale;
            PoliSimTheme.Rule(new Rect(arrowX, rect.y + half - 1f, 12f * scale, 2f), PoliSimTheme.Tint(PoliSimTheme.Neutral, 0.6f));

            float draftX = arrowX + 22f * scale;
            GUI.Label(new Rect(draftX, rect.y, 120f * scale, half), "DRAFT",
                Sized(_label, PoliSimTheme.FontLabel, changed ? PoliSimTheme.Draft : PoliSimTheme.TextMuted, scale, TextAnchor.LowerRight));
            GUI.Label(new Rect(draftX, rect.y + half, 120f * scale, half), draftText,
                Sized(_mono, PoliSimTheme.FontTitle, draftColor, scale, TextAnchor.UpperRight));

            if (!changed)
            {
                return;
            }

            string deltaText = (delta > 0f ? "+" : "−") + Mathf.Abs(delta).ToString("0.0") + unitSuffix;
            var deltaStyle = Sized(_mono, PoliSimTheme.FontBodySmall - 2, PoliSimTheme.Draft, scale, TextAnchor.MiddleCenter);
            float w = deltaStyle.CalcSize(new GUIContent(deltaText)).x + 16f * scale;
            var pill = new Rect(draftX + 130f * scale, rect.y + half - 9f * scale, w, 18f * scale);
            PoliSimTheme.RoundedBox(pill, PoliSimTheme.Tint(PoliSimTheme.Draft, 0.14f), 9f * scale);
            GUI.Label(pill, deltaText, deltaStyle);
        }

        /// <summary>Draft track: the standing value as a grey underlay behind the coloured draft fill.</summary>
        public static void DraftTrack(Rect rect, float standingFraction, float draftFraction, UiPalette.SystemArea area)
        {
            float radius = rect.height * 0.5f;
            PoliSimTheme.RoundedBox(rect, PoliSimTheme.BarTrack, radius);
            PoliSimTheme.RoundedBox(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(standingFraction), rect.height),
                new Color(1f, 1f, 1f, 0.16f), radius);
            PoliSimTheme.RoundedBox(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(draftFraction), rect.height),
                PoliSimTheme.Tint(PoliSimTheme.Accent(area), 0.9f), radius);
        }

        // --- 5. Decision card chrome ---------------------------------------------------------

        public enum Urgency
        {
            Scheduled,
            Soon,
            Urgent
        }

        /// <summary>
        /// Draws the card shell (fill, urgency-tinted border, category spine, corner wash) and the
        /// badge row, and returns the content rect the caller fills with title/body/facts/actions.
        /// </summary>
        public static Rect DecisionCard(Rect rect, string kind, string urgencyText, Urgency urgency, string deadline, UiPalette.SystemArea area, float scale)
        {
            EnsureStyles(scale);

            Color urgencyColor = urgency == Urgency.Urgent ? PoliSimTheme.Bad
                : urgency == Urgency.Soon ? PoliSimTheme.Caution
                : PoliSimTheme.Accent(area);

            Color border = urgency == Urgency.Scheduled ? PoliSimTheme.Hairline : PoliSimTheme.Tint(urgencyColor, 0.32f);
            PoliSimTheme.RoundedCard(rect, PoliSimTheme.Card, border, PoliSimTheme.RadiusPanel * scale);
            PoliSimTheme.LeftSpine(rect, area, 4f * scale);

            float padX = 22f * scale;
            float padY = 20f * scale;
            var badgeRow = new Rect(rect.x + padX, rect.y + padY, rect.width - padX * 2f, 20f * scale);

            float x = badgeRow.x;
            x += Badge(new Rect(x, badgeRow.y, 0f, badgeRow.height), kind, PoliSimTheme.Accent(area), scale) + 8f * scale;
            Badge(new Rect(x, badgeRow.y, 0f, badgeRow.height), urgencyText, urgencyColor, scale);

            GUI.Label(badgeRow, deadline, Sized(_mono, PoliSimTheme.FontMicro + 1, PoliSimTheme.TextMuted, scale, TextAnchor.MiddleRight));

            return new Rect(rect.x + padX, badgeRow.yMax + 12f * scale, rect.width - padX * 2f, rect.yMax - badgeRow.yMax - padY - 12f * scale);
        }

        /// <summary>Auto-width caps badge. Returns the width drawn so badges can be laid out in a row.</summary>
        public static float Badge(Rect rect, string text, Color tone, float scale)
        {
            EnsureStyles(scale);

            var style = Sized(_label, PoliSimTheme.FontLabel, tone, scale, TextAnchor.MiddleCenter);
            float w = style.CalcSize(new GUIContent(text)).x + 20f * scale;
            var box = new Rect(rect.x, rect.y, w, rect.height);
            PoliSimTheme.RoundedBox(box, PoliSimTheme.Tint(tone, 0.14f), 9f * scale);
            GUI.Label(box, text, style);
            return w;
        }

        // --- 6. Portrait placeholder ----------------------------------------------------------

        /// <summary>
        /// The abstract minister portrait: a hue ring, a dark disc, and a head circle plus a
        /// top-rounded shoulder rectangle in the portfolio hue. Three procedural shapes, no art
        /// asset — and the ring is the only thing that identifies the portfolio, so keep it.
        /// </summary>
        public static void Portrait(Rect rect, UiPalette.SystemArea area, float scale)
        {
            float size = Mathf.Min(rect.width, rect.height);
            var disc = new Rect(rect.x, rect.y, size, size);
            Color hue = PoliSimTheme.Accent(area);

            PoliSimTheme.RoundedBox(disc, hue, size * 0.5f);

            float ring = 2.5f * scale;
            var inner = new Rect(disc.x + ring, disc.y + ring, size - ring * 2f, size - ring * 2f);
            PoliSimTheme.RoundedBox(inner, PoliSimTheme.Hex(0x0E1218), inner.width * 0.5f);

            // Head and shoulders are drawn inside a GUI group so the disc clips them.
            GUI.BeginGroup(inner);
            float u = inner.width / 53f; // reference geometry is a 53px inner disc
            var head = new Rect(inner.width * 0.5f - 7f * u, 8.5f * u, 14f * u, 14f * u);
            PoliSimTheme.RoundedBox(head, hue, head.width * 0.5f);

            var shoulders = new Rect(inner.width * 0.5f - 14f * u, 27.5f * u, 28f * u, 26f * u);
            GUI.DrawTexture(shoulders, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, hue,
                Vector4.zero, new Vector4(14f * u, 14f * u, 0f, 0f));
            GUI.EndGroup();
        }
    }
}
