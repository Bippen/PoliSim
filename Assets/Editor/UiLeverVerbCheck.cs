using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Simulation;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// AF-11 (Elias's ruling q11 of 2026-10-18, `docs/RULINGS_2026-10-18.md`: <i>"every lever the UI writes gets a manager verb, and the impact ledger
    /// reads it"</i>; the audit's GA-0-55) - **no UI file writes a lever's model field itself.** Each lever the UI pulls outside a bill goes through its
    /// <see cref="SimulationManager"/> verb, and only the verb shows it to the impact ledger (<see cref="PolicyImpactLedger.BeforeLever"/>,
    /// <see cref="PolicyImpactLedger.AfterLever"/>) - so a UI write is a lever the ledger never sees, which is the defect the ruling closed.
    ///
    /// <para><b>What it decides.</b> (1) THE SCAN: every file under `Assets/Scripts/UI`, comments stripped (a write named in a comment is not one), for
    /// each lever's direct write - the partner override, the central bank's head, the fleet's orders, the cabinet's ministers, the fiscal stance a Finance partner asks - and for any write to
    /// the economy's state; one is a FAULT naming the file and line. (2) THE ROUTE: each lever's verb is called from the UI (a lever with no route has
    /// lost its surface, and the row here says which), and each <see cref="PlayerLeverKind"/> is built by exactly one verb in the manager's source.
    /// (3) THE TEETH: every pattern catches a planted write and passes a planted read and a planted verb call, every run - a pattern that cannot
    /// tell them apart fails here rather than passing the scan by construction.</para>
    /// </summary>
    public static class UiLeverVerbCheck
    {
        private sealed class Lever
        {
            public string Name;
            public Regex Write;
            public string Verb;     // null: no verb - the write itself is the fault
            public string[] Caught; // planted writes the pattern must catch
            public string[] Passed; // planted reads and verb calls it must pass
        }

        /// <summary>An assignment of any form: `=` (never `==` or `=>`), the compound operators (`%=` and `??=` among them), `++` and `--`.</summary>
        private const string Assign = @"\s*(?:\?\?=|%=|\+=|-=|\*=|/=|&=|\|=|\^=|<<=|>>=|=(?![=>])|\+\+|--)";

        /// <summary>An index, one level of nesting deep - `[p]`, `[order[i]]`.</summary>
        private const string Index = @"\[(?:[^\[\]]|\[[^\[\]]*\])*\]";

        private static readonly Lever[] Levers =
        {
            new Lever
            {
                Name = "the trade partner's override (Set override)", Verb = "SetPartnerTariffOverride",
                Write = new Regex(@"\bPlayerTariffOverride" + Assign, RegexOptions.Compiled),
                Caught = new[] { "link.PlayerTariffOverride = Mathf.Clamp(r, 0f, 50f);", "link.PlayerTariffOverride += 1f;", "link.PlayerTariffOverride %= 5f;", "new TradePartner(p, e, i) { PlayerTariffOverride = 4f };" },
                Passed = new[] { "float s = link.PlayerTariffOverride;", "if (link.PlayerTariffOverride == x) { }", "if (link.PlayerTariffOverride >= 0f) { }", "bool h = link.HasPlayerTariffOverride;", "_simulationManager.SetPartnerTariffOverride(c.Id, link.PartnerId, out _);" },
            },
            new Lever
            {
                Name = "the central bank's head", Verb = "AppointCentralBankHead",
                Write = new Regex(@"\bCurrentFedChair" + Assign, RegexOptions.Compiled),
                Caught = new[] { "_playerCountry.CurrentFedChair = candidate;", "c.CurrentFedChair ??= candidate;", "var copy = new Country { CurrentFedChair = chair };" },
                Passed = new[] { "if (_playerCountry.CurrentFedChair == null) { }", "if (c.CurrentFedChair != null) { }", "FedChair chair = _playerCountry.CurrentFedChair;", "_simulationManager.AppointCentralBankHead(_playerCountry.Id, c, out _);" },
            },
            new Lever
            {
                Name = "a fleet order", Verb = "PlaceFleetOrder",
                Write = new Regex(@"\bEnergyFleet\s*\.\s*(?:Place|Withdraw)\s*\(|\bFleetOrders\s*(?:" + Index + @")?" + Assign
                                  + @"|\.\s*FleetOrders\s*\.\s*(?:Add|AddRange|Insert|InsertRange|Remove|RemoveAt|RemoveAll|RemoveRange|Clear|Reverse|Sort)\s*\(", RegexOptions.Compiled),
                Caught = new[] { "EnergyFleet.Place(c, k, up, year, turn);", "EnergyFleet.Withdraw(c, order);", "c.FleetOrders.Add(order);", "c.FleetOrders = null;", "c.FleetOrders[0] = order;", "c.FleetOrders ??= new List<EnergyFleet.Order>();" },
                Passed = new[] { "foreach (EnergyFleet.Order o in EnergyFleet.Queue(c)) { }", "int n = EnergyFleet.PendingCount(c);", "EnergyFleet.Sanitise(loaded);", "int k = c.FleetOrders.Count;", "var o = c.FleetOrders[0];", "_simulationManager.PlaceFleetOrder(c.Id, k, up, year, out _);" },
            },
            new Lever
            {
                Name = "a minister's appointment", Verb = "AppointMinister",
                Write = new Regex(@"\bCabinetMinisters\s*(?:" + Index + @")?" + Assign + @"|\.\s*CabinetMinisters\s*\.\s*(?:Add|TryAdd)\s*\(", RegexOptions.Compiled),
                Caught = new[] { "c.CabinetMinisters[portfolio] = candidate;", "c.CabinetMinisters[order[i]] = candidate;", "c.CabinetMinisters.Add(portfolio, candidate);", "c.CabinetMinisters = new Dictionary<CabinetPortfolio, CabinetMinister>();" },
                Passed = new[] { "if (c.CabinetMinisters.TryGetValue(portfolio, out CabinetMinister m)) { }", "bool held = c.CabinetMinisters.ContainsKey(portfolio);", "var m = c.CabinetMinisters[order[i]];", "_simulationManager.AppointMinister(c.Id, portfolio, candidate, out _);" },
            },
            new Lever
            {
                Name = "a minister's reshuffle", Verb = "ReshuffleMinister",
                Write = new Regex(@"\.\s*CabinetMinisters\s*\.\s*(?:Remove|Clear)\s*\(", RegexOptions.Compiled),
                Caught = new[] { "c.CabinetMinisters.Remove(portfolio);", "c.CabinetMinisters.Clear();" },
                Passed = new[] { "_cabinetCandidatesByPortfolio.Remove(portfolio);", "_simulationManager.ReshuffleMinister(c.Id, portfolio, label, out _);" },
            },
            new Lever
            {
                Name = "the fiscal stance a Finance partner asks", Verb = "SetFinanceStanceTarget",
                Write = new Regex(@"\bFinanceStancePlayerTarget" + Assign, RegexOptions.Compiled),
                Caught = new[] { "c.FinanceStancePlayerTarget = 1f;", "c.FinanceStancePlayerTarget -= 0.5f;" },
                Passed = new[] { "float asked = c.FinanceStancePlayerTarget;", "_simulationManager.SetFinanceStanceTarget(PlayerCountryId, set);" },
            },
            new Lever
            {
                Name = "the economy's state (a lever's state write rides its verb)", Verb = null,
                Write = new Regex(@"\.\s*State\s*(?:\.\s*[A-Za-z_]\w*|" + Index + @")+" + Assign, RegexOptions.Compiled),
                Caught = new[] { "c.State.ApprovalRating = Mathf.Clamp(c.State.ApprovalRating - cost, 0f, 100f);", "c.State.Gdp += 1f;", "c.State.ApprovalRating %= 10f;", "c.State.Sub.Field = 2f;", "c.State.Shares[k] = 0.5f;" },
                Passed = new[] { "float a = c.State.ApprovalRating;", "if (c.State.Gdp == 0f) { }", "int s = c.State.Gdp.CompareTo(x);" },
            },
        };

        /// <summary>What the scan does not see, said in its own output.</summary>
        private const string OutOfScope = "a write through an alias (a local holding the model's object or collection), a field passed by ref or out, and every file outside Assets/Scripts/UI - the film harness (Assets/Scripts/Testing, UiScreenshotDriver) stages state directly by design";

        private static readonly Regex KindBuilt = new Regex(@"new\s+PlayerLever\s*\(\s*PlayerLeverKind\s*\.\s*(\w+)", RegexOptions.Compiled);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var failures = new List<string>();
            var sb = new StringBuilder();
            sb.Append("=== UiLeverVerbCheck (AF-11, q11 of 2026-10-18): every lever the UI pulls goes through its manager verb, which the impact ledger reads ===\n");

            // (3) the teeth first: a pattern that cannot tell a write from a read verifies nothing below
            int caught = 0, passed = 0;
            foreach (Lever lever in Levers)
            {
                foreach (string plant in lever.Caught)
                {
                    if (lever.Write.IsMatch(plant)) { caught++; } else { failures.Add($"the teeth: {lever.Name}'s pattern missed the planted write `{plant}`"); }
                }
                foreach (string plant in lever.Passed)
                {
                    if (!lever.Write.IsMatch(plant)) { passed++; } else { failures.Add($"the teeth: {lever.Name}'s pattern caught the planted read or verb call `{plant}`"); }
                }
            }
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  the teeth: {0} planted write(s) caught, {1} planted read(s) and verb call(s) passed\n", caught, passed));

            // (1) the scan, and (2) the routes
            const string ui = "Assets/Scripts/UI";
            var writes = new Dictionary<Lever, List<string>>();
            var routes = new Dictionary<Lever, List<string>>();
            foreach (Lever lever in Levers) { writes[lever] = new List<string>(); routes[lever] = new List<string>(); }
            int files = 0;
            if (Directory.Exists(ui))
            {
                foreach (string path in Directory.GetFiles(ui, "*.cs", SearchOption.AllDirectories))
                {
                    files++;
                    string rel = path.Replace('\\', '/');
                    string code = SourceText.ReadWithoutComments(path);
                    foreach (Lever lever in Levers)
                    {
                        foreach (Match m in lever.Write.Matches(code)) { writes[lever].Add(rel + ":" + LineOf(code, m.Index).ToString(CultureInfo.InvariantCulture) + " `" + m.Value.Trim() + "`"); }
                        if (lever.Verb == null) { continue; }
                        foreach (Match m in Regex.Matches(code, @"\.\s*" + lever.Verb + @"\s*\(")) { routes[lever].Add(Path.GetFileName(rel) + ":" + LineOf(code, m.Index).ToString(CultureInfo.InvariantCulture)); }
                    }
                }
            }
            if (files == 0) { failures.Add($"no source under {ui} was read - the scan verified NOTHING"); }
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  the scan: {0} UI source file(s), comments stripped\n  out of its scope, stated: {1}\n", files, OutOfScope));
            foreach (Lever lever in Levers)
            {
                string route = lever.Verb == null ? "no verb - a write is the fault" : $"verb {lever.Verb} called at " + (routes[lever].Count == 0 ? "NOWHERE" : string.Join(", ", routes[lever]));
                sb.Append(string.Format(CultureInfo.InvariantCulture, "  {0}: {1} direct write(s); {2}\n", lever.Name, writes[lever].Count, route));
                foreach (string w in writes[lever]) { failures.Add($"{w} - {lever.Name} written by the UI, past " + (lever.Verb == null ? "every verb" : "SimulationManager." + lever.Verb) + ": the impact ledger never sees it"); }
                if (lever.Verb != null && routes[lever].Count == 0) { failures.Add($"{lever.Name}: the UI never calls SimulationManager.{lever.Verb} - the lever lost its surface, or reached the model another way"); }
                if (lever.Verb != null && typeof(SimulationManager).GetMethod(lever.Verb) == null) { failures.Add($"{lever.Name}: SimulationManager has no public {lever.Verb}"); }
            }

            // (2) each lever kind built by exactly one verb in the manager's source
            const string manager = "Assets/Scripts/Simulation/SimulationManager.cs";
            var built = new Dictionary<string, int>(StringComparer.Ordinal);
            if (File.Exists(manager))
            {
                foreach (Match m in KindBuilt.Matches(SourceText.ReadWithoutComments(manager)))
                {
                    built.TryGetValue(m.Groups[1].Value, out int n);
                    built[m.Groups[1].Value] = n + 1;
                }
            }
            else { failures.Add(manager + " is not on disk - the levers' verbs were not read"); }
            var kinds = new List<string>();
            foreach (PlayerLeverKind kind in Enum.GetValues(typeof(PlayerLeverKind)))
            {
                built.TryGetValue(kind.ToString(), out int n);
                kinds.Add(kind + " " + n.ToString(CultureInfo.InvariantCulture));
                if (n != 1) { failures.Add($"the lever {kind} is built by {n} verb(s) in SimulationManager - one verb pulls each lever"); }
            }
            sb.Append("  the levers built in the manager (kind, verbs): ").Append(string.Join(" · ", kinds)).Append('\n');

            foreach (string f in failures) { sb.Append("  ⚠ FAULT  ").Append(f).Append('\n'); }
            sb.Append($"\n=== UiLeverVerbCheck: {(failures.Count == 0 ? "CLEAN" : failures.Count + " fault(s)")} ===\n");
            if (failures.Count == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures.Count == 0 ? 0 : 1);
        }

        private static int LineOf(string code, int at)
        {
            int line = 1;
            for (int i = 0; i < at && i < code.Length; i++) { if (code[i] == '\n') { line++; } }
            return line;
        }
    }
}
