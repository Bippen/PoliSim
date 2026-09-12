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
        public const float CarbonTaxShare = 0.1f;   // ⚠ unread since EN-4c (2026-09-11): the carbon line's revenue is its rate per tonne × the taxed tonnes (TaxBases.RevenueAtRate), not a share of GDP; the row stays for the table's completeness
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
    /// maxima. All mins are 0. ⚠ The carbon tax is not a percentage (EN-4c, 2026-09-11): its rate is the
    /// country's currency per tonne of CO₂, so its bound is stated in the book's dollars per tonne and
    /// each country's line carries that bound converted into its own currency (<see cref="TaxLine.RateCeiling"/>).
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
        /// <remarks>CONVENTION (EN-4c, 2026-09-11) - the carbon tax dial's ceiling, US DOLLARS PER TONNE of CO₂ (the book's unit), converted into each country's currency on its line (WorldFactory.SeedTaxLines → TaxLine.RateCeiling, rounded to ten): about twice the highest statutory rate in the set (Sweden's 1 330 SEK, near $125) and the upper reach of the carbon-price paths the IPCC's 1.5 °C pathways carry for 2030 - a dial's reach, not a legal maximum.</remarks>
        public const float CarbonTaxMax = 300f;
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
    ///
    /// <para><b>The carbon tax's rate is not a percentage (EN-4c, ruled 2026-09-11).</b> It is the country's
    /// CURRENCY PER TONNE OF CO₂ - Sweden's 1 330 SEK, Germany's 30 EUR, France's 44.6 EUR at the 2023 seed -
    /// one stored rate, one presented rate, one meaning: the statutory one. Its revenue is rate × the taxed
    /// tonnes (TaxBases.RevenueAtRate) - TRANSPORT's tonnes since EN-4d (2026-09-11, §467: the ETS-covered power
    /// fleet is exempt of the tax by statute, applied at sector level, and the dispatch does not read the tax) - and
    /// the approval and stance terms read it through <see cref="PointsOf"/> - one per cent of the dial's range
    /// per point, as it was when the dial had no unit - so a tax that is a price per tonne and taxes that are
    /// percentages share one political scale.</para>
    /// </summary>
    [Serializable]
    public class TaxLine
    {
        public TaxType Type;
        public float Rate;
        public bool IsImplemented;

        /// <summary>EN-4c: this line's own ceiling where the type's bound is not in the line's unit - the carbon tax's dollars-per-tonne bound converted into the country's currency (WorldFactory.SeedTaxLines); 0 = the type's bound. Persisted with the line.</summary>
        public float RateCeiling;

        /// <summary>True for the one instrument whose rate is a price per tonne rather than a percentage of a base.</summary>
        public bool IsPerTonne => Type == TaxType.CarbonTax;

        /// <summary>A rate change in the POLITICAL scale the approval and stance terms read: a percentage tax's points as they are; the carbon tax's currency-per-tonne change as the per cent of its dial's range it spans (EN-4c) - the scale the dial had when it ran 0–100 without a unit, kept so the terms' authored sensitivities keep their meaning.</summary>
        public float PointsOf(float delta) => IsPerTonne ? delta * 100f / Math.Max(1e-6f, MaxRate) : delta;

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

        /// <summary>Derived from Type via TaxTypeRateRanges - the bounds SimulationManager.ApplyTaxRateChanges clamps a requested rate to - or this line's own ceiling where one is set (the carbon tax, in the country's currency per tonne).</summary>
        public float MaxRate => RateCeiling > 0f ? RateCeiling : TaxTypeRateRanges.GetMaxRate(Type);

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
            return new TaxLine(Type, Rate, IsImplemented) { RateCeiling = RateCeiling };
        }
    }
}
