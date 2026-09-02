using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Two blends of a country's own tracked policy data - the size of its state and the reach of its
    /// regulation and welfare - kept for the Statistics comparison after P2-3.2 (2026-09-02) moved the
    /// political compass to the CHES scales. They were the compass's axes from the Political Systems
    /// Overhaul until then; they are readings of what a government does, not positions anyone published,
    /// and the compass now plots only published positions and seat-weighted means of them.
    /// </summary>
    public static class PolicyStanceAxes
    {
        private const float MinAxisValue = 0f;     // CONVENTION: the 0–100 display scale the two blends are clamped to
        private const float MaxAxisValue = 100f;   // CONVENTION: see MinAxisValue

        /// <summary>Average implemented tax rate blended with total government spending as a share of GDP (0–100).</summary>
        public static float GetFiscalSizeAxisValue(Country country)
        {
            float taxSum = 0f;
            int taxCount = 0;
            foreach (TaxLine taxLine in country.TaxLines)
            {
                if (!taxLine.IsImplemented) continue;
                taxSum += taxLine.Rate;
                taxCount++;
            }
            float avgTaxRate = taxCount > 0 ? taxSum / taxCount : 0f;

            float spendingPercentOfGdp;
            if (country.SpendingLines.Count > 0)
            {
                float total = 0f;
                foreach (SpendingLine line in country.SpendingLines)
                {
                    total += line.Amount;
                }
                spendingPercentOfGdp = country.State.GDP > 0f ? total / country.State.GDP * 100f : 0f;
            }
            else
            {
                spendingPercentOfGdp = country.GovernmentSpendingRate;
            }

            return Mathf.Clamp((avgTaxRate + spendingPercentOfGdp) * 0.5f, MinAxisValue, MaxAxisValue);
        }

        /// <summary>Average sector regulation level blended with the average generosity of implemented welfare programs (0–100).</summary>
        public static float GetRegulationWelfareAxisValue(Country country)
        {
            float regulationSum = 0f;
            foreach (Sector sector in country.Sectors)
            {
                regulationSum += sector.RegulationLevel;
            }
            float avgRegulation = country.Sectors.Count > 0 ? regulationSum / country.Sectors.Count : 50f;

            float generositySum = 0f;
            int welfareCount = 0;
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (!program.IsImplemented) continue;
                generositySum += program.GenerosityLevel;
                welfareCount++;
            }
            float avgGenerosity = welfareCount > 0 ? generositySum / welfareCount : 0f;

            return Mathf.Clamp((avgRegulation + avgGenerosity) * 0.5f, MinAxisValue, MaxAxisValue);
        }
    }
}
