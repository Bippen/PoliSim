using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>CL-3 (2026-09-16, §523): the sheet on disk is the sheet the code and §346 generate today, Elias's column aside -
    /// <see cref="PlaySheetGenerator"/> regenerated in memory and compared. Cheap bar. (Its own file: the evidence check reads a
    /// registered check off the file named after it.)</summary>
    public static class PlaySheetCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            string path = Path.Combine(Directory.GetCurrentDirectory(), PlaySheetGenerator.SheetRelative);
            if (!File.Exists(path))
            {
                Debug.LogError("PLAY SHEET: " + PlaySheetGenerator.SheetRelative + " is not on disk - run PlaySheetGenerator.");
                CheckExit.Finish(1); return;
            }
            try
            {
                List<PlaySheetGenerator.SheetRow> rows = PlaySheetGenerator.Rows();
                string expected = PlaySheetGenerator.WithoutLastColumn(PlaySheetGenerator.Render(rows, null));
                string actual = PlaySheetGenerator.WithoutLastColumn(File.ReadAllText(path));
                if (expected != actual)
                {
                    Debug.LogError("PLAY SHEET: " + PlaySheetGenerator.SheetRelative + " is STALE - a constant moved or a §346 row changed since it was generated; run PlaySheetGenerator (Elias's column is kept).");
                    CheckExit.Finish(1); return;
                }
                Debug.Log(string.Format(CultureInfo.InvariantCulture, "PLAY SHEET: the sheet on disk is the sheet generated today - {0} entries, every constant read off the code.", rows.Count));
                Debug.Log("=== PlaySheetCheck: ALL ASSERTIONS PASS ===");
                CheckExit.Finish(0);
            }
            catch (Exception e)
            {
                Debug.LogError("PLAY SHEET: the sheet cannot be generated - " + e.Message);
                CheckExit.Finish(1);
            }
        }
    }
}
