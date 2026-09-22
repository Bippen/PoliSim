// §574 — `CheckExit` as the standalone runner needs it: the same three calls the Editor's own carries
// (`ArmLogFold`, `Finish`, `Collect`), with the Editor's exit replaced by a collected code.
//
// ⚠ This is the ONE piece of the suite's machinery that is reimplemented rather than shared, and the reason is worth
// stating: the Editor's `CheckExit` lives inside `CheckSuite.cs`, which names all forty-six checks and would drag every
// engine dependency in the project into a console build. The API here is exactly the three members the document checks
// call; anything else they reach for will not compile, which is the signal that the check belongs in the Editor.
using System;

namespace PoliSim.EditorTools
{
    public static class CheckExit
    {
        private static bool _collecting;
        private static int _worst;
        private static int _errors;

        /// <summary>In the Editor this folds the log so an ATTRIB during a run is not read as the check's own verdict. Here the runner prints in order, so it counts errors.</summary>
        public static void ArmLogFold()
        {
            _errors = 0;
        }

        public static void Finish(int code)
        {
            if (code == 0 && _errors > 0) { code = 1; }

            if (_collecting) { _worst = Math.Max(_worst, code); return; }

            Environment.Exit(code);
        }

        public static int Collect(Action check)
        {
            bool wasCollecting = _collecting;
            int outerWorst = _worst;
            _collecting = true;
            _worst = 0;
            _errors = 0;
            try
            {
                check();
                return _worst;
            }
            finally
            {
                _collecting = wasCollecting;
                _worst = Math.Max(outerWorst, _worst);
            }
        }

        /// <summary>The runner's own hook: every LogError seen while a check runs counts, so a check that logs an error and finishes 0 still fails.</summary>
        public static void CountError() => _errors++;
    }
}
