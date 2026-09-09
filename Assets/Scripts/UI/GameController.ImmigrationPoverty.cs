using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// P5-C6 (2026-09-06) - THE IMMIGRATION-AND-POVERTY-DEPTH FAMILY'S PLATE on the shared core (9c, inherited by shape): irregular migration → KEY·OPEN
    /// with a TWO-DEFINITION chip (Eurostat's FLOW for the five, the DHS STOCK for the USA - its own band, never one axis); poverty gap → KEY·BOUNDED lower ◂
    /// (Eurostat's median gap for the five, the OECD's for the USA, the vintage in the source line and the OECD's second reading for the five in the unit
    /// line where they disagree); underemployment → KEY·BOUNDED lower ◂; homelessness → KEY·OPEN with each country's own DEFINITION and year in the unit
    /// line and a DEFINITION chip. Home: the People page's society block, the plate whole under environment.
    /// </summary>
    public partial class GameController
    {
        private Rect _migrationPlateLastArea;

        private void DrawMigrationPovertyFamilyPlate()
        {
            Country country = _playerCountry;
            MigrationPovertySeeds m = country.MigrationPoverty;
            string countryName = DisplayName.Of(country.Id.ToString()).ToUpperInvariant();
            PlateFamily("Immigration · poverty depth", "2022–24", "SOURCED · EUROSTAT · OECD · DHS · BLS");
            if (m == null || !m.Seeded)
            {
                GUILayout.Label("This country carries no immigration and poverty family - the spine covers six, and this is not one of them.", _labelStyle);
                return;
            }

            EconomyState s = country.State;
            StatHistory history = country.History;
            bool stock = m.MigrationIsStock;
            string gapUnit = m.PovertyGapIsOecd
                ? "% BELOW 60 % LINE · OECD · LOWER ◂"
                : "% BELOW 60 % LINE · LOWER ◂ · OECD " + PlateFigure(m.PovertyGapOecd, 1);
            var rows = new List<PlateRow>
            {
                stock
                    ? new PlateRow("Irregular migration", "PER 10 000 · STOCK · UNAUTHORIZED RESIDENTS", "DHS OHSS · 1 JAN " + m.MigrationYear + " · 10.99 M", PlateFigure(s.IrregularMigrationPer10k, 0),
                        PlateBand.Open, 0f, 400f, s.IrregularMigrationPer10k, null, true, new[] { "IMMIGRATION POLICY ▸", "BORDER ENFORCEMENT ▸" }, history?.IrregularMigrationPer10k.Quarterly, new[] { "SOURCED", "STOCK" }, true, flag: DeskProvenance.TwoDefinitionGlyph)
                    : new PlateRow("Irregular migration", "PER 10 000 · FLOW · FOUND ILLEGALLY PRESENT", "EUROSTAT migr_eipre · " + m.MigrationYear, PlateFigure(s.IrregularMigrationPer10k, 1),
                        PlateBand.Open, 0f, 40f, s.IrregularMigrationPer10k, MigrationPeers(x => x.MigrationIsStock ? -1f : x.IrregularMigrationPer10k), true, new[] { "IMMIGRATION POLICY ▸", "BORDER ENFORCEMENT ▸" }, history?.IrregularMigrationPer10k.Quarterly, new[] { "SOURCED", "FLOW" }, true, flag: DeskProvenance.TwoDefinitionGlyph),
                new PlateRow("Poverty gap", gapUnit, (m.PovertyGapIsOecd ? "OECD IDD · PG_INC_DISP · PL_60 · " : "EUROSTAT ilc_li11 · MED_EI · B_60 · ") + m.PovertyGapYear, PlateFigure(s.PovertyGap, 1, "%"),
                    PlateBand.Bounded, 0f, 50f, s.PovertyGap, MigrationPeers(x => x.PovertyGapIsOecd == m.PovertyGapIsOecd ? x.PovertyGap : -1f), true, new[] { "WELFARE GENEROSITY ▸", "MINIMUM WAGE ▸" }, history?.PovertyGap.Quarterly, new[] { "SOURCED", m.PovertyGapIsOecd ? "OECD" : "EUROSTAT" }, true),
                new PlateRow("Underemployment", "% OF EMPLOYMENT · LOWER ◂" + (stock ? " · 16+ BLS" : " · 20–64"), (stock ? "BLS LNS12032194 ÷ LNS12000000 · " : "EUROSTAT lfsi_sup_a ÷ lfsi_emp_a · ") + m.UnderemploymentYear, PlateFigure(s.Underemployment, 2, "%"),
                    PlateBand.Bounded, 0f, 6f, s.Underemployment, MigrationPeers(x => x.Underemployment), true, new[] { "UNEMPLOYMENT GAP ▸" }, null, new[] { "SOURCED" }, true),
                new PlateRow("Homelessness", "PER 10 000 · " + m.HomelessDefinition, "OECD AHD HC3.1.A1 · " + m.HomelessYear, PlateFigure(s.HomelessPer10k, 0),
                    PlateBand.Open, 0f, 60f, s.HomelessPer10k, MigrationPeers(x => x.HomelessPer10k), true, new[] { "HOUSING LINE — PER HEAD ▸", "HOUSING OVERBURDEN ▸" }, history?.HomelessPer10k.Quarterly, new[] { "SOURCED", "DEF " + m.HomelessYear }, true),
            };

            Color areaInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Welfare);
            string footText = "SEEDS: EUROSTAT BY INDEX, THE OECD HOUSING AND INCOME DATABASES, THE DHS 2022 ESTIMATE, BLS · TWO DEFINITIONS WHERE THE SOURCES HAVE TWO - THE FIVE'S FLOW AND THE USA'S STOCK NEVER SHARE AN AXIS, THE GAP'S TWO VINTAGES ARE BOTH PRINTED · HOMELESSNESS CARRIES EACH COUNTRY'S OWN DEFINITION AND YEAR · THE PEER DOTS ARE THE SAME-DEFINITION COUNTRIES ONLY · PER-CAPITA INCOME AND THE EMPLOYMENT BREAKDOWN LIVE ON THEIR OWN INSTRUMENTS · COUPLINGS: THE SPINE'S TABLES, DRAFT UNTIL MEASURED";
            _migrationPlateLastArea = DrawPlateRows(rows, areaInk, footText, false, row => null);
        }

        private float[] MigrationPeers(System.Func<MigrationPovertySeeds, float> read)
        {
            var peers = new List<float>();
            World world = _simulationManager.World;
            if (world == null) { return peers.ToArray(); }
            foreach (CountryId id in PeerOrder)
            {
                if (id == PlayerCountryId) { continue; }
                Country c = world.GetCountry(id);
                if (c?.MigrationPoverty == null || !c.MigrationPoverty.Seeded) { continue; }
                float v = read(c.MigrationPoverty);
                if (v >= 0f) { peers.Add(v); }
            }
            return peers.ToArray();
        }
    }
}
