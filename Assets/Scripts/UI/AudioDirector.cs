using System;
using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// The cue catalog (P4-2, 2026-09-03): every sound the game can make, by name. A cue is DISPLAY, never
    /// simulation - nothing in <c>PoliSim.Simulation</c> references this type, the director reads no model
    /// state, and a run with audio muted, missing or disabled produces byte-identical trajectories (the
    /// trajectory check verifies that, not this comment).
    /// </summary>
    public enum AudioCue
    {
        /// <summary>Any button the player presses (the idiom's paper and brass faces, the Desk chips, the pagers).</summary>
        ButtonPress,
        /// <summary>A ledger slider's draft snapping to its next step under a drag.</summary>
        SliderStep,
        /// <summary>A folder opened from the rail - the screen switch.</summary>
        FolderSwitch,
        /// <summary>The Desk taken back from a folder.</summary>
        FolderReturn,
        /// <summary>A division carried - the newest record queued for the ceremony passed.</summary>
        BillPasses,
        /// <summary>A division lost.</summary>
        BillFails,
        /// <summary>Time held by an interrupt - a decision landed on the Desk (the frame's banner appears).</summary>
        InterruptRaised,
        /// <summary>Election night: a constituency's count comes in.</summary>
        ConstituencyDeclares,
    }

    /// <summary>
    /// One row of the catalog: the file a cue resolves to (a name under <c>Resources/Audio/Cues</c>, no extension),
    /// or null when the pack holds nothing for it yet; and whether the file is the cue's OWN sound or a stand-in
    /// from the same pack, named so the coverage check prints it and the record can ask for the real one.
    /// </summary>
    public readonly struct AudioCueEntry
    {
        public readonly AudioCue Cue;
        public readonly string File;
        public readonly bool Provisional;
        public readonly string Note;
        public AudioCueEntry(AudioCue cue, string file, bool provisional, string note) { Cue = cue; File = file; Provisional = provisional; Note = note; }
    }

    /// <summary>
    /// The catalog and the one AudioSource path. Files come from Elias's pack (AssetPackArchive/Sound_Effect_Package,
    /// inventoried in COMPLETED.md §261 - origin, magic bytes, digest per file); only files that passed every check
    /// were copied under Resources/Audio/Cues. Both directions of coverage are asserted by AudioCueCoverageCheck the
    /// way sprites are: every cue's file exists (or the cue says it has none), and every file under the folder is
    /// named by a cue.
    ///
    /// The pack's licence: CONFIRMED BY OWNER, 2026-09-03 (ERRANDS E-10, closed by ruling; COMPLETED.md §278). No terms
    /// are transcribed here - none were given to transcribe - and the two AAC-container files the pack also holds are
    /// set aside by the same ruling: not imported, no re-export chased; the stand-ins the catalog names stay stand-ins,
    /// and the coverage check keeps counting them as such on every run.
    /// </summary>
    public static class AudioDirector
    {
        public const string ResourceFolder = "Audio/Cues";
        public const string VolumePref = "polisim.audio.volume";
        public const string MutePref = "polisim.audio.mute";

        /// <summary>The catalog. A null file is an honest gap: the cue is wired at its event and resolves to silence, and the check prints why.</summary>
        public static readonly AudioCueEntry[] Catalog =
        {
            new AudioCueEntry(AudioCue.ButtonPress, "Click1", false, "the pack's first click"),
            new AudioCueEntry(AudioCue.SliderStep, "Click2", true, "the pack's Slider_Click_Sound is an AAC stream in an MP4 container, which Unity's importer does not read; SET ASIDE by ruling (E-10, Elias 2026-09-03) - the second click stands in, and stays a stand-in"),
            new AudioCueEntry(AudioCue.FolderSwitch, "Folderswitch1", false, "the pack's first folder switch"),
            new AudioCueEntry(AudioCue.FolderReturn, "Folderswitch2", false, "the pack's second folder switch"),
            new AudioCueEntry(AudioCue.BillPasses, "ApprovalStamp", false, "the pack's approval stamp"),
            new AudioCueEntry(AudioCue.BillFails, null, false, "the pack's RejectedBillTear is an AAC stream in an MP4 container, which Unity's importer does not read; SET ASIDE by ruling (E-10, Elias 2026-09-03) - no stand-in, a stamp or a click would say the wrong thing; the cue is wired and silent"),
            new AudioCueEntry(AudioCue.InterruptRaised, "Folderswitch2", true, "the pack holds no interrupt sound; a folder landing on the Desk stands in, and stays a stand-in (E-10, closed by ruling 2026-09-03)"),
            new AudioCueEntry(AudioCue.ConstituencyDeclares, "Click2", true, "the pack holds no declaration sound; a click stands in, and stays a stand-in (E-10, closed by ruling 2026-09-03)"),
        };

        private static readonly Dictionary<AudioCue, AudioClip> Clips = new Dictionary<AudioCue, AudioClip>();
        private static readonly HashSet<AudioCue> Missing = new HashSet<AudioCue>();
        private static AudioSource _source;
        private static float _volume = -1f;
        private static bool _mute;
        private static bool _prefsRead;
        private static float _lastStepAt = -1f;

        /// <summary>Master volume 0..1, persisted. Read on first touch so a harness that never asks never touches PlayerPrefs.</summary>
        public static float Volume
        {
            get { ReadPrefs(); return _volume; }
            set { ReadPrefs(); _volume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(VolumePref, _volume); Apply(); }
        }

        public static bool Mute
        {
            get { ReadPrefs(); return _mute; }
            set { ReadPrefs(); _mute = value; PlayerPrefs.SetInt(MutePref, value ? 1 : 0); Apply(); }
        }

        /// <summary>The cues fired since the last reset, oldest first - the harness reads it to prove a wired event fired its cue. Display bookkeeping only.</summary>
        public static readonly List<AudioCue> Fired = new List<AudioCue>();

        public static AudioCueEntry Entry(AudioCue cue)
        {
            foreach (AudioCueEntry e in Catalog) { if (e.Cue == cue) { return e; } }
            return new AudioCueEntry(cue, null, false, "not in the catalog");
        }

        /// <summary>Fire a cue by its enum. Silent (and cheap) when muted, when the cue has no file, or when the audio system is off (batch mode).</summary>
        public static void Fire(AudioCue cue)
        {
            Fired.Add(cue);
            if (Fired.Count > 256) { Fired.RemoveAt(0); }
            if (Mute) { return; }
            AudioClip clip = Clip(cue);
            if (clip == null) { return; }
            AudioSource source = Source();
            if (source == null) { return; }
            source.PlayOneShot(clip, Volume);
        }

        /// <summary>The slider's step cue, rate-limited to one every 40 ms so a fast drag ticks rather than buzzes.</summary>
        public static void FireStep()
        {
            float now = Time.unscaledTime;
            if (_lastStepAt >= 0f && now - _lastStepAt < 0.04f) { return; }
            _lastStepAt = now;
            Fire(AudioCue.SliderStep);
        }

        /// <summary>Fire a cue by its name (the harness's trigger: `AudioCueBoard` in the Editor, `-cuesweep` on the driver). False when the name is not a cue.</summary>
        public static bool Fire(string cueName)
        {
            if (Enum.TryParse(cueName, true, out AudioCue cue)) { Fire(cue); return true; }
            return false;
        }

        /// <summary>The clip a cue resolves to, loaded once; null for a cue without a file. The load result is remembered either way.</summary>
        public static AudioClip Clip(AudioCue cue)
        {
            if (Clips.TryGetValue(cue, out AudioClip cached)) { return cached; }
            if (Missing.Contains(cue)) { return null; }
            AudioCueEntry entry = Entry(cue);
            AudioClip clip = string.IsNullOrEmpty(entry.File) ? null : Resources.Load<AudioClip>(ResourceFolder + "/" + entry.File);
            if (clip == null) { Missing.Add(cue); return null; }
            Clips[cue] = clip;
            return clip;
        }

        private static void ReadPrefs()
        {
            if (_prefsRead) { return; }
            _prefsRead = true;
            _volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePref, 0.8f));
            _mute = PlayerPrefs.GetInt(MutePref, 0) == 1;
        }

        private static void Apply()
        {
            if (_source != null) { _source.mute = _mute; }
        }

        private static AudioSource Source()
        {
            if (_source != null) { return _source; }
            if (!Application.isPlaying) { return null; }
            var host = new GameObject("PoliSim Audio");
            UnityEngine.Object.DontDestroyOnLoad(host);
            _source = host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.mute = Mute;
            if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null) { host.AddComponent<AudioListener>(); }
            return _source;
        }
    }
}
