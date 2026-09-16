using System;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Elections;
using PoliSim.Persistence;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// CL-3 (2026-09-16, §523): **THE PERSISTED QUEUE IS THE RECORD OF THE PLAY.** A player's campaign is saved as a replay record
    /// (`PlayerCampaignRecord`, C-R4b): the streams' draw counts at its start, the days stepped, every queued decision with its day, kind,
    /// target and outlay, every answer to a story. So the record of a play is not a diary - it is the save. This dump prints that record
    /// as a markdown table from a save file, so what Elias played is read off the file he played it in and never retold.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.PlayRecordDump.Run -save=&lt;path to a .json save&gt; -logFile &lt;path&gt;`
    /// (no `-save=`: the pre-campaign save the protocol stages, `playtest_4_precampaign_day1.json` in the saves directory).
    /// </summary>
    public static class PlayRecordDump
    {
        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        private static string Arg(string prefix, string fallback)
        {
            foreach (string a in Environment.GetCommandLineArgs()) { if (a.StartsWith(prefix, StringComparison.Ordinal)) { return a.Substring(prefix.Length); } }
            return fallback;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            string path = Arg("-save=", Path.Combine(SaveGameService.DefaultSaveDirectory, PlayProtocolStaging.PreCampaignSaveName + ".json"));
            if (!File.Exists(path)) { Debug.LogError("PLAY RECORD: no save at " + path); CheckExit.Finish(1); return; }
            SaveGame save;
            try { save = SaveGameService.LoadFromFile(path); }
            catch (Exception e) { Debug.LogError("PLAY RECORD: the save does not load - " + e.Message); CheckExit.Finish(1); return; }
            var sb = new StringBuilder();
            sb.Append(F("=== PLAY RECORD: {0} ===\n", path));
            sb.Append(F("    format {0} · master seed {1} · player {2} · turn {3} · date {4:yyyy-MM-dd}\n", save.SaveVersion, save.MasterSeed, save.PlayerCountryId, save.CurrentTurn, save.CurrentDate));
            PlayerCampaignRecord c = save.PlayerCampaign;
            if (c == null)
            {
                sb.Append("    no campaign record yet - the campaign has not opened in this save (a pre-campaign save reads this way until the run-up's first decision is queued).\n");
            }
            else
            {
                sb.Append(F("    campaign: election {0:yyyy-MM-dd} · start {1:yyyy-MM-dd} · days stepped {2} · pre-campaign days {3}, stepped {4} · queued decisions {5} · story answers {6}\n",
                    c.ElectionDate, c.StartDate, c.DaysStepped, c.PreCampaignDays, c.PreCampaignDaysStepped, c.Queue.Count, c.ScandalAnswers != null ? c.ScandalAnswers.Count : 0));
                sb.Append("\n| day | kind | region | issue | outlay | role |\n|---|---|---|---|---|---|\n");
                foreach (QueuedDecisionRecord q in c.Queue)
                {
                    sb.Append(F("| {0} | {1} | {2} | {3} | {4:N0} | {5} |\n", q.Day, q.Kind, q.RegionIndex < 0 ? "national" : q.RegionIndex.ToString(CultureInfo.InvariantCulture), q.Issue < 0 ? "-" : ((IssueId)q.Issue).ToString(), q.Spend, q.Role < 0 ? "-" : q.Role.ToString(CultureInfo.InvariantCulture)));
                }
                if (c.ScandalAnswers != null && c.ScandalAnswers.Count > 0)
                {
                    sb.Append("\n| day | story answered with |\n|---|---|\n");
                    foreach (ScandalAnswerRecord s in c.ScandalAnswers) { sb.Append(F("| {0} | {1} |\n", s.Day, s.Response)); }
                }
            }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
