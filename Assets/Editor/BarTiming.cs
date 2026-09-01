using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **Wall-clock per stage of the bar, appended to a log the next run appends to.**
    ///
    /// <para>⚠ <b>THIS IS NOT A CHECK.</b> It never fails, never gates a bar, and R-N5 does not apply to
    /// it: it reports, it does not judge. A stage that throws is still timed and still recorded — the
    /// timing is a measurement of what the run DID, which is exactly the thing this project has twice
    /// been unable to establish after the fact.</para>
    ///
    /// <para><b>Why it exists (ruled 2026-09-01).</b> The review that proposed it measured that
    /// <b>no wall-clock duration for any bar, capture pass or trajectory suite exists anywhere in the
    /// record</b> — the records log capture counts and byte-identity to four decimals and not one figure
    /// for how long the run took. Every duration in the repo came from diagnosing a FAILURE, never from
    /// measuring a success. **Nothing about incrementality can be decided until the cost is a number**,
    /// which is why this was ranked ahead of every incrementality proposal rather than alongside them.</para>
    ///
    /// <para><b>The log is append-only and lives outside the tree</b> (<c>Logs/</c> is gitignored), so a
    /// run compares against every run before it without a document ever transcribing a figure. ⚠ **That is
    /// the claim convention working**: the cost of the bar is GENERATED, never written down.</para>
    ///
    /// <para>⚠ <b>InvariantCulture at every format site</b>, per the standing rule. This machine's locale
    /// renders a decimal point as a comma, which has already corrupted figures in this project's logs
    /// once; a TSV of durations is exactly where that would happen again.</para>
    /// </summary>
    internal static class BarTiming
    {
        /// <summary>⚠ Relative to the project root, and <c>Logs/</c> is gitignored ON PURPOSE — this
        /// accumulates locally and is never a committed artifact somebody could quote as current.</summary>
        private const string LogRelative = "Logs/bar_timing.tsv";

        private static readonly List<KeyValuePair<string, long>> Stages = new List<KeyValuePair<string, long>>();
        private static readonly Stopwatch Wall = new Stopwatch();
        private static string _group = string.Empty;

        /// <summary>Opens a run. Clears any previous stages so a second group in the same process does not
        /// inherit the first one's rows.</summary>
        public static void Begin(string group)
        {
            _group = group;
            Stages.Clear();
            Wall.Reset();
            Wall.Start();
        }

        /// <summary>
        /// Times one stage and returns whatever it returned. ⚠ **The stopwatch is stopped in a
        /// <c>finally</c>**, so a stage that THROWS is still recorded — an untimed stage would make the
        /// total unaccountable exactly when something went wrong.
        /// </summary>
        public static int Measure(string name, Func<int> body)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return body();
            }
            finally
            {
                sw.Stop();
                Stages.Add(new KeyValuePair<string, long>(name, sw.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// Closes a run: appends one row per stage plus a TOTAL row, and logs the slowest few so a reader
        /// of the run's own output sees where the time went without opening the file.
        /// </summary>
        public static void End(int exitCode)
        {
            Wall.Stop();

            // ⚠ A timing failure must never fail a bar. The measurement is worth having and it is not
            // worth a red run, so every filesystem error here is swallowed with a line and no more.
            try
            {
                string root = Directory.GetCurrentDirectory();
                string path = Path.Combine(root, LogRelative);
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                bool fresh = !File.Exists(path);
                var sb = new StringBuilder();
                if (fresh)
                {
                    sb.Append("utc\tgroup\tstage\tms\texit\n");
                }

                string stamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
                foreach (KeyValuePair<string, long> stage in Stages)
                {
                    sb.Append(stamp).Append('\t')
                      .Append(_group).Append('\t')
                      .Append(stage.Key).Append('\t')
                      .Append(stage.Value.ToString(CultureInfo.InvariantCulture)).Append('\t')
                      .Append(exitCode.ToString(CultureInfo.InvariantCulture)).Append('\n');
                }

                sb.Append(stamp).Append('\t')
                  .Append(_group).Append('\t')
                  .Append("TOTAL").Append('\t')
                  .Append(Wall.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture)).Append('\t')
                  .Append(exitCode.ToString(CultureInfo.InvariantCulture)).Append('\n');

                File.AppendAllText(path, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Debug.Log($"TIMING: could not append to {LogRelative} ({e.GetType().Name}: {e.Message}). "
                          + "The run is unaffected; only its timing was not recorded.");
            }

            Debug.Log($"TIMING: {_group} total {Format(Wall.ElapsedMilliseconds)} over {Stages.Count} stage(s). "
                      + $"Slowest: {Slowest(3)}. Appended to {LogRelative}.");
        }

        /// <summary>The slowest N stages, named with their share — the one line that says whether the bar
        /// is one expensive stage or many.</summary>
        private static string Slowest(int count)
        {
            var ordered = new List<KeyValuePair<string, long>>(Stages);
            ordered.Sort((a, b) => b.Value.CompareTo(a.Value));

            long total = 0;
            foreach (KeyValuePair<string, long> s in Stages) { total += s.Value; }
            if (total <= 0) { return "nothing measured"; }

            var sb = new StringBuilder();
            for (int i = 0; i < count && i < ordered.Count; i++)
            {
                if (i > 0) { sb.Append(", "); }
                long pct = ordered[i].Value * 100 / total;
                sb.Append(ordered[i].Key).Append(' ').Append(Format(ordered[i].Value))
                  .Append(" (").Append(pct.ToString(CultureInfo.InvariantCulture)).Append("%)");
            }

            return sb.ToString();
        }

        private static string Format(long ms)
        {
            double seconds = ms / 1000.0;
            return seconds.ToString("0.0", CultureInfo.InvariantCulture) + " s";
        }
    }
}
