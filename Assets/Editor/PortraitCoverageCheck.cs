using System;
using System.Collections.Generic;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Asks whether every PORTRAIT the game can ask for resolves through the same <c>Resources.Load</c>
    /// path the roster draws with - <see cref="IconLibrary.GetCabinetPortrait"/> and
    /// <see cref="IconLibrary.GetFedChairPortrait"/> - rather than whether files exist on disk.
    ///
    /// Run: <c>Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.PortraitCoverageCheck.Run -logFile &lt;path&gt;</c>, or from the suite.
    ///
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 14 - cite a check by its enumeration, never by its
    /// intent): every <see cref="CabinetMinister"/> in <c>CabinetSystem.CandidatePool</c> across every
    /// portfolio, every <see cref="FedChair"/> in <c>FederalReserveSystem.CandidatePool</c>, and every
    /// sitting <c>Country.CurrentFedChair</c> in <c>WorldFactory.CreateDefault()</c>. It does NOT
    /// enumerate <c>Portraits/</c> on disk - counting the folder gives "N of N" forever, including after
    /// a name is added to a pool with no art (the <c>PartyMarkCoverageCheck</c> lesson: enumerate the
    /// display side, not the storage side). The pools are private statics and are read by reflection,
    /// so the check derives from the pool the roster actually draws from and never keeps a name list
    /// of its own that could drift.</para>
    ///
    /// <para><b>Why a gap here is invisible in play.</b> <c>IconLibrary</c>'s contract is
    /// null-on-missing, and the roster degrades to the procedural placeholder by design - the right
    /// runtime behaviour, since drawing someone else's face would actively mislead. So a pool member
    /// whose art never landed, or landed under a hand-written <c>.meta</c> that leaves it unloadable, is
    /// silent everywhere except here. A pool member with no portrait FAILS the check. The sitting turn-0
    /// chair is reported separately and does not fail it: that seed name is deliberately outside the
    /// candidate pool and no portrait was ever requested for it (a roadmap question, not a gap).</para>
    ///
    /// Born 2026-08-27 with the Progress5 import (the batch of eight completing the cabinet set), because
    /// no check enumerated portraits until then - their coverage had only ever been asserted from the
    /// filesystem.
    /// </summary>
    public static class PortraitCoverageCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();

            // SELF-TEST FIRST: a known-present portrait must load through the real accessor, or every
            // "MISSING" below is a broken probe rather than a real gap.
            Texture2D reference = IconLibrary.GetCabinetPortrait(CabinetPortfolio.FinanceTreasury, "Elena Voskresenskaya");
            Debug.Log("SELFTEST portrait_cabinet_financetreasury_elena_voskresenskaya -> " +
                      (reference != null ? $"{reference.width}x{reference.height} {reference.format} OK" : "NULL - BROKEN, results below are void"));

            var ministers = ReadPool<Dictionary<CabinetPortfolio, List<CabinetMinister>>>(typeof(CabinetSystem), "CandidatePool");
            var chairs = ReadPool<List<FedChair>>(typeof(FederalReserveSystem), "CandidatePool");
            if (ministers == null || ministers.Count == 0 || chairs == null || chairs.Count == 0)
            {
                // ⚠ AN EMPTY ENUMERATION IS NOT A PASS. If reflection finds nothing, the pools were
                // renamed or restructured and this check must be re-pointed - it has verified nothing.
                Debug.LogError("  EMPTY ENUMERATION - CabinetSystem.CandidatePool or FederalReserveSystem.CandidatePool " +
                               "not found by reflection (renamed? no longer static?). VERIFIED NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            int total = 0, missing = 0, ministerCount = 0;
            foreach (KeyValuePair<CabinetPortfolio, List<CabinetMinister>> entry in ministers)
            {
                foreach (CabinetMinister minister in entry.Value)
                {
                    total++;
                    ministerCount++;
                    Texture2D portrait = IconLibrary.GetCabinetPortrait(minister.Portfolio, minister.Name);
                    if (portrait == null)
                    {
                        Debug.Log($"  MISSING   {minister.Portfolio,-20} {minister.Name} -> does not resolve through Resources.Load");
                        missing++;
                    }
                    else
                    {
                        Debug.Log($"  ok        {minister.Portfolio,-20} {minister.Name,-24} {portrait.width}x{portrait.height} {portrait.format}");
                    }
                }
            }

            foreach (FedChair chair in chairs)
            {
                total++;
                Texture2D portrait = IconLibrary.GetFedChairPortrait(chair.Name);
                if (portrait == null)
                {
                    Debug.Log($"  MISSING   {"FedChair",-20} {chair.Name} -> does not resolve through Resources.Load");
                    missing++;
                }
                else
                {
                    Debug.Log($"  ok        {"FedChair",-20} {chair.Name,-24} {portrait.width}x{portrait.height} {portrait.format}");
                }
            }

            // The sitting chairs of the default world: reported, never counted. No portrait has been
            // requested for a seed name outside the candidate pool; if that changes, this line is where
            // the new ask will first be visible.
            int sitting = 0;
            World world = WorldFactory.CreateDefault();
            foreach (Country country in world.Countries)
            {
                if (country.CurrentFedChair == null) { continue; }
                sitting++;
                Texture2D portrait = IconLibrary.GetFedChairPortrait(country.CurrentFedChair.Name);
                Debug.Log($"  sitting   {country.Id,-20} {country.CurrentFedChair.Name,-24} " +
                          (portrait != null ? $"{portrait.width}x{portrait.height} {portrait.format}" : "no portrait (none requested; not counted)"));
            }

            Debug.Log($"=== Portrait coverage: {total - missing} of {total} pool members resolve through Resources.Load " +
                      $"({ministerCount} ministers across {ministers.Count} portfolios + {chairs.Count} Fed chairs; {sitting} sitting chair(s) reported, not counted; " +
                      "NOT Portraits/ on disk) ===");
            CheckExit.Finish(missing == 0 ? 0 : 1);
        }

        private static T ReadPool<T>(Type owner, string fieldName) where T : class
        {
            FieldInfo field = owner.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            return field?.GetValue(null) as T;
        }
    }
}
