using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The audit run's trajectory (`docs/audit/AUDIT_PROTOCOL.md`: "Seed and log every run. The same seed run twice must give an identical
    /// trajectory"): one `D` row per day the player's country lived through - every public `EconomyState` field (the trajectory dump's own
    /// field list, `TrajectoryBaselineDump.StateFields`, so a field added later joins without an edit here) and the political state - and,
    /// at every turn boundary, one `T` row per country of the world. The digest is SHA-256 over every line written, in order; two runs are
    /// the same trajectory when their digests are equal, and <see cref="FirstDifference"/> names the first line and column where two differ.
    /// </summary>
    internal sealed class AuditTrajectory : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly SHA256 _sha = SHA256.Create();
        private readonly FieldInfo[] _fields;
        private bool _finished;
        private string _digest;

        internal string Path { get; }
        internal int Rows { get; private set; }

        internal AuditTrajectory(string dir, string id)
        {
            Path = System.IO.Path.Combine(dir, id + ".traj.csv");
            _writer = new StreamWriter(Path, false, new UTF8Encoding(false)) { NewLine = "\n" };
            _fields = TrajectoryBaselineDump.StateFields();
            var head = new StringBuilder("kind,day,date,turn,country");
            foreach (FieldInfo f in _fields) { head.Append(',').Append(f.Name); }
            head.Append(",party,partyApproval,pm,cabinet,support,outcome,caretaker,seats,elections,lastElection,presidentialContests,chair,gameOver");
            Write(head.ToString());
        }

        /// <summary>The day row of the player's country.</summary>
        internal void Day(int day, DateTime date, int turn, Country c, bool gameOver)
        {
            var sb = new StringBuilder(4096);
            sb.Append("D,").Append(day.ToString(CultureInfo.InvariantCulture)).Append(',').Append(Date(date)).Append(',')
              .Append(turn.ToString(CultureInfo.InvariantCulture)).Append(',').Append(c.Id);
            AppendState(sb, c);
            GovernmentRecord g = c.Government;
            sb.Append(',').Append(Cell(c.PlayerPartyAbbrev))
              .Append(',').Append(c.PartyApprovalRating.ToString("R", CultureInfo.InvariantCulture))
              .Append(',').Append(Cell(g?.PmParty))
              .Append(',').Append(Cell(g == null ? null : string.Join("+", g.Cabinet)))
              .Append(',').Append(Cell(g == null ? null : string.Join("+", g.Support)))
              .Append(',').Append(Cell(g?.Outcome))
              .Append(',').Append(g != null && g.Caretaker ? "1" : "0")
              .Append(',').Append(Cell(Seats(c.ParliamentSeats)))
              .Append(',').Append(c.ElectionHistory.Count.ToString(CultureInfo.InvariantCulture))
              .Append(',').Append(c.ElectionHistory.Count > 0 ? Date(c.ElectionHistory[c.ElectionHistory.Count - 1].Date) : string.Empty)
              .Append(',').Append(c.PresidentialElections.Count.ToString(CultureInfo.InvariantCulture))
              .Append(',').Append(Cell(c.CurrentFedChair?.Name))
              .Append(',').Append(gameOver ? "1" : "0");
            Write(sb.ToString());
        }

        /// <summary>The turn rows: every country's state at a boundary.</summary>
        internal void Turn(int day, DateTime date, int turn, World world)
        {
            foreach (Country c in world.Countries)
            {
                var sb = new StringBuilder(4096);
                sb.Append("T,").Append(day.ToString(CultureInfo.InvariantCulture)).Append(',').Append(Date(date)).Append(',')
                  .Append(turn.ToString(CultureInfo.InvariantCulture)).Append(',').Append(c.Id);
                AppendState(sb, c);
                sb.Append(',').Append(Cell(c.PlayerPartyAbbrev)).Append(',').Append(c.PartyApprovalRating.ToString("R", CultureInfo.InvariantCulture))
                  .Append(',').Append(Cell(c.Government?.PmParty)).Append(',').Append(Cell(c.Government == null ? null : string.Join("+", c.Government.Cabinet)))
                  .Append(",,,,").Append(Cell(Seats(c.ParliamentSeats))).Append(',').Append(c.ElectionHistory.Count.ToString(CultureInfo.InvariantCulture)).Append(",,,,");
                Write(sb.ToString());
            }
        }

        /// <summary>A marker line (a save and load, a pause, a speed change): written to the file for the reader, kept OUT of the digest - the
        /// protocol's comparisons (save, load and continue against an uninterrupted run; 1x against 3x) ask whether the trajectory itself moved.</summary>
        internal void Mark(int day, DateTime date, string what) => Write("M," + day.ToString(CultureInfo.InvariantCulture) + "," + Date(date) + "," + Cell(what), hashed: false);

        private void AppendState(StringBuilder sb, Country c)
        {
            foreach (FieldInfo f in _fields) { sb.Append(',').Append(Cell(Format(f.GetValue(c.State)))); }
        }

        internal string Finish()
        {
            if (_finished) { return _digest; }
            _finished = true;
            _sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var hex = new StringBuilder(64);
            foreach (byte b in _sha.Hash) { hex.Append(b.ToString("x2", CultureInfo.InvariantCulture)); }
            _digest = hex.ToString();
            _writer.Flush();
            _writer.Dispose();
            return _digest;
        }

        public void Dispose()
        {
            if (!_finished) { Finish(); }
            _sha.Dispose();
        }

        private void Write(string line, bool hashed = true)
        {
            if (hashed)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(line + "\n");
                _sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
            }

            _writer.Write(line);
            _writer.Write('\n');
            Rows++;
        }

        internal static string Format(object value)
        {
            switch (value)
            {
                case null: return string.Empty;
                case float f: return f.ToString("R", CultureInfo.InvariantCulture);
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                case int i: return i.ToString(CultureInfo.InvariantCulture);
                case long l: return l.ToString(CultureInfo.InvariantCulture);
                case bool b: return b ? "1" : "0";
                case DateTime t: return Date(t);
                default: return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        private static string Date(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>A CSV cell: commas and line breaks cannot appear inside one.</summary>
        private static string Cell(string s) => string.IsNullOrEmpty(s) ? string.Empty : s.Replace(',', ';').Replace('\n', ' ').Replace('\r', ' ');

        private static string Seats(Dictionary<string, int> seats) =>
            seats == null ? string.Empty : string.Join("|", seats.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + ":" + p.Value.ToString(CultureInfo.InvariantCulture)));

        /// <summary>Where two runs' trajectories part: the first line that differs, and in it the first column, by the header's name.</summary>
        internal static string FirstDifference(string dir, string idA, string idB)
        {
            string a = System.IO.Path.Combine(dir, idA + ".traj.csv");
            string b = System.IO.Path.Combine(dir, idB + ".traj.csv");
            if (!File.Exists(a) || !File.Exists(b)) { return "a trajectory file is missing - nothing compared"; }
            using (var ra = new StreamReader(a))
            using (var rb = new StreamReader(b))
            {
                string[] header = null;
                for (int line = 1; ; line++)
                {
                    string la = NextHashed(ra);
                    string lb = NextHashed(rb);
                    if (la == null && lb == null) { return "the files are identical line for line (the digests differ only if a file was written twice)"; }
                    if (la == null || lb == null) { return $"line {line}: one trajectory ends ({(la == null ? idA : idB)}) where the other goes on"; }
                    if (header == null) { header = la.Split(','); }
                    if (la == lb) { continue; }
                    string[] ca = la.Split(',');
                    string[] cb = lb.Split(',');
                    for (int i = 0; i < Math.Max(ca.Length, cb.Length); i++)
                    {
                        string va = i < ca.Length ? ca[i] : "(none)";
                        string vb = i < cb.Length ? cb[i] : "(none)";
                        if (va == vb) { continue; }
                        string name = ca.Length > 0 && ca[0] == "D" && i < header.Length ? header[i] : (ca.Length > 0 && ca[0] == "T" && i < header.Length ? header[i] : "column " + (i + 1));
                        string where = ca.Length > 3 ? $"{ca[0]} row, day {ca[1]} ({ca[2]})" : "a row";
                        return $"hashed line {line}, {where}, {name}: {va} against {vb}";
                    }
                }
            }
        }

        /// <summary>The next line the digest covers - marker lines are skipped, as the digest skips them.</summary>
        private static string NextHashed(StreamReader r)
        {
            string line;
            do { line = r.ReadLine(); } while (line != null && line.StartsWith("M,", StringComparison.Ordinal));
            return line;
        }
    }
}
