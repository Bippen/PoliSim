using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// K58 (ruled 2026-10-23): "the average minister's loyalty, knowledge and popularity are the pool's means, not 100" - and the ruling's own words that
    /// this rolls occasional leaks and scandals, MEASURED: how often in an idle governing year. On every start, the head's party the player (France's the
    /// governing mode's what-if) and the average cabinet seated as a new game seats it:
    /// <list type="number">
    /// <item><description><b>Under pressure</b> (approval PLANTED below the floor): the year's loyalty roll (`CabinetSystem.TryRollCabinetEvents`, the one the
    /// turn makes) drawn `Trials` times on the cabinet stream, the chairs and approval restored before each - the share of years with at least
    /// one event, the leaks and the resignations per year. It fails while no event rolls (the attributes at 100, as before K58) and while one rolls without
    /// pressure.</description></item>
    /// <item><description><b>How often a year is under pressure</b>: the shared no-policy century's approval after each year's turn (`NoPolicyCentury`, the
    /// player's country with no party and no decision - read as an idle government's, DECLARED: an idle player tables no bill, so no bill is lost and the
    /// approval floor alone decides), the years below the floor.</description></item>
    /// </list>
    /// The product is the reading the ruling asked for: leaks (and resignations) per idle governing year. Printed, not pinned: it is a measurement of the
    /// ruled build, and a run that rolled nothing would fail (1).
    /// </summary>
    public static class AverageMinisterLeakMeasure
    {
        /// <summary>CONVENTION: the Monte Carlo draws per start - enough for the per-year shares to two places; a measurement's sample size, no model figure.</summary>
        private const int Trials = 4000;

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== AverageMinisterLeakMeasure (K58): the average minister at the pool's means - leaks and resignations in an idle governing year ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                sb.Append(F("    the average minister: loyalty {0:F2}, knowledge {1:F2}, popularity {2:F2} (the pool's means), efficiency {3:F2}; the pressure floor approval {4:0.#}\n",
                    CabinetSystem.PoolMeanLoyalty, CabinetSystem.PoolMeanKnowledge, CabinetSystem.PoolMeanPopularity, CabinetSystem.PoolMeanEfficiency, CabinetSystem.PressureApprovalFloor));
                foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France })
                {
                    int eventYears, leaks, resignations, calm;
                    int chairs;
                    using (SimulationManager.EpochScope())
                    {
                        WorldClock.ApplyStart(id);
                        SimulationRandom.Seed(777);
                        EnergyMarket.ResetCalibration();
                        World world = WorldFactory.CreateDefault();
                        Country c = world.GetCountry(id);
                        string party = c.Government?.PmParty;
                        if (WorldClock.GoverningModeOnly(id) && party != null) { c.Government = GovernmentRecord.WhatIfGoverning(c, party, WorldClock.StartDate(id), world); }
                        if (string.IsNullOrEmpty(party)) { Check(false, F("{0}: the start's government names no head's party - nothing to seat", id)); continue; }
                        c.PlayerPartyAbbrev = party;
                        c.CabinetMinisters.Clear();
                        chairs = CabinetSystem.SeatAverageCabinet(c);
                        var seated = new Dictionary<CabinetPortfolio, CabinetMinister>(c.CabinetMinisters);
                        DateTime day = WorldClock.StartDate(id);
                        float pressed = CabinetSystem.PressureApprovalFloor - 5f;
                        eventYears = 0; leaks = 0; resignations = 0;
                        for (int t = 0; t < Trials; t++)
                        {
                            c.CabinetMinisters = new Dictionary<CabinetPortfolio, CabinetMinister>(seated);
                            c.State.ApprovalRating = pressed;
                            List<CabinetEventRecord> fired = CabinetSystem.TryRollCabinetEvents(c, day);
                            if (fired.Count > 0) { eventYears++; }
                            foreach (CabinetEventRecord e in fired) { if (e.Kind == CabinetEventKind.Leak) { leaks++; } else { resignations++; } }
                        }
                        // no pressure: approval above the floor and no bill lost - nothing rolls
                        c.CabinetMinisters = new Dictionary<CabinetPortfolio, CabinetMinister>(seated);
                        c.State.ApprovalRating = CabinetSystem.PressureApprovalFloor + 10f;
                        calm = CabinetSystem.TryRollCabinetEvents(c, day).Count;
                        c.CabinetMinisters = seated;
                    }

                    IReadOnlyList<NoPolicyCentury.Year> century = NoPolicyCentury.For(id);
                    int pressureYears = 0;
                    foreach (NoPolicyCentury.Year y in century) { if (y.ApprovalRating < CabinetSystem.PressureApprovalFloor) { pressureYears++; } }
                    double pPressure = (double)pressureYears / century.Count;
                    double leaksPerPressedYear = (double)leaks / Trials, resignationsPerPressedYear = (double)resignations / Trials, eventShare = (double)eventYears / Trials;
                    Check(eventYears > 0 && leaks > 0 && calm == 0,
                        F("{0}: {1} chairs at the average - under pressure a year rolls an event in {2:P1} of {3} draws ({4:F3} leaks, {5:F3} resignations a year); without pressure {6}; the no-policy century under the floor in {7} of {8} years ({9:P0}) - so {10:F3} leaks and {11:F3} resignations per idle governing year, one leak every {12} years",
                            id, chairs, eventShare, Trials, leaksPerPressedYear, resignationsPerPressedYear, calm == 0 ? "none" : calm + " (FAIL: an event without pressure)",
                            pressureYears, century.Count, pPressure, pPressure * leaksPerPressedYear, pPressure * resignationsPerPressedYear,
                            pPressure * leaksPerPressedYear > 0 ? (1.0 / (pPressure * leaksPerPressedYear)).ToString("0", CultureInfo.InvariantCulture) : "never (the century never under the floor)"));
                }
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { EnergyMarket.ResetTurnState(); }
            sb.Append(failures == 0 ? "=== AverageMinisterLeakMeasure: measured ===" : F("=== AverageMinisterLeakMeasure: {0} FAILED ===", failures));
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
