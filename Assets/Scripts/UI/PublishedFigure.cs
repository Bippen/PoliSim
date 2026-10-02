using PoliSim.Data;

namespace PoliSim.UI
{
    /// <summary>
    /// A published statistic as a BULLETIN rather than a chart: status, value, reference period,
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
    /// this date*. A stat block, not a trend.
    ///
    /// <para><b>Channel 2 is present but cannot vary here.</b> The same measurement found that FIVE OF
    /// SIX published stats are single-estimate - published once and final immediately, with no revision
    /// stage at all. GDP is the only series that is ever preliminary. So the status these figures carry is
    /// almost always FINAL, which is correct and carries little information, and that is a fact about the
    /// simulation rather than a gap in the rendering.</para>
    ///
    /// <para>§738 (UI v3.5): the bulletin is a SLIP LINE - the Crime tab's block, its one drawer, folded into the crime reading's slip when the tab
    /// became tiles (rule 1: almost no sub-text at rest). The badge's status is the line's last word.</para>
    /// </summary>
    public static class PublishedFigure
    {
        /// <summary>The bulletin as one slip line: *LABEL 42.1 · FOR JAN 2025 - DEC 2025 · RELEASED 15 MAR 2026 · FINAL*; a series with no release yet says so.</summary>
        public static string Line(string label, PublishedSeries series, int decimals = 1)
        {
            PublishedEntry latest = series?.Latest();
            if (latest == null)
            {
                return label.ToUpperInvariant() + " · NOT YET PUBLISHED - THE FIRST RELEASE IS STILL AHEAD";
            }
            string status = latest.Status == RevisionStatus.Preliminary ? "PRELIMINARY" : latest.Status.ToString().ToUpperInvariant();
            return (label + " " + UiFormat.Number(latest.Value, decimals)
                + $" · for {latest.ReferencePeriodStart:MMM yyyy} - {latest.ReferencePeriodEnd:MMM yyyy} · released {latest.PublicationDate:d MMM yyyy} · ").ToUpperInvariant() + status;
        }
    }
}
