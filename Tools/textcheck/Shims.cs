// §574 (2026-09-22, the efficiency pass's item 2b) — THE ENGINE'S EDGE, FOR CHECKS THAT NEVER TOUCH THE ENGINE.
//
// The document tier's checks read `.cs` and `.md` files and print. Eight of them use no Unity type at all beyond
// `Debug.Log`, `Application.dataPath` and (in one case) the log callback - measured 2026-09-22, §573 - and yet each ran
// inside a Unity Editor that cost 23 s to start for ≈ 0 s of work. These shims are the whole of what they need, so the
// SAME SOURCE FILES compile in a plain console project and run in about a second.
//
// ⚠ NOTHING HERE IMPLEMENTS BEHAVIOUR. Every type below is the smallest surface that lets the checks' own code compile
// and log; when a check grows a real engine dependency it stops compiling here, which is the signal to leave it in the
// Editor rather than to widen this file. A shim that started simulating Unity would be a second engine to keep true.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public enum LogType { Error = 0, Assert = 1, Warning = 2, Log = 3, Exception = 4 }

    public static class Debug
    {
        public delegate void Handler(string condition, string stackTrace, LogType type);

        /// <summary>Every line the checks print, in order - the runner writes them to the console and the log file.</summary>
        public static readonly List<string> Lines = new List<string>();

        public static event Handler Logged;

        public static void Log(object message) => Emit(message, LogType.Log);

        public static void LogWarning(object message) => Emit(message, LogType.Warning);

        public static void LogError(object message) => Emit(message, LogType.Error);

        private static void Emit(object message, LogType type)
        {
            string text = message?.ToString() ?? string.Empty;
            Lines.Add(text);
            Console.WriteLine(text);
            Logged?.Invoke(text, string.Empty, type);
        }
    }

    public static class Application
    {
        /// <summary>Set by the runner before any check runs: the project's `Assets` directory, as Unity would report it.</summary>
        public static string dataPath { get; set; } = string.Empty;

        public static bool isBatchMode => true;

        public delegate void LogCallback(string condition, string stackTrace, LogType type);

        public static event LogCallback logMessageReceived
        {
            add { Debug.Logged += new Debug.Handler(value); }
            remove { Debug.Logged -= new Debug.Handler(value); }
        }
    }

    public static class Mathf
    {
        public static int RoundToInt(double f) => (int)Math.Round(f, MidpointRounding.AwayFromZero);

        public static float Max(float a, float b) => a > b ? a : b;

        public static int Max(int a, int b) => a > b ? a : b;

        public static float Min(float a, float b) => a < b ? a : b;

        public static int Min(int a, int b) => a < b ? a : b;

        public static float Abs(float f) => Math.Abs(f);

        public static float Clamp01(float f) => f < 0f ? 0f : f > 1f ? 1f : f;
    }
}

namespace UnityEditor
{
    // §574: the two document checks that name `using UnityEditor;` never call anything in it - the using is habit, not
    // dependency. The namespace exists here so their source compiles unchanged; adding a MEMBER to it would mean a check
    // had grown a real Editor dependency, and that check belongs in the Editor.
    internal static class EditorNamespaceExistsForCompilation
    {
    }
}
