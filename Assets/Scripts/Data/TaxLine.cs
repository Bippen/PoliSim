using System;

namespace PoliSim.Data
{
    /// <summary>
    /// Rough illustrative weights for how much of GDP each TaxType's base typically represents -
    /// gameplay-tuning constants, not precise economic figures (real tax-base sizes vary hugely by
    /// country and by how exemptions/thresholds are structured; these are flat, uniform stand-ins
    /// so TaxLine.BaseShareOfGdp * Rate gives a plausible-scale revenue contribution per turn).
    /// Tariffs is deliberately absent - it never gets a TaxLine (see TaxType's doc comment) and
    /// GetBaseShareOfGdp returns 0 for it as a defensive fallback.
    /// </summary>
    public static class TaxTypeBaseShares
    {
        public const float IncomeTaxShare = 0.4f;
        public const float CorporateTaxShare = 0.15f;
        public const float VatShare = 0.5f;
        public const float PayrollTaxShare = 0.4f;
        public const float CapitalGainsTaxShare = 0.05f;
        public const float SalesTaxShare = 0.5f;
        public const float ExciseTaxShare = 0.1f;
        public const float PropertyTaxShare = 0.1f;
        public const float EstateTaxShare = 0.02f;
        public const float WealthTaxShare = 0.05f;
        public const float CarbonTaxShare = 0.1f;
        public const float StampDutyShare = 0.02f;

        public static float GetBaseShareOfGdp(TaxType type)
        {
            switch (type)
            {
                case TaxType.IncomeTax: return IncomeTaxShare;
                case TaxType.CorporateTax: return CorporateTaxShare;
                case TaxType.VAT: return VatShare;
                case TaxType.PayrollTax: return PayrollTaxShare;
                case TaxType.CapitalGainsTax: return CapitalGainsTaxShare;
                case TaxType.SalesTax: return SalesTaxShare;
                case TaxType.ExciseTax: return ExciseTaxShare;
                case TaxType.PropertyTax: return PropertyTaxShare;
                case TaxType.EstateTax: return EstateTaxShare;
                case TaxType.WealthTax: return WealthTaxShare;
                case TaxType.CarbonTax: return CarbonTaxShare;
                case TaxType.StampDuty: return StampDutyShare;
                default: return 0f; // Tariffs (and anything unrecognized) contribute 0 through this system
            }
        }
    }

    /// <summary>
    /// Per-TaxType rate bounds a player can directly set TaxLine.Rate to (via
    /// PolicyDecision.TaxRateOverrides) - wide enough that a meaningful policy shift is reachable in
    /// a single turn rather than dozens of small nudges. Gameplay-tuning bounds, not precise legal
    /// maxima. All mins are 0; CarbonTax keeps the original generic [0, 100] bound rather than a
    /// narrower type-specific one.
    /// </summary>
    public static class TaxTypeRateRanges
    {
        public const float IncomeTaxMax = 70f;
        public const float CorporateTaxMax = 50f;
        public const float VatMax = 30f;
        public const float PayrollTaxMax = 75f;
        public const float CapitalGainsTaxMax = 50f;
        public const float SalesTaxMax = 30f;
        public const float ExciseTaxMax = 30f;
        public const float PropertyTaxMax = 10f;
        public const float EstateTaxMax = 60f;
        public const float WealthTaxMax = 5f;
        public const float CarbonTaxMax = 100f;
        public const float StampDutyMax = 30f;

        public static float GetMinRate(TaxType type)
        {
            return 0f;
        }

        public static float GetMaxRate(TaxType type)
        {
            switch (type)
            {
                case TaxType.IncomeTax: return IncomeTaxMax;
                case TaxType.CorporateTax: return CorporateTaxMax;
                case TaxType.VAT: return VatMax;
                case TaxType.PayrollTax: return PayrollTaxMax;
                case TaxType.CapitalGainsTax: return CapitalGainsTaxMax;
                case TaxType.SalesTax: return SalesTaxMax;
                case TaxType.ExciseTax: return ExciseTaxMax;
                case TaxType.PropertyTax: return PropertyTaxMax;
                case TaxType.EstateTax: return EstateTaxMax;
                case TaxType.WealthTax: return WealthTaxMax;
                case TaxType.CarbonTax: return CarbonTaxMax;
                case TaxType.StampDuty: return StampDutyMax;
                default: return 100f; // Tariffs (and anything unrecognized) - never actually used, since Tariffs has no TaxLine
            }
        }
    }

    /// <summary>
    /// One tax instrument in a country's fiscal portfolio: which TaxType, its current Rate (%,
    /// persistent - set turn to turn by PolicyDecision.TaxRateOverrides, not reset), and whether
    /// it's currently implemented (toggled immediately by the player, not deferred to Advance Turn -
    /// see GameController's Tax Policy panel). Revenue only comes from implemented lines; see
    /// SimulationManager.GetTotalTaxRevenue.
    /// </summary>
    [Serializable]
    public class TaxLine
    {
        public TaxType Type;
        public float Rate;
        public bool IsImplemented;

        /// <summary>The UNIFORM stand-in base for this instrument (TaxTypeBaseShares), derived from Type and not stored.
        /// ⚠ Since D-16 (a) (2026-09-04) NO revenue site reads this: every one - the turn's revenue, the household burden
        /// term, the Budget's estimates, the Policy Web's caption, the diagnostics - reads
        /// <see cref="TaxBaseTable.BaseShareOfGdp"/>, which serves the sourced per-country base for the five and this
        /// stand-in for the USA (F-B's perimeter reason) and for any instrument without a sourced row. A new site that
        /// multiplies a rate by THIS property is on the wrong base for five countries; it stays only as the stand-in the
        /// table falls back to.</summary>
        public float BaseShareOfGdp => TaxTypeBaseShares.GetBaseShareOfGdp(Type);

        /// <summary>Derived from Type via TaxTypeRateRanges - the bounds SimulationManager.ApplyTaxRateChanges clamps a requested rate to.</summary>
        public float MinRate => TaxTypeRateRanges.GetMinRate(Type);

        /// <summary>Derived from Type via TaxTypeRateRanges - the bounds SimulationManager.ApplyTaxRateChanges clamps a requested rate to.</summary>
        public float MaxRate => TaxTypeRateRanges.GetMaxRate(Type);

        public TaxLine() { }

        public TaxLine(TaxType type, float rate, bool isImplemented)
        {
            Type = type;
            Rate = rate;
            IsImplemented = isImplemented;
        }

        /// <summary>Used by SimulationManager.PreviewTurn's throwaway country clone - TaxLine.Rate is mutated by ApplyTaxRateChanges, so the preview needs its own copies, not shared references.</summary>
        public TaxLine Clone()
        {
            return new TaxLine(Type, Rate, IsImplemented);
        }
    }
}
