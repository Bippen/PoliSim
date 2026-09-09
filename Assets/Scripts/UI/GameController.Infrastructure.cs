using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// P5-C4 (2026-09-06) - THE INFRASTRUCTURE FAMILY'S PLATE on the shared core (9c, inherited by shape): road quality → KEY·BOUNDED with a DATED
    /// chip (WEF 2019 - the survey ended with that edition, so the year prints with the figure); road connectivity → KEY·OPEN 70–100; congestion → a
    /// FETCH row (the TomTom Traffic Index is a web report with no file); road length → ABSENT and stated (the IRF World Road Statistics is paid).
    /// Home: the People page's society block (the catalog's column), the plate whole under education.
    /// </summary>
    public partial class GameController
    {
        private Rect _infrastructurePlateLastArea;

        private void DrawInfrastructureFamilyPlate()
        {
            Country country = _playerCountry;
            InfrastructureSeeds f = country.Infrastructure;
            string countryName = DisplayName.Of(country.Id.ToString()).ToUpperInvariant();
            PlateFamily("Infrastructure", "2019–24", "SOURCED · WEF GCR · OECD");
            if (f == null || !f.Seeded)
            {
                GUILayout.Label("This country carries no infrastructure family - the spine covers six, and this is not one of them.", _labelStyle);
                return;
            }

            EconomyState s = country.State;
            StatHistory history = country.History;
            bool draftLive = false;
            float draftSpending = 0f, standingSpending = 0f;
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (!InfrastructureFamily.IsInfrastructureLine(line.Category)) { continue; }
                standingSpending += line.Amount;
                if (_spendingLineInputs.TryGetValue(line.Category, out float drafted)) { draftSpending += drafted; draftLive = true; }
                else { draftSpending += line.Amount; }
            }

            var rows = new List<PlateRow>
            {
                new PlateRow("Road quality", "SCORE 0–100 · SURVEY 2018–19 · DATED 2019", "WEF GCR 2019 · ROADINF · RANK " + f.RoadQualityRank + " OF 141", PlateFigure(s.RoadQuality, 1),
                    PlateBand.Bounded, 50f, 100f, s.RoadQuality, InfrastructurePeers(x => x.RoadQuality), false, new[] { "INFRASTRUCTURE LINE — PER HEAD ▸", "DECAY ▸" }, history?.RoadQuality.Quarterly, new[] { "SOURCED", "DATED 2019" }, true, flag: DeskProvenance.DatedGlyph),
                new PlateRow("Road connectivity", "INDEX 0–100 · TEN-CITY TRAVEL SPEEDS", "WEF GCR 2019 · ROADQUALIDX", PlateFigure(s.RoadConnectivity, 1),
                    PlateBand.Open, 70f, 100f, s.RoadConnectivity, InfrastructurePeers(x => x.RoadConnectivity), false, new[] { "ROAD QUALITY ▸", "POPULATION ▸" }, history?.RoadConnectivity.Quarterly, new[] { "SOURCED" }, true),
                new PlateRow("Congestion", "% EXTRA TRAVEL TIME · PER CITY", "TOMTOM INDEX 2024 · INRIX SCORECARD 2024", "billed",
                    PlateBand.Absent, 0f, 60f, -1f, null, true, new[] { "ROAD CONNECTIVITY ▸" }, null, new[] { "BILLED" }, false, "BILLED · A PAGE AND A CHART, NO DATA · CONNECTIVITY STANDS IN"),
                new PlateRow("Road length", "KM · THE NETWORK", "IRF WORLD ROAD STATISTICS · PAID", "absent",
                    PlateBand.Absent, 0f, 1f, -1f, null, false, new[] { "NOT SIMULATED" }, null, new[] { "ABSENT · STATED" }, false, "NO OPEN FILE PUBLISHES IT · NEVER 0, NEVER A DASH"),
            };

            Color areaInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Infrastructure);
            string footText = "SEEDS: THE WEF GCI 4.0 2019 DATASET, VERIFIED BY CONTENT · THE ROAD-QUALITY SURVEY ENDED WITH THAT EDITION - THE FIGURE IS DATED AND SAYS SO · THE OWN TICK IS THIS COUNTRY, THE SHORT TICKS THE OTHER FIVE AT SEED · BILLED AND ABSENT ARE WORDS · COUPLINGS: THE INFRASTRUCTURE SPINE'S TABLES, DRAFT UNTIL MEASURED";
            _infrastructurePlateLastArea = DrawPlateRows(rows, areaInk, footText, draftLive, row =>
            {
                if (!draftLive || !row.Name.StartsWith("Road quality")) { return null; }
                float with = InfrastructureFamily.ProjectRoadQuality(country, draftSpending);
                float without = InfrastructureFamily.ProjectRoadQuality(country, standingSpending);
                return (with - without, false, "PTS");
            });
        }

        private float[] InfrastructurePeers(System.Func<InfrastructureSeeds, float> read)
        {
            var peers = new List<float>();
            World world = _simulationManager.World;
            if (world == null) { return peers.ToArray(); }
            foreach (CountryId id in PeerOrder)
            {
                if (id == PlayerCountryId) { continue; }
                Country c = world.GetCountry(id);
                if (c?.Infrastructure == null || !c.Infrastructure.Seeded) { continue; }
                float v = read(c.Infrastructure);
                if (v >= 0f) { peers.Add(v); }
            }
            return peers.ToArray();
        }
    }
}
