using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Command-line entry point for running SimulationTestRunner inside the real Unity Editor:
    /// `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.BatchSimulationRunner.Run -logFile &lt;path&gt; [-turns=N] [-scenario=X]
    /// [-runmatrix]`. SimulationTestRunner reads those trailing arguments itself (via
    /// Environment.GetCommandLineArgs()) - passing `-runmatrix` runs its full baseline/stress/
    /// sustainedexploit/tariffoverride x 100/500-turn matrix (8 combinations) in one Play session,
    /// each against its own fresh World; omitting it runs a single (-turns=/-scenario=, default
    /// 100/baseline) combination, matching the original single-run behavior. This is now the standard
    /// way to validate a simulation change - see "Real-Unity Validation is the Standard Path" in
    /// CLAUDE.md - superseding the standalone C# harness as the primary validation tool (the harness
    /// remains useful for fast iteration/sweeping before a change is ready to confirm here). Since
    /// SimulationTestRunner.Start() runs entirely synchronously (no coroutines/yields, however many
    /// turns or scenarios requested), Unity's own single-threaded update loop can't advance this
    /// script's wait-frame counter until Start() has fully returned - so the frame count below is a
    /// completion SIGNAL, not a real-time budget; it doesn't need to scale with -runmatrix's larger
    /// workload. Lives under Assets/Editor, which Unity excludes from player builds automatically -
    /// not gameplay code.
    /// </summary>
    public static class BatchSimulationRunner
    {
        private const int FramesToWaitAfterPlay = 15;

        // State lives in SessionState, not static fields, because ENTERING PLAY MODE TRIGGERS A DOMAIN
        // RELOAD - and a domain reload wipes every static field and unsubscribes every delegate.
        //
        // That was the bug behind this project's long-running "batch run hangs forever" problem. The old
        // version subscribed WaitThenExit to EditorApplication.update and then set isPlaying = true; the
        // reload that followed silently removed that subscription and reset the frame counter, so
        // WaitThenExit never ran again and EditorApplication.Exit was never called. Unity finished the
        // simulation, logged its summary, and then sat in Play mode indefinitely.
        //
        // The diagnostic that identifies this: "BatchSimulationRunner: exiting after wait." below never
        // appeared in ANY run's log, hung or otherwise. Five occurrences were misattributed to Search
        // indexing and later to a crashed asset import worker - both were bystanders visible in the same
        // logs. Disabling parallel import removed the worker crash entirely and the hang persisted
        // unchanged, which is what ruled that explanation out.
        //
        // SessionState survives domain reloads (it is scoped to the Editor session), and
        // [InitializeOnLoadMethod] re-subscribes after each reload, so the exit path now survives the
        // very event that used to destroy it.
        private const string ActiveKey = "PoliSim.BatchSimulationRunner.Active";
        private const string FramesKey = "PoliSim.BatchSimulationRunner.FramesWaited";

        [InitializeOnLoadMethod]
        private static void ReattachAfterDomainReload()
        {
            if (SessionState.GetBool(ActiveKey, false))
            {
                // Ruling 1 (2026-08-25): the matrix runner exited 0 unconditionally, and CLAUDE.md's
                // own note said "the matrix bar greps for ATTRIB" EXTERNALLY - the exit code reflected
                // nothing the run asserted, the exact hole this ruling closes. Arm the fold HERE, in
                // the post-Play-mode-reload domain (entering Play triggers a domain reload that wipes
                // CheckExit's static state, so arming in Run below would be erased): this callback
                // runs after that reload and before SimulationTestRunner.Start()'s first frame, so the
                // subscription is live when the ledger audits raise ATTRIB during the matrix. Anomalies
                // stay LogWarning and are deliberately NOT folded - they are MEASURED and judged (stress
                // scenarios flag legitimately), not asserted; only an Error-level audit failure gates.
                CheckExit.ArmLogFold();
                EditorApplication.update -= WaitThenExit;
                EditorApplication.update += WaitThenExit;
            }
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(FramesKey, 0);
            EditorApplication.update -= WaitThenExit;
            EditorApplication.update += WaitThenExit;
            EditorApplication.isPlaying = true;
        }

        private static void WaitThenExit()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            int framesWaited = SessionState.GetInt(FramesKey, 0) + 1;
            SessionState.SetInt(FramesKey, framesWaited);
            if (framesWaited < FramesToWaitAfterPlay)
            {
                return;
            }

            EditorApplication.update -= WaitThenExit;
            SessionState.SetBool(ActiveKey, false);
            Debug.Log("BatchSimulationRunner: exiting after wait.");

            // Through CheckExit.Finish, not a raw Exit(0), so the log fold armed above can turn a
            // run that logged any ATTRIB into a nonzero exit (ruling 1). A clean matrix run still
            // exits 0. Exit directly rather than setting isPlaying = false first and exiting in the
            // same frame: leaving Play mode is itself asynchronous, so the old sequence asked Unity to
            // tear down Play mode and terminate simultaneously.
            CheckExit.Finish(0);
        }
    }
}
