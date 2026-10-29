using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The pldesk film's Low (Reviews/2026-10-29_pldesk_integrator.md): the minister alert printed "A leak from InteriorJustice" - the portfolio's C#
    /// identifier, not its display name. The simulation wrote `{appointment.Key}` into the alert and `{cabinetEvent.Portfolio}` into the approval
    /// ledger's line, because the display names lived in a private UI helper. They now live in <see cref="Effectiveness.DisplayName"/>.
    ///
    /// <para><b>What it decides.</b> (1) Every <see cref="CabinetPortfolio"/> has a display name that is not a joined identifier (no lower-case letter
    /// followed by a capital). (2) THE SCAN: no interpolated string under `Assets/Scripts` (comments stripped, Debug.Log lines excluded - the log is not the player's)
    /// writes a portfolio value raw - `{x.Portfolio}`, `{portfolio}`, `{appointment.Key}`; one is a FAULT naming the file and line. (3) THE TEETH:
    /// the pattern catches the two planted writes the Low came from and passes the fixed form, every run.</para>
    /// </summary>
    public static class PortfolioDisplayNameCheck
    {
        private static readonly Regex RawPortfolio = new Regex(@"\{[A-Za-z_.]*(?:\.Portfolio|\b[pP]ortfolio|appointment\.Key)(?:\.ToString\(\))?(?:[,:][^}]*)?\}|DisplayName\.(?:Of|Spaced)\([A-Za-z_.]*[pP]ortfolio", RegexOptions.Compiled);
        private static readonly Regex Joined = new Regex(@"[a-z][A-Z]", RegexOptions.Compiled);

        private static readonly string[] Caught =
        {
            "record.Text = $\"A leak from {appointment.Key} ({appointment.Value.Name}) has cost the government\";",
            "ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, $\"Cabinet leak ({cabinetEvent.Portfolio})\", d);",
            "string s = $\"Minister for {portfolio}\";",
            "string s = $\"Minister for {portfolio.ToString()} and {post.Portfolio,-12}\";",
            "Reshuffle(c.Id, portfolio, $\"Cabinet reshuffle ({DisplayName.Of(portfolio.ToString())})\", out _);",
        };
        private static readonly string[] Passed =
        {
            "record.Text = $\"A leak from {Effectiveness.DisplayName(appointment.Key)} ({appointment.Value.Name}) has cost\";",
            "ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, $\"Cabinet leak ({Effectiveness.DisplayName(cabinetEvent.Portfolio)})\", d);",
            "string s = $\"{appointment.Value.Name}\";",
            "Reshuffle(c.Id, portfolio, $\"Cabinet reshuffle ({name})\", out _);",
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var failures = new List<string>();
            var sb = new StringBuilder();
            sb.Append("=== PortfolioDisplayNameCheck: no player-visible text prints a portfolio's raw identifier ===\n");

            int caught = 0, passed = 0;
            foreach (string plant in Caught) { if (RawPortfolio.IsMatch(plant)) { caught++; } else { failures.Add($"the teeth: the pattern missed the planted raw write `{plant}`"); } }
            foreach (string plant in Passed) { if (!RawPortfolio.IsMatch(plant)) { passed++; } else { failures.Add($"the teeth: the pattern caught the planted display-name write `{plant}`"); } }
            sb.Append($"  the teeth: {caught} planted raw write(s) caught, {passed} planted display-name write(s) passed\n");

            int names = 0;
            foreach (CabinetPortfolio p in System.Enum.GetValues(typeof(CabinetPortfolio)))
            {
                string name = Effectiveness.DisplayName(p);
                if (string.IsNullOrEmpty(name) || Joined.IsMatch(name)) { failures.Add($"the names: {p} displays as '{name}', a joined identifier"); } else { names++; }
            }
            sb.Append($"  the names: {names} portfolio(s) display as words\n");

            int files = 0, hits = 0;
            foreach (string path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                files++;
                string[] lines = SourceText.ReadWithoutComments(path).Split('\n');   // comments stripped (CommentImmunityCheck): a raw write named in a comment is not one
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line.Contains("Debug.Log") || !line.Contains("$\"")) { continue; }
                    if (RawPortfolio.IsMatch(line)) { hits++; failures.Add($"the scan: {path.Replace('\\', '/')}:{i + 1} writes a portfolio raw - {line.Trim()}"); }
                }
            }
            sb.Append($"  the scan: {files} file(s), {hits} raw write(s) - its limits: interpolated strings, a portfolio named portfolio / .Portfolio / appointment.Key (another name, a concatenation or a non-portfolio enum is not seen)\n");

            foreach (string f in failures) { sb.Append("  ⚠ FAULT  ").Append(f).Append('\n'); }
            sb.Append($"\n=== PortfolioDisplayNameCheck: {(failures.Count == 0 ? "CLEAN" : failures.Count + " fault(s)")} ===\n");
            if (failures.Count == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures.Count == 0 ? 0 : 1);
        }
    }
}
