using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoliSim.Data;
using PoliSim.Simulation;

namespace PoliSim.Persistence
{
    /// <summary>Thrown for every refused or failed load, always with a message written for the
    /// player-facing surface that will eventually show it - never a bare stack trace.</summary>
    public class SaveLoadException : Exception
    {
        public SaveLoadException(string message) : base(message) { }
    }

    /// <summary>
    /// Serialization, version gate, atomic file IO, and the zone-identity assert - the whole
    /// mechanism half of item 8 (the mechanism report in CLAUDE.md is the reference; the shapes are
    /// in SaveGame.cs). Engine-free except for <see cref="DefaultSaveDirectory"/>, so the Editor
    /// diagnostic can run all of it in batch mode against in-memory strings and temp paths.
    /// </summary>
    public static class SaveGameService
    {
        /// <summary>
        /// The format epoch. Ruling A (2026-08-16): additive model changes do NOT bump this -
        /// MissingMemberHandling.Ignore plus field defaults absorb them for free. A model SWAP that
        /// re-keys or replaces persisted state (item 10's political overhaul is the named case:
        /// ParliamentSeats is keyed by PartyArchetype, which it retires) BUMPS this, and the loader
        /// refuses the older save with a plain message. No migration machinery pre-release.
        /// </summary>
        // W-G1 (2026-08-30) bumped this 1 -> 2: `Country.ParliamentSeats` was re-keyed from
        // PartyArchetype (four shared fictional archetypes) to the real parties of each country.
        // This is precisely the "model SWAP that re-keys persisted state" the paragraph above names,
        // and older saves are refused with a plain message rather than migrated.
        public const int CurrentSaveVersion = 2;

        /// <summary>
        /// One settings object, built fresh per call (JsonSerializerSettings is mutable; sharing a
        /// static instance invites a distant caller to reconfigure everyone else's serialization).
        ///
        /// - **PreserveReferencesHandling.Objects** is hazard 1: Germany/France/Italy hold ONE
        ///   CurrencyZone instance, and only $id/$ref keeps it one object through a round trip.
        ///   Objects, not All: no list in the graph is shared, and array tracking would only add
        ///   noise to the populate-into-readonly path.
        /// - **MissingMemberHandling.Ignore** + TypeNameHandling.None are the version-tolerance
        ///   posture (additive drift free, no $type coupling to type names).
        /// - **ObjectCreationHandling stays Auto** deliberately: every readonly collection in the
        ///   graph is initialized EMPTY at declaration, so populate-in-place is correct - and the
        ///   round-trip diagnostic asserts collection counts precisely because this reasoning is a
        ///   claim about the graph, not about Json.NET (hazard 3).
        /// </summary>
        public static JsonSerializerSettings BuildSettings()
        {
            return new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                Formatting = Formatting.Indented,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
            };
        }

        /// <summary>
        /// Builds the root save object from the live game. <paramref name="ui"/> is null for saves
        /// written by batch tools. <paramref name="savedAtUtc"/> exists so the diagnostic can pin the
        /// one nondeterministic field and compare whole serialized saves for equality; real callers
        /// omit it.
        /// </summary>
        public static SaveGame CreateSaveGame(SimulationManager sim, World world, CountryId playerCountryId, UiDraftState ui, DateTime? savedAtUtc = null)
        {
            return new SaveGame
            {
                SaveVersion = CurrentSaveVersion,
                SavedAtUtc = savedAtUtc ?? DateTime.UtcNow,
                PlayerCountryId = playerCountryId,
                MasterSeed = SimulationRandom.MasterSeed,
                RngDrawCounts = SimulationRandom.CaptureDrawCounts(),
                CurrentTurn = sim.CurrentTurn,
                CurrentDate = sim.CurrentDate,
                CurrencyZoneGroups = CaptureZoneGroups(world),
                World = world,
                Sim = sim.CaptureSaveState(),
                Ui = ui
            };
        }

        /// <summary>The zone-identity partition as it actually exists in the live graph: one inner
        /// list per distinct CurrencyZone INSTANCE (reference identity, not value equality - two
        /// zones at the same rate are still two zones).</summary>
        public static List<List<CountryId>> CaptureZoneGroups(World world)
        {
            var groups = new List<List<CountryId>>();
            var zoneToGroup = new Dictionary<CurrencyZone, List<CountryId>>();
            foreach (Country country in world.Countries)
            {
                if (country.CurrencyZone == null)
                {
                    continue;
                }

                if (!zoneToGroup.TryGetValue(country.CurrencyZone, out List<CountryId> group))
                {
                    group = new List<CountryId>();
                    zoneToGroup[country.CurrencyZone] = group;
                    groups.Add(group);
                }

                group.Add(country.Id);
            }

            return groups;
        }

        public static string Serialize(SaveGame save)
        {
            return JsonConvert.SerializeObject(save, BuildSettings());
        }

        /// <summary>
        /// The version gate runs BEFORE the full deserialize, against the raw parse - a save from
        /// after a model swap may not even deserialize into today's types, and the player deserves
        /// the version message rather than whatever exception that would produce. Then hazard 1's
        /// restore assert runs on the result, so no caller can obtain a world with a silently split
        /// currency zone.
        /// </summary>
        public static SaveGame Deserialize(string json)
        {
            int version;
            try
            {
                JObject root = JObject.Parse(json);
                version = root.Value<int?>("SaveVersion") ?? -1;
            }
            catch (JsonException)
            {
                throw new SaveLoadException("This file is not a PoliSim save (it does not parse as one).");
            }

            if (version != CurrentSaveVersion)
            {
                throw new SaveLoadException(
                    $"This save uses format version {version}; this build reads version {CurrentSaveVersion}. " +
                    "Pre-release saves do not carry across simulation-model changes - start a new game. (Ruling A, 2026-08-16.)");
            }

            SaveGame save = JsonConvert.DeserializeObject<SaveGame>(json, BuildSettings());
            if (save?.World == null || save.World.Countries == null || save.World.Countries.Count == 0)
            {
                throw new SaveLoadException("This save parsed but carries no world - it is truncated or corrupt.");
            }

            AssertZoneIdentity(save);
            return save;
        }

        /// <summary>
        /// Hazard 1's assert: the restored graph must reproduce the saved zone partition as
        /// REFERENCE identity - every country in a recorded group shares literally one CurrencyZone
        /// object, and no two distinct groups share one. Throws rather than warns: a world that
        /// fails this looks healthy and simulates wrong (a Eurozone rate change stops reaching two
        /// of its three members), which is the worst available failure shape.
        /// </summary>
        public static void AssertZoneIdentity(SaveGame save)
        {
            var seenZones = new List<CurrencyZone>();
            foreach (List<CountryId> group in save.CurrencyZoneGroups)
            {
                CurrencyZone shared = null;
                foreach (CountryId id in group)
                {
                    Country country = save.World.GetCountry(id);
                    if (country?.CurrencyZone == null)
                    {
                        throw new SaveLoadException($"Corrupt save: {id} lost its currency zone in the round trip.");
                    }

                    if (shared == null)
                    {
                        shared = country.CurrencyZone;
                    }
                    else if (!ReferenceEquals(shared, country.CurrencyZone))
                    {
                        throw new SaveLoadException(
                            $"Corrupt save: the shared currency zone containing {group[0]} restored as more than one object - " +
                            "a rate change would no longer reach every member. Refusing the load.");
                    }
                }

                foreach (CurrencyZone earlier in seenZones)
                {
                    if (ReferenceEquals(earlier, shared))
                    {
                        throw new SaveLoadException("Corrupt save: two distinct currency zones restored as one object.");
                    }
                }

                seenZones.Add(shared);
            }
        }

        /// <summary>
        /// The one restore orchestrator, used by BOTH real load and the diagnostic - RNG restore and
        /// manager restore travel together on purpose (the AdvanceCountryDayTick lesson: two callers
        /// each carrying the list is how the list drifts). The caller still owns re-resolving its own
        /// references INTO the restored world by id (hazard 2) - see GameController.RestoreFromSave.
        /// </summary>
        public static void RestoreInto(SimulationManager sim, SaveGame save)
        {
            SimulationRandom.RestoreState(save.MasterSeed, save.RngDrawCounts);
            sim.RestoreSaveState(save.World, save.CurrentTurn, save.CurrentDate, save.Sim);
        }

        /// <summary>Atomic: serialize beside the target, then swap - a crash mid-save must never
        /// destroy the previous save, or this system reintroduces the exact loss it exists to
        /// prevent. The previous save survives as .bak.</summary>
        public static void SaveToFile(string path, SaveGame save)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tmp = path + ".tmp";
            File.WriteAllText(tmp, Serialize(save));
            if (File.Exists(path))
            {
                File.Replace(tmp, path, path + ".bak");
            }
            else
            {
                File.Move(tmp, path);
            }
        }

        public static SaveGame LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new SaveLoadException($"No save file at {path}.");
            }

            return Deserialize(File.ReadAllText(path));
        }

        /// <summary>Outside the repository by construction (the repository-weight finding made that a
        /// requirement): %USERPROFILE%\AppData\LocalLow\&lt;company&gt;\PoliSim\saves on Windows.</summary>
        public static string DefaultSaveDirectory => Path.Combine(UnityEngine.Application.persistentDataPath, "saves");

        public static string DefaultSlotPath => Path.Combine(DefaultSaveDirectory, "slot1.json");

        /// <summary>One save file as the saves MENU sees it - the root header fields read without a
        /// full deserialize. An unreadable or wrong-version file still LISTS (Compatible=false with
        /// the reason), because a save that silently vanishes from the menu reads as data loss.</summary>
        public class SaveHeader
        {
            public string Path;
            public string Name;
            public bool Compatible;
            public int Version = -1;
            public CountryId PlayerCountryId;
            public int Turn;
            public DateTime Date;
            public DateTime SavedAtUtc;
            public string Error;
        }

        /// <summary>
        /// Every *.json in <paramref name="directory"/> as a header row, newest save first. Parses
        /// the whole file (JObject has no partial mode) but touches only root fields - a handful of
        /// ~1 MB parses on menu open, never per frame; the menu caches the result and refreshes only
        /// after its own save/delete actions. `.tmp`/`.bak` siblings are deliberately not listed -
        /// the .bak story is the menu's footer text, not a second row per save.
        /// </summary>
        public static List<SaveHeader> ListSaves(string directory)
        {
            var headers = new List<SaveHeader>();
            if (!Directory.Exists(directory))
            {
                return headers;
            }

            foreach (string path in Directory.GetFiles(directory, "*.json"))
            {
                var header = new SaveHeader
                {
                    Path = path,
                    Name = System.IO.Path.GetFileNameWithoutExtension(path)
                };
                headers.Add(header);
                try
                {
                    JObject root = JObject.Parse(File.ReadAllText(path));
                    header.Version = root.Value<int?>("SaveVersion") ?? -1;
                    if (header.Version != CurrentSaveVersion)
                    {
                        header.Error = $"format {header.Version} - this build reads {CurrentSaveVersion}";
                        continue;
                    }

                    header.PlayerCountryId = (CountryId)System.Enum.Parse(typeof(CountryId), root.Value<string>("PlayerCountryId") ?? "", true);
                    header.Turn = root.Value<int?>("CurrentTurn") ?? 0;
                    header.Date = root.Value<DateTime?>("CurrentDate") ?? default;
                    header.SavedAtUtc = root.Value<DateTime?>("SavedAtUtc") ?? default;
                    header.Compatible = true;
                }
                catch (Exception e)
                {
                    header.Error = e is JsonException ? "not a readable save" : e.Message;
                }
            }

            headers.Sort((a, b) => b.SavedAtUtc.CompareTo(a.SavedAtUtc));
            return headers;
        }

        /// <summary>Deletes a save and its .bak/.tmp siblings together - stated in the menu's footer,
        /// so "delete" never leaves a ghost backup the player believes is gone.</summary>
        public static void DeleteSave(string path)
        {
            foreach (string target in new[] { path, path + ".bak", path + ".tmp" })
            {
                if (File.Exists(target))
                {
                    File.Delete(target);
                }
            }
        }

        /// <summary>A player-typed save name reduced to a safe filename stem - invalid filename
        /// characters and dots stripped (dots would collide with the .bak/.tmp sibling scheme),
        /// whitespace collapsed. Empty result = not saveable under this name.</summary>
        public static string SanitizeSaveName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "";
            }

            var builder = new System.Text.StringBuilder(name.Length);
            foreach (char c in name.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ')
                {
                    builder.Append(c == ' ' ? '_' : c);
                }
            }

            return builder.ToString();
        }
    }
}
