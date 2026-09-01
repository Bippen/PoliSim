using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PoliSim.UI;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Asks whether every SEEDED POLITICAL PARTY has a ballot mark that resolves and imported correctly.
    ///
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 14): every party returned by every static
    /// <c>BuildParties()</c> in the loaded assemblies — the party list, which is the display side. Not
    /// the contents of <c>Emblems/</c>.</para>
    ///
    /// ⚠ **IT ENUMERATED THE MARK FILES UNTIL 2026-08-11, AND THAT WAS THE WRONG SIDE.** Counting the
    /// folder gives "4 of 4" and keeps giving it forever — including after a fifth party exists with no
    /// mark, because a party with no art contributes no file to count. That is exactly the
    /// `icon_stat_interestrate` lesson (*enumerate the display enum, not the storage struct*) applied to
    /// parties, and it was **already live rather than hypothetical**: the US seed carries FOUR parties
    /// and TWO `MarkName`s, so the folder-enumerated version reported green while half the seeded parties
    /// had no mark at all.
    ///
    /// <para>It matters more from here, not less. The Sweden and Poland electoral work is in flight and
    /// the four-archetype model is retiring; Sweden alone seats eight parties. A folder-enumerated check
    /// would report green while eight parties shared two drawings.</para>
    ///
    /// <para><b>Both directions, because they fail differently.</b> A party with no mark is a GAP the UI
    /// tolerates by design (the mark accessor returns null and every call site draws
    /// the row without one) — so it is reported, never fatal. Two parties sharing one mark is a LIE: the
    /// hemicycle renders them identically and the player reads one bloc where there are two. That fails.
    /// </para>
    ///
    /// ⚠ **Reflection rather than a direct reference, deliberately.** The seed types are new and still
    /// moving; a compile-time reference would make this check a build dependency of theirs and break the
    /// Editor if they are renamed or absent. Reflection also means a NEW country's seed is covered the
    /// day it lands, with no edit here — the same derive-don't-list argument the enumeration fix is about.
    /// </summary>
    public static class PartyMarkCoverageCheck
    {
        /// <summary>
        /// ⚠ **DELIVERED MARKS THAT NO SEEDED PARTY CLAIMS — a ratchet at 1, measured 2026-09-01.**
        ///
        /// <para>The one is **`mark_party_us_lib`**: the file is real and **no Libertarian party is
        /// seeded**, so nothing can claim it. ⚠ **It is not "fixed" by inventing a party to consume a
        /// file** — that is the tail wagging the dog, and the file waits until the USA's seed has a reason
        /// to include one.</para>
        ///
        /// <para>The other three — `mark_party_se_v`, `mark_party_us_rep`, `mark_party_us_dem` — **were
        /// orphans for days after W-G1 seeded their parties**, under an output line that called orphans
        /// *"not a defect: art precedes the seed by design"*. True when written, wrong once the seeds
        /// landed, and nothing re-read it. **They are claimed now. Lower this ceiling as each remaining
        /// one is claimed; never raise it.**</para>
        /// </summary>
        private const int UnconsumedCeiling = 0;

        /// <summary>⚠ **Marks that NO SEED CAN EVER CLAIM, each with the arithmetic for why.** Distinct
        /// from the ratchet above, which is for art WAITING on a seed: a row here is art whose party has
        /// no place in the model as the model is defined, so waiting is not what it is doing.
        ///
        /// <para>⚠ **It is policed**, because "no party can claim it" is the sentence that would retire an
        /// inconvenient mark: an entry naming a file that is not on disk fails, and an entry naming a mark
        /// that a seeded party DOES claim fails.</para></summary>
        private static readonly (string Mark, string Reason)[] NoSeedCanClaim =
        {
            ("mark_party_us_lib",
             "⚠ ARITHMETIC, not an omission. `PartySystem` seeds the USA by SEATS in the House, whose "
             + "SOURCED size is 435 - and it seeds REP 220 + DEM 215 = 435, the WHOLE chamber. There is no "
             + "seat for a third party to hold, so a Libertarian party cannot be seeded without taking a "
             + "seat from one of the two that hold them, which would be inventing a result. The file is "
             + "kept rather than retired: it is delivered art, and the day this model gains a vote-share "
             + "dimension the USA's third parties live in, it is already drawn.")
        };

        private const string EmblemFolder = "Assets/Resources/Art/UI/Emblems";
        private const string MarkResourcePath = "Art/UI/Emblems/";

        public static void Run()
        {
            // SELF-TEST FIRST: if a known-good mark does not load, every "missing" below is a broken
            // probe rather than a real gap.
            //
            // W-G1 moved the probe from a retired archetype EMBLEM to a delivered real MARK. Its job
            // is unchanged; `mark_party_se_s` is the one mark that exists, which makes it the only
            // honest probe available.
            Texture2D reference = Resources.Load<Texture2D>(MarkResourcePath + "mark_party_se_s");
            Debug.Log($"SELFTEST mark_party_se_s -> " +
                      $"{(reference != null ? "OK" : "NULL - BROKEN, results below are void")}");

            List<(string party, string mark)> parties = CollectSeededParties();
            if (parties.Count == 0)
            {
                // ⚠ NOT PRESENT and BROKEN are different, and only one of them is a failure. The real
                // party system lives on a feature branch; on a branch without it there is no claim for
                // this check to falsify, so a red here would be permanent noise of exactly the kind that
                // teaches people to stop reading a check. It still says loudly that it verified nothing.
                bool partyTypeExists = AppDomain.CurrentDomain.GetAssemblies()
                    .Any(a => a.GetType("PoliSim.Data.PoliticalParty", false) != null);

                if (!partyTypeExists)
                {
                    Debug.LogWarning("  PARTY SYSTEM NOT PRESENT on this branch - PoliSim.Data.PoliticalParty " +
                                     "does not exist, so there are no seeded parties to check. " +
                                     "VERIFIED NOTHING; this is not evidence of coverage.");
                    CheckExit.Finish(0);
                    return;
                }

                Debug.LogError("  NO PARTY SEEDS FOUND - PoliticalParty exists but no static BuildParties() " +
                               "returned any. Either the seeds were removed or this check's discovery is " +
                               "broken; either way it is not evidence of coverage.");
                CheckExit.Finish(1);
                return;
            }

            int errors = 0, gaps = 0;
            var claimed = new Dictionary<string, string>();

            foreach ((string party, string mark) in parties)
            {
                if (string.IsNullOrEmpty(mark))
                {
                    // Tolerated by design, but INVISIBLE to a folder-enumerated check, which is the whole
                    // reason this one enumerates parties.
                    Debug.LogWarning($"  no mark   {party} - MarkName unset; the row draws without one");
                    gaps++;
                    continue;
                }

                if (claimed.TryGetValue(mark, out string firstOwner))
                {
                    Debug.LogError($"  SHARED    {party} and {firstOwner} both use '{mark}'. " +
                                   $"Two parties rendering identically reads as one bloc.");
                    errors++;
                    continue;
                }

                claimed[mark] = party;

                // ⚠ Resources.Load DIRECTLY, not IconLibrary.GetPartyMark, and this is a correction.
                // The accessor lives on the politics feature branch alongside the seeds; referencing it
                // compiled here only because this check was verified while that branch's uncommitted
                // code was still sitting in the working tree. On `main` it is CS0117 and takes the whole
                // Editor assembly down with it. GetPartyMark is a one-line wrapper over exactly this
                // call, so nothing is lost by asking Resources directly - and the check now compiles on
                // any branch, which is the property it needed.
                Texture2D texture = Resources.Load<Texture2D>(MarkResourcePath + mark);
                if (texture == null)
                {
                    Debug.LogError($"  MISSING   {party} -> '{mark}' does not resolve through Resources.Load");
                    errors++;
                    continue;
                }

                // ⚠ KEPT FROM THE EXTENDED VERSION, and it is the half that caught the real defect.
                // Resolution proves the GUID, the path and that the meta parses. It says nothing about
                // whether block compression took effect - and compression on white-on-alpha at icon size
                // is the documented damage vector. All four marks resolved at 128x128 while every one was
                // DXT5. `ImporterSettingsCheck` now asserts this across every sprite under Art/UI (149
                // when this was written; its own summary line carries the live count); it stays here
                // too because this check is what a party-facing failure should name.
                if (texture.format != TextureFormat.RGBA32)
                {
                    Debug.LogError($"  DAMAGED   {party} -> '{mark}' imported {texture.format}, expected RGBA32. " +
                                   $"Marks are white-on-alpha and tinted at draw time.");
                    errors++;
                    continue;
                }

                Debug.Log($"  ok        {party} -> {mark} {texture.width}x{texture.height} {texture.format}");
            }

            // The other direction: art delivered ahead of the party that will use it.
            //
            // ⚠ ORPHANS BY SEQUENCING ARE NOT DEFECTS, and saying so in the output is the point.
            // `mark_party_se_*` (Sweden's banner and star) landed before Sweden's seed exists, which is
            // the INTENDED order - Design was asked for a proof of concept on three parties rather than
            // forty precisely so a batch would not be drawn ahead of the screens that use it. Reported so
            // "delivered" and "used" do not silently diverge, and worded so it is not re-triaged as a gap
            // on every future run.
            // ⚠ THE LINE ABOVE WAS TRUE WHEN IT WAS WRITTEN AND WENT WRONG WHEN THE SEEDS LANDED
            // (corrected 2026-09-01). "Orphan by sequencing, not a defect" was right while Sweden had no
            // seeded parties. W-G1 then seeded 53 real ones — and `mark_party_se_v`, `mark_party_us_rep`
            // and `mark_party_us_dem` went on sitting on disk, delivered and claimed by nobody, for days.
            // **Delivered art that no seed consumes is S-32's class inside the asset pipeline**: the file
            // is real, every check is green, and nothing draws it.
            //
            // Orphans are still REPORTED with their reason — a batch really can precede its seeds — but
            // they are RATCHETED now, so an orphan that COULD be claimed cannot wait forever.
            var orphans = new List<string>(OrphanMarks(claimed.Keys));

            // ⚠ Separate the art that is WAITING from the art that CANNOT be claimed. Both were counted as
            // one number, so a permanent absence sat on a ratchet that could never reach zero - and a
            // ratchet that cannot reach zero teaches its readers to stop expecting it to.
            var permanent = new List<string>();
            var deadExemptions = new List<string>();
            foreach (var x in NoSeedCanClaim)
            {
                if (claimed.ContainsKey(x.Mark)) { deadExemptions.Add(x.Mark + " (a seeded party DOES claim it)"); continue; }
                if (!orphans.Contains(x.Mark)) { deadExemptions.Add(x.Mark + " (no such delivered mark)"); continue; }

                orphans.Remove(x.Mark);
                permanent.Add(x.Mark);
                Debug.Log("  permanent " + x.Mark + " - " + x.Reason);
            }

            foreach (string d in deadExemptions)
            {
                errors++;
                Debug.LogError("  DEAD EXEMPTION " + d + ". ⚠ An exemption that excuses nothing reads as coverage and "
                               + "outlives what it named. Delete it or fix what it names.");
            }

            foreach (string orphan in orphans)
            {
                Debug.Log($"  awaiting  {orphan} - delivered, and no seeded party claims it.");
            }

            RatchetLedger.Report("PartyMarkCoverageCheck.UNCONSUMED", orphans.Count, UnconsumedCeiling);

            if (orphans.Count > UnconsumedCeiling)
            {
                errors++;
                Debug.LogError($"  UNCONSUMED {orphans.Count} delivered mark(s) that no seeded party claims, above the "
                               + $"recorded ceiling of {UnconsumedCeiling}: {string.Join(", ", orphans.ToArray())}. "
                               + "⚠ Claim it on the party it was drawn for, or record why no party can — delivered art "
                               + "nothing consumes is queued art, and queued art is indistinguishable from art that was "
                               + "never delivered. LOWER the ceiling as each is claimed; never raise it.");
            }

            Debug.Log($"=== Party marks: {parties.Count} seeded part(ies), {claimed.Count} with a resolving mark, " +
                      $"{gaps} without one, {orphans.Count} delivered-but-unconsumed (ceiling {UnconsumedCeiling}), " +
                      $"{errors} error(s) ===");
            CheckExit.Finish(errors == 0 ? 0 : 1);
        }

        /// <summary>Every party from every seed, found by shape rather than by name so a new country is covered without an edit here.</summary>
        private static List<(string party, string mark)> CollectSeededParties()
        {
            var found = new List<(string, string)>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    MethodInfo builder = type.GetMethod("BuildParties",
                        BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                    if (builder == null)
                    {
                        continue;
                    }

                    if (!(builder.Invoke(null, null) is IEnumerable list))
                    {
                        continue;
                    }

                    foreach (object party in list)
                    {
                        if (party == null)
                        {
                            continue;
                        }

                        Type partyType = party.GetType();
                        string label = ReadString(party, partyType, "EnglishName")
                                       ?? ReadString(party, partyType, "Id")
                                       ?? partyType.Name;
                        found.Add(($"{type.Name}/{label}", ReadString(party, partyType, "MarkName")));
                    }
                }
            }

            return found;
        }

        private static string ReadString(object instance, Type type, string member)
        {
            FieldInfo field = type.GetField(member, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(instance) as string;
            }

            PropertyInfo property = type.GetProperty(member, BindingFlags.Public | BindingFlags.Instance);
            return property?.GetValue(instance) as string;
        }

        private static IEnumerable<string> OrphanMarks(ICollection<string> used)
        {
            if (!Directory.Exists(EmblemFolder))
            {
                yield break;
            }

            foreach (string path in Directory.GetFiles(EmblemFolder, "mark_party_*.png", SearchOption.TopDirectoryOnly)
                         .OrderBy(p => p))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (!used.Contains(name))
                {
                    yield return name;
                }
            }
        }
    }
}
