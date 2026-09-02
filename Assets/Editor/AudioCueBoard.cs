using System;
using UnityEditor;
using UnityEngine;
using PoliSim.UI;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P4-2 (2026-09-03): the harness-only trigger - every cue in the catalog, fired by name from a button, for
    /// Elias's own test. Editor-only (this file lives under Assets/Editor), so the game ships nothing of it. The
    /// game must be in Play mode for a sound to come out (the director's AudioSource lives in the play scene);
    /// in Edit mode the board says so and the buttons do nothing. The board also shows the master volume and
    /// mute the Saves screen persists, so a silent test can be told from a muted one.
    /// </summary>
    public sealed class AudioCueBoard : EditorWindow
    {
        [MenuItem("PoliSim/Audio/Cue Board")]
        public static void Open()
        {
            GetWindow<AudioCueBoard>("Audio Cue Board").Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Fire any cue by name (P4-2). Play mode required for sound.", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Application.isPlaying ? "Play mode: ON - buttons fire the director." : "Play mode: OFF - enter Play mode to hear anything.");
            EditorGUI.BeginChangeCheck();
            float volume = EditorGUILayout.Slider("Master volume", AudioDirector.Volume, 0f, 1f);
            bool mute = EditorGUILayout.Toggle("Mute", AudioDirector.Mute);
            if (EditorGUI.EndChangeCheck()) { AudioDirector.Volume = volume; AudioDirector.Mute = mute; }
            EditorGUILayout.Space();
            foreach (AudioCueEntry e in AudioDirector.Catalog)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!Application.isPlaying || string.IsNullOrEmpty(e.File)))
                    {
                        if (GUILayout.Button(e.Cue.ToString(), GUILayout.Width(170f))) { AudioDirector.Fire(e.Cue); }
                    }
                    EditorGUILayout.LabelField((e.File ?? "no file") + (e.Provisional ? "  (stand-in)" : string.Empty), GUILayout.Width(200f));
                    EditorGUILayout.LabelField(e.Note, EditorStyles.wordWrappedMiniLabel);
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Fired this session: {AudioDirector.Fired.Count} (last: {(AudioDirector.Fired.Count > 0 ? AudioDirector.Fired[AudioDirector.Fired.Count - 1].ToString() : "-")})");
        }
    }
}
