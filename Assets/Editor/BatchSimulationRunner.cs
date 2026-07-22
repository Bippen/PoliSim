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
        private static int _framesWaited;

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            _framesWaited = 0;
            EditorApplication.update += WaitThenExit;
            EditorApplication.isPlaying = true;
        }

        private static void WaitThenExit()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            _framesWaited++;
            if (_framesWaited < FramesToWaitAfterPlay)
            {
                return;
            }

            EditorApplication.update -= WaitThenExit;
            Debug.Log("BatchSimulationRunner: exiting after wait.");
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }
}
