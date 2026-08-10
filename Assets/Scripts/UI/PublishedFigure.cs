using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// A published statistic rendered as a BULLETIN rather than a chart: badge, value, reference period,
    /// publication date. Behaviour 6's channel 1 alone.
    ///
    /// **Why some published stats get this and others get a graph.** `PublicationCadenceCheck` measured
    /// the real cadences over twelve simulated years: Inflation and Unemployment publish monthly (143
    /// releases), GDP quarterly (142), and PovertyRate, Population and CrimeIndex ANNUALLY - eleven
    /// releases in twelve years. Eleven points beside a daily live series does not read as a comparison;
    /// it reads as a broken graph. The comparison framing on Statistics ("compare against the live
    /// figures above") earns its place at monthly and quarterly cadence and stops earning it at annual.
    ///
    /// So an annual published figure is what it actually is: *this number, for this period, released on
    /// this date*. A stat block, not a trend - and a stat block sits in a paper ledger where a sparse
    /// chart would not.
    ///
    /// <para><b>Channel 2 is present but cannot vary here.</b> The same measurement found that FIVE OF
    /// SIX published stats are single-estimate - published once and final immediately, with no revision
    /// stage at all. GDP is the only series that is ever preliminary. So the frame these figures draw is
    /// always solid, which is correct and carries no information, and that is a fact about the
    /// simulation rather than a gap in the rendering.</para>
    /// </summary>
    public static class PublishedFigure
    {
        public static void Draw(string label, PublishedSeries series, GUIStyle labelStyle, MoneyUnit? moneyUnit, string valueFormat = "F1")
        {
            PublishedEntry latest = series?.Latest();
            if (latest == null)
            {
                GUILayout.Label($"{label}: not yet published - the first release is still ahead.", labelStyle);
                return;
            }

            bool preliminary = latest.Status == RevisionStatus.Preliminary;
            Color ink = preliminary ? PoliSimTheme.Caution : PoliSimTheme.TextSecondary;
            string status = preliminary ? "PRELIMINARY" : latest.Status.ToString().ToUpperInvariant();

            string value = moneyUnit.HasValue
                ? UiFormat.Money(latest.Value, moneyUnit.Value)
                : latest.Value.ToString(valueFormat, System.Globalization.CultureInfo.InvariantCulture);

            GUILayout.BeginHorizontal();

            // Channel 1, all three parts: the badge says what KIND of figure this is, the reference
            // period says what it MEASURES, and the publication date says when it became knowable. A
            // published figure without its period and date is just a number that happens to be old.
            var badgeRect = GUILayoutUtility.GetRect(
                PoliSimWidgets.MeasuredWidth(status, labelStyle, labelStyle.fontSize * 1.4f),
                labelStyle.fontSize + 6f,
                GUILayout.ExpandWidth(false));

            if (Event.current.type == EventType.Repaint)
            {
                Texture2D chip = IconLibrary.GetChrome("ui_chip_outline");
                Color previous = GUI.color;
                GUI.color = ink;
                if (chip != null)
                {
                    GUI.DrawTexture(badgeRect, chip, ScaleMode.StretchToFill);
                }
                GUI.color = previous;
            }

            LedgerRow.Cell(badgeRect, status, labelStyle, ink, TextAnchor.MiddleCenter);

            GUILayout.Space(labelStyle.fontSize * 0.5f);
            GUILayout.Label($"{label}: {value}", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Period and date on their own line, in the caption voice - they qualify the figure rather
            // than competing with it.
            Color previousText = labelStyle.normal.textColor;
            labelStyle.normal.textColor = PoliSimTheme.TextSecondary;
            GUILayout.Label(
                $"    for {latest.ReferencePeriodStart:MMM yyyy} - {latest.ReferencePeriodEnd:MMM yyyy}, released {latest.PublicationDate:d MMM yyyy}",
                labelStyle);
            labelStyle.normal.textColor = previousText;
        }
    }
}
