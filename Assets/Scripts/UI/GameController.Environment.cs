using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// P5-C5 (2026-09-06) - THE ENVIRONMENT FAMILY'S PLATE on the shared core (9c, inherited by shape): total CO₂/head → KEY·OPEN lower ◂ (the headline,
    /// all gases, DERIVED from the sector keys); the power / transport split → the DISTRIBUTION of the key under it (the stacked bar: power, transport,
    /// the rest at seed); power and transport CO₂ per person → KEY·OPEN lower ◂; generation by source → a FETCH row (the family picks the stacked bar,
    /// never a pie). Home: the People page's society block (the catalog's Environment block), the plate whole under infrastructure. While a carbon-tax
    /// draft is live on the Budget's tax rows, 5c's arrow paints on the power row.
    /// </summary>
    public partial class GameController
    {
        private Rect _environmentPlateLastArea;

        private void DrawEnvironmentFamilyPlate()
        {
            Country country = _playerCountry;
            EnvironmentSeeds e = country.Environment;
            string countryName = DisplayName.Of(country.Id.ToString()).ToUpperInvariant();
            DrawCohortCaption($"ENVIRONMENT · SOCIETY · FAMILY 4 OF 6 · {countryName} · {_simulationManager.CurrentDate.Year}", "SOURCED · EDGAR 2024 · WB 2023");
            if (e == null || !e.Seeded)
            {
                GUILayout.Label("This country carries no environment family - the spine covers six, and this is not one of them.", _labelStyle);
                return;
            }

            EconomyState s = country.State;
            StatHistory history = country.History;
            float standingRate = EnvironmentFamily.CarbonTaxRate(country);
            bool draftLive = _taxRateInputs.TryGetValue(TaxType.CarbonTax, out float draftedRate) && !Mathf.Approximately(draftedRate, standingRate);
            float ghg = EnvironmentFamily.GhgPerCapitaNow(country);
            float other = Mathf.Max(0f, ghg - s.PowerCo2PerCapita - s.TransportCo2PerCapita);
            float[] segments = { s.PowerCo2PerCapita, s.TransportCo2PerCapita, other };
            string[] segmentLabels = { "POWER", "TRANSPORT", "THE REST AT SEED" };
            var rows = new List<PlateRow>
            {
                new PlateRow("Emissions per person", "t CO2-eq · ALL GASES · LOWER ◂", "EDGAR 2024 · GHG PER CAPITA · 2023 · DERIVED", PlateFigure(ghg, 2),
                    PlateBand.Open, 0f, 20f, ghg, EnvironmentPeers(x => x.GhgPerCapita), true, new[] { "POWER ▸", "TRANSPORT ▸", "THE REST HELD AT SEED" }, null, new[] { "DERIVED" }, false),
                new PlateRow("… the split", "t CO2-eq · POWER · TRANSPORT · THE REST", "EDGAR 2024 · BY SECTOR · 2023", PlateFigure(ghg, 2),
                    PlateBand.Distribution, 0f, Mathf.Max(0.01f, ghg), s.PowerCo2PerCapita, null, true, new[] { "READOUT · THE KEYS' OWN SHARES" }, null, new[] { "DERIVED" }, false, null, segments, segmentLabels),
                new PlateRow("Electricity CO2 / head", "t CO2 · POWER ÷ POPULATION · LOWER ◂", "EDGAR 2024 · POWER · WB POP · 2023", PlateFigure(s.PowerCo2PerCapita, 2),
                    PlateBand.Open, 0f, 5f, s.PowerCo2PerCapita, EnvironmentPeers(x => x.PowerCo2PerCapita), true, new[] { "CARBON TAX ▸", "ENERGY LINE ▸" }, history?.PowerCo2PerCapita.Quarterly, new[] { "SOURCED" }, true),
                new PlateRow("Transport CO2 / head", "t CO2 · TRANSPORT ÷ POPULATION · LOWER ◂", "EDGAR 2024 · TRANSPORT · WB POP · 2023", PlateFigure(s.TransportCo2PerCapita, 2),
                    PlateBand.Open, 0f, 6f, s.TransportCo2PerCapita, EnvironmentPeers(x => x.TransportCo2PerCapita), true, new[] { "CARBON TAX ▸", "INFRASTRUCTURE LINE ▸" }, history?.TransportCo2PerCapita.Quarterly, new[] { "SOURCED" }, true),
                e.HasMix
                    ? new PlateRow("Electricity by source", "% · COAL·GAS·NUCLEAR·HYDRO·WIND·SOLAR·OTHER", "EMBER · EUROSTAT nrg_bal_peh · EIA · 2023", PlateFigure(e.MixShares[0] + e.MixShares[1], 0, "% FOSSIL"),
                        PlateBand.Distribution, 0f, 100f, -1f, null, true, new[] { "STATIC SEED", "CARBON TAX · OWN PASS" }, null, new[] { "SOURCED", "CROSS-CHECKED" }, false, null, e.MixShares, EnvironmentFamily.MixLabels)
                    : new PlateRow("Electricity by source", "% OF GENERATION", "EMBER · EUROSTAT nrg_bal_peh · EIA 1.1", "billed",
                        PlateBand.Absent, 0f, 100f, -1f, null, false, new[] { "CARBON TAX ▸" }, null, new[] { "BILLED" }, false, "NOT SEEDED FOR THIS COUNTRY · THE SIX ARE"),
            };

            Color areaInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Sectors);
            string footText = "SEEDS: THE EDGAR 2024 GHG BOOKLET, VERIFIED BY CONTENT, OVER WORLD BANK POPULATIONS 2023 · THE HEADLINE IS ALL GASES, THE KEYS CO₂ · THE OWN TICK IS THIS COUNTRY, THE DOTS THE OTHER FIVE AT SEED · THE CARBON TAX'S BASE STAYS ON OUTPUT - MOVING IT HERE IS SHEETED, NOT BUILT · COUPLINGS: THE ENVIRONMENT SPINE'S TABLES, DRAFT UNTIL MEASURED";
            _environmentPlateLastArea = DrawPlateRows(rows, areaInk, footText, draftLive, row =>
            {
                if (!draftLive || !row.Name.StartsWith("Electricity CO2")) { return null; }
                float with = EnvironmentFamily.ProjectPowerCo2(country, draftedRate);
                float without = EnvironmentFamily.ProjectPowerCo2(country, standingRate);
                return (with - without, true, "t");
            });
        }

        private float[] EnvironmentPeers(System.Func<EnvironmentSeeds, float> read)
        {
            var peers = new List<float>();
            World world = _simulationManager.World;
            if (world == null) { return peers.ToArray(); }
            foreach (CountryId id in PeerOrder)
            {
                if (id == PlayerCountryId) { continue; }
                Country c = world.GetCountry(id);
                if (c?.Environment == null || !c.Environment.Seeded) { continue; }
                float v = read(c.Environment);
                if (v >= 0f) { peers.Add(v); }
            }
            return peers.ToArray();
        }
    }
}
