using System;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Data;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// AF-6 (q6, ruled 2026-10-18): "one generated table maps each of the eight departments to a portfolio; add an economy portfolio only where none
    /// fits". The table is <see cref="Departments"/>; its rendering is `docs/generated/DEPARTMENT_PORTFOLIOS.md`, written by <see cref="Write"/>. This check
    /// fails while the file differs from what the table renders (line ends aside - a Windows checkout may hand the file back with CRLF), while a department
    /// maps to no portfolio, and while the economy portfolio holds no department (q6 adds it only where none fits). Pinned in the cheap bar and the
    /// documents bar, so a document-only commit to the file still runs it.
    /// </summary>
    public static class DepartmentPortfolioCheck
    {
        public const string DocRelative = "docs/generated/DEPARTMENT_PORTFOLIOS.md";
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        /// <summary>The whole document, LF line ends: a fixed head, then <see cref="Departments.RenderTable"/> - the head lives here, in the Editor, so the
        /// runtime carries no document prose (MetaTextCheck).</summary>
        public static string Document() =>
            "# The eight departments and their portfolios (AF-6, q6)\n\n"
            + "GENERATED - never hand-edited. Written by `PoliSim.EditorTools.DepartmentPortfolioCheck.Write` from `Departments` "
            + "(`Assets/Scripts/Data/CabinetPortfolio.cs`); `DepartmentPortfolioCheck` fails while this file differs from it.\n\n"
            + "The ruling (q6, `docs/RULINGS_2026-10-18.md`): *one generated table maps each of the eight departments to a portfolio; add an economy "
            + "portfolio only where none fits.* The eight are the catalog's (`docs/data/SOCIETY_STATS.md` section 6).\n\n"
            + Departments.RenderTable();

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== DepartmentPortfolioCheck (AF-6, q6): the eight departments' portfolios, generated and held ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                Department[] all = (Department[])Enum.GetValues(typeof(Department));
                int unmapped = 0;
                foreach (Department d in all) { if (!Enum.IsDefined(typeof(CabinetPortfolio), Departments.PortfolioOf(d))) { unmapped++; } }
                Check(all.Length == 8 && unmapped == 0, F("{0} departments, every one mapped to a portfolio ({1} unmapped)", all.Length, unmapped));
                Check(Departments.Of(CabinetPortfolio.Economy).Count > 0,
                    F("the economy portfolio holds {0} department(s) none of the six fits - q6 adds it only where none fits", Departments.Of(CabinetPortfolio.Economy).Count));
                string want = Document();
                string path = Path.Combine(Root, DocRelative);
                string onDisk = File.Exists(path) ? File.ReadAllText(path, new UTF8Encoding(false)).Replace("\r\n", "\n") : null;
                Check(onDisk != null && onDisk == want, onDisk == null ? F("{0} does not exist - write it: -executeMethod PoliSim.EditorTools.DepartmentPortfolioCheck.Write", DocRelative)
                    : onDisk == want ? F("{0} is what the table renders ({1} lines)", DocRelative, want.Split('\n').Length - 1)
                    : F("{0} is STALE against the table - regenerate: -executeMethod PoliSim.EditorTools.DepartmentPortfolioCheck.Write", DocRelative));
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n').Append(ex.StackTrace).Append('\n'); }
            sb.Append(failures == 0 ? "=== DepartmentPortfolioCheck: the table holds ===" : F("=== DepartmentPortfolioCheck: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Writes the document from the table.</summary>
        public static void Write()
        {
            CheckExit.ArmLogFold();
            string path = Path.Combine(Root, DocRelative);
            File.WriteAllText(path, Document(), new UTF8Encoding(false));
            Debug.Log("=== DepartmentPortfolioCheck.Write (AF-6): written " + DocRelative + " ===");
            CheckExit.Finish(0);
        }
    }
}
