using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// P5-C3 (2026-09-06) - THE EDUCATION FAMILY'S PLATE, 9c's grammar inherited by shape (the board's own table for C3): PISA mean → KEY·OPEN
    /// 350–550 higher ◂ - a FETCH with no score, so the row prints the word and the source; graduation % → KEY·BOUNDED - a fetch likewise;
    /// early leavers % → KEY·BOUNDED lower ◂ (the USA ABSENT and stated); attainment by level → the DISTRIBUTION form, 2a's stacked bar in the
    /// band cell, one row - drawn here for the first time; students/teacher → CARD-ONLY (the Education minister's card carries them, no page row).
    /// Same six cells, same helpers as the health plate; the plate is one drawing under health on the People page.
    /// </summary>
    public partial class GameController
    {
        private Rect _educationPlateLastArea;

        private void DrawEducationFamilyPlate()
        {
            Country country = _playerCountry;
            EducationSeeds e = country.Education;
            string countryName = DisplayName.Of(country.Id.ToString()).ToUpperInvariant();
            DrawCohortCaption($"EDUCATION · SOCIETY · FAMILY 2 OF 6 · {countryName} · {_simulationManager.CurrentDate.Year}", "SOURCED · OECD EAG · EUROSTAT 2023–25");
            if (e == null || !e.Seeded)
            {
                GUILayout.Label("This country carries no education family - the spine covers six, and this is not one of them.", _labelStyle);
                return;
            }

            EconomyState s = country.State;
            StatHistory history = country.History;
            bool draftLive = false;
            float draftSpending = 0f, standingSpending = 0f;
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (!EducationFamily.IsEducationLine(line.Category)) { continue; }
                standingSpending += line.Amount;
                if (_spendingLineInputs.TryGetValue(line.Category, out float drafted)) { draftSpending += drafted; draftLive = true; }
                else { draftSpending += line.Amount; }
            }

            string attainmentYear = e.AttainmentYear > 0 ? " · " + e.AttainmentYear : "";
            string leaversYear = e.EarlyLeaversYear > 0 ? " · " + e.EarlyLeaversYear : "";
            float[] segments = { s.AttainmentBelowUpperSecondary, s.AttainmentUpperSecondary, s.AttainmentTertiary };
            string[] segmentLabels = { "BELOW UPPER", "UPPER SEC.", "TERTIARY" };
            var rows = new List<PlateRow>
            {
                new PlateRow("Academic score · PISA", "PISA MEAN · HIGHER ◂", "OECD PISA 2022 · VOL. I · TABLES I.B1.2.1–3", "billed",
                    PlateBand.Absent, 350f, 550f, -1f, null, false, new[] { "STUDENTS PER TEACHER ▸", "EFFECTIVENESS (C7) ▸" }, null, new[] { "BILLED" }, false, "BILLED · THE TABLES SIT BEHIND THE WWW HOST, NOT ON THE SDMX API"),
                new PlateRow("Graduation rate", "% AT TYPICAL AGE · UPPER SEC.", "OECD EAG 2024 · TABLE B3.1", "billed",
                    PlateBand.Absent, 60f, 100f, -1f, null, false, new[] { "EARLY LEAVERS ▸" }, null, new[] { "BILLED" }, false, "BILLED · NO OECD FLOW HOLDS THE RATE; THE FLOWS HOLD COUNTS"),
                e.HasEarlyLeavers
                    ? new PlateRow("Early leavers", "% OF 18–24 · LEFT EDUCATION · LOWER ◂", "EUROSTAT edat_lfse_14" + leaversYear, PlateFigure(s.EarlyLeavers, 1, "%"),
                        PlateBand.Bounded, 0f, 20f, s.EarlyLeavers, EducationPeers(x => x.EarlyLeavers), true, new[] { "YOUTH UNEMPLOYMENT ▸", "EDUCATION LINE — PER PUPIL ▸" }, history?.EarlyLeavers.Quarterly, new[] { "SOURCED" }, true)
                    : new PlateRow("Early leavers", "% OF 18–24 · LEFT EDUCATION", "EUROSTAT edat_lfse_14 · NO USA", "absent",
                        PlateBand.Absent, 0f, 20f, -1f, null, true, new[] { "NOT SIMULATED — YOUTH UNEMPLOYMENT REACHES ATTAINMENT DIRECTLY" }, null, new[] { "ABSENT · STATED" }, false, "NCES STATUS DROPOUT IS ANOTHER DEFINITION ON ANOTHER AGE BAND · SE DE FR IT PL REPORT"),
                new PlateRow("Attainment by level · 25–64", "% OF 25–64 · ISCED 0–2 · 3–4 · 5–8", "OECD EAG · LSO_NEAC_DISTR_EA" + attainmentYear, PlateFigure(EducationFamily.AtLeastUpperSecondary(country), 1, "%") + " ≥ UPPER SEC.",
                    PlateBand.Distribution, 0f, 100f, s.AttainmentTertiary, EducationPeers(x => x.Tertiary), false, new[] { "THE STOCK-FLOW LINE ▸", "EARLY LEAVERS ▸" }, history?.AttainmentTertiary.Quarterly, new[] { "SOURCED" }, true, null, segments, segmentLabels),
            };

            Color areaInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Welfare);
            string footText = "SEEDS: OECD EAG AND EUROSTAT, LATEST OBSERVATION PER COUNTRY · THE OWN TICK IS THIS COUNTRY, THE DOTS ARE THE OTHER FIVE AT SEED · THE DISTRIBUTION IS 2a's STACKED BAR, ITS SEGMENTS THE THREE ISCED BANDS · STUDENTS PER TEACHER IS THE CARD'S KEY, NOT A PAGE ROW · BILLED AND ABSENT ARE WORDS · COUPLINGS: THE EDUCATION SPINE'S TABLES, DRAFT UNTIL MEASURED";
            _educationPlateLastArea = DrawPlateRows(rows, areaInk, footText, draftLive, row =>
            {
                if (!row.Name.StartsWith("Early leavers") || !draftLive) { return null; }
                // The 5c arrow on the leavers row: the per-pupil push - next year's early leavers WITH the draft against WITHOUT, in points.
                EducationFamily.Targets with = EducationFamily.TargetsFor(country, draftSpending / Mathf.Max(0.0001f, s.PriceLevel));
                EducationFamily.Targets without = EducationFamily.TargetsFor(country, standingSpending / Mathf.Max(0.0001f, s.PriceLevel));
                if (with.Leavers < 0f || without.Leavers < 0f) { return null; }
                float now = s.EarlyLeavers;
                float deltaPts = (now + (with.Leavers - now) * EducationFamily.LeaversReversionPerYear) - (now + (without.Leavers - now) * EducationFamily.LeaversReversionPerYear);
                return (deltaPts, true, "PTS");
            });
        }

        private float[] EducationPeers(System.Func<EducationSeeds, float> read)
        {
            var peers = new List<float>();
            World world = _simulationManager.World;
            if (world == null) { return peers.ToArray(); }
            foreach (CountryId id in PeerOrder)
            {
                if (id == PlayerCountryId) { continue; }
                Country c = world.GetCountry(id);
                if (c?.Education == null || !c.Education.Seeded) { continue; }
                float v = read(c.Education);
                if (v >= 0f) { peers.Add(v); }
            }
            return peers.ToArray();
        }
    }
}
